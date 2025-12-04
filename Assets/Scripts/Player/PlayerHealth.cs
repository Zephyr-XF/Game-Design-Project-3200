using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int currentHealth; // 仅用于在Inspector观察，实际逻辑走StatsManager
    public int maxHealth = 200; // 请在Inspector面板确认这里填的是200

    [Header("UI References")]
    public TMP_Text healthText;
    public Animator healthTextAnim;

    [Header("Debugging")]
    public bool enableDebug = true;

    // --- 核心修复：Awake ---
    public void Awake()
    {
        // 1. 检查 StatsManager 是否存在
        if (StatsManager.Instance != null)
        {
            // 2. 强制把这里设置的 MaxHealth 同步给 Manager
            StatsManager.Instance.maxHealth = maxHealth;

            // 3. 强制把当前血量回满
            StatsManager.Instance.currentHealth = maxHealth;

            if (enableDebug) Debug.Log($"[PlayerHealth] 已同步数据到 StatsManager。最大血量: {maxHealth}");
        }
        else
        {
            Debug.LogError("严重错误：场景中找不到 StatsManager！请确保有一个物体挂载了 StatsManager 脚本。");
        }

        // 同步本地变量（为了让你在 Inspector 里看着舒服）
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 确保游戏开始第一帧 UI 显示正确
        UpdateHealthUI();
    }

    public void ChangeHealth(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeHealth called: {amount}, StatsManager Current: {StatsManager.Instance.currentHealth}");

        // 播放受伤/回血的 UI 动画
        if (healthTextAnim != null)
        {
            healthTextAnim.Play("UI");
        }

        // 修改 StatsManager 中的数据
        StatsManager.Instance.currentHealth += amount;

        // 限制血量上限
        if (StatsManager.Instance.currentHealth > StatsManager.Instance.maxHealth)
        {
            StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
        }
        // 检查死亡
        else if (StatsManager.Instance.currentHealth <= 0)
        {
            StatsManager.Instance.currentHealth = 0;
            UpdateHealthUI();
            Die();
            return;
        }

        // 同步本地变量以便观察
        currentHealth = StatsManager.Instance.currentHealth;

        UpdateHealthUI();

        if (enableDebug) Debug.Log($"Health after change: {StatsManager.Instance.currentHealth}");
    }

    public void SetMaxHealth()
    {
        if (healthTextAnim != null) healthTextAnim.Play("UI");

        // 恢复满血逻辑
        StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
        currentHealth = StatsManager.Instance.maxHealth; // 同步本地

        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null && StatsManager.Instance != null)
        {
            string newText = "HP:" + StatsManager.Instance.currentHealth + "/" + StatsManager.Instance.maxHealth;
            healthText.text = newText;

            if (enableDebug)
            {
                Debug.Log($"UI updated: {newText}");
            }
        }
        else
        {
            // 防止报错刷屏
            if (healthText == null) Debug.LogError("PlayerHealth: healthText 未赋值！");
        }
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        // 在这里添加死亡逻辑，例如显示 Game Over 界面
        // 暂时禁用玩家物体
        gameObject.SetActive(false);
    }
}