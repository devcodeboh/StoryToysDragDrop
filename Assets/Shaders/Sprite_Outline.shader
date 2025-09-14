Shader "Shader Graphs/Sprite_Outline"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _ThicknessPx ("Thickness (px)", Range(0,8)) = 3
        _Softness ("Softness", Range(0,1)) = 0
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Sprite Unlit"
            Tags{ "LightMode" = "Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float _ThicknessPx;
                float _Softness;
                float _AlphaThreshold;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height

            // Helper: max alpha on 8-neighbour ring at radius r
            float SampleMaxRing(float2 uv, float r, float dx, float dy)
            {
                float m = 0;
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( dx*r, 0)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-dx*r, 0)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0,  dy*r)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -dy*r)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( dx*r,  dy*r)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( dx*r, -dy*r)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-dx*r,  dy*r)).a);
                m = max(m, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-dx*r, -dy*r)).a);
                return m;
            }

            // Dilate field up to radius using 0.5 px steps to avoid gaps
            float DilateMax(float2 uv, float dx, float dy, float radius)
            {
                float m = 0;
                m = max(m, SampleMaxRing(uv, 0.5, dx, dy) * step(0.25, radius));
                m = max(m, SampleMaxRing(uv, 1.0, dx, dy) * step(0.75, radius));
                m = max(m, SampleMaxRing(uv, 1.5, dx, dy) * step(1.25, radius));
                m = max(m, SampleMaxRing(uv, 2.0, dx, dy) * step(1.75, radius));
                m = max(m, SampleMaxRing(uv, 2.5, dx, dy) * step(2.25, radius));
                m = max(m, SampleMaxRing(uv, 3.0, dx, dy) * step(2.75, radius));
                return m;
            }

            struct appdata
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float baseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                float dx = _MainTex_TexelSize.x;
                float dy = _MainTex_TexelSize.y;
                // Max neighbor alpha up to thickness (with half-pixel steps)
                float m = DilateMax(i.uv, dx, dy, _ThicknessPx + 0.5);
                float th = saturate(_AlphaThreshold);
                float cBin = step(th, baseAlpha);
                // Strict outside-only contour: neighbors opaque AND center transparent
                float outline = saturate(step(th, m) * (1.0 - cBin));
                // Optional soft band expansion to remove micro gaps, still only outside
                if (_Softness > 0)
                {
                    float band = saturate(m - baseAlpha);
                    outline = max(outline, smoothstep(th, th + _Softness, band) * (1.0 - cBin));
                }
                float alpha = outline * i.color.a * _OutlineColor.a;
                float3 rgb = _OutlineColor.rgb;
                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
