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

    [Header("死亡UI")]
    [Tooltip("玩家死亡时显示的 UI（例如游戏结束画面、重新开始按钮等）")]
    public GameObject deathUI;

    [Header("游戏光标")]
    [Tooltip("自定义光标对象，加载关卡时启用")]
    public GameObject gameCursor;

    // Loading 动态省略号状态（只在协程内部使用）
    private float loadingDotInterval = 0.5f; // 点更新间隔

    public SpawnPoint2D[] allSpawnPoints;

    // 标记玩家是否已经死亡（避免重复触发死亡UI）
    private bool playerHasDied = false;

    private void Start()
    {
        Time.timeScale = 1f;
        if (loadingUI != null) loadingUI.SetActive(false); // 初始关闭
        if (loadingText != null) loadingText.text = ""; // 初始清空
        if (deathUI != null) deathUI.SetActive(false); // 初始关闭死亡UI
        if (gameCursor != null) gameCursor.SetActive(false); // 初始关闭游戏光标
        playerHasDied = false; // 初始化死亡标记

        // 订阅玩家死亡事件
        PlayerHealth.OnPlayerDied += OnPlayerDeath;
        Debug.Log("[LevelController2D] 已订阅 PlayerHealth.OnPlayerDied 事件");

        Debug.Log($"[LevelController2D] Start() 被调用，当前关卡索引: {currentIndex}");
        LoadLevel(currentIndex);
    }

    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        PlayerHealth.OnPlayerDied -= OnPlayerDeath;
        Debug.Log("[LevelController2D] 已取消订阅 PlayerHealth.OnPlayerDied 事件");
    }

    public void NextLevel()
    {
        currentIndex++;
        Debug.Log($"[LevelController2D] NextLevel() 被调用，切换到关卡索引: {currentIndex}");

        if (currentIndex >= levelConfigs.Length)
        {
            Debug.Log("[LevelController2D] 所有关卡结束");
            
            // 禁用游戏光标
            if (gameCursor != null)
            {
                gameCursor.SetActive(false);
                Debug.Log("[LevelController2D] 所有关卡结束，游戏光标已禁用");
            }
            
            return;
        }
        
        // 禁用当前关卡的光标，准备加载下一关
        if (gameCursor != null)
        {
            gameCursor.SetActive(false);
            Debug.Log("[LevelController2D] 准备切换关卡，游戏光标已临时禁用");
        }
        
        LoadLevel(currentIndex);
    }

    /// <summary>
    /// 从第一关重新开始游戏
    /// </summary>
    public void RestartFromFirstLevel()
    {
        Debug.Log("[LevelController2D] RestartFromFirstLevel() 被调用，重置到第一关");
        
        // 立即隐藏死亡UI（优先级最高）
        if (deathUI != null) 
        {
            Debug.Log($"[LevelController2D] 死亡UI状态检查 - 当前activeSelf: {deathUI.activeSelf}, activeInHierarchy: {deathUI.activeInHierarchy}");
            Debug.Log($"[LevelController2D] 死亡UI名称: {deathUI.name}, 完整路径: {GetGameObjectPath(deathUI)}");
            
            deathUI.SetActive(false);
            
            Debug.Log($"[LevelController2D] 执行 SetActive(false) 后 - activeSelf: {deathUI.activeSelf}, activeInHierarchy: {deathUI.activeInHierarchy}");
            
            if (deathUI.activeSelf || deathUI.activeInHierarchy)
            {
                Debug.LogError("[LevelController2D] ? 警告：死亡UI在SetActive(false)后仍然是激活状态！");
                Debug.LogError($"[LevelController2D] 检查父对象: {(deathUI.transform.parent != null ? deathUI.transform.parent.name : "无父对象")}");
            }
            else
            {
                Debug.Log("[LevelController2D] ? 死亡UI已成功关闭");
            }
        }
        else
        {
            Debug.LogError("[LevelController2D] ? deathUI 为 null！请在Inspector中检查是否正确分配！");
        }
        
        // 重置游戏状态
        currentIndex = 0;
        Time.timeScale = 1f; // 确保时间恢复正常
        playerHasDied = false; // 重置死亡标记
        
        // 重新激活玩家对象（如果被禁用）
        if (player != null && !player.activeSelf)
        {
            player.SetActive(true);
            Debug.Log("[LevelController2D] 玩家对象已重新激活");
            
            // 激活玩家后，再次确保死亡UI保持关闭
            if (deathUI != null && deathUI.activeSelf)
            {
                deathUI.SetActive(false);
                Debug.LogWarning("[LevelController2D] 玩家激活后检测到死亡UI被意外激活，已强制关闭");
            }
        }
        
        LoadLevel(currentIndex);
    }

    // 辅助方法：获取GameObject的完整层级路径
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    /// <summary>
    /// 玩家死亡时调用此函数（由 PlayerHealth 事件触发）
    /// </summary>
    public void OnPlayerDeath()
    {
        Debug.Log("[LevelController2D] ========================================");
        Debug.Log("[LevelController2D] OnPlayerDeath() 被调用 - 接收到玩家死亡事件！");
        
        // 防止重复触发 - 已注释
        // if (playerHasDied)
        // {
        //     Debug.LogWarning("[LevelController2D] OnPlayerDeath() 已经处理过，跳过重复调用");
        //     return;
        // }

        playerHasDied = true; // 标记玩家已死亡
        Debug.Log("[LevelController2D] 死亡标记已设置: playerHasDied = true");
        
        // 禁用游戏光标
        if (gameCursor != null)
        {
            gameCursor.SetActive(false);
            Debug.Log("[LevelController2D] 玩家死亡，游戏光标已禁用");
        }
        
        // 显示死亡UI
        if (deathUI != null)
        {
            Debug.Log($"[LevelController2D] 准备显示死亡UI - 当前activeSelf: {deathUI.activeSelf}");
            Debug.Log($"[LevelController2D] 死亡UI名称: {deathUI.name}, 路径: {GetGameObjectPath(deathUI)}");
            
            deathUI.SetActive(true);
            
            Debug.Log($"[LevelController2D] 执行 SetActive(true) 后 - activeSelf: {deathUI.activeSelf}, activeInHierarchy: {deathUI.activeInHierarchy}");
            
            if (!deathUI.activeSelf && !deathUI.activeInHierarchy)
            {
                Debug.LogError("[LevelController2D] ? 警告：死亡UI在SetActive(true)后仍然是非激活状态！");
                Debug.LogError($"[LevelController2D] 可能父对象被禁用了，父对象: {(deathUI.transform.parent != null ? deathUI.transform.parent.name + " (Active: " + deathUI.transform.parent.gameObject.activeSelf + ")" : "无父对象")}");
            }
            else
            {
                Debug.Log("[LevelController2D] ? 死亡UI已成功显示");
            }
        }
        else
        {
            Debug.LogWarning("[LevelController2D] 死亡UI未分配！");
        }
        
        Debug.Log("[LevelController2D] ========================================");
        
        // 可选：暂停游戏
        // Time.timeScale = 0f;
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
        // 确保死亡UI被隐藏（双重保险）
        if (deathUI != null && deathUI.activeSelf)
        {
            deathUI.SetActive(false);
            Debug.Log("[LevelController2D] LoadLevelSequence: 死亡UI已隐藏");
        }
        
        yield return TransitionTimer(); // 显示 Loading 特效

        LevelConfig2D config = levelConfigs[index];
        Debug.Log($"[LevelController2D] 加载关卡: {config.levelName}, levelID: {config.levelID}, playerSpawnId: {config.playerSpawnId}");

        // 确保玩家对象被激活
        if (player != null && !player.activeSelf)
        {
            player.SetActive(true);
            Debug.Log("[LevelController2D] 玩家对象已激活");
            
            // 激活玩家后，立即检查死亡UI是否被意外激活
            if (deathUI != null && deathUI.activeSelf)
            {
                deathUI.SetActive(false);
                Debug.LogWarning("[LevelController2D] LoadLevelSequence: 玩家激活后检测到死亡UI被意外激活，已强制关闭");
            }
        }

        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
            Debug.Log($"[LevelController2D] 自动查找 SpawnPoint2D，找到 {allSpawnPoints.Length} 个");
            if (allSpawnPoints.Length > 0)
            {
                Debug.Log("[LevelController2D] SpawnPoint 列表: " + string.Join(", ", allSpawnPoints.Select(s => s.spawnId + ":" + s.name)));
            }
        }

        if (allSpawnPoints.Length > 0)
        {
            // 修复：使用 playerSpawnId 而不是 levelID
            TeleportPlayer2D(config, config.playerSpawnId);
        }
        else
        {
            Debug.LogWarning("[LevelController2D] 场景中没有 SpawnPoint2D！请给玩家创建至少一个 SpawnPoint2D。");
        }

        if (snapCameraOnLevelLoad && player != null)
        {
            SnapCinemachine2DToPlayer();
        }

        // 先启用游戏光标（在触发祝福系统之前）
        if (gameCursor != null)
        {
            gameCursor.SetActive(true);
            Debug.Log("[LevelController2D] 游戏光标已启用");
            
            // 确保系统光标可见（防止被自定义光标隐藏）
            UnityEngine.Cursor.visible = true;
            Debug.Log("[LevelController2D] 系统光标已设置为可见");
        }

        // 加载完成后，最后一次确认死亡UI是关闭的
        if (deathUI != null && deathUI.activeSelf)
        {
            deathUI.SetActive(false);
            Debug.LogWarning("[LevelController2D] 关卡加载完成时检测到死亡UI处于激活状态，已强制关闭");
        }

        // 只在非首关触发祝福系统（首关不需要选祝福）
        if (index > 0 && BlessingManager.Instance != null)
        {
            BlessingManager.Instance.TriggerBlessing();
            Debug.Log("[LevelController2D] 祝福系统已触发");
        }
        else if (index == 0)
        {
            Debug.Log("[LevelController2D] 首关跳过祝福触发");
        }
        else
        {
            Debug.LogWarning("[LevelController2D] BlessingManager.Instance 为空，跳过祝福触发");
        }

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

        // 强制重新搜索所有 SpawnPoint（避免缓存问题）
        allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
        Debug.Log($"[LevelController2D] 重新搜索 SpawnPoint2D，找到 {allSpawnPoints.Length} 个");
        
        if (allSpawnPoints.Length == 0)
        {
            Debug.LogError("[LevelController2D] 场景中没有任何 SpawnPoint2D，无法传送玩家");
            return;
        }
        
        // 打印所有 SpawnPoint 的详细信息
        Debug.Log("[LevelController2D] === 所有 SpawnPoint 列表 ===");
        foreach (var point in allSpawnPoints)
        {
            Debug.Log($"  - {point.name}: spawnId = {point.spawnId}, 位置 = {point.transform.position}");
        }
        Debug.Log("[LevelController2D] ========================");

        // 查找所有匹配的 SpawnPoint
        SpawnPoint2D[] matchingSpawnPoints = allSpawnPoints.Where(p => p.spawnId == spawnId).ToArray();
        
        if (matchingSpawnPoints.Length == 0)
        {
            Debug.LogError($"[LevelController2D] ? 找不到 spawnId = {spawnId} 的 SpawnPoint2D");
            Debug.LogError($"[LevelController2D] 可用的 spawnId: {string.Join(", ", allSpawnPoints.Select(p => p.spawnId))}");
            return;
        }

        // 如果有多个相同 ID 的 SpawnPoint，随机选择一个
        SpawnPoint2D sp;
        if (matchingSpawnPoints.Length > 1)
        {
            int randomIndex = Random.Range(0, matchingSpawnPoints.Length);
            sp = matchingSpawnPoints[randomIndex];
            Debug.Log($"[LevelController2D] 找到 {matchingSpawnPoints.Length} 个 spawnId={spawnId} 的生成点，随机选择第 {randomIndex} 个: {sp.name}");
        }
        else
        {
            sp = matchingSpawnPoints[0];
            Debug.Log($"[LevelController2D] ? 匹配到唯一生成点：{sp.name} (spawnId={sp.spawnId})");
        }

        if (sp.resetPlayerState)
        {
            Debug.Log("[LevelController2D] 检测到需要重置玩家状态");
            ClearPlayerState(player);
        }

        Vector3 targetPos = sp.GetWorldPosition();
        Debug.Log($"[LevelController2D] 传送前 玩家: {player.transform.position} -> 目标: {targetPos}");
        player.transform.position = targetPos;
        Debug.Log($"[LevelController2D] 传送后 玩家位置: {player.transform.position}");

        if (sp.snapCameraToPlayer && snapCameraOnLevelLoad)
        {
            Debug.Log("[LevelController2D] 检测到需要快照相机");
            SnapCinemachine2DToPlayer();
        }

        Debug.Log($"[LevelController2D] TeleportPlayerToSpawnId() 完成，spawnId: {spawnId}，选择的生成点: {sp.name}");
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

        // 重置刚体速度
        if (playerRb2D != null)
        {
            playerRb2D.velocity = Vector2.zero;
            playerRb2D.angularVelocity = 0f;
            Debug.Log("[LevelController2D] 刚体速度 / 角速度 已清零");
        }

        // 重置血量和死亡状态
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth();
            Debug.Log("[LevelController2D] PlayerHealth 已重置为最大");
            
            // 通过禁用并重新启用来触发 OnEnable，确保 isDead 被重置
            if (player.activeSelf)
            {
                player.SetActive(false);
                player.SetActive(true);
                Debug.Log("[LevelController2D] 通过重新激活玩家对象来重置 PlayerHealth.isDead 状态");
            }
        }

        // 重置动画状态机
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.Rebind();        // 重置所有参数和状态到默认值
            playerAnimator.Update(0f);      // 立即更新一帧
            Debug.Log("[LevelController2D] 玩家 Animator 已重置");
        }
        else
        {
            Debug.LogWarning("[LevelController2D] 玩家身上没有找到 Animator 组件");
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