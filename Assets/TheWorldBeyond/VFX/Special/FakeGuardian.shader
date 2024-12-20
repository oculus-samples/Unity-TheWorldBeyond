// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/FakeGuardian" {
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_CamPosition("Cam Position", Vector) = (0,0,0,0)
		_WallScale("Wall Scale", Vector) = (1,1,0,0)
		_GuardianFade("Fade Amount", Range(0 , 1)) = 0

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
			Blend SrcAlpha OneMinusSrcAlpha, Zero One

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
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float3 _CamPosition;
			uniform float2 _WallScale;
			float _GuardianFade;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv);
				float xTile = ((i.uv.x * 2 * _WallScale.x) % 1);
				float yTile = ((i.uv.y * 2 * _WallScale.y) % 1);
				float grid = step(0.493, abs(xTile - 0.5));
				grid = max(grid, step(0.493, abs(yTile - 0.5)));
				float cornerFade = lerp(0.35, 0.0, saturate(_GuardianFade-0.5)*2);
				float corners = step(cornerFade, abs(xTile - 0.5));
				corners *= step(cornerFade, abs(yTile - 0.5));
				float camProximity = saturate(distance(i.worldPos, _CamPosition));
				float redRing = step(0.5, 1-abs((camProximity - 0.5) * 30));
				grid *= step(0.5, camProximity);
				float colorBlend = saturate(distance(i.worldPos, _CamPosition));
				float3 gridColor = lerp(float3(1, 0, 0), float3(0, 0.5, 1), smoothstep(0.5,0.6,colorBlend));
				float finalAlpha = saturate((corners * grid * _GuardianFade) + redRing);
				return float4(gridColor.r, gridColor.g, gridColor.b, finalAlpha);
			}
			ENDCG
		}
	}
}
