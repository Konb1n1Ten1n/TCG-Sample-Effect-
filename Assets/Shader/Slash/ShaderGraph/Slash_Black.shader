Shader "Custom/Slash_Black"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
        [Range(0, 1)] _CircleRadius("Circle Radius", Float) = 0.5
        [Range(0, 1)] _CircleSoftness("Circle Softness", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _CircleRadius;
                float _CircleSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                // UV座標を中心を(0,0)とする座標系に変換
                float2 centerUV = IN.uv - 0.5;
                
                // 中心からの距離を計算
                float dist = length(centerUV);
                
                // smoothstepで円の境界をぼやけさせる
                // _CircleRadiusより内側は1、外側は0、境界は滑らかに補間
                float circle = 1.0 - smoothstep(_CircleRadius - _CircleSoftness, _CircleRadius, dist);
                
                // 円の内側を黒くする
                color.rgb = lerp(color.rgb, float3(0, 0, 0), circle);
                
                // 円の部分だけ不透明に、それ以外は透明に
                color.a = circle;
                
                return color;
            }
            ENDHLSL
        }
    }
}
