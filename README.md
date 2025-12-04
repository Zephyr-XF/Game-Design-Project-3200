## 备注，更改记录和需要告知队友的信息写在这里

### HYC

### LSF
- 创建了一个全新的角色prefab，名字是PlayerRed，用作正式主角
- 按下J进行普通攻击，一共有四段
- 按下K释放技能1，按下L释放技能2，按下U释放技能3
- 释放技能L会出现时停特写，按下K或者U可以解除特写。以后会做成击破的瞬间给出特写，然后可以瞬间接上技能，以及时停特效。

### WYC

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
