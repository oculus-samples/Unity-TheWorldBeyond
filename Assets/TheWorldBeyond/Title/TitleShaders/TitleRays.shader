// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/TitleRays" {
	Properties
	{
		_Color("Color", Color) = (0,0,0,0)
		_FogCubemap("Fog Cubemap", CUBE) = "white" {}

	}
		SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100
		ZTest LEqual
		ZWrite Off
		Cull Off

		Pass
		{
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			half4 _Color;
			uniform samplerCUBE _FogCubemap;

            struct meshdata
            {
                float4 vertex : POSITION;
				half4 vertexColor : COLOR;
            };

            struct interpolators
            {
                float4 vertex : SV_POSITION;
				half4 vertexColor : TEXCOORD0;
				float3 worldPos : TEXCOORD1;

            };


            interpolators vert (meshdata v)
            {
                interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.vertexColor = v.vertexColor;
                return o;
            }

            fixed4 frag (interpolators i) : SV_Target
            {
				half distanceGradient = pow(distance(half3(20, 200, 300), i.worldPos), 0.6);
				half distanceRipple = sin(distanceGradient * 2 + ((_Time.y + _Time.x) * 2)) * 0.25 + 1;
				//return frac(distanceRipple);

				half3 worldViewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));
				half4 foggingColor = texCUBE(_FogCubemap, worldViewDirection);
                fixed4 col = i.vertexColor * _Color * foggingColor * distanceRipple;
                return col;
            }
            ENDCG
        }
    }
}
