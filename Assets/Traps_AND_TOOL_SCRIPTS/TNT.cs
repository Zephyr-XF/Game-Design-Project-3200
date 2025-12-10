using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TNT 炸药陷阱
/// 功能：玩家或其他物体触碰后触发爆炸动画，对范围内的敌人和玩家造成伤害
/// 特点：支持延迟初始化音效系统，避免场景加载时的音效问题
/// </summary>
public class TNT : MonoBehaviour
{
    [Header("爆炸设置")]
    [Tooltip("爆炸伤害")]
    public int explosionDamage = 20;

    [Tooltip("爆炸范围对象（需要有CircleCollider2D组件）")]
    public GameObject explosionRangeObject;

    [Header("目标层级")]
    [Tooltip("敌人层")]
    public LayerMask enemyLayer;

    [Tooltip("玩家层")]
    public LayerMask playerLayer;

    [Tooltip("对敌人造成伤害")]
    public bool damageEnemies = true;

    [Tooltip("对玩家造成伤害")]
    public bool damagePlayer = true;

    [Header("触发设置")]
    [Tooltip("玩家是否可以触发TNT爆炸")]
    public bool canBeTriggeredByPlayer = true;

    [Tooltip("敌人是否可以触发TNT爆炸")]
    public bool canBeTriggeredByEnemy = false;

    [Header("其他触发设置")]
    [Tooltip("是否允许其他物体触发（弹药、投掷物等）")]
    public bool canBeTriggeredByOthers = true;

    [Tooltip("可以触发TNT的其他层级列表（如弹药层、投掷物层、其他TNT等）")]
    public List<LayerMask> otherTriggerLayers = new List<LayerMask>();

    [Header("动画组件")]
    [Tooltip("动画控制器")]
    public Animator anim;

    [Header("销毁延迟")]
    [Tooltip("爆炸后延迟销毁时间（秒）")]
    public float destroyDelay = 0.01f;

    [Header("音效设置")]
    [Tooltip("音效源组件")]
    public AudioSource audioSource;

    [Tooltip("点燃音效（动画开始时播放）")]
    public AudioClip igniteSound;

    [Tooltip("爆炸音效（爆炸瞬间播放）")]
    public AudioClip explosionSound;

    [Header("调试")]
    [Tooltip("启用调试日志")]
    public bool enableDebug = true;

    // 私有变量
    private bool hasTriggered = false;      // 是否已触发爆炸
    private bool isInitialized = false;     // 音效系统是否已初始化
    private bool canBeTriggered = false;    // 是否可以被触发（添加这个）
    private Collider2D explosionRangeCollider;  // 爆炸范围碰撞器
    private Collider2D tntCollider;         // TNT本体的碰撞器

    void Start()
    {
        if (enableDebug)
            Debug.Log($"[TNT] Start 被调用 - Time.time: {Time.time}");

        // 强制禁用 AudioSource 的 PlayOnAwake，防止自动播放
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();

            if (enableDebug)
                Debug.Log($"[TNT] AudioSource.playOnAwake 已设置为 false");
        }

