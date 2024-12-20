// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/PassthroughShadow" {
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}

		[Header(DepthTest)]
		[Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0 //"Off"
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
				ZWrite[_ZWrite]
				ZTest[_ZTest]
				BlendOp[_BlendOpColor],[_BlendOpAlpha]
				Blend SrcAlpha OneMinusSrcAlpha, One One

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

						sampler2D _MainTex;
						float4 _MainTex_ST;

						v2f vert(appdata v)
						{
							v2f o;
							o.vertex = UnityObjectToClipPos(v.vertex);
							float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
							o.uv = TRANSFORM_TEX(v.uv, _MainTex);
							return o;
						}

						fixed4 frag(v2f i) : SV_Target {
							fixed4 col = tex2D(_MainTex, i.uv);
							return float4(0, 0, 0, col.a);
						}
						ENDCG
					}
		}
}
