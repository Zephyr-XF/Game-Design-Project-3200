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
            // Ensure this is a root object, otherwise DontDestroyOnLoad won't work
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject); 
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
        isSelecting = false; // Unlock selection for new round
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

    private bool isSelecting = false; // Add selection lock

    public void ChooseBlessing(BlessingData choice)
    {
        if (isSelecting) return; // Prevent double click
        isSelecting = true;

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
                 // --- New Switch Logic for All Stats ---
                 foreach (var mod in choice.modifiers)
                 {
                     switch (mod.statType)
                     {
                         case StatType.Damage:
                             StatsManager.Instance.damage += mod.amount;
                             break;
                         case StatType.Speed:
                             StatsManager.Instance.UpdateSpeed(mod.amount);
                             break;
                         case StatType.MaxHealth:
                             StatsManager.Instance.UpdateMaxHealth(mod.amount);
                             StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth; // Heal up
                             StatsManager.Instance.UpdateHealth(0); 
                             break;
                             
                         case StatType.Impact:
                             StatsManager.Instance.impact += mod.amount;
                             break;
                             
                         case StatType.Skill2Cooldown:
                             StatsManager.Instance.skill2Cooldown += (mod.amount / 10.0f); // e.g. -10 => -1.0s
                             break;
                             
                         case StatType.Skill2DamageMult:
                             StatsManager.Instance.skill2DamageMult += (mod.amount / 10.0f); // e.g. 5 => +0.5x
                             break;
                             
                         case StatType.MinAttackInterval:
                             StatsManager.Instance.minAttackInterval += (mod.amount / 100.0f); // e.g. -5 => -0.05s
                             break;
                             
                         case StatType.TriggerCinematic:
                             if (mod.amount > 0) StatsManager.Instance.canTriggerAnimEventCinematic = true;
                             break;
                     }
                 }

                 // Apply Sanity Cost
                 if(choice.sanityCost > 0)
                 {
                     // Apply Sanity Cost via PlayerSanity wrapper
                     var playerSanity = FindObjectOfType<PlayerSanity>();
                     if (playerSanity != null)
                     {
                         playerSanity.ChangeSanity(-choice.sanityCost);
                     }
                     else
                     {
                         // Fallback just in case
                         StatsManager.Instance.UpdateSanity(-choice.sanityCost);
                     }
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
    private int bonusImpact = 0;
    private int bonusSkill2Cooldown = 0; // Stored as raw int
    private int bonusSkill2DamageMult = 0; // Stored as raw int
    private int bonusMinAttackInterval = 0; // Stored as raw int
    private bool bonusCinematicTrigger = false; // New: Cinematic trigger

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
            
            StatsManager.Instance.impact += bonusImpact;
            StatsManager.Instance.skill2Cooldown += (bonusSkill2Cooldown / 10.0f);
            StatsManager.Instance.skill2DamageMult += (bonusSkill2DamageMult / 10.0f);
            StatsManager.Instance.minAttackInterval += (bonusMinAttackInterval / 100.0f);
            
            if(bonusCinematicTrigger) 
                StatsManager.Instance.canTriggerAnimEventCinematic = true;
            
            // Reset health to full
            
            // Reset health to full
            StatsManager.Instance.currentHealth = StatsManager.Instance.maxHealth;
            StatsManager.Instance.UpdateHealth(0); // Refresh UI
        }
    }

    private void ApplyStatPersistent(BlessingData data)
    {
        foreach (var mod in data.modifiers)
        {
            switch (mod.statType)
            {
                case StatType.Damage:
                    bonusDamage += mod.amount;
                    break;
                case StatType.Speed:
                    bonusSpeed += mod.amount;
                    break;
                case StatType.MaxHealth:
                    bonusMaxHealth += mod.amount;
                    break;
                case StatType.Impact:
                    bonusImpact += mod.amount;
                    break;
                case StatType.Skill2Cooldown:
                    bonusSkill2Cooldown += mod.amount;
                    break;
                case StatType.Skill2DamageMult:
                    bonusSkill2DamageMult += mod.amount;
                    break;
                case StatType.MinAttackInterval:
                    bonusMinAttackInterval += mod.amount;
                    break;
                case StatType.TriggerCinematic:
                    if (mod.amount > 0) bonusCinematicTrigger = true;
                    break;
            }
        }
    }
}
