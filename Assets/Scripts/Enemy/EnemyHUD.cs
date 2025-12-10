using UnityEngine;
using UnityEngine.UI;

public class EnemyHUD : MonoBehaviour
{
    [Header("UI 组件")]
    public Image healthFill;      // 红色血条
    public Image resilienceFill;  // 黄色韧性条

    // ★★★ 新增：Awake 提示文字对象 ★★★
    [Tooltip("拖入显示 'Awake!' 文字的那个 GameObject")]
    public GameObject awakeTipObject;

    public GameObject canvasGameObject;

    // 缓存数据
    private EnemyHealth enemyHealth;
    private Camera mainCam;
    private Transform targetPoint;

    public void Setup(EnemyHealth healthScript, Transform pointToFollow)
    {
        enemyHealth = healthScript;
        mainCam = Camera.main;

        if (pointToFollow != null)
        {
            targetPoint = pointToFollow;
        }
        else
        {
            targetPoint = healthScript.transform;
        }

        // ★ 初始化时先隐藏提示文字
        if (awakeTipObject != null)
        {
            awakeTipObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (enemyHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. UI跟随
        if (targetPoint != null) transform.position = targetPoint.position;

        // 2. UI面向摄像机
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;

        // 3. 更新血条
        if (healthFill != null && enemyHealth.maxHealth > 0)
        {
            healthFill.fillAmount = (float)enemyHealth.currentHealth / enemyHealth.maxHealth;
        }

        // 4. 更新韧性条
        if (resilienceFill != null && enemyHealth.maxResilience > 0)
        {
            float resRatio = enemyHealth.currentResilience / enemyHealth.maxResilience;
            resilienceFill.fillAmount = resRatio;
        }

        // ★★★ 5. 新增：控制 Awake 提示显示 ★★★
        if (awakeTipObject != null)
        {
            // 如果韧性归零 (处于清醒状态)，显示文字；否则隐藏
            bool isAwake = enemyHealth.currentResilience <= 0;

            // 只有状态改变时才调用 SetActive (优化性能)
            if (awakeTipObject.activeSelf != isAwake)
            {
                awakeTipObject.SetActive(isAwake);
            }
        }
    }
}