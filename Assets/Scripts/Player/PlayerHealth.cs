using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Component References")]
    public Animator animator; // 拖拽玩家身上的Animator组件到这里

    [Header("UI References")]
    public Image healthBarFill;

    [Header("Debugging")]
    public bool enableDebug = true;

    private bool isDead = false; // 添加一个死亡状态，防止多次调用Die()

    private void Awake()
    {
        // 如果没有在Inspector中拖拽，可以尝试自动获取
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        isDead = false; // 当对象被重新激活时，重置死亡状态
        UpdateHealthUI();
    }

    public void ChangeHealth(int amount)
    {
        if (isDead) return; // 如果已经死了，就不要再执行任何逻辑了

        if (enableDebug) Debug.Log($"PlayerHealth.ChangeHealth({amount}) 被调用。当前血量: {StatsManager.Instance.currentHealth}");

        StatsManager.Instance.UpdateHealth(amount);
        UpdateHealthUI();

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

    private void UpdateHealthUI()
    {
        if (healthBarFill != null && StatsManager.Instance.maxHealth > 0)
        {
            healthBarFill.fillAmount = (float)StatsManager.Instance.currentHealth / StatsManager.Instance.maxHealth;
        }
    }

    private void Die()
    {
        if (isDead) return; // 再次检查，确保万无一失
        isDead = true;

        Debug.Log("玩家死亡。触发死亡动画...");

        // 触发我们之前在Animator中设置的名为"Die"的Trigger
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        else
        {
            // 如果没有动画，作为后备方案，立即禁用对象
            Debug.LogWarning("Animator未找到！立即禁用对象。");
            gameObject.SetActive(false);
        }

        // 移除了 gameObject.SetActive(false);
        // 它将在动画结束时通过Animation Event调用
    }

    // 这个函数将由动画事件调用
    // 必须是 public
    public void OnDeathAnimationFinished()
    {
        Debug.Log("死亡动画播放完毕。禁用玩家对象。");
        // 这里可以执行游戏结束、显示菜单等逻辑
        gameObject.SetActive(false); // 动画放完了，现在可以安全地禁用它了
    }
}


