using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryUI : MonoBehaviour
{
    public static MemoryUI Instance;

    [Header("UI Elements")]
    public GameObject panel; // 整个弹窗面板
    public Image memoryImage; // 显示碎片大图
    public TMP_Text contentText; // 用于显示剧情文本
    public GameObject buttonsContainer; // 包含两个按钮的父物体，方便统一隐藏
    public Button reminisceButton;
    public Button shatterButton;
    public AudioSource audioSource; // 用于播放回忆语音

    private MemoryFragment currentFragment;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panel != null) panel.SetActive(false);
        if (contentText != null) contentText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // 绑定按钮事件
        if (reminisceButton != null)
            reminisceButton.onClick.AddListener(OnReminisceClicked);
        
        if (shatterButton != null)
            shatterButton.onClick.AddListener(OnShatterClicked);
    }

    public void Open(MemoryFragment fragment)
    {
        if (fragment == null) return;
        
        currentFragment = fragment;
        
        if (buttonsContainer != null) buttonsContainer.SetActive(true);
        if (contentText != null) contentText.gameObject.SetActive(false);
        
        // Stop any previous audio
        if (audioSource != null) audioSource.Stop();
        
        if (memoryImage != null && fragment.detailImage != null)
        {
            memoryImage.sprite = fragment.detailImage;
            memoryImage.gameObject.SetActive(true);
        }
        else if (memoryImage != null)
        {
            memoryImage.gameObject.SetActive(false);
        }

        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnReminisceClicked()
    {
        if (currentFragment == null) return;

        // 不直接关闭，而是进入阅读模式
        StartCoroutine(ShowReminisceContent());
    }

    private System.Collections.IEnumerator ShowReminisceContent()
    {
        // 1. 隐藏按钮
        if (buttonsContainer != null) buttonsContainer.SetActive(false);

        // 2. 显示文本
        if (contentText != null)
        {
            contentText.text = currentFragment.memoryContent;
            contentText.gameObject.SetActive(true);
        }

        float waitTime = 3.0f; // 默认最少显示3秒，防止音频太短看不清字

        // 3. 播放语音并计算等待时间
        if (audioSource != null && currentFragment.memoryVoice != null)
        {
            audioSource.PlayOneShot(currentFragment.memoryVoice);
            
            // 如果音频长度超过3秒，就按音频长度来等
            if (currentFragment.memoryVoice.length > waitTime)
            {
                waitTime = currentFragment.memoryVoice.length;
            }
        }

        // 4. 等待 (智能时长)
        yield return new WaitForSecondsRealtime(waitTime); 
        
        // 5. 关闭 UI
        Close();

        Time.timeScale = 1f;
        currentFragment.PerformReminisceEffectOnly(); 
    }

    private void OnShatterClicked()
    {
        if (currentFragment == null) return;

        Close();
        Time.timeScale = 1f; 
        currentFragment.PerformShatter();
    }
}
