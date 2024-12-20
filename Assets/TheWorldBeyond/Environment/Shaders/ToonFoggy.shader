// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/ToonFoggy"
{
		Properties
		{
			_SaturationDistance("Saturation Distance", Range(0 , 1)) = 1
			_FogCubemap("Fog Cubemap", CUBE) = "white" {}
			_FogStrength("Fog Strength", Range(0 , 1)) = 1
			_FogStartDistance("Fog Start Distance", Range(0 , 100)) = 1
			_FogEndDistance("Fog End Distance", Range(0 , 2000)) = 100
			_FogExponent("Fog Exponent", Range(0 , 1)) = 1
			_LightingRamp("Lighting Ramp", 2D) = "white" {}
			_MainTex("MainTex", 2D) = "white" {}

			_Color("Color", Color) = (0,0,0,0)
			_Overbrightening("Overbrightening", Range(0 , 2)) = 1

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
					half3 normal : NORMAL;
					half4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct vertexOutput
				{
					float4 vertex : SV_POSITION;
					half foggingRange : TEXCOORD0;
					half3 worldNormal : TEXCOORD1;
					half2 uvMain : TEXCOORD2;
					half2 uvDetail : TEXCOORD3;
					half3 worldViewDirection : TEXCOORD4;


					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};




				uniform sampler2D _LightingRamp;
				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
				uniform sampler2D _DetailTex;
				uniform half4 _DetailTex_ST;
				uniform half _DetailTexStrength;
				uniform half4 _Color;
				uniform samplerCUBE _FogCubemap;
				uniform half _FogStartDistance;
				uniform half _FogEndDistance;
				uniform half _FogExponent;
				uniform half _SaturationDistance;
				uniform half _FogStrength;
				uniform half _Overbrightening;

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

					o.uvMain = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
					o.uvDetail = v.texcoord * _DetailTex_ST.xy + _DetailTex_ST.zw;
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
					half4 mainTexture = tex2D(_MainTex, i.uvMain);

					//lighting
					half halfLambert = dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * 0.5 + 0.5;
					half4 lightingRamp = tex2D(_LightingRamp, halfLambert.xx);
					half4 finalLighting = (half4(_LightColor0.rgb, 0.0)  * lightingRamp) + (half4(UNITY_LIGHTMODEL_AMBIENT.xyz, 0));
					half4 litTexture = (finalLighting * mainTexture * _Color) * _Overbrightening;

					//fogging
					half4 foggingColor = texCUBE(_FogCubemap, i.worldViewDirection);
					half4 foggedColor = lerp(litTexture, foggingColor , (i.foggingRange * _FogStrength));

					//desaturating
					half desaturatedColor = dot(foggedColor, 1.2 * half3(0.299, 0.587, 0.114));

					//saturating with distance
					half satDistance = saturate( (_SaturationDistance * 11) - (i.foggingRange * 10) );
					half3 finalColor = lerp(foggedColor.rgb, desaturatedColor, satDistance);
					finalColor = fastPow(finalColor, 0.455);

					return  half4(finalColor, 1.0);
				}
				ENDCG
			}

		////////////////////////////////////////////////////////////////////
		// secondary lights
		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off

			CGPROGRAM

		#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile DIRECTIONAL POINT SPOT

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityShaderVariables.cginc"
			#include "AutoLight.cginc"

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
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				half3 mainTexCoords : TEXCOORD2;
				half4 lightDirection : TEXCOORD3;
				half foggingRange : TEXCOORD4;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _LightingRamp;
			uniform sampler2D _MainTex;
			uniform half4 _MainTex_ST;
			uniform half4 _Color;
			uniform half _FogStartDistance;
			uniform half _FogEndDistance;
			uniform half _FogExponent;
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
				o.foggingRange = clamp(((distance(_WorldSpaceCameraPos, o.worldPos) - _FogStartDistance) / (_FogEndDistance - _FogStartDistance)), 0.0, 1.0);
				o.foggingRange = fastPow(o.foggingRange, _FogExponent);


				#if defined(POINT) || defined(SPOT)
					half3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - o.worldPos;
					o.lightDirection = half4 (normalize(fragmentToLightSource), (1.0 / length(fragmentToLightSource)));
				#else
					o.lightDirection = half4 ((_WorldSpaceLightPos0.xyz), 1.0);
				#endif
				return o;
			}


			fixed4 frag(vertexOutput i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				//lighting
				UNITY_LIGHT_ATTENUATION(attenuation, 0, i.worldPos);
				half lambert = dot(i.lightDirection.xyz, i.worldNormal) * 0.5 + 0.5;
				half4 lightingRamp = tex2D(_LightingRamp, lambert.xx);
				half4 finalLighting = float4(_LightColor0.rgb, 0.0) * attenuation  * lightingRamp;

				//main texture
				half2 uv_MainTex = i.mainTexCoords.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				half4 mainTexture = tex2D(_MainTex, uv_MainTex);
				half4 coloredTexture = lerp( half4 (1, 1, 1, 1), (mainTexture * _Color), 0.5);

				//fogging
				half4 foggedColor = lerp(finalLighting, half4 (0, 0, 0, 0), (i.foggingRange * _FogStrength));


				half4 finalColor = foggedColor * coloredTexture; //fog is black, so light strength fades to 0 with depth
				finalColor = fastPow(finalColor, 0.455);
				return  finalColor;
			}
			ENDCG
		}



	}
}
