using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Planner : MonoBehaviour
{
    private readonly List<Tuple<Vector3, Vector3>> _debugRayList = new List<Tuple<Vector3, Vector3>>();
    public bool isSuperManPlanner;

    private void Start()
    {
        StartCoroutine(Plan());
    }

    private void Check(Dictionary<string, object> state, ItemType type)
    {

        var items = Navigation.instance.AllItems();
        var inventories = Navigation.instance.AllInventories();
        var floorItems = items.Except(inventories);
        var item = floorItems.FirstOrDefault(x => x.type == type);
        var here = transform.position;
        state["accessible" + type.ToString()] = item != null && Navigation.instance.Reachable(here, item.transform.position, _debugRayList);

        state["dead" + type.ToString()] = false;
    }

    private IEnumerator Plan()
    {
        yield return new WaitForSeconds(0.2f);

        var observedState = new Dictionary<string, object>();

        var nav = Navigation.instance;
        var floorItems = nav.AllItems();
        var inventory = nav.AllInventories();
        var everything = nav.AllItems().Union(nav.AllInventories());

        Check(observedState, ItemType.Entity);
        Check(observedState, ItemType.Sword);
        Check(observedState, ItemType.Enemy);
        Check(observedState, ItemType.Training);
        Check(observedState, ItemType.Door);
        Check(observedState, ItemType.Coins);

        var actions = CreatePossibleActionsList();

        GoapState initial = new GoapState();
        initial.worldState = new WorldState()
        {
            isSuperMan = isSuperManPlanner,
            healPoints = 100,
            exp = 0,
            values = new Dictionary<string, object>()
        };

        initial.worldState.values = observedState;

        //foreach (var item in initial.worldState.values)
        //{
        //    Debug.Log(item.Key + " ---> " + item.Value);
        //}

        GoapState goal = new GoapState();
        goal.worldState.values["has" + ItemType.Sword.ToString()] = true;


        Func<GoapState, float> heuristc = (curr) =>
        {
            int count = 0;
            string key = "has" + ItemType.Sword.ToString();
            if (!curr.worldState.values.ContainsKey(key) || !(bool)curr.worldState.values[key])
                count++;
            return count;
        };

        Func<GoapState, bool> objectice = (curr) =>
         {
             string key = "has" + ItemType.Sword.ToString();
             return curr.worldState.values.ContainsKey(key) && (bool)curr.worldState.values["has" + ItemType.Sword.ToString()];
         };

        var actDict = new Dictionary<string, ActionEntity>() {
              { "Kill"  , ActionEntity.Kill }
            , { "Pickup", ActionEntity.PickUp }
            , { "Buy"  , ActionEntity.Buy}
            , { "Training"  , ActionEntity.Training }
            , { "Heal"  , ActionEntity.Heal }
        };

        var plan = Goap.Execute(initial, null, objectice, heuristc, actions);

        if (plan == null)
            Debug.Log("Couldn't plan");
        else
        {
            GetComponent<Guy>().ExecutePlan(
                plan
                .Select(a =>
                {
                    Item i2 = everything.FirstOrDefault(i => i.type == a.item);
                    if (actDict.ContainsKey(a.Name) && i2 != null)
                    {
                        return Tuple.Create(actDict[a.Name], i2);
                    }
                    else
                    {
                        return null;
                    }
                }).Where(a => a != null)
                .ToList()
            );
        }
    }

    private List<GoapAction> CreatePossibleActionsList()
    {
        return new List<GoapAction>()
        {

            new GoapAction("Pickup")
                .SetCost(1f)
                .SetItem(ItemType.Sword)
                .Pre((gS) =>
                {
                    return gS.worldState.values.ContainsKey("accessible"+ ItemType.Sword.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Sword.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.values["accessible"+ ItemType.Sword.ToString()] = false;
                    gS.worldState.values["has"+ ItemType.Sword.ToString()] = true;
                    return gS;
                    }
                )


            , new GoapAction("Buy")
                .SetCost(10f)
                .SetItem(ItemType.Door)
                .Pre((gS) =>
                {
                    return gS.worldState.specialItem == "Coins" &&
                            gS.worldState.values.ContainsKey("accessible"+ ItemType.Door.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Door.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.values["accessible"+ ItemType.Door.ToString()] = false;
                    gS.worldState.values["accessible"+ ItemType.Sword.ToString()] = true;
                    return gS;
                    }
                )

             , new GoapAction("Pickup")
                .SetCost(1f)
                .SetItem(ItemType.Coins)
                .Pre((gS) =>
                {
                    return  gS.worldState.values.ContainsKey("accessible"+ ItemType.Coins.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Coins.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.specialItem = "Coins";
                    gS.worldState.values["accessible"+ ItemType.Door.ToString()] = true;
                    return gS;
                    }
                )


            , new GoapAction("Kill")
                .SetCost(5f)
                .SetItem(ItemType.Enemy)
                .Pre((gS) =>
                {
                    return  gS.worldState.exp >= 50 &&
                            gS.worldState.values.ContainsKey("accessible"+ ItemType.Enemy.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Enemy.ToString()];

                })

                .Effect((gS) =>
                {
                     if (!gS.worldState.isSuperMan)
                    {
                         gS.worldState.healPoints -= 30;
                    }

                    gS.worldState.values["dead"+ ItemType.Enemy.ToString()] = true;
                    gS.worldState.values["accessible"+ ItemType.Coins.ToString()] = true;
                    gS.worldState.values["accessible"+ ItemType.Heal1.ToString()] = true;
                    return gS;
                })

            , new GoapAction("Kill")
                .SetCost(5f)
                .SetItem(ItemType.Enemy2)
                .Pre((gS) =>
                {
                    return  gS.worldState.exp >= 150 &&
                            gS.worldState.values.ContainsKey("accessible"+ ItemType.Enemy2.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Enemy2.ToString()];

                })

                .Effect((gS) =>
                {
                    if (!gS.worldState.isSuperMan)
                    {
                         gS.worldState.healPoints -= 30;
                    }

                    gS.worldState.values["dead"+ ItemType.Enemy2.ToString()] = true;
                    gS.worldState.values["accessible"+ ItemType.Sword.ToString()] = true;
                    return gS;
                })

             , new GoapAction("Training")
                .SetCost(2f)
                .SetItem(ItemType.Training)
                .Pre((gS) =>
                {
                    return  gS.worldState.values.ContainsKey("accessible"+ ItemType.Training.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Training.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.exp += 100;
                    gS.worldState.values["accessible"+ ItemType.Training.ToString()] = false;
                    gS.worldState.values["accessible"+ ItemType.Enemy.ToString()] = true;
                    return gS;
                })

            , new GoapAction("Training")
                .SetCost(2f)
                .SetItem(ItemType.Training2)
                .Pre((gS) =>
                {
                    return  gS.worldState.values.ContainsKey("accessible"+ ItemType.Training2.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Training2.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.exp += 100;
                    gS.worldState.values["accessible"+ ItemType.Training2.ToString()] = false;
                    gS.worldState.values["accessible"+ ItemType.Enemy2.ToString()] = true;
                    return gS;
                })

            , new GoapAction("Heal")
                .SetCost(3f)
                .SetItem(ItemType.Heal1)
                .Pre((gS) =>
                {
                    return  gS.worldState.values.ContainsKey("accessible"+ ItemType.Heal1.ToString()) &&
                            (bool)gS.worldState.values["accessible"+ ItemType.Heal1.ToString()];

                })

                .Effect((gS) =>
                {
                    gS.worldState.healPoints += (100 - gS.worldState.healPoints);
                    gS.worldState.values["accessible"+ ItemType.Heal1.ToString()] = false;
                    gS.worldState.values["accessible"+ ItemType.Training2.ToString()] = true;
                    return gS;
                })

        };
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var t in _debugRayList)
        {
            Gizmos.DrawRay(t.Item1, (t.Item2 - t.Item1).normalized);
            Gizmos.DrawCube(t.Item2 + Vector3.up, Vector3.one * 0.2f);
        }
    }

}
