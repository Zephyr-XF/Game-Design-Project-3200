using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public ItemSO itemSO;
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Image itemImage;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;
    public  int price;
    public void Initialize(ItemSO newItemSO, int price)
    {
        itemSO = newItemSO;
        itemImage.sprite = itemSO.icon;
        itemNameText.text = itemSO.itemName;
        this.price = price;
        priceText.text = price.ToString();
    }
    public void OnBuyButtonClicked()
    {
        shopManager.TryBuyItem(itemSO, price); 

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shopInfo != null)
            shopInfo.ShowItemInfo(itemSO);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        shopInfo.HideItemInfo();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (shopInfo != null)
        {
            shopInfo.FollowMouse();
        }
    }
}
