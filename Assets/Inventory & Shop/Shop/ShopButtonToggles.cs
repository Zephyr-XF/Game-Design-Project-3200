using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopButtonToggles : MonoBehaviour
{
    public void OpenItemShop()
    {
        if (ShopKeeper.currentShopKeeper != null)
        {
            ShopKeeper.currentShopKeeper.OpenItemShop();
        }
    }
    
    public void OpenToolShop()
    {
        if (ShopKeeper.currentShopKeeper != null)
        {
            ShopKeeper.currentShopKeeper.OpenToolShop();
        }
    }
}