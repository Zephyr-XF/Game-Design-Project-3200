using Cinemachine;
using System.Linq;
using UnityEngine;
using System.Collections; // 为协程支持
using TMPro;

public class LevelController2D : MonoBehaviour
{
    [Header("关卡配置")]
    public LevelConfig2D[] levelConfigs;
    public int currentIndex = 0;

    [Header("玩家")]
    public GameObject player;           // 玩家 Transform
    public Rigidbody2D playerRb2D;     // 玩家刚体（如果有）

    //[Header("祝福系统")]
    //public GameObject BlessingSystem;

    [Header("Cinemachine 2D")]
    public CinemachineVirtualCamera vcam;
    public bool snapCameraOnLevelLoad = true;

    [Header("摄像机限制")]
    //public Collider2D cameraBoundsCollider;

    [Header("加载动画")]
    public int delayTime = 6;
    public Animator transitionAnimator;

    [Header("加载等待UI")]
    [Tooltip("传送/加载等待时显示的 UI（例如遮罩、Loading 提示）。仅在等待计时中启用，结束后自动关闭")]
    public GameObject loadingUI;
    [Tooltip("显示 Loading 动态文字的 TextMeshPro 组件")]
    public TMP_Text loadingText; // 不再关联生命值

    // Loading 动态省略号状态（只在协程内部使用）
    private float loadingDotInterval = 0.5f; // 点更新间隔

    public SpawnPoint2D[] allSpawnPoints;

    private void Start()
    {
        Time.timeScale = 1f;
        if (loadingUI != null) loadingUI.SetActive(false); // 初始关闭
        if (loadingText != null) loadingText.text = ""; // 初始清空
        Debug.Log($"[LevelController2D] Start() 被调用，当前关卡索引: {currentIndex}");
        LoadLevel(currentIndex);
    }

    public void NextLevel()
    {
        currentIndex++;
        Debug.Log($"[LevelController2D] NextLevel() 被调用，切换到关卡索引: {currentIndex}");

        if (currentIndex >= levelConfigs.Length)
        {
            Debug.Log("[LevelController2D] 所有关卡结束");
            return;
        }
        
        LoadLevel(currentIndex);
    }

    public void LoadLevel(int index)
    {
        
        Debug.Log($"[LevelController2D] LoadLevel() 被调用，加载关卡索引: {index}");

        if (index < 0 || index >= levelConfigs.Length)
        {
            Debug.LogError($"[LevelController2D] Level index 超出范围: {index}");
            return;
        }
        // 启动协程执行加载流程（包含祝福系统）
        
        StartCoroutine(LoadLevelSequence(index));


        
    }

    /// <summary>
    /// 加载关卡序列，包含祝福触发
    /// </summary>
    

    private IEnumerator LoadLevelSequence(int index )
    {
        yield return TransitionTimer(); // 显示 Loading 动效

        LevelConfig2D config = levelConfigs[index];
        Debug.Log($"[LevelController2D] 加载关卡: {config.levelName}, levelID: {config.levelID}");

        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
            Debug.Log($"[LevelController2D] 自动查找 SpawnPoint2D，共找到 {allSpawnPoints.Length} 个");
            if (allSpawnPoints.Length > 0)
            {
                Debug.Log("[LevelController2D] SpawnPoint 列表: " + string.Join(", ", allSpawnPoints.Select(s => s.spawnId + ":" + s.name)));
            }
        }

        if (allSpawnPoints.Length > 0)
        {
            TeleportPlayer2D(config, config.levelID);
        }
        else
        {
            Debug.LogWarning("[LevelController2D] 场景中没有 SpawnPoint2D，跳过玩家传送。请添加一个 SpawnPoint2D。");
        }

        if (snapCameraOnLevelLoad && player != null)
        {
            SnapCinemachine2DToPlayer();
        }

        BlessingManager.Instance.TriggerBlessing();




