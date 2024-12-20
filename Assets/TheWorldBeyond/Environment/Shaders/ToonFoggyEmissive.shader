// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/ToonFoggyEmissive"
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


		_Color("Color", Color) = (0,0,0,0)
		_Overbrightening("Overbrightening", Range(0 , 2)) = 1

		_EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
		_Emission("Emission", Range(0, 1)) = 1

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
		#pragma multi_compile _ SHADOWS_SCREEN

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
			float foggingRange : TEXCOORD0;

			half3 worldNormal : TEXCOORD1;
			half3 mainTexCoords : TEXCOORD2;
			half3 worldViewDirection : TEXCOORD3;

			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};




		uniform sampler2D _LightingRamp;
		uniform half4 _Color;
		uniform samplerCUBE _FogCubemap;
		uniform half _FogStartDistance;
		uniform half _FogEndDistance;
		uniform half _FogExponent;
		uniform half _SaturationDistance;
		uniform half _FogStrength;
		uniform half _Overbrightening;

		uniform half _Emission;
		uniform half4 _EmissionColor;

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

			o.vertex = UnityObjectToClipPos(v.vertex);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			o.worldViewDirection = normalize(UnityWorldSpaceViewDir(worldPos));

			o.foggingRange = clamp(((distance(_WorldSpaceCameraPos, worldPos) - _FogStartDistance) / (_FogEndDistance - _FogStartDistance)), 0.0, 1.0);
			o.foggingRange = fastPow(o.foggingRange, _FogExponent);

			return o;
		}


		half4 frag(vertexOutput i) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(i);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			//lighting
			half halfLambert = dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * 0.5 + 0.5;
			half4 lightingRamp = tex2D(_LightingRamp, halfLambert.xx);
			half4 finalLighting = (half4(_LightColor0.rgb, 0.0) * lightingRamp) + (half4(UNITY_LIGHTMODEL_AMBIENT.xyz, 0));
			half4 litTexture = (finalLighting  * _Color) * _Overbrightening;
			litTexture += _EmissionColor * _Emission;

			//fogging
			half4 foggingColor = texCUBE(_FogCubemap, i.worldViewDirection);
			half3 finalColor = lerp(litTexture.rgb, foggingColor.rgb, (i.foggingRange * _FogStrength));
			finalColor = fastPow(finalColor, 0.454);
			return  half4(finalColor, 1.0);
		}
		ENDCG
		}

	}
}
