using UnityEngine;

public class ShipSelectButton : MonoBehaviour
{
    public HangarManager hm;
    public int Index => hm.currentIndex;

    public void SelectShip()
    {
        string equippedShip = DataHolder.Instance.avaliableShips[Index]._name;
        PlayerPrefs.SetString("Selected_Ship_Name", equippedShip);
        PlayerPrefs.SetInt("Hanger_shipIndex", Index);
        hm.UpdateShipUiState();

    }
}