using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraFilterModified : MonoBehaviour
{
    public Shader filterShader;

    [Header("--- 动态控制 (由Manager脚本控制) ---")]
    [Tooltip("0 = 正常画风, 1 = 完全进入击破时停状态")]
    [Range(0f, 1f)] public float breakEffectIntensity = 0f;

    // ========================================================================
    // 基础画风设置 (平时游戏的样子)
    // ========================================================================
    [Header("--- 基础画风设置 (平时常驻) ---")]
    [Header("Color Tint (RGB Intensity)")]
    [Range(0.0f, 5.0f)] public float baseRedIntensity = 1.0f;
    [Range(0.0f, 5.0f)] public float baseGreenIntensity = 1.0f;
    [Range(0.0f, 5.0f)] public float baseBlueIntensity = 1.0f;

    [Header("Saturation (Color Vibrance)")]
    [Range(0.0f, 2.0f)] public float baseRedSat = 1.0f;
    [Range(0.0f, 2.0f)] public float baseGreenSat = 1.0f;
    [Range(0.0f, 2.0f)] public float baseBlueSat = 1.0f;

    [Header("Blur & Haze")]
    [Range(0, 10)] public int baseBlurSize = 0;
    [Range(0.0f, 1.0f)] public float baseHazeIntensity = 0.0f;

    // ========================================================================
    // 击破模式设置 (目标值)
    // ========================================================================
    [Header("--- 击破特效设置 (基于基础值的变化) ---")]

    [Tooltip("时停时的【整体亮度倍率】。\n1.0 = 保持原样\n>1.0 = 画面变亮(过曝)\n<1.0 = 画面变暗")]
    public float breakIntensityMultiplier = 1.5f;

    [Tooltip("时停时的【整体饱和度系数】。\n0.0 = 完全黑白\n1.0 = 保持原有色彩")]
    [Range(0f, 1f)]
    public float breakSaturationMultiplier = 0.0f;

    [Tooltip("时停时，画面的模糊程度 (直接过渡到此值)")]
    public int breakTargetBlur = 3;

    [Tooltip("时停时，画面的朦胧/发光程度 (直接过渡到此值)")]
    public float breakTargetHaze = 0.5f;


    private Material material;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (filterShader != null)
        {
            if (material == null) material = new Material(filterShader);

            // ===========================================================
            // 核心修改：使用乘法倍率，保留原有的 RGB 比例 (色调)
            // ===========================================================

            // 1. 计算当前的 亮度倍率 (从 1.0 过渡到 设置的倍率)
            float currentIntensityMult = Mathf.Lerp(1.0f, breakIntensityMultiplier, breakEffectIntensity);

            float currRedInt = baseRedIntensity * currentIntensityMult;
            float currGreenInt = baseGreenIntensity * currentIntensityMult;
            float currBlueInt = baseBlueIntensity * currentIntensityMult;

            // 2. 计算当前的 饱和度系数 (从 1.0 过渡到 设置的系数)
            float currentSatMult = Mathf.Lerp(1.0f, breakSaturationMultiplier, breakEffectIntensity);

            float currRedSat = baseRedSat * currentSatMult;
            float currGreenSat = baseGreenSat * currentSatMult;
            float currBlueSat = baseBlueSat * currentSatMult;

            // 3. 模糊和朦胧依然是线性过渡
            int currBlur = (int)Mathf.Lerp(baseBlurSize, breakTargetBlur, breakEffectIntensity);
            float currHaze = Mathf.Lerp(baseHazeIntensity, breakTargetHaze, breakEffectIntensity);

            // ===========================================================
            // 传递给 Shader
            // ===========================================================
            material.SetFloat("_RedIntensity", currRedInt);
            material.SetFloat("_GreenIntensity", currGreenInt);
            material.SetFloat("_BlueIntensity", currBlueInt);

            material.SetFloat("_RedSat", currRedSat);
            material.SetFloat("_GreenSat", currGreenSat);
            material.SetFloat("_BlueSat", currBlueSat);

            material.SetInt("_BlurSize", currBlur);
            material.SetFloat("_HazeIntensity", currHaze);

            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    public void SetBreakIntensity(float value)
    {
        breakEffectIntensity = Mathf.Clamp01(value);
    }

    void OnDisable()
    {
        if (material != null) DestroyImmediate(material);
    }

    // ========================================================================
    // 新增的函数接口
    // ========================================================================

    /// <summary>
    /// 应用预设A: 一个高对比度、偏品红色的清晰风格。
    /// </summary>
    [ContextMenu("应用预设A (品红/清晰)")]
    public void ApplyPresetA()
    {
        // 设置 RGB 强度
        baseRedIntensity = 1f;
        baseGreenIntensity = 0.62f;
        baseBlueIntensity = 1.69f;

        // 设置 RGB 饱和度
        baseRedSat = 1f;
        baseGreenSat = 0.172f;
        baseBlueSat = 1f;

        // 设置模糊与朦胧
        baseBlurSize = 0;
        baseHazeIntensity = 0f;
    }

    /// <summary>
    /// 应用预设B: 一个低亮度、偏冷蓝色、带有模糊和朦胧感的风格。
    /// </summary>
    [ContextMenu("应用预设B (冷蓝/模糊)")]
    public void ApplyPresetB()
    {
        // 设置 RGB 强度
        baseRedIntensity = 0.38f;
        baseGreenIntensity = 0.12f;
        baseBlueIntensity = 1.41f;

        // 设置 RGB 饱和度
        baseRedSat = 0.8f;
        baseGreenSat = 0.172f;
        baseBlueSat = 0.82f;

        // 设置模糊与朦胧
        baseBlurSize = 3;
        baseHazeIntensity = 0.465f;
    }
}





