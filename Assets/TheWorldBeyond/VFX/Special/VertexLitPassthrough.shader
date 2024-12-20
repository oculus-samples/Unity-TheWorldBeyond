// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/VertexLitPassthrough" {
    Properties
    {
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
                            float4 color : COLOR;
                        };

                        struct v2f
                        {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                            float4 vertexColor : COLOR;
                            float3 worldPos : TEXCOORD1;
                            half3 normals : TEXCOORD2;
                        };

                        float _Inflation;
                        float _InvertedAlpha;

                        v2f vert(appdata v)
                        {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            float4 origin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                            o.uv = v.uv;
                            o.vertexColor = v.color;
                            o.normals = UnityObjectToWorldNormal(v.normal.xyz);
                            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                            return o;
                        }

                        fixed4 frag(v2f i) : SV_Target {
                            half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                            half fresnel = saturate(dot(worldViewDir, i.normals.xyz));
                            float alpha = 1-(i.vertexColor.a * smoothstep(0,0.5, fresnel));
                            return float4(alpha, 0, 0, alpha);
                        }
                        ENDCG
                    }
        }
}
