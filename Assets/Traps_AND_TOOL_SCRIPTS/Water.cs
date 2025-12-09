using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSlowZone : MonoBehaviour
{
    [Header("减速设置")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;     // 踩在水里时的速度倍率（0.5 = 降为原来一半）
    public bool affectPlayer = true;
    public bool affectEnemy = true;

    [Header("识别方式")]
    public LayerMask playerLayer;          // 玩家 Layer
    public LayerMask enemyLayer;           // 敌人 Layer

    [Header("调试")]
    public bool enableDebug = true;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip enterWaterSound;

    // 记录被减速前的原始速度（一个对象进出多次也能正确恢复）
    private Dictionary<GameObject, float> originalSpeedDict = new Dictionary<GameObject, float>();
    
    // 防止游戏开始时播放音效
    private bool isInitialized = false;

    private void Reset()
    {
        // 建议把水的 Collider2D 设为 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (enableDebug)
            Debug.Log($"[WaterSlowZone] Awake 被调用 - Time.time: {Time.time}");
        
        // 使用 Awake + 协程，更早地设置初始化标志
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        if (enableDebug)
            Debug.Log($"[WaterSlowZone] 初始化协程开始 - Time.time: {Time.time}");
        
        // 使用实际时间延迟，确保场景完全加载（更可靠）
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[WaterSlowZone] ? 初始化完成，音效系统已启用 - Time.time: {Time.time}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebug)
            Debug.Log($"[WaterSlowZone] OnTriggerEnter2D - 对象: {other.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}, Time.time: {Time.time}");
        
        int otherLayer = other.gameObject.layer;

        // 玩家
        if (affectPlayer && IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug)
                Debug.Log($"[WaterSlowZone] 检测到玩家进入 - 对象: {other.name}");
            ApplySlowToPlayer(other.gameObject);
        }

        // 敌人
        if (affectEnemy && IsInLayerMask(otherLayer, enemyLayer))
        {
            if (enableDebug)
                Debug.Log($"[WaterSlowZone] 检测到敌人进入 - 对象: {other.name}");
            ApplySlowToEnemy(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // 玩家
        if (affectPlayer && IsInLayerMask(otherLayer, playerLayer))
        {
            RestoreSpeed(other.gameObject);
        }

        // 敌人
        if (affectEnemy && IsInLayerMask(otherLayer, enemyLayer))
        {
            RestoreSpeed(other.gameObject);
        }
    }

    // --- 核心：给玩家减速 ---

    void ApplySlowToPlayer(GameObject obj)
    {
        // 玩家通过 PlayerMovement 组件，使用 StatsManager.Instance.speed
        var playerMovement = obj.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"WaterSlowZone: {obj.name} 没有找到 PlayerMovement 组件，无法减速。");
            return;
        }

        if (!originalSpeedDict.ContainsKey(obj))
        {
            // 玩家速度存储在 StatsManager 中
            if (StatsManager.Instance != null)
            {
                float originalSpeed = StatsManager.Instance.speed;
                originalSpeedDict.Add(obj, originalSpeed);

                int newSpeed = Mathf.RoundToInt(originalSpeed * slowMultiplier);
                StatsManager.Instance.speed = newSpeed;

                if (enableDebug)
                    Debug.Log($"WaterSlowZone: 玩家 {obj.name} 进入水中，速度 {originalSpeed} -> {newSpeed}");
            }
            else if (enableDebug)
            {
                Debug.LogError("WaterSlowZone: StatsManager.Instance 为空，无法修改玩家速度！");
            }
        }

        // 播放音效
        PlayEnterWaterSound(obj);
    }

    void ApplySlowToEnemy(GameObject obj)
    {
        // 敌人通过 EnemyMovement 组件，使用本地的 speed 字段
        var enemyMovement = obj.GetComponent<EnemyMovement>();

        if (enemyMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"WaterSlowZone: {obj.name} 没有找到 EnemyMovement 组件，无法减速。");
            return;
        }

        if (!originalSpeedDict.ContainsKey(obj))
        {
            float originalSpeed = enemyMovement.speed;
            originalSpeedDict.Add(obj, originalSpeed);

            float newSpeed = originalSpeed * slowMultiplier;
            enemyMovement.speed = newSpeed;

            if (enableDebug)
                Debug.Log($"WaterSlowZone: 敌人 {obj.name} 进入水中，速度 {originalSpeed} -> {newSpeed}");
        }

        // 播放音效
        PlayEnterWaterSound(obj);
    }

    // 离开水，恢复速度
    void RestoreSpeed(GameObject obj)
    {
        if (!originalSpeedDict.TryGetValue(obj, out float originalSpeed))
            return;

        // 尝试恢复玩家速度（通过 StatsManager）
        var playerMovement = obj.GetComponent<PlayerMovement>();
        if (playerMovement != null && StatsManager.Instance != null)
        {
            StatsManager.Instance.speed = Mathf.RoundToInt(originalSpeed);
            
            if (enableDebug)
                Debug.Log($"WaterSlowZone: 玩家 {obj.name} 离开水，速度恢复为 {originalSpeed}");
        }

        // 尝试恢复敌人速度（通过 EnemyMovement）
        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.speed = originalSpeed;
            
            if (enableDebug)
                Debug.Log($"WaterSlowZone: 敌人 {obj.name} 离开水，速度恢复为 {originalSpeed}");
        }

        originalSpeedDict.Remove(obj);
    }

    private void PlayEnterWaterSound(GameObject obj)
    {
        if (enableDebug)
        {
            Debug.Log($"[PlayEnterWaterSound] 被调用 - 对象: {obj.name}, isInitialized: {isInitialized}, Time.time: {Time.time}");
        }
        
        // 只有在初始化完成后才播放音效
        if (!isInitialized)
        {
            if (enableDebug)
                Debug.Log($"[PlayEnterWaterSound] ? 初始化未完成，跳过播放音效 - 对象: {obj.name}");
            return;
        }
        
        if (enableDebug)
            Debug.Log($"[PlayEnterWaterSound] ? 初始化已完成，检查音效组件...");
            
        if (audioSource != null && enterWaterSound != null)
        {
            audioSource.PlayOneShot(enterWaterSound);
            if (enableDebug)
                Debug.Log($"[PlayEnterWaterSound] ? 音效已播放 - 对象: {obj.name}, AudioClip: {enterWaterSound.name}");
        }
        else
        {
            if (enableDebug)
            {
                if (audioSource == null)
                    Debug.LogWarning($"[PlayEnterWaterSound] ? AudioSource 为空 - 对象: {obj.name}");
                if (enterWaterSound == null)
                    Debug.LogWarning($"[PlayEnterWaterSound] ? enterWaterSound 为空 - 对象: {obj.name}");
            }
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}