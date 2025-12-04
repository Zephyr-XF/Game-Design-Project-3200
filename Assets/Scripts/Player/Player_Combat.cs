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
    public int maxCombo = 3;            // 最大连击段数
    public float comboResetTimer = 1f; // 超过这个时间未攻击，连击重置
    public float minAttackInterval = 0.2f; // 防止玩家按键过快
    public string attackAnmiTrigger = "AttackTrigger";

    [Header("Poise / Impact Settings (新功能)")]
    public float basePoiseDamage = 5f;   // 普攻削韧值
    public float skill1PoiseDamage = 20f; // 技能1削韧值 (高)
    public float skill2PoiseDamage = 40f; // 技能2削韧值 (极高，容易打出清醒状态)

    [Header("Skill 1 Settings")]
    public float skill1Cooldown = 5f;      
    public float skill1DamageMult = 2.0f;  
    public string skill1AnimTrigger = "Skill1Trigger"; 

    [Header("Skill 2 Settings")]
    public float skill2Cooldown = 8f;      
    public float skill2DamageMult = 3.0f;  
    public string skill2AnimTrigger = "Skill2Trigger"; 

    private int currentComboStep = 0;  
    private float lastAttackTime = 0;  
    private float nextAttackAllowedTime = 0; 

    private float skill1Timer = 0; 
    private float skill2Timer = 0; 

    private AttackType currentAttackType = AttackType.None; 

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
        if (Time.time < nextAttackAllowedTime || IsCastingSkill()) return;

        currentAttackType = AttackType.BasicCombo; 

        currentComboStep++;
        if (currentComboStep > maxCombo) currentComboStep = 1;

        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger(attackAnmiTrigger);

        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + minAttackInterval;
    }

    public void CastSkill1()
    {
        if (skill1Timer > 0 || IsCastingSkill()) return;

        currentAttackType = AttackType.Skill1; 
        skill1Timer = skill1Cooldown;          
        ResetCombo();
        anim.SetTrigger(skill1AnimTrigger);
    }

    public void CastSkill2()
    {
        if (skill2Timer > 0 || IsCastingSkill()) return;

        currentAttackType = AttackType.Skill2; 
        skill2Timer = skill2Cooldown;          
        ResetCombo();
        anim.SetTrigger(skill2AnimTrigger);
    }

    // --- 核心修改：造成伤害逻辑 ---
    public void DealDamage()
    {
        if(statsUI != null) statsUI.UpdateDamage(); // 保护判空

        // 获取范围内的敌人
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        foreach (Collider2D enemy in enemies)
        {
            // 获取新的 EnemyHealth 脚本 (之前修改过的)
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            EnemyKonckBack knockback = enemy.GetComponent<EnemyKonckBack>();

            if (health != null)
            {
                // 1. 基础数值准备
                float damageToDeal = StatsManager.Instance.damage;
                float poiseToDeal = basePoiseDamage; // 默认削韧

                // 2. 根据攻击类型计算 伤害倍率 和 削韧值
                switch (currentAttackType)
                {
                    case AttackType.BasicCombo:
                        // 普攻连击最后一下伤害和削韧都提高
                        if (currentComboStep == maxCombo) 
                        {
                            damageToDeal *= 1.5f;
                            poiseToDeal *= 1.5f; 
                        }
                        break;

                    case AttackType.Skill1:
                        damageToDeal *= skill1DamageMult;
                        poiseToDeal = skill1PoiseDamage; // 使用技能设定值
                        break;

                    case AttackType.Skill2:
                        damageToDeal *= skill2DamageMult;
                        poiseToDeal = skill2PoiseDamage; // 使用技能设定值
                        break;
                }

                // 3. 调用新的 TakeDamage 方法 (传入 int 伤害 和 float 削韧)
                health.TakeDamage((int)damageToDeal, poiseToDeal);
            }

            // 4. 处理击退 (击退逻辑保持不变，或者你可以让技能造成更强的击退)
            if (knockback != null)
            {
                float forceMult = (currentAttackType == AttackType.Skill1 || currentAttackType == AttackType.Skill2) ? 1.5f : 1f;
                // 注意：如果 StatsManager 没有 knockbackTime，请替换为具体数值或在 StatsManager 中添加
                knockback.Knockback(transform, StatsManager.Instance.knockbackForce * forceMult, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
            }
        }
    }

    private bool IsCastingSkill()
    {
        return currentAttackType == AttackType.Skill1 || currentAttackType == AttackType.Skill2;
    }

    public void FinishAttacking()
    {
        currentAttackType = AttackType.None; 
    }

    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / skill2Cooldown);

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        // 注意：如果 StatsManager 在编辑器模式下报错，可以加个判空或者写死一个半径用于Debug
        float range = (StatsManager.Instance != null) ? StatsManager.Instance.weaponRange : 1.5f;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }
}