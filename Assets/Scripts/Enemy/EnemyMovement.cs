using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    static string IsIdling = "IsIdling";
    static string IsWalking = "IsWalking";
    static string IsAttacking = "IsAttacking";
    static string IsStunned = "IsStunned";
    static string IsSkill2 = "IsSkill2"; // ★ 新增：技能2的动画参数

    [Header("普通攻击设置")]
    public float attacCoolDown = 1f;
    public float attackCoolDownTimer = 1f;
    public int attackrange = 2;

    [Header("Boss 技能 2 设置")] // ★ 新增：技能2的配置
    public bool hasSkill2 = false; // 是否拥有技能2 (普通怪不勾选)
    public float skill2CoolDown = 5f;
    public float skill2Timer = 0f;
    public float skill2Range = 4f; // 技能2通常范围大一点

    [Header("通用设置")]
    public float playerRange = 5f;
    public Transform dectectionPoint;
    public LayerMask playerLayer;

    public float speed = 1.5f;
    private int facingDirection = 1;


    public EnemyState enemyState;

    private Rigidbody2D rb;
    private Transform player;
    private Animator anim;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        ChangeState(EnemyState.idle);
    }

    void Update()
    {
        // ★ 修改：增加检测，如果正在放技能2，也不要移动或执行其他逻辑
        if (enemyState != EnemyState.knockback &&
            enemyState != EnemyState.dreamshatter &&
            enemyState != EnemyState.skill2)
        {
            CheckForPlayer();

            // 冷却计时
            if (attackCoolDownTimer > 0)
            {
                attackCoolDownTimer -= Time.deltaTime;
            }

            // ★ 新增：技能2冷却计时
            if (skill2Timer > 0)
            {
                skill2Timer -= Time.deltaTime;
            }

            if (enemyState == EnemyState.chase)
            {
                ChasePlayer();
            }
        }
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(dectectionPoint.position, playerRange, playerLayer);

        if (hits.Length > 0)
        {
            player = hits[0].transform;
            float distance = Vector2.Distance(transform.position, player.position);

            // ★★★ 核心修改：优先级判断逻辑 ★★★

            // 1. 优先判定 Skill 2 (如果是Boss 且 冷却好 且 距离够 且 没在攻击)
            if (hasSkill2 && distance <= skill2Range && skill2Timer <= 0 && enemyState != EnemyState.attack && enemyState != EnemyState.skill2)
            {
                skill2Timer = skill2CoolDown; // 重置冷却
                rb.velocity = Vector2.zero;   // 停下
                ChangeState(EnemyState.skill2);
            }
            // 2. 其次判定 普通攻击
            else if (distance <= attackrange && attackCoolDownTimer <= 0 && enemyState != EnemyState.attack && enemyState != EnemyState.skill2)
            {
                attackCoolDownTimer = attacCoolDown;
                rb.velocity = Vector2.zero;
                ChangeState(EnemyState.attack);
            }
            // 3. 最后判定 追逐
            else if (distance > attackrange && enemyState != EnemyState.attack && enemyState != EnemyState.skill2)
            {
                ChangeState(EnemyState.chase);
            }
        }
        else
        {
            // 没人时切回 Idle (增加 skill2 的排除)
            if (enemyState != EnemyState.knockback &&
                enemyState != EnemyState.dreamshatter &&
                enemyState != EnemyState.attack &&
                enemyState != EnemyState.skill2)
            {
                rb.velocity = Vector2.zero;
                ChangeState(EnemyState.idle);
            }
        }
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        // 简单的转向逻辑
        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            facingDirection *= -1;
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * speed;
    }

    public void ChangeState(EnemyState newState)
    {
        // 退出旧状态清理
        if (enemyState == EnemyState.idle) anim.SetBool(IsIdling, false);
        else if (enemyState == EnemyState.chase) anim.SetBool(IsWalking, false);
        else if (enemyState == EnemyState.attack) anim.SetBool(IsAttacking, false);
        else if (enemyState == EnemyState.skill2) anim.SetBool(IsSkill2, false); // ★ 清理技能2
        else if (enemyState == EnemyState.dreamshatter)
        {
            sr.color = Color.white;
        }

        enemyState = newState;

        // 进入新状态设置
        if (enemyState == EnemyState.idle)
        {
            anim.SetBool(IsIdling, true);
            rb.velocity = Vector2.zero;
        }
        else if (enemyState == EnemyState.chase)
        {
            anim.SetBool(IsWalking, true);
        }
        else if (enemyState == EnemyState.attack)
        {
            anim.SetBool(IsAttacking, true);
            rb.velocity = Vector2.zero;
        }
        else if (enemyState == EnemyState.skill2) // ★ 新增：设置技能2动画
        {
            anim.SetBool(IsSkill2, true);
            rb.velocity = Vector2.zero;
        }
        else if (enemyState == EnemyState.dreamshatter)
        {
            rb.velocity = Vector2.zero;
            sr.color = Color.magenta;
        }
    }

    public void FinishAttack()
    {
        ChangeState(EnemyState.idle);
    }

    // ★★★ 新增：技能2 结束方法 ★★★
    // 必须在 Skill2 动画的最后一帧添加 Animation Event 调用此方法
    public void FinishSkill2()
    {
        ChangeState(EnemyState.idle);
    }

    public void ApplyForceToPoint(Vector2 targetPoint, float force)
    {
        if (rb == null) return;
        Vector2 direction = (targetPoint - (Vector2)transform.position).normalized;
        rb.AddForce(direction * force, ForceMode2D.Force);
    }

    public void ApplyForceToPoint(Transform target, float force)
    {
        if (target != null) ApplyForceToPoint(target.position, force);
    }

    public void ApplyForceAwayFromPoint(Vector2 sourcePoint, float force)
    {
        if (rb == null) return;
        Vector2 direction = ((Vector2)transform.position - sourcePoint).normalized;
        rb.AddForce(direction * force, ForceMode2D.Force);
    }

    public void ApplyForceAwayFromPoint(Transform source, float force)
    {
        if (source != null) ApplyForceAwayFromPoint(source.position, force);
    }

    private void OnDrawGizmosSelected()
    {
        if (dectectionPoint == null) return;

        // 红色圈：普攻检测范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(dectectionPoint.position, playerRange);

        // ★ 黄色圈：技能2释放范围
        if (hasSkill2)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, skill2Range);
        }
    }
}

// ★ 修改：添加 skill2 状态
public enum EnemyState
{
    idle,
    chase,
    attack,
    skill2,
    knockback,
    dreamshatter
}