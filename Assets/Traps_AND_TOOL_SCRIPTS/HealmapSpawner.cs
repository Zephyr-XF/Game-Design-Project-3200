using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealmapSpawner : MonoBehaviour
{
    [Header("回血法阵预制体")]
    [Tooltip("回血法阵的预制体")]
    public GameObject healmapPrefab;
    
    [Header("召唤设置")]
    [Tooltip("法阵存在时间（秒），0表示永久存在")]
    [Range(0f, 60f)]
    public float duration = 10f;
    
    [Tooltip("召唤位置偏移（相对于道具位置）")]
    public Vector3 spawnOffset = Vector3.zero;
    
    [Tooltip("是否在召唤后销毁道具本身")]
    public bool destroyAfterSpawn = true;
    
    [Tooltip("道具销毁延迟时间（秒）")]
    public float destroyDelay = 0.1f;
    
    [Header("召唤效果")]
    [Tooltip("召唤时播放的音效")]
    public AudioClip spawnSound;
    
    [Tooltip("召唤特效预制体（可选）")]
    public GameObject spawnEffectPrefab;
    
    [Tooltip("特效持续时间（秒）")]
    public float effectDuration = 1f;
    
    [Header("触发设置")]
    [Tooltip("触发召唤的方式")]
    public SpawnTriggerType triggerType = SpawnTriggerType.OnCollision;
    
    [Tooltip("延迟召唤时间（秒）")]
    public float spawnDelay = 0f;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private bool hasSpawned = false;
    private GameObject spawnedHealmap = null;
    
    /// <summary>
    /// 触发召唤的方式
    /// </summary>
    public enum SpawnTriggerType
    {
        OnCollision,    // 碰撞时召唤
        OnTrigger,      // 进入触发器时召唤
        OnStart,        // 游戏开始时召唤
        Manual          // 手动调用
    }

    void Start()
    {
        // 验证设置
        if (healmapPrefab == null)
        {
            Debug.LogError("[HealmapSpawner] 回血法阵预制体未设置！");
            enabled = false;
            return;
        }
        
        // 如果设置为开始时召唤
        if (triggerType == SpawnTriggerType.OnStart)
        {
            if (spawnDelay > 0)
            {
                StartCoroutine(DelayedSpawn());
            }
            else
            {
                SpawnHealmap();
            }
        }
        
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 已初始化 - 触发方式: {triggerType}");
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggerType != SpawnTriggerType.OnCollision || hasSpawned)
            return;
        
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 碰撞检测: {collision.gameObject.name}");
        
        if (spawnDelay > 0)
        {
            StartCoroutine(DelayedSpawn());
        }
        else
        {
            SpawnHealmap();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerType != SpawnTriggerType.OnTrigger || hasSpawned)
            return;
        
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 触发器检测: {other.gameObject.name}");
        
        if (spawnDelay > 0)
        {
            StartCoroutine(DelayedSpawn());
        }
        else
        {
            SpawnHealmap();
        }
    }
    
    /// <summary>
    /// 延迟召唤
    /// </summary>
    private IEnumerator DelayedSpawn()
    {
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 延迟 {spawnDelay} 秒后召唤");
        
        yield return new WaitForSeconds(spawnDelay);
        SpawnHealmap();
    }
    
    /// <summary>
    /// 召唤回血法阵（公共方法，可手动调用）
    /// </summary>
    public void SpawnHealmap()
    {
        if (hasSpawned)
        {
            if (enableDebug)
                Debug.LogWarning("[HealmapSpawner] 已经召唤过法阵，跳过");
            return;
        }
        
        if (healmapPrefab == null)
        {
            Debug.LogError("[HealmapSpawner] 回血法阵预制体未设置！");
            return;
        }
        
        hasSpawned = true;
        
        // 计算召唤位置
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        // 实例化回血法阵
        spawnedHealmap = Instantiate(healmapPrefab, spawnPosition, Quaternion.identity);
        
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 召唤回血法阵于: {spawnPosition}");
        
        // 播放召唤音效
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, spawnPosition);
        }
        
        // 播放召唤特效
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // 如果设置了持续时间，启动销毁计时
        if (duration > 0)
        {
            StartCoroutine(DestroyHealmapAfterDuration());
        }
        
        // 如果设置了销毁道具本身
        if (destroyAfterSpawn)
        {
            if (destroyDelay > 0)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }
            
            if (enableDebug)
                Debug.Log($"[HealmapSpawner] 道具将在 {destroyDelay} 秒后销毁");
        }
    }
    
    /// <summary>
    /// 在持续时间后销毁回血法阵
    /// </summary>
    private IEnumerator DestroyHealmapAfterDuration()
    {
        if (enableDebug)
            Debug.Log($"[HealmapSpawner] 回血法阵将在 {duration} 秒后消失");
        
        yield return new WaitForSeconds(duration);
        
        if (spawnedHealmap != null)
        {
            // 可以在这里添加消失特效
            if (enableDebug)
                Debug.Log("[HealmapSpawner] 销毁回血法阵");
            
            Destroy(spawnedHealmap);
        }
    }
    
    /// <summary>
    /// 手动召唤（外部调用）
    /// </summary>
    public void ManualSpawn()
    {
        if (triggerType == SpawnTriggerType.Manual)
        {
            SpawnHealmap();
        }
        else if (enableDebug)
        {
            Debug.LogWarning("[HealmapSpawner] 触发类型不是 Manual，无法手动召唤");
        }
    }
    
    /// <summary>
    /// 取消召唤（提前销毁法阵）
    /// </summary>
    public void CancelHealmap()
    {
        if (spawnedHealmap != null)
        {
            if (enableDebug)
                Debug.Log("[HealmapSpawner] 手动取消回血法阵");
            
            Destroy(spawnedHealmap);
            spawnedHealmap = null;
        }
    }
    
    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public float GetRemainingTime()
    {
        // 这需要额外的计时逻辑，这里返回预设的持续时间
        return duration;
    }
    
    void OnDrawGizmos()
    {
        // 在Scene视图中绘制召唤位置
        Gizmos.color = Color.green;
        Vector3 spawnPos = transform.position + spawnOffset;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        
        // 绘制从道具到召唤位置的连线
        if (spawnOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, spawnPos);
        }
    }
}
