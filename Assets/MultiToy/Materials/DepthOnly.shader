/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
