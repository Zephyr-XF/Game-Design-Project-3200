using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerAudio playerAudio;
    public Rigidbody2D rb;
    public Animator anim;
    private bool isKnockBack;
    public Player_Combat player_Combat;

    [Header("Run Settings")]
    public float runMultiplier = 1.5f;
    private bool isRunning = false;

    // --- 冲刺设置 (Dash Settings) ---
    [Header("Dash Settings")]
    public float dashSpeed = 10f;        // 冲刺速度
    public float dashDuration = 0.2f;    // 冲刺持续时间
    public float dashCooldown = 1f;      // 冲刺冷却时间
    private bool isDashing;              // 当前是否正在冲刺中
    private bool canDash = true;         // 当前是否可以冲刺（冷却是否就绪）
    private Vector2 dashDirection;       // 记录冲刺那一瞬间的方向

    [Header("Ray Display Settings")]
    public bool showDirectionRay = true;
    public float rayLength = 2f;
    public Color rayColor = Color.red;

    [Header("Turn Settings")]
    public bool facingRight = true;

    private Vector2 lastMovementDirection;

    void Start()
    {
        player_Combat = GetComponent<Player_Combat>();
        playerAudio = GetComponent<PlayerAudio>();
    }

    private void Update()
    {
        // --- 新增：冲刺输入检测 ---
        if (Input.GetKeyDown(KeyCode.Space) && canDash && !isKnockBack)
        {
            StartCoroutine(Dash());
        }

        // 如果正在冲刺，就不需要检测跑步逻辑了
        if (isDashing) return;

        // 检测跑步按键 (左Shift 或 右Shift)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
    }

    void FixedUpdate()
    {
        // --- 如果正在冲刺，跳过普通移动逻辑 ---
        if (isDashing)
        {
            playerAudio.ManageFootsteps(false, false);
            return; // 冲刺由协程控制，这里直接返回，或者在这里赋予刚体速度
        }

        if (player_Combat.IsCurrentlyAttacking())
        {
            rb.velocity = Vector2.zero;
            // 确保移动动画也被关闭
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", false);
            playerAudio.ManageFootsteps(false, false);
            return;
        }
        // ---------------------------------------

        if (isKnockBack == false)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 movement = new Vector2(horizontal, vertical).normalized;

            bool isMoving = movement != Vector2.zero;
            playerAudio.ManageFootsteps(isMoving, isRunning);


            if (isMoving)
            {
                anim.SetBool("isWalking", true);
                anim.SetBool("isRunning", isRunning);

                lastMovementDirection = movement; // 记录最后的移动方向

                HandleFlip(horizontal);
            }
            else
            {
                anim.SetBool("isWalking", false);
                anim.SetBool("isRunning", false);
            }

            float currentSpeed = StatsManager.Instance.speed;

            if (isRunning && movement != Vector2.zero)
            {
                currentSpeed *= runMultiplier;
            }

            rb.velocity = movement * currentSpeed;
        }
        else
        {
            // 被击退时停止脚步声
            playerAudio.ManageFootsteps(false, false);
        }
    }

    // --- 冲刺协程逻辑 ---
    private IEnumerator Dash()
    {
        canDash = false; // 进入冷却
        isDashing = true; // 标记为正在冲刺

        playerAudio.PlayDash();

        // 确定冲刺方向：如果有输入则按输入方向，没有输入则按最后移动方向或面朝方向
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(horizontal, vertical).normalized;

        if (inputDir != Vector2.zero)
        {
            dashDirection = inputDir;
        }
        else
        {
            // 如果玩家站着不动按冲刺，可以选择向前冲，或者向最后移动方向冲
            // 这里使用最后一次移动的方向，如果从未移动过，则根据面朝方向决定
            if (lastMovementDirection == Vector2.zero)
                dashDirection = facingRight ? Vector2.right : Vector2.left;
            else
                dashDirection = lastMovementDirection;
        }

        // 给刚体施加冲刺速度
        rb.velocity = dashDirection * dashSpeed;

        anim.SetTrigger("Dash");

        // --- 冲刺过程 ---
        yield return new WaitForSeconds(dashDuration);

        isDashing = false; // 冲刺结束，恢复控制权
        rb.velocity = Vector2.zero; // 可选：冲刺结束后立即停下，增加打击感

        // --- 冷却过程 ---
        yield return new WaitForSeconds(dashCooldown);
        canDash = true; // 冷却结束，可以再次冲刺
    }

    void HandleFlip(float horizontal)
    {
        // 防止冲刺时意外翻转
        if (isDashing) return;

        if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight))
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void Knockback(Transform enemy, float force, float stunTime)
    {
        // 如果正在冲刺，可能免疫击退，或者被击退打断，这里选择打断冲刺
        isDashing = false;
        isKnockBack = true;

        Vector2 direction = (transform.position - enemy.position).normalized;
        rb.velocity = direction * force;
        StartCoroutine(KnockbackCounter(stunTime * 0));
    }

    IEnumerator KnockbackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        rb.velocity = Vector2.zero;
        isKnockBack = false;
    }
}
