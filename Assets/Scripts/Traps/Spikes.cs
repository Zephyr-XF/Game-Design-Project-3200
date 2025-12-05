using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("�˺�����")]
    public int spikeDamage = 10;            // ÿ�δ̳���ɵ��˺�

    [Header("��������")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.7f;     // �ȵ��ش�ʱ���ٶȱ��ʣ�0.7 = ��Ϊԭ��70%��
    public bool enableSlow = true;          // �Ƿ����ü���Ч��

    [Header("��ʱ��������")]
    public float idleTime = 1.5f;           // ����״̬����ʱ��
    public float activeTime = 1.0f;         // �̳�״̬����ʱ��
    public bool startActive = false;        // �Ƿ�һ��ʼ���Ǵ̳�״̬

    [Header("Ŀ��㼶")]
    public LayerMask playerLayer;           // Player ���ڵ� Layer
    public LayerMask enemyLayer;            // Enemy ���ڵ� Layer

    [Header("�������")]
    public Animator anim;                   // ���Ƶش̶���
    public Collider2D damageCollider;       // �����˺�������ײ��(���� IsTrigger = true)

    [Header("����")]
    public bool enableDebug = true;

    private bool isActive = false;          // ��ǰ�Ƿ���"�̳�"״̬
    private bool canDamage = false;         // ��ǰ���ִ̳��Ƿ񻹿��Զ����/��������˺�����ֹһ֡��Σ�

    // ��¼�����ٵĶ�����ԭʼ�ٶ�
    private Dictionary<GameObject, float> slowedPlayers = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> slowedEnemies = new Dictionary<GameObject, float>();

    private void Start()
    {
        // �Զ����Ի�ȡ����
        if (anim == null)
            anim = GetComponent<Animator>();

        if (damageCollider == null)
            damageCollider = GetComponent<Collider2D>();

        ValidateComponents();

        // ���� startActive ������ʼ״̬
        isActive = startActive;
        UpdateVisualState();

        // ������ʱѭ��
        StartCoroutine(SpikeRoutine());
    }

    void ValidateComponents()
    {
        if (!enableDebug) return;

        if (damageCollider == null)
            Debug.LogWarning("Spikes: û�ҵ��˺��õ� Collider2D������ Inspector �з��䡣");

        if (anim == null)
            Debug.LogWarning("Spikes: û�ҵ� Animator ������ش̽����Ქ�Ŷ�����");

        if (playerLayer == 0)
            Debug.LogWarning("Spikes: Player Layer δ���ã��޷��ɿ������ҡ�");

        if (enemyLayer == 0)
            Debug.LogWarning("Spikes: Enemy Layer δ���ã��޷��ɿ������ˡ�");
    }

    /// <summary>
    /// ���Ƶش̵�ѭ��״̬������ -> ���� -> ���� -> ��
    /// </summary>
    IEnumerator SpikeRoutine()
    {
        while (true)
        {
            if (!isActive)
            {
                // ����׶�
                if (enableDebug) Debug.Log("Spikes: ��������״̬");
                canDamage = false;                // ����ʱ������˺�
                if (damageCollider != null)
                    damageCollider.enabled = false;

                yield return new WaitForSeconds(idleTime);

                // �е��̳�
                SetActive(true);
            }
            else
            {
                // �̳��׶�
                if (enableDebug) Debug.Log("Spikes: ����̳�״̬");
                canDamage = true;                 // ���ִ̳������˺�
                if (damageCollider != null)
                    damageCollider.enabled = true;

                yield return new WaitForSeconds(activeTime);

                // �е�����
                SetActive(false);
            }
        }
    }

    /// <summary>
    /// ���ô̵�"�̳� / ����"״̬
    /// </summary>
    void SetActive(bool active)
    {
        isActive = active;
        UpdateVisualState();
    }

    /// <summary>
    /// ���¶����������֣�
    /// </summary>
    void UpdateVisualState()
    {
        if (anim != null)
        {
            // ����� "IsActive" Ҫ���� Animator �ﶨ��Ĳ�����һ��
            anim.SetBool("IsActive", isActive);
        }
    }

    /// <summary>
    /// ���/���˽���ش�����ʱ����������������Ч��
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // ����Ƿ������
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            // Ӧ�ü���Ч�������۵ش��Ƿ�̳���
            if (enableSlow)
            {
                ApplySlowToPlayer(other.gameObject);
            }

            // ֻ�ڴ̳�״̬����˺�
            if (isActive && canDamage)
            {
                if (enableDebug)
                    Debug.Log($"Spikes: ����� {other.gameObject.name} ����������ײ");

                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.ChangeHealth(-spikeDamage);
                    if (enableDebug)
                        Debug.Log($"Spikes: �������� {spikeDamage} ���˺���");
                }
                else if (enableDebug)
                {
                    Debug.LogError($"Spikes: ��� {other.gameObject.name} ����û�� PlayerHealth �����");
                }
            }
            return;
        }

        // ����Ƿ��ǵ���
        if (IsInLayerMask(otherLayer, enemyLayer))
        {
            // Ӧ�ü���Ч�������۵ش��Ƿ�̳���
            if (enableSlow)
            {
                ApplySlowToEnemy(other.gameObject);
            }

            // ֻ�ڴ̳�״̬����˺�
            if (isActive && canDamage)
            {
                if (enableDebug)
                    Debug.Log($"Spikes: ����� {other.gameObject.name} ����������ײ");

                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(spikeDamage, 0);
                    if (enableDebug)
                        Debug.Log($"Spikes: �Ե������ {spikeDamage} ���˺���");
                }
                else if (enableDebug)
                {
                    Debug.LogError($"Spikes: ���� {other.gameObject.name} ����û�� EnemyHealth �����");
                }
            }
        }
    }

    /// <summary>
    /// ���/�����뿪�ش�����ʱ�������ָ��ٶȣ�
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;

        // ����뿪
        if (IsInLayerMask(otherLayer, playerLayer))
        {
            RestorePlayerSpeed(other.gameObject);
        }

        // �����뿪
        if (IsInLayerMask(otherLayer, enemyLayer))
        {
            RestoreEnemySpeed(other.gameObject);
        }
    }

    /// <summary>
    /// �����Ӧ�ü���Ч��
    /// </summary>
    void ApplySlowToPlayer(GameObject obj)
    {
        var playerMovement = obj.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"Spikes: {obj.name} û���ҵ� PlayerMovement ������޷����١�");
            return;
        }

        if (StatsManager.Instance == null)
        {
            if (enableDebug)
                Debug.LogError("Spikes: StatsManager.Instance Ϊ�գ��޷��޸�����ٶȣ�");
            return;
        }

        // ����Ѿ��ڼ����У����ظ�Ӧ��
        if (slowedPlayers.ContainsKey(obj))
            return;

        // ��¼ԭʼ�ٶȲ�Ӧ�ü���
        int originalSpeed = StatsManager.Instance.speed;
        slowedPlayers.Add(obj, originalSpeed);

        int slowedSpeed = Mathf.RoundToInt(originalSpeed * slowMultiplier);
        StatsManager.Instance.speed = slowedSpeed;

        if (enableDebug)
            Debug.Log($"Spikes: ��� {obj.name} ����ش̣��ٶ� {originalSpeed} -> {slowedSpeed}");
    }

    /// <summary>
    /// �Ե���Ӧ�ü���Ч��
    /// </summary>
    void ApplySlowToEnemy(GameObject obj)
    {
        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement == null)
        {
            if (enableDebug)
                Debug.LogWarning($"Spikes: {obj.name} û���ҵ� EnemyMovement ������޷����١�");
            return;
        }

        // ����Ѿ��ڼ����У����ظ�Ӧ��
        if (slowedEnemies.ContainsKey(obj))
            return;

        // ��¼ԭʼ�ٶȲ�Ӧ�ü���
        float originalSpeed = enemyMovement.speed;
        slowedEnemies.Add(obj, originalSpeed);

        float slowedSpeed = originalSpeed * slowMultiplier;
        enemyMovement.speed = slowedSpeed;

        if (enableDebug)
            Debug.Log($"Spikes: ���� {obj.name} ����ش̣��ٶ� {originalSpeed} -> {slowedSpeed}");
    }

    /// <summary>
    /// �ָ�����ٶ�
    /// </summary>
    void RestorePlayerSpeed(GameObject obj)
    {
        if (!slowedPlayers.TryGetValue(obj, out float originalSpeed))
            return;

        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.speed = Mathf.RoundToInt(originalSpeed);

            if (enableDebug)
                Debug.Log($"Spikes: ��� {obj.name} �뿪�ش̣��ٶȻָ�Ϊ {originalSpeed}");
        }

        slowedPlayers.Remove(obj);
    }

    /// <summary>
    /// �ָ������ٶ�
    /// </summary>
    void RestoreEnemySpeed(GameObject obj)
    {
        if (!slowedEnemies.TryGetValue(obj, out float originalSpeed))
            return;

        var enemyMovement = obj.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.speed = originalSpeed;

            if (enableDebug)
                Debug.Log($"Spikes: ���� {obj.name} �뿪�ش̣��ٶȻָ�Ϊ {originalSpeed}");
        }

        slowedEnemies.Remove(obj);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}