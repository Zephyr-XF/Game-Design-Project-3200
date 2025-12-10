using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public enum AttackType { None, BasicCombo, Skill1, Skill2, Skill3 }

    [Header("Combat References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public Animator anim;

    // --- 技能和连击的数值设置已移至 StatsManager ---
    [Header("Combat Timing Settings")]
    public float comboResetTimer = 1f;
    public float minAttackInterval = 0.2f;

    [Header("Animation Triggers")]
    public string attackAnmiTrigger = "AttackTrigger";
    public string skill1AnimTrigger = "Skill1Trigger";
    public string skill2AnimTrigger = "Skill2Trigger";
    public string skill3AnimTrigger = "Skill3Trigger";


    private PlayerAudio playerAudio;
    private int currentComboStep = 0;
    private float lastAttackTime = 0;
    private float nextAttackAllowedTime = 0;

    private float skill1Timer = 0;
    private float skill2Timer = 0;
    private float skill3Timer = 0;

    private AttackType currentAttackType = AttackType.None;

    private void Awake()
    {
        playerAudio = GetComponent<PlayerAudio>();
        if (playerAudio == null)
        {
            Debug.LogError("PlayerAudio component not found on Player!");
        }
    }

    private void Update()
    {
        // 检查是否处于电影化破防的时间冻结状态
        if (PlayerBreakEffectManager.Instance != null && PlayerBreakEffectManager.Instance.IsTimeFrozen)
        {
            if (Input.GetKeyDown(KeyCode.K)) ExecuteBreakSkill(AttackType.Skill1);
            else if (Input.GetKeyDown(KeyCode.L)) ExecuteBreakSkill(AttackType.Skill2);
            else if (Input.GetKeyDown(KeyCode.U)) ExecuteBreakSkill(AttackType.Skill3);
            return; // 在时间冻结时，不执行后续逻辑
        }

        // 连击重置逻辑
        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }

        // 技能冷却计时
        if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0) skill3Timer -= Time.deltaTime;

        if (SkillsCooldownUIManager.Instance != null)
        {
            // 计算并更新技能1的UI
            float skill1Ratio = (StatsManager.Instance.skill1Cooldown > 0)
                ? skill1Timer / StatsManager.Instance.skill1Cooldown
                : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(0, skill1Ratio);

            // 计算并更新技能2的UI
            float skill2Ratio = (StatsManager.Instance.skill2Cooldown > 0)
                ? skill2Timer / StatsManager.Instance.skill2Cooldown
                : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(1, skill2Ratio);

            // 计算并更新技能3的UI
            float skill3Ratio = (StatsManager.Instance.skill3Cooldown > 0)
                ? skill3Timer / StatsManager.Instance.skill3Cooldown
                : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(2, skill3Ratio);
        }

        // 玩家输入检测
        if (Input.GetKeyDown(KeyCode.J)) Attack();
        if (Input.GetKeyDown(KeyCode.K)) CastSkill1();
        if (Input.GetKeyDown(KeyCode.L)) CastSkill2();
        if (Input.GetKeyDown(KeyCode.U)) CastSkill3();
    }

    public void Attack()
    {
        if (Time.time < nextAttackAllowedTime || IsCurrentlyAttacking()) return;

        currentAttackType = AttackType.BasicCombo;

        currentComboStep++;
        // 从 StatsManager 获取最大连击数
        if (currentComboStep > StatsManager.Instance.maxCombo)
        {
            currentComboStep = 1;
        }

        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger(attackAnmiTrigger);

        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + minAttackInterval;
    }

    #region Skill Casting
    public void CastSkill1()
    {
        if (skill1Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill1();
    }

    private void PerformSkill1()
    {
        currentAttackType = AttackType.Skill1;
        // 从 StatsManager 获取技能冷却时间
        skill1Timer = StatsManager.Instance.skill1Cooldown;
        ResetCombo();
        anim.SetTrigger(skill1AnimTrigger);
    }

    public void CastSkill2()
    {
        if (skill2Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill2();
    }

    private void PerformSkill2()
    {
        currentAttackType = AttackType.Skill2;
        // 从 StatsManager 获取技能冷却时间
        skill2Timer = StatsManager.Instance.skill2Cooldown;
        ResetCombo();
        anim.SetTrigger(skill2AnimTrigger);
    }

    public void CastSkill3()
    {
        if (skill3Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill3();
    }

    private void PerformSkill3()
    {
        currentAttackType = AttackType.Skill3;
        // 从 StatsManager 获取技能冷却时间
        skill3Timer = StatsManager.Instance.skill3Cooldown;
        ResetCombo();
        anim.SetTrigger(skill3AnimTrigger);
    }
    #endregion

    /// <summary>
    /// 【重要】这个方法也需要绑定到攻击动画的【有效帧】上，与播放声音的事件放在一起。
    /// 这样可以确保伤害判定的时机与视觉、听觉效果完全同步。
    /// </summary>
    public void DealDamage()
    {
        statsUI.UpdateDamage();

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        float finalKnockbackForce = StatsManager.Instance.knockbackForce;
        float finalKnockbackTime = StatsManager.Instance.knockbackTime;
        float finalStunTime = StatsManager.Instance.stunTime;

        // 2. 根据当前的攻击类型，定制化修改这些参数
        switch (currentAttackType)
        {
            case AttackType.BasicCombo:
                // 普攻的击退效果可以弱一些
                finalKnockbackForce *= 0f;
                // 如果是连招的最后一下，给予更强的击退效果！ (从 StatsManager 获取最大连击数)
                if (currentComboStep == StatsManager.Instance.maxCombo)
                {
                    finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f; // 击退力更强
                    finalKnockbackTime = StatsManager.Instance.knockbackTime * 0f;  // 击退时间更长
                    finalStunTime = StatsManager.Instance.stunTime * 0f;       // 眩晕时间也更长
                }
                break;

            case AttackType.Skill1:
                // 技能1：中等击退
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f;
                finalStunTime = StatsManager.Instance.stunTime * 0f;
                break;

            case AttackType.Skill2:
                // 技能2：强力击飞，击退时间很长但眩晕时间短
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f;
                finalKnockbackTime = StatsManager.Instance.knockbackTime * 0f;
                finalStunTime = StatsManager.Instance.stunTime * 0f; // 敌人飞出去很远，但落地后很快恢复
                break;

            case AttackType.Skill3:
                // 技能3：终极技能，超强击退和长眩晕
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 3.0f;
                finalKnockbackTime = StatsManager.Instance.knockbackTime * 2.0f;
                finalStunTime = StatsManager.Instance.stunTime * 3.0f;
                break;
        }

        foreach (Collider2D enemy in enemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            EnemyKonckBack knockback = enemy.GetComponent<EnemyKonckBack>();

            if (health != null)
            {
                bool wasResilientBeforeAttack = health.currentResilience > 0;
                float damageToDeal = StatsManager.Instance.damage;
                float impactToDeal = StatsManager.Instance.impact;

                switch (currentAttackType)
                {
                    case AttackType.BasicCombo:
                        // 从 StatsManager 获取最大连击数
                        if (currentComboStep == StatsManager.Instance.maxCombo) damageToDeal *= 1.5f;
                        break;
                    case AttackType.Skill1:
                        // 从 StatsManager 获取技能伤害倍率
                        damageToDeal *= StatsManager.Instance.skill1DamageMult;
                        break;
                    case AttackType.Skill2:
                        // 从 StatsManager 获取技能伤害倍率
                        damageToDeal *= StatsManager.Instance.skill2DamageMult;
                        break;
                    case AttackType.Skill3:
                        // 从 StatsManager 获取技能伤害倍率
                        damageToDeal *= StatsManager.Instance.skill3DamageMult;
                        break;
                }

                health.TakeDamage((int)damageToDeal, impactToDeal);

                if (wasResilientBeforeAttack && health.currentResilience <= 0)
                {
                    if (PlayerBreakEffectManager.Instance != null)
                    {
                        PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);
                        break; // 找到一个破防的敌人就触发效果并跳出循环
                    }
                }
            }

            if (knockback != null)
            {
                knockback.Knockback(transform, finalKnockbackForce, finalKnockbackTime, finalStunTime);
            }
        }
    }

    public bool IsCurrentlyAttacking()
    {
        return currentAttackType != AttackType.None;
    }

    /// <summary>
    /// 在动画结束时由动画事件调用，表示攻击动作完成
    /// </summary>
    public void FinishAttacking()
    {
        currentAttackType = AttackType.None;
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    #region Cooldown UI
    // 从 StatsManager 获取技能冷却时间用于UI计算
    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / StatsManager.Instance.skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / StatsManager.Instance.skill2Cooldown);
    public float GetSkill3CooldownRatio() => Mathf.Clamp01(skill3Timer / StatsManager.Instance.skill3Cooldown);
    #endregion

    #region Cinematic Break Effect
    public void TriggerCinematicEffect()
    {
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);
        }
    }

    void ExecuteBreakSkill(AttackType skillToExecute)
    {
        PlayerBreakEffectManager.Instance.UnlockTimeButKeepCamera();

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
    /// 在电影化技能动画结束时由动画事件调用
    /// </summary>
    public void OnCinematicSkillFinished()
    {
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.RestoreCameraView();
        }
    }
    #endregion

    // =====================================================================
    // --- 新增区域: 用于动画事件调用的方法 ---
    // 在Unity的Animation窗口中，将这些方法绑定到动画片段的关键帧上。
    // =====================================================================
    #region Animation Event Handlers

    /// <summary>
    /// 在普通攻击动画的有效帧上调用此方法来播放声音
    /// </summary>
    public void PlayAttackSound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlayBasicAttack(currentComboStep);
        }
    }

    /// <summary>
    /// 在技能1动画的有效帧上调用此方法来播放声音
    /// </summary>
    public void PlaySkill1Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill1();
        }
    }

    /// <summary>
    /// 在技能2动画的有效帧上调用此方法来播放声音
    /// </summary>
    public void PlaySkill2Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill2();
        }
    }

    /// <summary>
    /// 在技能3动画的有效帧上调用此方法来播放声音
    /// </summary>
    public void PlaySkill3Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill3();
        }
    }

    #endregion
}




