using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("说话者的名字")]
    public string speakerName;
    
    [Tooltip("说话者的头像（可选）")]
    public Sprite portrait;

    [Tooltip("对话内容")]
    [TextArea(3, 10)]
    public string text;

    [Header("分支选项 (如果有，就不会自动跳下一句)")]
    public List<DialogueChoice> choices;
}

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("按钮上显示的文字")]
    public string buttonText;

    [Tooltip("点击后的行为")]
    public ChoiceActionType actionType;

    [Tooltip("点击后跳转到的新对话数据 (仅当 ActionType 为 OpenNextDialogue 时有效)")]
    public DialogueDataSO nextDialogue;
}

public enum ChoiceActionType
{
    OpenNextDialogue, // 跳转下一段对话
    OpenShop,         // 打开商店
    CloseDialogue     // 结束对话
}

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueDataSO : ScriptableObject
{
    [Tooltip("对话内容列表")]
    public List<DialogueLine> lines;

}
