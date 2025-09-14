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
            // Derivative ops (fwidth) require at least SM3.0 on some platforms
            #pragma target 3.0

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

            // NOTE:
            // The original approach sampled neighbor texels in texture space to build an outline.
            // In builds with sprite atlasing/compression, neighbor samples can leak across atlas
            // padding, producing filled shapes instead of a contour. To make the effect robust
            // across editor and player, we switch to a derivative-based edge band that depends on
            // the alpha gradient (screen-space), then restrict it to the outside only.

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
                float th = saturate(_AlphaThreshold);

                // Edge strength based on alpha gradient in screen-space
                // fwidth(a) ~= |da/dx| + |da/dy| across 1 pixel
                float fa = fwidth(baseAlpha);

                // Thickness control (approx pixels). Increase multiplier to get wider band.
                float bandWidth = fa * max(1.0, _ThicknessPx);

                // Edge band that rises where alpha crosses the threshold from 0 -> 1
                float edgeBand = smoothstep(th - bandWidth, th, baseAlpha);

                // Outside-only mask (don't draw over opaque interior)
                float outside = 1.0 - step(th, baseAlpha);
                float outline = edgeBand * outside;

                // Extra softness widens the transition a bit more
                if (_Softness > 0)
                {
                    float extra = saturate(_Softness * fa * 2.0);
                    outline = smoothstep(0.0, 1.0, outline + extra);
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
