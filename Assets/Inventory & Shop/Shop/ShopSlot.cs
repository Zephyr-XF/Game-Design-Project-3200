using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("物品信息")]
    public ItemSO itemSO;
    public ToolSO toolSO;
    
    [Header("UI元素")]
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Image itemImage;
    
    [Header("管理器")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;
    
    public int price;
    private bool isTool;
    
    public void InitializeItem(ItemSO newItemSO, int price)
    {
        isTool = false;
        itemSO = newItemSO;
        toolSO = null;
        itemImage.sprite = itemSO.icon;
        itemNameText.text = itemSO.itemName;
        this.price = price;
        priceText.text = price.ToString();
    }
    
    public void InitializeTool(ToolSO newToolSO, int price)
    {
        isTool = true;
        toolSO = newToolSO;
        itemSO = null;
        itemImage.sprite = toolSO.icon;
        itemNameText.text = toolSO.toolName;
        this.price = price;
        priceText.text = price.ToString();
    }
    
    public void OnBuyButtonClicked()
    {
        if (isTool)
        {
            shopManager.TryBuyTool(toolSO, price);
        }
        else
        {
            shopManager.TryBuyItem(itemSO, price);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shopInfo != null)
        {
            if (isTool && toolSO != null)
            {
                shopInfo.ShowToolInfo(toolSO);
            }
            else if (!isTool && itemSO != null)
            {
                shopInfo.ShowItemInfo(itemSO);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopInfo != null)
        {
            shopInfo.HideItemInfo();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (shopInfo != null)
        {
            shopInfo.FollowMouse();
        }
    }
}
