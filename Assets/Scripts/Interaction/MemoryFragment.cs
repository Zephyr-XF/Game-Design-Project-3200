using UnityEngine;

public class MemoryFragment : MonoBehaviour
{
    [Header("Data")]
    [TextArea] public string memoryContent; // 直接存储文本内容
    public AudioClip memoryVoice; // 回忆时的配音
    [Tooltip("回忆奖励的Sanity值，0表示使用Manager默认值")]
    public int customSanityReward = 0;
    
    [Header("Settings")]
    public bool isShattered = false;
    public float interactionRange = 2.0f;
    public KeyCode interactKey = KeyCode.F;
    
    [Header("Visuals")]
    public Sprite detailImage; // UI里显示的高清大图
    public GameObject visualModel;
    public ParticleSystem shatterEffect;

    private Transform player;
    private bool playerInRange = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (isShattered) return;

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            playerInRange = dist <= interactionRange;
        }

        // 交互逻辑：打开UI
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // 确保没有其他UI干扰
            if (MemoryUI.Instance != null && !MemoryUI.Instance.panel.activeSelf)
            {
                MemoryUI.Instance.Open(this);
            }
        }
    }

    // 由 MemoryUI 直接调用 (文本显示完之后)
    public void PerformReminisceEffectOnly()
    {
        // 应用效果
        if (MemoryEffectManager.Instance != null)
        {
            MemoryEffectManager.Instance.ApplyReminisceEffects(customSanityReward);
        }

        // 直接销毁
        isShattered = true; 
        Destroy(gameObject);
    }

    // 由 MemoryUI 按钮调用
    public void PerformShatter()
    {
        if (isShattered) return;
        isShattered = true;

        if (MemoryEffectManager.Instance != null)
        {
            MemoryEffectManager.Instance.TriggerShatterEffect();
        }

        // 播放特效
        if (shatterEffect != null)
        {
            Instantiate(shatterEffect, transform.position, Quaternion.identity);
        }

        // 销毁自身
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
