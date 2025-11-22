using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public Rigidbody2D rb;
    public Animator anim;
    public bool isWalking = false;
    private bool isKnockBack;
    public Player_Combat player_Combat;


    [Header("Ray Display Settings")]
    public bool showDirectionRay = true;
    public float rayLength = 2f;
    public Color rayColor = Color.red;

    [Header("Turn Settings")]
    public bool facingRight = true;

    private Vector2 lastMovementDirection;

    private void Update()
    {
        if (Input.GetButtonDown("Slash"))
        {
            player_Combat.Attack();
        }
    }

    // Start is called before the first frame update
    void FixedUpdate()
    {
        if (isKnockBack == false)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 movement = new Vector2(horizontal, vertical);

            if (movement != Vector2.zero)
            {
                anim.SetBool("isWalking", true);
                lastMovementDirection = movement.normalized;

                HandleFlip(horizontal);
            }
            else
            {
                anim.SetBool("isWalking", false);
            }

            rb.velocity = movement * speed;
        }

    }

    void HandleFlip(float horizontal)
    {
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
        isKnockBack = true;
        Vector2 direction = (transform.position - enemy.position).normalized;
        rb.velocity = direction * force;
        StartCoroutine(KnockbackCounter(stunTime));
    }

    IEnumerator KnockbackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        rb.velocity = Vector2.zero;
        isKnockBack = false;
    }
}
