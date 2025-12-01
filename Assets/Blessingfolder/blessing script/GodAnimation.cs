using UnityEngine;

public class GodAnimation : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatSpeed = 0.5f; // Slower floating
    public float floatAmount = 10f; // Pixels to move up/down

    [Header("Breathing Settings")]
    public float scaleSpeed = 0.5f; // Slower breathing
    public float scaleAmount = 0.05f; // Percentage to scale up/down

    private Vector3 startPos;
    private Vector3 startScale;

    private void OnEnable()
    {
        // Wait for the layout system to calculate the correct position first
        StartCoroutine(InitializePosition());
    }

    private System.Collections.IEnumerator InitializePosition()
    {
        yield return new WaitForEndOfFrame();
        startPos = transform.localPosition;
        startScale = transform.localScale;
    }

    private void Update()
    {
        // Floating (Up and Down)
        float newY = startPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmount;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // Breathing (Scaling)
        float scaleOffset = Mathf.Sin(Time.unscaledTime * scaleSpeed) * scaleAmount;
        transform.localScale = startScale + Vector3.one * scaleOffset;
    }
}
