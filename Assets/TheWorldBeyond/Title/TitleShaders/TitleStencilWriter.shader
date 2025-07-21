// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/TitleStencilWriter"{
	Properties{
		[IntRange] _StencilRef("Stencil Reference Value", Range(0,255)) = 0
	}

	SubShader{

		Tags {   "Queue" = "Geometry-1"} // stuck way up here so that it sorts with transparent objects

		ZWrite Off

		ColorMask 0 // Don't write to any colour channels
		//Blend Zero One
		Stencil{
			Ref[_StencilRef]
			Comp Always
			Pass Replace
		}



		Pass {



			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct meshdata {
				half4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct interpolators {
				half4 pos : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			interpolators vert(meshdata v) {
				interpolators o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(interpolators i) : COLOR
			{
				return 0;
			}

			ENDCG
		}
	}
}
