using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickTNT : MonoBehaviour
{
    [Header("爆炸设置")]
    [Tooltip("爆炸伤害")]
    public int explosionDamage = 20;
    
    [Tooltip("爆炸范围对象")]
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
    
    [Header("粘附设置")]
    [Tooltip("不会粘附的层列表（TNT会穿过这些层）")]
    public List<LayerMask> notStickLayers = new List<LayerMask>();
    
    [Tooltip("粘附后是否成为子物体")]
    public bool becomeChildOnStick = true;
    
    [Tooltip("粘附后的偏移量")]
    public Vector3 stickOffset = Vector3.zero;
    
    [Tooltip("支持通过Trigger碰撞器粘附")]
    public bool canStickToTrigger = true;
    
    [Header("触发设置")]
    [Tooltip("玩家是否可以触发TNT爆炸")]
    public bool canBeTriggeredByPlayer = true;
    
    [Tooltip("敌人是否可以触发TNT爆炸")]
    public bool canBeTriggeredByEnemy = false;
    
    [Tooltip("是否允许其他物体触发（弹药、投掷物等）")]
    public bool canBeTriggeredByOthers = true;
    
    [Tooltip("可以触发TNT的其他层级列表")]
    public List<LayerMask> otherTriggerLayers = new List<LayerMask>();
    
    [Header("动画组件")]
    public Animator anim;
    
    [Header("销毁延迟")]
    public float destroyDelay = 0.01f;
    
    [Header("音效设置")]
    [Tooltip("粘附音效")]
    public AudioClip stickSound;
    
    [Tooltip("点燃音效")]
    public AudioClip igniteSound;
    
    [Tooltip("爆炸音效")]
    public AudioClip explosionSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("调试")]
    public bool enableDebug = true;
    
    private bool hasTriggered = false;
    private bool isInitialized = false;
    private bool isStuck = false;
    private Collider2D explosionRangeCollider;
    private Collider2D tntCollider;
    private Rigidbody2D rb;
    private Transform stuckTo;
    private Vector3 stuckLocalPosition;
    private AudioSource audioSource;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        tntCollider = GetComponent<Collider2D>();
        
        if (explosionRangeObject != null)
        {
            explosionRangeCollider = explosionRangeObject.GetComponent<Collider2D>();
        }
        
        // 设置音效组件
        SetupAudioSource();
        
        ValidateComponents();
        StartCoroutine(InitializeAfterDelay());
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 初始化完成 - Time: {Time.time}");
    }
    
    /// <summary>
    /// 设置音效组件
    /// </summary>
    void SetupAudioSource()
    {
        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 配置AudioSource
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = 0f; // 2D音效
        audioSource.Stop();
        
        if (enableDebug)
            Debug.Log("[StickTNT] 音效组件设置完成");
    }
    
    IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 延迟初始化完成");
    }
    
    void ValidateComponents()
    {
        if (enableDebug)
        {
            if (explosionRangeObject == null)
                Debug.LogError("[StickTNT] 未设置爆炸范围对象");
            if (anim == null)
                Debug.LogWarning("[StickTNT] Animator未设置");
            if (rb == null)
                Debug.LogWarning("[StickTNT] 需要Rigidbody2D组件来实现粘附");
            if (tntCollider == null)
                Debug.LogWarning("[StickTNT] 需要Collider2D组件来检测碰撞");
            if (audioSource == null)
                Debug.LogWarning("[StickTNT] AudioSource组件未设置");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (enableDebug)
            Debug.Log($"[StickTNT] OnCollisionEnter2D - 碰到: {collision.gameObject.name}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        // 如果已经粘附或已触发爆炸，不再处理
        if (isStuck || hasTriggered) return;
        
        // 检查是否是不粘附的层
        if (IsInNotStickLayers(collision.gameObject.layer))
        {
            if (enableDebug)
                Debug.Log($"[StickTNT] 穿过不粘附层: {LayerMask.LayerToName(collision.gameObject.layer)}");
            return;
        }
        
        // 粘附到物体上
        StickToObject(collision.transform);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebug)
            Debug.Log($"[StickTNT] OnTriggerEnter2D - 碰到: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}, isStuck: {isStuck}, hasTriggered: {hasTriggered}");
        
        // 只处理粘附逻辑，不处理爆炸触发
        if (!isStuck && !hasTriggered && canStickToTrigger)
        {
            // 检查是否是不粘附的层
            if (IsInNotStickLayers(other.gameObject.layer))
            {
                if (enableDebug)
                    Debug.Log($"[StickTNT] 穿过不粘附层（Trigger）: {LayerMask.LayerToName(other.gameObject.layer)}");
                return;
            }
            
            // 粘附到物体上
            if (enableDebug)
                Debug.Log($"[StickTNT] 通过Trigger粘附到: {other.gameObject.name}");
            
            StickToObject(other.transform);
        }
    }
    
    void StickToObject(Transform target)
    {
        isStuck = true;
        
        // 停止物理运动
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // 保存粘附位置
        stuckTo = target;
        
        // 是否成为子物体
        if (becomeChildOnStick)
        {
            transform.SetParent(stuckTo);
            stuckLocalPosition = transform.localPosition + stickOffset;
            transform.localPosition = stuckLocalPosition;
        }
        else
        {
            // 保存相对位置
            stuckLocalPosition = transform.position - stuckTo.position + stickOffset;
        }
        
        // 播放粘附音效
        PlaySound(stickSound);
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 粘附成功 - 目标: {target.gameObject.name}, Layer: {LayerMask.LayerToName(target.gameObject.layer)}");
        
        // 粘附后立即触发爆炸
        if (enableDebug)
            Debug.Log($"[StickTNT] 粘附完成，立即触发爆炸动画！");
        
        TriggerExplosion();
    }
    
    void Update()
    {
        // 如果粘附但不是子物体，手动跟随目标
        if (isStuck && !becomeChildOnStick && stuckTo != null)
        {
            transform.position = stuckTo.position + stuckLocalPosition;
        }
    }
    
    // 公共方法：外部调用触发爆炸（例如通过爆炸范围检测器）
    public void TriggerExplosion()
    {
        if (hasTriggered) return;
        
        hasTriggered = true;
        
        // 解除粘附
        if (isStuck && becomeChildOnStick)
        {
            transform.SetParent(null);
        }
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 触发爆炸 - Time: {Time.time}");
        
        // 播放点燃音效
        if (isInitialized)
        {
            PlaySound(igniteSound);
        }
        
        // 触发动画（动画事件会调用Explode方法）
        if (anim != null)
        {
            anim.SetBool("IsExploded", true);
            
            if (enableDebug)
                Debug.Log($"[StickTNT] 爆炸动画已触发 - IsExploded = true");
        }
    }
    
    public void Explode()
    {
        if (enableDebug)
            Debug.Log($"[StickTNT] 执行爆炸 - 位置: {transform.position}");
        
        // 播放爆炸音效
        PlaySound(explosionSound);
        
        // 执行伤害判定
        DealExplosionDamage();
        
        // 销毁对象
        Destroy(gameObject, destroyDelay);
    }
    
    void DealExplosionDamage()
    {
        if (explosionRangeCollider == null)
        {
            Debug.LogError("[StickTNT] 爆炸范围碰撞器未设置");
            return;
        }
        
        if (damageEnemies)
        {
            DamageEnemies();
        }
        
        if (damagePlayer)
        {
            DamagePlayer();
        }
    }
    
    void DamageEnemies()
    {
        if (enemyLayer == 0) return;
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;
        
        List<Collider2D> enemies = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, enemies);
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 检测到 {hitCount} 个敌人");
        
        foreach (Collider2D enemy in enemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(explosionDamage, 0);
                
                if (enableDebug)
                    Debug.Log($"[StickTNT] 对 {enemy.gameObject.name} 造成 {explosionDamage} 点伤害");
            }
        }
    }
    
    void DamagePlayer()
    {
        if (playerLayer == 0) return;
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;
        
        List<Collider2D> players = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, players);
        
        if (enableDebug)
            Debug.Log($"[StickTNT] 检测到 {hitCount} 个玩家");
        
        foreach (Collider2D player in players)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(-explosionDamage);
                
                if (enableDebug)
                    Debug.Log($"[StickTNT] 对玩家造成 {explosionDamage} 点伤害");
            }
        }
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
            
            if (enableDebug)
                Debug.Log($"[StickTNT] 播放音效: {clip.name}");
        }
    }
    
    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask) != 0;
    }
    
    bool IsInNotStickLayers(int layer)
    {
        if (notStickLayers == null || notStickLayers.Count == 0)
            return false;
        
        foreach (var layerMask in notStickLayers)
        {
            if (IsInLayerMask(layer, layerMask))
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制爆炸范围
        if (explosionRangeCollider != null)
        {
            Gizmos.color = Color.red;
            
            if (explosionRangeCollider is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }
        
        // 绘制粘附状态
        if (isStuck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
