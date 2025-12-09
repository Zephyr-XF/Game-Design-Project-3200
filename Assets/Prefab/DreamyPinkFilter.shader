Shader "Custom/DreamyFilterModified"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // RGB 强度
        _RedIntensity ("Red Intensity", Float) = 1.0
        _GreenIntensity ("Green Intensity", Float) = 1.0
        _BlueIntensity ("Blue Intensity", Float) = 1.0

        // RGB 饱和度
        _RedSat ("Red Saturation", Float) = 1.0
        _GreenSat ("Green Saturation", Float) = 1.0
        _BlueSat ("Blue Saturation", Float) = 1.0

        // 模糊与发光
        _BlurSize ("Blur Radius", Int) = 1
        _HazeIntensity("Haze Intensity", Range(0.0, 1.0)) = 0.3
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            // 变量由 C# 脚本每帧传入
            float _RedIntensity;
            float _GreenIntensity;
            float _BlueIntensity;
            
            float _RedSat;
            float _GreenSat;
            float _BlueSat;

            int   _BlurSize;
            float _HazeIntensity;

            // RGB 转 HSV 辅助函数
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // HSV 转 RGB 辅助函数
            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            // 调整特定色相的饱和度
            float3 AdjustSpecificSaturation(float3 color)
            {
                float3 hsv = RGBtoHSV(color);
                float hue = hsv.x;

                // 根据色相范围应用不同的饱和度倍率
                // 注意：如果 C# 传入的 _RedSat 等全部趋近于 0，这里输出的就是黑白
                if (hue > 0.2 && hue < 0.5) 
                {
                    hsv.y *= _GreenSat;
                }
                else if (hue >= 0.5 && hue < 0.85) 
                {
                    hsv.y *= _BlueSat;
                }
                else 
                {
                    hsv.y *= _RedSat;
                }

                return HSVtoRGB(hsv);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 采样原始颜色
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // 2. 采样并计算模糊颜色 (简单的 Box Blur)
                // 优化：如果 BlurSize 为 0，直接使用原始颜色，省去循环
                float3 blurredColor = originalColor.rgb;
                
                if (_BlurSize > 0)
                {
                    float3 blurredSum = float3(0.0, 0.0, 0.0);
                    int sampleCount = 0;
                    for (int y = -_BlurSize; y <= _BlurSize; y++)
                    {
                        for (int x = -_BlurSize; x <= _BlurSize; x++)
                        {
                            float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
                            blurredSum += tex2D(_MainTex, i.uv + offset).rgb;
                            sampleCount++;
                        }
                    }
                    blurredColor = blurredSum / sampleCount;
                }

                // 3. 应用饱和度调整 (保持色调的关键步骤)
                float3 satAdjustedClear = AdjustSpecificSaturation(originalColor.rgb);
                float3 satAdjustedBlur = AdjustSpecificSaturation(blurredColor);

                // 4. 应用 RGB 强度 (亮度)
                // C# 已经根据 Base * Multiplier 计算好了具体的值，这里直接乘即可
                float3 rgbMultiplier = float3(_RedIntensity, _GreenIntensity, _BlueIntensity);
                
                float3 finalClear = satAdjustedClear * rgbMultiplier;
                float3 finalBlur = satAdjustedBlur * rgbMultiplier;

                // 5. 混合清晰层和模糊层 (Haze 效果)
                float3 finalColor = lerp(finalClear, finalBlur, _HazeIntensity);

                return fixed4(finalColor, originalColor.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}





