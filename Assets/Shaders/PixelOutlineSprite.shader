Shader "HilmiVSRoti/PixelOutlineSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0.05, 0.04, 0.08, 1)
        _OutlineEnabled ("Outline Enabled", Float) = 1
        _PixelStepsX ("Pixel Steps X (0 = Native)", Float) = 0
        _PixelStepsY ("Pixel Steps Y (0 = Native)", Float) = 0
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.60
        _InnerEdgeThreshold ("Inner Edge Threshold (0 = Off)", Range(0, 1.5)) = 0.30
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineEnabled;
                float _PixelStepsX;
                float _PixelStepsY;
                float _AlphaCutoff;
                float _InnerEdgeThreshold;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                bool useGrid = (_PixelStepsX > 0.5 && _PixelStepsY > 0.5);
                float2 grid = float2(_PixelStepsX, _PixelStepsY);
                float2 uv = useGrid ? (floor(IN.uv * grid) + 0.5) / grid : IN.uv;
                float2 stepUv = useGrid ? (1.0 / grid) : _MainTex_TexelSize.xy;

                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 cR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(stepUv.x, 0));
                half4 cL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(stepUv.x, 0));
                half4 cU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, stepUv.y));
                half4 cD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, stepUv.y));

                if (texCol.a >= _AlphaCutoff)
                {
                    if (_OutlineEnabled > 0.5 && _InnerEdgeThreshold > 0.01)
                    {
                        float lum = dot(texCol.rgb, float3(0.299, 0.587, 0.114));
                        bool edgeR = cR.a >= _AlphaCutoff && distance(texCol.rgb, cR.rgb) > _InnerEdgeThreshold && lum <= dot(cR.rgb, float3(0.299, 0.587, 0.114));
                        bool edgeD = cD.a >= _AlphaCutoff && distance(texCol.rgb, cD.rgb) > _InnerEdgeThreshold && lum <= dot(cD.rgb, float3(0.299, 0.587, 0.114));
                        if (edgeR || edgeD)
                        {
                            return half4(_OutlineColor.rgb * IN.color.rgb, _OutlineColor.a * IN.color.a);
                        }
                    }

                    return half4(texCol.rgb * IN.color.rgb, IN.color.a);
                }

                if (_OutlineEnabled > 0.5)
                {
                    if (max(max(cR.a, cL.a), max(cU.a, cD.a)) >= _AlphaCutoff)
                    {
                        return half4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a);
                    }
                }

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
