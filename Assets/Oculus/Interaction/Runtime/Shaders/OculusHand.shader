/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/OculusHand"
{
    Properties
    {
        [Header(General)]
        _ColorTop("Color Top", Color) = (0.1960784, 0.2039215, 0.2117647, 1)
        _ColorBottom("Color Bottom", Color) = (0.1215686, 0.1254902, 0.1294117, 1)
        _Opacity("Opacity", Range( 0 , 1)) = 0.8

        [Header(Fresnel)]
        _FresnelPower("FresnelPower", Range( 0 , 5)) = 0.16

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.5377358,0.5377358,0.5377358,1)
        _OutlineWidth("Outline Width", Range( 0 , 0.005)) = 0.00134
        _OutlineOpacity("Outline Opacity", Range( 0 , 1)) = 0.4
        _OutlineIntensity("Outline Intensity", Range( 0 , 1)) = 1
        _OutlinePinchRange("Outline Pinch Range", Float) = 0.15
        _OutlineGlowIntensity("Outline Glow Intensity", Range( 0 , 1)) = 0
        _OutlineGlowColor("Outline Glow Color", Color) = (1,1,1,1)
        _OutlineSphereHardness("Outline Sphere Hardness", Range( 0 , 1)) = 0.3

        [Header(Pinch)]
        _PinchPosition("Pinch Position", Vector) = (0,0,0,0)
        _PinchRange("Pinch Range", Float) = 0.03
        _PinchIntensity("Pinch Intensity", Range( 0 , 1)) = 0
        _PinchColor("Pinch Color", Color) = (0.95,0.95,0.95,1)

        [Header(Wrist)]
        _WristLocalOffset("Wrist Local Offset", Vector) = (0,0,0,0)
        _WristRange("Wrist Range", Float) = 0.06
        _WristScale("Wrist Scale", Float) = 1.0

        [Header(Finger Glow)]
        _FingerGlowMask("Finger Glow Mask", 2D) = "white" {}
        _FingerGlowColor("Finger Glow Color", Color) = (1,1,1,1)

        [HideInInspector] _texcoord( "", 2D ) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"
        }
        LOD 200

        //==============================================================================
        // Depth Pass
        //==============================================================================

        Pass
        {
            ZWrite On
            Cull Off
            ColorMask 0
        }


        //==============================================================================
        // Outline Pass
        //==============================================================================

        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent+0"
        }
        Cull Front
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma target 3.0
#pragma surface outlineSurf Outline nofog keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc

        void outlineVertexDataFunc(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            v.vertex.xyz += v.normal * _OutlineWidth; // Outline
        }

        inline half4 LightingOutline(SurfaceOutput s, half3 lightDir, half atten) {
            return half4(0, 0, 0, s.Alpha);
        }

        void outlineSurf(Input i, inout SurfaceOutput o) {
            // Sphere mask
            float3 mask = ((i.worldPos - _PinchPosition) / _OutlinePinchRange);
            float dotValue = clamp(dot(mask, mask), 0.0, 1.0);
            float sphereMask = pow(dotValue, _OutlineSphereHardness);

            // Compute outline color
            float3 outlineGlow = saturate(1.0 - sphereMask) * _OutlineGlowIntensity *
                    _OutlineGlowColor.rgb;

            // Wrist
            float3 wristPosition = mul(unity_ObjectToWorld, _WristLocalOffset).xyz;
            float wristSphere = length(wristPosition - i.worldPos);
            float wristRangeScaled = _WristRange * _WristScale;
            float wristSphereStep = smoothstep(wristRangeScaled * 0.333, wristRangeScaled, wristSphere);

            // Output
            o.Emission = (_OutlineColor.rgb * _OutlineIntensity) + outlineGlow;
            o.Alpha = _OutlineOpacity * wristSphereStep;
            if (o.Alpha < 0.1)
            {
                discard;
            }
        }
        ENDCG

        //==============================================================================
        // Fesnel / Color / Alpha Pass
        //==============================================================================

        Tags
        {
            "RenderType" = "MaskedOutline" "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"
        }
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE
        #include "UnityPBSLighting.cginc"
