using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("伤害设置")]
    public int spikeDamage = 10;            // 每次刺出造成的伤害

    [Header("减速设置")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.7f;     // 踩到地刺时的速度倍率（0.7 = 降为原来70%）
    public bool enableSlow = true;          // 是否启用减速效果

    [Header("定时触发设置")]
    public float idleTime = 1.5f;           // 收起状态持续时长
    public float activeTime = 1.0f;         // 刺出状态持续时长
    public bool startActive = false;        // 是否一开始就是刺出状态

    [Header("目标层级")]
    public LayerMask playerLayer;           // Player 所在的 Layer
    public LayerMask enemyLayer;            // Enemy 所在的 Layer

    [Header("组件引用")]
    public Animator anim;                   // 控制地刺动画
    public Collider2D damageCollider;       // 负责伤害检测的碰撞体(建议 IsTrigger = true)

    [Header("调试")]
    public bool enableDebug = true;

    private bool isActive = false;          // 当前是否处于"刺出"状态
    private bool canDamage = false;         // 当前这轮刺出是否还可以对玩家/敌人造成伤害（防止一帧多次）

    // 记录被减速的对象及其原始速度
    private Dictionary<GameObject, float> slowedPlayers = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> slowedEnemies = new Dictionary<GameObject, float>();

    private void Start()
    {
        // 自动尝试获取引用
        if (anim == null)
            anim = GetComponent<Animator>();

        if (damageCollider == null)
            damageCollider = GetComponent<Collider2D>();

        ValidateComponents();

        // 根据 startActive 决定初始状态
        isActive = startActive;
        UpdateVisualState();

        // 开启定时循环
        StartCoroutine(SpikeRoutine());
    }

    void ValidateComponents()
    {
        if (!enableDebug) return;

        if (damageCollider == null)
            Debug.LogWarning("Spikes: 没找到伤害用的 Collider2D，请在 Inspector 中分配。");

        if (anim == null)
            Debug.LogWarning("Spikes: 没找到 Animator 组件，地刺将不会播放动画。");

        if (playerLayer == 0)
            Debug.LogWarning("Spikes: Player Layer 未设置，无法可靠检测玩家。");

        if (enemyLayer == 0)
            Debug.LogWarning("Spikes: Enemy Layer 未设置，无法可靠检测敌人。");
    }

    /// <summary>
    /// 控制地刺的循环状态：收起 -> 弹出 -> 收起 -> …
    /// </summary>
    IEnumerator SpikeRoutine()
    {
        while (true)
        {
            if (!isActive)
            {
                // 收起阶段
                if (enableDebug) Debug.Log("Spikes: 进入收起状态");
                canDamage = false;                // 收起时不造成伤害
                if (damageCollider != null)
                    damageCollider.enabled = false;

                yield return new WaitForSeconds(idleTime);

                // 切到刺出
                SetActive(true);
            }
            else
            {
                // 刺出阶段
                if (enableDebug) Debug.Log("Spikes: 进入刺出状态");
                canDamage = true;                 // 本轮刺出可以伤害
                if (damageCollider != null)
                    damageCollider.enabled = true;

                yield return new WaitForSeconds(activeTime);

                // 切到收起
                SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置刺的"刺出 / 收起"状态
    /// </summary>
    void SetActive(bool active)
    {
        isActive = active;
        UpdateVisualState();
    }

    /// <summary>
    /// 更新动画（纯表现）
    /// </summary>
    void UpdateVisualState()
    {
        if (anim != null)
        {
            // 这里的 "IsActive" 要跟你 Animator 里定义的参数名一致
            anim.SetBool("IsActive", isActive);
        }
    }

    /// <summary>
    /// 玩家/敌人进入地刺区域时触发（减速立即生效）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // 检查是否是玩家
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            // 应用减速效果（无论地刺是否刺出）
            if (enableSlow)
            {
                ApplySlowToPlayer(other.gameObject);
            }

            // 只在刺出状态造成伤害
            if (isActive && canDamage)
            {
                if (enableDebug)
                    Debug.Log($"Spikes: 与玩家 {other.gameObject.name} 发生触发碰撞");

                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.ChangeHealth(-spikeDamage);
                    if (enableDebug)
                        Debug.Log($"Spikes: 对玩家造成 {spikeDamage} 点伤害。");
                }
                else if (enableDebug)
                {
                    Debug.LogError($"Spikes: 玩家 {other.gameObject.name} 身上没有 PlayerHealth 组件！");
                }
            }
            return;
        }

        // 检查是否是敌人
        if (IsInLayerMask(otherLayer, enemyLayer))
        {
            // 应用减速效果（无论地刺是否刺出）
            if (enableSlow)
            {
                ApplySlowToEnemy(other.gameObject);
            }

            // 只在刺出状态造成伤害
            if (isActive && canDamage)
            {
                if (enableDebug)
                    Debug.Log($"Spikes: 与敌人 {other.gameObject.name} 发生触发碰撞");

                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.ChangeHealth(-spikeDamage);
                    if (enableDebug)
                        Debug.Log($"Spikes: 对敌人造成 {spikeDamage} 点伤害。");
                }
                else if (enableDebug)
                {
                    Debug.LogError($"Spikes: 敌人 {other.gameObject.name} 身上没有 EnemyHealth 组件！");
                }
            }
        }
    }

    /// <summary>
    /// 玩家/敌人离开地刺区域时触发（恢复速度）
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // 玩家离开
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            RestorePlayerSpeed(other.gameObject);
        }

        // 敌人离开
        if (IsInLayerMask(otherLayer, enemyLayer))
        {
            RestoreEnemySpeed(other.gameObject);
        }
    }

    /// <summary>
    /// 对玩家应用减速效果
    /// </summary>
    void ApplySlowToPlayer(GameObject obj)
    {
        var playerMovement = obj.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"Spikes: {obj.name} 没有找到 PlayerMovement 组件，无法减速。");
            return;
        }

        if (StatsManager.Instance == null)
        {
            if (enableDebug)
                Debug.LogError("Spikes: StatsManager.Instance 为空，无法修改玩家速度！");
            return;
        }

        // 如果已经在减速中，不重复应用
        if (slowedPlayers.ContainsKey(obj))
            return;

        // 记录原始速度并应用减速
        int originalSpeed = StatsManager.Instance.speed;
        slowedPlayers.Add(obj, originalSpeed);

        int slowedSpeed = Mathf.RoundToInt(originalSpeed * slowMultiplier);
        StatsManager.Instance.speed = slowedSpeed;

        if (enableDebug)
            Debug.Log($"Spikes: 玩家 {obj.name} 进入地刺，速度 {originalSpeed} -> {slowedSpeed}");
    }

    /// <summary>
    /// 对敌人应用减速效果
    /// </summary>
    void ApplySlowToEnemy(GameObject obj)
    {
        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"Spikes: {obj.name} 没有找到 EnemyMovement 组件，无法减速。");
            return;
        }

        // 如果已经在减速中，不重复应用
        if (slowedEnemies.ContainsKey(obj))
            return;

        // 记录原始速度并应用减速
        float originalSpeed = enemyMovement.speed;
        slowedEnemies.Add(obj, originalSpeed);

        float slowedSpeed = originalSpeed * slowMultiplier;
        enemyMovement.speed = slowedSpeed;

        if (enableDebug)
            Debug.Log($"Spikes: 敌人 {obj.name} 进入地刺，速度 {originalSpeed} -> {slowedSpeed}");
    }

    /// <summary>
    /// 恢复玩家速度
    /// </summary>
    void RestorePlayerSpeed(GameObject obj)
    {
        if (!slowedPlayers.TryGetValue(obj, out float originalSpeed))
            return;

        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.speed = Mathf.RoundToInt(originalSpeed);
            
            if (enableDebug)
                Debug.Log($"Spikes: 玩家 {obj.name} 离开地刺，速度恢复为 {originalSpeed}");
        }

        slowedPlayers.Remove(obj);
    }

    /// <summary>
    /// 恢复敌人速度
    /// </summary>
    void RestoreEnemySpeed(GameObject obj)
    {
        if (!slowedEnemies.TryGetValue(obj, out float originalSpeed))
            return;

        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.speed = originalSpeed;
            
            if (enableDebug)
                Debug.Log($"Spikes: 敌人 {obj.name} 离开地刺，速度恢复为 {originalSpeed}");
        }

        slowedEnemies.Remove(obj);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}