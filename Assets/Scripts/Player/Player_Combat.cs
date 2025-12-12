using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    // ... (大部分变量保持不变) ...
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

    private float skill1Timer = 0;
    private float skill2Timer = 0;
    private float skill3Timer = 0;

    private AttackType currentAttackType = AttackType.None;

    // --- 新增 --- 状态标志，用于判断是否刚由破韧触发了第一次特写
    private bool isWaitingForAnimEventCinematic = false;

    // ... (Awake, Update, Attack, Toss, Skill Casting 等函数保持不变) ...
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
        if (Input.GetKeyDown(KeyCode.F)) Toss(); // --- 新增 --- 检测 F 键输入
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
        // --- 修改 --- 从 StatsManager 获取最小攻击间隔
        nextAttackAllowedTime = Time.time + StatsManager.Instance.minAttackInterval;
    }

    public void Toss()
    {
        if (IsCurrentlyAttacking()) return;

        currentAttackType = AttackType.Toss;
        ResetCombo();
        anim.SetTrigger(tossAnimTrigger);
    }

    // Skill Casting 区域保持不变...
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
        // ... (方法前半部分保持不变) ...
        statsUI.UpdateDamage();

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        float finalKnockbackForce = StatsManager.Instance.knockbackForce;
        float finalKnockbackTime = StatsManager.Instance.knockbackTime;
        float finalStunTime = StatsManager.Instance.stunTime;

        // 此处 switch 块保持不变...
        switch (currentAttackType)
        {
            case AttackType.BasicCombo:
                // 普攻的击退效果可以弱一些
                finalKnockbackForce *= 0f;
                if (currentComboStep == StatsManager.Instance.maxCombo)
                {
                    finalKnockbackForce = StatsManager.Instance.knockbackForce * 0f; // 击退力更强
                    finalKnockbackTime = StatsManager.Instance.knockbackTime * 0f;  // 击退时间更长
                    finalStunTime = StatsManager.Instance.stunTime * 0f;       // 眩晕时间也更长
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

                // 此处 switch 块保持不变...
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

                // --- 修改 --- 这里是核心逻辑改动点
                if (wasResilientBeforeAttack && health.currentResilience <= 0)
                {
                    if (PlayerBreakEffectManager.Instance != null)
                    {
                        // 阶段1：破韧，必定触发特写
                        PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);

                        // 阶段2 的准备：设置状态标志，表示我们现在处于“等待动画事件触发额外特写”的状态
                        isWaitingForAnimEventCinematic = true;

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

    public void FinishAttacking()
    {
        currentAttackType = AttackType.None;
        // --- 新增 --- 攻击动作结束时，重置特写等待状态，以防万一
        isWaitingForAnimEventCinematic = false;
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    // Cooldown UI 区域保持不变...
    #region Cooldown UI
    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / StatsManager.Instance.skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / StatsManager.Instance.skill2Cooldown);
    public float GetSkill3CooldownRatio() => Mathf.Clamp01(skill3Timer / StatsManager.Instance.skill3Cooldown);
    #endregion

    // --- 修改 --- Cinematic Break Effect 区域
    #region Cinematic Break Effect

    /// <summary>
    /// 这个方法被绑定在动画事件上。
    /// 现在它会检查触发条件，而不是总是执行。
    /// </summary>
    public void TriggerCinematicEffect()
    {
        // 条件检查：
        // 1. 必须处于 "等待动画事件触发特写" 的状态 (即刚刚破韧)
        // 2. 必须 StatsManager 中的全局开关为 true
        if (isWaitingForAnimEventCinematic && StatsManager.Instance.canTriggerAnimEventCinematic)
        {
            if (PlayerBreakEffectManager.Instance != null)
            {
                // 阶段2：条件触发特写
                PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);

                // 重要：触发后立即重置状态，防止在同一次攻击中被意外地多次触发
                isWaitingForAnimEventCinematic = false;
            }
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

    public void OnCinematicSkillFinished()
    {
        if (PlayerBreakEffectManager.Instance != null)
        {
            PlayerBreakEffectManager.Instance.RestoreCameraView();
        }
        // 当电影化技能结束时，也确保重置了等待状态
        isWaitingForAnimEventCinematic = false;
    }
    #endregion

    // ... (Animation Event Handlers 区域保持不变) ...
    #region Animation Event Handlers
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



