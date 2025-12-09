# MiniBlackHole 动画设置指南

## 1. 动画控制器结构

### 推荐的动画状态
```
Idle (空闲) → Activate (激活) → Pull (吸引) → Deactivate (消失)
```

---

## 2. 动画事件设置

### 在 Activate（激活）动画中
在动画完全展开的那一帧添加事件：
- **事件函数**: `StartPulling()`
- **时机**: 动画播放到 50%-70% 时（黑洞完全展开）

### 在 Pull（吸引）动画中（可选）
如果你有循环的吸引动画：
- 这个状态会持续播放
- 不需要添加特殊事件

### 在 Deactivate（消失）动画中
在动画结束前添加事件：
- **事件函数**: `EndPulling()`
- **时机**: 动画播放到 10%-20% 时（开始收缩）

在动画最后一帧添加事件：
- **事件函数**: `DestroyBlackHole()`
- **时机**: 动画播放到 95%-100% 时

---

## 3. Animator Controller 参数（可选）

如果需要通过代码控制动画：
```
参数名: IsActive (Bool)
参数名: Pull (Trigger)
参数名: Deactivate (Trigger)
```

---

## 4. Unity 中的设置步骤

### 步骤 1：创建 Animator Controller
1. 在 Project 窗口右键 → Create → Animator Controller
2. 命名为 `MiniBlackHole_Animator`

### 步骤 2：创建动画片段
1. 选中黑洞预制体
2. Window → Animation → Animation
3. 创建以下动画：
   - `BlackHole_Activate.anim` (0.5-1秒)
   - `BlackHole_Pull.anim` (循环动画)
   - `BlackHole_Deactivate.anim` (0.5-1秒)

### 步骤 3：添加动画事件
1. 打开 Animation 窗口
2. 选择 `BlackHole_Activate` 动画
3. 点击时间轴上要添加事件的位置
4. 点击上方的 "添加事件" 按钮（小旗子图标）
5. 在 Inspector 中设置函数名为 `StartPulling`

重复以上步骤为其他动画添加事件。

### 步骤 4：配置状态机
```
[Entry] → Activate → Pull → Deactivate → [Exit]
```

过渡条件：
- Activate → Pull: `Has Exit Time = true`, Exit Time = 1.0
- Pull → Deactivate: `Has Exit Time = false`, 使用 Trigger 参数
- 或者：Pull 自动过渡到 Deactivate（`pullDuration` 秒后）

---

## 5. 组件配置

### 在 Unity Inspector 中设置 MiniBlackHole 组件：

#### 组件引用
- **Animator**: 拖入动画组件
- **Pull Collider**: 自动创建（或手动拖入）

#### 吸引设置
- **Target Layers**: 选择 `Enemy` 层
- **Pull Force**: 10-20（吸引力强度）
- **Pull Radius**: 3-5（碰撞箱范围）
- **Max Pull Distance**: 8-15（最大吸引距离）
- **Pull Duration**: 3（吸引持续时间，秒）
- **Lifetime**: 5（黑洞总生命周期，秒）

#### 伤害设置（可选）
- **Deal Damage**: ?（勾选启用伤害）
- **Damage Per Second**: 5-10

#### 调试
- **Enable Debug**: ?（开发时勾选）

---

## 6. 示例动画时间轴

```
Activate 动画 (1秒)
├─ 0.0s: 黑洞开始出现
├─ 0.5s: 黑洞完全展开
│         ↓ 添加事件: StartPulling()
└─ 1.0s: 进入 Pull 状态

Pull 动画 (循环，3秒)
├─ 旋转、脉冲效果
└─ 自动结束后进入 Deactivate

Deactivate 动画 (0.5秒)
├─ 0.0s: 开始收缩
│         ↓ 添加事件: EndPulling()
├─ 0.4s: 几乎消失
│         ↓ 添加事件: DestroyBlackHole()
└─ 0.5s: 完全消失
```

---

## 7. 代码调用示例

### 从工具投掷系统生成黑洞
```csharp
// 在 ToolThrow.cs 或其他脚本中
GameObject blackHole = Instantiate(blackHolePrefab, position, Quaternion.identity);
MiniBlackHole blackHoleScript = blackHole.GetComponent<MiniBlackHole>();

// 可选：立即开始吸引（如果不通过动画事件）
// blackHoleScript.StartPulling();
```

---

## 8. 调试提示

### 在 Scene 视图中查看范围
- 选中黑洞对象
- 会显示青色圆圈（Pull Radius）
- 会显示黄色圆圈（Max Pull Distance）

### 控制台输出
启用 `Enable Debug` 后会看到：
```
[MiniBlackHole] 黑洞生成，吸引范围: 3m，持续时间: 3s
[MiniBlackHole] 敌人进入范围: Enemy_01
[MiniBlackHole] 开始拉取敌人
[MiniBlackHole] 拉取协程启动，持续 3 秒
[MiniBlackHole] 对 Enemy_01 造成 2.5 伤害
[MiniBlackHole] 停止拉取敌人
[MiniBlackHole] 黑洞销毁
```

---

## 9. 常见问题

### Q: 敌人不被吸引？
A: 检查：
1. 敌人图层是否在 `Target Layers` 中
2. 敌人是否有 `Rigidbody2D` 组件
3. `Pull Collider` 的 `Is Trigger` 是否勾选
4. 是否调用了 `StartPulling()`

### Q: 黑洞立即消失？
A: 检查：
1. `Lifetime` 是否设置得太短
2. 动画事件是否过早调用了 `DestroyBlackHole()`

### Q: 吸引力太弱？
A: 调整：
1. 增加 `Pull Force` 值
2. 减小 `Max Pull Distance` 值
3. 确保敌人的 `Rigidbody2D` 的 `Mass` 不要太大

---

## 10. 性能优化建议

1. **限制黑洞数量**: 同时存在的黑洞不要超过 3-5 个
2. **调整 FixedUpdate 频率**: 如果卡顿，可以每 2-3 帧更新一次吸引力
3. **使用对象池**: 如果频繁生成黑洞，使用对象池复用

---

## 完成清单

- [ ] 创建 Animator Controller
- [ ] 创建 3 个动画片段（Activate, Pull, Deactivate）
- [ ] 在 Activate 动画中添加 `StartPulling()` 事件
- [ ] 在 Deactivate 动画中添加 `EndPulling()` 和 `DestroyBlackHole()` 事件
- [ ] 配置状态机过渡
- [ ] 在 Inspector 中配置 MiniBlackHole 组件参数
- [ ] 设置 Target Layers 为 Enemy
- [ ] 测试吸引效果
- [ ] 调整吸引力参数

祝你制作出酷炫的黑洞效果！??
