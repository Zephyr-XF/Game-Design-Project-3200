using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNT : MonoBehaviour
{
    [Header("��ը����")]
    public int explosionDamage = 20;

    [Header("��ը��Χ����")]
    public GameObject explosionRangeObject; // ��Ҫ��CircleCollider2D��������

    [Header("Ŀ��㼶")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;

    public bool damageEnemies = true;
    public bool damagePlayer = true;

    [Header("��������")]
    [Tooltip("����Ƿ���Դ���TNT��ը")]
    public bool canBeTriggeredByPlayer = true;

    [Tooltip("�����Ƿ���Դ���TNT��ը")]
    public bool canBeTriggeredByEnemy = false;

    [Header("����������")]
    [Tooltip("�Ƿ��������������崥�������塢Ͷ����ȣ�")]
    public bool canBeTriggeredByOthers = true;

    [Tooltip("���Դ���TNT�������㼶�б���������㡢Ͷ����㡢����TNT�ȣ�")]
    public List<LayerMask> otherTriggerLayers = new List<LayerMask>();

    [Header("��������")]
    public Animator anim;

    [Header("�����ӳ�")]
    public float destroyDelay = 0.01f;

    [Header("��Ч����")]
    public AudioSource audioSource;
    public AudioClip igniteSound;      // ������Ч��������ʼʱ���ţ�
    public AudioClip explosionSound;   // ��ը��Ч����ը˲�䲥�ţ�

    [Header("����")]
    public bool enableDebug = true;

    private bool hasTriggered = false;
    private bool isInitialized = false;
    private Collider2D explosionRangeCollider;
    private Collider2D tntCollider; // TNT��������ײ��

    void Start()
    {
        if (enableDebug)
            Debug.Log($"[TNT] Start ������ - Time.time: {Time.time}");

        // ǿ�ƽ��� AudioSource �� PlayOnAwake����ֹ�Զ�����
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();

            if (enableDebug)
                Debug.Log($"[TNT] AudioSource.playOnAwake ������Ϊ false");
        }

        // ���������ȡCircleCollider2D
        if (explosionRangeObject != null)
        {
            explosionRangeCollider = explosionRangeObject.GetComponent<Collider2D>();

            if (explosionRangeCollider != null && enableDebug)
            {
                Debug.Log($"TNT: �� {explosionRangeObject.name} ��ȡ�� {explosionRangeCollider.GetType().Name} ��Ϊ��ը��Χ");
            }
            else if (enableDebug)
            {
                Debug.LogError($"TNT: {explosionRangeObject.name} ��û���ҵ�CircleCollider2D���!");
            }
        }
        else if (enableDebug)
        {
            Debug.LogWarning("TNT: δ���ñ�ը��Χ����!");
        }

        // ��ȡTNT��������ײ��
        tntCollider = GetComponent<Collider2D>();

        ValidateComponents();

        // �ӳٳ�ʼ��
        StartCoroutine(InitializeAfterDelay());
    }

    IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        isInitialized = true;

        if (enableDebug)
            Debug.Log($"[TNT] ? ��ʼ����ɣ���Чϵͳ������ - Time.time: {Time.time}");
    }

    void ValidateComponents()
    {
        if (enableDebug)
        {
            if (explosionRangeObject == null)
                Debug.LogError("TNT: δ���ñ�ը��Χ��������Inspector�з���һ����CircleCollider2D��GameObject");
            else if (explosionRangeCollider == null)
                Debug.LogError("TNT: ��ը��Χ������û��CircleCollider2D�����");

            if (anim == null)
                Debug.LogWarning("TNT: Animator���δ����");

            if (enemyLayer == 0)
                Debug.LogWarning("TNT: Enemy Layerδ����");

            if (damagePlayer && playerLayer == 0)
                Debug.LogWarning("TNT: Player Layerδ����");


            if (tntCollider == null)
                Debug.LogWarning("TNT: TNT GameObject��û��Collider2D������޷������ײ");

            if (canBeTriggeredByOthers && (otherTriggerLayers == null || otherTriggerLayers.Count == 0))
                Debug.LogWarning("TNT: �����������������������б�Ϊ��");
            else if (canBeTriggeredByOthers && otherTriggerLayers != null)
            {
                int enabledCount = 0;
                int unsetCount = 0;
                foreach (var layerMask in otherTriggerLayers)
                {
                    if (layerMask != 0)
                    {
                        enabledCount++;
                    }
                }

                if (enabledCount == 0)
                    Debug.LogWarning("TNT: �������������������д����㶼������");
                else if (enableDebug)
                    Debug.Log($"TNT: ������ {enabledCount} �����õĴ����㣨{unsetCount} ��δ����Layer Mask��");
            }
        }
    }

    /// <summary>
    /// ������ը��������ҹ���ʱ���ã�
    /// </summary>
    void TriggerExplosion()
    {
        if (hasTriggered)
        {
            if (enableDebug) Debug.LogWarning("TNT�Ѿ��������������ظ�����");
            return;
        }

        hasTriggered = true;

        if (enableDebug)
            Debug.Log($"[TNT] TriggerExplosion ������ - isInitialized: {isInitialized}, Time.time: {Time.time}");

        // ����������Ч��ֻ���ڳ�ʼ����ɺ�Ų��ţ�
        if (isInitialized && audioSource != null && igniteSound != null)
        {
            audioSource.PlayOneShot(igniteSound);
            if (enableDebug)
                Debug.Log($"[TNT] ? ������Ч�Ѳ��� - AudioClip: {igniteSound.name}");
        }
        else if (!isInitialized && enableDebug)
        {
            Debug.Log($"[TNT] ? ��ʼ��δ��ɣ���������������Ч");
        }

        // ֻ���ö�������ΪTRUE����ִ�б�ը�߼�
        if (anim != null)
        {
            anim.SetBool("IsExploded", true);
            if (enableDebug) Debug.Log("TNT�����ö������ȴ��¼�����Explode()");
        }
        else if (enableDebug)
        {
            Debug.LogWarning("TNT: û��Animator���");
        }
    }

    /// <summary>
    /// ִ�б�ը���Ӷ����¼����ã�
    /// </summary>
    public void Explode()
    {
        if (enableDebug)
        {
            Debug.Log($"[TNT] Explode �����ã�λ��: {transform.position}, Time.time: {Time.time}");
        }

        // ���ű�ը��Ч����ը˲�䣩
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
            if (enableDebug)
                Debug.Log($"[TNT] ? ��ը��Ч�Ѳ��� - AudioClip: {explosionSound.name}");
        }
        else if (enableDebug)
        {
            if (audioSource == null)
                Debug.LogWarning($"[TNT] ? AudioSource Ϊ��");
            if (explosionSound == null)
                Debug.LogWarning($"[TNT] ? explosionSound Ϊ��");
        }

        // ִ���˺��ж�
        DealExplosionDamage();

        // �ӳ�����TNT����
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// ������ը�˺�
    /// </summary>
    void DealExplosionDamage()
    {
        if (explosionRangeCollider == null)
        {
            Debug.LogError("TNT Error: ��ը��Χ��ײ��(explosionRangeCollider)δ���ã��޷�����˺�");
            return;
        }

        if (damageEnemies)
        {
            DamageEnemies();
        }

        // �������ڱ�ը��˲���ӡ damagePlayer ����ʵֵ
        if (enableDebug) Debug.Log($"[����ʱ���] ��ը˲��, damagePlayer ��ֵ��: {damagePlayer}");

        // ��� damagePlayer ����ֵ
        if (damagePlayer)
        {
            if (enableDebug) Debug.Log("DamagePlayer ���ͨ����׼�����������˺�...");
            DamagePlayer();
        }
        else
        {
            if (enableDebug) Debug.LogWarning("DamagePlayer Ϊ false����������ҵ��˺�������Inspector�м�顣");
        }
    }

    void DamageEnemies()
    {
        if (enemyLayer == 0)
        {
            Debug.LogError("TNT: Enemy Layerδ���ã��޷�������");
            return;
        }

        // ʹ��OverlapCollider��ⷶΧ�ڵĵ���
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;

        List<Collider2D> enemies = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, enemies);

        if (enableDebug)
        {
            Debug.Log($"TNT��ը��⵽ {hitCount} ������");
        }

        foreach (Collider2D enemy in enemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(explosionDamage, 0);

                if (enableDebug)
                    Debug.Log($"�� {enemy.gameObject.name} ��� {explosionDamage} ���˺�");
            }
        }
    }

    void DamagePlayer()
    {
        if (playerLayer == 0)
        {
            Debug.LogError("TNT Error: Player Layer δ��Inspector�����ã��޷������ҡ�");
            return;
        }

        // ʹ��OverlapCollider��ⷶΧ�ڵ����
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;

        List<Collider2D> players = new List<Collider2D>();
        int hitCount = explosionRangeCollider.OverlapCollider(filter, players);

        if (enableDebug) Debug.Log($"TNT��ը��⵽ {hitCount} ����ҡ�");

        if (hitCount == 0)
        {
            if (enableDebug) Debug.LogWarning("δ��⵽��ҡ�����: \n1. ����Ƿ��ڱ�ը��Χ�ڣ�\n2. ��ҵ�Layer�Ƿ�����Ϊ'Player'��\n3. TNT��Player Layer Mask�Ƿ��ѹ�ѡ'Player'��");
        }

        foreach (Collider2D player in players)
        {
            if (enableDebug) Debug.Log($"�ҵ����: {player.gameObject.name}");
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(-explosionDamage);

                if (enableDebug)
                    Debug.Log($"�ɹ��������� {explosionDamage} ���˺���");
            }
            else
            {
                if (enableDebug) Debug.LogError($"������� {player.gameObject.name} ����û���ҵ� PlayerHealth �ű���");
            }
        }
    }

    /// <summary>
    /// ֻʹ��OnTriggerEnter2D��������ײ
    /// ע�͵�OnCollisionEnter2D�Ա����ظ�����
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // �����Լ��ͱ�ը��Χ����
        if (other.gameObject == gameObject || other.gameObject == explosionRangeObject)
        {
            return;
        }

        int otherLayer = other.gameObject.layer;

        if (enableDebug)
            Debug.Log($"TNT�� {other.gameObject.name} ���� (Layer: {LayerMask.LayerToName(otherLayer)})");

        // ��⵽���ʱ��������ը
        if (canBeTriggeredByPlayer && IsInLayerMask(otherLayer, playerLayer))
        {
            if (enableDebug) Debug.Log("TNT����Ҵ�����׼��������ը��");
            TriggerExplosion();
            return;
        }

        // ��⵽������������
        if (canBeTriggeredByOthers)
        {
            foreach (var layerMask in otherTriggerLayers)
            {
                if (IsInLayerMask(otherLayer, layerMask))
                {
                    if (enableDebug) Debug.Log($"TNT�������������崥����׼����ը��");
                    TriggerExplosion();
                    return;
                }
            }
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }



    private void OnDrawGizmosSelected()
    {
        // ��Scene��ͼ�п��ӻ���ը��Χ
        Collider2D rangeCollider = explosionRangeCollider;

        // �����Ϸδ���У����Դ�explosionRangeObject��ȡ
        if (rangeCollider == null && explosionRangeObject != null)
        {
            rangeCollider = explosionRangeObject.GetComponent<Collider2D>();
        }

        if (rangeCollider != null && rangeCollider is CircleCollider2D)
        {
            CircleCollider2D circle = rangeCollider as CircleCollider2D;
            Vector3 colliderPosition = rangeCollider.transform.position;
            Vector3 center = colliderPosition + (Vector3)circle.offset;
            float worldRadius = circle.radius * rangeCollider.transform.lossyScale.x;

            // ���ư�͸�����Բ
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(center, worldRadius);

            // ���ƺ�ɫ�߿�Բ
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, worldRadius);

            // ����TNT���ĵ�
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.15f);

            // ����������ʾTNT�ͱ�ը��Χ�Ĺ�ϵ
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, center);
        }
        else
        {
            // ���û���ҵ�CircleCollider2D�����ƾ���
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    public void DealDamage(int damage)
    {
        explosionDamage = damage;
        TriggerExplosion();
    }
}