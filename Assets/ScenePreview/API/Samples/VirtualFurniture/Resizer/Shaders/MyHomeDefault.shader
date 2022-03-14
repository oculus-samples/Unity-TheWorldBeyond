Shader "Custom/MyHome Default" {
	Properties {
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5

		[Space(10)]
		_BumpMap ("Normal Map", 2D) = "bump" {}

		[Space(10)]
		[Header(SPECULAR)]
		[Toggle] _IsSpecular ("Enable", int) = 0
		_Shininess ("Shininess", Range (0, 0.999)) = 0.078125
		_Glossiness("Glossiness", Range (0, 1)) = 1
		_GlossinessMap ("Glossiness Map", 2D) = "white" {}

		[Space(10)]
		[Header(TRIPLANAR)]
		[Toggle] _IsTriplanar ("Enable", int) = 0
		_Tiling("Tiling", Range(0,50)) = 1
	}

	SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 400
		Cull Off


		CGPROGRAM
		#pragma surface surf Custom alphatest:_Cutoff vertex:vert exclude_path:prepass nolightmap noforwardadd interpolateview
		#pragma target 4.0

		#pragma shader_feature _NORMALMAP

		float4 _Color;
		sampler2D _MainTex;
		sampler2D _BumpMap;

		int _IsTriplanar;
		float _Tiling;

		int _IsSpecular;
		float _Shininess;
		float _Glossiness;
		sampler2D _GlossinessMap;

		float3 ApplySaturation(float4 startcolor, float saturation){
			float3 intensity = dot(startcolor.rgb, float3(0.299,0.587,0.114));
			startcolor.rgb = lerp(intensity, startcolor.rgb, saturation);
			return startcolor.rgb;
		}

		float Remap (float value, float from1, float to1, float from2, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		inline fixed4 LightingCustom (SurfaceOutput s, fixed3 lightDir, fixed3 floatDir, fixed atten)
		{
			// Compute lighting
			fixed diff = max (0, dot (s.Normal, lightDir));
			fixed nh = max (0, dot (s.Normal, floatDir));
			fixed spec = pow (nh, s.Specular*128) * s.Gloss;
			spec = lerp (0, spec, _IsSpecular);

			float3 lightColor = _LightColor0.rgb;
			fixed3 lighting = lightColor * diff;
			float shadowsIntensity =  -0.3;
			float shadowsSaturation = 0.45;
			float shadowsBrightness = -0.25;
			float shadows = lerp ( shadowsIntensity, 1, atten);
			float3 finalLighting = lighting * shadows + spec;

			// Set shadows colors as a saturated dark version of the main color
			fixed4 color = float4 (_Color * s.Albedo.rgb, 1);
			fixed3 shadowsColor = ApplySaturation(color, shadowsSaturation) + shadowsBrightness;
			color.rgb = lerp (shadowsColor, color, finalLighting);

			// Make colors dark when light is low (below 0.3)
			float lightIntensity = dot(lightColor, float3(0.299,0.587,0.114));
			lightIntensity = Remap(lightIntensity, 0, 0.3, 0, 1);
			color.rgb *= saturate(lightIntensity);

			color.rgb *= lightColor;

			UNITY_OPAQUE_ALPHA(color.a);
			return color;
		}

		struct Input {
			float2 uv_MainTex;
			float4 color: COLOR;
			float3 localPos: TEXCOORD2;
			float3 localNormal: TEXCOORD3;
		};

		void vert(inout appdata_full v, out Input IN)
		{
			UNITY_INITIALIZE_OUTPUT(Input, IN);
			IN.localPos = v.vertex.xyz;
			IN.localNormal = v.normal.xyz;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			float4 triplanarColor = 1;
			float3 triplanarNormal = 1;
			float triplanarGloss = 0;

			if(_IsTriplanar == 1)
			{
				// Blending factor of triplanar mapping
				float3 bf = normalize(abs(IN.localNormal));
				bf /= dot(bf, (float3)1);
				// Triplanar mapping
				float2 tx = IN.localPos.yz * _Tiling;
				float2 ty = IN.localPos.zx * _Tiling;
				float2 tz = IN.localPos.xy * _Tiling;
				// Triplanar color
				float4 cx = tex2D(_MainTex, tx) * bf.x;
				float4 cy = tex2D(_MainTex, ty) * bf.y;
				float4 cz = tex2D(_MainTex, tz) * bf.z;
				triplanarColor = (cx + cy + cz);

				// Triplanar normal map
				float4 nx = tex2D(_BumpMap, tx) * bf.x;
				float4 ny = tex2D(_BumpMap, ty) * bf.y;
				float4 nz = tex2D(_BumpMap, tz) * bf.z;
				triplanarNormal = UnpackScaleNormal(nx + ny + nz, _Tiling);

				// Specular and triplanar glossiness
				float mx = tex2D(_GlossinessMap, tx).g * bf.x;
				float my = tex2D(_GlossinessMap, ty).g * bf.y;
				float mz = tex2D(_GlossinessMap, tz).g * bf.z;
				triplanarGloss = lerp((float4)1, mx + my + mz, _Tiling) ;
			}

			float4 standardColor = tex2D(_MainTex, IN.uv_MainTex);
			float4 finalColor = lerp (standardColor, triplanarColor, _IsTriplanar);
			//Use vertex color as ambient occlusion
			float3 occlusionsColor = finalColor - 0.3;
			float3 colorAO = lerp (occlusionsColor, finalColor, IN.color) ;

			o.Albedo = colorAO;
			o.Alpha = standardColor.a;

			float standardGloss = tex2D(_GlossinessMap, IN.uv_MainTex);
			o.Gloss = lerp (standardGloss, triplanarGloss, _IsTriplanar) * _Glossiness;
			o.Specular = _Shininess;

			float3 standardNormal = UnpackNormal (tex2D(_BumpMap, IN.uv_MainTex));
			float3 finalNormal = lerp (standardNormal, triplanarNormal, _IsTriplanar);
			//o.Normal = finalNormal;
		}
		ENDCG
	}

	FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
