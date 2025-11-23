using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    static string IsIdling = "IsIdling";
    static string IsWalking = "IsWalking";
    static string IsAttacking = "IsAttacking";


    public float attacCoolDown = 1f;
    public float attackCoolDownTimer = 1f;
    public float playerRange = 5f;
    public Transform dectectionPoint;
    public LayerMask playerLayer;

    public float speed = 1.5f;
    private int facingDirection = 1; // 1 for right, -1 for left
    public int attackrange = 2;

    public EnemyState enemyState;

    private Rigidbody2D rb;
    private Transform player;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        ChangeState(EnemyState.idle);
    }

    void Update()
    {
        if (enemyState != EnemyState.knockback)
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
            else if (enemyState == EnemyState.attack)
            {
                // Ensure the enemy stops moving when idle
            }
        }

    }
    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(dectectionPoint.position, playerRange, playerLayer);
        if (hits.Length > 0)
        {
            player = hits[0].transform;

            if (Vector2.Distance(transform.position, player.position) <= attackrange && attackCoolDownTimer <= 0)
            {
                attackCoolDownTimer = attacCoolDown;
                rb.velocity = Vector2.zero; // Stop moving when in attack range
                ChangeState(EnemyState.attack);
            }
            else if (Vector2.Distance(transform.position, player.position) > attackrange && enemyState != EnemyState.attack)
            {
                ChangeState(EnemyState.chase);
            }

        }
        else
        {
            rb.velocity = Vector2.zero; // Stop moving when player exits
            ChangeState(EnemyState.idle);
        }
    }



    private void ChasePlayer()
    {
        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {

            facingDirection *= -1;
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }

        // 移动
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * speed;
    }

    public void ChangeState(EnemyState newState)
    {
        // 关闭当前状态的动画
        if (enemyState == EnemyState.idle)
        {
            anim.SetBool(IsIdling, false);
        }
        else if (enemyState == EnemyState.chase)
        {
            anim.SetBool(IsWalking, false);
        }
        else if (enemyState == EnemyState.attack)
        {
            anim.SetBool(IsAttacking, false);
        }

        enemyState = newState;

        // 设置新状态的动画和行为
        if (enemyState == EnemyState.idle)
        {
            anim.SetBool(IsIdling, true);
            rb.velocity = Vector2.zero;  // ✅ 停止移动
        }
        else if (enemyState == EnemyState.chase)
        {
            anim.SetBool(IsWalking, true);
            // chase 状态下的移动由 ChasePlayer() 方法处理
        }
        else if (enemyState == EnemyState.attack)
        {
            anim.SetBool(IsAttacking, true);
            rb.velocity = Vector2.zero;  // ✅ 攻击时停止移动
        }
    }

    public void Attack()
    {
        // Attack logic here
        Debug.Log("Enemy attacks!");

        // 攻击完成后可以返回追逐状态或idle状态
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackrange)
        {
            // 如果玩家仍在攻击范围内，继续攻击或返回追逐
            ChangeState(EnemyState.chase);
        }
        else
        {
            ChangeState(EnemyState.idle);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (dectectionPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(dectectionPoint.position, playerRange);
    }
}

public enum EnemyState
{
    idle,
    chase,
    attack,
    knockback
}