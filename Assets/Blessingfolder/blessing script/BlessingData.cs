using UnityEngine;

[CreateAssetMenu(fileName = "NewBlessing", menuName = "Blessing/Blessing Data")]
public class BlessingData : ScriptableObject
{
    public string godName;
    public Sprite godImage;
    [TextArea] public string description;
    
    public StatType statType;
    public int amount;
}

public enum StatType
{
    Damage,
    Speed,
    MaxHealth
}
