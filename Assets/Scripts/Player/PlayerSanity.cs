using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 管理玩家的清醒值（Sanity）系统。
/// 负责UI更新、数值变化调用，以及当清醒值降为0时的逻辑。
/// 数值本身存储在 StatsManager 中。
/// </summary>
public class PlayerSanity : MonoBehaviour
{
    [Tooltip("用于显示清醒条填充的Image组件")]
    public Image sanityBarFill; // <-- 2. 添加Image引用

    [Header("Debugging")]
    public bool enableDebug = true;

    // 在游戏开始时，更新一次UI以显示初始值
    private void Start()
    {
        UpdateSanityUI();
    }

    /// <summary>
    /// 【接口】改变玩家的清醒值。
    /// </summary>
    /// <param name="amount">要改变的数值。正数为增加，负数为减少。</param>
    public void ChangeSanity(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeSanity called: {amount}, Current Sanity (before change): {StatsManager.Instance.currentSanity}");

        // 调用StatsManager来处理实际的数值更新和范围限制
        StatsManager.Instance.UpdateSanity(amount);

        // 更新UI显示
        UpdateSanityUI();

        // 检查清醒值是否已降至0
        if (StatsManager.Instance.currentSanity <= 0)
        {
            // 确保数值不会低于0
            StatsManager.Instance.currentSanity = 0;
            GoInsane(); // 调用理智耗尽的逻辑
        }

        if (enableDebug) Debug.Log($"Sanity after change: {StatsManager.Instance.currentSanity}");
    }

    /// <summary>
    /// 【接口】获取玩家当前的清醒值。
    /// </summary>
    /// <returns>返回当前清醒值的整数。</returns>
    public int GetCurrentSanity()
    {
        return StatsManager.Instance.currentSanity;
    }

    /// <summary>
    /// 更新清醒值的UI文本显示。
    /// </summary>
    private void UpdateSanityUI()
    {

        // <-- 3. 添加更新清醒条逻辑
        if (sanityBarFill != null)
        {
            // 计算当前清醒值百分比
            sanityBarFill.fillAmount = (float)StatsManager.Instance.currentSanity / StatsManager.Instance.maxSanity;
        }

    }

    /// <summary>
    /// 当清醒值降低到一定程度时触发的逻辑。
    /// </summary>
    private void GoInsane()
    {
        if (enableDebug) Debug.Log("Player has gone insane! (玩家已理智耗尽!)");

        // 在这里添加理智耗尽后的具体游戏逻辑
        // 例如：
        // 1. 屏幕出现后处理特效（视觉扭曲、颜色变化）
        // 2. 玩家控制变得混乱（方向键颠倒）
        // 3. 出现幻觉或怪物
        // 4. 触发游戏失败或进入特殊状态
    }
}
