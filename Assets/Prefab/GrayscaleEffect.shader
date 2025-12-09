Shader "Hidden/GrayscaleEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Effect Intensity", Range(0.0, 1.0)) = 1.0 // 添加一个强度参数
    }
    SubShader
    {
        // No culling or depth
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
            float _Intensity; // 接收脚本传来的强度

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // 计算灰度值 (Luminance)
                float grayscale = dot(col.rgb, float3(0.299, 0.587, 0.114));
                // 创建一个灰度颜色
                fixed3 grayCol = fixed3(grayscale, grayscale, grayscale);
                // 使用_Intensity在原始颜色和灰度颜色之间插值
                col.rgb = lerp(col.rgb, grayCol, _Intensity);
                return col;
            }
            ENDCG
        }
    }
}

