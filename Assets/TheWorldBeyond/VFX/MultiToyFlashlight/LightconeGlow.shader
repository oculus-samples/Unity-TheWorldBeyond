// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/LightconeGlow" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Intensity("Intensity", Range(0 , 1)) = 1
		_ScrollAmount("Scroll Amount", Float) = 0

		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 8 //"Always"
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" }
			LOD 100

			Pass
			{

			ZTest[_ZTest]
			Zwrite Off
			Cull Off
			Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
				half2 uvScroll1 : TEXCOORD1;
				half2 uvScroll2 : TEXCOORD2;
               // UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float2 projPos : TEXCOORD3;
				//float depth : DEPTH;
            };

            sampler2D _MainTex;
            half4 _MainTex_ST;
			half4 _Color;
			half _Intensity;
			float _ScrollAmount;
			sampler2D _CameraDepthTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
               // o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv = v.uv;

				float scroll = _ScrollAmount;
				// enable this to use game time (in editor)
				//float scroll = _Time.x;

				o.uvScroll1 = float2(o.uv.x + scroll.x * 2, o.uv.y + scroll.x * 3);
				o.uvScroll2 = float2(o.uv.x - scroll.x * 2, (o.uv.y * 0.5) + scroll.x * 2);

				//o.depth = -UnityObjectToViewPos(v.vertex).z * _ProjectionParams.w;
				o.projPos = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 map1 = tex2D(_MainTex, i.uvScroll1);
				half4 map2 = tex2D(_MainTex, i.uvScroll2);

				half mask = saturate (map1.r - map2.b);

				// get a linear & normalized (0-1) depth value of the scene
				//float frameDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.projPos.xy));
				// convert the normalized value to world units, with depth of 0 at this pixel
				//frameDepth = (frameDepth - i.depth) * _ProjectionParams.z;
				// remap it to a fade distance
				//frameDepth = saturate(frameDepth / 0.05);

				half4 color = lerp(_Color, _Color * 2, mask);
				half fade = i.uv.y * saturate((1 - i.uv.y) * 10);
				half4 final = float4(color.rgb, mask * fade * _Intensity);// *frameDepth);
                return final;
            }
            ENDCG
        }
    }
}
