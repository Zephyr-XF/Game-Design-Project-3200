using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNT : MonoBehaviour
{
    [Header("爆炸设置")]
    public int explosionDamage = 50; // 建议伤害高一点
    public float explosionPoiseDamage = 40f; // ✨ 新增：爆炸的削韧值(冲击力)
    public float explosionForce = 10f; // ✨ 新增：物理击退力度

    [Header("爆炸范围物体")]
    public GameObject explosionRangeObject; // 包含CircleCollider2D的外挂物体

    [Header("目标层级")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;

    public bool damageEnemies = true;
    public bool damagePlayer = true; // 确保对玩家造成伤害

    [Header("触发设置")]
    public bool explodeOnPlayerAttack = true;

    [Header("组件引用")]
    public Animator anim;

    [Header("销毁设置")]
    public float destroyDelay = 0.01f;

    [Header("调试")]
    public bool enableDebug = true;

    private bool hasTriggered = false;
    private Collider2D explosionRangeCollider;
    private Collider2D tntCollider; // TNT自身的碰撞体

    void Start()
    {
        // 从外挂物体获取CircleCollider2D
        if (explosionRangeObject != null)
        {
            explosionRangeCollider = explosionRangeObject.GetComponent<Collider2D>();

            if (explosionRangeCollider != null && enableDebug)
            {
                Debug.Log($"TNT: 从 {explosionRangeObject.name} 获取到 {explosionRangeCollider.GetType().Name} 作为爆炸范围");
            }
            else if (enableDebug)
            {
                Debug.LogError($"TNT: {explosionRangeObject.name} 上没有找到CircleCollider2D组件!");
            }
        }
        else if (enableDebug)
        {
            Debug.LogWarning("TNT: 未设置爆炸范围物体!");
        }

        // 获取TNT自身的碰撞体
        tntCollider = GetComponent<Collider2D>();

        ValidateComponents();
    }

    void ValidateComponents()
    {
        if (enableDebug)
        {
            if (explosionRangeObject == null)
                Debug.LogError("TNT: 未设置爆炸范围物体！请在Inspector中分配包含CircleCollider2D的GameObject");
            else if (explosionRangeCollider == null)
                Debug.LogError("TNT: 爆炸范围物体上没有CircleCollider2D组件！");

            if (anim == null)
                Debug.LogWarning("TNT: Animator组件未分配");

            if (enemyLayer == 0)
                Debug.LogWarning("TNT: Enemy Layer未设置");

            if (damagePlayer && playerLayer == 0)
                Debug.LogWarning("TNT: Player Layer未设置");


            if (tntCollider == null)
                Debug.LogWarning("TNT: TNT GameObject上没有Collider2D组件，无法检测碰撞");
        }
    }

    /// <summary>
    /// 触发爆炸动画（玩家攻击时调用）
    /// </summary>
    void TriggerExplosion()
    {
        if (hasTriggered)
        {
            if (enableDebug) Debug.LogWarning("TNT动画已触发过，忽略重复调用");
            return;
        }

        hasTriggered = true;

        // 只设置动画参数为TRUE，不执行爆炸逻辑
        if (anim != null)
        {
            anim.SetBool("IsExploded", true);
            if (enableDebug) Debug.Log("TNT动画触发，等待动画事件调用Explode()");

        }
        else if (enableDebug)
        {
            Debug.LogWarning("TNT: 没有Animator组件");
        }
    }

    /// <summary>
    /// 执行爆炸（从动画事件调用）
    /// </summary>
    public void Explode()
    {
        if (enableDebug)
        {
            Debug.Log($"TNT在位置 {transform.position} 爆炸!");
        }

        // 执行伤害判定
        DealExplosionDamage();

        // 延迟销毁TNT对象
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// 处理爆炸伤害
    /// </summary>
    void DealExplosionDamage()
    {
        if (explosionRangeCollider == null)
        {
            Debug.LogError("TNT Error: 爆炸范围碰撞箱(explosionRangeCollider)未设置，无法造成伤害");
            return;
        }

        if (damageEnemies)
        {
            DamageEnemies();
        }

        // 检查 damagePlayer 布尔值
        if (damagePlayer)
        {
            DamagePlayer();
        }
    }

    void DamageEnemies()
    {
        if (enemyLayer == 0)
        {
            Debug.LogError("TNT: Enemy Layer未设置，无法检测敌人");
            return;
        }

        // 使用OverlapCollider检测范围内的敌人
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;

        List<Collider2D> enemies = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, enemies);

        if (enableDebug)
        {
            Debug.Log($"TNT爆炸检测到 {hitCount} 个敌人");
        }

        foreach (Collider2D enemy in enemies)
        {
            // 1. 获取敌人血量脚本 (EnemyHealth)
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // 🔥 核心修改：使用 TakeDamage 替代 ChangeHealth，并传入削韧值
                enemyHealth.TakeDamage(explosionDamage, explosionPoiseDamage);

                if (enableDebug)
                    Debug.Log($"对 {enemy.gameObject.name} 造成 {explosionDamage} 点伤害和 {explosionPoiseDamage} 点削韧");
            }

            // 2. 获取敌人击退脚本 (EnemyKonckBack) - ✨ 新增功能
            EnemyKonckBack knockback = enemy.GetComponent<EnemyKonckBack>();
            if (knockback != null)
            {
                // 参数：击退源头(TNT位置), 力度, 击退时间, 眩晕时间
                knockback.Knockback(transform, explosionForce, 0.2f, 0.5f);
            }
        }
    }

    void DamagePlayer()
    {
        if (playerLayer == 0)
        {
            Debug.LogError("TNT Error: Player Layer 未在Inspector中设置，无法检测玩家。");
            return;
        }

        // 使用OverlapCollider检测范围内的玩家
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;

        List<Collider2D> players = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, players);

        if (enableDebug) Debug.Log($"TNT爆炸检测到 {hitCount} 个玩家。");

        foreach (Collider2D player in players)
        {
            // 1. 造成伤害
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // PlayerHealth 还是用的 ChangeHealth (负数扣血)
                playerHealth.ChangeHealth(-explosionDamage);

                if (enableDebug)
                    Debug.Log($"成功对玩家造成 {explosionDamage} 点伤害。");
            }

            // 2. 造成击退 - ✨ 新增功能
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // 参数：击退源头, 力度, 眩晕时间
                playerMovement.Knockback(transform, explosionForce, 0.5f);
            }
        }
    }

    /// <summary>
    /// 只使用OnTriggerEnter2D来检测碰撞
    /// 注释掉OnCollisionEnter2D以避免重复触发
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 跳过自己和爆炸范围物体
        if (other.gameObject == gameObject || other.gameObject == explosionRangeObject)
        {
            return;
        }

        int otherLayer = other.gameObject.layer;

        // 检测到玩家时，只触发动画
        // (如果是用攻击触发，建议检查是否是玩家的武器Tag或者Attack Layer，这里暂时保持原样检查Player Layer)
        if (explodeOnPlayerAttack && IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug) Debug.Log("TNT被玩家接触/攻击触发，触发爆炸动画");
            TriggerExplosion();
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    private void OnDrawGizmosSelected()
    {
        // ... (保持原有的 Gizmos 代码不变) ...
        Collider2D rangeCollider = explosionRangeCollider;
        if (rangeCollider == null && explosionRangeObject != null)
        {
            rangeCollider = explosionRangeObject.GetComponent<Collider2D>();
        }

        if (rangeCollider != null && rangeCollider is CircleCollider2D)
        {
            CircleCollider2D circle = rangeCollider as CircleCollider2D;
            Vector3 colliderPosition = rangeCollider.transform.position;
            Vector3 center = colliderPosition + (Vector3)circle.offset;
            float worldRadius = circle.radius * rangeCollider.transform.lossyScale.x;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(center, worldRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, worldRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.15f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, center);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    public void DealDamage(int damage)
    {
        // 这是一个被外部调用的接口（例如玩家砍了一下TNT）
        // 我们不修改TNT的爆炸伤害，而是直接触发爆炸
        TriggerExplosion();
    }
}