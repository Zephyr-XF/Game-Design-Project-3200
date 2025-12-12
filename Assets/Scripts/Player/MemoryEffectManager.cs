using System.Collections;
using UnityEngine;

public class MemoryEffectManager : MonoBehaviour
{
    public static MemoryEffectManager Instance;

    [Header("Settings")]
    public float debuffDuration = 5f; // 回忆后的虚弱时间
    public float buffDuration = 10f;  // 摧毁后的强化时间
    public float damageReductionMultiplier = 0.7f; // 减少30%攻击力
    public float damageIncreaseMultiplier = 1.5f;  // 增加50%攻击力
    public int sanityRestoreAmount = 20;
    public int sanityCostAmount = 30;

    private bool isCorrupted = false; // 防止重复触发

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 触发回忆效果 (仅数值和状态变化，不含剧情播放)
    /// </summary>
    public void ApplyReminisceEffects(int sanityReward)
    {
        // 1. 奖励 Sanity
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.UpdateSanity(sanityReward > 0 ? sanityReward : sanityRestoreAmount);
        }

        // 2. 应用 Debuff (虚弱)
        StartCoroutine(ApplyDamageModifier(damageReductionMultiplier, debuffDuration));
        
        Debug.Log("Memory Reminisced: Sanity Restored, Damage Reduced temporarily.");
    }

    /// <summary>
    /// 触发摧毁 (Shatter)
    /// </summary>
    public void TriggerShatterEffect()
    {
        // 1. 扣除 Sanity
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.UpdateSanity(-sanityCostAmount);
        }

        // 2. 应用 Buff (增伤)
        StartCoroutine(ApplyDamageModifier(damageIncreaseMultiplier, buffDuration));
        
        Debug.Log("Memory Shattered: Sanity Lost, Damage Increased temporarily.");
    }

    /// <summary>
    /// 通用修饰器协程：修改 -> 等待 -> 恢复
    /// </summary>
    private IEnumerator ApplyDamageModifier(float multiplier, float duration)
    {
        if (StatsManager.Instance == null) yield break;

        // 记录原始值 (注意：这假设在持续期间没有其他东西修改Damage，简单实现)
        // 更稳健的做法是记录增加/减少了多少点数值
        int originalDamage = StatsManager.Instance.damage;
        int modifiedDamage = Mathf.RoundToInt(originalDamage * multiplier);

        // 应用修改
        StatsManager.Instance.damage = modifiedDamage;
        if (StatsManager.Instance.statsUI != null) StatsManager.Instance.statsUI.UpdateDamage(); // 刷新UI

        yield return new WaitForSeconds(duration);

        // 恢复 (直接设回记录的原始值，比做除法更精准)
        StatsManager.Instance.damage = originalDamage;
        if (StatsManager.Instance.statsUI != null) StatsManager.Instance.statsUI.UpdateDamage(); // 刷新UI
    }
}
