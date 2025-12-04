using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNT : MonoBehaviour
{
    [Header("爆炸设置")]
    public int explosionDamage = 20;
    
    [Header("爆炸范围设置")]
    public GameObject explosionRangeObject; // 需要有CircleCollider2D的子物体
    
    [Header("目标层级")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;

    public bool damageEnemies = true;
    public bool damagePlayer = true;
    
    [Header("触发设置")]
    public bool explodeOnPlayerAttack = true;
    
    [Header("动画设置")]
    public Animator anim;
    
    [Header("销毁延迟")]
    public float destroyDelay = 0.01f;
    
    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip igniteSound;      // 引爆音效（动画开始时播放）
    public AudioClip explosionSound;   // 爆炸音效（爆炸瞬间播放）
    
    [Header("调试")]
    public bool enableDebug = true;
    
    private bool hasTriggered = false;
    private bool isInitialized = false;
    private Collider2D explosionRangeCollider;
    private Collider2D tntCollider; // TNT自身的碰撞体

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
        
        // 从子物体获取CircleCollider2D
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
        
        // 延迟初始化
        StartCoroutine(InitializeAfterDelay());
    }
    
    IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[TNT] ? 初始化完成，音效系统已启用 - Time.time: {Time.time}");
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
            if (enableDebug) Debug.LogWarning("TNT已经被触发，不可重复触发");
            return;
        }
        
        hasTriggered = true;
        
        if (enableDebug)
            Debug.Log($"[TNT] TriggerExplosion 被调用 - isInitialized: {isInitialized}, Time.time: {Time.time}");
        
        // 播放引爆音效（只有在初始化完成后才播放）
        if (isInitialized && audioSource != null && igniteSound != null)
        {
            audioSource.PlayOneShot(igniteSound);
            if (enableDebug)
                Debug.Log($"[TNT] ? 引爆音效已播放 - AudioClip: {igniteSound.name}");
        }
        else if (!isInitialized && enableDebug)
        {
            Debug.Log($"[TNT] ? 初始化未完成，跳过播放引爆音效");
        }
        
        // 只设置动画参数为TRUE即可执行爆炸逻辑
        if (anim != null)
        {
            anim.SetBool("IsExploded", true);
            if (enableDebug) Debug.Log("TNT：设置动画，等待事件调用Explode()");
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
            Debug.Log($"[TNT] Explode 被调用，位置: {transform.position}, Time.time: {Time.time}");
        }
        
        // 播放爆炸音效（爆炸瞬间）
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
            if (enableDebug)
                Debug.Log($"[TNT] ? 爆炸音效已播放 - AudioClip: {explosionSound.name}");
        }
        else if (enableDebug)
        {
            if (audioSource == null)
                Debug.LogWarning($"[TNT] ? AudioSource 为空");
            if (explosionSound == null)
                Debug.LogWarning($"[TNT] ? explosionSound 为空");
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
        
        // 新增：在爆炸的瞬间打印 damagePlayer 的真实值
        if (enableDebug) Debug.Log($"[运行时检查] 爆炸瞬间, damagePlayer 的值是: {damagePlayer}");

        // 检查 damagePlayer 布尔值
        if (damagePlayer)
        {
            if (enableDebug) Debug.Log("DamagePlayer 检查通过，准备对玩家造成伤害...");
            DamagePlayer();
        }
        else
        {
            if (enableDebug) Debug.LogWarning("DamagePlayer 为 false，跳过对玩家的伤害。请在Inspector中检查。");
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
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-explosionDamage);
                
                if (enableDebug) 
                    Debug.Log($"对 {enemy.gameObject.name} 造成 {explosionDamage} 点伤害");
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
        
        if (hitCount == 0)
        {
            if(enableDebug) Debug.LogWarning("未检测到玩家。请检查: \n1. 玩家是否在爆炸范围内？\n2. 玩家的Layer是否设置为'Player'？\n3. TNT的Player Layer Mask是否已勾选'Player'？");
        }
        
        foreach (Collider2D player in players)
        {
            if (enableDebug) Debug.Log($"找到玩家: {player.gameObject.name}");
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(-explosionDamage);
                
                if (enableDebug) 
                    Debug.Log($"成功对玩家造成 {explosionDamage} 点伤害。");
            }
            else
            {
                if (enableDebug) Debug.LogError($"错误：玩家 {player.gameObject.name} 身上没有找到 PlayerHealth 脚本！");
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
        
        if (enableDebug) 
            Debug.Log($"TNT被 {other.gameObject.name} 触发 (Layer: {LayerMask.LayerToName(otherLayer)})");
        
        // 检测到玩家时，只触发动画
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug) Debug.Log("TNT被玩家攻击触发，触发爆炸动画");
            TriggerExplosion();
        }
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    

    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中可视化爆炸范围
        Collider2D rangeCollider = explosionRangeCollider;
        
        // 如果游戏未运行，尝试从explosionRangeObject获取
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
            
            // 绘制半透明填充圆
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(center, worldRadius);
            
            // 绘制红色线框圆
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, worldRadius);
            
            // 绘制TNT中心点
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.15f);
            
            // 绘制连线显示TNT和爆炸范围的关系
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

    public void DealDamage(int damage)
    {
        explosionDamage = damage;
        TriggerExplosion();
    }
}