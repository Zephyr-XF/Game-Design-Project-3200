using UnityEngine;
using UnityEngine.UI; // 必须引用

/// <summary>
/// 管理玩家的清醒值（Sanity）系统。
/// 负责UI更新、数值变化调用，以及当清醒值降为0时的逻辑。
/// </summary>
public class PlayerSanity : MonoBehaviour
{
    public static PlayerSanity Instance;

    [Header("UI References")]
    [Tooltip("用于显示清醒条填充的Image组件")]
    public Image sanityBarFill;

    // --- 修改部分开始 ---

    [Header("Visual Effect References")]
    [Tooltip("拖入带有 CameraFilterModified 脚本的摄像机对象")]
    public CameraFilterModified cameraFilter; // <-- 新增：对相机滤镜脚本的引用

    [Tooltip("仅在低Sanity时显示的额外UI遮罩（如血迹、噪点）")]
    public Image screenOverlayImage; // <-- 重命名，功能更清晰

    [Tooltip("低Sanity时遮罩的颜色和透明度")]
    public Color lowSanityOverlayColor = new Color(1f, 1f, 1f, 0.4f);

    [Tooltip("低Sanity时显示的特殊纹理 (比如血迹、噪点图)")]
    public Sprite lowSanityTexture;

    // --- 不再需要的旧变量（可以删除或注释掉） ---
    // public Color highSanityColor;
    // public Color lowSanityColor;

    // --- 修改部分结束 ---

    [Header("Debugging")]
    public bool enableDebug = true;

    private bool isFilterHiddenForCinematic = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 确保一开始没有奇怪的图片残留
        if (screenOverlayImage != null)
        {
            screenOverlayImage.sprite = null;
            screenOverlayImage.enabled = false; // 默认隐藏
        }

        UpdateSanityUI();
        UpdateSanityFilter(); // 初始化滤镜
    }

    public void ChangeSanity(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeSanity called: {amount}");

        StatsManager.Instance.UpdateSanity(amount);

        UpdateSanityUI();
        UpdateSanityFilter(); // 每次数值改变时更新滤镜状态

        if (StatsManager.Instance.currentSanity <= 0)
        {
            StatsManager.Instance.currentSanity = 0;
            GoInsane();
        }
    }

    public int GetCurrentSanity()
    {
        return StatsManager.Instance.currentSanity;
    }

    private void UpdateSanityUI()
    {
        if (sanityBarFill != null)
        {
            sanityBarFill.fillAmount = (float)StatsManager.Instance.currentSanity / StatsManager.Instance.maxSanity;
        }
    }

    /// <summary>
    /// 【核心修改】根据当前Sanity值，调用CameraFilterModified的预设，并更新UI遮罩
    /// </summary>
    public void UpdateSanityFilter()
    {
        // 检查相机滤镜引用是否存在
        if (cameraFilter == null)
        {
            Debug.LogWarning("CameraFilterModified 引用未在 PlayerSanity 脚本中设置！");
            return;
        }

        // 如果正处于特写状态，强制隐藏UI遮罩，但滤镜可能依然需要保持（取决于设计）
        if (isFilterHiddenForCinematic)
        {
            if (screenOverlayImage != null) screenOverlayImage.enabled = false;
            return;
        }

        int currentSanity = StatsManager.Instance.currentSanity;

        // 逻辑：Sanity > 50 应用预设A，否则应用预设B并显示遮罩
        if (currentSanity > 50)
        {
            // 1. 应用清晰、高对比度的品红预设
            cameraFilter.ApplyPresetA();

            // 2. 隐藏额外的UI遮罩
            if (screenOverlayImage != null)
            {
                screenOverlayImage.enabled = false;
            }
        }
        else
        {
            // 1. 应用模糊、低亮度的冷蓝预设
            cameraFilter.ApplyPresetB();

            // 2. 显示额外的UI遮罩（血迹等）
            if (screenOverlayImage != null && lowSanityTexture != null)
            {
                screenOverlayImage.enabled = true;
                screenOverlayImage.sprite = lowSanityTexture;
                screenOverlayImage.color = lowSanityOverlayColor; // 使用独立的颜色控制遮罩本身
            }
        }
    }

    /// <summary>
    /// 【接口】供Player_Combat调用。
    /// 当特写开始时传入 true (隐藏UI遮罩)，结束时传入 false (恢复UI遮罩)。
    /// 注意：这现在只影响UI遮罩，不影响后处理滤镜。
    /// </summary>
    public void ToggleFilterForCinematic(bool hideOverlay)
    {
        isFilterHiddenForCinematic = hideOverlay;

        if (hideOverlay)
        {
            // 立即隐藏UI遮罩
            if (screenOverlayImage != null) screenOverlayImage.enabled = false;
        }
        else
        {
            // 恢复显示，并根据当前数值重置正确的样式
            UpdateSanityFilter();
        }
    }

    private void GoInsane()
    {
        if (enableDebug) Debug.Log("Player has gone insane!");
        // 此处可添加其他逻辑
    }
}