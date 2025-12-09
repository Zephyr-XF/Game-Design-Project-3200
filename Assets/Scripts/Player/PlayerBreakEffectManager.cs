using UnityEngine;
using Cinemachine;
using System.Collections;

// 这个脚本现在是击破特效的完整控制器，管理时间、相机、UI和画面滤镜。
[RequireComponent(typeof(AudioSource))]
public class PlayerBreakEffectManager : MonoBehaviour
{
    public static PlayerBreakEffectManager Instance { get; private set; }

    [Header("Camera & Visuals")]
    [Tooltip("主玩法相机")]
    public CinemachineVirtualCamera mainGameplayCamera;
    [Tooltip("击破特写相机(VCam_BreakEffect)的GameObject")]
    public GameObject breakEffectCameraObject;
    [Tooltip("挂载了 CameraFilterModified 脚本的主相机 (Main Camera)")]
    public CameraFilterModified cameraFilter;

    [Header("UI")]
    [Tooltip("挂载了 BreakQTEUI 脚本的UI面板")]
    public BreakQTEUI breakUI;

    [Header("Audio Settings")]
    [Tooltip("时停触发时的音效列表 (随机播放一个)")]
    public AudioClip[] timeFreezeClips;
    [Tooltip("专门播放不受时停影响音效的AudioSource")]
    public AudioSource managerAudioSource;

    [Header("Time Freeze Settings")]
    [Tooltip("时停效果的最大持续时间（秒）")]
    public float maxFreezeDuration = 3.0f;
    [Tooltip("滤镜效果（变灰/恢复）的过渡速度")]
    public float transitionSpeed = 2.0f;

    // 状态标记
    public bool IsTimeFrozen { get; private set; } = false;
    private float currentFreezeTimer;
    private Coroutine filterCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 确保各种组件在开始时处于正确状态
        if (breakEffectCameraObject != null)
        {
            breakEffectCameraObject.SetActive(false);
        }
        if (breakUI != null)
        {
            breakUI.HideUI();
        }
        // 游戏开始时，确保滤镜处于“基础画风”状态
        if (cameraFilter != null)
        {
            cameraFilter.SetBreakIntensity(0f);
        }
    }

    void Update()
    {
        // 只在时停状态下处理倒计时逻辑
        if (IsTimeFrozen)
        {
            // 1. 倒计时（使用非缩放时间，因为Time.timeScale为0）
            currentFreezeTimer -= Time.unscaledDeltaTime;

            // 2. 更新 UI 进度条
            if (breakUI != null)
            {
                breakUI.UpdateTimer(Mathf.Max(0, currentFreezeTimer / maxFreezeDuration));
            }

            // 3. 时间到了，强制结束时停
            if (currentFreezeTimer <= 0)
            {
                // 模拟玩家未做选择，自动恢复
                Debug.Log("Break time out!");
                UnlockTimeButKeepCamera();
                RestoreCameraView(); // 同时恢复相机
            }
        }
    }

    /// <summary>
    /// 第一步：触发冻结和所有特效
    /// </summary>
    public void TriggerCinematicFreeze(Transform focusTarget)
    {
        if (IsTimeFrozen) return;

        IsTimeFrozen = true;
        Time.timeScale = 0f;
        currentFreezeTimer = maxFreezeDuration; // 重置计时器

        // 停止玩家身上可能在播放的常规音效
        var playerAudio = focusTarget.GetComponent<PlayerAudio>();
        if (playerAudio != null) playerAudio.StopAllAudio();

        PlayRandomFreezeSound();

        // 切换相机
        if (breakEffectCameraObject != null)
        {
            var vcam = breakEffectCameraObject.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = focusTarget;
                vcam.LookAt = focusTarget;
                Vector3 vcamPos = vcam.transform.position;
                Vector3 targetPos = new Vector3(focusTarget.position.x, focusTarget.position.y, vcamPos.z);
                vcam.OnTargetObjectWarped(focusTarget, targetPos - vcamPos);
            }
            breakEffectCameraObject.SetActive(true);
        }

        // 显示UI
        if (breakUI != null) breakUI.ShowUI();

        // 【核心改动】启动滤镜过渡协程（从0到1）
        StartFilterTransition(1f);
    }

    /// <summary>
    /// 第二步：玩家做出选择，解锁时间，但保持特写
    /// </summary>
    public void UnlockTimeButKeepCamera()
    {
        if (!IsTimeFrozen) return; // 避免重复调用

        Time.timeScale = 1f;
        IsTimeFrozen = false;

        // 隐藏UI
        if (breakUI != null) breakUI.HideUI();

        // 【核心改动】启动滤镜恢复协程（从当前值到0）
        StartFilterTransition(0f);
    }

    /// <summary>
    /// 第三步：技能动画播放完毕，恢复主相机
    /// </summary>
    public void RestoreCameraView()
    {
        if (breakEffectCameraObject != null)
        {
            breakEffectCameraObject.SetActive(false);
        }
    }

    private void PlayRandomFreezeSound()
    {
        if (timeFreezeClips != null && timeFreezeClips.Length > 0 && managerAudioSource != null)
        {
            managerAudioSource.PlayOneShot(timeFreezeClips[Random.Range(0, timeFreezeClips.Length)]);
        }
    }

    /// <summary>
    /// 统一管理滤镜过渡的协程
    /// </summary>
    private void StartFilterTransition(float targetIntensity)
    {
        if (filterCoroutine != null)
        {
            StopCoroutine(filterCoroutine);
        }
        filterCoroutine = StartCoroutine(TransitionFilter(targetIntensity));
    }

    private IEnumerator TransitionFilter(float targetIntensity)
    {
        if (cameraFilter == null) yield break;

        float startIntensity = cameraFilter.breakEffectIntensity;
        float time = 0;

        while (time < 1f)
        {
            // 使用非缩放时间进行插值，确保在时停时也能正常过渡
            time += transitionSpeed * Time.unscaledDeltaTime;

            float newIntensity = Mathf.Lerp(startIntensity, targetIntensity, time);
            cameraFilter.SetBreakIntensity(newIntensity);

            yield return null;
        }

        // 确保最终停在目标值
        cameraFilter.SetBreakIntensity(targetIntensity);
    }
}