#include "Lighting.cginc"
#pragma target 3.0

        struct Input {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_FingerGlowMask;
        };

        // General
        uniform float4 _ColorTop;
        uniform float4 _ColorBottom;
        uniform float _Opacity;
        uniform float _FresnelPower;

        // Outline
        uniform float _OutlineWidth;
        uniform float _OutlineIntensity;
        uniform float4 _OutlineColor;
        uniform float _OutlinePinchRange;
        uniform float _OutlineSphereHardness;
        uniform float _OutlineGlowIntensity;
        uniform float4 _OutlineGlowColor;
        uniform float _OutlineOpacity;

        // Pinch
        uniform float _PinchRange;
        uniform float3 _PinchPosition;
        uniform float _PinchIntensity;
        uniform float4 _PinchColor;

        // Wrist
        uniform float4 _WristLocalOffset;
        uniform float _WristRange;
        uniform float _WristScale;

        // Finger Glow
        uniform sampler2D _FingerGlowMask;
        uniform float4 _FingerGlowColor;
        uniform float _ThumbGlowValue;
        uniform float _IndexGlowValue;
        uniform float _MiddleGlowValue;
        uniform float _RingGlowValue;
        uniform float _PinkyGlowValue;

        void vertexDataFunc(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        inline half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten) {
            return half4(0, 0, 0, s.Alpha);
        }

        void surf(Input i, inout SurfaceOutput o) {
            float3 wristPosition = mul(unity_ObjectToWorld, _WristLocalOffset).xyz;
            float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

            // Fresnel + color
            float fresnelNdot = dot(i.worldNormal, worldViewDir);
            float fresnel = 1.0 * pow(1.0 - fresnelNdot, _FresnelPower);
            float4 color = lerp(_ColorTop, _ColorBottom, fresnel);

            // Pinch sphere
            float pinchSphere = length(_PinchPosition - i.worldPos);
            float pinchSphereStep = smoothstep(_PinchRange - 0.01, _PinchRange, pinchSphere);
            float4 pinchColor = float4(
                    saturate((1.0 - pinchSphereStep) * _PinchIntensity * _PinchColor.rgb),
                    0.0);

            // Wrist fade
            //
            // Visually this means that at approx 33% of the sphere range we want full transparency (map to 0)
            // to 100% of our sphere range to be opaque.
            float wristSphere = length(wristPosition - i.worldPos);
            float wristRangeScaled = _WristRange * _WristScale;
            float wristSphereStep = smoothstep(wristRangeScaled * 0.333, wristRangeScaled, wristSphere);

            // Finger glows
            //
            // the value is packed into a texture where each pixel has two bytes (red and green channels):
            //
            // the red channel is used to mask each glow area, currently every finger,
            // with the thumb being the 1st bit and pinky the 5th bit (6-8 currently unused).
            //
            // the green channel is used to indicate the intensity of the glow. This works only because the glow maps
            // between fingers do not overlap.

            float3 glowMaskPixelColor = tex2D(_FingerGlowMask, i.uv_FingerGlowMask);
            int glowMaskR = glowMaskPixelColor.r * 255;
            float glowIntensity = saturate(
                    glowMaskPixelColor.g *
                    ((glowMaskR & 0x1) * _ThumbGlowValue +
                        ((glowMaskR >> 1) & 0x1) * _IndexGlowValue +
                        ((glowMaskR >> 2) & 0x1) * _MiddleGlowValue +
                        ((glowMaskR >> 3) & 0x1) * _RingGlowValue +
                        ((glowMaskR >> 4) & 0x1) * _PinkyGlowValue));

            float4 glowColor = glowIntensity * _FingerGlowColor;

            // Emission + Opacity
            o.Emission = saturate(color + pinchColor + glowColor).rgb;
            o.Alpha = _Opacity * wristSphereStep;
        }
        ENDCG

        CGPROGRAM
        #pragma surface surf Unlit keepalpha vertex:vertexDataFunc
        ENDCG
    }
    Fallback "Diffuse"
}
