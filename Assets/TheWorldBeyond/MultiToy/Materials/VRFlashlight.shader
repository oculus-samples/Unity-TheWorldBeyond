// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/VRFlashlight" {
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Intensity("Intensity", float) = 1.0

		[Header(DepthTest)]
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
		[Toggle] _ZWrite("ZWrite", Float) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" }
			LOD 100

			Pass
			{
				ZWrite[_ZWrite]
				ZTest[_ZTest]
				BlendOp RevSub, Add
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
							float4 vertcol : COLOR;
						};

						struct v2f
						{
							float2 uv : TEXCOORD0;
							float4 vertex : SV_POSITION;
							float4 vertcol : TEXCOORD1;
							float2 projPos : TEXCOORD2;
							//float depth : DEPTH;
						};

						sampler2D _MainTex;
						float4 _MainTex_ST;
						float _Intensity;
						//sampler2D _CameraDepthTexture;

						v2f vert(appdata v)
						{
							v2f o;
							o.vertex = UnityObjectToClipPos(v.vertex);
							float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
							o.uv = TRANSFORM_TEX(v.uv, _MainTex);
							o.vertcol = v.vertcol;

							//o.depth = -UnityObjectToViewPos(v.vertex).z * _ProjectionParams.w;
							o.projPos = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;

							return o;
						}

						fixed4 frag(v2f i) : SV_Target {
							fixed4 col = tex2D(_MainTex, i.uv);

							// get a linear & normalized (0-1) depth value of the scene
							//float frameDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.projPos.xy));
							// convert the normalized value to world units, with depth of 0 at this pixel
							//frameDepth = (frameDepth - i.depth) * _ProjectionParams.z;
							// remap it to a fade distance
							//frameDepth = saturate(frameDepth / 0.1);

							return float4(0, 0, 0, col.r * _Intensity * i.vertcol.a);
						}
						ENDCG
					}
		}
}
