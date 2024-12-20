// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/LightBeamPassthroughAnimated" {
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
	    _Inflation("Inflation", float) = 0
		_AdditiveGlowStrength("Additive Glow Strength", Range(0 , 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

		// passthrough control
        Pass
        {
			ZWrite Off
            ZTest LEqual
			BlendOp RevSub, Min
            Blend Zero One, One One
			//Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				half2 uv : TEXCOORD0;
				half3 normal : NORMAL;
				half4 color : COLOR;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
				half4 vertexColor : COLOR;
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;
            half _Inflation;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex + v.normal * _Inflation);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.vertexColor = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
				half3 worldViewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));

				// fadeout with fresnel
				half fresnel = dot(worldViewDirection, i.worldNormal);
				fresnel = pow(fresnel, 5);
				// fadeout before clipping
				float fadeRangeBegin = 0.3;
				float fadeRangeEnd = 0.1;
				float FadeoutDistance = clamp(((distance(_WorldSpaceCameraPos, i.worldPos) - fadeRangeEnd) / (fadeRangeBegin - fadeRangeEnd)), 0.0, 1.0);

				// animate pulsing. Color is masked by vertex.r and texture.r
				half sinPulse = sin(-_Time.y + i.worldPos.y + col.b) * 0.5 + 0.5;
				sinPulse += sin(-_Time.w + i.worldPos.y + col.g) * 0.5 + 0.5;
				half alpha = col.r * sinPulse * fresnel * i.vertexColor.r * FadeoutDistance;
			   return half4(0, 0, 0, 1 - alpha);

				//return half4(1, 1, 1, alpha);

            }
            ENDCG
        }


		// inner glow
		Pass
		{
			ZWrite Off
			ZTest LEqual
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half2 uv : TEXCOORD0;
				half3 normal : NORMAL;
				half4 color : COLOR;
			};

			struct v2f
			{
				half2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
				half4 vertexColor : COLOR;
			};

			sampler2D _MainTex;
			half4 _MainTex_ST;
			half _Inflation;
			half _AdditiveGlowStrength;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex + v.normal * _Inflation);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.vertexColor = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv);
				half3 worldViewDirection = normalize(UnityWorldSpaceViewDir(i.worldPos));
				// fadeout with fresnel
				half fresnel = saturate( dot( worldViewDirection, i.worldNormal));
				fresnel = pow(fresnel, 5);
				// fadeout before clipping
				float fadeRangeBegin = 0.3;
				float fadeRangeEnd = 0.1;
				float FadeoutDistance =  clamp(((distance(_WorldSpaceCameraPos, i.worldPos) - fadeRangeEnd) / (fadeRangeBegin - fadeRangeEnd)), 0.0, 1.0);

				// animate pulsing. Color is masked by vertex.r and texture alpha
				half sinPulse = sin(-_Time.y + i.worldPos.y + col.b) * 0.5 + 0.5;
				sinPulse += sin(-_Time.w + i.worldPos.y + col.g) * 0.5 + 0.5;
				half alpha = col.a * sinPulse * _AdditiveGlowStrength * fresnel * i.vertexColor.r * FadeoutDistance;
				return half4(alpha, alpha, alpha, alpha);

				}
				ENDCG
			}


    }
}
