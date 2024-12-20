// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/SceneMeshOutline" {
    Properties
    {
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
        _EffectPosition("Effect Position", Vector) = (0,1000,0,1)
        _EffectRadius("Effect Radius", float) = 1
        _EffectIntensity("Effect Intensity", float) = 1
        _EdgeTimeline("Edge Anim Timeline", float) = 1
        _CeilingHeight("CeilingHeight", float) = 1
        [IntRange] _StencilRef("Stencil Reference Value", Range(0, 255)) = 0
    }
        SubShader
    {
        Stencil{
                Ref[_StencilRef]
                Comp NotEqual
        }
        Tags { "Queue" = "Transparent" }
        LOD 100
        BlendOp Add, Max
        Blend One Zero, One One

        // First Pass: render outside shell of hand, as depth object
        Pass {
        ColorMask 0 Blend SrcAlpha OneMinusSrcAlpha CGPROGRAM
#pragma vertex vert
#pragma fragment frag
        // make fog work
        #pragma multi_compile_fog

        #include "UnityCG.cginc"

           struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
              };

           struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
              };

              v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
              }

              fixed4 frag(v2f i) : SV_Target {
                float4 mask = float4(1,1,1,0);
                return mask;
              }
              ENDCG
            }


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float4 vertWorld : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _EdgeColor;
            float4 _EffectPosition;
            float _EffectRadius;
            float _EffectIntensity;
            float _EdgeTimeline;
            float _CeilingHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.vertWorld = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float glow = 1 - pow(i.color.r, 0.2);
                float stroke = 1 - step(0.03, i.color.r);
                float edgeReveal = (i.vertWorld.y*10) + (_EdgeTimeline * 11 *_CeilingHeight) - (_CeilingHeight*10);

                float edgeEffect = saturate(glow * 0.5 + stroke);
                float4 col = edgeEffect * _EffectIntensity * _EdgeColor * saturate(edgeReveal);
                float lightIntensity = distance(i.vertWorld, _EffectPosition) / _EffectRadius;
                lightIntensity = pow(saturate(lightIntensity), 0.9);
                col.a = lightIntensity * 0.97;
                return col;
            }
            ENDCG
        }
    }
}
