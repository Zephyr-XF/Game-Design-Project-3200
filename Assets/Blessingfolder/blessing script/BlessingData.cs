using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBlessing", menuName = "Blessing/Blessing Data")]
public class BlessingData : ScriptableObject
{
    public string godName;
    public Sprite godImage;
    [TextArea] public string description;
    [TextArea] public string quote; // 鼠标悬停时神说的台词
    
    [Header("Attributes")]
    public List<StatModifier> modifiers; // Changed to list for multiple attributes

    [Tooltip("Sanity cost when choosing this blessing")]
    public int sanityCost; // New attribute: Sanity Cost
}

[System.Serializable]
public struct StatModifier
{
    public StatType statType;
    public int amount;
}

public enum StatType
{
    Damage,
    Speed,
    MaxHealth,
    Impact,            // New: Impact damage
    Skill2Cooldown,    // New: Reduce cooldown
    Skill2DamageMult,  // New: Skill 2 multiplier
    MinAttackInterval, // New: Attack speed limit
    TriggerCinematic   // New: Enable cinematic finisher
}
