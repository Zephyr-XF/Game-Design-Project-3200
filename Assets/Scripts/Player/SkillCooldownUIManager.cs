using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个纯粹的UI显示控制器。
/// 它不包含任何计时逻辑，只负责接收指令并更新技能图标的填充量。
/// </summary>
public class SkillsCooldownUIManager : MonoBehaviour
{
    public static SkillsCooldownUIManager Instance;

    // 注意：这里的 SkillUI 类被移除了，因为我们只需要一个简单的 Image 数组
    public Image[] skillCooldownFills = new Image[3];

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 更新指定技能的冷却UI显示。
    /// </summary>
    /// <param name="skillIndex">技能索引 (0=技能1, 1=技能2, ...)</param>
    /// <param name="fillAmount">填充量 (一个0到1之间的值)</param>
    public void UpdateSkillDisplay(int skillIndex, float fillAmount)
    {
        if (skillIndex < 0 || skillIndex >= skillCooldownFills.Length)
        {
            Debug.LogWarning("Trying to update a skill display with an invalid index: " + skillIndex);
            return;
        }

        Image fillImage = skillCooldownFills[skillIndex];
        if (fillImage != null)
        {
            // 直接设置从外部传来的填充比例
            fillImage.fillAmount = fillAmount;
        }
    }
}

