// ============================================================================
// StylizedExplosion.shader：金色风格化爆炸片
//
// 用途：
//   1. 生成中心闪光、圆形冲击和简单尖刺轮廓
//   2. 用 UV 半径和角度控制爆炸外形
//   3. 给爆炸核心和低透明烟尘共用一套材质参数
// ============================================================================

Shader "BooomJam/Effects/StylizedExplosion"
{
    // ================================================================
    //  材质参数
    // ================================================================
    Properties
    {
        // 外圈主色，核心材质用金色，烟尘材质可调成棕灰色。
        [HDR]_Color ("Color", Color) = (1.0, 0.64, 0.18, 1.0)

        // 中心亮色，爆炸核心通常接近白色。
        [HDR]_CoreColor ("Core Color", Color) = (1.0, 0.96, 0.68, 1.0)

        // 整体亮度。
        _Intensity ("Intensity", Range(0.0, 8.0)) = 2.6

        // 内圈裁剪半径，用来做圆环或保留中心。
        _RadialCut ("Radial Cut", Range(0.0, 1.0)) = 0.15

        // 内外边缘软化范围。
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12

        // 尖刺数量。
        _Spikes ("Spikes", Range(0.0, 32.0)) = 10.0

        // 尖刺强度，数值越大外轮廓越尖。
        _SpikeStrength ("Spike Strength", Range(0.0, 1.0)) = 0.5

        // 基础透明度，粒子颜色的 Alpha 会继续参与相乘。
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.85
    }

    SubShader
    {
        // 透明队列，只走 URP Forward。
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

            // 爆炸核心用加色混合；烟尘材质如果要更厚，可单独改 shader 混合模式。
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 粒子系统会传入位置、UV 和顶点色。
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

            // UnityPerMaterial 保持 SRP Batcher 兼容。
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                half _Intensity;
                half _RadialCut;
                half _EdgeSoftness;
                half _Spikes;
                half _SpikeStrength;
                half _Alpha;
            CBUFFER_END

            // 顶点阶段只做空间转换，爆炸轮廓在片元阶段生成。
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // 半径控制内外边缘，角度控制尖刺起伏。
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float spike = sin(angle * max(_Spikes, 1.0)) * 0.5 + 0.5;
                float outer = lerp(0.82, 1.18, spike * _SpikeStrength);

                // disc 是整体爆炸片，core 是中心高亮。
                float disc = smoothstep(_RadialCut, _RadialCut + _EdgeSoftness, radius);
                disc *= 1.0 - smoothstep(outer - _EdgeSoftness, outer, radius);
                float core = 1.0 - smoothstep(0.0, 0.38, radius);
                half alpha = disc * _Alpha * input.color.a;
                half3 color = lerp(_Color.rgb, _CoreColor.rgb, core) * _Intensity * input.color.rgb;
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
