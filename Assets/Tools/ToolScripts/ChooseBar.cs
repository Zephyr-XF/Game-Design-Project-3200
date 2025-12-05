using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseBar : MonoBehaviour
{
    [Header("引用")]
    public ToolManager toolManager;
    public RectTransform chooseBarIndicator;
    
    [Header("按键设置")]
    public KeyCode switchKey = KeyCode.Q; // 切换工具键
    
    [Header("移动设置")]
    public float moveSpeed = 10f;
    public bool smoothMove = true;
    
    [Header("音效设置")]
    [Tooltip("音频源组件（可选，留空则自动添加）")]
    public AudioSource audioSource;
    
    [Tooltip("切换槽位的音效")]
    public AudioClip switchSound;
    
    [Tooltip("音效音量（0-1）")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private int currentSelectedIndex = 0;
    private Vector3 targetPosition;
    private float inputCooldown = 0.2f;
    private float lastInputTime = 0f;

    void Start()
    {
        if (toolManager == null)
        {
            toolManager = FindObjectOfType<ToolManager>();
            if (toolManager == null)
            {
                Debug.LogError("[ChooseBar] 错误：未找到 ToolManager！");
                enabled = false;
                return;
            }
        }
        
        if (chooseBarIndicator == null)
        {
            Debug.LogWarning("[ChooseBar] 未设置选择指示器！");
        }
        
        // 获取或添加 AudioSource 组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                if (enableDebug)
                    Debug.Log("[ChooseBar] 自动添加了 AudioSource 组件");
            }
        }
        
        // 配置 AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = soundVolume;
        
        // 延迟初始化位置，等待 ToolManager 完成位置记录
        StartCoroutine(DelayedInitialize());
    }
    
    /// <summary>
    /// 延迟初始化，等待布局完成
    /// </summary>
    private IEnumerator DelayedInitialize()
    {
        // 等待两帧，确保 ToolManager 的 WaitForEndOfFrame 已完成
        yield return new WaitForEndOfFrame();
        yield return null;
        
        // 现在初始化位置
        currentSelectedIndex = 0; // 确保从槽位 0 开始
        UpdateChooseBarPosition(true);
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] 延迟初始化完成，当前槽位: {currentSelectedIndex}");
    }

    void Update()
    {
        HandleInput();
        
        // 平滑移动指示器
        if (smoothMove && chooseBarIndicator != null)
        {
            chooseBarIndicator.localPosition = Vector3.Lerp(
                chooseBarIndicator.localPosition, 
                targetPosition, 
                Time.deltaTime * moveSpeed
            );
        }
    }

    private void HandleInput()
    {
        // Q键 - 切换工具槽（带冷却）
        if (Time.time - lastInputTime >= inputCooldown && Input.GetKeyDown(switchKey))
        {
            SwitchToNextSlot();
            lastInputTime = Time.time;
        }
        
        // 注意：F键投掷功能已移至 ToolThrow 组件
    }

    private void SwitchToNextSlot()
    {
        int slotCount = toolManager.GetToolSlotCount();
        if (slotCount == 0)
            return;
        
        currentSelectedIndex--;
        if (currentSelectedIndex < 0)
        {
            currentSelectedIndex = slotCount - 1;
        }
        
        UpdateChooseBarPosition(false);
        
        // 播放切换音效
        PlaySwitchSound();
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] Q键切换到槽位: {currentSelectedIndex}");
    }
    
    /// <summary>
    /// 播放切换音效（只播放一小段）
    /// </summary>
    private void PlaySwitchSound()
    {
        if (switchSound == null || audioSource == null)
            return;
        
        // 播放音效
        audioSource.PlayOneShot(switchSound, soundVolume);
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] 播放切换音效");
    }

    private void UpdateChooseBarPosition(bool immediate)
    {
        if (toolManager == null || chooseBarIndicator == null)
            return;
        
        int slotCount = toolManager.GetToolSlotCount();
        if (slotCount == 0)
            return;
        
        if (currentSelectedIndex < 0 || currentSelectedIndex >= slotCount)
            currentSelectedIndex = 0;
        
        // 从ToolManager获取槽位位置
        targetPosition = toolManager.GetToolSlotPosition(currentSelectedIndex);
        
        if (immediate || !smoothMove)
        {
            chooseBarIndicator.localPosition = targetPosition;
        }
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] 更新指示器位置到槽位 {currentSelectedIndex}: {targetPosition}");
    }

    /// <summary>
    /// 获取当前选中的工具槽
    /// </summary>
    public ToolSlot GetCurrentSelectedSlot()
    {
        if (toolManager == null)
            return null;
        
        return toolManager.GetToolSlot(currentSelectedIndex);
    }

    /// <summary>
    /// 获取当前选中的槽位索引
    /// </summary>
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    /// <summary>
    /// 手动设置选中的槽位
    /// </summary>
    public void SetSelectedIndex(int index)
    {
        if (toolManager == null)
            return;
        
        int slotCount = toolManager.GetToolSlotCount();
        if (index < 0 || index >= slotCount)
            return;
        
        currentSelectedIndex = index;
        UpdateChooseBarPosition(false);
        
        // 播放切换音效
        PlaySwitchSound();
    }
}
