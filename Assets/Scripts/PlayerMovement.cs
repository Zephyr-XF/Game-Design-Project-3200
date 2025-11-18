using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public Rigidbody2D rb;
    public Animator anim;

    public bool isWalking = false;

    [Header("射线显示设置")]
    public bool showDirectionRay = true;
    public float rayLength = 2f;
    public Color rayColor = Color.red;
    
    [Header("翻转设置")]
    public bool facingRight = true; // 角色默认朝向
    
    private Vector2 lastMovementDirection;

    // Start is called before the first frame update
    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 movement = new Vector2(horizontal, vertical);

        // 记录移动方向
        if (movement != Vector2.zero)
        {
            anim.SetBool("isWalking", true);
            lastMovementDirection = movement.normalized;
            
            // 处理翻转
            HandleFlip(horizontal);
        }
        else
        {
            anim.SetBool("isWalking", false);
        }

        rb.velocity = movement * speed;
    }
    
    void HandleFlip(float horizontal)
    {
        // 向右移动且当前朝左，或向左移动且当前朝右时翻转
        if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight))
        {
            Flip();
        }
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        
        // 通过缩放翻转
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    void Update()
    {
        // 绘制方向射线
        if (showDirectionRay && lastMovementDirection != Vector2.zero)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)lastMovementDirection * rayLength;
            
            Debug.DrawLine(startPos, endPos, rayColor);
        }
    }
}
