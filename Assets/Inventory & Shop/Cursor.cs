using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    [Header("光标设置")]
    [Tooltip("是否隐藏系统光标")]
    public bool hideSystemCursor = false;
    
    [Tooltip("光标跟随速度（0=瞬间跟随，值越大越平滑）")]
    [Range(0f, 20f)]
    public float smoothSpeed = 0f;
    
    [Tooltip("是否使用平滑跟随")]
    public bool useSmoothFollow = false;
    
    [Header("点击动画")]
    [Tooltip("光标动画控制器")]
    public Animator cursorAnimator;
    
    [Tooltip("是否启用点击动画")]
    public bool enableClickAnimation = true;
    
    [Tooltip("点击动画触发器名称")]
    public string clickAnimationTrigger = "Click";
    
    [Header("调试")]
    public bool enableDebug = false;
    
    private Camera mainCamera;
    private Vector3 targetPosition;

    void Start()
    {
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[Cursor] 未找到主摄像机！");
            enabled = false;
            return;
        }
        
        // 获取 Animator 组件
        if (cursorAnimator == null)
        {
            cursorAnimator = GetComponent<Animator>();
            if (cursorAnimator == null && enableClickAnimation && enableDebug)
            {
                Debug.LogWarning("[Cursor] 未找到 Animator 组件，点击动画将被禁用");
            }
        }
        
        // 隐藏系统光标
        if (hideSystemCursor)
        {
            UnityEngine.Cursor.visible = false;
            
            if (enableDebug)
                Debug.Log("[Cursor] 系统光标已隐藏");
        }
        
        // 初始化位置
        UpdateCursorPosition();
    }

    void Update()
    {
        UpdateCursorPosition();
        
        // 检测鼠标左键点击
        if (enableClickAnimation && Input.GetMouseButtonDown(0))
        {
            PlayClickAnimation();
        }
    }
    
    /// <summary>
    /// 更新光标位置到鼠标位置
    /// </summary>
    private void UpdateCursorPosition()
    {
        if (mainCamera == null)
            return;
        
        // 获取鼠标在屏幕上的位置
        Vector3 mouseScreenPosition = Input.mousePosition;
        
        // 将屏幕坐标转换为世界坐标
        mouseScreenPosition.z = mainCamera.WorldToScreenPoint(transform.position).z;
        targetPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        
        // 根据设置选择跟随方式
        if (useSmoothFollow && smoothSpeed > 0)
        {
            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
        else
        {
            // 瞬间跟随
            transform.position = targetPosition;
        }
        
        if (enableDebug)
        {
            Debug.Log($"[Cursor] 鼠标位置: {mouseScreenPosition}, 世界坐标: {targetPosition}");
        }
    }
    
    /// <summary>
    /// 播放点击动画
    /// </summary>
    public void PlayClickAnimation()
    {
        if (cursorAnimator == null)
        {
            if (enableDebug)
                Debug.LogWarning("[Cursor] Animator 未设置，无法播放点击动画");
            return;
        }
        
        if (string.IsNullOrEmpty(clickAnimationTrigger))
        {
            if (enableDebug)
                Debug.LogWarning("[Cursor] 点击动画触发器名称为空");
            return;
        }
        
        cursorAnimator.SetTrigger(clickAnimationTrigger);
        
        if (enableDebug)
            Debug.Log($"[Cursor] 播放点击动画: {clickAnimationTrigger}");
    }
    
    void OnDestroy()
    {
        // 恢复系统光标显示
        if (hideSystemCursor)
        {
            UnityEngine.Cursor.visible = true;
            
            if (enableDebug)
                Debug.Log("[Cursor] 系统光标已恢复显示");
        }
    }
    
    void OnDisable()
    {
        // 当脚本被禁用时，恢复系统光标
        if (hideSystemCursor)
        {
            UnityEngine.Cursor.visible = true;
        }
    }
    
    void OnEnable()
    {
        // 当脚本被启用时，隐藏系统光标
        if (hideSystemCursor)
        {
            UnityEngine.Cursor.visible = false;
        }
    }
}
