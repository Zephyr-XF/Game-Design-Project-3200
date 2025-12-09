# MiniBlackHole 快速使用指南 ??

## 脚本功能总览

这个脚本实现了一个**小型黑洞**，可以：
- ? 吸引指定图层的敌人
- ? 对敌人造成持续伤害（可选）
- ? 通过动画事件控制吸引的开始和结束
- ? 自动销毁（可配置生命周期）

---

## 核心功能

### 1. 吸引系统
- 使用 `CircleCollider2D` 检测范围内的敌人
- 通过 `Rigidbody2D.AddForce` 施加吸引力
- 距离越近，吸引力越强（平方反比）

### 2. 伤害系统
- 每秒造成可配置的伤害
- 每0.5秒检测一次
- 调用 `EnemyHealth.TakeDamage(damage, 0f)` 方法

### 3. 动画事件接口
```csharp
StartPulling()      // 开始吸引
EndPulling()        // 结束吸引
DestroyBlackHole()  // 销毁黑洞
```

---

## Unity 设置步骤

### Step 1: 创建黑洞预制体
1. 创建空物体，命名为 `MiniBlackHole`
2. 添加 `SpriteRenderer` 组件（黑洞贴图）
3. 添加 `Animator` 组件
4. 添加 `MiniBlackHole` 脚本

### Step 2: 配置组件

#### MiniBlackHole 脚本参数
```
组件引用:
├─ Animator: [自动获取]
└─ Pull Collider: [自动创建]

吸引设置:
├─ Target Layers: Enemy（勾选 Enemy 层）
├─ Pull Force: 15
├─ Pull Radius: 4
├─ Max Pull Distance: 10
├─ Pull Duration: 3
└─ Lifetime: 5

伤害设置:
├─ Deal Damage: ?（勾选）
└─ Damage Per Second: 8

调试:
└─ Enable Debug: ?（开发时勾选）
```

### Step 3: 创建动画

#### 推荐的动画结构
1. **BlackHole_Activate** (1秒)
   - 黑洞从无到有展开
   - 在 0.5s 处添加事件 `StartPulling()`

2. **BlackHole_Pull** (循环)
   - 旋转、脉冲效果
   - 循环播放

3. **BlackHole_Deactivate** (0.5秒)
   - 黑洞收缩消失
   - 在 0.1s 处添加事件 `EndPulling()`
   - 在 0.45s 处添加事件 `DestroyBlackHole()`

### Step 4: 配置 Animator Controller

状态机流程：
```
Entry → Activate → Pull → Deactivate
```

过渡条件：
- Activate → Pull: Has Exit Time = true
- Pull → Deactivate: 使用 Trigger 参数或定时

---

## 在投掷系统中使用

### 方法1: 作为工具实例

在 `ToolInstanceSO` 中：
```
Tool Name: 迷你黑洞
Tool Prefab: [拖入 MiniBlackHole 预制体]
Throw Speed: 15
Drag: 0
Auto Stop Time: 0（不需要自动停止）
```

### 方法2: 在其他脚本中生成

```csharp
// 在需要生成黑洞的地方
GameObject blackHole = Instantiate(blackHolePrefab, position, Quaternion.identity);

// 可选：获取组件并配置
MiniBlackHole bh = blackHole.GetComponent<MiniBlackHole>();
bh.pullForce = 20f;  // 自定义吸引力
```

---

## 参数调优指南

### 吸引力太弱？
```
增加 Pull Force: 15 → 25
减小 Max Pull Distance: 10 → 8
```

### 吸引力太强？
```
减小 Pull Force: 15 → 10
增加 Max Pull Distance: 10 → 12
```

### 范围太小？
```
增加 Pull Radius: 4 → 6
增加 Max Pull Distance: 10 → 15
```

### 持续时间太短？
```
增加 Pull Duration: 3 → 5
增加 Lifetime: 5 → 8
```

---

## 调试技巧

### 1. 查看范围
选中黑洞对象，会在 Scene 视图中显示：
- **青色圆圈**: Pull Radius（碰撞检测范围）
- **黄色圆圈**: Max Pull Distance（最大吸引距离）

