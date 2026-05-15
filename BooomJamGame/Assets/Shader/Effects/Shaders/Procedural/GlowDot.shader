// ============================================================================
// GlowDot.shader：金色亮点和短线
//
// 用途：
//   1. 给小火星、亮点和短线粒子提供软圆形发光
//   2. 用 UV 距离生成中心亮核和外圈柔光
//   3. 配合 Additive 混合补充高光
// ============================================================================

Shader "BooomJam/Effects/GlowDot"
{
    // ================================================================
    //  材质参数
    // ================================================================
    Properties
    {
        // 外圈主色，通常用金色或橙金色。
        [HDR]_Color ("Color", Color) = (1.0, 0.78, 0.32, 1.0)

        // 中心亮核颜色，通常接近白色。
        [HDR]_CoreColor ("Core Color", Color) = (1.0, 0.98, 0.72, 1.0)

        // 整体亮度。
        _Intensity ("Intensity", Range(0.0, 8.0)) = 3.0

        // 衰减曲线，数值越大中心越集中。
        _Power ("Power", Range(0.25, 8.0)) = 2.0

        // 基础透明度，粒子颜色的 Alpha 会继续参与相乘。
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
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

            // 亮点使用加色混合，适合火星和高光。
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
                half _Power;
                half _Alpha;
            CBUFFER_END

            // 顶点阶段只做空间转换，发光形状在片元阶段生成。
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

                // 以 UV 中心为圆心，距离越近越亮。
                float2 centeredUv = input.uv * 2.0 - 1.0;
                float distanceToCenter = length(centeredUv);
                float glow = pow(saturate(1.0 - distanceToCenter), _Power);

                // 中心亮核单独提出来，避免小粒子看起来发灰。
                float core = smoothstep(0.48, 0.0, distanceToCenter);
                half alpha = glow * _Alpha * input.color.a;
                half3 color = lerp(_Color.rgb, _CoreColor.rgb, core) * _Intensity * input.color.rgb;
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
