using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyKonckBack : MonoBehaviour
{
    private Rigidbody2D rb;
    private EnemyMovement enemyMovement;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyMovement = GetComponent<EnemyMovement>();
    }

    public void Knockback(Transform playerTransform, float knockbackForce, float knockbackTime, float stunTime)
    {
        // ★关键修改：如果敌人已经处于“清醒状态(Dreamshatter)”，则忽略普通击退
        // 这样防止短时间的击退打断了长时间的处决机会
        if (enemyMovement.enemyState == EnemyState.dreamshatter)
        {
            return;
        }

        enemyMovement.ChangeState(EnemyState.knockback);
        StartCoroutine(StunTimer(knockbackTime, stunTime));
        Vector2 direction = (transform.position - playerTransform.position).normalized;
        rb.velocity = direction * knockbackForce;
    }

    IEnumerator StunTimer(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);

        // 只有当前状态还是 knockback 时才切回 idle
        // 防止协程运行期间状态被改变（例如突然被打入 Dreamshatter）
        if (enemyMovement.enemyState == EnemyState.knockback)
        {
            enemyMovement.ChangeState(EnemyState.idle);
        }
    }
}