using UnityEngine;
using UnityEngine.UI;

public class EnemyHUD : MonoBehaviour
{
    [Header("UI 组件")]
    public Image healthFill;     // 拖入红色的血条 Image (必须是 Filled 类型)
    public Image resilienceFill; // 拖入黄色的韧性条 Image (必须是 Filled 类型)
    public GameObject canvasGameObject; // 可选：用于隐藏整个血条

    // 缓存数据
    private EnemyHealth enemyHealth;
    private Camera mainCam;
    private Transform targetPoint; // ★ 新增：实际跟随的目标点

    // ★ 修改 Setup：接收目标点
    public void Setup(EnemyHealth healthScript, Transform pointToFollow)
    {
        enemyHealth = healthScript;
        mainCam = Camera.main; // 获取主摄像机

        // 设定跟随逻辑
        if (pointToFollow != null)
        {
            targetPoint = pointToFollow;
        }
        else
        {
            // 如果你在 Inspector 里忘了拖拽挂载点，这里做一个保底：跟随怪物脚底
            targetPoint = healthScript.transform;
            Debug.LogWarning($"怪物 {healthScript.name} 没有设置 HealthBarPoint，血条将显示在脚底。");
        }
    }

    void LateUpdate()
    {
        // 如果怪物本体没了，立刻销毁血条
        if (enemyHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. UI跟随目标点 (Position)
        // 这里的 targetPoint 就是你在怪物头顶放置的那个子物体
        if (targetPoint != null)
        {
            transform.position = targetPoint.position;
        }

        // 2. UI始终面向摄像机 (Rotation - 广告牌效果)
        // 这样可以防止怪物转身时，血条变薄或者翻转
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }

        // 3. 更新血条 (使用 fillAmount 0~1)
        if (healthFill != null)
        {
            // 防止除以0错误
            if (enemyHealth.maxHealth > 0)
            {
                float hpRatio = (float)enemyHealth.currentHealth / enemyHealth.maxHealth;
                healthFill.fillAmount = hpRatio;
            }
        }

        // 4. 更新韧性条
        if (resilienceFill != null)
        {
            if (enemyHealth.maxResilience > 0)
            {
                float resRatio = enemyHealth.currentResilience / enemyHealth.maxResilience;
                resilienceFill.fillAmount = resRatio;
            }
        }
    }
}