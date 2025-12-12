using UnityEngine;
using UnityEngine.UI; // 必须引用

/// <summary>
/// 管理玩家的清醒值（Sanity）系统。
/// 负责UI更新、数值变化调用，以及当清醒值降为0时的逻辑。
/// </summary>
public class PlayerSanity : MonoBehaviour
{
    public static PlayerSanity Instance; // 1. 单例模式，方便Combat脚本调用

    [Header("UI References")]
    [Tooltip("用于显示清醒条填充的Image组件")]
    public Image sanityBarFill;

    [Header("Visual Filter Settings")]
    [Tooltip("覆盖全屏的Image组件，作为滤镜")]
    public Image screenFilterImage; // <-- 拖入一个覆盖全屏的UI Image，记得把Raycast Target关掉

    [Tooltip("Sanity > 50 时的浅色滤镜颜色 (例如: 纯透明 或 淡淡的冷色)")]
    public Color highSanityColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("Sanity <= 50 时的深色滤镜颜色 (例如: 深红色，半透明)")]
    public Color lowSanityColor = new Color(0.5f, 0f, 0f, 0.4f);

    [Tooltip("Sanity <= 50 时显示的特殊纹理 (比如血迹、噪点图)")]
    public Sprite lowSanityTexture;

    [Header("Debugging")]
    public bool enableDebug = true;

    // 内部状态，记录当前滤镜是否应该被强制隐藏（用于特写）
    private bool isFilterHiddenForCinematic = false;

    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 确保一开始没有奇怪的图片残留
        if (screenFilterImage != null) screenFilterImage.sprite = null;
        UpdateSanityUI();
        UpdateSanityFilter(); // 初始化滤镜
    }

    /// <summary>
    /// 【接口】改变玩家的清醒值。
    /// </summary>
    public void ChangeSanity(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeSanity called: {amount}");

        StatsManager.Instance.UpdateSanity(amount);

        UpdateSanityUI();
        UpdateSanityFilter(); // <-- 每次数值改变时更新滤镜状态

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
    /// 根据当前Sanity值更新全屏滤镜的样式
    /// </summary>
    public void UpdateSanityFilter()
    {
        if (screenFilterImage == null) return;

        // 如果正处于特写状态，强制隐藏，不更新视觉
        if (isFilterHiddenForCinematic)
        {
            screenFilterImage.enabled = false;
            return;
        }

        screenFilterImage.enabled = true;
        int currentSanity = StatsManager.Instance.currentSanity;

        // 逻辑：Sanity > 50 为浅色模式，否则为深色+图片模式
        if (currentSanity > 50)
        {
            // 浅色模式：颜色变浅，移除图片
            screenFilterImage.color = highSanityColor;
            screenFilterImage.sprite = null;
        }
        else
        {
            // 深色模式：颜色变深，设置图片
            screenFilterImage.color = lowSanityColor;
            if (lowSanityTexture != null)
            {
                screenFilterImage.sprite = lowSanityTexture;
            }
        }
    }

    /// <summary>
    /// 【接口】供Player_Combat调用。
    /// 当特写开始时传入 true (隐藏滤镜)，结束时传入 false (恢复滤镜)。
    /// </summary>
    public void ToggleFilterForCinematic(bool hideFilter)
    {
        isFilterHiddenForCinematic = hideFilter;

        if (hideFilter)
        {
            // 立即隐藏
            if (screenFilterImage != null) screenFilterImage.enabled = false;
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
