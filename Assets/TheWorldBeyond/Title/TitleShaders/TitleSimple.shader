// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/TitleSimple" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

    }
    SubShader
    {
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry-2" }

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0




        Pass
        {
            CGPROGRAM

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif

            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing



            #include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"

            struct meshdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;
			half4 _Color;

            interpolators vert (meshdata v)
            {
                interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
                return o;
            }

            fixed4 frag (interpolators i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return half4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