### 2. 控制台输出
启用 `Enable Debug` 后可以看到：
```
[MiniBlackHole] 黑洞生成，吸引范围: 4m，持续时间: 3s
[MiniBlackHole] 敌人进入范围: Enemy_01
[MiniBlackHole] 开始拉取敌人
[MiniBlackHole] 拉取协程启动，持续 3 秒
[MiniBlackHole] 对 Enemy_01 造成 4 伤害
```

### 3. 常见问题排查

**Q: 敌人不被吸引？**
- [ ] 检查敌人图层是否在 `Target Layers` 中
- [ ] 敌人是否有 `Rigidbody2D` 组件
- [ ] `Pull Collider` 的 `Is Trigger` 是否勾选
- [ ] 动画是否调用了 `StartPulling()`

**Q: 黑洞瞬间消失？**
- [ ] `Lifetime` 是否设置合理（至少 > Pull Duration）
- [ ] 动画事件是否过早调用 `DestroyBlackHole()`

**Q: 没有伤害？**
- [ ] `Deal Damage` 是否勾选
- [ ] 敌人是否有 `EnemyHealth` 组件
- [ ] `Damage Per Second` 是否 > 0

---

## 代码接口说明

### 公共方法

```csharp
// 开始吸引（动画事件）
public void StartPulling()

// 结束吸引（动画事件）
public void EndPulling()

// 销毁黑洞（动画事件）
public void DestroyBlackHole()
```

### 可配置属性

```csharp
public Animator animator;              // 动画组件
public CircleCollider2D pullCollider;  // 碰撞箱
public LayerMask targetLayers;         // 目标图层
public float pullForce;                // 吸引力
public float pullRadius;               // 吸引范围
public float maxPullDistance;          // 最大距离
public float pullDuration;             // 吸引时长
public float lifetime;                 // 生命周期
public bool dealDamage;                // 是否造成伤害
public float damagePerSecond;          // 每秒伤害
```

---

## 进阶玩法

### 1. 不同强度的黑洞
创建多个预制体变体：
- 小型黑洞: Pull Force = 10, Radius = 3
- 中型黑洞: Pull Force = 20, Radius = 5
- 大型黑洞: Pull Force = 30, Radius = 8

### 2. 连锁反应
在 `DestroyBlackHole()` 前生成多个小黑洞：
```csharp
// 在 MiniBlackHole 中添加
public GameObject miniBlackHolePrefab;
public int spawnCount = 3;

public void DestroyBlackHole()
{
    // 生成多个小黑洞
    for (int i = 0; i < spawnCount; i++)
    {
        Vector2 offset = Random.insideUnitCircle * 2f;
        Instantiate(miniBlackHolePrefab, transform.position + (Vector3)offset, Quaternion.identity);
    }
    
    Destroy(gameObject);
}
```

### 3. 配合其他陷阱
黑洞可以把敌人吸引到其他陷阱上：
- 黑洞 + 地刺 = 群体击杀
- 黑洞 + TNT = 爆炸连锁
- 黑洞 + 黑洞 = 超强吸力

---

## 性能优化

### 如果出现卡顿：

1. **限制同时存在的黑洞数量**
```csharp
public static int maxBlackHoles = 3;
private static int currentBlackHoles = 0;

void Start()
{
    if (currentBlackHoles >= maxBlackHoles)
    {
        Destroy(gameObject);
        return;
    }
    currentBlackHoles++;
}

void OnDestroy()
{
    currentBlackHoles--;
}
```

2. **降低更新频率**
```csharp
// 改为每2帧更新一次
private int frameCount = 0;

void FixedUpdate()
{
    frameCount++;
    if (frameCount % 2 != 0) return;
    
    if (isPulling)
    {
        PullEnemies();
    }
}
```

3. **使用对象池**
如果频繁生成黑洞，使用对象池而不是 Instantiate/Destroy。

---

## 总结

? **功能完整**: 吸引、伤害、动画控制  
? **易于配置**: 所有参数都可在 Inspector 中调整  
? **性能友好**: 使用事件驱动，按需更新  
? **扩展性强**: 可以轻松添加新功能  

现在你可以创建超酷的黑洞陷阱了！???
