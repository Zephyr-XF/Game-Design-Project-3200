using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    static string IsIdling = "IsIdling";
    static string IsWalking = "IsWalking";
    static string IsAttacking = "IsAttacking";
    static string IsStunned = "IsStunned";
    static string IsSkill2 = "IsSkill2";
    static string IsDead = "IsDead";

    [Header("普通攻击设置")]
    public float attacCoolDown = 1f;
    public float attackCoolDownTimer = 1f;
    public int attackrange = 2;

    [Header("Boss 技能 2 设置")]
    public bool hasSkill2 = false;
    public float skill2CoolDown = 5f;
    public float skill2Timer = 0f;
    public float skill2Range = 4f;

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

    // ★★★ 新增 1：引用 Combat 脚本 ★★★
    private Enemy_Combat combatScript;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // ★★★ 新增 2：获取 Combat 组件 ★★★
        combatScript = GetComponent<Enemy_Combat>();

        ChangeState(EnemyState.idle);
    }

    void Update()
    {
        // 1. 如果已经死了，直接退出，不要进行任何检测或移动
        if (enemyState == EnemyState.dead) return;

        // 2. 正常状态逻辑 (非击退、非眩晕、非技能、非攻击)
        if (enemyState != EnemyState.knockback &&
            enemyState != EnemyState.dreamshatter &&
            enemyState != EnemyState.skill2 &&
            enemyState != EnemyState.attack)
        {
            // ★★★ 这里的代码之前被你不小心删掉了！需补回 ★★★

            // A. 检测周围有没有玩家 (决定是追逐还是攻击)
            CheckForPlayer();

            // B. 攻击冷却计时
            if (attackCoolDownTimer > 0)
            {
                attackCoolDownTimer -= Time.deltaTime;
            }

            // C. 技能2冷却计时
            if (skill2Timer > 0)
            {
                skill2Timer -= Time.deltaTime;
            }

            // D. 如果处于追逐状态，执行移动逻辑
            if (enemyState == EnemyState.chase)
            {
                ChasePlayer();
            }
        }
        // 3. 如果处于 攻击 或 技能2 状态，强制刹车防止滑步
        else if (enemyState == EnemyState.attack || enemyState == EnemyState.skill2)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(dectectionPoint.position, playerRange, playerLayer);

        if (hits.Length > 0)
        {
            player = hits[0].transform;
            float distance = Vector2.Distance(transform.position, player.position);

            // 1. 优先判定 Skill 2
            if (hasSkill2 && distance <= skill2Range && skill2Timer <= 0 && enemyState != EnemyState.attack && enemyState != EnemyState.skill2)
            {
                skill2Timer = skill2CoolDown;
                rb.velocity = Vector2.zero;
                ChangeState(EnemyState.skill2);
            }
            // 2. 其次判定 普通攻击
            else if (distance <= attackrange && attackCoolDownTimer <= 0 && enemyState != EnemyState.attack && enemyState != EnemyState.skill2)
            {
                attackCoolDownTimer = attacCoolDown;
                rb.velocity = Vector2.zero;

                // ★★★ 核心修改 3：必须调用 Combat 脚本的 StartAttack！ ★★★
                // 这一步才会触发 Trigger 和 AttackID 的切换
                if (combatScript != null)
                {
                    combatScript.StartAttack();
                }

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
            // 没人时切回 Idle
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

    // ... ChasePlayer 保持不变 ...
    private void ChasePlayer()
    {
        if (player == null) return;
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
        // 1. 退出旧状态清理
        if (enemyState == EnemyState.idle) anim.SetBool(IsIdling, false);
        else if (enemyState == EnemyState.chase) anim.SetBool(IsWalking, false);
        else if (enemyState == EnemyState.attack) anim.SetBool(IsAttacking, false);
        else if (enemyState == EnemyState.skill2) anim.SetBool(IsSkill2, false);
        // ★ 注意：死亡触发通常用 Trigger，这里如果用 Bool 也可以，只要不再切回其他状态即可

        enemyState = newState;

        // 2. 进入新状态设置
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
        else if (enemyState == EnemyState.skill2)
        {
            anim.SetBool(IsSkill2, true);
            rb.velocity = Vector2.zero;
        }
        // ★★★ 新增：死亡状态处理 ★★★
        else if (enemyState == EnemyState.dead)
        {
            // 播放死亡动画 (需要在 Animator 里设置 Trigger "Die" 或者 Bool "IsDead")
            anim.SetTrigger("Die");

            // 彻底停下
            rb.velocity = Vector2.zero;

            // ★ 关键：关掉碰撞体，防止玩家被尸体挡住路
            GetComponent<Collider2D>().enabled = false;
        }
    }

    // ... 下面的 Finish 方法保持不变 ...
    public void FinishAttack()
    {
        ChangeState(EnemyState.idle);
    }

    public void FinishSkill2()
    {
        ChangeState(EnemyState.idle);
    }

    // ... ApplyForce 和 Gizmos 保持不变 ...
    public void ApplyForceToPoint(Vector2 targetPoint, float force) { if (rb == null) return; Vector2 direction = (targetPoint - (Vector2)transform.position).normalized; rb.AddForce(direction * force, ForceMode2D.Force); }
    public void ApplyForceToPoint(Transform target, float force) { if (target != null) ApplyForceToPoint(target.position, force); }
    public void ApplyForceAwayFromPoint(Vector2 sourcePoint, float force) { if (rb == null) return; Vector2 direction = ((Vector2)transform.position - sourcePoint).normalized; rb.AddForce(direction * force, ForceMode2D.Force); }
    public void ApplyForceAwayFromPoint(Transform source, float force) { if (source != null) ApplyForceAwayFromPoint(source.position, force); }
    private void OnDrawGizmosSelected() { if (dectectionPoint == null) return; Gizmos.color = Color.red; Gizmos.DrawWireSphere(dectectionPoint.position, playerRange); if (hasSkill2) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, skill2Range); } }
}

public enum EnemyState
{
    idle,
    chase,
    attack,
    skill2,
    knockback,
    dreamshatter,
    dead // ★ 新增
}