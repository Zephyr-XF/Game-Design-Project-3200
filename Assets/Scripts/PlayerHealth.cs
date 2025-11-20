using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public int currentHealth;
    public int maxHealth = 100;

    [Header("UI设置")]
    public TMP_Text healthText;

    [Header("调试设置")]
    public bool enableDebug = true;

    public Animator healthTextAnim;

    public void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        Debug.Log("Text");
        UpdateHealthUI(); // 初始化时更新UI
    }

    public void ChangeHealth(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeHealth called: {amount}, Current: {currentHealth}");
        healthTextAnim.Play("UI");
        currentHealth += amount;
        
        // 限制生命值范围
        if(currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if(currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateHealthUI(); // 先更新UI再死亡
            Die();
            return; // 死亡后直接返回
        }
        
        // 关键修复：无论什么情况都更新UI
        UpdateHealthUI();
        
        if (enableDebug) Debug.Log($"Health after change: {currentHealth}");
    }

    // 专门的UI更新方法
    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            string newText = "HP:" + currentHealth + "/" + maxHealth;
            healthText.text = newText;
            
            if (enableDebug)
            {
                Debug.Log($"UI updated: {newText}");
            }
        }
        else
        {
            Debug.LogError("healthText is null! Please assign the TMP_Text component in Inspector.");
        }
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        // Add death handling logic here (e.g., respawn, game over screen)
        gameObject.SetActive(false);
    }

    
        
    
}


