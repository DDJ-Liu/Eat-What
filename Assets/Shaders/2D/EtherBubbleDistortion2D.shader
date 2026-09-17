Shader "EatWhat/2D/Ether Bubble Distortion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Mask", 2D) = "white" {}
        [PerRendererData] _SceneColorTexture ("Captured Scene Color", 2D) = "black" {}
        [PerRendererData] _CaptureValid ("Capture Valid", Float) = 0
        [PerRendererData] _DistortionStrength ("Distortion Strength", Range(-0.08, 0.08)) = 0.012
        [PerRendererData] _WaveFrequency ("Wave Frequency", Range(0.5, 20)) = 5
        [PerRendererData] _WaveSpeed ("Wave Speed", Range(-8, 8)) = 0.7
        [PerRendererData] _FlowSpeed ("Flow Speed", Range(-4, 4)) = 0.35
        [PerRendererData] _Softness ("Edge Softness", Range(0.01, 0.5)) = 0.2
        [PerRendererData] _RimColor ("Rim Color", Color) = (0.55, 0.88, 1.15, 1)
        [PerRendererData] _RimIntensity ("Rim Intensity", Range(0, 4)) = 0.55
        [PerRendererData] _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.28
        [PerRendererData] _BubbleAspect ("Bubble Aspect", Float) = 1

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 screenPosition : TEXCOORD0;
            float2 uv : TEXCOORD1;
            half4 color : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_SceneColorTexture);
        SAMPLER(sampler_SceneColorTexture);
        float4 _MainTex_ST;
        float4 _SceneColorTexture_TexelSize;
        float4 _Color;
        half4 _RendererColor;
        float _CaptureValid;
        float _DistortionStrength;
        float _WaveFrequency;
        float _WaveSpeed;
        float _FlowSpeed;
        float _Softness;
        half4 _RimColor;
        float _RimIntensity;
        float _RimAlpha;
        float _BubbleAspect;

        Varyings BubbleVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #ifdef UNITY_INSTANCING_ENABLED
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteFlip);
            #endif

            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.screenPosition = ComputeScreenPos(output.positionCS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color * _Color * _RendererColor;
            #ifdef UNITY_INSTANCING_ENABLED
                output.color *= unity_SpriteColor;
            #endif
            return output;
        }

        half4 BubbleFragment(Varyings input) : SV_Target
        {
            [branch]
            if (_CaptureValid < 0.5 || _SceneColorTexture_TexelSize.z <= 0.0 ||
                _SceneColorTexture_TexelSize.w <= 0.0)
                return half4(0, 0, 0, 0);

            float2 local = input.uv * 2.0 - 1.0;
            float safeAspect = clamp(abs(_BubbleAspect), 0.01, 100.0);
            float2 metric = local;
            if (safeAspect >= 1.0) metric.x *= safeAspect;
            else metric.y /= safeAspect;

            float radius = length(metric);
            float softness = clamp(_Softness, 0.01, 0.5);
            float circleMask = 1.0 - smoothstep(1.0 - softness, 1.0, radius);
            half spriteMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
            float mask = saturate(circleMask * spriteMask * input.color.a);
            if (mask <= 0.0001) return half4(0, 0, 0, 0);

            float2 screenUv = input.screenPosition.xy / max(input.screenPosition.w, 0.0001);
            #if UNITY_UV_STARTS_AT_TOP
                if (_SceneColorTexture_TexelSize.y < 0.0) screenUv.y = 1.0 - screenUv.y;
            #endif

            float2 radialDirection = radius > 0.0001 ? metric / radius : float2(0, 0);
            float time = _Time.y;
            float wave = sin((radius * clamp(_WaveFrequency, 0.5, 20.0) -
                time * clamp(_WaveSpeed, -8.0, 8.0)) * 6.28318530718);
            float lens = (1.0 - saturate(radius)) * 0.65;
            float safeFlowSpeed = clamp(_FlowSpeed, -4.0, 4.0);
            float2 flow = float2(
                sin((metric.y * 2.1 + time * safeFlowSpeed) * 3.14159265359),
                cos((metric.x * 1.7 - time * safeFlowSpeed * 0.83) * 3.14159265359));
            float2 distortion = (radialDirection * (lens + wave * 0.35) + flow * 0.2) *
                clamp(_DistortionStrength, -0.08, 0.08) * mask;

            float2 edgeGuard = abs(_SceneColorTexture_TexelSize.xy) * 1.5;
            float2 sampleUv = clamp(screenUv + distortion, edgeGuard, 1.0 - edgeGuard);
            half4 sceneColor = SAMPLE_TEXTURE2D(_SceneColorTexture, sampler_SceneColorTexture, sampleUv);

            float rimBand = saturate(1.0 - abs(radius - (1.0 - softness * 0.45)) /
                max(softness, 0.01));
            float rimAlpha = saturate(_RimAlpha) * rimBand;
            half3 rimRgb = _RimColor.rgb * clamp(_RimIntensity, 0.0, 4.0);
            half3 resultRgb = lerp(sceneColor.rgb, rimRgb, rimAlpha);
            half resultAlpha = mask * max(sceneColor.a, rimAlpha * _RimColor.a);
            return half4(resultRgb, resultAlpha);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex BubbleVertex
            #pragma fragment BubbleFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex BubbleVertex
            #pragma fragment BubbleFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
