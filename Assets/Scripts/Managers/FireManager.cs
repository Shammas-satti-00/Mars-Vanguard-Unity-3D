using UnityEngine;

public class FireManager : MonoBehaviour
{
    public EquipmentManager em;


    public void FireCannons()
    {
        em.FireAllCannons();
    }

    public void FireLaunchers()
    {
        em.FireAllLaunchers();
    }

    public void ActivateBoost()
    {
        em.ActivateBoost();
    }

    public void DeactivateBoost()
    {
        em.DeactivateBoost();
    }


    public void SetEquipmentManager(EquipmentManager em)
    {
        this.em = em;
    }



}
