Shader "EatWhat/2D/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _OutlineColor ("Outline Color", Color) = (0.12, 0.09, 0.08, 1)
        [PerRendererData] _OutlineThickness ("Outline Thickness (Source Pixels)", Range(0, 32)) = 4
        [PerRendererData] _ShadowEnabled ("Shadow Enabled", Float) = 0
        [PerRendererData] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        [PerRendererData] _ShadowOffset ("Shadow Offset (Source Pixels)", Vector) = (0, 0, 0, 0)
        [PerRendererData] _ShadowOpacity ("Shadow Opacity", Range(0, 1)) = 0
        [PerRendererData] _ShadowBlur ("Shadow Blur (Source Pixels)", Range(0, 32)) = 0

        // SpriteRenderer compatibility properties.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
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
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        float4 _Color;
        half4 _RendererColor;
        half4 _OutlineColor;
        float _OutlineThickness;
        float _ShadowEnabled;
        half4 _ShadowColor;
        float2 _ShadowOffset;
        float _ShadowOpacity;
        float _ShadowBlur;

        Varyings OutlineVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #ifdef UNITY_INSTANCING_ENABLED
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteFlip);
            #endif

            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color * _Color * _RendererColor;

            #ifdef UNITY_INSTANCING_ENABLED
                output.color *= unity_SpriteColor;
            #endif

            return output;
        }

        half SampleAlpha(float2 uv)
        {
            // Shadow and outline coverage come only from the original Sprite alpha.
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
        }

        half SampleCoverage(float2 uv)
        {
            half alpha = SampleAlpha(uv);
            return alpha > (1.0h / 255.0h) ? alpha : 0.0h;
        }

        half SampleRingAlpha(float2 uv, float2 radius)
        {
            const float sin30 = 0.5;
            const float cos30 = 0.8660254;
            half alpha = 0;

            alpha = max(alpha, SampleCoverage(uv + float2( radius.x, 0)));
            alpha = max(alpha, SampleCoverage(uv + float2( cos30 * radius.x,  sin30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2( sin30 * radius.x,  cos30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(0,  radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(-sin30 * radius.x,  cos30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(-cos30 * radius.x,  sin30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(-radius.x, 0)));
            alpha = max(alpha, SampleCoverage(uv + float2(-cos30 * radius.x, -sin30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(-sin30 * radius.x, -cos30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2(0, -radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2( sin30 * radius.x, -cos30 * radius.y)));
            alpha = max(alpha, SampleCoverage(uv + float2( cos30 * radius.x, -sin30 * radius.y)));

            return alpha;
        }

        half FindNeighbourAlpha(float2 uv, float2 radius, float thickness)
        {
            // A single ring makes high widths look like several displaced copies of the sprite.
            // Fill the radius with evenly spaced 30-degree rings so sharp alpha features cannot
            // escape through the gaps as long spikes. radius already uses source-pixel texels.
            if (thickness <= 2.0)
                return SampleRingAlpha(uv, radius);

            if (thickness <= 4.0)
                return max(SampleRingAlpha(uv, radius * 0.5), SampleRingAlpha(uv, radius));

            if (thickness <= 12.0)
            {
                half alpha = SampleRingAlpha(uv, radius * (1.0 / 3.0));
                alpha = max(alpha, SampleRingAlpha(uv, radius * (2.0 / 3.0)));
                return max(alpha, SampleRingAlpha(uv, radius));
            }

            half wideAlpha = SampleRingAlpha(uv, radius * 0.25);
            wideAlpha = max(wideAlpha, SampleRingAlpha(uv, radius * 0.5));
            wideAlpha = max(wideAlpha, SampleRingAlpha(uv, radius * 0.75));
            return max(wideAlpha, SampleRingAlpha(uv, radius));
        }

        half SampleShadowAlpha(float2 uv, float2 blurRadius)
        {
            // blur == 0 is a one-tap hard edge. Enabled blur uses a normalized
            // 3x3 Gaussian kernel: (1 2 1 / 2 4 2 / 1 2 1) * (1 / 16).
            if (_ShadowBlur <= 0.0)
                return SampleAlpha(uv);

            half weightedAlpha = 0;
            weightedAlpha += SampleAlpha(uv + float2(-blurRadius.x, -blurRadius.y));
            weightedAlpha += SampleAlpha(uv + float2( 0,            -blurRadius.y)) * 2.0h;
            weightedAlpha += SampleAlpha(uv + float2( blurRadius.x, -blurRadius.y));
            weightedAlpha += SampleAlpha(uv + float2(-blurRadius.x,  0)) * 2.0h;
            weightedAlpha += SampleAlpha(uv) * 4.0h;
            weightedAlpha += SampleAlpha(uv + float2( blurRadius.x,  0)) * 2.0h;
            weightedAlpha += SampleAlpha(uv + float2(-blurRadius.x,  blurRadius.y));
            weightedAlpha += SampleAlpha(uv + float2( 0,             blurRadius.y)) * 2.0h;
            weightedAlpha += SampleAlpha(uv + float2( blurRadius.x,  blurRadius.y));
            return weightedAlpha * (1.0h / 16.0h);
        }

        half4 ComposeLegacyOutline(Varyings input)
        {
            half4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            half4 spriteColor = textureColor * input.color;
            float thickness = max(_OutlineThickness, 0.0);
            float2 radius = _MainTex_TexelSize.xy * thickness;

            half neighbourAlpha = FindNeighbourAlpha(input.uv, radius, thickness);
            neighbourAlpha *= input.color.a;

            half outlineCoverage = saturate(neighbourAlpha - spriteColor.a) * _OutlineColor.a;
            half resultAlpha = saturate(spriteColor.a + outlineCoverage);
            half spriteWeight = saturate(spriteColor.a * 2.0h);
            half3 resultRgb = lerp(_OutlineColor.rgb, spriteColor.rgb, spriteWeight);

            return half4(resultRgb, resultAlpha);
        }

        half4 OutlineFragment(Varyings input) : SV_Target
        {
            half4 foreground = ComposeLegacyOutline(input);

            // The default/disabled path returns the legacy outline result exactly and
            // avoids every extra shadow texture sample.
            [branch]
            if (_ShadowEnabled < 0.5 || _ShadowOpacity <= 0.0)
                return foreground;

            float2 safeOffset = clamp(_ShadowOffset, -128.0, 128.0);
            float safeBlur = clamp(_ShadowBlur, 0.0, 32.0);
            float2 shadowUv = input.uv - (_MainTex_TexelSize.xy * safeOffset);
            float2 blurRadius = _MainTex_TexelSize.xy * safeBlur;
            half shadowCoverage = SampleShadowAlpha(shadowUv, blurRadius);
            half shadowAlpha = saturate(shadowCoverage * input.color.a * _ShadowColor.a * saturate(_ShadowOpacity));

            // Straight-alpha source-over composition establishes the invariant order:
            // shadow (bottom), legacy outline (middle), original sprite (top).
            half resultAlpha = foreground.a + shadowAlpha * (1.0h - foreground.a);
            half3 premultipliedRgb = foreground.rgb * foreground.a;
            premultipliedRgb += _ShadowColor.rgb * shadowAlpha * (1.0h - foreground.a);
            half3 resultRgb = resultAlpha > 0.0001h ? premultipliedRgb / resultAlpha : half3(0, 0, 0);
            return half4(resultRgb, resultAlpha);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        // Forward fallback keeps the material visible if the renderer is not using a 2D Renderer.
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
