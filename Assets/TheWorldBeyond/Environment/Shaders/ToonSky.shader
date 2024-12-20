// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/ToonSky"
{
		Properties
		{
			_SaturationDistance("Saturation Distance", Range(0 , 1)) = 1
			_FogCubemap("Fog Cubemap", CUBE) = "white" {}
			_FogStrength("Fog Strength", Range(0 , 1)) = 1
			_FogStartDistance("Fog Start Distance", Range(0 , 100)) = 1
			_FogEndDistance("Fog End Distance", Range(0 , 2000)) = 100
			_FogExponent("Fog Exponent", Range(0 , 1)) = 1
			_MainTex("MainTex", 2D) = "white" {}

			_CloudColor("Cloud Color", Color) = (0,0,0,0)
			_CloudMixStrength("Cloud Mix Strength", Range(0 , 1)) = 1

			_MountainColor("Mountains Color", Color) = (0,0,0,0)
			_MountainMixStrength("Mountain Mix Strength", Range(0 , 1)) = 1

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

			SubShader
		{
			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
			LOD 100

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
					half4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct vertexOutput
				{
					float4 vertex : SV_POSITION;
					half foggingRange : TEXCOORD0;
					half3 worldNormal : TEXCOORD1;
					half2 mainTexCoords : TEXCOORD2;
					half3 worldViewDirection : TEXCOORD3;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};




				uniform sampler2D _MainTex;

				uniform samplerCUBE _FogCubemap;
				uniform half _FogStartDistance;
				uniform half _FogEndDistance;
				uniform half _FogExponent;
				uniform half _SaturationDistance;
				uniform half _FogStrength;
				uniform half _CloudMixStrength;
				uniform half _MountainMixStrength;
				uniform half4 _CloudColor;
				uniform half4 _MountainColor;

				half3 fastPow(half3 a, half b) {
					return a / ((1.0 - b) * a + b);
				}

				vertexOutput vert(vertexInput v)
				{
					vertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					o.worldNormal = UnityObjectToWorldNormal(v.normal.xyz);
					o.mainTexCoords.xy = v.texcoord.xy * half2(2, 1);
					o.vertex = UnityObjectToClipPos(v.vertex);
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.worldViewDirection = normalize(UnityWorldSpaceViewDir(worldPos));
					o.foggingRange = clamp(((distance(_WorldSpaceCameraPos, worldPos) - _FogStartDistance) / (_FogEndDistance - _FogStartDistance)), 0.0, 1.0);
					o.foggingRange = fastPow(o.foggingRange, _FogExponent);

					return o;
				}



				fixed4 frag(vertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					//main texture
					half4 mainTexture = tex2D(_MainTex, i.mainTexCoords);

					//fogging
					half4 foggingColor = texCUBE(_FogCubemap, i.worldViewDirection);


					// lay clouds over fog color
					half4 clouds = lerp(foggingColor, _CloudColor, (mainTexture.r * _CloudMixStrength));
					// lay mountains over fog color
					half4 mountains = lerp(foggingColor, _MountainColor, (mainTexture.g * _MountainMixStrength));
					// lay fogged mountains over fogged clouds
					half4 mountainsOverClouds = lerp(clouds, mountains, (mainTexture.b));

					//desaturating
					half desaturatedColor = dot(mountainsOverClouds, 1.2 * half3(0.299, 0.587, 0.114));

					//saturating with distance
					half satDistance = saturate( (_SaturationDistance * 11) - (i.foggingRange * 10) );
					half3 finalColor = lerp(mountainsOverClouds, desaturatedColor, satDistance);
					finalColor = fastPow(finalColor, 0.455);
					return  half4(finalColor.rgb, 1.0);
				}
				ENDCG
			}

	}
}
