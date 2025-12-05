using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using TMPro; // ❌ 不需要了，因为不再处理 UI 文字

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;
    public StatsUI statsUI;

    // ❌ 删除：public TMP_Text healthText;  <-- 这个现在归 PlayerHealth 管

    [Header("Combat Stats")]
    public int damage;
    public float weaponRange;
    public float knockbackForce;
    public float knockbackTime;
    public float stunTime;

    [Header("Movement Stats")]
    public int speed;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateMaxHealth(int amount)
    {
        maxHealth += amount;
        // ❌ 删除：healthText.text = ... 
        // 这里的 UI 更新现在由 PlayerHealth 自动处理
    }

    public void UpdateHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth >= maxHealth)
            currentHealth = maxHealth;

        // ❌ 删除：healthText.text = ...
        // 这里的 UI 更新现在由 PlayerHealth 自动处理
    }

    public void UpdateSpeed(int amount)
    {
        speed += amount;
        // 这个可以保留，用于实时刷新属性面板的数值
        if (statsUI != null) statsUI.UpdateAllStats();
    }
}