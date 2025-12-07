using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 使用 TextMeshPro
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI 组件")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Image portraitImage;
    public GameObject interactHint; // "按 F 互动" 的提示 UI
    
    [Header("分支选项组件")]
    public GameObject choicesPanel; // 放按钮的父物体 (Grid/Vertical Layout Group)
    public GameObject choiceButtonPrefab; // 按钮预制体 (包含 Button 和 TextMeshProUGUI)

    [Header("打字机效果设置")]
    public float typingSpeed = 0.02f;

    private Queue<DialogueLine> sentences;
    private bool isDialogueActive = false;
    private bool isWaitingForChoice = false; // 是否正在等待玩家做选择
    private DialogueDataSO currentDialogueData;
    private DialogueLine currentLine; // 当前正在显示的句子
    private Coroutine typingCoroutine;
    private float dialogueStartTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sentences = new Queue<DialogueLine>();
        isDialogueActive = false;
        
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactHint != null) interactHint.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    private void Update()
    {
        // 如果正在等待选择，或者是打字机还没打完，禁止按 F 跳过
        if (isWaitingForChoice) return;

        // 如果对话正在进行，检测输入以进行下一句
        // 增加一个小延迟防止启动对话的那一次按键直接跳过第一句
        if (isDialogueActive && Time.time > dialogueStartTime + 0.1f)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                DisplayNextSentence();
            }
        }
    }

    // --- 外部调用接口 ---

    // 显示/隐藏 "按F互动" 提示
    public void ShowInteractHint(bool show)
    {
        if (interactHint != null && !isDialogueActive) // 如果正在对话中，不要显示提示
        {
            interactHint.SetActive(show);
        }
    }

    public void StartDialogue(DialogueDataSO dialogueData)
    {
        dialogueStartTime = Time.time; // 记录开始时间
        currentDialogueData = dialogueData;
        isDialogueActive = true;
        isWaitingForChoice = false;
        
        // 隐藏互动提示
        if (interactHint != null) interactHint.SetActive(false);

        // 显示对话面板
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        // 隐藏选项面板（以防万一）
        if (choicesPanel != null) choicesPanel.SetActive(false);

        // 清空之前的句子
        sentences.Clear();

        foreach (DialogueLine line in dialogueData.lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // 如果还在排队，就不要打断（除非我们要强制切）
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = sentences.Dequeue();

        // 设置UI
        if (nameText != null) nameText.text = currentLine.speakerName;
        
        if (portraitImage != null)
        {
            if (currentLine.portrait != null)
            {
                portraitImage.sprite = currentLine.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        // 打字机效果
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine line)
    {
        string sentence = line.text;
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char letter in sentence.ToCharArray())
            {
                dialogueText.text += letter;
                // 使用 Realtime 以支持暂停时对话
                yield return new WaitForSecondsRealtime(typingSpeed); 
            }
        }

        // 打字结束后，检查有没有选项
        if (line.choices != null && line.choices.Count > 0)
        {
            DisplayChoices(line.choices);
        }
    }

    private void DisplayChoices(List<DialogueChoice> choices)
    {
        if (choicesPanel == null || choiceButtonPrefab == null) return;

        isWaitingForChoice = true;
        choicesPanel.SetActive(true);

        // 清除旧按钮
        foreach (Transform child in choicesPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // 生成新按钮
        foreach (DialogueChoice choice in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesPanel.transform);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();

            if (btnText != null) btnText.text = choice.buttonText;

            // 绑定点击事件
            btn.onClick.AddListener(() => OnChoiceSelected(choice));
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        // 隐藏选项面板
        if (choicesPanel != null) choicesPanel.SetActive(false);
        isWaitingForChoice = false;

        // 根据 Action 执行操作
        switch (choice.actionType)
        {
            case ChoiceActionType.OpenNextDialogue:
                if (choice.nextDialogue != null)
                {
                    StartDialogue(choice.nextDialogue);
                }
                else
                {
                    EndDialogue(); // 如果没配置下一个，就直接结束
                }
                break;

            case ChoiceActionType.OpenShop:
                EndDialogue();
                OpenShop();
                break;

            case ChoiceActionType.CloseDialogue:
                EndDialogue();
                break;
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);

        Debug.Log("对话结束");
    }

    private void OpenShop()
    {
        // 这里尝试调用 MerchantNPC 关联的 ShopKeeper
        // 由于 Manager 是全局的，我们不知道当前跟哪个 NPC 说话
        // 简单处理：查找当前激活的 MerchantNPC 的 ShopKeeper，或者让 MerchantNPC 自己去 Open
        
        Debug.Log("尝试打开商店...");
        // 临时方案：直接通过 Find 找（以后可以优化）
        var shopKeeper = FindObjectOfType<ShopKeeper>();
        if (shopKeeper != null)
        {
            shopKeeper.OpenShopUI();
        }
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
