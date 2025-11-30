using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    [Header("Combat References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public Animator anim;

    [Header("Combo Settings")]
    public int maxCombo = 3;           // 最大连击段数
    public float comboResetTimer = 1f; // 超过这个时间未攻击，连击重置
    public float minAttackInterval = 0.2f; // 防止玩家按键过快（最小攻击间隔）

    private int currentComboStep = 0;  // 当前连击段数
    private float lastAttackTime = 0;  // 上次按下攻击键的时间
    private float nextAttackAllowedTime = 0; // 下一次允许攻击的时间点

    private void Update()
    {
        // 连击超时重置逻辑
        // 如果当前有连击，且距离上次攻击时间已经超过了允许的重置时间，则重置
        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }
    }

    public void Attack()
    {
        // 检查是否允许攻击（防止一秒钟按10次导致的动画鬼畜）
        if (Time.time < nextAttackAllowedTime) return;

        // 更新连击步数
        currentComboStep++;

        // 如果超过最大连击数，重置为1（或者根据需求重置为0）
        if (currentComboStep > maxCombo)
        {
            currentComboStep = 1;
        }

        // 发送参数给 Animator
        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger("AttackTrigger");

        // 更新时间记录
        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + minAttackInterval;
    }

    // 这个方法由动画事件(Animation Event)调用，在每一段攻击动画的“击中帧”添加此事件
    public void DealDamage()
    {

        statsUI.UpdateDamage();

        // 获取范围内所有敌人
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        // 遍历所有敌人造成伤害
        foreach (Collider2D enemy in enemies)
        {
            // 判空保护
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            EnemyKonckBack knockback = enemy.GetComponent<EnemyKonckBack>();

            if (health != null)
            {
                // 根据连击段数，这里甚至可以做伤害倍率（例如第3段伤害更高）
                int finalDamage = StatsManager.Instance.damage;
                if (currentComboStep == maxCombo) finalDamage = (int)(finalDamage * 1.5f); // 举例：终结技1.5倍伤害

                health.ChangeHealth(-finalDamage);
            }

            if (knockback != null)
            {
                knockback.Knockback(transform, StatsManager.Instance.knockbackForce, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
            }
        }
    }

    // 辅助方法：重置连击
    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    // 用于动画结束时的事件
    public void FinishAttacking()
    {

    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, StatsManager.Instance.weaponRange);
    }
}
