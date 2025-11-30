using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public enum AttackType { None, BasicCombo, Skill1, Skill2 }

    [Header("Combat References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public Animator anim;

    [Header("Combo Settings")]
    public int maxCombo = 3;           // 最大连击段数
    public float comboResetTimer = 1f; // 超过这个时间未攻击，连击重置
    public float minAttackInterval = 0.2f; // 防止玩家按键过快（最小攻击间隔）
    public string attackAnmiTrigger = "AttackTrigger";

    [Header("Skill 1 Settings")]
    public float skill1Cooldown = 5f;      // 技能1冷却时间
    public float skill1DamageMult = 2.0f;  // 技能1伤害倍率
    public string skill1AnimTrigger = "Skill1Trigger"; // 动画机里的Trigger名字

    [Header("Skill 2 Settings")]
    public float skill2Cooldown = 8f;      // 技能2冷却时间
    public float skill2DamageMult = 3.0f;  // 技能2伤害倍率
    public string skill2AnimTrigger = "Skill2Trigger"; // 动画机里的Trigger名字

    private int currentComboStep = 0;  // 当前连击段数
    private float lastAttackTime = 0;  // 上次按下攻击键的时间
    private float nextAttackAllowedTime = 0; // 下一次允许攻击的时间点

    private float skill1Timer = 0; // 技能1当前剩余冷却时间
    private float skill2Timer = 0; // 技能2当前剩余冷却时间

    private AttackType currentAttackType = AttackType.None; // 当前正在进行的攻击类型

    private void Update()
    {
        // 1. 处理普通攻击连击重置
        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }

        // 2. 处理技能冷却倒计时
        if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0) skill2Timer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.J)) Attack();      // 普攻
        if (Input.GetKeyDown(KeyCode.K)) CastSkill1();  // 技能1
        if (Input.GetKeyDown(KeyCode.L)) CastSkill2();  // 技能2
    }

    public void Attack()
    {
        // 如果正在放技能，或者攻击间隔未到，禁止普攻
        if (Time.time < nextAttackAllowedTime || IsCastingSkill()) return;

        currentAttackType = AttackType.BasicCombo; // 标记当前为普攻

        currentComboStep++;
        if (currentComboStep > maxCombo) currentComboStep = 1;

        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger(attackAnmiTrigger);

        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + minAttackInterval;
    }

    // --- 技能1 逻辑 ---
    public void CastSkill1()
    {
        // 检查冷却 & 是否允许攻击
        if (skill1Timer > 0 || IsCastingSkill()) return;

        currentAttackType = AttackType.Skill1; // 标记当前为技能1
        skill1Timer = skill1Cooldown;          // 重置冷却

        // 可以在这里重置连击段数，防止技能接普攻出现动画不连贯
        ResetCombo();

        anim.SetTrigger(skill1AnimTrigger);
    }

    // --- 技能2 逻辑 ---
    public void CastSkill2()
    {
        // 检查冷却 & 是否允许攻击
        if (skill2Timer > 0 || IsCastingSkill()) return;

        currentAttackType = AttackType.Skill2; // 标记当前为技能2
        skill2Timer = skill2Cooldown;          // 重置冷却

        ResetCombo();

        anim.SetTrigger(skill2AnimTrigger);
    }

    public void DealDamage()
    {
        statsUI.UpdateDamage();

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        foreach (Collider2D enemy in enemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            EnemyKonckBack knockback = enemy.GetComponent<EnemyKonckBack>();

            if (health != null)
            {
                // 获取基础伤害
                float damageToDeal = StatsManager.Instance.damage;

                // 根据当前攻击类型计算最终伤害
                switch (currentAttackType)
                {
                    case AttackType.BasicCombo:
                        // 普攻逻辑：最终段伤害加成
                        if (currentComboStep == maxCombo) damageToDeal *= 1.5f;
                        break;

                    case AttackType.Skill1:
                        damageToDeal *= skill1DamageMult;
                        break;

                    case AttackType.Skill2:
                        damageToDeal *= skill2DamageMult;
                        break;
                }

                health.ChangeHealth(-(int)damageToDeal);
            }

            if (knockback != null)
            {
                // 技能可能造成更强的击退（可选）
                float forceMult = (currentAttackType == AttackType.Skill1 || currentAttackType == AttackType.Skill2) ? 1.5f : 1f;
                knockback.Knockback(transform, StatsManager.Instance.knockbackForce * forceMult, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
            }
        }
    }

    private bool IsCastingSkill()
    {
        // 如果当前标记是技能，且还没被重置（说明动作还没做完或者刚开始）
        // 注意：这里逻辑比较简单，如果需要严格锁死输入，建议在 FinishAttacking 中重置 currentAttackType
        return currentAttackType == AttackType.Skill1 || currentAttackType == AttackType.Skill2;
    }

    public void FinishAttacking()
    {
        currentAttackType = AttackType.None; // 攻击动作结束，状态归零
        // 这里也可以把 nextAttackAllowedTime 稍微重置一下，允许立刻接下一个动作
    }

    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / skill2Cooldown);


    // 辅助方法：重置连击
    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, StatsManager.Instance.weaponRange);
    }
}
