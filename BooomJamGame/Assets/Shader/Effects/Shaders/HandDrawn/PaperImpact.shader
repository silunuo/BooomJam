// ============================================================================
// PaperImpact.shader：手绘纸板受击贴片
//
// 用途：
//   1. 读取手绘爆点贴图的 Alpha，做贴在平面上的卡通受击图
//   2. 用红黄双色给中间和外圈做统一调色
//   3. 保留贴图里的黑色线稿和纸面颗粒
// ============================================================================

Shader "BooomJam/Effects/PaperImpact"
{
    // ================================================================
    //  材质参数
    // ================================================================
    Properties
    {
        // 手绘爆点贴图，建议使用带透明背景的 PNG。
        [MainTexture]_MainTex ("Main Texture", 2D) = "white" {}

        // 外圈颜色，默认偏红。
        [HDR]_OuterColor ("Outer Color", Color) = (1.0, 0.16, 0.06, 1.0)

        // 内圈颜色，默认偏黄。
        [HDR]_InnerColor ("Inner Color", Color) = (1.0, 0.86, 0.12, 1.0)

        // 贴图原色参与比例，数值越大越接近原手绘图。
        _TextureInfluence ("Texture Influence", Range(0.0, 1.0)) = 0.55

        // 黑色线稿保留阈值，数值越大，更多暗部会保留下来。
        _LineThreshold ("Line Threshold", Range(0.02, 0.55)) = 0.24

        // 颜色对比度，数值越大越硬。
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.05

        // 整体透明度，脚本播放时也会动态写入这个值。
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0

        // 播放开头的白黄闪一下强度，由脚本动态写入。
        _Flash ("Flash", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _OuterColor;
                half4 _InnerColor;
                half _TextureInfluence;
                half _LineThreshold;
                half _Contrast;
                half _Alpha;
                half _Flash;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = saturate(tex.a * _Alpha * input.color.a);

                half luma = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half2 centeredUv = input.uv - half2(0.5, 0.5);
                half radial = saturate(1.0 - length(centeredUv) * 1.7);
                half brightMask = saturate((luma - 0.08) / 0.82);
                half innerMask = saturate(max(radial, brightMask * 0.55));

                half3 palette = lerp(_OuterColor.rgb, _InnerColor.rgb, innerMask);
                half3 paperColor = lerp(palette, palette * max(tex.rgb, half3(0.18, 0.18, 0.18)) * 1.25, _TextureInfluence);

                // 暗部按原贴图走，黑色线稿就不会被红黄调色盖掉。
                half lineMask = 1.0 - smoothstep(_LineThreshold * 0.55, _LineThreshold, luma);
                half3 color = lerp(paperColor, tex.rgb, lineMask);
                color = max(half3(0.0, 0.0, 0.0), (color - 0.5) * _Contrast + 0.5);
                color += _InnerColor.rgb * _Flash * (1.0 - lineMask) * 0.45;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
