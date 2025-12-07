using UnityEngine;

public class MerchantNPC : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("此NPC对应的对话数据")]
    public DialogueDataSO dialogueData;
    
    [Tooltip("交互范围")]
    public float interactRange = 3f;
    
    [Tooltip("玩家层级（用于检测玩家是否在范围内）")]
    public LayerMask playerLayer;

    private bool isPlayerInRange = false;

    private void Update()
    {
        // 1. 检测玩家是否在范围内
        CheckPlayerInRange();

        // 2. 如果玩家在范围内，显示提示
        if (isPlayerInRange)
        {
            // 防止对话进行时还显示提示
            if (!DialogueManager.Instance.IsDialogueActive())
            {
                DialogueManager.Instance.ShowInteractHint(true);
            }
        }
        else
        {
            // 玩家离开，隐藏提示
            DialogueManager.Instance.ShowInteractHint(false);
        }

        // 3. 处理输入
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            // 如果对话还没开始，就开始对话
            if (!DialogueManager.Instance.IsDialogueActive())
            {
                DialogueManager.Instance.StartDialogue(dialogueData);
            }
        }
    }

    private void CheckPlayerInRange()
    {
        // 简单的范围检测：画一个圆，看看有没有玩家
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, playerLayer);
        isPlayerInRange = (hit != null);
    }

    // 在编辑器里画出范围圈，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
