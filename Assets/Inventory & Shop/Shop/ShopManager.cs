using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopSlot[] shopSlots;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ToolManager toolManager;

    public void PopulateShopItems(List<ShopItems> shopItems)
    {
        for (int i = 0; i < shopItems.Count && i < shopSlots.Length; i++)
        {
            ShopItems shopItem = shopItems[i];
            shopSlots[i].InitializeItem(shopItem.itemSO, shopItem.price);
            shopSlots[i].gameObject.SetActive(true);
        }
        for (int i = shopItems.Count; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(false);
        }
    }
    
    public void PopulateShopTools(List<ShopTools> shopTools)
    {
        for (int i = 0; i < shopTools.Count && i < shopSlots.Length; i++)
        {
            ShopTools shopTool = shopTools[i];
            shopSlots[i].InitializeTool(shopTool.toolSO, shopTool.price);
            shopSlots[i].gameObject.SetActive(true);
        }
        for (int i = shopTools.Count; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(false);
        }
    }
    
    public void TryBuyItem(ItemSO itemSO, int price)
    {
        if (itemSO != null && inventoryManager.gold >= price)
        {
            if (HasSpaceForItem(itemSO))
            {
                inventoryManager.gold -= price;
                inventoryManager.goldText.text = inventoryManager.gold.ToString();
                inventoryManager.AddItem(itemSO, 1);
            }
        }
    }
    
    public void TryBuyTool(ToolSO toolSO, int price)
    {
        if (toolSO != null && inventoryManager.gold >= price)
        {
            if (HasSpaceForTool(toolSO))
            {
                inventoryManager.gold -= price;
                inventoryManager.goldText.text = inventoryManager.gold.ToString();
                toolManager.AddTool(toolSO, 1);
            }
        }
    }
    
    private bool HasSpaceForItem(ItemSO itemSO)
    {
        foreach (var slot in inventoryManager.itemSlots)
        {
            if (slot.itemSO == itemSO && slot.quantity < itemSO.stackSize)
            {
                return true;
            }
            else if (slot.itemSO == null)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool HasSpaceForTool(ToolSO toolSO)
    {
        foreach (var slot in toolManager.toolSlots)
        {
            if (slot.toolSO == toolSO && slot.quantity < toolSO.maxStackSize)
            {
                return true;
            }
            else if (slot.toolSO == null)
            {
                return true;
            }
        }
        return false;
    }
    
    public void SellItem(ItemSO itemSO)
    {
        if (itemSO == null)
            return;
        foreach (var slot in shopSlots)
        {
            if (slot.itemSO == itemSO)
            {
                inventoryManager.gold += slot.price;
                inventoryManager.goldText.text = inventoryManager.gold.ToString();
                return;
            }
        }
    }
    
    public void SellTool(ToolSO toolSO)
    {
        if (toolSO == null)
            return;
            
        inventoryManager.gold += toolSO.sellPrice;
        inventoryManager.goldText.text = inventoryManager.gold.ToString();
    }
}

[System.Serializable]
public class ShopItems
{
    public ItemSO itemSO;
    public int price;
}