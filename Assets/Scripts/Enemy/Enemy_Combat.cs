using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    private Animator anim; // ★ 新增：获取动画组件

    [Header("普通攻击设置")]
    public int atk = 1;
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public float knockbackForce;
    public float stunTime;
    public LayerMask playerLayers;

    [Header("多段攻击设置")] // ★ 新增：控制轮流攻击
    public int totalAttackAnimations = 3; // 你有几套攻击动作就填几
    private int currentAttackIndex = 0;   // 内部计数器

    [Header("Skill 2: 传送门炸弹")]
    public GameObject skill2Prefab;
    public int skill2Damage = 3;
    public float skill2Radius = 2.0f;
    public float skill2HeightOffset = 4.0f;

    void Start()
    {
        // ★ 获取自身 Animator 组件
        anim = GetComponent<Animator>();
    }

    // ★★★ 新增：AI 脚本应该调用这个方法来“发起”攻击 ★★★
    // 它的作用是：告诉 Animator 该播放哪个动作，并更新下一次的动作索引
    public void StartAttack()
    {
        if (anim != null)
        {
            // 1. 设置当前要播放的攻击 ID (0, 1, 2...)
            anim.SetInteger("AttackID", currentAttackIndex);

            // 2. 触发攻击动画 (需在 Animator 中设置名为 "TriggerAttack" 的 Trigger)
            anim.SetTrigger("TriggerAttack");

            // 3. 计算下一次攻击的 ID (循环逻辑：0 -> 1 -> 2 -> 0 ...)
            currentAttackIndex = (currentAttackIndex + 1) % totalAttackAnimations;
        }
    }

    // 普通攻击的具体伤害判定 (保持不变)
    // ★ 注意：这个方法最好由 Animation Event 调用，而不是直接在 AI 里调用
    // 这样才能实现“刀挥出去才扣血”的效果
    public void Attack()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayers);
        if (hitPlayers.Length > 0)
        {
            hitPlayers[0].GetComponent<PlayerHealth>().ChangeHealth(-atk);
            hitPlayers[0].GetComponent<PlayerMovement>().Knockback(transform, knockbackForce, stunTime);
        }
    }

    // Skill 2: 传送门炸弹 (保持不变)
    public void CastSkill2()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && skill2Prefab != null)
        {
            Vector3 spawnPos = player.transform.position;
            spawnPos.y += skill2HeightOffset;

            GameObject skillObj = Instantiate(skill2Prefab, spawnPos, Quaternion.identity);

            SkillDamage script = skillObj.GetComponent<SkillDamage>();
            if (script != null)
            {
                script.Setup(skill2Damage, skill2Radius, playerLayers, transform);
            }
        }
    }
}