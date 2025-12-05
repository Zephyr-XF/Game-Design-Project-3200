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
    [Header("Base Settings")]
    public Vector3 baseScale = Vector3.one; // Explicitly set the desired scale in Inspector

    // private Vector3 startScale; // Removed, use baseScale instead

    private void OnEnable()
    {
        // Wait for the layout system to calculate the correct position first
        StartCoroutine(InitializePosition());
    }

    private bool isHovered = false;

    public void SetHover(bool hovered)
    {
        isHovered = hovered;
        if (hovered)
        {
            // Reset scale to original when hovering starts so external script can control it
            transform.localScale = baseScale;
        }
    }

    private System.Collections.IEnumerator InitializePosition()
    {
        yield return new WaitForEndOfFrame();
        startPos = transform.localPosition;
        
        // Force the scale to the base scale immediately
        transform.localScale = baseScale;
    }

    private void Update()
    {
        // Floating (Up and Down) - Always active
        float newY = startPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmount;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // Breathing (Scaling) - Only active if NOT hovered
        if (!isHovered)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * scaleSpeed) * scaleAmount;
            transform.localScale = baseScale + Vector3.one * scaleOffset;
        }
    }
}
