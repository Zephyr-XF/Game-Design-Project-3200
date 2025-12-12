using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryUI : MonoBehaviour
{
    public static MemoryUI Instance;

    [Header("UI Elements")]
    public GameObject panel; 
    public Image memoryImage; 
    public RawImage videoDisplay; // 现在改名叫 shatterTarget 可能更合适，但保留原名也可以
    public TMP_Text contentText; 
    public GameObject buttonsContainer; 
    public Button reminisceButton;
    public Button shatterButton;
    public AudioSource audioSource;
    // VideoPlayer 引用已移除

    private MemoryFragment currentFragment;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panel != null) panel.SetActive(false);
        if (contentText != null) contentText.gameObject.SetActive(false);
        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
    }
    // SetupVideoTexture 方法已移除

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

        // 1. 深度清理战场
        var oldDebris = GameObject.FindGameObjectsWithTag("Untagged"); 
        foreach(var obj in oldDebris) {
            if(obj.name.StartsWith("Debris_Root")) Destroy(obj);
        }
        
        if (buttonsContainer != null) buttonsContainer.SetActive(true);
        if (contentText != null) contentText.gameObject.SetActive(false);
        
        if (audioSource != null) audioSource.Stop();
        
        // 隐藏旧 Image
        if (memoryImage != null) memoryImage.gameObject.SetActive(false);
        
        // 设置 Raw Image
        if (videoDisplay != null)
        {
             if (fragment.detailImage != null)
             {
                 videoDisplay.texture = fragment.detailImage.texture; 
                 // 2. 强制复活 RawImage
                 videoDisplay.gameObject.SetActive(true);
                 videoDisplay.color = Color.white;
                 videoDisplay.transform.localScale = Vector3.one;
                 videoDisplay.transform.localRotation = Quaternion.identity;
             }
             else
             {
                 videoDisplay.gameObject.SetActive(false);
             }
        }
        else if (videoDisplay != null)
        {
             videoDisplay.gameObject.SetActive(false);
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

        // 视频保持暂停 (定格状态)
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

        float waitTime = 3.0f; // 默认最少显示3秒

        // 3. 播放语音并计算等待时间
        if (audioSource != null && currentFragment.memoryVoice != null)
        {
            audioSource.PlayOneShot(currentFragment.memoryVoice);
            if (currentFragment.memoryVoice.length > waitTime)
            {
                waitTime = currentFragment.memoryVoice.length;
            }
        }

        // 4. 等待
        yield return new WaitForSecondsRealtime(waitTime); 
        
        // 5. 关闭 UI
        Close();

        Time.timeScale = 1f;
        currentFragment.PerformReminisceEffectOnly(); 
    }

    private void OnShatterClicked()
    {
        if (currentFragment == null) return;
        StartCoroutine(PlayShatterEffectAndClose());
    }

    private System.Collections.IEnumerator PlayShatterEffectAndClose()
    {
        // 1. 隐藏按钮
        if (buttonsContainer != null) buttonsContainer.SetActive(false);

        // 2. 恢复时间！让物理引擎动起来！
        Time.timeScale = 1f;

        // 3. 只有 RawImage 是可见的，我们炸碎它！
        if (UiImageShatter.Instance != null && videoDisplay != null && videoDisplay.texture != null)
        {
            UiImageShatter.Instance.Shatter(videoDisplay);
        }

        // 4. 等待碎裂动画差不多掉落完
        yield return new WaitForSeconds(3.0f); // 也就是我们看碎渣飞的时间

        // 5. 关闭并应用效果
        Close();
        // Time.timeScale 已经是 1 了，不用再设
        currentFragment.PerformShatter();
    }
}
