// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/PassthroughWall" {
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_EffectPosition("Effect Position", Vector) = (0,1000,0,1)
		_EffectTimer("Effect Timer", Range(0.0,1.0)) = 1.0
		_InvertedMask("Inverted Mask", float) = 1
		_PatternTiling("Pattern Tiling", float) = 1

		[Header(DepthTest)]
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOpColor("Blend Color", Float) = 2 //"ReverseSubtract"
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha("Blend Alpha", Float) = 3 //"Min"
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" }
			LOD 100

			Pass
			{
			// hack - comment out these 4 lines to see texture in editor view
				ZWrite Off
				ZTest[_ZTest]
				BlendOp[_BlendOpColor],[_BlendOpAlpha]
				Blend Zero One, One One

				CGPROGRAM
			// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members center)
			//#pragma exclude_renderers d3d11
						#pragma vertex vert
						#pragma fragment frag

						#include "UnityCG.cginc"

						struct appdata
						{
							float4 vertex : POSITION;
							float2 uv : TEXCOORD0;
							float3 normal : NORMAL;
							float4 color : COLOR;
						};

						struct v2f
						{
							float2 uv : TEXCOORD0;
							float4 vertex : SV_POSITION;
							float4 vertWorld : TEXCOORD1;
							half3 objectScale : TEXCOORD2;
							half4 sin : TEXCOORD3;
							float4 vertexColor : COLOR;

						};

						sampler2D _MainTex;
						float4 _MainTex_ST;
						float4 _EffectPosition;
						float _EffectTimer;
						float _InvertedMask;
						float _PatternTiling;





						v2f vert(appdata v)
						{
							v2f o;
							o.vertex = UnityObjectToClipPos(v.vertex);
							o.vertWorld = mul(unity_ObjectToWorld, v.vertex);
							float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
							o.uv = TRANSFORM_TEX(v.uv, _MainTex);
							// finding object scale in the vertex shader
							o.vertexColor = (1 - v.color) * 4;
							half3x3 m = (half3x3)UNITY_MATRIX_M;
							o.objectScale = half3(
								length(half3(m[0][0], m[1][0], m[2][0])),
								length(half3(m[0][1], m[1][1], m[2][1])),
								length(half3(m[0][2], m[1][2], m[2][2]))
								);
							o.sin.x = sin(_Time.y + 0.0) * 0.5 + 0.5;
							o.sin.y = sin(_Time.y + 1.0) * 0.5 + 0.5;
							o.sin.z = sin(_Time.y + 2.0) * 0.5 + 0.5;
							o.sin.w = sin(_Time.y + 3.0) * 0.5 + 0.5;

							return o;
						}

						fixed4 frag(v2f i) : SV_Target {
							float radialDist = distance(i.vertWorld, _EffectPosition) * 10;
							float dist = saturate(radialDist+1 - _EffectTimer * 50);
							// "max" the ring radius
							if (_EffectTimer >= 1.0) {
								dist = 0;
							}
							fixed4 col = tex2D(_MainTex, half2(i.uv.x * _PatternTiling, i.uv.y ) * _MainTex_ST.xy);
							//half debug = saturate( (abs(sin(_Time.y + 0.0)) * 4));

							half colAnimatedR = saturate((col.r * 5) - ((i.sin.x * 5) + i.vertexColor.r));
							half colAnimatedG = saturate((col.g * 5) - ((i.sin.y * 5) + i.vertexColor.r));
							half colAnimatedB = saturate((col.b * 5) - ((i.sin.z * 5) + i.vertexColor.r));
							half colAnimatedA = saturate((col.a * 5) - ((i.sin.w * 5) + i.vertexColor.r));

							float alpha = lerp(dist, 1 - dist, _InvertedMask);
							float final = alpha * saturate(colAnimatedR + colAnimatedG + colAnimatedB + colAnimatedA) ;
							//float final = alpha * saturate(colAnimatedR );

							//clip(final - 0.05);
							return float4(final, final, final, final);
						}
						ENDCG
					}
		}
}
