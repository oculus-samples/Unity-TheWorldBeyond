// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/EdgePassthroughParticle" {
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _EffectPosition("Effect Position", Vector) = (0,1000,0,1)
        _EffectTimer("Effect Timer", Range(0.0,1.0)) = 1.0
        _InvertedMask("Inverted Mask", float) = 1

        [Header(DepthTest)]
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0 //"Off"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("Blend Color", Float) = 2 //"ReverseSubtract"
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("Blend Alpha", Float) = 3 //"Min"
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            ZWrite[_ZWrite]
            ZTest[_ZTest]
            BlendOp[_BlendOpColor],[_BlendOpAlpha]
            Blend OneMinusSrcAlpha One, One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Inflation;
            uniform float4 _EffectPosition;
            uniform float _EffectTimer;
            uniform float _InvertedMask;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float radialDist = distance(i.worldPos, _EffectPosition) * 10;
                float dist = saturate(radialDist + 5 - _EffectTimer * 50);
                // "max" the ring radius
                if (_EffectTimer >= 1.0) {
                    dist = 0;
                }

                float alpha = lerp(dist, 1 - dist, _InvertedMask);
                return float4(_Color.r, _Color.g, _Color.b, 1-(alpha * col.r));
            }
            ENDCG
        }
    }
}
