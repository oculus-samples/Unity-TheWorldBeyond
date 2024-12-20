// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/ToonFoggyOppy"
{
		Properties
		{
			_SaturationAmount("Saturation Amount", Range(0 , 1)) = 1
			_FogCubemap("Fog Cubemap", CUBE) = "white" {}
			_FogStrength("Fog Strength", Range(0 , 1)) = 1
			_FogStartDistance("Fog Start Distance", Range(0 , 100)) = 1
			_FogEndDistance("Fog End Distance", Range(0 , 2000)) = 100
			_FogExponent("Fog Exponent", Range(0 , 1)) = 1
			_LightingRamp("Lighting Ramp", 2D) = "white" {}
			_MainTex("MainTex", 2D) = "white" {}
			_Color("Color", Color) = (0,0,0,0)
			_GlowColor("Glow Color", Color) = (0,0,0,0)
			_GlowStrength("Glow Strength", Range(0 , 1)) = 1

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
				#include "Lighting.cginc"
				#include "UnityShaderVariables.cginc"

				#include "AutoLight.cginc"



				struct vertexInput
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					half4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct vertexOutput
				{
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;

					half3 worldNormal : TEXCOORD1;
					half3 mainTexCoords : TEXCOORD2;
					half4 localPosition : TEXCOORD3;
					half3 worldViewDirection : TEXCOORD4;
					half2 throbber : TEXCOORD5;
					half foggingRange : TEXCOORD6;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};




				uniform sampler2D _LightingRamp;
				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
				uniform half4 _Color;
				uniform half4 _GlowColor;
				uniform half _GlowStrength;
				uniform samplerCUBE _FogCubemap;
				uniform half _FogStartDistance;
				uniform half _FogEndDistance;
				uniform half _FogExponent;
				uniform half _SaturationAmount;
				uniform half _FogStrength;

				half4 fastPow(half4 a, half b) {
					return a / ((1.0 - b) * a + b);
				}


				vertexOutput vert(vertexInput v)
				{
					vertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);

					o.worldNormal = UnityObjectToWorldNormal(v.normal.xyz);
					o.mainTexCoords.xyz = v.texcoord.xyz;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.localPosition = v.vertex;
					o.worldViewDirection = normalize(UnityWorldSpaceViewDir(o.worldPos));

					o.throbber.x = sin((_Time.y * 2.9) + (v.vertex.x * 7));
					o.throbber.y = sin((_Time.y * 3.1) - (v.vertex.z * 6.5));
					o.foggingRange = clamp(((distance(_WorldSpaceCameraPos, o.worldPos) - _FogStartDistance) / (_FogEndDistance - _FogStartDistance)), 0.0, 1.0);
					o.foggingRange = fastPow(o.foggingRange, _FogExponent);
					return o;
				}



				fixed4 frag(vertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					//main texture
					half4 mainTexture = tex2D(_MainTex, i.mainTexCoords.xy);

					//lighting
					half halfLambert = dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * 0.5 + 0.5;
					half4 lightingRamp = tex2D(_LightingRamp, halfLambert.xx);
					half4 finalLighting = (half4(_LightColor0.rgb, 0.0) * lightingRamp) + (half4(UNITY_LIGHTMODEL_AMBIENT.xyz, 0));
					//glowing bits
					half glowThrob = (_GlowStrength * 2) * (((i.throbber.x  + i.throbber.y) * 0.25 + 0.5) + 0.5);
					half4 glowingAreas = ((1 - mainTexture.a) * _GlowColor * glowThrob) ;
					half4 litTexture = ((finalLighting * mainTexture * _Color) * 1.15) + glowingAreas;


					//fogging
					half4 foggingColor = texCUBE(_FogCubemap, i.worldViewDirection);
					half4 foggedColor = lerp(litTexture, foggingColor , (i.foggingRange * _FogStrength));

					//desaturating
					half desaturatedColor = dot(foggedColor, 1.1 * half3(0.299, 0.587, 0.114));

					//saturating control
					half3 blackAndWhiteToColor = lerp(desaturatedColor, foggedColor, _SaturationAmount);

					half4 finalColor = half4(blackAndWhiteToColor.rgb, 1.0);
					finalColor = fastPow(finalColor, 0.454);

					return  half4(finalColor.rgb, 1.0);
				}
				ENDCG
			}

	}
}
