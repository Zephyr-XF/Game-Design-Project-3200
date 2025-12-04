using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    public ItemSO itemSO;
    public int quantity;
    public Image itemImage;
    public TMP_Text quantityText;
    private InventoryManager inventoryManager;
    private static ShopManager activeShop;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private void Start()
    {
        inventoryManager = GetComponentInParent<InventoryManager>();
    }
    private void OnEnable()
    {
        ShopKeeper.OnShopStateChanged += HandleShopStateChanged;
    }
    private void OnDisable()
    {
        ShopKeeper.OnShopStateChanged -= HandleShopStateChanged;
    }
    private void HandleShopStateChanged(ShopManager shopManager, bool isOpen)
    {
        activeShop = isOpen ? shopManager : null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (quantity > 0)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (activeShop != null)
                {
                    activeShop.SellItem(itemSO);
                    quantity--;
                    UpdateUI();
                }
                else{
                    if (itemSO.currentHealth > 0 && StatsManager.Instance.currentHealth >= StatsManager.Instance.maxHealth)
                        return;
                    inventoryManager.UseItem(this);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                inventoryManager.DropItem(this);
            }
        }
    }

    public void UpdateUI()
    {
        if (enableDebug)
            Debug.Log($"[InventorySlot] UpdateUI 调用 - Slot: {name}, ItemSO: {itemSO?.itemName ?? "null"}, Quantity: {quantity}");
        
        if (quantity <= 0)
        {
            itemSO = null;
            if (enableDebug)
                Debug.Log($"[InventorySlot] 数量<=0，清空itemSO - Slot: {name}");
        }
        if(itemSO != null)
        {
            itemImage.sprite = itemSO.icon;
            itemImage.gameObject.SetActive(true);
            quantityText.text = quantity.ToString();
            
            if (enableDebug)
                Debug.Log($"[InventorySlot] 显示物品 - Slot: {name}, Item: {itemSO.itemName}, Icon: {itemSO.icon?.name ?? "null"}, Image Active: {itemImage.gameObject.activeSelf}");
        }
        else
        {
            itemImage.gameObject.SetActive(false);
            quantityText.text = "";
            
            if (enableDebug)
                Debug.Log($"[InventorySlot] 隐藏物品图标 - Slot: {name}");
        }
    }
}
