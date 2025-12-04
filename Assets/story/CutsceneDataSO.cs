using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CutsceneSlide
{
    [Header("画面内容")]
    public Sprite image; // 图片
    [TextArea(3, 5)]
    public string text;  // 字幕
    
    [Header("音频 (可选)")]
    public AudioClip voiceover; // 配音
    
    [Header("设置")]
    public float displayTime = 5f; // 如果没有配音，这张图展示多久
}

[CreateAssetMenu(fileName = "NewCutsceneData", menuName = "Story/Cutscene Data")]
public class CutsceneDataSO : ScriptableObject
{
    [Header("跳转设置")]
    public string nextSceneName = "Level1"; // 动画播完后去哪个场景

    [Header("幻灯片内容")]
    public List<CutsceneSlide> slides = new List<CutsceneSlide>();
}
