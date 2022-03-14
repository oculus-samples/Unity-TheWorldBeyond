Shader "Custom/Pixelated Cube" {
    Properties {
        _PixelSize ("Pixel Size", Float ) = 16
        [Space(15)]
        _Base_Color2 ("Color Light", Color) = (0.6029412,0.4513167,0.1330017,1)
        _Base_Color1 ("Color Dark", Color) = (0.3455882,0.230731,0.05844506,1)

        [Header(COLOR MASK)]
        [Toggle] _Mask_Enable ("Enable", Float ) = 0
        [Toggle] _Mask_Procedural ("Enable Procedural Mask", Float ) = 0
        _Mask_Bleeding ("Bleeding", Range(0, 2)) = 0.25
        //_Pivot_Offset ("Offset", vector) = (1,1,1,1)
        _Mask_Opacity ("Opacity", Range(0, 1)) = 1
        [Space(15)]
        _Mask ("Texture", 2D) = "white" {}
        _Mask_Color1 ("Color Light", Color) = (0.4214473,0.6029412,0.1330017,1)
        _Mask_Color2 ("Color Dark", Color) = (0.2483227,0.427451,0.04693579,1)

        [Header(DETAIL MASK)]
        _Detail_Texture ("Opacity", Range(0,1) ) = 0.6029412
        _Detail_Mask ("Texture", 2D) = "white" {}
        _Detail_Color ("Color", Color) = (0.2794118,0.1781888,0.08628894,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Back

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _PixelSize;
            float4 _Base_Color1;
            float4 _Base_Color2;

            float _Mask_Enable;
            float _Mask_Procedural;
            float _Mask_Bleeding;
            float4 _Pivot_Offset;
            float _Mask_Opacity;
            sampler2D _Mask;
            float4 _Mask_ST;
            float4 _Mask_Color1;
            float4 _Mask_Color2;

            float _Detail_Texture;
            sampler2D _Detail_Mask;
            float4 _Detail_Mask_ST;
            float4 _Detail_Color;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float3 localNormal : NORMAL;
                float3 worldNormal : WORLDNORMAL;
                float4 col : COLOR;
                float3 worldPos : WORLDPOS;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.localNormal = v.normal;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityObjectToClipPos( v.vertex );
                o.worldPos = v.vertex.xyz;
                float h = (_Mask_Bleeding + (v.vertex.y / _Pivot_Offset.y) ) / (_Mask_Bleeding/_Pivot_Offset.y);
                o.col = lerp (0,1, h);
                return o;
            }

            float Pixelate(float2 uv)
            {
                float2 tiling = floor(float2(uv.x * _PixelSize, uv.y * _PixelSize));
                float2 pixel = tiling + 0.2127 + tiling.x * 0.3713 * tiling.y;
                float2 noise = 4.789 * sin(489.123 * (pixel));
                float pixelated = frac(noise.x * noise.y * (1 + pixel.x));
                return pixelated;
            }

            float MapTexture(sampler2D tex, float4 tex_ST, float2 uv)
            {
                float2 mappedUV = TRANSFORM_TEX (uv, tex);
                float4 mappedTex = floor(tex2D(tex, mappedUV));
                return mappedTex;
            }

            float4 TriplanarColor(float4 colorX, float4 colorY, float4 colorZ, float3 normals)
            {
                float4 col_front = colorX * normals.z;
                float4 col_side = colorZ  * normals.x;
                float4 col_top = colorY * normals.y;

                return col_front + col_side + col_top;
            }

            float4 frag(VertexOutput i) : COLOR {
                float3 normals = normalize(abs(i.localNormal));
                normals /= dot(normals, (float3)1);
                float2 uvX = i.worldPos.zy;
                float2 uvY = i.worldPos.xz;
                float2 uvZ = i.worldPos.xy;

                float4 pixelated = TriplanarColor (Pixelate (uvZ), Pixelate (uvY), Pixelate (uvX), normals);

                float4 maskTex = TriplanarColor (MapTexture(_Mask, _Mask_ST, uvZ), MapTexture(_Mask, _Mask_ST, uvY), MapTexture(_Mask, _Mask_ST, uvX), normals);
                float4 detailTex = TriplanarColor (MapTexture(_Detail_Mask, _Detail_Mask_ST, uvZ), MapTexture(_Detail_Mask, _Detail_Mask_ST, uvY), MapTexture(_Detail_Mask, _Detail_Mask_ST, uvX), normals);

                float3 mainColor = lerp(_Base_Color1.rgb, _Base_Color2.rgb, pixelated);
                float3 detailColor = lerp(_Detail_Color.rgb, mainColor, detailTex.rgb * _Detail_Color.a);
                float3 enableDetail = lerp( mainColor, detailColor, _Detail_Texture );

                float3 maskColor = lerp(_Mask_Color1.rgb, _Mask_Color2.rgb, pixelated);
                maskColor = lerp(maskColor,mainColor,(1.0 - _Mask_Opacity));

                float proceduralFade = saturate(i.col + pixelated);
                float proceduralMask = lerp( maskTex.rgb, proceduralFade, _Mask_Procedural );
                proceduralMask = lerp (0, proceduralMask, _Mask_Enable);


                float4 lighting = TriplanarColor (float4(0.9,0.9,0.9,1), float4(1.15,1.15,1.15,1), float4(0.65,0.65,0.65,1), normalize(abs(i.worldNormal)));
                float3 finalColor = lerp(enableDetail,maskColor,proceduralMask) * lighting;
                return float4(finalColor.rgb,1);
            }
            ENDCG
        }
    }
}
