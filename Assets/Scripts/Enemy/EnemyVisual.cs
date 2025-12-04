using System.Collections;
using UnityEngine;

public class EnemyVisuals : MonoBehaviour
{
    [Header("受击反馈")]
    public Color hitColor = Color.red;    // 受击颜色
    public float flashDuration = 0.1f;    // 闪烁时间

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    // 供外部调用的方法
    public void PlayHitEffect()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashColor());
    }

    IEnumerator FlashColor()
    {
        sr.color = hitColor; // 变红
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor; // 恢复
    }

    // 供外部调用：进入清醒状态的视觉效果
    public void SetDreamshatterVisual(bool isShattered)
    {
        if (isShattered)
            sr.color = Color.gray; // 或者变暗、变蓝，表示破防
        else
            sr.color = originalColor;
    }
}