        Debug.Log($"[LevelController2D] 加载关卡完成: {config.levelName}");
    }

    private void TeleportPlayer2D(LevelConfig2D config,int spawnId)
    {
        if (player == null)
        {
            Debug.LogError("[LevelController2D] Player 引用为空，无法传送玩家。");
            return;
        }
        Debug.Log($"[LevelController2D] TeleportPlayer2D -> spawnId {spawnId}");
        TeleportPlayerToSpawnId(spawnId);
        Debug.Log($"[LevelController2D] TeleportPlayer2D() 完成: {config.levelName}");
    }

    public void TeleportPlayerToSpawnId(int spawnId)
    {
        Debug.Log($"[LevelController2D] TeleportPlayerToSpawnId() 开始，目标 spawnId: {spawnId}");

        if (player == null)
        {
            Debug.LogError("[LevelController2D] Player 为空");
            return;
        }

        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
            Debug.Log($"[LevelController2D] (传送时) 自动查找 SpawnPoint2D，共找到 {allSpawnPoints.Length} 个");
            if (allSpawnPoints.Length == 0)
            {
                Debug.LogError("[LevelController2D] 场景中没有任何 SpawnPoint2D，无法传送玩家");
                return;
            }
            Debug.Log("[LevelController2D] SpawnPoint 列表: " + string.Join(", ", allSpawnPoints.Select(s => s.spawnId + ":" + s.name)));
        }

        SpawnPoint2D sp = allSpawnPoints.FirstOrDefault(p => p.spawnId == spawnId);
        if (sp == null)
        {
            Debug.LogError($"[LevelController2D] 找不到 spawnId = {spawnId} 的 SpawnPoint2D。可用: {string.Join(", ", allSpawnPoints.Select(p => p.spawnId))}");
            return;
        }

        Debug.Log($"[LevelController2D] 匹配到重生点对象 {sp.name} (spawnId={sp.spawnId}) useTransformPosition={sp.useTransformPosition}");

        if (sp.resetPlayerState)
        {
            Debug.Log("[LevelController2D] 重生点要求清理玩家状态");
            ClearPlayerState(player);
        }

        Vector3 targetPos = sp.GetWorldPosition();
        Debug.Log($"[LevelController2D] 传送前 玩家: {player.transform.position} -> 目标: {targetPos}");
        player.transform.position = targetPos;
        Debug.Log($"[LevelController2D] 传送后 玩家位置: {player.transform.position}");

        if (sp.snapCameraToPlayer && snapCameraOnLevelLoad)
        {
            Debug.Log("[LevelController2D] 重生点要求相机对齐");
            SnapCinemachine2DToPlayer();
        }

        Debug.Log($"[LevelController2D] TeleportPlayerToSpawnId() 完成，spawnId: {spawnId}");
    }

    private void SnapCinemachine2DToPlayer()
    {
        if (vcam == null || player == null)
        {
            Debug.LogWarning("[LevelController2D] SnapCinemachine2DToPlayer 被跳过，vcam 或 player 为空");
            return;
        }

        if (vcam.Follow != player)
        {
            Debug.Log("[LevelController2D] 相机 Follow 目标修正为玩家");
            vcam.Follow = player.transform;
        }

        var camTr = vcam.transform;
        Vector3 before = camTr.position;
        camTr.position = new Vector3(
            player.transform.position.x,
            player.transform.position.y,
            camTr.position.z
        );
        Debug.Log($"[LevelController2D] 相机位置同步: {before} -> {camTr.position}");

        var brain = CinemachineCore.Instance.GetActiveBrain(0);
        if (brain != null)
        {
            brain.ManualUpdate();
            Debug.Log("[LevelController2D] Cinemachine Brain 强制更新");
        }
    }

    public void ClearPlayerState(GameObject player)
    {
        Debug.Log("[LevelController2D] ClearPlayerState() 开始");

        if (player == null)
        {
            Debug.LogWarning("[LevelController2D] ClearPlayerState() 取消，player 为空");
            return;
        }

        if (playerRb2D != null)
        {
            playerRb2D.velocity = Vector2.zero;
            playerRb2D.angularVelocity = 0f;
            Debug.Log("[LevelController2D] 刚体速度 / 角速度 已清零");
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth();
            Debug.Log("[LevelController2D] PlayerHealth 已重置为最大");
        }

        Debug.Log("[LevelController2D] ClearPlayerState() 结束");
    }

    // 加载动画协程（内部处理 Loading 文字）
    public IEnumerator TransitionTimer()
    {
        Debug.Log("[LevelController2D] TransitionTimer() 开始");
        if (loadingUI != null) loadingUI.SetActive(true);
        if (transitionAnimator != null) transitionAnimator.SetBool("IsTransition", true);

        float elapsed = 0f;
        float nextDotTime = 0f;
        int dotCount = 0; // 局部点数量

        while (elapsed < delayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= nextDotTime)
            {
                nextDotTime += loadingDotInterval;
                dotCount = (dotCount + 1) % 4; // 0-3 循环
                if (loadingText != null)
                {
                    loadingText.text = "Loading" + new string('.', dotCount);
                }
            }
            yield return null;
        }

        if (transitionAnimator != null) transitionAnimator.SetBool("IsTransition", false);
        if (loadingUI != null) loadingUI.SetActive(false);
        if (loadingText != null) loadingText.text = ""; // 收尾清空或可改成 "Done"
        Debug.Log("[LevelController2D] TransitionTimer() 结束");
    }

    public void TeleportPlayerWithDelay(int spawnId)
    {
        Debug.Log($"[LevelController2D] TeleportPlayerWithDelay() 调用，spawnId={spawnId}");
        StartCoroutine(TeleportDelaySequence(spawnId));
    }

    private IEnumerator TeleportDelaySequence(int spawnId)
    {
        Debug.Log("[LevelController2D] TeleportDelaySequence 协程开始");
        yield return TransitionTimer();
        Debug.Log("[LevelController2D] TransitionTimer 完成，开始实际传送");
        TeleportPlayerToSpawnId(spawnId);
        Debug.Log("[LevelController2D] TeleportDelaySequence 协程结束");
    }
}