/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Skybox/ThreeColorGradient" {
	Properties {
		_TopColor ("Top Color", Color) = (1, 0.3, 0.3, 0)
		_MiddleColor ("MiddleColor", Color) = (1.0, 1.0, 0.8)
		_BottomColor ("Bottom Color", Color) = (0.3, 0.3, 1, 0)
		_Up ("Up", Vector) = (0, 1, 0)
		_Exp ("Exp", Range(0, 16)) = 1 
		_DitherStrength("Dither Strength", int) = 16
	}
	SubShader {
		Tags {
			"RenderType" = "Background"
			"Queue" = "Background"
			"PreviewType" = "Skybox"
		}
		Pass {
			ZWrite Off
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			fixed3 _TopColor, _BottomColor, _MiddleColor;
			float3 _Up;
			float _Exp;
            float _DitherStrength;

			struct appdata {
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float3 texcoord : TEXCOORD0;
			};

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			inline half DitherAnimatedNoise(half2 screenPos) {
              half noise = frac(
                  dot(uint3(screenPos, floor(fmod(_Time.y * 10, 4))), uint3(2, 7, 23) / 17.0f));
              noise -= 0.5; // remap from [0..1[ to [-0.5..0.5[
              half noiseScaled = (noise / _DitherStrength);
              return noiseScaled;
            }


			fixed4 frag (v2f i) : SV_TARGET {
				float3 texcoord = normalize(i.texcoord);
				half ditherNoise = DitherAnimatedNoise(i.vertex.xy);

				float3 up = normalize(_Up);
				float d = dot(texcoord, up);
				float s = sign(d);
				return fixed4(lerp(_MiddleColor, s < 0.0 ? _BottomColor : _TopColor, (pow(abs(d), _Exp) + ditherNoise )), 1);
			}

			ENDCG
		}
	}
	CustomEditor "GradientSkybox.LinearThreeColorGradientSkyboxGUI"
}
