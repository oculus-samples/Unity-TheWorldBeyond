// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "TheWorldBeyond/DepthOnly" {
  Properties {
  }
  SubShader {
    Tags{"RenderType" = "Transparent"} LOD 100

        // First Pass: render outside shell of hand, as depth object
        Pass {
      ColorMask 0 Blend SrcAlpha OneMinusSrcAlpha CGPROGRAM
#pragma vertex vert
#pragma fragment frag
// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"

          struct appdata {
        float4 vertex : POSITION;
      };

      struct v2f {
        UNITY_FOG_COORDS(1)
        float4 vertex : SV_POSITION;
      };

      v2f vert(appdata v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        UNITY_TRANSFER_FOG(o, o.vertex);
        return o;
      }

      fixed4 frag(v2f i) : SV_Target {
        //clip(mask.r - 0.5);
        return float4(0,0,0,0);
      }
      ENDCG
    }
  }
}
