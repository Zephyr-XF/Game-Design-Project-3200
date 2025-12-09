Shader "Custom/DreamyFilterModified"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // RGB 强度 (原有功能)
        _RedIntensity ("Red Intensity", Range(0.0, 5.0)) = 1.0
        _GreenIntensity ("Green Intensity", Range(0.0, 5.0)) = 1.0
        _BlueIntensity ("Blue Intensity", Range(0.0, 5.0)) = 1.0

        // RGB 饱和度 (新增功能)
        _RedSat ("Red Saturation", Range(0.0, 2.0)) = 1.0
        _GreenSat ("Green Saturation", Range(0.0, 2.0)) = 1.0
        _BlueSat ("Blue Saturation", Range(0.0, 2.0)) = 1.0

        // 模糊设置
        _BlurSize ("Blur Radius", Range(0, 10)) = 1
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
            
            float _RedIntensity;
            float _GreenIntensity;
            float _BlueIntensity;
            
            float _RedSat;
            float _GreenSat;
            float _BlueSat;

            int   _BlurSize;
            float _HazeIntensity;

            // ================= 辅助函数：RGB 与 HSV 转换 =================
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            // ================= 核心逻辑：调整特定颜色的饱和度 =================
            float3 AdjustSpecificSaturation(float3 color)
            {
                float3 hsv = RGBtoHSV(color);
                float hue = hsv.x;

                // 定义颜色的色相范围 (0-1)
                // 绿色范围: 约 0.25 - 0.5 (90度到180度之间)
                if (hue > 0.2 && hue < 0.5) 
                {
                    hsv.y *= _GreenSat;
                }
                // 蓝色范围: 约 0.5 - 0.85 (180度到300度之间)
                else if (hue >= 0.5 && hue < 0.85) 
                {
                    hsv.y *= _BlueSat;
                }
                // 红色范围: 跨越 0 和 1 (300度到60度之间)
                else if (hue >= 0.85 || hue <= 0.2) 
                {
                    hsv.y *= _RedSat;
                }

                return HSVtoRGB(hsv);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ----- 第1步: 获取原始颜色 -----
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // ----- 第2步: 计算模糊颜色 -----
                float3 blurredSum = float3(0.0, 0.0, 0.0);
                int sampleCount = 0;
                
                // 简单的盒式模糊
                for (int y = -_BlurSize; y <= _BlurSize; y++)
                {
                    for (int x = -_BlurSize; x <= _BlurSize; x++)
                    {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
                        blurredSum += tex2D(_MainTex, i.uv + offset).rgb;
                        sampleCount++;
                    }
                }
                float3 blurredColor = blurredSum / sampleCount;

                // ----- 第3步: 应用特定颜色的饱和度调整 -----
                // 我们对清晰图和模糊图分别做颜色调整
                // 这样如果把绿色变灰，那么绿色的物体发出的光（Blur）也会变灰
                float3 satAdjustedClear = AdjustSpecificSaturation(originalColor.rgb);
                float3 satAdjustedBlur = AdjustSpecificSaturation(blurredColor);

                // ----- 第4步: 应用 RGB Intensity 强度调整 (原有功能) -----
                float3 rgbMultiplier = float3(_RedIntensity, _GreenIntensity, _BlueIntensity);
                
                float3 finalClear = satAdjustedClear * rgbMultiplier;
                float3 finalBlur = satAdjustedBlur * rgbMultiplier;

                // ----- 第5步: 混合清晰与模糊 (朦胧效果) -----
                float3 finalColor = lerp(finalClear, finalBlur, _HazeIntensity);

                return fixed4(finalColor, originalColor.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}





