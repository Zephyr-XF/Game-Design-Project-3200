using UnityEngine;
using Cinemachine;

// 这个脚本现在变成了一个简单的状态切换器
public class PlayerBreakEffectManager : MonoBehaviour
{
    public static PlayerBreakEffectManager Instance { get; private set; }

    [Header("相机引用")]
    // 在Inspector中，把你的“主玩法相机”拖到这里
    public CinemachineVirtualCamera mainGameplayCamera;
    // 在Inspector中，把刚才创建的“VCam_BreakEffect”拖到这里
    public GameObject breakEffectCameraObject; // 改成GameObject，因为我们要SetActive

    // 状态标记
    public bool IsTimeFrozen { get; private set; } = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 确保特写相机在开始时是关闭的
        if (breakEffectCameraObject != null)
        {
            breakEffectCameraObject.SetActive(false);
        }
    }

    /// <summary>
    /// 第一步：触发冻结和镜头切换
    /// </summary>
    public void TriggerCinematicFreeze(Transform focusTarget)
    {
        if (IsTimeFrozen) return;

        IsTimeFrozen = true;
        Time.timeScale = 0f;

        // 【核心改动】只需激活特写相机即可！
        // Cinemachine会因为它的优先级更高而自动开始平滑过渡。
        if (breakEffectCameraObject != null)
        {
            var vcam = breakEffectCameraObject.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = focusTarget;
                vcam.LookAt = focusTarget;

                // --- 【核心改动】 ---
                // 1. 获取VCam当前的位置
                Vector3 vcamPos = vcam.transform.position;
                // 2. 获取玩家的XY位置，但使用VCam自己的Z位置
                Vector3 targetPos = new Vector3(focusTarget.position.x, focusTarget.position.y, vcamPos.z);
                // 3. 计算一个不会改变Z轴的位移
                Vector3 deltaPosition = targetPos - vcamPos;

                // 4. 使用这个安全的位移来传送
                vcam.OnTargetObjectWarped(focusTarget, deltaPosition);
            }
            breakEffectCameraObject.SetActive(true);
        }
    }

    /// <summary>
    /// 第二步：解锁时间，但保持镜头特写
    /// </summary>
    public void UnlockTimeButKeepCamera()
    {
        Time.timeScale = 1f;
        IsTimeFrozen = false;
        // 什么都不用做，因为特写相机依然是激活状态，优先级最高
    }

    /// <summary>
    /// 第三步：平滑地恢复镜头
    /// </summary>
    public void RestoreCameraView()
    {
        // 【核心改动】只需禁用特写相机！
        // Cinemachine会发现没有高优先级的相机了，于是自动平滑地切回“主玩法相机”。
        if (breakEffectCameraObject != null)
        {
            breakEffectCameraObject.SetActive(false);
        }
    }
}
