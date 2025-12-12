using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopInfo : MonoBehaviour
{
    public CanvasGroup infoPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    [Header("Stats Fields")]
    public TMP_Text[] statText;
    private RectTransform infoPanelRect;
    
    private void Awake()
    {
        infoPanelRect = GetComponent<RectTransform>();
    }
    
    public void ShowItemInfo(ItemSO itemSO)
    {
        infoPanel.alpha = 1;
        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemDescription;
        List<string> stats = new List<string>();
        
        if (itemSO.currentHealth > 0)
            stats.Add("Health: " + itemSO.currentHealth.ToString());
        if (itemSO.maxHealth > 0)
            stats.Add("MaxHealth: " + itemSO.maxHealth.ToString());
        if (itemSO.damage > 0)
            stats.Add("Damage: " + itemSO.damage.ToString());
        if (itemSO.speed > 0)
            stats.Add("Speed: " + itemSO.speed.ToString());
        if (itemSO.duration > 0)
            stats.Add("Duration: " + itemSO.duration.ToString());
            
        DisplayStats(stats);
    }
    
    public void ShowToolInfo(ToolSO toolSO)
    {
        infoPanel.alpha = 1;
        itemNameText.text = toolSO.toolName;
        itemDescriptionText.text = toolSO.toolDescription;
        List<string> stats = new List<string>();
        
        if (toolSO.maxStackSize > 1)
            stats.Add("Max Stack: " + toolSO.maxStackSize.ToString());
        if (toolSO.maxDurability > 0)
            stats.Add("Durability: " + toolSO.maxDurability.ToString());
        if (toolSO.cooldownTime > 0)
            stats.Add("Cooldown: " + toolSO.cooldownTime.ToString() + "s");
        if (toolSO.canUseInCombat)
            stats.Add("Combat Use: Yes");
        else
            stats.Add("Combat Use: No");
            
        DisplayStats(stats);
    }
    
    private void DisplayStats(List<string> stats)
    {
        if (stats.Count <= 0)
        {
            for (int i = 0; i < statText.Length; i++)
            {
                statText[i].gameObject.SetActive(false);
            }
            return;
        }
        
        for (int i = 0; i < statText.Length; i++)
        {
            if (i < stats.Count)
            {
                statText[i].text = stats[i];
                statText[i].gameObject.SetActive(true);
            }
            else
            {
                statText[i].gameObject.SetActive(false);
            }
        }
    }
    
    public void HideItemInfo()
    {
        infoPanel.alpha = 0;
        itemNameText.text = "";
        itemDescriptionText.text = "";
    }
    
    public void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 offset = new Vector3(10, -10, 0);
        infoPanelRect.position = mousePosition + offset;
    }
}
