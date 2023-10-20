using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using IA2;

public enum ActionEntity
{
    NextStep,
    FailedStep,
    Success,
    Kill,
    PickUp,
    Buy,
    Training,
    Heal
}

public class Guy : MonoBehaviour
{
    private EventFSM<ActionEntity> _fsm;
    private Item _target;
    private Entity _ent;


    IEnumerable<Tuple<ActionEntity, Item>> _plan;

    private void PerformAttack(Entity us, Item other)
    {
        Debug.Log("PerformAttack", other.gameObject);
        if (other != _target) return;
        other.Kill();
        _fsm.Feed(ActionEntity.NextStep);
    }

    private void PerformTraining(Entity us, Item other)
    {
        Debug.Log("PerformTraining", other.gameObject);
        if (other != _target) return;
        Vector3 scaleChange = new Vector3(0.2f, 0.2f, 0.2f);
        this.transform.localScale += scaleChange;
        other.GetComponent<BoxCollider>().enabled = false;
        _fsm.Feed(ActionEntity.NextStep);
    }

    private void PerformHeal(Entity us, Item other)
    {
        Debug.Log("PerformHeal", other.gameObject);
        if (other != _target) return;
        other.GetComponent<ParticleSystem>().Play();
        other.GetComponent<BoxCollider>().enabled = false;
        _fsm.Feed(ActionEntity.NextStep);
    }

    private void PerformBuy(Entity us, Item other)
    {
        Debug.Log("PerformBuy", other.gameObject);

        if (other != _target) return;

        var coins = _ent.items.FirstOrDefault(it => it.type == ItemType.Coins);
        var door = other.GetComponent<Door>();
        if (door && coins)
        {
            door.Open();
            Destroy(_ent.Removeitem(coins).gameObject);
            _fsm.Feed(ActionEntity.NextStep);
        }
        else
        {
            _fsm.Feed(ActionEntity.FailedStep);
        }

    }

    private void PerformPickUp(Entity us, Item other)
    {
        Debug.Log("PerformPickUp", other.gameObject);

        if (other != _target) return;
        _ent.AddItem(other);
        _fsm.Feed(ActionEntity.NextStep);

    }

    private void NextStep(Entity ent, Waypoint wp, bool reached)
    {
        _fsm.Feed(ActionEntity.NextStep);
    }

    private void Awake()
    {
        _ent = GetComponent<Entity>();

        var any = new State<ActionEntity>("any");

        var idle = new State<ActionEntity>("idle");
        var bridgeStep = new State<ActionEntity>("planStep");
        var failStep = new State<ActionEntity>("failStep");
        var success = new State<ActionEntity>("success");

        var kill = new State<ActionEntity>("kill");
        var pickup = new State<ActionEntity>("pickup");
        var buy = new State<ActionEntity>("buy");
        var training = new State<ActionEntity>("training");
        var heal = new State<ActionEntity>("heal");


        kill.OnEnter += a =>
        {
            _ent.GoTo(_target.transform.position);
            _ent.OnHitItem += PerformAttack;
        };

        kill.OnExit += a => _ent.OnHitItem -= PerformAttack;
        
        pickup.OnEnter += a => { _ent.GoTo(_target.transform.position); _ent.OnHitItem += PerformPickUp; };
        pickup.OnExit += a => _ent.OnHitItem -= PerformPickUp;

        buy.OnEnter += a => { _ent.GoTo(_target.transform.position); _ent.OnHitItem += PerformBuy; };
        buy.OnExit += a => _ent.OnHitItem -= PerformBuy;

        heal.OnEnter += a => { _ent.GoTo(_target.transform.position); _ent.OnHitItem += PerformHeal; };
        heal.OnExit += a => _ent.OnHitItem -= PerformHeal;

        training.OnEnter += a =>
        {
            _ent.GoTo(_target.transform.position);
            _ent.OnHitItem += PerformTraining;
        };
        training.OnExit += a => _ent.OnHitItem -= PerformTraining;


        failStep.OnEnter += a => { _ent.Stop(); Debug.Log("Plan failed"); };

        bridgeStep.OnEnter += a =>
        {
            var step = _plan.FirstOrDefault();
            if (step != null)
            {

                _plan = _plan.Skip(1);
                var oldTarget = _target;
                _target = step.Item2;
                if (!_fsm.Feed(step.Item1))
                    _target = oldTarget;
            }
            else
            {
                _fsm.Feed(ActionEntity.Success);
            }
        };

        success.OnEnter += a => { Debug.Log("Success"); };
        success.OnUpdate += () => { _ent.Jump(); };

        StateConfigurer.Create(any)
            .SetTransition(ActionEntity.NextStep, bridgeStep)
            .SetTransition(ActionEntity.FailedStep, idle)
            .Done();

        StateConfigurer.Create(bridgeStep)
            .SetTransition(ActionEntity.Kill, kill)
            .SetTransition(ActionEntity.PickUp, pickup)
            .SetTransition(ActionEntity.Buy, buy)
            .SetTransition(ActionEntity.Success, success)
            .SetTransition(ActionEntity.Training, training)
            .SetTransition(ActionEntity.Heal, heal)
            .Done();

        _fsm = new EventFSM<ActionEntity>(idle, any);
    }

    public void ExecutePlan(List<Tuple<ActionEntity, Item>> plan)
    {
        _plan = plan;
        _fsm.Feed(ActionEntity.NextStep);
    }

    private void Update()
    {
        _fsm.Update();
    }

}
