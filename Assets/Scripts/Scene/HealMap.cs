using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealMap : MonoBehaviour
{
    [Header("UI 设置 (必须拖拽)")]
    [Tooltip("选择菜单：包含回血、回蓝、祝福三个按钮")]
    public GameObject selectionUIPanel;

    // ★ 已移除：不再需要手动拖拽 Blessing 面板，直接用单例调用
    // public GameObject specialFeatureUI; 

    [Header("回血设置")]
    public int healAmount = 1;
    public float healInterval = 1f;

    [Header("Clarity (Sanity) 设置")]
    public float clarityAmount = 5f;
    public float clarityInterval = 1f;

    [Header("识别方式")]
    public LayerMask playerLayer;
    public bool affectPlayer = true;

    [Header("调试")]
    public bool enableDebug = true;

    [Header("动画设置")]
    public Animator anim;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip enterHealZoneSound; // 激活法阵时的音效

    [Space(10)]
    public AudioClip healSound; // 持续恢复时的音效

    // 内部状态
    private bool isPlayerInRange = false;
    private GameObject currentPlayerObj;
    private Coroutine activeCoroutine;
    private bool isEffectActive = false; // 法阵是否已使用
    private bool isInitialized = false;

    private void Awake()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }

        // 隐藏选择菜单
        if (selectionUIPanel != null) selectionUIPanel.SetActive(false);

        StartCoroutine(InitializeAfterDelay());
    }

    private void Update()
    {
        // 只要按下 E 键，无论条件是否满足，先打印状态！
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (enableDebug)
            {
                string uiStatus = (selectionUIPanel != null) ? "正常" : "【空！请拖拽】";
                Debug.Log($"[调试] 按了E键 >>> 玩家在范围内: {isPlayerInRange} | 效果运行中: {isEffectActive} | UI面板: {uiStatus}");
            }

            // 原有的逻辑
            if (isPlayerInRange && !isEffectActive)
            {
                if (selectionUIPanel != null)
                {
                    if (!selectionUIPanel.activeSelf) ShowSelectionUI();
                    else CloseSelectionUI();
                }
                else
                {
                    Debug.LogError("[HealMap] 报错：Selection UI Panel 没赋值！去 Inspector 里拖进去！");
                }
            }
        }
    }

    // --- UI 相关逻辑 ---
    private void ShowSelectionUI()
    {
        if (selectionUIPanel != null) selectionUIPanel.SetActive(true);
    }

    private void CloseSelectionUI()
    {
        if (selectionUIPanel != null) selectionUIPanel.SetActive(false);
    }

    // ==========================================
    // ★★★ 按钮绑定区域 ★★★
    // ==========================================

    // 按钮 1：回血
    public void OnChooseHeal()
    {
        if (currentPlayerObj == null) return;
        CloseSelectionUI();

        // 激活法阵视觉效果
        ActivateShrineState();

        // 启动回血逻辑
        activeCoroutine = StartCoroutine(HealHealthRoutine(currentPlayerObj));
        if (enableDebug) Debug.Log("[HealMap] 选择功能：生命恢复");
    }

    // 按钮 2：回 Clarity/Sanity
    public void OnChooseClarity()
    {
        if (currentPlayerObj == null) return;
        CloseSelectionUI();

        // 激活法阵视觉效果
        ActivateShrineState();

        // 启动回蓝逻辑
        activeCoroutine = StartCoroutine(RestoreClarityRoutine(currentPlayerObj));
        if (enableDebug) Debug.Log("[HealMap] 选择功能：Sanity 恢复");
    }

    // ★★★ 按钮 3：打开 Blessing 系统 (修改) ★★★
    public void OnChooseFeature()
    {
        if (currentPlayerObj == null) return;

        // 1. 关闭原来的 3 选 1 菜单
        CloseSelectionUI();

        // 2. 调用 BlessingManager 单例打开祝福界面
        if (BlessingManager.Instance != null)
        {
            BlessingManager.Instance.TriggerBlessing();
            if (enableDebug) Debug.Log("[HealMap] 调用 BlessingManager 打开祝福界面");
        }
        else
        {
            Debug.LogError("[HealMap] 致命错误：场景里找不到 BlessingManager！请把 BlessingSystem Prefab 拖进场景！");
        }

        // 3. 激活法阵视觉效果 (变亮、播音效、锁定法阵)
        // 这样法阵就算“被使用过”了，不能再按 E 交互
        ActivateShrineState();
    }

    // ==========================================
    // 核心逻辑
    // ==========================================

    private void ActivateShrineState()
    {
        if (isEffectActive) return;

        // 1. 锁定状态，防止再次按 E
        isEffectActive = true;

        // 2. 播放动画 (变亮/运转)
        UpdateVisualStateHeal(true);

        // 3. 播放激活音效 (只播一次)
        if (isInitialized && audioSource != null && enterHealZoneSound != null)
        {
            audioSource.PlayOneShot(enterHealZoneSound);
        }
    }

    // 停止效果 (用于玩家离开时)
    private void StopEffect()
    {
        // 停止协程 (回血/回蓝)
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        // ★ 新增：如果玩家离开法阵范围，强制关闭祝福界面
        if (BlessingManager.Instance != null)
        {
            BlessingManager.Instance.CloseBlessingUI();
        }

        // 重置状态
        isEffectActive = false;
        CloseSelectionUI();
        UpdateVisualStateHeal(false);
    }

    // --- 协程逻辑 (保持不变) ---

    private IEnumerator HealHealthRoutine(GameObject obj)
    {
        var playerHealth = obj.GetComponent<PlayerHealth>();
        while (true)
        {
            if (StatsManager.Instance != null && StatsManager.Instance.currentHealth < StatsManager.Instance.maxHealth)
            {
                if (playerHealth != null) playerHealth.ChangeHealth(healAmount);
                if (audioSource != null && healSound != null) audioSource.PlayOneShot(healSound);
            }
            yield return new WaitForSeconds(healInterval);
        }
    }

    private IEnumerator RestoreClarityRoutine(GameObject obj)
    {
        while (true)
        {
            if (StatsManager.Instance != null)
            {
                if (StatsManager.Instance.currentSanity < StatsManager.Instance.maxSanity)
                {
                    StatsManager.Instance.UpdateSanity((int)clarityAmount);
                    if (audioSource != null && healSound != null) audioSource.PlayOneShot(healSound);
                }
            }
            yield return new WaitForSeconds(clarityInterval);
        }
    }

    // --- 触发器逻辑 ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebug) Debug.Log($"[物理检测] 碰到了: {other.name} (Layer: {other.gameObject.layer})");
        if (!affectPlayer) return;

        if (IsInLayerMask(other.gameObject.layer, playerLayer))
        {
            isPlayerInRange = true;
            currentPlayerObj = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!affectPlayer) return;
        if (IsInLayerMask(other.gameObject.layer, playerLayer))
        {
            isPlayerInRange = false;
            currentPlayerObj = null;
            StopEffect();
        }
    }

    // --- 辅助方法 ---
    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }

    void UpdateVisualStateHeal(bool isActive)
    {
        if (anim != null) anim.SetBool("IsHeal", isActive);
    }
}