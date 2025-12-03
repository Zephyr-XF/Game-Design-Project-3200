using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public ItemSO itemSO;
    public SpriteRenderer sr;
    public Animator anim;
    public bool canBePickedUp = true;
    public int quantity;

    [Header("Sorting")]
    public string sortingLayerName = "Low";
    public int sortingOrder = 0;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip goldPickupSound;      // 金币拾取音效
    public AudioClip itemPickupSound;      // 普通物品拾取音效

    [Header("调试")]
    public bool enableDebug = false;

    public static event Action<ItemSO, int> OnItemLooted;

    private bool isInitialized = false;

    private void Awake()
    {
        // 强制检查：如果是默认层则修正为 Low
        if (string.IsNullOrEmpty(sortingLayerName) || sortingLayerName == "Default")
        {
            sortingLayerName = "Low";
        }

        if (sr != null && !string.IsNullOrEmpty(sortingLayerName))
        {
            sr.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            sr.sortingOrder = sortingOrder;
        }

        // 强制禁用 AudioSource 的 PlayOnAwake，防止自动播放
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
            
            if (enableDebug)
                Debug.Log($"[Loot] AudioSource.playOnAwake 已设置为 false");
        }

        // 延迟初始化
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[Loot] ✓ 初始化完成，音效系统已启用 - Time.time: {Time.time}");
    }

    private void OnValidate()
    {
        if (itemSO == null) return;
        UpdateAppearance();
    }

    public void Initialize(ItemSO itemSO, int quantity)
    {
        this.itemSO = itemSO;
        this.quantity = quantity;
        canBePickedUp = false;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (sr == null) return;
        
        sr.sprite = itemSO.icon;
        this.name = itemSO.itemName;

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            sr.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            sr.sortingOrder = sortingOrder;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && canBePickedUp == true)
        {
            if (enableDebug)
                Debug.Log($"[Loot] 玩家拾取物品: {itemSO.itemName}, isGold: {itemSO.isGold}, isInitialized: {isInitialized}");

            // 播放拾取音效
            PlayPickupSound();

            anim.Play("LootPickup");
            OnItemLooted?.Invoke(itemSO, quantity);
            Destroy(gameObject, 0.5f);
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canBePickedUp = true;
        }
    }

    private void PlayPickupSound()
    {
        // 只有在初始化完成后才播放音效
        if (!isInitialized)
        {
            if (enableDebug)
                Debug.Log($"[Loot] ❌ 初始化未完成，跳过播放拾取音效");
            return;
        }

        if (audioSource == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[Loot] ⚠ AudioSource 为空，无法播放音效");
            return;
        }

        // 根据物品的 isGold 属性选择音效
        if (itemSO != null && itemSO.isGold)
        {
            // 播放金币音效
            if (goldPickupSound != null)
            {
                audioSource.PlayOneShot(goldPickupSound);
                if (enableDebug)
                    Debug.Log($"[Loot] ✓ 播放金币拾取音效 - AudioClip: {goldPickupSound.name}");
            }
            else if (enableDebug)
            {
                Debug.LogWarning($"[Loot] ⚠ goldPickupSound 为空");
            }
        }
        else
        {
            // 播放普通物品音效
            if (itemPickupSound != null)
            {
                audioSource.PlayOneShot(itemPickupSound);
                if (enableDebug)
                    Debug.Log($"[Loot] ✓ 播放物品拾取音效 - AudioClip: {itemPickupSound.name}");
            }
            else if (enableDebug)
            {
                Debug.LogWarning($"[Loot] ⚠ itemPickupSound 为空");
            }
        }
    }
}
