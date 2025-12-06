using System.Collections;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public ToolSlot[] toolSlots;
    public GameObject toolLootPrefab;
    public Transform player;
    
    [Header("选择系统")]
    public ChooseBar chooseBar;

    [Header("调试")]
    public bool enableDebug = false;
    
    // 存储每个ToolSlot的位置信息
    private Vector3[] toolSlotPositions;

    private void Start()
    {
        // 确保所有工具槽都激活
        foreach(var slot in toolSlots)
        {
            if (slot != null && !slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(true);
                if (enableDebug)
                    Debug.Log($"[GameToolManager] 激活工具槽: {slot.name}");
            }
            slot.UpdateUI();
        }
        
        // 延迟记录位置，等待布局完成初始化
        StartCoroutine(DelayedRecordPositions());
        
        // 如果选择条未初始化
        if (chooseBar == null)
        {
            chooseBar = FindObjectOfType<ChooseBar>();
        }
    }
    
    private IEnumerator DelayedRecordPositions()
    {
        // 等待一帧，让布局组件完成计算
        yield return new WaitForEndOfFrame();
        
        // 记录所有ToolSlot的位置
        RecordToolSlotPositions();
    }

    private void OnEnable()
    {
        ToolLoot.OnToolLooted += AddTool;
    }

    private void OnDisable()
    {
        ToolLoot.OnToolLooted -= AddTool;
    }
    
    /// <summary>
    /// 记录所有ToolSlot的位置
    /// </summary>
    private void RecordToolSlotPositions()
    {
        if (toolSlots == null || toolSlots.Length == 0)
        {
            if (enableDebug)
                Debug.LogWarning("[GameToolManager] toolSlots 为空，无法记录位置");
            return;
        }
        
        toolSlotPositions = new Vector3[toolSlots.Length];
        
        for (int i = 0; i < toolSlots.Length; i++)
        {
            if (toolSlots[i] != null)
            {
                RectTransform rectTransform = toolSlots[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    toolSlotPositions[i] = rectTransform.localPosition;
                    
                    if (enableDebug)
                        Debug.Log($"[GameToolManager] 记录槽位 {i} ({toolSlots[i].name}) 位置: {toolSlotPositions[i]}");
                }
                else
                {
                    Debug.LogWarning($"[GameToolManager] 槽位 {i} ({toolSlots[i].name}) 没有 RectTransform 组件");
                }
            }
        }
        
        if (enableDebug)
            Debug.Log($"[GameToolManager] 位置记录完成，共记录 {toolSlotPositions.Length} 个槽位");
    }

    public void AddTool(ToolSO toolSO, int quantity)
    {
        if (toolSO == null)
        {
            if (enableDebug)
                Debug.LogWarning("[GameToolManager] 尝试添加空工具");
            return;
        }

        if (enableDebug)
            Debug.Log($"[GameToolManager] 添加工具: {toolSO.toolName} x{quantity}");

        // 首先尝试堆叠到已有的工具槽
        foreach(var slot in toolSlots)
        {
            if (slot.toolSO == toolSO && slot.quantity < toolSO.maxStackSize)
            {
                int availableSpace = toolSO.maxStackSize - slot.quantity;
                int amountToAdd = Mathf.Min(availableSpace, quantity);
                slot.quantity += amountToAdd;
                quantity -= amountToAdd;
                slot.UpdateUI();
                if (quantity <= 0)
                    return;
            }
        }
        
        // 如果还有剩余，尝试找空槽位
        foreach(var slot in toolSlots)
        {
            if(slot.toolSO == null)
            {
                int amountToAdd = Mathf.Min(toolSO.maxStackSize, quantity);
                slot.toolSO = toolSO;
                slot.quantity = quantity;
                slot.UpdateUI();
                return;
            }
        }
        
        // 如果工具栏已满，掉落剩余工具
        if (quantity > 0)
        {
            if (enableDebug)
                Debug.Log($"[GameToolManager] 工具栏已满，掉落工具 x{quantity}");
            DropToolLoot(toolSO, quantity);
        }
    }
    
    public void DropTool(ToolSlot slot)
    {
        DropToolLoot(slot.toolSO, 1);
        slot.quantity--;
        if (slot.quantity <= 0)
        {
            slot.toolSO = null;
        }
        slot.UpdateUI();
    }
    
    private void DropToolLoot(ToolSO toolSO, int quantity)
    {
        ToolLoot toolLoot = Instantiate(toolLootPrefab, player.position, Quaternion.identity).GetComponent<ToolLoot>();
        toolLoot.Initialize(toolSO, quantity);
    }
    
    /// <summary>
    /// 获取指定索引的工具槽位置
    /// </summary>
    public Vector3 GetToolSlotPosition(int index)
    {
        if (toolSlotPositions == null || index < 0 || index >= toolSlotPositions.Length)
        {
            if (enableDebug)
                Debug.LogWarning($"[GameToolManager] 无法获取槽位 {index} 的位置，toolSlotPositions: {(toolSlotPositions == null ? "null" : toolSlotPositions.Length.ToString())}");
            return Vector3.zero;
        }
        
        if (enableDebug)
            Debug.Log($"[GameToolManager] 返回槽位 {index} 的位置: {toolSlotPositions[index]}");
        
        return toolSlotPositions[index];
    }
    
    /// <summary>
    /// 获取指定索引的工具槽
    /// </summary>
    public ToolSlot GetToolSlot(int index)
    {
        if (index < 0 || index >= toolSlots.Length)
            return null;
        return toolSlots[index];
    }
    
    /// <summary>
    /// 获取工具槽的总数
    /// </summary>
    public int GetToolSlotCount()
    {
        return toolSlots != null ? toolSlots.Length : 0;
    }
    
    /// <summary>
    /// 刷新所有槽位位置（当UI改变时调用）
    /// </summary>
    public void RefreshToolSlotPositions()
    {
        RecordToolSlotPositions();
    }
}
