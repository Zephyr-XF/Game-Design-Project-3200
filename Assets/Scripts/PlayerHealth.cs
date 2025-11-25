using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int currentHealth;
    public int maxHealth = 100;

    [Header("UI References")]
    public TMP_Text healthText;

    [Header("Debugging")]
    public bool enableDebug = true;

    public Animator healthTextAnim;

    public void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        Debug.Log("Text");
        UpdateHealthUI();
    }

    public void ChangeHealth(int amount)
    {
        if (enableDebug) Debug.Log($"ChangeHealth called: {amount}, Current: {currentHealth}");
        healthTextAnim.Play("UI");
        StatsManager.Instance.currentHealth += amount;

        if (StatsManager.Instance.currentHealth > maxHealth)
        {
            StatsManager.Instance.currentHealth = maxHealth;
        }
        else if (StatsManager.Instance.currentHealth <= 0)
        {
            StatsManager.Instance.currentHealth = 0;
            UpdateHealthUI();
            Die();
            return;
        }

        UpdateHealthUI();

        if (enableDebug) Debug.Log($"Health after change: {currentHealth}");
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
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


