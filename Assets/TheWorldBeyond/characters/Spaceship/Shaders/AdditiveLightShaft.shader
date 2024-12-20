// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/AdditiveLightShaft" {
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
		_Brightness("Brightness", Range(0 , 1)) = 1

		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }
    SubShader
    {
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}

		Cull[_Cull]
		Blend One One
		ZWrite Off
		LOD 100

        Pass
        {
            CGPROGRAM
			#pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
			#pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float4 color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            uniform float4 _Color;
			uniform half _Brightness;

            v2f vert (appdata v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				fixed4 col = _Color * i.color;
				col *= i.color.a * _Brightness;

				return col;
            }
            ENDCG
        }
    }
}
