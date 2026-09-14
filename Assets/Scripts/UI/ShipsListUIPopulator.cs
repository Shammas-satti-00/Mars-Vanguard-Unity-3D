using System;
using UnityEngine;
using UnityEngine.UI;

public class ShipListUIpopulator : MonoBehaviour
{
    public GameObject listEntryPrefab;
    public Transform listContent; // better as Transform for parenting
    public HangarManager manager;

    void Start()
    {

        foreach (var ship in DataHolder.Instance.avaliableShips)
        {
            GameObject entry = Instantiate(listEntryPrefab, listContent);
            entry.GetComponent<Image>().sprite = DataHolder.Instance.icons[ship.spriteIndex];
            //Button btn = entry.GetComponentInChildren<Button>();
            //if (btn != null)
            //{
            //    btn.onClick.AddListener(() =>
            //    {
            //        manager.SelectCannon(index);
            //    });
            //}
        }
    }
}
