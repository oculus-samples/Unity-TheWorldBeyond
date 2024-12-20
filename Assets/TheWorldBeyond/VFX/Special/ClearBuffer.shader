// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/ClearBuffer" {
	Properties
	{
		_ClearColor("Clear Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_InvertedAlpha("Inverted Alpha", float) = 1

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
				ZWrite On
				Cull Front
				ZTest[_ZTest]
			Blend SrcAlpha OneMinusSrcAlpha
				//BlendOp[_BlendOpColor],[_BlendOpAlpha]
				//Blend Zero One, One One

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
						};

						struct v2f
						{
							float2 uv : TEXCOORD0;
							float4 vertex : SV_POSITION;
						};

						float4 _ClearColor;
						float _InvertedAlpha;

						v2f vert(appdata v)
						{
							v2f o;
							o.vertex = UnityObjectToClipPos(v.vertex);
							float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
							o.uv = v.uv;
							return o;
						}

						fixed4 frag(v2f i) : SV_Target {
							float4 final = _ClearColor;
							final.a = lerp(final.a, 1 - final.a, _InvertedAlpha);
							return final;
						}
						ENDCG
					}
		}
}
