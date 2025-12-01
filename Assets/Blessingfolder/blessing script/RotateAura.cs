using UnityEngine;

public class RotateAura : MonoBehaviour
{
    public float rotateSpeed = 30f; // Degrees per second

    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.unscaledDeltaTime);
    }
}
