# 双属性祝福系统实现计划 (Dual Attribute System Plan)

本计划旨在扩展现有的祝福系统，使其支持单一祝福同时修改多个属性（例如：同时增加攻击力和速度），并为未来更多属性的加入预留接口。

## 1. 数据结构改造 (`BlessingData.cs`)

目前 `BlessingData` 只能存储单一属性 (`statType`, `amount`)。我们需要将其改为支持列表或数组。

### 变更方案：
引入一个结构体 `StatModifier` 来封装属性修改。

```csharp
[System.Serializable]
public struct StatModifier 
{
    public StatType statType; // 属性类型 (如 Damage, Speed, 甚至将来的 Critical, Evasion)
    public int amount;        // 数值
}

public class BlessingData : ScriptableObject
{
    // ... 原有的 name, image, description ...

    // [DELETE] 删除旧的单属性字段
    // public StatType statType;
    // public int amount;

    // [NEW] 新增修改器列表
    [Header("Attributes")]
    public List<StatModifier> modifiers; 

    public int sanityCost; // 保持不变
}
```

---

## 2. 管理器逻辑升级 (`BlessingManager.cs`)

`BlessingManager` 负责应用这些属性。由于数据结构变了，应用逻辑必须从“只处理一个”改为“遍历处理列表”。

### 变更点 A: 属性累积记录
目前使用 `int bonusDamage`, `int bonusSpeed` 单独变量记录。
**建议**：为了应对未来更多属性（如暴击、闪避），建议改为使用 `Dictionary<StatType, int>` 来存储所有累积加成。

```csharp
// [NEW] 使用字典替代零散变量
private Dictionary<StatType, int> accumulatedBonuses = new Dictionary<StatType, int>();
```

### 变更点 B: 应用逻辑 (`ChooseBlessing` & `ApplyStatPersistent`)

```csharp
// 伪代码示例
public void ChooseBlessing(BlessingData choice)
{
    // 1. 遍历所有修改器进行持久化记录
    foreach(var mod in choice.modifiers)
    {
        // 记录到 accumulatedBonuses 字典中
        ApplyStatPersistent(mod);
    }

    // 2. 立即应用到当前的 StatsManager (如果有)
    if(StatsManager.Instance != null)
    {
        foreach(var mod in choice.modifiers)
        {
            ApplyToStatsManager(mod);
        }
        
        // 单独处理 Sanity Cost (因为它不是 StatType)
        if(choice.sanityCost > 0) StatsManager.Instance.UpdateSanity(-choice.sanityCost);
    }
    
    // ... 隐藏 UI 和恢复时间 ...
}
```

### 变更点 C: 场景加载恢复 (`OnSceneLoaded`)

也就是重写 `OnSceneLoaded`，遍历 `accumulatedBonuses` 字典，把所有存下来的加成一次性丢给新的 `StatsManager`。

---

## 3. 属性枚举扩展 (`BlessingData.cs` -> `StatType`)

等待队友将新属性（如暴击率、闪避率等）合并进 `StatsManager` 后，只需在 `StatType` 枚举中添加对应名字：

```csharp
public enum StatType
{
    Damage,
    Speed,
    MaxHealth,
    // [Pending] 等队友合并后添加:
    // CriticalChance,
    // EvasionRate,
    // AttackRange,
    // ...
}
```

然后在 `BlessingManager` 的 `ApplyToStatsManager` 方法里的 `switch` 语句中增加对应的处理分支即可。

## 下一步行动建议

1.  **备份**：由于涉及数据结构变更 (Delete `statType`), 现有的 ScriptableObject 数据可能会丢失配置。建议先截图记录现有数据的数值，或者写一个简单的迁移脚本。
2.  **执行修改**：按照上述计划修改脚本。
3.  **重新配置**：回到 Unity Inspector，重新把所有祝福卡的属性填入新的 List 中。
