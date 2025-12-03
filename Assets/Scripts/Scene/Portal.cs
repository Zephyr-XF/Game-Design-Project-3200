using UnityEngine;
using System.Collections;

/// <summary>
/// 2D 传送门：
/// 玩家接触触发器后，等待一段时间再传送。
/// （在暂停游戏期间或延迟不自动消失，可防重使用，根据自己需求）
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

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip teleportSound;

    private bool isWaiting = false;   // 正在延迟中的标记
    private bool isInitialized = false; // 防止游戏开始时播放音效

    private void Reset()
    {
        // 确保是 Trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        Debug.Log($"[Teleporter2D] Awake 被调用 - Time.time: {Time.time}", this);
        
        // 强制禁用 AudioSource 的 PlayOnAwake，防止自动播放
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
            Debug.Log($"[Teleporter2D] AudioSource.playOnAwake 已设置为 false，已停止播放", this);
        }
        
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
        
        // 使用协程延迟初始化音效系统
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        Debug.Log($"[Teleporter2D] 初始化协程开始 - Time.time: {Time.time}", this);
        
        // 使用实际时间延迟，确保场景完全加载
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        Debug.Log($"[Teleporter2D] ? 初始化完成，音效系统已启用 - Time.time: {Time.time}", this);
    }

    private void Start()
    {
        // 延迟一帧后才允许播放音效
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        isInitialized = true;
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

        // 如果设置了"延迟期间上锁"，并且当前已经在等待，就不再重复开启协程
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

        Debug.Log($"[Teleporter2D] 传送延迟开始，延迟时间: {teleportDelay} 秒, isInitialized: {isInitialized}, Time.time: {Time.time}", this);

        // 播放传送音效（只有在初始化完成后才播放）
        if (isInitialized && audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
            Debug.Log($"[Teleporter2D] ? 传送音效已播放", this);
        }
        else if (!isInitialized)
        {
            Debug.Log($"[Teleporter2D] ? 初始化未完成，跳过播放音效", this);
        }
        else
        {
            if (audioSource == null)
                Debug.LogWarning($"[Teleporter2D] ? AudioSource 为空", this);
            if (teleportSound == null)
                Debug.LogWarning($"[Teleporter2D] ? teleportSound 为空", this);
        }

        // 如果有延迟，就等待（若 teleportDelay <= 0，立即执行）
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

        // 传送完后解锁，希望只能传一次，或者在这里不做重置，影响逻辑）
        isWaiting = false;
        Debug.Log("[Teleporter2D] 传送完成，解锁状态。", this);
    }
}

