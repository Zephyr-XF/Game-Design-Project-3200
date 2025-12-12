## 备注，更改记录和需要告知队友的信息写在这里

### HYC

### LSF
- 创建了一个全新的角色prefab，名字是PlayerRed，用作正式主角
- 按下J进行普通攻击，一共有四段
- 按下K释放技能1，按下L释放技能2，按下U释放技能3
- 释放技能L会出现时停特写，按下K或者U可以解除特写。以后会做成击破的瞬间给出特写，然后可以瞬间接上技能，以及时停特效。

### WYC
- 创建了6种怪物
- 骷髅和哥布林是小怪
- 蘑菇和守卫是精英
- 魔鬼是boss
- 魔鬼做了技能
- 受击音效
- awake提示
- 

### XLH
- 创建了初版祝福系统页面prefab, 名字是BlessingSysterm
- 4位梦神会随机出现三位 背景可是视频/图片 有一定动画
- 使用流程
1.把 BlessingSystem Prefab 拖进场景。
2.在你想触发祝福的地方调用
BlessingManager.Instance.TriggerBlessing();
3.点击任意一个选项关闭页面
4.BlessingManager.Instance.CloseBlessingUI(); 强制关闭页面
5.测试按钮B
- 每个神新增了台词
- 完成人对话系统 (DialogueSysterm/Shopper1)prefab
- 将prefab拉进场景  在shopkeep脚本中挂载相应的shop canvas和shop manager
- 完成碎片回忆系统 (MemorySysterm/Trophy  draft paper)prefab
- 直接拉入场景
- 完成开场以及结尾动画 OpeningScene EndingScene
- 动画播放完毕后可以设置特定场景跳转
---

## 待办事项（共同编辑）
### 场景：
- 深层梦境与浅层梦境
- 休息房
- 问号房
- 房间串联，制作一场完整游戏


### 怪物：
- 更多种类的boss与技能


### 角色：
- 深层梦境与浅层梦境的滤镜


### 祝福：
- 房间偶尔散布的记忆碎片
- 神秘商人
- 受污染的祝福（更加强力，选择会降低清醒值，图片稍微变黑（显得受污染）


### 其他：
- 录制demo
