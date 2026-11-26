Shader "EatWhat/2D/Sprite Outline Merge 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness In Source Pixels", Float) = 1
        _ShadowContributor ("Shadow Contributor", Float) = 1
        _EntityMaskTex ("Entity And Shadow Mask", 2D) = "black" {}
        _CandidateTex ("Outline Candidates", 2D) = "black" {}
        _GroupShadowColor ("Group Shadow Color", Color) = (0,0,0,1)
        _GroupShadowOffset ("Group Shadow Offset In RT UV", Vector) = (0,0,0,0)
        _GroupShadowOpacity ("Group Shadow Opacity", Range(0,1)) = 0.5
        _GroupShadowSoftness ("Group Shadow Softness In RT Pixels", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_EntityMaskTex);
        SAMPLER(sampler_EntityMaskTex);
        TEXTURE2D(_CandidateTex);
        SAMPLER(sampler_CandidateTex);

        float4 _MainTex_TexelSize;
        float4 _EntityMaskTex_TexelSize;
        float4 _RendererColor;
        float4 _OutlineColor;
        float _OutlineThickness;
        float _ShadowContributor;
        float4x4 _WorldToSprite;
        float4 _SpriteLocalRect;
        float4 _SpriteUvRect;
        float4 _SpriteFlip;
        float4 _GroupWorldRect;
        float _SourcePixelWorld;
        float4 _GroupShadowColor;
        float4 _GroupShadowOffset;
        float _GroupShadowOpacity;
        float _GroupShadowSoftness;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.uv = input.uv;
            return output;
        }

        float2 SpriteUv(float3 worldPosition)
        {
            float2 local = mul(_WorldToSprite, float4(worldPosition, 1.0)).xy;
            float2 normalized = (local - _SpriteLocalRect.xy) / max(_SpriteLocalRect.zw, float2(1e-6, 1e-6));
            normalized.x = lerp(normalized.x, 1.0 - normalized.x, _SpriteFlip.x);
            normalized.y = lerp(normalized.y, 1.0 - normalized.y, _SpriteFlip.y);
            return _SpriteUvRect.xy + normalized * _SpriteUvRect.zw;
        }

        float InsideSpriteRect(float2 uv)
        {
            float2 minimum = _SpriteUvRect.xy;
            float2 maximum = minimum + _SpriteUvRect.zw;
            return step(minimum.x, uv.x) * step(minimum.y, uv.y) * step(uv.x, maximum.x) * step(uv.y, maximum.y);
        }

        float SourceAlpha(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * InsideSpriteRect(uv) * _RendererColor.a;
        }

        float2 GroupUv(float3 worldPosition)
        {
            return (worldPosition.xy - _GroupWorldRect.xy) / max(_GroupWorldRect.zw, float2(1e-6, 1e-6));
        }
        ENDHLSL

        // Index 0. Writes entity union to R and group-shadow contributor union to G.
        Pass
        {
            Name "MaskUnion"
            Tags { "LightMode"="SpriteOutlineMergeOffscreen" }
            Blend One One
            BlendOp Max
            ColorMask RG
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragMask
            half4 FragMask(Varyings input) : SV_Target
            {
                half alpha = SourceAlpha(SpriteUv(input.positionWS));
                return half4(alpha, alpha * saturate(_ShadowContributor), 0.0, 0.0);
            }
            ENDHLSL
        }

        // Index 1. Stable source-over candidates; full entity R excludes inner edges.
        Pass
        {
            Name "OutlineCandidate"
            Tags { "LightMode"="SpriteOutlineMergeOffscreen" }
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCandidate
            half4 FragCandidate(Varyings input) : SV_Target
            {
                float2 uv = SpriteUv(input.positionWS);
                float center = SourceAlpha(uv);
                float2 delta = _MainTex_TexelSize.xy * max(0.0, _OutlineThickness);
                float neighbor = 0.0;
                neighbor = max(neighbor, SourceAlpha(uv + float2(delta.x, 0.0)));
                neighbor = max(neighbor, SourceAlpha(uv - float2(delta.x, 0.0)));
                neighbor = max(neighbor, SourceAlpha(uv + float2(0.0, delta.y)));
                neighbor = max(neighbor, SourceAlpha(uv - float2(0.0, delta.y)));
                neighbor = max(neighbor, SourceAlpha(uv + delta));
                neighbor = max(neighbor, SourceAlpha(uv - delta));
                neighbor = max(neighbor, SourceAlpha(uv + float2(delta.x, -delta.y)));
                neighbor = max(neighbor, SourceAlpha(uv + float2(-delta.x, delta.y)));
                float entity = SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, GroupUv(input.positionWS)).r;
                half alpha = saturate(neighbor - center) * (1.0 - entity) * _OutlineColor.a;
                return half4(_OutlineColor.rgb, alpha);
            }
            ENDHLSL
        }

        // Index 2. Ordinary host pass; only the private shadow material enables it.
        Pass
        {
            Name "ShadowDisplay"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragShadow
            half4 FragShadow(Varyings input) : SV_Target
            {
                float2 sourceUv = input.uv - _GroupShadowOffset.xy;
                float2 blur = _EntityMaskTex_TexelSize.xy * max(0.0, _GroupShadowSoftness);
                float alpha = 0.0;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv).g * 0.24;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv + float2(blur.x, 0)).g * 0.12;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv - float2(blur.x, 0)).g * 0.12;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv + float2(0, blur.y)).g * 0.12;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv - float2(0, blur.y)).g * 0.12;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv + blur).g * 0.07;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv - blur).g * 0.07;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv + float2(blur.x, -blur.y)).g * 0.07;
                alpha += SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, sourceUv + float2(-blur.x, blur.y)).g * 0.07;
                return half4(_GroupShadowColor.rgb, saturate(alpha) * _GroupShadowColor.a * _GroupShadowOpacity);
            }
            ENDHLSL
        }

        // Index 3. Ordinary host pass; entity R is sampled again as a safety exclusion.
        Pass
        {
            Name "OutlineDisplay"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOutline
            half4 FragOutline(Varyings input) : SV_Target
            {
                half4 candidate = SAMPLE_TEXTURE2D(_CandidateTex, sampler_CandidateTex, input.uv);
                half entity = SAMPLE_TEXTURE2D(_EntityMaskTex, sampler_EntityMaskTex, input.uv).r;
                candidate.a *= 1.0 - entity;
                return candidate;
            }
            ENDHLSL
        }
    }
}
