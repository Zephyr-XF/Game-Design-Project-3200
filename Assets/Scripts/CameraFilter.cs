using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraFilterModified : MonoBehaviour
{
    // 将新的Shader文件拖到这里
    public Shader filterShader;

    [Header("RGB Intensity (Color Tint)")]
    [Range(0.0f, 5.0f)]
    public float redIntensity = 1.0f;
    [Range(0.0f, 5.0f)]
    public float greenIntensity = 1.0f;
    [Range(0.0f, 5.0f)]
    public float blueIntensity = 1.0f;

    [Header("Color Saturation Control")]
    [Tooltip("降低此值可使红色物体变灰")]
    [Range(0.0f, 2.0f)]
    public float redSaturation = 1.0f;

    [Tooltip("降低此值可使绿色物体变灰")]
    [Range(0.0f, 2.0f)]
    public float greenSaturation = 1.0f;

    [Tooltip("降低此值可使蓝色物体变灰")]
    [Range(0.0f, 2.0f)]
    public float blueSaturation = 1.0f;

    [Header("Haze & Blur Settings")]
    [Tooltip("The radius of the blur in pixels.")]
    [Range(0, 10)]
    public int blurSize = 1;

    [Range(0.0f, 1.0f)]
    public float hazeIntensity = 0.3f;

    private Material material;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (filterShader != null)
        {
            if (material == null)
            {
                material = new Material(filterShader);
            }

            // 传递 RGB 强度
            material.SetFloat("_RedIntensity", redIntensity);
            material.SetFloat("_GreenIntensity", greenIntensity);
            material.SetFloat("_BlueIntensity", blueIntensity);

            // 传递 RGB 饱和度设置 (新增)
            material.SetFloat("_RedSat", redSaturation);
            material.SetFloat("_GreenSat", greenSaturation);
            material.SetFloat("_BlueSat", blueSaturation);

            // 传递 模糊设置
            material.SetInt("_BlurSize", blurSize);
            material.SetFloat("_HazeIntensity", hazeIntensity);

            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }
}



