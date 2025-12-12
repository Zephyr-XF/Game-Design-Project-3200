using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("基础属性")]
    public int maxHealth = 100;
    public int currentHealth;
    public int expReward = 3;

    // ★★★ 新增：音效设置 ★★★
    [Header("音效设置")]
    public AudioClip hitSound;        // 受伤音效
    private AudioSource audioSource;

    [Header("UI 设置")]
    public GameObject hudPrefab;
    private EnemyHUD myHUD;

    // ★★★ 血条挂载点 ★★★
    [Tooltip("在怪物子物体里创建一个空物体放在头顶，并拖到这里")]
    public Transform healthBarPoint;

    [Header("清醒值/韧性系统")]
    public float maxResilience = 50f;
    public float currentResilience;
    public float shatterDuration = 5f;
    public float shatterDamageMultiplier = 2f;

    // 事件
    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;

    // 引用
    private EnemyMovement enemyMovement;
    private SpriteRenderer spriteRenderer;

    // ★★★ 新增：防止鞭尸标记 ★★★
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        currentResilience = maxResilience;
        enemyMovement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 初始化 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 生成血条 (逻辑保持不变)
        if (hudPrefab != null)
        {
            GameObject canvasObj = GameObject.Find("WorldCanvas");
            if (canvasObj != null)
            {
                GameObject hudObj = Instantiate(hudPrefab, canvasObj.transform);
                myHUD = hudObj.GetComponent<EnemyHUD>();
                myHUD.Setup(this, healthBarPoint);
                hudObj.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("场景中找不到名为 'WorldCanvas' 的画布！");
            }
        }
    }

    /// <summary>
    /// 玩家攻击时调用此方法
    /// </summary>
    public void TakeDamage(int damage, float poiseDamage)
    {
        // ★★★ 如果已经死了，直接忽略后续伤害，防止重复触发死亡逻辑 ★★★
        if (isDead) return;

        // 尝试播放受击特效
        if (TryGetComponent(out EnemyVisuals visuals))
        {
            visuals.PlayHitEffect();
        }

        // 播放受伤音效
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // 1. 判断是否处于清醒(破防)状态
        bool isShattered = (enemyMovement.enemyState == EnemyState.dreamshatter);

        // 2. 计算最终伤害
        int finalDamage = isShattered ? Mathf.RoundToInt(damage * shatterDamageMultiplier) : damage;

        currentHealth -= finalDamage;

        // ★★★ 3. 死亡检测 (关键修改) ★★★
        if (currentHealth <= 0)
        {
            Die(); // 调用专门的死亡方法
            return;
        }

        // 4. 处理韧性逻辑 (只有活着且未破防时才扣)
        if (!isShattered)
        {
            currentResilience -= poiseDamage;

            if (currentResilience <= 0)
            {
                StartCoroutine(EnterDreamshatterState());
            }
        }
    }

    // ★★★ 新增：专门处理死亡逻辑的方法 ★★★
    void Die()
    {
        isDead = true; // 标记已死

        // 1. 切换到死亡状态 (EnemyMovement 会负责播放动画和关掉碰撞体)
        if (enemyMovement != null)
        {
            enemyMovement.ChangeState(EnemyState.dead);
        }

        // 2. 发送奖励
        if (OnMonsterDefeated != null) OnMonsterDefeated(expReward);

        // 3. 销毁血条 UI (血条不需要等动画播完，现在就可以消失)
        if (myHUD != null) Destroy(myHUD.gameObject);

        // ★ 注意：不要在这里 Destroy(gameObject)，要等动画事件！
    }

    // ★★★ 这个方法必须由 Animator Event 调用 ★★★
    // 请在 Death 动画的最后一帧添加事件，Function 选择 DestroyEnemy
    public void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    // 处理清醒状态的协程 (保持不变)
    IEnumerator EnterDreamshatterState()
    {
        currentResilience = 0;
        enemyMovement.ChangeState(EnemyState.dreamshatter);
        Debug.Log(">>> 敌人进入清醒状态！");

        yield return new WaitForSeconds(shatterDuration);

        // 如果在这期间死了，就不需要恢复了
        if (!isDead)
        {
            currentResilience = maxResilience;
            Debug.Log("<<< 敌人从清醒状态恢复！");
            enemyMovement.ChangeState(EnemyState.idle);
        }
    }
}