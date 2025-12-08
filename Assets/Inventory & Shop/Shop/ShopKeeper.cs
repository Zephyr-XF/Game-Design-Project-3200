using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper currentShopKeeper;
    public Animator anim;
    public CanvasGroup shopCanvasGroup;
    public ShopManager shopManager;
    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopWeapons;
    [SerializeField] private List<ShopItems> shopArmor;
    public static event Action<ShopManager, bool> OnShopStateChanged;
    private bool playerInRange;
    private bool isShopOpen;
    void Update()
    {
        // 优先处理关闭：只要商店开着，按互动键就应该能关闭 (不管是否在范围内)
        if (isShopOpen)
        {
            if (Input.GetButtonDown("Interact") || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShopUI();
            }
            return; 
        }

        // 只有在没开店且人在范围内时，才允许打开
        if (playerInRange)
        {
            if (Input.GetButtonDown("Interact"))
            {
                 OpenShopUI();
            }
        }
    }

    public void OpenShopUI()
    {
        if (isShopOpen) return;

        Time.timeScale = 0;
        currentShopKeeper = this;
        isShopOpen = true;
        OnShopStateChanged?.Invoke(shopManager, true);
        shopCanvasGroup.alpha = 1;
        shopCanvasGroup.blocksRaycasts = true;
        shopCanvasGroup.interactable = true;
        OpenItemShop();
    }

    public void CloseShopUI()
    {
        if (!isShopOpen) return;

        Time.timeScale = 1;
        currentShopKeeper = null;
        isShopOpen = false;
        OnShopStateChanged?.Invoke(shopManager, false);
        shopCanvasGroup.alpha = 0;
        shopCanvasGroup.blocksRaycasts = false;
        shopCanvasGroup.interactable = false;
    }
    public void OpenItemShop()
    {
        shopManager.PopulateShopItems(shopItems);
    }
    public void OpenWeaponShop()
    {
        
        shopManager.PopulateShopItems(shopWeapons);
    }
    public void OpenArmorShop()
    {
        
        shopManager.PopulateShopItems(shopArmor);
    }
    
     private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            anim.SetBool("playInRange", true);
            playerInRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            anim.SetBool("playInRange", false);
            playerInRange = false;
        }
    }
}
