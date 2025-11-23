using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public int gold;
    public TMP_Text goldText;
    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }
    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    private void AddItem(ItemSO itemSO, int quantity)
    {
        if(itemSO.isGold)
        {
            
        }
    }
}
