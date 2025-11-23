using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Enemy_Combat : MonoBehaviour
{
    public int atk = 20;
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public float knockbackForce;
    public float stunTime;
    public LayerMask playerLayers;

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if(collision.gameObject.tag == "Player")
    //     {
    //         PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
    //         if (playerHealth != null)
    //         {
    //             playerHealth.ChangeHealth(-atk); // Reduce player health by 20 on collision
    //         }
    //         ;
    //     }
    // }

    public void Attack()
    {
        // Detect players in range of attack
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayers);
        // Damage them
        if (hitPlayers.Length > 0)
        {
            hitPlayers[0].GetComponent<PlayerHealth>().ChangeHealth(-atk);
            hitPlayers[0].GetComponent<PlayerMovement>().Knockback(transform, knockbackForce, stunTime);
        }
    }
}

