using Cinemachine;
using System.Linq;
using UnityEngine;

public class LevelController2D : MonoBehaviour
{
    [Header("关卡配置")]
    public LevelConfig2D[] levelConfigs;
    public int currentIndex = 0;

    [Header("玩家")]
    public GameObject player;           // 玩家 Transform
    public Rigidbody2D playerRb2D;     // 玩家刚体（如果有）

    [Header("Cinemachine 2D")]
    public CinemachineVirtualCamera vcam;
    public bool snapCameraOnLevelLoad = true;

    [Header("摄像机限制")]
    //public Collider2D cameraBoundsCollider;

    public SpawnPoint2D[] allSpawnPoints;

    private void Start()
    {
        Time.timeScale = 1f;
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

        LevelConfig2D config = levelConfigs[index];
        Debug.Log($"[LevelController2D] 加载关卡: {config.levelName}, levelID: {config.levelID}");

        // 预先查找所有重生点
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
            Debug.Log($"[LevelController2D] 自动查找 SpawnPoint2D，共找到 {allSpawnPoints.Length} 个");
        }

        // 如果场景中有重生点，则传送玩家
        if (allSpawnPoints.Length > 0)
        {
            TeleportPlayer2D(config, config.levelID);
        }
        else
        {
            Debug.LogWarning($"[LevelController2D] 场景中没有 SpawnPoint2D，跳过玩家传送。" +
                           "请在场景中添加至少一个 SpawnPoint2D 组件。");
        }

        // 同步 Cinemachine 相机
        if (snapCameraOnLevelLoad && player != null)
        {
            SnapCinemachine2DToPlayer();
        }

        Debug.Log($"[LevelController2D] 加载关卡完成: {config.levelName}");
    }

    private void TeleportPlayer2D(LevelConfig2D config,int spawnId)
    {
        if (player == null)
        {
            Debug.LogError("[LevelController2D] Player 引用为空，无法传送玩家。");
            return; // 添加 return，防止继续执行
        }
        
        TeleportPlayerToSpawnId(spawnId);
        Debug.Log($"[LevelController2D] TeleportPlayer2D() 被调用，传送玩家到关卡: {config.levelName}");
    }

    public void TeleportPlayerToSpawnId(int spawnId)
    {
        Debug.Log($"[LevelController2D] TeleportPlayerToSpawnId() 被调用，目标 spawnId: {spawnId}");

        if (player == null)
        {
            Debug.LogError("[LevelController2D] Player 为空");
            return;
        }

        // 自动查找所有 SpawnPoint2D（包括禁用的）
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            allSpawnPoints = FindObjectsOfType<SpawnPoint2D>(true);
            Debug.Log($"[LevelController2D] 自动查找 SpawnPoint2D，共找到 {allSpawnPoints.Length} 个");
            
            // 如果还是找不到，报错并返回
            if (allSpawnPoints.Length == 0)
            {
                Debug.LogError($"[LevelController2D] 场景中没有任何 SpawnPoint2D，无法传送玩家");
                return;
            }
        }

        // 查找匹配的重生点
        SpawnPoint2D sp = allSpawnPoints.FirstOrDefault(p => p.spawnId == spawnId);
        if (sp == null)
        {
            Debug.LogError($"[LevelController2D] 找不到 spawnId = {spawnId} 的 SpawnPoint2D。" +
                          $"可用的 spawnId: {string.Join(", ", allSpawnPoints.Select(p => p.spawnId))}");
            return;
        }

        Debug.Log($"[LevelController2D] 找到重生点: {sp.gameObject.name}, spawnId = {sp.spawnId}");

        // 1. 根据配置决定是否清理玩家状态
        if (sp.resetPlayerState)
        {
            ClearPlayerState(player);
        }

        // 2. 获取重生点的世界坐标
        Vector3 targetPos = sp.GetWorldPosition();

        Debug.Log($"[LevelController2D] 重生点世界坐标: {targetPos}");
        Debug.Log($"[LevelController2D] 玩家当前位置: {player.transform.position}");

        // 3. 传送玩家到目标位置
        player.transform.position = targetPos;

        Debug.Log($"[LevelController2D] 玩家已传送，新位置: {player.transform.position}");

        // 4. 相机是否跟随对齐
        if (sp.snapCameraToPlayer && snapCameraOnLevelLoad)
        {
            SnapCinemachine2DToPlayer();
        }

        Debug.Log($"[LevelController2D] 玩家已成功传送到重生点: {spawnId}");
    }

    /// <summary>
    /// 让 Cinemachine 2D 相机立刻跟到玩家当前位置，避免切关卡时抖动
    /// </summary>
    private void SnapCinemachine2DToPlayer()
    {
        if (vcam == null || player == null)
            return;

        // 确保相机 Follow 目标是玩家
        if (vcam.Follow != player)
            vcam.Follow = player.transform;

        // 把虚拟相机移到玩家附近（保持原来的 Z）
        var camTr = vcam.transform;
        camTr.position = new Vector3(
            player.transform.position.x,
            player.transform.position.y,
            camTr.position.z
        );

        var brain = CinemachineCore.Instance.GetActiveBrain(0);
        if (brain != null)
        {
            brain.ManualUpdate();
            Debug.Log("[LevelController2D] Cinemachine 相机已强制更新");
        }
    }

    public void ClearPlayerState(GameObject player)
    {
        Debug.Log("[LevelController2D] ClearPlayerState() 被调用");

        if (player == null)
        {
            Debug.LogWarning("[LevelController2D] Player 为空，无法清理状态");
            return;
        }

        if (playerRb2D != null)
        {
            playerRb2D.velocity = Vector2.zero;
            playerRb2D.angularVelocity = 0f;
            Debug.Log("[LevelController2D] 玩家速度已清理");
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth();
            Debug.Log("[LevelController2D] 玩家血量已回满");
        }
    }
}