using UnityEngine;

[CreateAssetMenu(fileName = "NewBlessing", menuName = "Blessing/Blessing Data")]
public class BlessingData : ScriptableObject
{
    public string godName;
    public Sprite godImage;
    [TextArea] public string description;
    [TextArea] public string quote; // 鼠标悬停时神说的台词
    
    public StatType statType;
    public int amount;
    [Tooltip("Sanity cost when choosing this blessing")]
    public int sanityCost; // New attribute: Sanity Cost
}

public enum StatType
{
    Damage,
    Speed,
    MaxHealth
}
