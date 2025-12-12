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
    
    [Header("ÉÌµêÎïÆ·ÁÐ±í")]
    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopTools> shopTools;
    
    public static event Action<ShopManager, bool> OnShopStateChanged;
    private bool playerInRange;
    private bool isShopOpen;
    
    void Update()
    {
        // ä¼åå¤çå³é­ï¼åªè¦ååºå¼çï¼æäºå¨é®å°±åºè¯¥è½å³é­ (ä¸ç®¡æ¯å¦å¨èå´å)
        if (isShopOpen)
        {
            if (Input.GetButtonDown("Interact") || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShopUI();
            }
            return; 
        }

        // åªæå¨æ²¡å¼åºä¸äººå¨èå´åæ¶ï¼æåè®¸æå¼
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
    
    public void OpenToolShop()
    {
        shopManager.PopulateShopTools(shopTools);
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

[System.Serializable]
public class ShopTools
{
    public ToolSO toolSO;
    public int price;
}
