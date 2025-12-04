using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("基础属性")]
    public int maxHealth = 100;
    public int currentHealth;
    public int expReward = 3;

    [Header("UI 设置")]
    public GameObject hudPrefab; // 拖入 EnemyHUD Prefab
    private EnemyHUD myHUD;

    // ★★★ 新增：血条挂载点 ★★★
    [Tooltip("在怪物子物体里创建一个空物体放在头顶，并拖到这里")]
    public Transform healthBarPoint;

    [Header("清醒值/韧性系统")]
    public float maxResilience = 50f; // 最大韧性
    public float currentResilience;
    public float shatterDuration = 5f; // 清醒状态持续时间
    public float shatterDamageMultiplier = 2f; // 清醒状态下的受伤倍率

    // 事件
    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;

    // 引用
    private EnemyMovement enemyMovement;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        currentHealth = maxHealth;
        currentResilience = maxResilience;
        enemyMovement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 生成血条
        if (hudPrefab != null)
        {
            // 1. 查找场景里的 WorldCanvas (确保你的Canvas叫这个名字)
            GameObject canvasObj = GameObject.Find("WorldCanvas");

            if (canvasObj != null)
            {
                // 生成在 Canvas 下面
                GameObject hudObj = Instantiate(hudPrefab, canvasObj.transform);

                // 获取 HUD 脚本
                myHUD = hudObj.GetComponent<EnemyHUD>();

                // ★★★ 关键修改：把 healthBarPoint 传过去 ★★★
                // 如果你忘了拖 healthBarPoint，这里传 null 进去，HUD脚本里会自动处理
                myHUD.Setup(this, healthBarPoint);

                // 重要：修正缩放 (防止生成后缩放变乱)
                hudObj.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("场景中找不到名为 'WorldCanvas' 的画布！请创建一个 RenderMode 为 World Space 的 Canvas。");
            }
        }
    }

    /// <summary>
    /// 玩家攻击时调用此方法
    /// </summary>
    /// <param name="damage">基础伤害</param>
    /// <param name="poiseDamage">削韧值(冲击力)</param>
    public void TakeDamage(int damage, float poiseDamage)
    {
        // 尝试播放受击特效
        if (TryGetComponent(out EnemyVisuals visuals))
        {
            visuals.PlayHitEffect();
        }

        // 1. 判断是否处于清醒(破防)状态
        bool isShattered = (enemyMovement.enemyState == EnemyState.dreamshatter);

        // 2. 计算最终伤害 (清醒状态下伤害翻倍)
        int finalDamage = isShattered ? Mathf.RoundToInt(damage * shatterDamageMultiplier) : damage;

        currentHealth -= finalDamage;
        // Debug.Log($"敌人受到伤害: {finalDamage} (倍率: {(isShattered ? shatterDamageMultiplier : 1)})");

        // 3. 死亡检测
        if (currentHealth <= 0)
        {
            if (OnMonsterDefeated != null) OnMonsterDefeated(expReward);
            if (myHUD != null) Destroy(myHUD.gameObject); // 销毁血条
            Destroy(gameObject); // 销毁怪物
            return;
        }

        // 4. 处理韧性逻辑 (只有在未处于清醒状态时才扣除韧性)
        if (!isShattered)
        {
            currentResilience -= poiseDamage;

            // 如果韧性归零，进入清醒状态
            if (currentResilience <= 0)
            {
                StartCoroutine(EnterDreamshatterState());
            }
        }
    }

    // 处理清醒状态的协程
    IEnumerator EnterDreamshatterState()
    {
        currentResilience = 0;
        // 切换到 Dreamshatter 状态
        enemyMovement.ChangeState(EnemyState.dreamshatter);
        Debug.Log(">>> 敌人进入清醒状态！无法移动且受到双倍伤害！");

        // 等待指定时间
        yield return new WaitForSeconds(shatterDuration);

        // 恢复状态
        currentResilience = maxResilience; // 韧性回满
        Debug.Log("<<< 敌人从清醒状态恢复！");

        // 切换回 Idle
        enemyMovement.ChangeState(EnemyState.idle);
    }
}