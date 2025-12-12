using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections; // 引用协程所需的命名空间

public class PlayerHealth : MonoBehaviour
{
    // 死亡事件 - 其他脚本可以订阅这个事件
    public static event Action OnPlayerDied;

    [Header("Component References")]
    public Animator animator; // 拖拽玩家身上的Animator组件到这里
    public Renderer characterRenderer; // 新增：拖拽玩家模型或精灵的Renderer组件

    [Header("UI References")]
    public Image healthBarFill;

    [Header("Damage Effect Settings")] // 新增：受击效果的设置
    public Color damageColor = Color.red; // 受击时闪烁的颜色
    public float flashDuration = 0.2f;    // 颜色闪烁的持续时间

    [Header("Debugging")]
    public bool enableDebug = true;

    private bool isDead = false;
    private Color originalColor; // 新增：用于存储原始颜色
    private Coroutine flashCoroutine; // 新增：用于管理颜色闪烁的协程

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 新增：自动获取Renderer并存储原始颜色
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInChildren<Renderer>(); // 尝试从子对象获取，适用于3D模型
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<Renderer>(); // 适用于精灵或简单对象
            }
        }
        if (characterRenderer != null)
        {
            originalColor = characterRenderer.material.color;
        }
    }

    private void OnEnable()
    {
        isDead = false;
        UpdateHealthUI();
        // 重置颜色，以防在禁用时还处于闪烁状态
        if (characterRenderer != null)
        {
            characterRenderer.material.color = originalColor;
        }
        
        if (enableDebug) Debug.Log("[PlayerHealth] OnEnable - 玩家已激活，死亡状态已重置");
    }

    public void ChangeHealth(int amount)
    {
        if (isDead) return;

        if (enableDebug) Debug.Log($"PlayerHealth.ChangeHealth({amount}) 被调用。当前血量: {StatsManager.Instance.currentHealth}");

        // 1. 先更新血量数据
        StatsManager.Instance.UpdateHealth(amount);
        UpdateHealthUI();

        // 2. 检查是否死亡
        if (StatsManager.Instance.currentHealth <= 0)
        {
            // 如果血量归零，直接死亡，不要播放受伤动画
            Die();
        }
        else
        {
            // 3. 如果没死，并且是受到伤害（amount为负数），才播放受伤效果
            if (amount < 0)
            {
                TakeDamageEffect();
            }
        }

        if (enableDebug) Debug.Log($"血量变化后: {StatsManager.Instance.currentHealth}");
    }

    // 新增：一个集中的受击效果方法
    private void TakeDamageEffect()
    {
        // 1. 触发受击动画
        if (animator != null)
        {
            animator.SetTrigger("Hurt"); // 我们将创建一个名为 "Hurt" 的Trigger
        }

        // 2. 触发颜色闪烁效果
        if (characterRenderer != null)
        {
            // 如果上一个闪烁协程还在运行，先停止它
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            // 开始新的闪烁协程
            flashCoroutine = StartCoroutine(FlashRedEffect());
        }
    }

    // 新增：用于处理颜色闪烁的协程
    private IEnumerator FlashRedEffect()
    {
        // 变成红色
        characterRenderer.material.color = damageColor;

        // 等待指定的持续时间
        yield return new WaitForSeconds(flashDuration);

        // 恢复原始颜色
        characterRenderer.material.color = originalColor;

        // 协程结束，重置引用
        flashCoroutine = null;
    }

    public void SetMaxHealth()
    {
        StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
        UpdateHealthUI();
        if (enableDebug) Debug.Log($"[PlayerHealth] 血量已重置为最大值: {StatsManager.Instance.maxHealth}");
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
        if (isDead) return;
        isDead = true;

        if (enableDebug) Debug.Log("[PlayerHealth] Die() - 玩家死亡，播放死亡动画...");

        if (animator != null)
        {
            animator.SetTrigger("Die");
            if (enableDebug) Debug.Log("[PlayerHealth] 死亡动画触发器已设置");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Animator未找到！立即触发死亡事件并禁用对象。");
            TriggerDeathEvent();
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 由动画事件调用：当死亡动画播放完毕时
    /// </summary>
    public void OnDeathAnimationFinished()
    {
        if (enableDebug) Debug.Log("[PlayerHealth] OnDeathAnimationFinished() - 死亡动画播放完毕");
        
        // 真正的死亡处理：触发死亡事件
        TriggerDeathEvent();
        
        // 禁用玩家对象
        gameObject.SetActive(false);
        if (enableDebug) Debug.Log("[PlayerHealth] 玩家对象已禁用");
    }

    /// <summary>
    /// 触发死亡事件，通知所有订阅者
    /// </summary>
    private void TriggerDeathEvent()
    {
        if (enableDebug)
        {
            // 检查是否有订阅者
            int subscriberCount = OnPlayerDied?.GetInvocationList()?.Length ?? 0;
            Debug.Log($"[PlayerHealth] 准备触发死亡事件 OnPlayerDied，当前订阅者数量: {subscriberCount}");
        }

        // 触发事件
        OnPlayerDied?.Invoke();

        if (enableDebug) Debug.Log("[PlayerHealth] 死亡事件已触发！");
    }
}


