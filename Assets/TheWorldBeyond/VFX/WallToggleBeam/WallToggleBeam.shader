// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/WallToggleBeam" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
			Zwrite Off
			ZTest LEqual
			Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half2 uv : TEXCOORD0;
				half4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				half4 color : COLOR;
				float3 objectPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;
            uniform half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.objectPos = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {


				half wigglyUVs = sin((i.objectPos.z + _Time.y) * 10)  ;
				wigglyUVs += sin(((i.objectPos.z * 0.5) + (_Time.y * 1.7)) * 10);
				wigglyUVs += sin(((i.objectPos.z * 0.25) + (_Time.y * 1.312)) * 10);
				wigglyUVs = wigglyUVs * 0.1666 + 0.5;

				half wiggleMaskMuzzle = saturate(i.uv.x * 4);
				wiggleMaskMuzzle = 1 - pow(1 - wiggleMaskMuzzle, 3);

				half wiggleMaskTarget = saturate((1 - i.uv.x) * 2);
				wiggleMaskTarget = 1 - pow(1 - wiggleMaskTarget, 3);

				half wiggleMasks = saturate (wiggleMaskMuzzle * wiggleMaskTarget);

				half2 animatedUV = float2(i.uv.x , i.uv.y + (((wigglyUVs * wiggleMasks) * 0.3) ));
				half4 BeamTexture = tex2D(_MainTex, animatedUV);

				half beamAnimatedPulse = frac(animatedUV.x * 5 -(_Time.y * 4));
				beamAnimatedPulse = saturate(abs(beamAnimatedPulse - 0.5) - 0.05);
				beamAnimatedPulse = saturate(beamAnimatedPulse * 20);

				half beamAlpha = BeamTexture.a  * i.color.a * beamAnimatedPulse;

				half4 finalColor = half4 ((BeamTexture.rgb  * i.color.rgb * _Color.rgb), beamAlpha);

                return (finalColor);
            }
            ENDCG
        }
    }
}
