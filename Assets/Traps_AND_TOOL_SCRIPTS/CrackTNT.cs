using System.Collections;
using UnityEngine;

public class CrackTNT : MonoBehaviour
{
    [Header("碰撞触发")]
    [Tooltip("是否在碰撞时立即释放TNT")]
    public bool triggerOnCollision = true;
    
    [Tooltip("忽略的碰撞层（不会触发释放）")]
    public LayerMask ignoreCollisionLayers;
    
    [Tooltip("投掷后的无敌时间（秒），防止立即触发")]
    public float immunityTime = 0.3f;
    
    [Header("召唤设置")]
    [Tooltip("要召唤的TNT预制体（通常是StickTNT）")]
    public GameObject thrownTNTPrefab;
    
    [Tooltip("TNT生成位置（如果为空则使用自身位置）")]
    public Transform spawnPoint;
    
    [Tooltip("召唤的TNT数量（默认4个：上下左右）")]
    public int spawnCount = 4;
    
    [Tooltip("飞出的速度")]
    public float throwSpeed = 10f;
    
    [Tooltip("飞出的力度")]
    public float throwForce = 5f;
    
    [Header("音效")]
    [Tooltip("召唤音效")]
    public AudioSource audioSource;
    public AudioClip spawnSound;
    
    private bool hasSpawned = false;
    private bool canTrigger = false;
    
    void Start()
    {
        StartCoroutine(ImmunityCountdown());
    }
    
    private IEnumerator ImmunityCountdown()
    {
        yield return new WaitForSeconds(immunityTime);
        canTrigger = true;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggerOnCollision || hasSpawned || !canTrigger) return;
        
        if (IsInLayerMask(collision.gameObject.layer, ignoreCollisionLayers)) return;
        
        SpawnThrownTNTs();
    }
    
    private void SpawnThrownTNTs()
    {
        if (hasSpawned) return;
        hasSpawned = true;
        
        if (thrownTNTPrefab == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // 播放音效
        if (audioSource != null && spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
        
        // 确定生成位置
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // 四个方向
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        int actualCount = Mathf.Min(spawnCount, directions.Length);
        
        // 生成TNT
        for (int i = 0; i < actualCount; i++)
        {
            GameObject tnt = Instantiate(thrownTNTPrefab, spawnPosition, Quaternion.identity);
            
            Rigidbody2D rb = tnt.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = directions[i] * throwSpeed;
                rb.AddForce(directions[i] * throwForce, ForceMode2D.Impulse);
            }
        }
        
        // 立即销毁
        Destroy(gameObject);
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask) != 0;
    }
    
    void OnDrawGizmosSelected()
    {
        if (spawnCount > 0)
        {
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            
            Gizmos.color = Color.yellow;
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            int actualCount = Mathf.Min(spawnCount, directions.Length);
            
            for (int i = 0; i < actualCount; i++)
            {
                Vector3 endPos = spawnPosition + (Vector3)(directions[i] * 2f);
                Gizmos.DrawLine(spawnPosition, endPos);
                Gizmos.DrawWireSphere(endPos, 0.3f);
            }
            
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }
    }
}
