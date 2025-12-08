using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    // --- 修改 ---: 在枚举中添加 Skill3
    public enum AttackType { None, BasicCombo, Skill1, Skill2, Skill3 }

    [Header("Combat References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public Animator anim;

    [Header("Combo Settings")]
    public int maxCombo = 3;
    public float comboResetTimer = 1f;
    public float minAttackInterval = 0.2f;
    public string attackAnmiTrigger = "AttackTrigger";

    [Header("Skill 1 Settings")]
    public float skill1Cooldown = 5f;
    public float skill1DamageMult = 2.0f;
    public string skill1AnimTrigger = "Skill1Trigger";

    [Header("Skill 2 Settings")]
    public float skill2Cooldown = 8f;
    public float skill2DamageMult = 3.0f;
    public string skill2AnimTrigger = "Skill2Trigger";

    // --- 新增 ---: 技能3的设置
    [Header("Skill 3 Settings")]
    public float skill3Cooldown = 12f;     // 技能3冷却时间
    public float skill3DamageMult = 4.0f;  // 技能3伤害倍率
    public string skill3AnimTrigger = "Skill3Trigger"; // 动画机里的Trigger名字

    private int currentComboStep = 0;
    private float lastAttackTime = 0;
    private float nextAttackAllowedTime = 0;

    private float skill1Timer = 0;
    private float skill2Timer = 0;
    private float skill3Timer = 0; // --- 新增 ---: 技能3冷却计时器

    private AttackType currentAttackType = AttackType.None;

    private void Update()
    {
        // --- 修改 ---: 扩展时停状态下的输入逻辑
        if (PlayerBreakEffectManager.Instance.IsTimeFrozen)
        {
            // 在时停时，根据按下的技能键来触发对应的破防技
            if (Input.GetKeyDown(KeyCode.K)) ExecuteBreakSkill(AttackType.Skill1);
            else if (Input.GetKeyDown(KeyCode.L)) ExecuteBreakSkill(AttackType.Skill2);
            else if (Input.GetKeyDown(KeyCode.U)) ExecuteBreakSkill(AttackType.Skill3);
            return; // 阻止其他 Update 逻辑
        }

        // 连击重置
        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }

        // 技能冷却
        if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0) skill3Timer -= Time.deltaTime; // --- 新增 ---

        // 输入检测
        if (Input.GetKeyDown(KeyCode.J)) Attack();
        if (Input.GetKeyDown(KeyCode.K)) CastSkill1();
        if (Input.GetKeyDown(KeyCode.L)) CastSkill2();
        if (Input.GetKeyDown(KeyCode.U)) CastSkill3(); // --- 新增 ---
    }

    public void Attack()
    {
        // --- 修改 ---: 使用新的状态检查方法
        if (Time.time < nextAttackAllowedTime || IsCurrentlyAttacking()) return;

        currentAttackType = AttackType.BasicCombo;

        currentComboStep++;
        if (currentComboStep > maxCombo) currentComboStep = 1;

        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger(attackAnmiTrigger);

        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + minAttackInterval;
    }

    #region Skill Casting
    // --- 技能1 逻辑 ---
    public void CastSkill1()
    {
        // --- 修改 ---: 使用新的状态检查方法
        if (skill1Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill1();
    }

    // 分离出核心施法逻辑，以便被强制调用
    private void PerformSkill1()
    {
        currentAttackType = AttackType.Skill1;
        skill1Timer = skill1Cooldown;
        ResetCombo();
        anim.SetTrigger(skill1AnimTrigger);
    }

    // --- 技能2 逻辑 ---
    public void CastSkill2()
    {
        // --- 修改 ---: 使用新的状态检查方法
        if (skill2Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill2();
    }

    // --- 新增 ---: 分离出技能2的核心施法逻辑
    private void PerformSkill2()
    {
        currentAttackType = AttackType.Skill2;
        skill2Timer = skill2Cooldown;
        ResetCombo();
        anim.SetTrigger(skill2AnimTrigger);
    }

    // --- 新增 ---: 技能3的完整逻辑
    public void CastSkill3()
    {
        if (skill3Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill3();
    }

    private void PerformSkill3()
    {
        currentAttackType = AttackType.Skill3;
        skill3Timer = skill3Cooldown;
        ResetCombo();
        anim.SetTrigger(skill3AnimTrigger);
    }
    #endregion

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
                bool wasResilientBeforeAttack = health.currentResilience > 0;

                float damageToDeal = StatsManager.Instance.damage;
                float impactToDeal = StatsManager.Instance.impact;

                // --- 修改 ---: 在switch中加入Skill3的伤害计算
                switch (currentAttackType)
                {
                    case AttackType.BasicCombo:
                        if (currentComboStep == maxCombo) damageToDeal *= 1.5f;
                        break;
                    case AttackType.Skill1:
                        damageToDeal *= skill1DamageMult;
                        break;
                    case AttackType.Skill2:
                        damageToDeal *= skill2DamageMult;
                        break;
                    case AttackType.Skill3: // --- 新增 ---
                        damageToDeal *= skill3DamageMult;
                        break;
                }

                health.TakeDamage((int)damageToDeal, impactToDeal);

                if (wasResilientBeforeAttack && health.currentResilience <= 0)
                {
                    // 就是这一击打破了韧性！触发特写！
                    if (PlayerBreakEffectManager.Instance != null)
                    {
                        // 使用 this.transform (即玩家自己) 作为聚焦目标
                        PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);

                        // 【重要优化】因为特写已经触发，我们可以立即跳出循环，
                        // 避免一击打破多个敌人时重复触发特写，同时也更高效。
                        break;
                    }
                }
            }

            if (knockback != null)
            {
                // --- 修改 ---: 击退逻辑也包含技能3
                float forceMult = (currentAttackType != AttackType.None && currentAttackType != AttackType.BasicCombo) ? 1.5f : 1f;
                knockback.Knockback(transform, StatsManager.Instance.knockbackForce * forceMult, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
            }
        }
    }

    public bool IsCurrentlyAttacking()
    {
        // 只要当前攻击类型不是None，就意味着玩家正处于一个动作中，不能移动
        return currentAttackType != AttackType.None;
    }

    /// <summary>
    /// 【重要】这个方法需要绑定到【所有攻击和技能动画】的【最后一帧】
    /// 动画结束时调用，将玩家状态重置为 None，允许进行下一个动作或移动。
    /// </summary>
    public void FinishAttacking()
    {
        currentAttackType = AttackType.None;
    }

    #region Cooldown UI
    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / skill2Cooldown);
    public float GetSkill3CooldownRatio() => Mathf.Clamp01(skill3Timer / skill3Cooldown); // --- 新增 ---
    #endregion

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    // private void OnDrawGizmosSelected()
    // {
    //     if (attackPoint == null) return;
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireSphere(attackPoint.position, StatsManager.Instance.weaponRange);
    // }

    #region Cinematic Break Effect
    public void TriggerCinematicEffect()
    {
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);
        }
    }

    // --- 修改 ---: 让函数可以处理所有技能
    void ExecuteBreakSkill(AttackType skillToExecute)
    {
        PlayerBreakEffectManager.Instance.UnlockTimeButKeepCamera();

        // 根据传入的技能类型，强制释放对应的技能
        switch (skillToExecute)
        {
            case AttackType.Skill1:
                PerformSkill1();
                break;
            case AttackType.Skill2:
                PerformSkill2();
                break;
            case AttackType.Skill3:
                PerformSkill3();
                break;
        }
    }

    /// <summary>
    /// 【重要】这个方法需要绑定到【所有破防演出技能】动画的【最后一帧】
    /// </summary>
    public void OnCinematicSkillFinished()
    {
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.RestoreCameraView();
        }
    }
    #endregion
}


