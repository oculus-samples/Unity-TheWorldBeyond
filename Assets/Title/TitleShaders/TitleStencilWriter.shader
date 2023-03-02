/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
