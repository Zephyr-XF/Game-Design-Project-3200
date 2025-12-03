using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealMap : MonoBehaviour
{
    [Header("回血设置")]
    [Tooltip("每次回血的数值")]
    public int healAmount = 1;
    
    [Tooltip("回血间隔时间（秒）")]
    public float healInterval = 1f;
    
    [Tooltip("是否影响玩家")]
    public bool affectPlayer = true;

    [Header("识别方式")]
    public LayerMask playerLayer;
    public Collider2D healAreaCollider;
    
    [Header("调试")]
    public bool enableDebug = true;

    [Header("动画设置")]
    public Animator anim;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip healSound;
    public AudioClip enterHealZoneSound;

    // 记录当前在回血区域内的玩家及其协程
    private Dictionary<GameObject, Coroutine> healingCoroutines = new Dictionary<GameObject, Coroutine>();
    
    // 防止游戏开始时播放音效
    private bool isInitialized = false;

    private void Reset()
    {
        // 确保回血区 Collider2D 设为 Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            
            if (enableDebug)
                Debug.Log($"[HealMap] Reset: Collider2D 已设置为 Trigger");
        }
    }

    private void OnValidate()
    {
        // 在 Inspector 中修改值时自动检查
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[HealMap] ? Collider2D 不是 Trigger！这可能导致敌人碰撞异常。");
        }
    }

    private void Awake()
    {
        if (enableDebug)
            Debug.Log($"[HealMap] Awake 被调用 - Time.time: {Time.time}");
        
        // 强制禁用 AudioSource 的 PlayOnAwake，防止自动播放
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
            
            if (enableDebug)
                Debug.Log($"[HealMap] AudioSource.playOnAwake 已设置为 false，已停止播放");
        }
        
        // 使用 Awake + 协程，更早地设置初始化标志
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        if (enableDebug)
            Debug.Log($"[HealMap] 初始化协程开始 - Time.time: {Time.time}");
        
        // 使用实际时间延迟，确保场景完全加载
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;
        
        if (enableDebug)
            Debug.Log($"[HealMap] ? 初始化完成，音效系统已启用 - Time.time: {Time.time}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebug)
            Debug.Log($"[HealMap] OnTriggerEnter2D - 对象: {other.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}, Time.time: {Time.time}");
        
        if (!affectPlayer)
            return;

        int otherLayer = other.gameObject.layer;

        // 检查是否是玩家
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug)
                Debug.Log($"[HealMap] 检测到玩家进入 - 对象: {other.name}");
            StartHealing(other.gameObject);
        }
        else if (enableDebug)
        {
            Debug.Log($"[HealMap] 非玩家对象进入（已忽略） - 对象: {other.name}, Layer: {LayerMask.LayerToName(otherLayer)}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!affectPlayer)
            return;

        int otherLayer = other.gameObject.layer;

        // 检查是否是玩家
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            StopHealing(other.gameObject);
        }
    }

    private void StartHealing(GameObject obj)
    {
        UpdateVisualStateHeal(true);
        
        if (enableDebug)
            Debug.Log($"[HealMap] StartHealing 被调用 - 对象: {obj.name}, isInitialized: {isInitialized}, Time.time: {Time.time}");
        
        // 播放进入回血区音效（只有在初始化完成后才播放）
        if (isInitialized && audioSource != null && enterHealZoneSound != null)
        {
            audioSource.PlayOneShot(enterHealZoneSound);
            if (enableDebug)
                Debug.Log($"[HealMap] ? 进入回血区音效已播放 - 对象: {obj.name}");
        }
        else if (!isInitialized && enableDebug)
        {
            Debug.Log($"[HealMap] ? 初始化未完成，跳过播放音效 - 对象: {obj.name}");
        }
        
        // 检查玩家是否有 PlayerHealth 组件
        var playerHealth = obj.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            if (enableDebug)
                Debug.LogWarning($"HealMap: {obj.name} 没有找到 PlayerHealth 组件，无法回血。");
            return;
        }

        // 如果已经在回血，不重复启动
        if (healingCoroutines.ContainsKey(obj))
        {
            if (enableDebug)
                Debug.Log($"HealMap: {obj.name} 已经在回血区域中。");
            return;
        }

        // 启动持续回血协程
        Coroutine healCoroutine = StartCoroutine(HealOverTime(obj, playerHealth));
        healingCoroutines.Add(obj, healCoroutine);

        if (enableDebug)
            Debug.Log($"HealMap: {obj.name} 进入回血区域，开始持续回血。");
    }

    private void StopHealing(GameObject obj)
    {
        UpdateVisualStateHeal(false);
        // 停止回血协程
        if (healingCoroutines.TryGetValue(obj, out Coroutine healCoroutine))
        {
            if (healCoroutine != null)
            {
                StopCoroutine(healCoroutine);
            }

            healingCoroutines.Remove(obj);

            if (enableDebug)
                Debug.Log($"HealMap: {obj.name} 离开回血区域，停止回血。");
        }
    }

    private IEnumerator HealOverTime(GameObject obj, PlayerHealth playerHealth)
    {
        while (true)
        {
            // 检查是否已达最大生命值
            if (StatsManager.Instance != null && 
                StatsManager.Instance.currentHealth < StatsManager.Instance.maxHealth)
            {
                playerHealth.ChangeHealth(healAmount);
                
                // 播放回血音效
                if (audioSource != null && healSound != null)
                {
                    audioSource.PlayOneShot(healSound);
                    if (enableDebug)
                        Debug.Log($"[HealMap] 播放音效: 回血 - {healSound.name}");
                }

                if (enableDebug)
                    Debug.Log($"HealMap: {obj.name} 回血 {healAmount} 点，当前生命值: {StatsManager.Instance.currentHealth}/{StatsManager.Instance.maxHealth}");
            }
            else if (enableDebug)
            {
                Debug.Log($"HealMap: {obj.name} 生命值已满，暂停回血。");
            }

            // 等待指定的回血间隔
            yield return new WaitForSeconds(healInterval);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }

    private void OnDisable()
    {
        // 当脚本被禁用时，停止所有回血协程
        foreach (var kvp in healingCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        healingCoroutines.Clear();
    }

    void UpdateVisualStateHeal(bool isActive)
    {
        if (anim != null)
        {
            // 这里的 "IsActive" 要跟你 Animator 里定义的参数名一致
            anim.SetBool("IsHeal", isActive);
        }
    }
}
