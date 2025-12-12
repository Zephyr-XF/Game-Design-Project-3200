using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniBlueHole : MonoBehaviour
{
    [Header("动画组件")]
    [Tooltip("动画控制器")]
    public Animator animator;
    
    [Header("音效设置")]
    [Tooltip("蓝洞生成音效")]
    public AudioClip spawnSound;
    
    [Tooltip("推力音效")]
    public AudioClip pushSound;
    
    [Tooltip("蓝洞消失音效")]
    public AudioClip despawnSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("推力设置")]
    [Tooltip("可被推走的目标图层名称")]
    public string[] targetLayerNames = new string[] { "Enemy" };
    
    [Tooltip("推力强度")]
    [Range(1f, 1000f)]
    public float pushForce = 15f;
    
    [Tooltip("推力范围半径")]
    [Range(1f, 10f)]
    public float pushRadius = 5f;
    
    [Tooltip("最大推力距离（超过此距离不再推力）")]
    [Range(5f, 20f)]
    public float maxPushDistance = 10f;
    
    [Tooltip("销毁延迟（秒）")]
    [Range(0f, 2f)]
    public float destroyDelay = 0.5f;
    
    [Header("伤害设置")]
    [Tooltip("是否对敌人造成伤害")]
    public bool dealDamage = false;
    
    [Tooltip("造成的伤害")]
    [Range(0f, 100f)]
    public float damage = 10f;
    
    [Header("韧性伤害设置")]
    [Tooltip("是否对敌人造成韧性伤害")]
    public bool dealResilienceDamage = true;
    
    [Tooltip("造成的韧性伤害")]
    [Range(0f, 100f)]
    public float resilienceDamage = 20f;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    // 私有变量
    private Rigidbody2D blueHoleRigidbody;
    private AudioSource audioSource;
    private bool hasPushed = false;
    
    void Start()
    {
        // 获取或添加Animator组件
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null && enableDebug)
            {
                Debug.LogWarning("[MiniBlueHole] 未找到Animator组件");
            }
        }
        
        // 设置音效组件
        SetupAudioSources();
        
        // 播放生成音效
        PlaySound(spawnSound);
        
        // 获取蓝洞自己的Rigidbody2D
        blueHoleRigidbody = GetComponent<Rigidbody2D>();
        if (blueHoleRigidbody != null)
        {
            // 锁定蓝洞自己的Rigidbody2D
            blueHoleRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            
            if (enableDebug)
                Debug.Log($"[MiniBlueHole] 找到蓝洞Rigidbody2D并锁定");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[MiniBlueHole] 蓝洞没有Rigidbody2D组件");
        }
        
        // 立即推力
        PerformPush();
        
        // 延迟销毁
        Destroy(gameObject, destroyDelay);
        
        if (enableDebug)
            Debug.Log($"[MiniBlueHole] 蓝洞生成，推力范围: {pushRadius}m，将在 {destroyDelay}s 后销毁");
    }
    
    /// <summary>
    /// 执行推力（仅执行一次）
    /// </summary>
    private void PerformPush()
    {
        if (hasPushed) return;
        
        hasPushed = true;
        
        // 设置动画参数为推力状态
        SetAnimationParameter("IsPushing", true);
        
        // 播放推力音效
        PlaySound(pushSound);
        
        // 使用OverlapCircle检测范围内的所有对象
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pushRadius);
        
        if (enableDebug)
            Debug.Log($"[MiniBlueHole] 开始推力，检测到 {colliders.Length} 个对象");
        
        foreach (Collider2D col in colliders)
        {
            // 检查是否在目标图层列表中
            if (IsInTargetLayers(col.gameObject))
            {
                GameObject enemy = col.gameObject;
                
                // 计算从蓝洞向外的方向和距离
                Vector2 direction = (Vector2)enemy.transform.position - (Vector2)transform.position;
                float distance = direction.magnitude;
                
                // 超过最大推力距离，跳过推力
                if (distance > maxPushDistance)
                {
                    if (enableDebug)
                        Debug.Log($"[MiniBlueHole] {enemy.name} 超出最大推力距离");
                    continue;
                }
                
                // 归一化方向（向外推）
                direction.Normalize();
                
                // 距离越近，推力越强（平方反比）
                float distanceMultiplier = Mathf.Clamp01(1f - (distance / maxPushDistance));
                float currentPushForce = pushForce * distanceMultiplier;
                
                // 尝试通过EnemyMovement施加推力
                EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
                if (enemyMovement != null)
                {
                    // 使用负的力让EnemyMovement向外推
                    enemyMovement.ApplyForceAwayFromPoint(transform.position, currentPushForce);
                    
                    if (enableDebug)
                        Debug.Log($"[MiniBlueHole] 通过EnemyMovement推开 {enemy.name}，推力: {currentPushForce}");
                }
                else
                {
                    // 如果没有EnemyMovement，直接对Rigidbody2D施加推力
                    Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.AddForce(direction * currentPushForce, ForceMode2D.Impulse);
                        
                        if (enableDebug)
                            Debug.Log($"[MiniBlueHole] 通过Rigidbody2D推开 {enemy.name}，推力: {currentPushForce}");
                    }
                }
                
                // 造成伤害
                if (dealDamage || dealResilienceDamage)
                {
                    EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        int damageAmount = dealDamage ? (int)damage : 0;
                        float resilienceDamageAmount = dealResilienceDamage ? resilienceDamage : 0f;
                        
                        enemyHealth.TakeDamage(damageAmount, resilienceDamageAmount);
                        
                        if (enableDebug)
                        {
                            if (dealDamage && dealResilienceDamage)
                                Debug.Log($"[MiniBlueHole] 对 {enemy.name} 造成 {damageAmount} 伤害 和 {resilienceDamageAmount} 韧性伤害");
                            else if (dealDamage)
                                Debug.Log($"[MiniBlueHole] 对 {enemy.name} 造成 {damageAmount} 伤害");
                            else if (dealResilienceDamage)
                                Debug.Log($"[MiniBlueHole] 对 {enemy.name} 造成 {resilienceDamageAmount} 韧性伤害");
                        }
                    }
                }
            }
        }
        
        if (enableDebug)
            Debug.Log("[MiniBlueHole] 推力完成");
    }
    
    /// <summary>
    /// 设置动画参数
    /// </summary>
    /// <param name="paramName">参数名</param>
    /// <param name="value">参数值</param>
    public void SetAnimationParameter(string paramName, bool value)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // 检查参数是否存在
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(paramName, value);
                    
                    if (enableDebug)
                        Debug.Log($"[MiniBlueHole] 设置动画参数: {paramName} = {value}");
                    
                    return;
                }
            }
            
            if (enableDebug)
                Debug.LogWarning($"[MiniBlueHole] 动画参数 '{paramName}' 不存在或类型不匹配");
        }
    }
    
    /// <summary>
    /// 检查游戏对象是否在目标图层中
    /// </summary>
    private bool IsInTargetLayers(GameObject obj)
    {
        if (obj == null) return false;
        
        int layer = obj.layer;
        foreach (string layerName in targetLayerNames)
        {
            if (LayerMask.NameToLayer(layerName) == layer)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 设置音效组件
    /// </summary>
    private void SetupAudioSources()
    {
        // 主音效源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = 0f; // 2D音效
        
        if (enableDebug)
            Debug.Log("[MiniBlueHole] 音效组件设置完成");
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
            
            if (enableDebug)
                Debug.Log($"[MiniBlueHole] 播放音效: {clip.name}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制推力范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pushRadius);
        
        // 绘制最大推力距离
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxPushDistance);
    }
}
