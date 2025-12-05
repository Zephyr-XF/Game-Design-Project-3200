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
    public KeyCode throwKey = KeyCode.F;  // 投掷工具键
    
    [Header("移动设置")]
    public float moveSpeed = 10f;
    public bool smoothMove = true;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private int currentSelectedIndex = 0;
    private Vector3 targetPosition;
    private float inputCooldown = 0.2f;
    private float lastInputTime = 0f;
    private float throwCooldown = 0.3f;
    private float lastThrowTime = 0f;

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
        
        // F键 - 投掷当前工具（带冷却）
        if (Time.time - lastThrowTime >= throwCooldown && Input.GetKeyDown(throwKey))
        {
            ThrowCurrentTool();
            lastThrowTime = Time.time;
        }
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
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] Q键切换到槽位: {currentSelectedIndex}");
    }
    
    /// <summary>
    /// 投掷当前选中的工具
    /// </summary>
    private void ThrowCurrentTool()
    {
        if (toolManager == null)
        {
            if (enableDebug)
                Debug.LogWarning("[ChooseBar] ToolManager 未设置，无法投掷");
            return;
        }
        
        ToolSlot currentSlot = GetCurrentSelectedSlot();
        if (currentSlot == null)
        {
            if (enableDebug)
                Debug.LogWarning("[ChooseBar] 当前槽位为空");
            return;
        }
        
        if (currentSlot.toolSO == null || currentSlot.quantity <= 0)
        {
            if (enableDebug)
                Debug.LogWarning("[ChooseBar] 当前槽位没有工具或数量为0");
            return;
        }
        
        if (enableDebug)
            Debug.Log($"[ChooseBar] F键投掷工具: {currentSlot.toolSO.toolName} (槽位 {currentSelectedIndex})");
        
        // 调用 ToolManager 的 UseTool 方法
        toolManager.UseTool(currentSlot);
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
    }
}
