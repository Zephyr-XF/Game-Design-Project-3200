using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolThrow : MonoBehaviour
{
    [Header("投掷设置")]
    public Transform throwPoint;
    
    [Header("工具与实例对应关系")]
    [Tooltip("工具数据列表")]
    public List<ToolSO> toolList = new List<ToolSO>();
    
    [Tooltip("工具实例数据列表（与工具列表一一对应）")]
    public List<ToolInstanceSO> toolInstanceList = new List<ToolInstanceSO>();
    
    [Header("调试")]
    public bool enableDebug = false;

    /// <summary>
    /// 应用工具投掷效果
    /// </summary>
    /// <param name="toolSO">工具数据</param>
    /// <param name="toolInstanceSO">工具实例数据（可选，如果为null则从列表中查找）</param>
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

        // 必须有工具实例数据和预制体才能投掷
        if (toolInstanceSO == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolThrow] 工具 {toolSO.toolName} 没有对应的工具实例数据，无法投掷！");
            return;
        }

        if (toolInstanceSO.toolPrefab == null)
        {
            if (enableDebug)
                Debug.LogWarning($"[ToolThrow] 工具实例 {toolInstanceSO.instanceName} 没有挂载预制体 (toolPrefab)，无法投掷！");
            return;
        }

        if (enableDebug)
            Debug.Log($"[ToolThrow] 投掷工具: {toolSO.toolName} -> {toolInstanceSO.instanceName}");

        ThrowToolInstance(toolInstanceSO);
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
    /// 使用工具实例数据投掷
    /// </summary>
    private void ThrowToolInstance(ToolInstanceSO toolInstanceSO)
    {
        if (throwPoint == null)
        {
            Debug.LogError("[ToolThrow] 投掷点未设置");
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

        // 应用投掷力
        Vector2 throwDirection = throwPoint.right;
        rb.velocity = throwDirection * toolInstanceSO.throwSpeed;
        
        // 应用阻力设置
        rb.drag = toolInstanceSO.drag;
        rb.angularDrag = toolInstanceSO.angularDrag;
        
        // rb.gravityScale = toolInstanceSO.gravityScale;

        // 如果设置了自动停止时间，启动定时停止
        if (toolInstanceSO.autoStopTime > 0)
        {
            StartCoroutine(StopToolAfterTime(rb, toolInstanceSO.autoStopTime));
        }

        // 添加重力控制组件
        // ThrownToolGravityController gravityController = thrownTool.AddComponent<ThrownToolGravityController>();
        // gravityController.Initialize(throwPoint.position.y, rb, enableDebug);

        // 播放投掷音效
        if (toolInstanceSO.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(toolInstanceSO.throwSound, throwPoint.position);
        }

        if (enableDebug)
            Debug.Log($"[ToolThrow] 投掷工具实例: {toolInstanceSO.instanceName}, 速度: {toolInstanceSO.throwSpeed}, 阻力: {toolInstanceSO.drag}, 自动停止: {toolInstanceSO.autoStopTime}秒");
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
}
