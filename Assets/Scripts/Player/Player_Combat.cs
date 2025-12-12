using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    // 定义攻击类型枚举
    public enum AttackType { None, BasicCombo, Skill1, Skill2, Skill3, Toss }

    [Header("Combat References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public Animator anim;

    [Header("Combat Timing Settings")]
    public float comboResetTimer = 1f;

    [Header("Animation Triggers")]
    public string attackAnmiTrigger = "AttackTrigger";
    public string skill1AnimTrigger = "Skill1Trigger";
    public string skill2AnimTrigger = "Skill2Trigger";
    public string skill3AnimTrigger = "Skill3Trigger";
    public string tossAnimTrigger = "TossTrigger";

    private PlayerAudio playerAudio;
    private int currentComboStep = 0;
    private float lastAttackTime = 0;
    private float nextAttackAllowedTime = 0;

    // 技能冷却计时器
    private float skill1Timer = 0;
    private float skill2Timer = 0;
    private float skill3Timer = 0;

    private AttackType currentAttackType = AttackType.None;

    // 状态标志：用于判断是否处于“破韧后等待动画事件触发二次特写”的状态
    private bool isWaitingForAnimEventCinematic = false;

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
        // 1. 检查是否处于电影化破防的时间冻结状态
        if (PlayerBreakEffectManager.Instance != null && PlayerBreakEffectManager.Instance.IsTimeFrozen)
        {
            // 在时间冻结期间，侦听输入以执行处决技
            if (Input.GetKeyDown(KeyCode.K)) ExecuteBreakSkill(AttackType.Skill1);
            else if (Input.GetKeyDown(KeyCode.L)) ExecuteBreakSkill(AttackType.Skill2);
            else if (Input.GetKeyDown(KeyCode.U)) ExecuteBreakSkill(AttackType.Skill3);
            return; // 冻结时不执行后续逻辑
        }

        // 2. 连击重置逻辑
        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }

        // 3. 技能冷却计时
        if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0) skill3Timer -= Time.deltaTime;

        // 4. 更新技能冷却UI
        if (SkillsCooldownUIManager.Instance != null)
        {
            float skill1Ratio = (StatsManager.Instance.skill1Cooldown > 0) ? skill1Timer / StatsManager.Instance.skill1Cooldown : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(0, skill1Ratio);

            float skill2Ratio = (StatsManager.Instance.skill2Cooldown > 0) ? skill2Timer / StatsManager.Instance.skill2Cooldown : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(1, skill2Ratio);

            float skill3Ratio = (StatsManager.Instance.skill3Cooldown > 0) ? skill3Timer / StatsManager.Instance.skill3Cooldown : 0;
            SkillsCooldownUIManager.Instance.UpdateSkillDisplay(2, skill3Ratio);
        }

        // 5. 玩家输入检测
        if (Input.GetKeyDown(KeyCode.J)) Attack();
        if (Input.GetKeyDown(KeyCode.K)) CastSkill1();
        if (Input.GetKeyDown(KeyCode.L)) CastSkill2();
        if (Input.GetKeyDown(KeyCode.U)) CastSkill3();
        if (Input.GetKeyDown(KeyCode.F)) Toss();
    }

    #region Action Methods (Attack, Toss, Skills)

    public void Attack()
    {
        if (Time.time < nextAttackAllowedTime || IsCurrentlyAttacking()) return;

        currentAttackType = AttackType.BasicCombo;

        currentComboStep++;
        if (currentComboStep > StatsManager.Instance.maxCombo)
        {
            currentComboStep = 1;
        }

        anim.SetInteger("AttackComboStep", currentComboStep);
        anim.SetTrigger(attackAnmiTrigger);

        lastAttackTime = Time.time;
        nextAttackAllowedTime = Time.time + StatsManager.Instance.minAttackInterval;
    }

    public void Toss()
    {
        if (IsCurrentlyAttacking()) return;

        currentAttackType = AttackType.Toss;
        ResetCombo();
        anim.SetTrigger(tossAnimTrigger);
    }

    public void CastSkill1()
    {
        if (skill1Timer > 0 || IsCurrentlyAttacking()) return;
        PerformSkill1();
    }

    private void PerformSkill1()
    {
        currentAttackType = AttackType.Skill1;
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
        skill3Timer = StatsManager.Instance.skill3Cooldown;
        ResetCombo();
        anim.SetTrigger(skill3AnimTrigger);
    }

    #endregion

    /// <summary>
    /// 核心战斗逻辑：造成伤害、应用击退、检测破韧
    /// </summary>
    public void DealDamage()
    {
        statsUI.UpdateDamage();

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        float finalKnockbackForce = StatsManager.Instance.knockbackForce;
        float finalKnockbackTime = StatsManager.Instance.knockbackTime;
        float finalStunTime = StatsManager.Instance.stunTime;

        // 根据攻击类型调整控制效果
        switch (currentAttackType)
        {
            case AttackType.BasicCombo:
                finalKnockbackForce *= 0f;
                if (currentComboStep == StatsManager.Instance.maxCombo)
                {
                    finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f;
                    finalKnockbackTime = StatsManager.Instance.knockbackTime * 0f;
                    finalStunTime = StatsManager.Instance.stunTime * 0f;
                }
                break;
            case AttackType.Skill1:
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f;
                finalStunTime = StatsManager.Instance.stunTime * 0f;
                break;
            case AttackType.Skill2:
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f;
                finalKnockbackTime = StatsManager.Instance.knockbackTime * 0f;
                finalStunTime = StatsManager.Instance.stunTime * 0f;
                break;
            case AttackType.Skill3:
                finalKnockbackForce = StatsManager.Instance.knockbackForce * 3.0f;
                finalKnockbackTime = StatsManager.Instance.knockbackTime * 2.0f;
                finalStunTime = StatsManager.Instance.stunTime * 3.0f;
                break;
            case AttackType.Toss:
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

                // 根据攻击类型调整伤害倍率
                switch (currentAttackType)
                {
                    case AttackType.BasicCombo:
                        if (currentComboStep == StatsManager.Instance.maxCombo) damageToDeal *= 1.5f;
                        break;
                    case AttackType.Skill1:
                        damageToDeal *= StatsManager.Instance.skill1DamageMult;
                        break;
                    case AttackType.Skill2:
                        damageToDeal *= StatsManager.Instance.skill2DamageMult;
                        break;
                    case AttackType.Skill3:
                        damageToDeal *= StatsManager.Instance.skill3DamageMult;
                        break;
                    case AttackType.Toss:
                        damageToDeal = 0;
                        impactToDeal = 0;
                        break;
                }

                health.TakeDamage((int)damageToDeal, impactToDeal);

                // --- 破韧检测 ---
                if (wasResilientBeforeAttack && health.currentResilience <= 0)
                {
                    if (PlayerBreakEffectManager.Instance != null)
                    {
                        // 触发第一次时间冻结（破韧特写）
                        PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);

                        // 标记状态：准备好接收动画事件来触发第二次特写
                        isWaitingForAnimEventCinematic = true;

                        // 找到一个破防敌人即可，防止重复触发
                        break;
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

    public void FinishAttacking()
    {
        currentAttackType = AttackType.None;
        // 攻击结束，重置特写等待状态
        isWaitingForAnimEventCinematic = false;
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    #region Cooldown UI Helpers
    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / StatsManager.Instance.skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / StatsManager.Instance.skill2Cooldown);
    public float GetSkill3CooldownRatio() => Mathf.Clamp01(skill3Timer / StatsManager.Instance.skill3Cooldown);
    #endregion

    #region Cinematic Break Effect (含滤镜控制)

    /// <summary>
    /// 由动画事件调用。
    /// 当处于“等待特写”状态且StatsManager允许时，触发第二次特写。
    /// </summary>
    public void TriggerCinematicEffect()
    {
        if (isWaitingForAnimEventCinematic && StatsManager.Instance.canTriggerAnimEventCinematic)
        {
            if (PlayerBreakEffectManager.Instance != null)
            {
                PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);
                // 触发后立即重置状态
                isWaitingForAnimEventCinematic = false;
            }
        }
    }

    /// <summary>
    /// 在破韧时间冻结期间，玩家按下技能键后调用此方法执行处决技。
    /// </summary>
    void ExecuteBreakSkill(AttackType skillToExecute)
    {
        // 1. 解锁时间流动，但保持特写相机视角
        PlayerBreakEffectManager.Instance.UnlockTimeButKeepCamera();

        // 2. 【新增】特写开始：通知PlayerSanity隐藏屏幕滤镜
        if (PlayerSanity.Instance != null)
        {
            PlayerSanity.Instance.ToggleFilterForCinematic(true);
        }

        // 3. 执行对应的技能逻辑
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
    /// 由特写技能的动画结束帧（或动画事件）调用。
    /// </summary>
    public void OnCinematicSkillFinished()
    {
        // 1. 恢复正常相机视角
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.RestoreCameraView();
        }

        // 2. 【新增】特写结束：通知PlayerSanity恢复屏幕滤镜
        if (PlayerSanity.Instance != null)
        {
            PlayerSanity.Instance.ToggleFilterForCinematic(false);
        }

        // 3. 确保状态重置
        isWaitingForAnimEventCinematic = false;
    }
    #endregion

    #region Animation Event Handlers (Audio)
    public void PlayAttackSound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlayBasicAttack(currentComboStep);
        }
    }
    public void PlaySkill1Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill1();
        }
    }
    public void PlaySkill2Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill2();
        }
    }
    public void PlaySkill3Sound()
    {
        if (playerAudio != null)
        {
            playerAudio.PlaySkill3();
        }
    }
    #endregion
}




