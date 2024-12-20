// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/OppyDimension"
{
		Properties
		{
			_SaturationAmount("Saturation Amount", Range(0 , 1)) = 1
			//_SaturationDistance("Saturation Distance", Range(0 , 1)) = 1
			_FogCubemap("Fog Cubemap", CUBE) = "white" {}
			_FogStrength("Fog Strength", Range(0 , 1)) = 1
			_FogStartDistance("Fog Start Distance", Range(0 , 100)) = 1
			_FogEndDistance("Fog End Distance", Range(0 , 2000)) = 100
			_FogExponent("Fog Exponent", Range(0 , 1)) = 1
			_LightingRamp("Lighting Ramp", 2D) = "white" {}
			_MainTex("MainTex", 2D) = "white" {}
			_TriPlanarFalloff("Triplanar Falloff", Range(0 , 10)) = 1
			_OppyPosition("Oppy Position", Vector) = (0,1000,0,0)
			_OppyRippleStrength("Oppy Ripple Strength", Range(0 , 1)) = 1
			_MaskRippleStrength("Mask Ripple Strength", Range(0, 1)) = 0

			_Color("Color", Color) = (0,0,0,0)

		    _EffectPosition("Effect Position", Vector) = (0,1000,0,1)
		    _EffectTimer("Effect Timer", Range(0.0,1.0)) = 1.0
		    _InvertedMask("Inverted Mask", float) = 1

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

			SubShader
		{
			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
			LOD 100

			CGINCLUDE
			#pragma target 3.0
			ENDCG
			AlphaToMask Off
			Cull Back
			ColorMask RGBA
			ZWrite On
			ZTest LEqual

			// the blending here (specifically the second part of each line: Min, One Zero) is what reveals Passthrough
			BlendOp Add, Min
			Blend One Zero, One Zero

			Offset 0 , 0

			Pass
			{
				Name "Base"
				Tags { "LightMode" = "ForwardBase" }
				CGPROGRAM

				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				// only defining to not throw compilation error over Unity 5.5
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
					float3 worldPos : TEXCOORD0;
					half3 worldNormal : TEXCOORD1;
					half3 projNormal : TEXCOORD2;
					half3 normalSign : TEXCOORD3;
					half3 worldViewDirection : TEXCOORD4;
					half foggingRange : TEXCOORD5;

					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};




				uniform sampler2D _LightingRamp;
				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
				uniform half _TriPlanarFalloff;
				uniform half4 _Color;
				uniform samplerCUBE _FogCubemap;
				uniform half _FogStartDistance;
				uniform half _FogEndDistance;
				uniform half _FogExponent;
				uniform half _SaturationAmount;
				uniform half _FogStrength;
				uniform float3 _OppyPosition;
				uniform half _OppyRippleStrength;
                uniform half _MaskRippleStrength;
				uniform float4 _EffectPosition;
				uniform float _EffectTimer;
				uniform float _InvertedMask;


				inline half4 TriplanarSampler(sampler2D projectedTexture, float3 worldPos, half3 normalSign, half3 projNormal, half2 tiling)
				{
					half4 xNorm = tex2D(projectedTexture, tiling * worldPos.zy * half2(normalSign.x, 1.0) + _MainTex_ST.zw);
					half4 yNorm = tex2D(projectedTexture, tiling * worldPos.xz * half2(normalSign.y, 1.0) + _MainTex_ST.zw);
					half4 zNorm = tex2D(projectedTexture, tiling * worldPos.xy * half2(-normalSign.z, 1.0) + _MainTex_ST.zw);
					return ( xNorm * projNormal.x ) + ( yNorm * projNormal.y ) + ( zNorm * projNormal.z );
				}

				half3 fastPow(half3 a, half b) {
					return a / ((1.0 - b) * a + b);
				}


				vertexOutput vert(vertexInput v)
				{
					vertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);

					o.worldNormal = UnityObjectToWorldNormal(v.normal);
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.worldViewDirection = normalize(UnityWorldSpaceViewDir(o.worldPos));

					o.projNormal= (pow(abs(o.worldNormal.xyz), _TriPlanarFalloff));
					o.projNormal /= (o.projNormal.x + o.projNormal.y + o.projNormal.z) + 0.00001;
					o.normalSign = sign(o.worldNormal.xyz);

					o.foggingRange = clamp(((distance(_WorldSpaceCameraPos, o.worldPos) - _FogStartDistance) / (_FogEndDistance - _FogStartDistance)), 0.0, 1.0);
					o.foggingRange = fastPow(o.foggingRange, _FogExponent);

					return o;
				}



				fixed4 frag(vertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					// main texture
					half4 mainTextureTriPlanar = TriplanarSampler(_MainTex, i.worldPos, i.normalSign, i.projNormal, _MainTex_ST.xy );

					// distance gradient
					half distanceToOppy = pow(distance(_OppyPosition, i.worldPos), 1.5);
					half distanceToOppyMask = saturate(1 - (distanceToOppy * 0.2));
					half distanceRipple = saturate(sin((distanceToOppy * 6) + (_Time.w * 2) ) * 0.5 + 0.25) * distanceToOppyMask * 4;
					// noisy mask
					half noisyMask = ((((_Time.y * 0.06 + (mainTextureTriPlanar.b * 1.73)) + (_Time.y * 0.19 + (mainTextureTriPlanar.g * 1.52))) % 1.0));
					noisyMask = abs(noisyMask - 0.5) * 2;

					// lighting
					half halfLambert = dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * 0.5 + 0.5;
					half4 lightingRamp = tex2D(_LightingRamp, halfLambert.xx);
					half4 finalLighting = (half4(_LightColor0.rgb, 0.0)  * lightingRamp) + (half4(UNITY_LIGHTMODEL_AMBIENT.xyz, 0));
					half4 litTexture = (finalLighting * _Color) + (mainTextureTriPlanar.rrrr   * ((noisyMask * 0.85) + (distanceRipple * _OppyRippleStrength)));

					// fogging
					half4 foggingColor = texCUBE(_FogCubemap, i.worldViewDirection);
					half4 foggedColor = lerp(litTexture, foggingColor , (i.foggingRange * _FogStrength));

					// desaturating
					half desaturatedColor = dot(foggedColor,  half3(0.299, 0.587, 0.114));
					// saturating with distance
					half3 finalColor = lerp( desaturatedColor.xxx, foggedColor, _SaturationAmount);
					finalColor = fastPow(finalColor, 0.455);

					// clip out pixels when toggling walls
					// this allows depth writing to still happen, ensuring that transparent effects aren't visible "through" the wall
					float radialDist = distance(i.worldPos, _EffectPosition) * 10;
					float dist = saturate(radialDist + 5 - _EffectTimer * 50);
					// "max" the ring radius
					if (_EffectTimer >= 1.0) {
						dist = 0;
					}
					float alpha = lerp(dist, 1 - dist, _InvertedMask);
					clip(alpha.r - 0.5);

					half distanceToBall = distance(_OppyPosition, i.worldPos);
					half maskRipple = saturate(sin((distanceToBall * 20) + (_Time.w * 2)) * 0.5 + 0.25) * saturate(1 - (distanceToBall * 0.5)) * 0.7;
                    maskRipple *= saturate((distanceToBall-0.2)*5);
                    return half4(finalColor, maskRipple * _MaskRippleStrength);
				}
				ENDCG
			}


	}
}
