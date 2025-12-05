using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolLoot : MonoBehaviour
{
    public ToolSO toolSO;
    public SpriteRenderer sr;
    public Animator anim;
    public bool canBePickedUp = true;
    public int quantity = 1;

    [Header("Sorting")]
    public string sortingLayerName = "Low";
    public int sortingOrder = 0;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip toolPickupSound;

    [Header("Debugging")]
    public bool enableDebug = false;

    public static event Action<ToolSO, int> OnToolLooted;

    private bool isInitialized = false;

    private void Awake()
    {
        // 如果 SpriteRenderer 没有手动赋值，自动查找
        if (sr == null)
        {
            // 1. 先在当前对象查找
            sr = GetComponent<SpriteRenderer>();
            
            // 2. 如果没找到，在子对象查找
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
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
                Debug.Log($"[ToolLoot] AudioSource.playOnAwake set to false");
        }

        // 延迟初始化
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[ToolLoot] ✓ Initialization complete, audio system enabled - Time.time: {Time.time}");
    }

    private void OnValidate()
    {
        if (toolSO == null) return;
        UpdateAppearance();
    }

    public void Initialize(ToolSO toolSO, int quantity)
    {
        this.toolSO = toolSO;
        this.quantity = quantity;
        canBePickedUp = false;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        // 如果 sr 为空，尝试查找
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        if (sr == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolLoot] SpriteRenderer 未找到，无法更新外观");
            return;
        }
        
        // 只更新图标如果ToolSO有icon
        if (toolSO.icon != null)
        {
            sr.sprite = toolSO.icon;
        }
        
        this.name = toolSO.toolName;

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
                Debug.Log($"[ToolLoot] Player picked up tool: {toolSO.toolName}, isInitialized: {isInitialized}");

            // Play pickup sound
            PlayPickupSound();

            if (anim != null)
            {
                anim.Play("LootPickup");
            }

            OnToolLooted?.Invoke(toolSO, quantity);
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
        // Play sound only after initialization is complete
        if (!isInitialized)
        {
            if (enableDebug)
                Debug.Log($"[ToolLoot] ❌ Initialization not complete, skipping pickup sound");
            return;
        }

        if (audioSource == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolLoot] ⚠ AudioSource is null, cannot play sound");
            return;
        }

        // Play tool pickup sound
        if (toolPickupSound != null)
        {
            audioSource.PlayOneShot(toolPickupSound);
            if (enableDebug)
                Debug.Log($"[ToolLoot] ✓ Playing tool pickup sound - AudioClip: {toolPickupSound.name}");
        }
        else if (enableDebug)
        {
            Debug.LogWarning($"[ToolLoot] ⚠ toolPickupSound is null");
        }
    }
}
