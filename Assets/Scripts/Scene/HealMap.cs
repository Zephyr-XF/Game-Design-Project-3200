using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealMap : MonoBehaviour
{
    [Header("UI 设置 (必须拖拽)")]
    [Tooltip("选择菜单：包含回血、回蓝、特殊功能三个按钮")]
    public GameObject selectionUIPanel;

    [Tooltip("功能 3：点击后要打开的那个特殊界面")]
    public GameObject specialFeatureUI; // ★ 新增：你要打开的那个界面

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

        // 隐藏所有相关UI
        if (selectionUIPanel != null) selectionUIPanel.SetActive(false);
        if (specialFeatureUI != null) specialFeatureUI.SetActive(false); // ★ 确保特殊界面一开始是关的

        StartCoroutine(InitializeAfterDelay());
    }

    private void Update()
    {
        // 只有当玩家在范围内、法阵没用过、且按了E键
        if (isPlayerInRange && !isEffectActive && Input.GetKeyDown(KeyCode.E))
        {
            if (selectionUIPanel != null)
            {
                // 如果选择面板没打开，就打开；打开了就关闭
                if (!selectionUIPanel.activeSelf) ShowSelectionUI();
                else CloseSelectionUI();
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

    // ★★★ 按钮 3：打开特殊界面 (新增) ★★★
    public void OnChooseFeature()
    {
        if (currentPlayerObj == null) return;

        // 1. 关闭选择菜单
        CloseSelectionUI();

        // 2. 打开你想要的那个特殊界面
        if (specialFeatureUI != null)
        {
            specialFeatureUI.SetActive(true);
        }
        else
        {
            Debug.LogError("[HealMap] 报错：Special Feature UI 没赋值！请在 Inspector 拖入你想打开的界面。");
        }

        // 3. 激活法阵视觉效果 (变亮、播音效、锁定法阵)
        // 这样法阵就算“被使用过”了，不能再按 E 交互
        ActivateShrineState();

        if (enableDebug) Debug.Log("[HealMap] 选择功能：打开特殊界面");
    }

    // ==========================================
    // 核心逻辑
    // ==========================================

    // ★ 提取出来的公共方法：处理法阵“被使用”后的视觉和状态
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

        // 如果特殊界面开着，也要强制关掉 (看你需求，一般离开法阵就关掉界面)
        if (specialFeatureUI != null) specialFeatureUI.SetActive(false);

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