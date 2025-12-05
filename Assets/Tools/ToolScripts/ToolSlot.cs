using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ToolSlot : MonoBehaviour, IPointerClickHandler
{
    public ToolSO toolSO;
    public int quantity;
    public Image toolImage;
    public TMP_Text quantityText;
    
    private ToolManager toolManager;
    private static ShopManager activeShop;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private void Start()
    {
        toolManager = GetComponentInParent<ToolManager>();
    }
    
    private void OnEnable()
    {
        ShopKeeper.OnShopStateChanged += HandleShopStateChanged;
    }
    
    private void OnDisable()
    {
        ShopKeeper.OnShopStateChanged -= HandleShopStateChanged;
    }
    
    private void HandleShopStateChanged(ShopManager shopManager, bool isOpen)
    {
        activeShop = isOpen ? shopManager : null;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (quantity > 0)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (activeShop != null)
                {
                    activeShop.SellTool(toolSO);
                    quantity--;
                    UpdateUI();
                }
                else
                {
                    // 左键使用工具（类似 InventorySlot 中的 UseItem）
                    if (toolManager != null)
                    {
                        toolManager.UseTool(this);
                        
                        if (enableDebug)
                            Debug.Log($"[ToolSlot] 使用工具: {toolSO?.toolName ?? "null"}");
                    }
                    else
                    {
                        if (enableDebug)
                            Debug.LogWarning("[ToolSlot] ToolManager 未设置，无法使用工具");
                    }
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                toolManager.DropTool(this);
            }
        }
    }

    public void UpdateUI()
    {
        if (enableDebug)
            Debug.Log($"[ToolSlot] UpdateUI 调用 - Slot GameObject: {name} (Active: {gameObject.activeSelf}), ToolSO: {toolSO?.toolName ?? "null"}, Quantity: {quantity}");
        
        if (quantity <= 0)
        {
            toolSO = null;
            if (enableDebug)
                Debug.Log($"[ToolSlot] 数量<=0，清空toolSO - Slot: {name}");
        }
        if(toolSO != null)
        {
            toolImage.sprite = toolSO.icon;
            toolImage.gameObject.SetActive(true);
            quantityText.text = quantity.ToString();
            
            if (enableDebug)
                Debug.Log($"[ToolSlot] ✓ 显示工具 - Slot GameObject: {name} (Active: {gameObject.activeSelf}), Tool: {toolSO.toolName}, toolImage (Active: {toolImage.gameObject.activeSelf})");
        }
        else
        {
            toolImage.gameObject.SetActive(false);
            quantityText.text = "";
            
            if (enableDebug)
                Debug.Log($"[ToolSlot] ✗ 隐藏工具图标 - Slot GameObject: {name} (Active: {gameObject.activeSelf}), toolImage (Active: {toolImage.gameObject.activeSelf})");
        }
    }
}
