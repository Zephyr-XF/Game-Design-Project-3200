using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speeder : MonoBehaviour
{
    [Header("速度设置")]
    [Range(1f, 3f)]
    public float speedMultiplier = 1.5f;     // 进入加速区时的速度倍率（1.5 = 变为原来1.5倍)
    public bool affectPlayer = true;
    public bool affectEnemy = true;

    [Header("识别方式")]
    public LayerMask playerLayer;          // 玩家 Layer
    public LayerMask enemyLayer;           // 敌人 Layer

    [Header("调试")]
    public bool enableDebug = true;

    // 记录对象进入前的原始速度，以便离开区域后也能正确恢复
    private Dictionary<GameObject, float> originalSpeedDict = new Dictionary<GameObject, float>();

    private void Reset()
    {
        // 确保加速区 Collider2D 设为 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // 玩家
        if (affectPlayer && IsInLayerMask(otherLayer, playerLayer))
        {
            ApplySpeedBoostToPlayer(other.gameObject);
        }

        // 敌人
        if (affectEnemy && IsInLayerMask(otherLayer, enemyLayer))
        {
            ApplySpeedBoostToEnemy(other.gameObject);
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

    // --- 加速模块：玩家加速 ---

    void ApplySpeedBoostToPlayer(GameObject obj)
    {
        // 假设通过 PlayerMovement 组件，使用 StatsManager.Instance.speed
        var playerMovement = obj.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"SpeedBoostZone: {obj.name} 没有找到 PlayerMovement 组件，无法加速。");
            return;
        }

        if (!originalSpeedDict.ContainsKey(obj))
        {
            // 玩家速度存储在 StatsManager 中
            if (StatsManager.Instance != null)
            {
                float originalSpeed = StatsManager.Instance.speed;
                originalSpeedDict.Add(obj, originalSpeed);

                int newSpeed = Mathf.RoundToInt(originalSpeed * speedMultiplier);
                StatsManager.Instance.speed = newSpeed;

                if (enableDebug)
                    Debug.Log($"SpeedBoostZone: 玩家 {obj.name} 进入加速区，速度 {originalSpeed} -> {newSpeed}");
            }
            else if (enableDebug)
            {
                Debug.LogError("SpeedBoostZone: StatsManager.Instance 为空，无法修改玩家速度！");
            }
        }
    }

    void ApplySpeedBoostToEnemy(GameObject obj)
    {
        // 敌人通过 EnemyMovement 组件，使用本地的 speed 字段
        var enemyMovement = obj.GetComponent<EnemyMovement>();

        if (enemyMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"SpeedBoostZone: {obj.name} 没有找到 EnemyMovement 组件，无法加速。");
            return;
        }

        if (!originalSpeedDict.ContainsKey(obj))
        {
            float originalSpeed = enemyMovement.speed;
            originalSpeedDict.Add(obj, originalSpeed);

            float newSpeed = originalSpeed * speedMultiplier;
            enemyMovement.speed = newSpeed;

            if (enableDebug)
                Debug.Log($"SpeedBoostZone: 敌人 {obj.name} 进入加速区，速度 {originalSpeed} -> {newSpeed}");
        }
    }

    // 离开加速区，恢复速度
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
                Debug.Log($"SpeedBoostZone: 玩家 {obj.name} 离开加速区，速度恢复为 {originalSpeed}");
        }

        // 尝试恢复敌人速度（通过 EnemyMovement）
        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.speed = originalSpeed;
            
            if (enableDebug)
                Debug.Log($"SpeedBoostZone: 敌人 {obj.name} 离开加速区，速度恢复为 {originalSpeed}");
        }

        originalSpeedDict.Remove(obj);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}
