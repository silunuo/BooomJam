// ============================================================================
// HitSlash.shader：金色受击尖刺片
//
// 用途：
//   1. 给粒子系统的 Stretch Billboard 画出尖锐的受击片
//   2. 用 UV 生成尖头、软边和中心亮线
//   3. 配合 Additive 混合做短时间高亮反馈
// ============================================================================

Shader "BooomJam/Effects/HitSlash"
{
    // ================================================================
    //  材质参数
    // ================================================================
    Properties
    {
        // 外侧主色，通常用金色。
        [HDR]_Color ("Color", Color) = (1.0, 0.76, 0.28, 1.0)

        // 中心亮线颜色，通常接近白色。
        [HDR]_CoreColor ("Core Color", Color) = (1.0, 0.95, 0.64, 1.0)

        // 整体亮度，Bloom 开启时会更明显。
        _Intensity ("Intensity", Range(0.0, 8.0)) = 2.4

        // 边缘软化范围，数值越大越柔。
        _Softness ("Softness", Range(0.001, 0.5)) = 0.16

        // 尖刺收束强度，数值越大越细长。
        _Taper ("Taper", Range(0.05, 2.0)) = 0.75

        // 边缘破碎感，数值越大形状越不规则。
        _Jagged ("Jagged", Range(0.0, 1.0)) = 0.35

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

            // 受击亮片用加色混合，避免透明排序影响主体亮度。
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
                half _Softness;
                half _Taper;
                half _Jagged;
                half _Alpha;
            CBUFFER_END

            // 小噪声用于打破边缘，避免每个尖刺片形状完全一样。
            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 顶点阶段只做空间转换，形状在片元阶段用 UV 生成。
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

                // x 控制尖刺长度方向，y 控制上下宽度。
                float2 uv = input.uv;
                float x = saturate(uv.x);
                float y = abs(uv.y * 2.0 - 1.0);

                // 越靠右越收尖，再叠一层轻微破碎。
                float taperMask = pow(1.0 - x, _Taper);
                float jag = lerp(1.0, 0.74 + Hash21(floor(float2(x * 18.0, y * 8.0))) * 0.48, _Jagged);
                float width = max(taperMask * jag, 0.001);

                // shape 是整体轮廓，head 负责切掉头尾硬边，core 是中间亮线。
                float shape = 1.0 - smoothstep(width, width + _Softness, y);
                float head = smoothstep(0.02, 0.18, x) * (1.0 - smoothstep(0.92, 1.0, x));
                float core = 1.0 - smoothstep(width * 0.22, width * 0.56 + _Softness * 0.15, y);
                float alpha = shape * head * _Alpha * input.color.a;
                half3 color = lerp(_Color.rgb, _CoreColor.rgb, core) * _Intensity * input.color.rgb;
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
