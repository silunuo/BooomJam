Shader "Custom/FlagBurn"
{
    Properties
    {
        [Header(Vertex Animation)]
        _WeightX              ("WeightX：X轴收缩程度", Range(0.0, 1.0)) = 0.0
        _WeightZ              ("WeightZ：Z轴起伏程度", Range(0.0, 1.0)) = 0.0
        _WaveLenght           ("WaveLenght：波长", Range(-3.0, 3.0)) = 0.0
        _WeightHz             ("WeightHz：起伏频率", Range(0.0, 0.1)) = 0.05
        _Speed                ("Speed：飘动速度", Range(0.0, 3.0)) = 0.0
        [Header(Burn)]
        [Toggle]_DissolveType ("BurnType：燃烧类型", int) = 0
        _MainTex              ("MainMap：基础色贴图", 2D) = "white"{}
        _NoiseTex             ("NoiseMap：燃烧噪波贴图", 2D) = "white"{}
        _EdgeRange            ("EdgeRange：燃烧范围", range(0.0, 1.0)) = 0.5
        _Contrast             ("Contrast：燃烧边缘对比度", Range(0.0, -5.0)) = 0.0
        _RampTex              ("RampMap：燃烧颜色映射图", 2D) = "white"{}
        [HDR]_FireColor       ("FireColor：燃烧颜色", Color) = (0.0, 0.0, 0.0, 1.0)
        _BurnSpeed            ("BurnSpeed：燃烧速度", Range(0.0, 3.0)) = 0.0
        _Cutoff               ("Cutoff：溶解裁剪", Range(0.0, 1.0)) = 0.1
    }
    
    SubShader
    {
        Tags       
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        HLSLINCLUDE
        
        // Remap method
        float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _DISSOLVETYPE_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float4 uv         : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
            float4 _NoiseTex_ST;
            half _WeightX;
            half _WeightZ;
            half _WaveLenght;
            half4 _FireColor;
            half _EdgeRange;
            half _WeightHz;
            half _Contrast;
            half _Speed;
            half _BurnSpeed;
            half _Cutoff;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            
            Varyings vert (Attributes v)
            {
                float2 uv = v.uv * _WeightHz;                                                                                                               // 构建顶点扰动贴图采样UV
                uv += float2(1.0, 0.0) * _Time.z * _Speed;                                                                                                  // 添加运动
                float var_NoiseTex = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv, 0).r;                                                            // 采样顶点扰动贴图
                Varyings o;
                o.positionOS = v.positionOS.xyz;                                                                                                            // 计算xy轴扰动值
                o.positionOS.x -=  _WeightX * var_NoiseTex * (1 - v.uv.y);
                o.positionOS.z +=  _WeightZ * var_NoiseTex * (1 - v.uv.y);
                o.positionCS = TransformObjectToHClip(o.positionOS.xyz);
                o.uv.xy = TRANSFORM_TEX(v.uv, _NoiseTex);
                #if _DISSOLVETYPE_ON
                o.uv -= frac(_Time.y * _BurnSpeed);                                                                                                         // UV流动
                #endif
                // o.uv.zw = v.uv;
                o.uv.zw = float2(1.0 - v.uv.y, v.uv.x);
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.zw);                                                                   // 主纹理采样
                half var_NoiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv.xy).r;                                                               // 噪波采样
                half cutoffTemp = Remap(_Cutoff, 0.0,1.0,_Contrast,_Contrast + 2.0);                                       // 将_Cutoff从0-1映射到-1-1
                half rangeMask = SafeDiv(var_NoiseTex - cutoffTemp,_Contrast);                                                                 // 计算遮罩范围 除法矫正
                half cutoffValue = 1.0 - rangeMask;                                                                                                         // 翻转遮罩 计算裁剪值
                #if _DISSOLVETYPE_ON                                                                                                                        // 燃烧类型开关宏
                rangeMask = SafeDiv(i.positionOS.y - cutoffTemp,_Contrast);                                                                                 // 使用模型空间下顶点Y轴做方向遮罩
                cutoffValue = 1 - rangeMask - var_NoiseTex;                                                                                                 // 加入方向遮罩 计算裁剪值
                #endif
                half edgeRange = clamp(1.0 - distance(cutoffValue, 0.5) / _EdgeRange, 0.0, 1.0);                                                            // 计算燃烧边缘范围
                half2 rampUV = float2(1.0 -edgeRange,0.5);                                                                                                  // 构建采样Ramp贴图UV
                half3 var_RampTex = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;                                                                // Ramp贴图采样
                half3 edgeColor = lerp(var_MainTex.rgb, var_RampTex * _FireColor.rgb, edgeRange);                                                           // 主纹理颜色与燃烧颜色混合
                clip(step(0.5, cutoffValue) - 0.5);                                                                                                         // 裁剪规则
                return half4(edgeColor, var_MainTex.a);
            }
            ENDHLSL
        }
        
        // 自定义阴影投射Pass
        Pass
        {
            Tags
            { "LightMode" = "ShadowCaster" }
            
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment

            #pragma multi_compile_instancing
            #pragma shader_feature _ _DISSOLVETYPE_ON
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float4 uv         : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
            float4 _NoiseTex_ST;
            half _WeightX;
            half _WeightZ;
            half _WaveLenght;
            half4 _FireColor;
            half _EdgeRange;
            half _WeightHz;
            half _Contrast;
            half _Speed;
            half _BurnSpeed;
            half _Cutoff;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            
            Varyings ShadowCasterVertex(Attributes v)
            {
                float2 uv = v.uv * _WeightHz;
                uv += float2(1.0, 0.0) * _Time.z * _Speed;
                float var_NoiseTex = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv, 0).r;
                
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionOS = v.positionOS.xyz;
                o.positionOS.x -=  _WeightX * var_NoiseTex * (1.0 - v.uv.y);                                                                              // 顶点变换与阴影偏移 与基础pass内处理一致
                o.positionOS.z +=  _WeightZ * var_NoiseTex * (1.0 - v.uv.y);
                
                float3 positionWS = TransformObjectToWorld(o.positionOS.xyz);                                                                             // 计算世界空间下法线与顶点位置
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                float4 positionCS = TransformWorldToHClip                                                                                                 // 计算阴影投射位置
                         (ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                
                #if UNITY_REVERSED_Z                                                                                                                      // 平台深度兼容处理
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                o.positionCS = positionCS;                                                                                                                // 将经过变换计算后的裁剪空间下的点赋值
                o.uv.xy = TRANSFORM_TEX(v.uv, _NoiseTex);
                #if _DISSOLVETYPE_ON
                o.uv -= frac(_Time.y * _BurnSpeed);
                #endif
                o.uv.zw = v.uv;
                return o;
            }

            half4 ShadowCasterFragment(Varyings i) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.zw);
                half var_NoiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv.xy).r;
                half cutoffTemp = Remap(_Cutoff, 0.0,1.0,_Contrast,_Contrast + 2.0);
                half rangeMask = SafeDiv((var_NoiseTex - cutoffTemp),_Contrast);
                half cutoffValue = 1.0 - rangeMask;
                #if _DISSOLVETYPE_ON
                rangeMask = SafeDiv(i.positionOS.y - cutoffTemp,_Contrast);
                cutoffValue = 1.0 - rangeMask - var_NoiseTex;
                #endif
                clip(step(0.5, cutoffValue) * var_MainTex.a - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}
