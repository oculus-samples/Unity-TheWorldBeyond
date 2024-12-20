// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/BallShader" {
	Properties
	{
		_CoreColor("CoreColor", Color) = (0,0,0,0)
		_Color("InnerColor", Color) = (0,0,0,0)
		_RimColor("RimColor", Color) = (0,0,0,0)
		_wobbleScale("wobbleScale", Range( -0.2 , 0.2)) = 0

	}
	CGINCLUDE
	half4 GetWobbleAmount(float3 worldPos, float time)
	{
		half4 wobbleVector = float4(sin((worldPos.x * 3.2) + (time * 6.3)), sin((worldPos.y * 5.0) + (time * 5.13)), sin((worldPos.z * 2.0) + (time * 3.23)), 0.0);
		return wobbleVector;
	}

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
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
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM



			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				half3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				half4 vertexColor : COLOR;
				half3 normals : normals;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform half _wobbleScale;
			uniform half4 _CoreColor;
			uniform half4 _RimColor;
			uniform half4 _Color;

			half4 fastPow(half4 a, half b) {
				return a / ((1.0 - b) * a + b);
			}

			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half4 wobbleVector = GetWobbleAmount(worldPos, _Time.y);
				o.normals = UnityObjectToWorldNormal(v.normal.xyz);
				o.vertexColor = v.color;

				float3 vertexValue = float3(0, 0, 0);
				half vertexMask = saturate ((1 - v.color.r) + 0.2 );
				vertexValue = ( wobbleVector * o.normals * _wobbleScale * vertexMask).xyz;
				v.vertex.xyz += vertexValue;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				return o;
			}

			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				half fresnel = saturate(dot(worldViewDir, i.normals.xyz));
				half4 baseColor = lerp( _RimColor ,_Color, pow( fresnel , 3.0 ));
				half coreColorThrobControl = ( sin( (_Time.y * 5.0) + (i.worldPos.z * 4) ) + sin ((_Time.y * 7.1) + (unity_WorldTransformParams.x * 2 )) ) * 2  + 5;
				half4 coreColor = pow(fresnel, coreColorThrobControl) * _CoreColor;
				half4 starColor =  _CoreColor * i.vertexColor * 0.5;
				half4 combinedColor = saturate(baseColor + coreColor + starColor);
				half4 finalColor = fastPow(combinedColor, 0.454);
				return half4 (finalColor.rgb, 1.0);;
			}
			ENDCG
		}

		// render outside shell of ball, as depth object
		// for the effect to work, this material must render before other opaque objects (<2000)
		Pass
		{
			ColorMask 0
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				half3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				half3 normals : normals;
			};

			uniform half _wobbleScale;

			v2f vert(appdata v)
			{
				v2f o;
				o.normals = UnityObjectToWorldNormal(v.normal.xyz);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half4 wobbleVector = GetWobbleAmount(worldPos, _Time.y);
				half vertexMask = saturate((1 - v.color.r) + 0.2);
				float3 vertexValue = (wobbleVector * o.normals * _wobbleScale * vertexMask).xyz;
				v.vertex.xyz += vertexValue;

				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 mask = float4(0,0,0,0);
				return mask;
			}
			ENDCG
		}
	}
}
