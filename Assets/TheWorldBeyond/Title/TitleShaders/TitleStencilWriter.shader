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

			struct meshdata {
				half4 vertex : POSITION;
			};
			struct interpolators {
				half4 pos : SV_POSITION;
			};

			interpolators vert(meshdata v) {
				interpolators o;
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
