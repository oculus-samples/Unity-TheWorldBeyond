// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/RimLit" {
    Properties
    {
        _Color ("Inner Color", Color) = (0,0,0,0)
		_RimColor ("Rim Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 worldNormal : TEXCOORD1;
				float3 viewDir: TEXCOORD2;
            };

            float4 _Color;
			float4 _RimColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = WorldSpaceViewDir(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float fresnel = dot(normalize(i.viewDir), normalize(i.worldNormal));
				fixed4 col = lerp(_RimColor, _Color,fresnel);
				col.a = 1.0f;
                return col;
            }
            ENDCG
        }
    }
}
