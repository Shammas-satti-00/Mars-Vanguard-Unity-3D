using UnityEngine;

public class Collectable : MonoBehaviour
{
    public CollectableType collectable;
    public int amount;
    private Collider _other;
    

    public void OnCollect()
    {
        switch (collectable)
        {
            case CollectableType.Health: RestoreHealth(); break;
            case CollectableType.Shield: RestoreShield(); break;
            case CollectableType.LauncherAmmo: RestoreLauncherAmmo(); break;
            case CollectableType.CannonAmmo: RestoreCannonAmmo(); break;
            case CollectableType.Coin: CollectCoins(); break;
        }
    }

    void RestoreHealth()
    {
        _other.GetComponent<DamageHandler>().ApplyHeal(amount);
    }

    void RestoreShield()
    {
        _other.GetComponent<DamageHandler>().ApplyShield(amount);
    }

    void RestoreCannonAmmo()
    {
        _other.GetComponent<EquipmentManager>().AddAmmoToAllCannons(amount);
    }

    void RestoreLauncherAmmo()
    {
        _other.GetComponent<EquipmentManager>().AddAmmoToAllLaunchers(amount);
    }

    void CollectCoins()
    {
        GameManager.Instance.AddCoins(amount);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(DataHolder.Instance.playerTag))
        { 
            _other = other;
            OnCollect();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(DataHolder.Instance.playerTag))
        {
            _other = collision.collider;
            OnCollect();
        }
    }
}

public enum CollectableType
{
    CannonAmmo,
    LauncherAmmo,
    Health,
    Shield,
    Coin,
}