using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    static string IsIdling = "IsIdling";
    static string IsWalking = "IsWalking";
    static string IsAttacking = "IsAttacking";
    static string IsStunned = "IsStunned";

    public float attacCoolDown = 1f;
    public float attackCoolDownTimer = 1f;
    public float playerRange = 5f;
    public Transform dectectionPoint;
    public LayerMask playerLayer;

    public float speed = 1.5f;
    private int facingDirection = 1;
    public int attackrange = 2;

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
        // 只有非击退、非清醒状态才执行逻辑
        if (enemyState != EnemyState.knockback && enemyState != EnemyState.dreamshatter)
        {
            CheckForPlayer();

            if (attackCoolDownTimer > 0)
            {
                attackCoolDownTimer -= Time.deltaTime;
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

            // 1. 攻击判定：距离够近 且 冷却好了 且 当前没有在攻击中
            if (distance <= attackrange && attackCoolDownTimer <= 0 && enemyState != EnemyState.attack)
            {
                // 注意：这里不要急着重置冷却时间，最好在 FinishAttack 里重置，或者在这里重置
                attackCoolDownTimer = attacCoolDown;
                rb.velocity = Vector2.zero;
                ChangeState(EnemyState.attack);
            }
            // 2. 追逐判定：距离远 且 没在攻击
            else if (distance > attackrange && enemyState != EnemyState.attack)
            {
                ChangeState(EnemyState.chase);
            }
        }
        else
        {
            // 没人时切回 Idle
            if (enemyState != EnemyState.knockback && enemyState != EnemyState.dreamshatter && enemyState != EnemyState.attack)
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
        else if (enemyState == EnemyState.dreamshatter)
        {
            rb.velocity = Vector2.zero;
            sr.color = Color.magenta;
        }
    }

    // ★★★ 关键修改：新增这个方法 ★★★
    // 必须在 Attack 动画的最后一帧添加 Animation Event 调用此方法
    public void FinishAttack()
    {
        // 强制把状态切回 Idle，这样 Update 里的 CheckForPlayer 才能再次让敌人动起来
        ChangeState(EnemyState.idle);
    }

    /// <summary>
    /// 对敌人施加朝向某个点的力
    /// </summary>
    /// <param name="targetPoint">目标点位置</param>
    /// <param name="force">施加的力大小</param>
    public void ApplyForceToPoint(Vector2 targetPoint, float force)
    {
        if (rb == null) return;

        // 计算方向
        Vector2 direction = (targetPoint - (Vector2)transform.position).normalized;

        // 施加力
        rb.AddForce(direction * force, ForceMode2D.Force);
    }

    /// <summary>
    /// 对敌人施加朝向某个Transform的力
    /// </summary>
    /// <param name="target">目标Transform</param>
    /// <param name="force">施加的力大小</param>
    public void ApplyForceToPoint(Transform target, float force)
    {
        if (target != null)
        {
            ApplyForceToPoint(target.position, force);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (dectectionPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(dectectionPoint.position, playerRange);
    }
}

public enum EnemyState
{
    idle,
    chase,
    attack,
    knockback,
    dreamshatter
}