using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    // 移除了 currentHealth 和 maxHealth，因为 StatsManager 是唯一的数据源

    [Header("UI References")]
    public Image healthBarFill;

    [Header("Debugging")]
    public bool enableDebug = true;

    // Start is fine, but OnEnable is often better for UI that might be disabled/re-enabled
    private void OnEnable()
    {
        // 确保一开始UI是正确的
        UpdateHealthUI();
    }

    public void ChangeHealth(int amount)
    {
        if (enableDebug) Debug.Log($"PlayerHealth.ChangeHealth({amount}) 被调用。当前血量: {StatsManager.Instance.currentHealth}");

        // 使用 StatsManager 来处理血量变化
        StatsManager.Instance.UpdateHealth(amount);

        // 更新UI显示
        UpdateHealthUI();

        // 检查死亡（在更新完UI之后）
        if (StatsManager.Instance.currentHealth <= 0)
        {
            Die();
        }

        if (enableDebug) Debug.Log($"血量变化后: {StatsManager.Instance.currentHealth}");
    }

    public void SetMaxHealth()
    {

        StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
        UpdateHealthUI();
    }

    // 每次血量变化时，都从 StatsManager 获取最新数据来更新UI
    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            // 确保maxHealth不为0，避免除零错误
            if (StatsManager.Instance.maxHealth > 0)
            {
                healthBarFill.fillAmount = (float)StatsManager.Instance.currentHealth / StatsManager.Instance.maxHealth;
            }
        }
    }

    private void Die()
    {
        Debug.Log("玩家死亡。");
        // 这里可以添加死亡逻辑，比如显示游戏结束画面、禁用玩家控制等
        gameObject.SetActive(false); // 简单地禁用玩家对象
    }
}


