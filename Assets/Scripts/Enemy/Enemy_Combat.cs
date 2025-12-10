using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    [Header("普通攻击设置")]
    public int atk = 1;
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public float knockbackForce;
    public float stunTime;
    public LayerMask playerLayers;

    [Header("Skill 2: 传送门炸弹")]
    // ★ 这里拖入我们等下制作的“分身”Prefab
    public GameObject skill2Prefab;
    // ★ 伤害值
    public int skill2Damage = 3;
    // ★ 爆炸范围
    public float skill2Radius = 2.0f;
    // ★ 传送门出现在玩家头顶多高的地方
    public float skill2HeightOffset = 4.0f;

    // 普通攻击 (保持不变)
    public void Attack()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayers);
        if (hitPlayers.Length > 0)
        {
            hitPlayers[0].GetComponent<PlayerHealth>().ChangeHealth(-atk);
            hitPlayers[0].GetComponent<PlayerMovement>().Knockback(transform, knockbackForce, stunTime);
        }
    }

    // ★★★ 新增：由 Boss “伸手”动画的 Animation Event 调用 ★★★
    public void CastSkill2()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && skill2Prefab != null)
        {
            // 1. 计算玩家头顶的位置
            Vector3 spawnPos = player.transform.position;
            spawnPos.y += skill2HeightOffset;

            // 2. 生成“特效分身”
            GameObject skillObj = Instantiate(skill2Prefab, spawnPos, Quaternion.identity);

            // 3. 把伤害参数传给分身 (这样就不用在分身里重复设置了)
            SkillDamage script = skillObj.GetComponent<SkillDamage>();
            if (script != null)
            {
                script.Setup(skill2Damage, skill2Radius, playerLayers, transform); // transform是Boss的位置，用于计算击退方向
            }
        }
    }
}