using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewToolInstance", menuName = "Tool/Tool Instance")]
public class ToolInstanceSO : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("实例名称")]
    public string instanceName;
    
    [Tooltip("关联的工具PreFab")]
    public GameObject toolPrefab;
    
    [Header("投掷属性")]
    [Tooltip("投掷速度")]
    public float throwSpeed = 10f;
    
    [Tooltip("投掷力度")]
    public float throwForce = 5f;
    
    [Tooltip("重力缩放")]
    public float gravityScale = 1f;
    
    [Header("运动控制")]
    [Tooltip("线性阻力 - 控制减速快慢（0=不减速）")]
    public float drag = 0f;
    
    [Tooltip("角阻力 - 控制旋转减速")]
    public float angularDrag = 0.05f;
    
    [Tooltip("自动停止时间（秒，0=不自动停止）")]
    public float autoStopTime = 0f;
    
    [Header("视觉效果")]
    [Tooltip("实例的精灵图")]
    public Sprite instanceSprite;
    
    
    
    [Header("音效")]
    [Tooltip("投掷音效")]
    public AudioClip throwSound;
    
    [Tooltip("碰撞音效")]
    public AudioClip hitSound;
    
    [Header("碰撞检测")]
    [Tooltip("碰撞层")]
    public LayerMask collisionLayers;
    
    [Tooltip("碰撞半径")]
    public float collisionRadius = 0.5f;
}
