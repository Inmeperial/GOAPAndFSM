using UnityEngine;
using System.Collections;

public enum ItemType
{
    Invalid,
    Entity,
    Sword,
    Enemy,
    Enemy2,
    Training,
    Training2,
    Door,
    Coins,
    Heal1,
    Heal2
}

public class Item : MonoBehaviour
{
    public ItemType type;
    private Waypoint _wp;
    private bool _insideInventory;

    public void OnInventoryAdd()
    {
        Destroy(GetComponent<Rigidbody>());
        _insideInventory = true;
        if (_wp)
            _wp.nearbyItems.Remove(this);
    }

    public void OnInventoryRemove()
    {
        gameObject.AddComponent<Rigidbody>();
        _insideInventory = false;
    }

    private void Start()
    {
        _wp = Navigation.instance.NearestTo(transform.position);
        _wp.nearbyItems.Add(this);
    }

    public void Kill()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {

        try
        {
            _wp.nearbyItems.Remove(this);
        }
        catch (System.Exception)
        {
            Debug.Log("OnDestroy with error = " + this.gameObject.name);
            throw;
        }

    }

    private void Update()
    {
        if (!_insideInventory)
        {
            _wp.nearbyItems.Remove(this);
            _wp = Navigation.instance.NearestTo(transform.position);
            _wp.nearbyItems.Add(this);
        }
    }
}
