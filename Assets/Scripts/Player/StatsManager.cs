using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;
    public StatsUI statsUI;

    [Header("Combat Stats")]
    public int damage; // To kill the enemy
    public int impact; // To break the enemy
    public float weaponRange;
    public float knockbackForce;
    public float knockbackTime;
    public float stunTime;

    [Header("Movement Stats")]
    public int speed;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;

    [Header("Sanity Stats")]
    public int currentSanity;
    public int maxSanity = 100;

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
    }

    public void UpdateHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth >= maxHealth)
            currentHealth = maxHealth;
    }

    public void UpdateSpeed(int amount)
    {
        speed += amount;
        // 这个可以保留，用于实时刷新属性面板的数值
        if (statsUI != null) statsUI.UpdateAllStats();
    }

    public void UpdateSanity(int amount)
    {
        currentSanity += amount;
        // 使用 Mathf.Clamp 将值限制在 0 和 maxSanity 之间
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
    }
}