// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/Title"
{
		Properties
		{

			_StarTexture("Star Texture", 2D) = "white" {}
			_MainTex("MainTex", 2D) = "white" {}
			_OppyPosition("Oppy Position", Vector) = (0,0,0,0)
			_OppyRippleStrength("Oppy Ripple Strength", Range(0 , 1)) = 1
			_Color("Color", Color) = (0,0,0,0)
			_UnderlyingColor("Underlying Color", Color) = (0,0,0,0)

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

			SubShader
		{
			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }

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

				struct vertexInput
				{
					float4 vertex : POSITION;
					half3 normal : NORMAL;
					half4 texcoord0 : TEXCOORD0;
					half4 texcoord1 : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct vertexOutput
				{
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					half2 texcoord0 : TEXCOORD1;
					half2 texcoord1 : TEXCOORD2;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};


				uniform sampler2D _StarTexture;
				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
				uniform half4 _Color;
				uniform half4 _UnderlyingColor;
				uniform float3 _OppyPosition;
				uniform half _OppyRippleStrength;




				vertexOutput vert(vertexInput v)
				{
					vertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

					o.texcoord0 = v.texcoord0;
					o.texcoord1 = v.texcoord1;
					return o;
				}



				fixed4 frag(vertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					//main texture
					half4 starTextures =  tex2D(_StarTexture, i.texcoord1.xy);;

					//distance gradient
					half distanceToOppy = pow(distance(_OppyPosition, i.worldPos), 1.5);
					//ripples over distance gradient (get banding if you scale time up too quickly!)
					half distanceRipple = sin(distanceToOppy * 2 + (_Time.y + _Time.x)) * 0.5 + 0.6;

					// noisy mask
					half noisyMask = ((((_Time.y * 0.07 + (starTextures.b * 1.73)) + (_Time.y * 0.21 + (starTextures.g * 1.52))) % 1.0));
					noisyMask = abs(noisyMask - 0.5) * ( _Color.r * 6); // recentering it around 0.5, scaling it by color
					half4 animatedStarTextures = starTextures.rrrr   * ((noisyMask * 0.85) + (distanceRipple * _OppyRippleStrength));
					animatedStarTextures = saturate(animatedStarTextures - 0.1);


					half4 mainTexture = tex2D(_MainTex, i.texcoord0.xy);
					half4 finalTexture = (mainTexture * half4(_UnderlyingColor.rgb, 1.0) * distanceRipple) + animatedStarTextures;

					half4 finalColor = finalTexture * (_Color );
					// ensure the alpha is 1, otherwise Passthrough will be revealed
					finalColor.a = 1;

					//half4 debug = half4(animatedStarTextures.rgb, 1);
					return  finalColor;
				}
				ENDCG
			}




	}
}
