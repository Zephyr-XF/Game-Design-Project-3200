using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniBlackHole : MonoBehaviour
{
    [Header("动画控制")]
    [Tooltip("动画控制器")]
    public Animator animator;
    
    [Header("音效设置")]
    [Tooltip("黑洞生成音效")]
    public AudioClip spawnSound;
    
    [Tooltip("吸引开始音效")]
    public AudioClip pullStartSound;
    
    [Tooltip("吸引循环音效")]
    public AudioClip pullLoopSound;
    
    [Tooltip("黑洞消失音效")]
    public AudioClip despawnSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("吸引设置")]
    [Tooltip("可被吸引的目标图层名称")]
    public string[] targetLayerNames = new string[] { "Enemy" };
    
    [Tooltip("吸引力强度")]
    [Range(1f, 1000f)]
    public float pullForce = 10f;
    
    [Tooltip("吸引范围半径")]
    [Range(1f, 10f)]
    public float pullRadius = 5f;
    
    [Tooltip("最大吸引距离（超过此距离不再吸引）")]
    [Range(5f, 20f)]
    public float maxPullDistance = 10f;
    
    [Tooltip("延迟开始吸引的时间（秒，0表示立即开始）")]
    [Range(0f, 5f)]
    public float delayBeforePull = 0.5f;
    
    [Tooltip("吸引持续时间（秒）")]
    [Range(1f, 10f)]
    public float pullDuration = 3f;
    
    [Tooltip("黑洞生命周期（秒，0表示永久）")]
    [Range(0f, 20f)]
    public float lifetime = 5f;
    
    [Header("伤害设置")]
    [Tooltip("是否对敌人造成伤害")]
    public bool dealDamage = false;
    
    [Tooltip("每秒造成的伤害")]
    [Range(0f, 50f)]
    public float damagePerSecond = 5f;
    
    [Header("韧性伤害设置")]
    [Tooltip("是否对敌人造成韧性伤害")]
    public bool dealResilienceDamage = true;
    
    [Tooltip("每秒造成的韧性伤害")]
    [Range(0f, 50f)]
    public float resilienceDamagePerSecond = 10f;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    // 私有变量
    private bool isPulling = false;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    private Dictionary<GameObject, Coroutine> damageCoroutines = new Dictionary<GameObject, Coroutine>();
    private Coroutine pullCoroutine;
    private Rigidbody2D blackHoleRigidbody;
    private RigidbodyConstraints2D originalBlackHoleConstraints;
    private AudioSource audioSource;
    private AudioSource loopAudioSource;
    
    void Start()
    {
        // 获取或添加Animator组件
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[MiniBlackHole] 未找到Animator组件");
            }
        }
        
        // 设置音效组件
        SetupAudioSources();
        
        // 播放生成音效
        PlaySound(spawnSound);
        
        // 获取黑洞自己的Rigidbody2D
        blackHoleRigidbody = GetComponent<Rigidbody2D>();
        if (blackHoleRigidbody != null)
        {
            // 保存原始约束
            originalBlackHoleConstraints = blackHoleRigidbody.constraints;
            
            if (enableDebug)
                Debug.Log($"[MiniBlackHole] 找到黑洞Rigidbody2D，原始约束: {originalBlackHoleConstraints}");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[MiniBlackHole] 黑洞没有Rigidbody2D组件");
        }
        
        // 延迟吸引或直接开始
        if (delayBeforePull > 0)
        {
            StartCoroutine(DelayedStartPulling());
        }
        else
        {
            // 立即开始吸引
            StartPulling();
        }
        
        // 如果设置了生命周期，设置定时销毁
        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
        }
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 黑洞生成，延迟: {delayBeforePull}s，吸引范围: {pullRadius}m，吸引时长: {pullDuration}s");
    }
    
    void FixedUpdate()
    {
        // 如果正在吸引，检测范围内的敌人并拉取
        if (isPulling)
        {
            DetectEnemiesInRange();
            PullEnemies();
        }
    }
    
    /// <summary>
    /// 检测范围内的敌人（手动检测，不使用碰撞箱）
    /// </summary>
    private void DetectEnemiesInRange()
    {
        // 使用临时列表记录当前帧的敌人
        List<GameObject> currentEnemies = new List<GameObject>();
        
        // 使用OverlapCircle检测范围内的所有对象
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pullRadius);
        
        foreach (Collider2D col in colliders)
        {
            // 检查是否在目标图层列表中
            if (IsInTargetLayers(col.gameObject))
            {
                currentEnemies.Add(col.gameObject);
                
                // 如果是新进入的敌人且启用了伤害，启动伤害协程
                if (dealDamage && !damageCoroutines.ContainsKey(col.gameObject))
                {
                    Coroutine damageCoroutine = StartCoroutine(DealDamageOverTime(col.gameObject));
                    damageCoroutines[col.gameObject] = damageCoroutine;
                    
                    if (enableDebug)
                        Debug.Log($"[MiniBlackHole] 检测到新敌人: {col.name} (图层: {LayerMask.LayerToName(col.gameObject.layer)})");
                }
            }
        }
        
        // 停止已离开范围的敌人的伤害协程
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in damageCoroutines)
        {
            if (!currentEnemies.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
                toRemove.Add(kvp.Key);
                
                if (enableDebug)
                    Debug.Log($"[MiniBlackHole] 敌人离开范围: {kvp.Key.name}");
            }
        }
        
        foreach (var enemy in toRemove)
        {
            damageCoroutines.Remove(enemy);
        }
        
        // 更新列表
        enemiesInRange = currentEnemies;
    }
    
    /// <summary>
    /// 拉取范围内的所有敌人
    /// </summary>
    private void PullEnemies()
    {
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            GameObject enemy = enemiesInRange[i];
            
            // 检查对象是否仍然有效
            if (enemy == null)
            {
                enemiesInRange.RemoveAt(i);
                continue;
            }
            
            // 计算到黑洞的方向和距离
            Vector2 direction = (Vector2)transform.position - (Vector2)enemy.transform.position;
            float distance = direction.magnitude;
            
            // 如果超过最大距离，不再吸引
            if (distance > maxPullDistance)
                continue;
            
            // 归一化方向
            direction.Normalize();
            
            // 距离越近，吸引力越强（平方反比）
            float distanceMultiplier = Mathf.Clamp01(1f - (distance / maxPullDistance));
            float currentPullForce = pullForce * distanceMultiplier;
            
            // 尝试通过EnemyMovement施加力
            EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
            if (enemyMovement != null)
            {
                enemyMovement.ApplyForceToPoint(transform.position, currentPullForce);
            }
            else
            {
                // 如果没有EnemyMovement，直接对Rigidbody2D施加力
                Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(direction * currentPullForce, ForceMode2D.Force);
                }
            }
        }
    }
    
    /// <summary>
    /// 对敌人持续造成伤害和韧性伤害
    /// </summary>
    private IEnumerator DealDamageOverTime(GameObject enemy)
    {
        if (enemy == null) yield break;
        
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth == null) yield break;
        
        float damageInterval = 0.5f; // 每0.5秒造成一次伤害
        float damagePerTick = dealDamage ? damagePerSecond * damageInterval : 0f;
        float resilienceDamagePerTick = dealResilienceDamage ? resilienceDamagePerSecond * damageInterval : 0f;
        
        while (isPulling && enemy != null)
        {
            // 检查敌人是否还在范围内
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > pullRadius)
            {
                if (enableDebug)
                    Debug.Log($"[MiniBlackHole] {enemy.name} 离开伤害范围");
                break;
            }
            
            // 调用TakeDamage方法，传入伤害和韧性伤害
            enemyHealth.TakeDamage((int)damagePerTick, resilienceDamagePerTick);
            
            if (enableDebug)
            {
                if (dealDamage && dealResilienceDamage)
                    Debug.Log($"[MiniBlackHole] 对 {enemy.name} 造成 {damagePerTick} 伤害 和 {resilienceDamagePerTick} 韧性伤害");
                else if (dealDamage)
                    Debug.Log($"[MiniBlackHole] 对 {enemy.name} 造成 {damagePerTick} 伤害");
                else if (dealResilienceDamage)
                    Debug.Log($"[MiniBlackHole] 对 {enemy.name} 造成 {resilienceDamagePerTick} 韧性伤害");
            }
            
            yield return new WaitForSeconds(damageInterval);
        }
        
        // 协程结束时从字典中移除
        if (enemy != null && damageCoroutines.ContainsKey(enemy))
        {
            damageCoroutines.Remove(enemy);
        }
    }
    
    /// <summary>
    /// 延迟后开始吸引敌人
    /// </summary>
    private IEnumerator DelayedStartPulling()
    {
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 等待 {delayBeforePull} 秒后开始吸引");
        
        // 等待延迟时间
        yield return new WaitForSeconds(delayBeforePull);
        
        // 设置动画参数（如果有的话）
        SetAnimationParameter("IsActive", true);
        
        // 开始吸引
        StartPulling();
    }
    
    // ==================== 动画事件接口 ====================
    
    /// <summary>
    /// 开始拉取（在动画中调用）
    /// </summary>
    public void StartPulling()
    {
        if (isPulling) return;
        
        if (pullCoroutine != null)
        {
            StopCoroutine(pullCoroutine);
        }
        
        // 锁定黑洞自己的Rigidbody2D
        LockBlackHoleRigidbody();
        
        // 播放吸引开始音效
        PlaySound(pullStartSound);
        
        // 播放吸引循环音效
        PlayLoopSound(pullLoopSound);
        
        pullCoroutine = StartCoroutine(PullCoroutine());
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 开始拉取敌人");
    }
    
    /// <summary>
    /// 拉取协程
    /// </summary>
    private IEnumerator PullCoroutine()
    {
        isPulling = true;
        
        // 设置动画参数为吸引状态
        SetAnimationParameter("IsDragging", true);
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 拉取协程启动，持续 {pullDuration} 秒");
        
        // 播放黑洞生成音效
        PlaySound(spawnSound, false);
        
        // 播放吸引开始音效
        PlaySound(pullStartSound, false);
        
        // 循环播放吸引循环音效
        PlaySound(pullLoopSound, true);
        
        // 持续吸引指定时间
        yield return new WaitForSeconds(pullDuration);
        
        // 自动结束拉取
        EndPulling();
    }
    
    /// <summary>
    /// 结束拉取（在动画中调用）
    /// </summary>
    public void EndPulling()
    {
        if (!isPulling) return;
        
        if (pullCoroutine != null)
        {
            StopCoroutine(pullCoroutine);
            pullCoroutine = null;
        }
        
        // 停止所有伤害协程
        foreach (var kvp in damageCoroutines)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }
        damageCoroutines.Clear();
        
        // 停止循环音效
        StopLoopSound();
        
        // 解锁黑洞自己的Rigidbody2D
        UnlockBlackHoleRigidbody();
        
        isPulling = false;
        enemiesInRange.Clear();
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 停止拉取敌人");
    }
    
    /// <summary>
    /// 销毁黑洞（在动画中调用）
    /// </summary>
    public void DestroyBlackHole()
    {
        // 播放消失音效
        PlaySound(despawnSound);
        
        // 设置动画参数为消失状态
        SetAnimationTrigger("Deactivate");
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 黑洞销毁");
        
        // 延迟销毁，让音效播放完
        float destroyDelay = despawnSound != null ? despawnSound.length : 0f;
        Destroy(gameObject, destroyDelay);
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
                        Debug.Log($"[MiniBlackHole] 设置动画参数: {paramName} = {value}");
                    
                    return;
                }
            }
            
            if (enableDebug)
                Debug.LogWarning($"[MiniBlackHole] 动画参数 '{paramName}' 不存在或类型不匹配");
        }
    }
    
    /// <summary>
    /// 设置动画触发器
    /// </summary>
    /// <param name="triggerName">触发器名</param>
    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(triggerName);
                    
                    if (enableDebug)
                        Debug.Log($"[MiniBlackHole] 触发动画: {triggerName}");
                    
                    return;
                }
            }
            
            if (enableDebug)
                Debug.LogWarning($"{triggerName} 动画触发器 '{triggerName}' 不存在");
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
    
    void OnDrawGizmosSelected()
    {
        // 绘制吸引范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
        
        // 绘制最大拉取距离
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxPullDistance);
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="loop">是否循环</param>
    private void PlaySound(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null) return;
        
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = soundVolume;
        audioSource.Play();
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 播放音效: {clip.name}，循环: {loop}");
    }
    
    /// <summary>
    /// 停止音效
    /// </summary>
    /// <param name="source">AudioSource组件</param>
    private void StopSound(AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            
            if (enableDebug)
                Debug.Log($"[MiniBlackHole] 停止音效: {source.clip.name}");
        }
    }
    
    /// <summary>
    /// 设置音效组件
    /// </summary>
    private void SetupAudioSources()
    {
        // 主音效源（用于一次性音效）
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = 0f; // 2D音效
        
        // 循环音效源（用于持续音效）
        loopAudioSource = gameObject.AddComponent<AudioSource>();
        loopAudioSource.playOnAwake = false;
        loopAudioSource.volume = soundVolume;
        loopAudioSource.spatialBlend = 0f; // 2D音效
        loopAudioSource.loop = true;
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 音效组件已设置");
    }
    
    /// <summary>
    /// 锁定黑洞自己的Rigidbody2D，防止被反作用力推动
    /// </summary>
    private void LockBlackHoleRigidbody()
    {
        if (blackHoleRigidbody == null) return;
        
        // 锁定黑洞的位置和旋转
        blackHoleRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 锁定黑洞Rigidbody2D (FreezeAll)");
    }
    
    /// <summary>
    /// 解锁黑洞自己的Rigidbody2D，恢复原始约束
    /// </summary>
    private void UnlockBlackHoleRigidbody()
    {
        if (blackHoleRigidbody == null) return;
        
        // 恢复原始约束
        blackHoleRigidbody.constraints = originalBlackHoleConstraints;
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 解锁黑洞Rigidbody2D (恢复为 {originalBlackHoleConstraints})");
    }
    
    // ==================== 音效方法 ====================
    
    /// <summary>
    /// 播放一次性音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        
        audioSource.PlayOneShot(clip, soundVolume);
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 播放音效: {clip.name}");
    }
    
    /// <summary>
    /// 播放循环音效
    /// </summary>
    private void PlayLoopSound(AudioClip clip)
    {
        if (clip == null || loopAudioSource == null) return;
        
        loopAudioSource.clip = clip;
        loopAudioSource.Play();
        
        if (enableDebug)
            Debug.Log($"[MiniBlackHole] 开始播放循环音效: {clip.name}");
    }
    
    /// <summary>
    /// 停止循环音效
    /// </summary>
    private void StopLoopSound()
    {
        if (loopAudioSource == null) return;
        
        loopAudioSource.Stop();
        
        if (enableDebug)
            Debug.Log("[MiniBlackHole] 停止循环音效");
    }
}
