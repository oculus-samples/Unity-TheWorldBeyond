// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/Stencil Sky"
{
		Properties
		{
			_MainTex("MainTex", 2D) = "white" {}
			_Color("Star Color", Color) = (0,0,0,0)
			_FogCubemap("Fog Cubemap", CUBE) = "white" {}

			[IntRange] _StencilRef("Stencil Reference Value", Range(0, 255)) = 0

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

			SubShader
		{


			Stencil{
				Ref[_StencilRef]
				Comp Equal
				//Comp Always

			}



			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry" }
			LOD 100

			CGINCLUDE
			#pragma target 3.0
			ENDCG
			Blend Off
			AlphaToMask Off
			Cull Back
			ColorMask RGBA
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0

			Pass
			{
				Name "Base"
				Tags { "LightMode" = "ForwardBase" }
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
					half2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct interpolators
				{
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD1;
					float3 worldPos : TEXCOORD0;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};


				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
                uniform half4 _Color;
				uniform samplerCUBE _FogCubemap;




				interpolators vert(meshdata v)
				{
					interpolators o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord * _MainTex_ST.xy ;
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

					return o;
				}



				fixed4 frag(interpolators i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);



					half3 worldViewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));
					half4 foggingColor = texCUBE(_FogCubemap, worldViewDirection);

					half worldUVx = atan2(i.worldPos.x, i.worldPos.z) * 1.75;

					half2 starUVs = half2(worldUVx, i.texcoord.y);
					half4 mainTexture = tex2D(_MainTex, starUVs);

					// twinkles
					half distanceGradient = pow(distance(half3(20, 200, 300), i.worldPos), 0.5);
					half distanceRipple = sin(distanceGradient * 2 + (_Time.y + _Time.x)) * 0.5 + 0.6;
					half noisyMask = ((((_Time.y * 0.17 + (mainTexture.b * 1.73)) + (_Time.y * 0.31 + (mainTexture.g * 1.52))) % 1.0));
					noisyMask = abs(noisyMask - 0.5) * (_Color.r * 6); // recentering it around 0.5, scaling it by color
					half animatedStarTextures = mainTexture.r   * ((noisyMask * 0.85) + (distanceRipple * 1));
					animatedStarTextures = saturate(animatedStarTextures - 0.1);



					half4 finalColor = foggingColor + (_Color * animatedStarTextures);
                    return finalColor;
				}
				ENDCG
			}




	}
}
