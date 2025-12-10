using UnityEngine;

public class SkillDamage : MonoBehaviour
{
    private int damage;
    private float radius;
    private LayerMask targetLayer;
    private Transform bossTransform;

    // ★ 新增：允许你在 Inspector 里调整判定圈的位置偏移
    [Header("判定圈偏移调整")]
    public Vector2 hitOffset;

    public void Setup(int dmg, float rad, LayerMask layer, Transform boss)
    {
        this.damage = dmg;
        this.radius = rad;
        this.targetLayer = layer;
        this.bossTransform = boss;
    }

    public void TriggerExplosion()
    {
        // ★ 修改：判定中心加上偏移量
        Vector3 checkPos = transform.position + (Vector3)hitOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, radius, targetLayer);

        foreach (var hit in hits)
        {
            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ChangeHealth(-damage);
            }

            PlayerMovement movement = hit.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                // ★ 修改：击退方向也基于新的中心点
                movement.Knockback(transform, 5f, 0.2f);
            }
        }
    }

    public void Finish()
    {
        Destroy(gameObject);
    }

    // ★ 修改：辅助线也画在偏移后的位置，方便你在编辑器里对齐
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // 实时显示偏移后的圈圈
        Gizmos.DrawWireSphere(transform.position + (Vector3)hitOffset, radius > 0 ? radius : 1f);
    }
}