        // 从爆炸范围对象获取CircleCollider2D
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
            Debug.LogWarning("TNT: 未设置爆炸范围对象!");
        }

        // 获取TNT本体的碰撞器
        tntCollider = GetComponent<Collider2D>();

        ValidateComponents();

        // 延迟初始化音效系统和启用触发
        StartCoroutine(InitializeAfterDelay());
    }

    /// <summary>
    /// 延迟初始化音效系统，避免场景加载时的音效问题
    /// </summary>
    IEnumerator InitializeAfterDelay()
    {
        // 添加延迟以避免初始化时的碰撞检测
        yield return new WaitForSeconds(0.2f);
        
        isInitialized = true;
        canBeTriggered = true; // 启用触发检测

        if (enableDebug)
            Debug.Log($"[TNT] 音效初始化完成，触发系统已启用 - Time.time: {Time.time}");
    }

    /// <summary>
    /// 验证必要组件是否设置正确
    /// </summary>
    void ValidateComponents()
    {
        if (enableDebug)
        {
            if (explosionRangeObject == null)
                Debug.LogError("TNT: 未设置爆炸范围对象，请在Inspector中分配一个带CircleCollider2D的GameObject");
            else if (explosionRangeCollider == null)
                Debug.LogError("TNT: 爆炸范围对象上没有CircleCollider2D组件");

            if (anim == null)
                Debug.LogWarning("TNT: Animator组件未设置");

            if (enemyLayer == 0)
                Debug.LogWarning("TNT: Enemy Layer未设置");

            if (damagePlayer && playerLayer == 0)
                Debug.LogWarning("TNT: Player Layer未设置");

            if (tntCollider == null)
                Debug.LogWarning("TNT: TNT GameObject上没有Collider2D组件，无法检测碰撞");

            if (canBeTriggeredByOthers && (otherTriggerLayers == null || otherTriggerLayers.Count == 0))
                Debug.LogWarning("TNT: 允许其他物体触发，但触发层列表为空");
            else if (canBeTriggeredByOthers && otherTriggerLayers != null)
            {
                int enabledCount = 0;
                foreach (var layerMask in otherTriggerLayers)
                {
                    if (layerMask != 0)
                    {
                        enabledCount++;
                    }
                }

                if (enabledCount == 0)
                    Debug.LogWarning("TNT: 触发层列表中所有层都未设置");
                else if (enableDebug)
                    Debug.Log($"TNT: 设置了 {enabledCount} 个可用的触发层");
            }
        }
    }

    /// <summary>
    /// 触发爆炸（由碰撞事件调用）
    /// </summary>
    void TriggerExplosion()
    {
        if (hasTriggered)
        {
            if (enableDebug) Debug.LogWarning("TNT已经被触发，忽略重复触发");
            return;
        }

        hasTriggered = true;

        if (enableDebug)
            Debug.Log($"[TNT] TriggerExplosion 被调用 - isInitialized: {isInitialized}, Time.time: {Time.time}");

        // 播放点燃音效（只在初始化完成后才播放）
        if (isInitialized && audioSource != null && igniteSound != null)
        {
            audioSource.PlayOneShot(igniteSound);
            if (enableDebug)
                Debug.Log($"[TNT] 点燃音效已播放 - AudioClip: {igniteSound.name}");
        }
        else if (!isInitialized && enableDebug)
        {
            Debug.Log($"[TNT] 初始化未完成，跳过播放点燃音效");
        }

        // 只设置动画参数为TRUE，由动画事件调用Explode()
        if (anim != null)
        {
            anim.SetBool("IsExploded", true);
            if (enableDebug) Debug.Log("TNT：已设置动画，等待事件调用Explode()");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("TNT: 没有Animator组件");
        }
    }

    /// <summary>
    /// 执行爆炸（由动画事件调用）
    /// </summary>
    public void Explode()
    {
        if (enableDebug)
        {
            Debug.Log($"[TNT] Explode 被调用，位置: {transform.position}, Time.time: {Time.time}");
        }

        // 播放爆炸音效（爆炸瞬间）
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
            if (enableDebug)
                Debug.Log($"[TNT] 爆炸音效已播放 - AudioClip: {explosionSound.name}");
        }
        else if (enableDebug)
        {
            if (audioSource == null)
                Debug.LogWarning($"[TNT] AudioSource 为空");
            if (explosionSound == null)
                Debug.LogWarning($"[TNT] explosionSound 为空");
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
            Debug.LogError("TNT Error: 爆炸范围碰撞器(explosionRangeCollider)未设置，无法造成伤害");
            return;
        }

        if (damageEnemies)
        {
            DamageEnemies();
        }

        // 在爆炸瞬间打印 damagePlayer 参数的实际值
        if (enableDebug) Debug.Log($"[爆炸时检查] 爆炸瞬间, damagePlayer 的值为: {damagePlayer}");

        // 检查 damagePlayer 参数值
        if (damagePlayer)
        {
            if (enableDebug) Debug.Log("DamagePlayer 条件通过，准备对玩家造成伤害...");
            DamagePlayer();
        }
        else
        {
            if (enableDebug) Debug.LogWarning("DamagePlayer 为 false，不会对玩家造成伤害（请在Inspector中检查）。");
        }
    }

    /// <summary>
    /// 对范围内的敌人造成伤害
    /// </summary>
    void DamageEnemies()
    {
        if (enemyLayer == 0)
        {
            Debug.LogError("TNT: Enemy Layer未设置，无法伤害敌人");
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
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(explosionDamage, 0);

                if (enableDebug)
                    Debug.Log($"对 {enemy.gameObject.name} 造成 {explosionDamage} 点伤害");
            }
        }
    }

    /// <summary>
    /// 对范围内的玩家造成伤害
    /// </summary>
    void DamagePlayer()
    {
        if (playerLayer == 0)
        {
            Debug.LogError("TNT Error: Player Layer 未在Inspector中设置，无法伤害玩家。");
            return;
        }

        // 使用OverlapCollider检测范围内的玩家
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;

        List<Collider2D> players = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, players);

        if (enableDebug) Debug.Log($"TNT爆炸检测到 {hitCount} 个玩家。");

        if (hitCount == 0)
        {
            if (enableDebug) Debug.LogWarning("未检测到玩家。原因: \n1. 玩家是否在爆炸范围内？\n2. 玩家的Layer是否正确设置为'Player'？\n3. TNT的Player Layer Mask是否已勾选'Player'？");
        }

        foreach (Collider2D player in players)
        {
            if (enableDebug) Debug.Log($"找到玩家: {player.gameObject.name}");
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(-explosionDamage);

                if (enableDebug)
                    Debug.Log($"成功对玩家造成 {explosionDamage} 点伤害！");
            }
            else
            {
                if (enableDebug) Debug.LogError($"玩家对象 {player.gameObject.name} 上没有找到 PlayerHealth 脚本组件");
            }
        }
    }

    /// <summary>
    /// 只使用OnTriggerEnter2D检测碰撞
    /// 注释掉OnCollisionEnter2D以避免重复触发
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 初始化未完成时，忽略所有触发
        if (!canBeTriggered)
        {
            if (enableDebug)
                Debug.Log($"[TNT] 初始化未完成，忽略触发: {other.gameObject.name}");
            return;
        }
        
        // 忽略自己和爆炸范围对象
        if (other.gameObject == gameObject || other.gameObject == explosionRangeObject)
        {
            return;
        }

        int otherLayer = other.gameObject.layer;

        if (enableDebug)
            Debug.Log($"TNT被 {other.gameObject.name} 触碰 (Layer: {LayerMask.LayerToName(otherLayer)})");

        // 检测到玩家时，触发爆炸
        if (canBeTriggeredByPlayer && IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug) Debug.Log("TNT被玩家触碰，准备触发爆炸。");
            TriggerExplosion();
            return;
        }

        // 检测到其他触发物体
        if (canBeTriggeredByOthers)
        {
            foreach (var layerMask in otherTriggerLayers)
            {
                if (IsInLayerMask(otherLayer, layerMask))
                {
                    if (enableDebug) Debug.Log($"TNT被其他触发物体触碰，准备爆炸。");
                    TriggerExplosion();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 检查指定层是否在LayerMask中
    /// </summary>
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    /// <summary>
    /// 在Scene视图中可视化爆炸范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中可视化爆炸范围
        Collider2D rangeCollider = explosionRangeCollider;

        // 如果游戏未运行，可以从explosionRangeObject获取
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

            // 绘制半透明红色圆
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(center, worldRadius);

            // 绘制红色边框圆
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, worldRadius);

            // 绘制TNT中心点
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.15f);

            // 绘制连线显示TNT与爆炸范围的关系
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, center);
        }
        else
        {
            // 如果没有找到CircleCollider2D，绘制警告
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    /// <summary>
    /// 公共方法：外部直接触发爆炸并设置伤害
    /// </summary>
    public void DealDamage(int damage)
    {
        explosionDamage = damage;
        TriggerExplosion();
    }
}