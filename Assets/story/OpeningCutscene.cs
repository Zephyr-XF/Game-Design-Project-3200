using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class OpeningCutscene : MonoBehaviour
{
    [Header("数据文件")]
    public CutsceneDataSO cutsceneData; // 拖入您创建的 SO 文件

    [Header("UI 组件")]
    public Image displayImage;       // 用于显示大图
    public TMP_Text subtitleText;    // 用于显示字幕
    public AudioSource audioSource;  // 用于播放配音
    public Button skipButton;        // 跳过按钮

    [Header("配置")]
    public float fadeDuration = 1.0f;       // 淡入淡出时间

    private void Start()
    {
        // 检查数据是否赋值
        if (cutsceneData == null)
        {
            Debug.LogError("请在 Inspector 中赋值 Cutscene Data！");
            return;
        }

        // 绑定跳过按钮
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCutscene);

        // 开始播放
        StartCoroutine(PlayCutsceneSequence());
    }

    private IEnumerator PlayCutsceneSequence()
    {
        // 初始状态：完全透明
        displayImage.color = new Color(1, 1, 1, 0);
        subtitleText.text = "";

        foreach (var slide in cutsceneData.slides)
        {
            // 1. 设置内容
            displayImage.sprite = slide.image;
            subtitleText.text = slide.text;

            // 2. 淡入图片
            yield return FadeImage(0f, 1f);

            // 3. 播放音频 (如果有)
            float waitTime = slide.displayTime;
            if (slide.voiceover != null)
            {
                audioSource.clip = slide.voiceover;
                audioSource.Play();
                waitTime = slide.voiceover.length + 0.5f; // 多停留0.5秒
            }

            // 4. 等待播放结束
            yield return new WaitForSeconds(waitTime);

            // 5. 淡出图片 (如果是最后一张，可以直接黑屏)
            yield return FadeImage(1f, 0f);
        }

        // 播放完毕，进入游戏
        LoadNextScene();
    }

    private IEnumerator FadeImage(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color c = displayImage.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            displayImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        displayImage.color = new Color(c.r, c.g, c.b, endAlpha);
    }

    public void SkipCutscene()
    {
        StopAllCoroutines();
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        Debug.Log("开场动画结束，进入下一关: " + cutsceneData.nextSceneName);
        SceneManager.LoadScene(cutsceneData.nextSceneName);
    }
}
