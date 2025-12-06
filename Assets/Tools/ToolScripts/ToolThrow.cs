using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolThrow : MonoBehaviour
{
    [Header("投掷设置")]
    public Transform throwPoint;
    
    [Header("目标位置")]
    [Tooltip("投掷目标位置（光标或其他目标）")]
    public Transform targetTransform;
    
    [Tooltip("如果未设置目标，使用鼠标世界坐标")]
    public bool useMouseAsTarget = true;
    
    [Header("引用")]
    [Tooltip("ChooseBar组件，用于获取当前选中的槽位")]
    public ChooseBar chooseBar;
    
    [Header("工具与实例对应关系")]
    [Tooltip("工具数据列表")]
    public List<ToolSO> toolList = new List<ToolSO>();
    
    [Tooltip("工具实例数据列表（与工具列表一一对应）")]
    public List<ToolInstanceSO> toolInstanceList = new List<ToolInstanceSO>();
    
    [Header("蓄力设置")]
    [Tooltip("投掷工具的按键")]
    public KeyCode throwKey = KeyCode.F;
    
    [Tooltip("最小蓄力时间（秒）")]
    public float minChargeTime = 0.1f;
    
    [Tooltip("最大蓄力时间（秒）")]
    public float maxChargeTime = 2f;
    
    [Tooltip("最小投掷力量倍数")]
    public float minPowerMultiplier = 0.5f;
    
    [Tooltip("最大投掷力量倍数")]
    public float maxPowerMultiplier = 2f;
    
    [Header("方向射线可视化")]
    [Tooltip("是否显示方向指示线")]
    public bool showDirectionLine = true;
    
    [Tooltip("方向射线长度")]
    public float rayLength = 3f;
    
    [Tooltip("方向射线颜色")]
    public Color rayColor = Color.cyan;
    
    [Tooltip("蓄力满时的射线颜色")]
    public Color rayColorCharged = Color.red;
    
    [Tooltip("方向射线宽度")]
    public float rayWidth = 0.1f;
    
    [Header("射线渲染设置")]
    [Tooltip("射线的Sorting Layer名称")]
    public string raySortingLayerName = "Default";
    
    [Tooltip("射线的Sorting Order（数值越大越靠前）")]
    public int raySortingOrder = 100;
    
    [Header("调试")]
    public bool enableDebug = false;
    
    [Tooltip("是否在Scene视图中显示选择范围（仅调试）")]
    public bool showGizmosInScene = false;
    
    // 蓄力状态
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private float currentChargeTime = 0f;
    private float currentPowerMultiplier = 1f;
    
    private LineRenderer lineRenderer;
    private Camera mainCamera;
    
    // 待投掷的工具和槽位
    private ToolInstanceSO pendingToolInstance = null;
    private ToolSlot pendingToolSlot = null;

    void Start()
    {
        // 创建LineRenderer用于可视化
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        
        // 设置Sorting Layer和Order
        lineRenderer.sortingLayerName = raySortingLayerName;
        lineRenderer.sortingOrder = raySortingOrder;
        
        if (enableDebug)
            Debug.Log($"[ToolThrow] LineRenderer 已创建: Layer={raySortingLayerName}, Order={raySortingOrder}");
        
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null && useMouseAsTarget && enableDebug)
        {
            Debug.LogWarning("[ToolThrow] 未找到主摄像机，无法使用鼠标作为目标");
        }
        
        // 获取ChooseBar组件
        if (chooseBar == null)
        {
            chooseBar = FindObjectOfType<ChooseBar>();
            if (chooseBar == null && enableDebug)
            {
                Debug.LogWarning("[ToolThrow] 未找到 ChooseBar 组件");
            }
        }
    }
    
    void Update()
    {
        HandleChargeInput();
        
        // 蓄力时更新可视化
        if (isCharging)
        {
            UpdateCharging();
        }
    }
    
    /// <summary>
    /// 处理蓄力输入
    /// </summary>
    private void HandleChargeInput()
    {
        // 按下F键 - 开始蓄力
        if (Input.GetKeyDown(throwKey) && !isCharging)
        {
            StartCharging();
        }
        
        // 松开F键 - 投掷
        if (Input.GetKeyUp(throwKey) && isCharging)
        {
            ExecuteThrow();
        }
    }
    
    /// <summary>
    /// 开始蓄力
    /// </summary>
    private void StartCharging()
    {
        // 检查是否有工具可投掷
        if (chooseBar == null)
        {
            if (enableDebug)
                Debug.LogWarning("[ToolThrow] ChooseBar 未设置");
            return;
        }
        
        ToolSlot currentSlot = chooseBar.GetCurrentSelectedSlot();
        if (currentSlot == null || currentSlot.toolSO == null || currentSlot.quantity <= 0)
        {
            if (enableDebug)
                Debug.LogWarning("[ToolThrow] 当前槽位没有可投掷的工具");
            return;
        }
        
        // 查找对应的工具实例
        ToolInstanceSO toolInstance = FindToolInstance(currentSlot.toolSO);
        if (toolInstance == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolThrow] 未找到工具 {currentSlot.toolSO.toolName} 的实例数据");
            return;
        }
        
        // 开始蓄力
        isCharging = true;
        chargeStartTime = Time.time;
        pendingToolInstance = toolInstance;
        pendingToolSlot = currentSlot;
        
        // 显示方向线
        if (showDirectionLine && lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
        
        if (enableDebug)
            Debug.Log($"[ToolThrow] 开始蓄力: {currentSlot.toolSO.toolName}");
    }
    
    /// <summary>
    /// 更新蓄力状态
    /// </summary>
    private void UpdateCharging()
    {
        // 计算蓄力时间
        currentChargeTime = Time.time - chargeStartTime;
        currentChargeTime = Mathf.Clamp(currentChargeTime, minChargeTime, maxChargeTime);
        
        // 计算力量倍数（线性插值）
        float t = (currentChargeTime - minChargeTime) / (maxChargeTime - minChargeTime);
        currentPowerMultiplier = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, t);
        
        // 更新方向线可视化
        UpdateDirectionVisualization();
    }
    
    /// <summary>
    /// 更新方向指示线
    /// </summary>
    private void UpdateDirectionVisualization()
    {
        if (lineRenderer == null || throwPoint == null)
            return;
        
        // 获取投掷方向
        Vector2 throwDirection = GetThrowDirection();
        
        // 计算蓄力进度（0-1）
        float chargeProgress = (currentChargeTime - minChargeTime) / (maxChargeTime - minChargeTime);
        chargeProgress = Mathf.Clamp01(chargeProgress);
        
        // 根据蓄力进度改变颜色
        Color currentColor = Color.Lerp(rayColor, rayColorCharged, chargeProgress);
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        
        // 根据蓄力进度改变长度
        float currentRayLength = rayLength * Mathf.Lerp(0.5f, 1.5f, chargeProgress);
        
        // 设置射线起点和终点
        Vector3 startPos = throwPoint.position;
        Vector3 endPos = startPos + (Vector3)throwDirection * currentRayLength;
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
    
    /// <summary>
    /// 获取投掷方向（从投掷点指向目标）
    /// </summary>
    private Vector2 GetThrowDirection()
    {
        if (throwPoint == null)
            return Vector2.right;
        
        Vector3 targetPosition;
        
        // 优先使用目标Transform
        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
        }
        // 否则使用鼠标世界坐标
        else if (useMouseAsTarget && mainCamera != null)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = mainCamera.WorldToScreenPoint(throwPoint.position).z;
            targetPosition = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }
        else
        {
            // 默认向玩家朝向投掷
            return throwPoint.right;
        }
        
        // 计算方向向量
        Vector2 direction = (targetPosition - throwPoint.position).normalized;
        
        return direction;
    }
    
    /// <summary>
    /// 执行投掷
    /// </summary>
    private void ExecuteThrow()
    {
        if (pendingToolInstance == null || pendingToolSlot == null)
        {
            if (enableDebug)
                Debug.LogWarning("[ToolThrow] 没有待投掷的工具");
            StopCharging();
            return;
        }
        
        // 获取投掷方向
        Vector2 throwDirection = GetThrowDirection();
        
        if (enableDebug)
        {
            Debug.Log($"[ToolThrow] 投掷 {pendingToolInstance.instanceName}");
            Debug.Log($"[ToolThrow] 蓄力时间: {currentChargeTime:F2}秒");
            Debug.Log($"[ToolThrow] 力量倍数: {currentPowerMultiplier:F2}x");
            Debug.Log($"[ToolThrow] 投掷方向: {throwDirection}");
        }
        
        // 实例化工具
        ThrowToolInstance(pendingToolInstance, throwDirection, currentPowerMultiplier);
        
        // 减少工具数量
        pendingToolSlot.quantity--;
        if (pendingToolSlot.quantity <= 0)
        {
            pendingToolSlot.toolSO = null;
        }
        pendingToolSlot.UpdateUI();
        
        // 停止蓄力
        StopCharging();
    }
    
    /// <summary>
    /// 停止蓄力
    /// </summary>
    private void StopCharging()
    {
        isCharging = false;
        currentChargeTime = 0f;
        currentPowerMultiplier = 1f;
        pendingToolInstance = null;
        pendingToolSlot = null;
        
        // 隐藏方向线
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        if (enableDebug)
            Debug.Log("[ToolThrow] 停止蓄力");
    }
    
    /// <summary>
    /// 从列表中查找工具对应的实例数据
    /// </summary>
    private ToolInstanceSO FindToolInstance(ToolSO toolSO)
    {
        if (toolList == null || toolInstanceList == null)
        {
            if (enableDebug)
                Debug.LogWarning("[ToolThrow] 工具列表或实例列表未初始化");
            return null;
        }

        if (toolList.Count != toolInstanceList.Count)
        {
            Debug.LogError($"[ToolThrow] 工具列表和实例列表数量不匹配！工具: {toolList.Count}, 实例: {toolInstanceList.Count}");
            return null;
        }

        // 查找对应的工具实例
        for (int i = 0; i < toolList.Count; i++)
        {
            if (toolList[i] == toolSO)
            {
                if (enableDebug)
                    Debug.Log($"[ToolThrow] 找到对应的工具实例: {toolSO.toolName} -> {toolInstanceList[i]?.instanceName}");
                return toolInstanceList[i];
            }
        }

        if (enableDebug)
            Debug.LogWarning($"[ToolThrow] 未找到工具 {toolSO.toolName} 对应的实例数据");
        
        return null;
    }
    
    /// <summary>
    /// 投掷工具实例
    /// </summary>
    private void ThrowToolInstance(ToolInstanceSO toolInstanceSO, Vector2 direction, float powerMultiplier)
    {
        if (throwPoint == null)
        {
            Debug.LogError("[ToolThrow] 投掷点未设置");
            return;
        }

        if (toolInstanceSO.toolPrefab == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolThrow] 工具实例 {toolInstanceSO.instanceName} 没有预制体");
            return;
        }

        // 实例化工具预制体
        GameObject thrownTool = Instantiate(toolInstanceSO.toolPrefab, throwPoint.position, throwPoint.rotation);
        
        // 获取刚体组件
        Rigidbody2D rb = thrownTool.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = thrownTool.AddComponent<Rigidbody2D>();
        }

        // 应用投掷力（速度 * 力量倍数）
        float finalSpeed = toolInstanceSO.throwSpeed * powerMultiplier;
        rb.velocity = direction * finalSpeed;
        
        // 应用阻力设置
        rb.drag = toolInstanceSO.drag;
        rb.angularDrag = toolInstanceSO.angularDrag;

        // 如果设置了自动停止时间，启动定时停止
        if (toolInstanceSO.autoStopTime > 0)
        {
            StartCoroutine(StopToolAfterTime(rb, toolInstanceSO.autoStopTime));
        }

        // 播放投掷音效
        if (toolInstanceSO.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(toolInstanceSO.throwSound, throwPoint.position);
        }

        if (enableDebug)
        {
            Debug.Log($"[ToolThrow] 已投掷: {toolInstanceSO.instanceName}");
            Debug.Log($"[ToolThrow] 最终速度: {finalSpeed} = {toolInstanceSO.throwSpeed} x {powerMultiplier:F2}");
            Debug.Log($"[ToolThrow] 方向: {direction}");
        }
    }
    
    /// <summary>
    /// 在指定时间后停止工具
    /// </summary>
    private IEnumerator StopToolAfterTime(Rigidbody2D rb, float time)
    {
        yield return new WaitForSeconds(time);
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            if (enableDebug)
                Debug.Log($"[ToolThrow] 工具已自动停止");
        }
    }
    
    /// <summary>
    /// 应用工具投掷效果（公共接口，保持兼容性）
    /// </summary>
    public void ApplyToolEffects(ToolSO toolSO, ToolInstanceSO toolInstanceSO = null)
    {
        if (toolSO == null)
        {
            Debug.LogError("[ToolThrow] 工具数据为空，无法投掷");
            return;
        }

        // 如果没有传入工具实例数据，尝试从列表中查找
        if (toolInstanceSO == null)
        {
            toolInstanceSO = FindToolInstance(toolSO);
        }

        if (toolInstanceSO == null || toolInstanceSO.toolPrefab == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolThrow] 工具 {toolSO.toolName} 无法投掷");
            return;
        }

        // 使用默认方向和力量进行投掷
        Vector2 throwDirection = GetThrowDirection();
        ThrowToolInstance(toolInstanceSO, throwDirection, 1f);
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmosInScene || throwPoint == null)
            return;
        
        // 在Scene视图中绘制投掷点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(throwPoint.position, 0.2f);
        
        // 绘制到目标的连线
        if (targetTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(throwPoint.position, targetTransform.position);
        }
    }
}
