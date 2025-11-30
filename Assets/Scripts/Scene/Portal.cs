using UnityEngine;
using System.Collections;

/// <summary>
/// 2D 传送门：
/// 玩家进入触发区域后，等待一定时间再传送。
/// 不暂停游戏，传送门不自动消失，可反复使用（除非你自己限制）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Teleporter2D : MonoBehaviour
{
    [Header("关卡控制器")]
    public LevelController2D levelController;

    [Header("传送目标")]
    [Tooltip("是否直接指定关卡索引（勾上则不走 NextLevel）")]
    public bool useCustomLevelIndex = false;

    [Tooltip("要传送到的关卡索引（对应 LevelController2D.levelConfigs 的下标）")]
    public int targetLevelIndex = 0;

    [Header("触发过滤")]
    [Tooltip("只有带这个 Tag 的对象进入才会触发，一般是 Player")]
    public string targetTag = "Player";

    [Header("延迟设置")]
    [Tooltip("传送前等待的时间（秒）")]
    public float teleportDelay = 0f;

    [Tooltip("在延迟期间，是否禁止再次触发（防止多次叠加协程）")]
    public bool lockDuringDelay = true;

    private bool isWaiting = false;   // 正在延迟中的标记

    private void Reset()
    {
        // 确保是 Trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        // 如果没在 Inspector 手动赋值，就自动在场景里找一个 LevelController2D
        if (levelController == null)
        {
            levelController = FindObjectOfType<LevelController2D>();
            if (levelController == null)
            {
                Debug.LogError("[Teleporter2D] 场景中找不到 LevelController2D，请确认已经有一个物体挂着这个脚本。", this);
            }
            else
            {
                Debug.Log("[Teleporter2D] 自动找到 LevelController2D: " + levelController.name, this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Teleporter2D] OnTriggerEnter2D 被触发，触发对象: {other.name}", this);

        // 只对玩家生效（或你指定的 Tag）
        if (!other.CompareTag(targetTag))
        {
            Debug.Log($"[Teleporter2D] 触发对象的 Tag 不匹配: {other.tag}，需要: {targetTag}", this);
            return;
        }

        if (levelController == null)
        {
            Debug.LogError("[Teleporter2D] levelController 为空，无法传送。", this);
            return;
        }

        // 如果设置了“延迟期间上锁”，并且当前已经在等待，就不再重复开启协程
        if (lockDuringDelay && isWaiting)
        {
            Debug.Log("[Teleporter2D] 当前正在等待传送，忽略重复触发。", this);
            return;
        }

        Debug.Log("[Teleporter2D] 开始传送协程。", this);
        // 开始传送协程
        StartCoroutine(TeleportWithDelay());
    }

    private IEnumerator TeleportWithDelay()
    {
        isWaiting = true;

        Debug.Log($"[Teleporter2D] 传送延迟开始，延迟时间: {teleportDelay} 秒", this);

        // 如果有延迟，就等待；如果 teleportDelay <= 0，则立刻执行
        if (teleportDelay > 0f)
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        Debug.Log("[Teleporter2D] 延迟结束，开始传送。", this);

        // --- 真正传送 ---
        if (useCustomLevelIndex)
        {
            Debug.Log($"[Teleporter2D] 使用自定义关卡索引传送，目标关卡索引: {targetLevelIndex}", this);
            levelController.LoadLevel(targetLevelIndex);
        }
        else
        {
            Debug.Log("[Teleporter2D] 使用 NextLevel 传送到下一个关卡。", this);
            levelController.NextLevel();
        }

        // 传送完解锁（如果你希望每次只用一次，可以在这里不解锁，或加别的逻辑）
        isWaiting = false;
        Debug.Log("[Teleporter2D] 传送完成，解锁状态。", this);
    }
}