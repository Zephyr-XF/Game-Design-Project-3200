using UnityEngine;

/// <summary>
/// 控制投掷物重力的组件
/// </summary>
public class ThrownToolGravityController : MonoBehaviour
{
    private float throwPointY;
    private Rigidbody2D rb;
    private bool enableDebug;
    private bool gravityDisabled = false;

    public void Initialize(float throwPointY, Rigidbody2D rigidbody, bool debug)
    {
        this.throwPointY = throwPointY;
        this.rb = rigidbody;
        this.enableDebug = debug;
    }

    private void Update()
    {
        if (rb == null) return;

        // 当投掷物低于投掷点时，禁用重力
        if (!gravityDisabled && transform.position.y < throwPointY)
        {
            rb.gravityScale = 0f;
            gravityDisabled = true;

            if (enableDebug)
                Debug.Log($"[ThrownToolGravityController] 投掷物低于投掷点，禁用重力 - 当前Y: {transform.position.y}, 投掷点Y: {throwPointY}");
        }
    }
}
