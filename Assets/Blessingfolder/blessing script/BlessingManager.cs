using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlessingManager : MonoBehaviour
{
    public static BlessingManager Instance;

    public BlessingUI blessingUI;
    public List<BlessingData> allBlessings; // Drag all created blessings here

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep it across scenes if needed, or just for this session
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Debug trigger for testing: Toggle with B
        if (Input.GetKeyDown(KeyCode.B)) 
        {
            if (blessingUI.blessingPanel.activeSelf)
            {
                blessingUI.Hide();
                Time.timeScale = 1; // Resume game if we just closed it
            }
            else
            {
                TriggerBlessing();
            }
        }
    }

    public void TriggerBlessing()
    {
        Debug.Log("Blessing Triggered!");
        // 1. Pause Game
        Time.timeScale = 0;

        // 2. Pick 3 random blessings unique
        BlessingData[] options = new BlessingData[3];
        
        // Create a temporary list to shuffle so we don't mess up the original order
        List<BlessingData> shuffledList = new List<BlessingData>(allBlessings);
        
        // Fisher-Yates shuffle
        for (int i = shuffledList.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            BlessingData temp = shuffledList[i];
            shuffledList[i] = shuffledList[rnd];
            shuffledList[rnd] = temp;
        }

        // Take the first 3 (or fewer if we don't have enough)
        int count = Mathf.Min(3, shuffledList.Count);
        for(int i=0; i<count; i++)
        {
            options[i] = shuffledList[i];
        }

        // 3. Show UI
        if (blessingUI != null)
        {
            blessingUI.ShowBlessings(options);
        }
        else
        {
            Debug.LogError("BlessingUI reference is missing in BlessingManager!");
        }
    }

    // New API for manually closing the UI
    public void CloseBlessingUI()
    {
        if (blessingUI != null)
        {
            // Pass null as chosenData for generic close (or handle it gracefully in UI)
            blessingUI.Hide(null, () => 
            {
                Time.timeScale = 1; // Resume game AFTER animation
            });
        }
    }

    public void ChooseBlessing(BlessingData choice)
    {
        // 1. Apply Stat (Persistent)
        ApplyStatPersistent(choice);

        // 2. Hide UI with Animation
        blessingUI.Hide(choice, () => 
        {
            // 3. Resume Game AFTER animation
            Time.timeScale = 1;

            // Apply stats immediately to current instance too
            if(StatsManager.Instance != null)
            {
                 StatsManager.Instance.damage += (choice.statType == StatType.Damage ? choice.amount : 0);
                 if(choice.statType == StatType.Speed) StatsManager.Instance.UpdateSpeed(choice.amount);
                 if(choice.statType == StatType.MaxHealth) 
                 {
                     StatsManager.Instance.UpdateMaxHealth(choice.amount);
                     StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth; // Heal up
                     StatsManager.Instance.UpdateHealth(0);
                 }
            }
        });
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // Keep track of accumulated bonuses
    private int bonusDamage = 0;
    private int bonusSpeed = 0;
    private int bonusMaxHealth = 0;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-apply bonuses to the new StatsManager instance
        if(StatsManager.Instance != null)
        {
            StatsManager.Instance.damage += bonusDamage;
            StatsManager.Instance.UpdateSpeed(bonusSpeed);
            StatsManager.Instance.UpdateMaxHealth(bonusMaxHealth);
            
            // Reset health to full
            StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
            StatsManager.Instance.UpdateHealth(0); // Refresh UI
        }
    }

    private void ApplyStatPersistent(BlessingData data)
    {
        switch (data.statType)
        {
            case StatType.Damage:
                bonusDamage += data.amount;
                break;
            case StatType.Speed:
                bonusSpeed += data.amount;
                break;
            case StatType.MaxHealth:
                bonusMaxHealth += data.amount;
                break;
        }
    }
}
