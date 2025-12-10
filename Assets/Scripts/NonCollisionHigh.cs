using System.Collections.Generic;
using UnityEngine;

public class NonCollisionHigh : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("碰撞箱组件")]
    public Collider2D hideZone;

    [Header("图层设置")]
    [Tooltip("敌人图层")]
    public LayerMask enemyLayer;

    [Tooltip("玩家图层")]
    public LayerMask playerLayer;

    [Header("调试")]
    public bool enableDebug = false;

    private Transform player;
    private List<EnemyMovement> enemiesInRange = new List<EnemyMovement>();
    private bool isPlayerHiding = false;

    void Start()
    {
        if (hideZone == null)
        {
            hideZone = GetComponent<Collider2D>();
            if (hideZone != null)
            {
                hideZone.isTrigger = true;
            }
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (enableDebug)
            Debug.Log("[CollisionHigh] 隐藏区域初始化完成");
    }

    void Update()
    {
        if (player != null && hideZone != null)
        {
            bool wasHiding = isPlayerHiding;
            isPlayerHiding = IsPlayerCompletelyInside();

            if (wasHiding != isPlayerHiding)
            {
                UpdateEnemiesDetection();

                if (enableDebug)
                    Debug.Log($"[CollisionHigh] 玩家隐藏状态: {isPlayerHiding}");
            }
        }
    }

    private bool IsPlayerCompletelyInside()
    {
        if (player == null || hideZone == null) return false;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return false;

        Bounds playerBounds = playerCollider.bounds;
        Bounds hideBounds = hideZone.bounds;

        bool completelyInside =
            playerBounds.min.x >= hideBounds.min.x &&
            playerBounds.max.x <= hideBounds.max.x &&
            playerBounds.min.y >= hideBounds.min.y &&
            playerBounds.max.y <= hideBounds.max.y;

        return completelyInside;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            EnemyMovement enemy = other.GetComponent<EnemyMovement>();
            if (enemy != null && !enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);

                if (isPlayerHiding)
                {
                    DisableEnemyDetection(enemy);
                }

                if (enableDebug)
                    Debug.Log($"[CollisionHigh] 敌人进入隐藏区域: {other.name}");
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        EnemyMovement enemy = other.GetComponent<EnemyMovement>();
        if (enemy != null && enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
            EnableEnemyDetection(enemy);

            if (enableDebug)
                Debug.Log($"[CollisionHigh] 敌人离开隐藏区域: {other.name}");
        }
    }

    private void UpdateEnemiesDetection()
    {
        foreach (var enemy in enemiesInRange)
        {
            if (enemy == null) continue;

            if (isPlayerHiding)
            {
                DisableEnemyDetection(enemy);
            }
            else
            {
                EnableEnemyDetection(enemy);
            }
        }
    }

    private void DisableEnemyDetection(EnemyMovement enemy)
    {
        if (enemy == null) return;

        // 使用 EnemyMovement 中的 ChangeState 方法强制进入 Idle
        enemy.ChangeState(EnemyState.idle);

        // 禁用敌人脚本，停止 Update 执行
        enemy.enabled = false;

        if (enableDebug)
            Debug.Log($"[CollisionHigh] 禁用敌人检测: {enemy.name}");
    }

    private void EnableEnemyDetection(EnemyMovement enemy)
    {
        if (enemy == null) return;

        // 重新启用敌人脚本
        enemy.enabled = true;

        if (enableDebug)
            Debug.Log($"[CollisionHigh] 恢复敌人检测: {enemy.name}");
    }

    void OnDrawGizmosSelected()
    {
        if (hideZone != null)
        {
            Gizmos.color = isPlayerHiding ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(hideZone.bounds.center, hideZone.bounds.size);
        }
    }
}