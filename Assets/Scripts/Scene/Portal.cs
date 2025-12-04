using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 2D 传送门：
/// 玩家接触触发器后，等待一段时间再传送。
/// 新增：检测传送门范围内是否有敌人，有敌人时阻止传送
/// 支持：当目标关卡有多个相同 ID 的 SpawnPoint 时，随机选择一个
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Teleporter2D : MonoBehaviour
{
    [Header("关卡控制器")]
    public LevelController2D levelController;

    [Header("传送目标")]
    [Tooltip("是否直接指定关卡索引；否则调用 NextLevel（）")]
    public bool useCustomLevelIndex = false;

    [Tooltip("要传送到的关卡索引（对应 LevelController2D.levelConfigs 的下标）")]
    public int targetLevelIndex = 0;

    [Header("触发配置")]
    [Tooltip("只有带这个 Tag 的对象才会触发（一般是 Player）")]
    public string targetTag = "Player";

    [Header("延迟配置")]
    [Tooltip("传送前等待的时间（秒）")]
    public float teleportDelay = 0f;

    [Tooltip("在延迟期间，是否阻止再次触发（避免多次启动协程）")]
    public bool lockDuringDelay = true;

    [Header("敌人检测")]
    [Tooltip("用于检测敌人的碰撞箱（通常是比传送门更大的范围）")]
    public Collider2D enemyDetectionCollider;

    [Tooltip("敌人的 Layer Mask")]
    public LayerMask enemyLayer;

    [Tooltip("是否启用敌人检测（有敌人时阻止传送）")]
    public bool checkForEnemies = true;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip teleportSound;
    public AudioClip blockedSound;  // 传送被阻止时的音效

    [Header("调试")]
    public bool enableDebug = false;
    
    [Header("提示")]
    [Tooltip("如果目标关卡有多个相同 ID 的 SpawnPoint，将随机选择一个")]
    [TextArea(2, 3)]
    public string randomSpawnNote = "提示：LevelController 会自动从相同 ID 的多个 SpawnPoint 中随机选择一个传送目标。";

    private bool isWaiting = false;
    private bool isInitialized = false;

    private void Reset()
    {
        // 确保是 Trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        // Debug.Log($"[Teleporter2D] Awake 被调用 - Time.time: {Time.time}", this);
        
        // 强制禁用 AudioSource 的 PlayOnAwake，防止自动播放
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
            // if (enableDebug) Debug.Log($"[Teleporter2D] AudioSource.playOnAwake 已设置为 false", this);
        }
        
        // 如果没在 Inspector 手动赋值，就自动在场景里找一个 LevelController2D
        if (levelController == null)
        {
            levelController = FindObjectOfType<LevelController2D>();
            if (levelController == null)
            {
                Debug.LogError("[Teleporter2D] 场景中找不到 LevelController2D，请确认已经有一个物体挂着这个脚本。", this);
            }
            // else if (enableDebug)
            // {
            //     Debug.Log("[Teleporter2D] 自动找到 LevelController2D: " + levelController.name, this);
            // }
        }
        
        // 使用协程延迟初始化音效系统
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        // if (enableDebug) Debug.Log($"[Teleporter2D] 初始化协程开始 - Time.time: {Time.time}", this);
        
        // 使用实际时间延迟，确保场景完全加载
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        // if (enableDebug) Debug.Log($"[Teleporter2D] ? 初始化完成，音效系统已启用 - Time.time: {Time.time}", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (enableDebug) Debug.Log($"[Teleporter2D] OnTriggerEnter2D 被触发，触发对象: {other.name}", this);

        // 只对玩家生效（或你指定的 Tag）
        if (!other.CompareTag(targetTag))
        {
            // if (enableDebug) Debug.Log($"[Teleporter2D] 触发对象的 Tag 不匹配: {other.tag}，需要: {targetTag}", this);
            return;
        }

        if (levelController == null)
        {
            Debug.LogError("[Teleporter2D] levelController 为空，无法传送。", this);
            return;
        }

        // 如果设置了"延迟期间上锁"，并且当前已经在等待，就不再重复开启协程
        if (lockDuringDelay && isWaiting)
        {
            if (enableDebug) Debug.Log("[Teleporter2D] 当前正在等待传送，忽略重复触发。", this);
            return;
        }

        // if (enableDebug) Debug.Log("[Teleporter2D] 开始传送协程。", this);
        // 开始传送协程
        StartCoroutine(TeleportWithDelay());
    }

    private IEnumerator TeleportWithDelay()
    {
        isWaiting = true;

        // if (enableDebug) Debug.Log($"[Teleporter2D] 传送延迟开始，延迟时间: {teleportDelay} 秒, isInitialized: {isInitialized}, Time.time: {Time.time}", this);

        // 检查是否有敌人在检测范围内
        if (checkForEnemies && HasEnemiesInRange())
        {
            if (enableDebug) Debug.Log("[Teleporter2D] ? 检测到敌人，传送被阻止！", this);
            
            // 播放阻止音效
            if (isInitialized && audioSource != null && blockedSound != null)
            {
                audioSource.PlayOneShot(blockedSound);
            }
            
            isWaiting = false;
            yield break; // 中止传送
        }

        // 播放传送音效（只有在初始化完成后才播放）
        if (isInitialized && audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
            // if (enableDebug) Debug.Log($"[Teleporter2D] ? 传送音效已播放", this);
        }
        // else if (!isInitialized && enableDebug)
        // {
        //     Debug.Log($"[Teleporter2D] ? 初始化未完成，跳过播放音效", this);
        // }

        // 如果有延迟，就等待（若 teleportDelay <= 0，立即执行）
        if (teleportDelay > 0f)
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        // 延迟后再次检查是否有敌人（防止延迟期间敌人进入）
        if (checkForEnemies && HasEnemiesInRange())
        {
            if (enableDebug) Debug.Log("[Teleporter2D] ? 延迟期间敌人进入，传送被阻止！", this);
            
            // 播放阻止音效
            if (audioSource != null && blockedSound != null)
            {
                audioSource.PlayOneShot(blockedSound);
            }
            
            isWaiting = false;
            yield break; // 中止传送
        }

        // if (enableDebug) Debug.Log("[Teleporter2D] 延迟结束，开始传送。", this);

        // --- 真正传送 ---
        if (useCustomLevelIndex)
        {
            // if (enableDebug) Debug.Log($"[Teleporter2D] 使用自定义关卡索引传送，目标关卡索引: {targetLevelIndex}", this);
            levelController.LoadLevel(targetLevelIndex);
        }
        else
        {
            // if (enableDebug) Debug.Log("[Teleporter2D] 使用 NextLevel 传送到下一个关卡。", this);
            levelController.NextLevel();
        }

        isWaiting = false;
        // if (enableDebug) Debug.Log("[Teleporter2D] 传送完成，解锁状态。", this);
    }

    /// <summary>
    /// 检测指定碰撞箱范围内是否有敌人
    /// </summary>
    private bool HasEnemiesInRange()
    {
        if (enemyDetectionCollider == null)
        {
            if (enableDebug) Debug.LogWarning("[Teleporter2D] enemyDetectionCollider 未设置，跳过敌人检测。", this);
            return false;
        }

        if (enemyLayer == 0)
        {
            if (enableDebug) Debug.LogWarning("[Teleporter2D] enemyLayer 未设置，跳过敌人检测。", this);
            return false;
        }

        // 使用 OverlapCollider 检测范围内的敌人
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;

        List<Collider2D> enemies = new List<Collider2D>();
        int enemyCount = enemyDetectionCollider.OverlapCollider(filter, enemies);

        if (enableDebug && enemyCount > 0)
        {
            Debug.Log($"[Teleporter2D] 检测到 {enemyCount} 个敌人在传送范围内", this);
            foreach (var enemy in enemies)
            {
                Debug.Log($"  - 敌人: {enemy.gameObject.name}", this);
            }
        }

        return enemyCount > 0;
    }

    private void OnDrawGizmosSelected()
    {
        // 在 Scene 视图中可视化敌人检测范围
        if (enemyDetectionCollider != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 橙色半透明
            
            if (enemyDetectionCollider is CircleCollider2D)
            {
                CircleCollider2D circle = enemyDetectionCollider as CircleCollider2D;
                Vector3 center = enemyDetectionCollider.transform.position + (Vector3)circle.offset;
                float radius = circle.radius * enemyDetectionCollider.transform.lossyScale.x;
                
                Gizmos.DrawSphere(center, radius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(center, radius);
            }
            else if (enemyDetectionCollider is BoxCollider2D)
            {
                BoxCollider2D box = enemyDetectionCollider as BoxCollider2D;
                Vector3 center = enemyDetectionCollider.transform.position + (Vector3)box.offset;
                Vector3 size = box.size;
                size.x *= enemyDetectionCollider.transform.lossyScale.x;
                size.y *= enemyDetectionCollider.transform.lossyScale.y;
                
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
        }

        // 绘制传送门中心点
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
}

