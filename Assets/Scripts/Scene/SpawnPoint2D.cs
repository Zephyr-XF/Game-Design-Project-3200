using UnityEngine;

/// <summary>
/// 2D 重生点：
/// - 可以指定一个父对象（spawnRoot）
/// - 记录相对父对象的"本地重生坐标"
/// - 提供本地/世界坐标转换
/// - 可选：重置玩家状态 / 相机对齐
/// </summary>
public class SpawnPoint2D : MonoBehaviour
{
    [Header("标识")]
    [Tooltip("在本关卡内保持唯一，用于通过 ID 查找该重生点")]
    public int spawnId = -1;

    [Header("坐标模式")]
    [Tooltip("如果勾选，使用此 GameObject 的世界坐标作为重生点；" +
             "如果不勾选，使用下面的本地坐标配置")]
    public bool useTransformPosition = true;

    [Header("父对象（可选）- 仅当不使用 Transform 位置时")]
    [Tooltip("如果为空，则使用当前 Transform 作为根；" +
             "如果不为空，则本地坐标 localSpawnPosition 是相对于这个根的。")]
    public Transform spawnRoot;

    [Header("本地重生坐标（相对于 spawnRoot）- 仅当不使用 Transform 位置时")]
    [Tooltip("相对于 spawnRoot 的 2D 坐标（Z 一般用 root 的 Z）")]
    public Vector2 localSpawnPosition;

    [Header("Z 轴设置")]
    [Tooltip("如果为 true，则世界坐标的 Z 使用 fixedZ；" +
             "否则使用 spawnRoot.position.z 或此对象的 Z")]
    public bool useFixedZ = true;
    public float fixedZ = 0f;

    [Header("重生行为")]
    [Tooltip("玩家传送到这个点时，是否重置玩家状态（速度、血量等）")]
    public bool resetPlayerState = true;

    [Tooltip("玩家传送到这个点时，是否让相机立刻对齐到玩家位置")]
    public bool snapCameraToPlayer = true;

    /// <summary>
    /// 计算当前的世界重生坐标
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        Vector3 world;

        if (useTransformPosition)
        {
            // 直接使用此 GameObject 的世界坐标
            world = transform.position;
            Debug.Log($"[SpawnPoint2D] 使用 Transform 世界坐标: {world}");
        }
        else
        {
            // 使用本地坐标配置
            Transform root = spawnRoot != null ? spawnRoot : transform;

            // 将本地坐标转换为世界坐标
            // TransformPoint 会自动处理父对象的位置、旋转和缩放
            world = root.TransformPoint(new Vector3(localSpawnPosition.x, localSpawnPosition.y, 0f));

            Debug.Log($"[SpawnPoint2D] 使用本地坐标转换:");
            Debug.Log($"  - Root: {root.name}, Root世界坐标: {root.position}");
            Debug.Log($"  - 本地坐标: {localSpawnPosition}");
            Debug.Log($"  - 转换后世界坐标: {world}");
        }

        // 处理 Z 轴
        if (useFixedZ)
        {
            world.z = fixedZ;
            Debug.Log($"[SpawnPoint2D] 使用固定 Z: {fixedZ}");
        }
        else if (!useTransformPosition && spawnRoot != null)
        {
            world.z = spawnRoot.position.z;
            Debug.Log($"[SpawnPoint2D] 使用 spawnRoot 的 Z: {spawnRoot.position.z}");
        }
        // 如果 useTransformPosition 为 true，则保持原有的 Z 值

        Debug.Log($"[SpawnPoint2D] 最终世界坐标: {world}");
        return world;
    }

    /// <summary>
    /// 把一个世界坐标转换为相对于 spawnRoot 的本地坐标（主要用于编辑器或调试）
    /// </summary>
    public Vector2 WorldToLocal2D(Vector3 worldPosition)
    {
        Transform root = spawnRoot != null ? spawnRoot : transform;

        Vector3 local = root.InverseTransformPoint(worldPosition);
        return new Vector2(local.x, local.y);
    }
}