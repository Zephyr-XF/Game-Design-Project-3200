using UnityEngine;
using UnityEngine.UI;

public class BreakQTEUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel; // 整个UI的父物体
    public Image backgroundImage; // 新增：背景图片 (半透明遮罩)
    public RectTransform[] skillIcons; // 三个技能图标 (对应 K, L, U)
    public Image timerBar; // 倒计时进度条 (Image Type 设为 Filled)

    [Header("Animation Settings")]
    public float shakeAmount = 5f; // 抖动幅度
    public float shakeSpeed = 20f; // 抖动速度

    private Vector3[] initialIconPositions;
    private bool isShown = false;

    void Awake()
    {
        // 记录图标的原始位置，以免抖歪了回不去
        if (skillIcons != null)
        {
            initialIconPositions = new Vector3[skillIcons.Length];
            for (int i = 0; i < skillIcons.Length; i++)
            {
                if (skillIcons[i] != null)
                    initialIconPositions[i] = skillIcons[i].anchoredPosition;
            }
        }

        // 初始隐藏
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    void Update()
    {
        if (!isShown) return;

        // 1. 图标抖动效果
        ShakeIcons();
    }

    public void ShowUI()
    {
        isShown = true;
        if (uiPanel != null) uiPanel.SetActive(true);
    }

    public void HideUI()
    {
        isShown = false;
        if (uiPanel != null) uiPanel.SetActive(false);

        // 还原位置
        ResetIconPositions();
    }

    public void UpdateTimer(float ratio)
    {
        if (timerBar != null)
        {
            timerBar.fillAmount = ratio;
            timerBar.color = ratio < 0.3f ? Color.red : Color.white;
        }
    }

    private void ShakeIcons()
    {
        if (skillIcons == null) return;

        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                float x = (Mathf.PerlinNoise(Time.unscaledTime * shakeSpeed, i * 10f) - 0.5f) * shakeAmount;
                float y = (Mathf.PerlinNoise(Time.unscaledTime * shakeSpeed, i * 10f + 50f) - 0.5f) * shakeAmount;
                skillIcons[i].anchoredPosition = initialIconPositions[i] + new Vector3(x, y, 0);
            }
        }
    }

    private void ResetIconPositions()
    {
        if (skillIcons == null) return;
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (skillIcons[i] != null)
                skillIcons[i].anchoredPosition = initialIconPositions[i];
        }
    }
}

