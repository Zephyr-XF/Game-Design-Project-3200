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

    [Header("Skill 3 Settings")]
    public float skill3Cooldown = 12f;
    public float skill3DamageMult = 4.0f;
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
        if (PlayerBreakEffectManager.Instance != null && PlayerBreakEffectManager.Instance.IsTimeFrozen)
        {
            if (Input.GetKeyDown(KeyCode.K)) ExecuteBreakSkill(AttackType.Skill1);
            else if (Input.GetKeyDown(KeyCode.L)) ExecuteBreakSkill(AttackType.Skill2);
            else if (Input.GetKeyDown(KeyCode.U)) ExecuteBreakSkill(AttackType.Skill3);
            return;
        }

        if (currentComboStep > 0 && Time.time - lastAttackTime > comboResetTimer)
        {
            ResetCombo();
        }

        if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0) skill3Timer -= Time.deltaTime;

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
        if (currentComboStep > maxCombo) currentComboStep = 1;

        // --- 音频播放已从此移除，将由动画事件调用 ---
        // playerAudio.PlayBasicAttack(currentComboStep); 

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
        // --- 音频播放已从此移除，将由动画事件调用 ---
        // playerAudio.PlaySkill1();
        currentAttackType = AttackType.Skill1;
        skill1Timer = skill1Cooldown;
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
        // --- 音频播放已从此移除，将由动画事件调用 ---
        // playerAudio.PlaySkill2();
        currentAttackType = AttackType.Skill2;
        skill2Timer = skill2Cooldown;
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
        // --- 音频播放已从此移除，将由动画事件调用 ---
        // playerAudio.PlaySkill3();
        currentAttackType = AttackType.Skill3;
        skill3Timer = skill3Cooldown;
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
                        if (currentComboStep == maxCombo) damageToDeal *= 1.5f;
                        break;
                    case AttackType.Skill1:
                        damageToDeal *= skill1DamageMult;
                        break;
                    case AttackType.Skill2:
                        damageToDeal *= skill2DamageMult;
                        break;
                    case AttackType.Skill3:
                        damageToDeal *= skill3DamageMult;
                        break;
                }

                health.TakeDamage((int)damageToDeal, impactToDeal);

                if (wasResilientBeforeAttack && health.currentResilience <= 0)
                {
                    if (PlayerBreakEffectManager.Instance != null)
                    {
                        PlayerBreakEffectManager.Instance.TriggerCinematicFreeze(transform);
                        break;
                    }
                }
            }

            if (knockback != null)
            {
                float forceMult = (currentAttackType != AttackType.None && currentAttackType != AttackType.BasicCombo) ? 1.5f : 1f;
                knockback.Knockback(transform, StatsManager.Instance.knockbackForce * forceMult, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
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
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
        anim.SetInteger("AttackComboStep", 0);
    }

    #region Cooldown UI
    public float GetSkill1CooldownRatio() => Mathf.Clamp01(skill1Timer / skill1Cooldown);
    public float GetSkill2CooldownRatio() => Mathf.Clamp01(skill2Timer / skill2Cooldown);
    public float GetSkill3CooldownRatio() => Mathf.Clamp01(skill3Timer / skill3Cooldown);
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



