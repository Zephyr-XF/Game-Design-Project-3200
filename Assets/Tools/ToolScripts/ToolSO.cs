using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "Tool/Tool Item")]
public class ToolSO : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("工具名称")]
    public string toolName;

    [Tooltip("工具描述")]
    [TextArea(3, 5)]
    public string toolDescription;

    [Tooltip("工具图标")]
    public Sprite icon;

    [Header("堆叠设置")]
    [Tooltip("最大堆叠数量")]
    public int maxStackSize = 1;


    [Tooltip("最大耐久度")]
    public int maxDurability = 100;

    [Tooltip("当前耐久度（运行时使用）")]
    [HideInInspector]
    public int currentDurability;

    [Header("使用设置")]
    [Tooltip("使用冷却时间（秒）")]
    public float cooldownTime = 0f;

    [Tooltip("是否可以在战斗中使用")]
    public bool canUseInCombat = true;

    [Header("价值")]
    [Tooltip("购买价格")]
    public int buyPrice = 100;

    [Tooltip("出售价格")]
    public int sellPrice = 50;

}