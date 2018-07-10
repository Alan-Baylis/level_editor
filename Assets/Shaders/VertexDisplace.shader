Shader "Standard (VertexDisplace)" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB) Occlusion (A)", 2D) = "white" {}
		_MetallicGlossMap ("Metallic (RGB)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_OcclusionStrength ("Occlusion Strength", Range(0,1)) = 0.0
		_Displacement ("Displacement", Range(0, 1.0)) = 0.3
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:disp addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D 	_MainTex;
		sampler2D 	_MetallicGlossMap;
		sampler2D 	_BumpMap;
		//sampler2D 	_OcclusionMap;
		half        _OcclusionStrength;


		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		half _Glossiness;
		fixed4 _Color;
		float _Displacement;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void disp (inout appdata_full v)
        {
            v.vertex.xyz += v.normal * v.color.r * _Displacement * 0.2;
        }
		
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 baseocc = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = baseocc.rgb;
			// Metallic and smoothness come from slider variables
			half4 metallic = tex2D (_MetallicGlossMap, IN.uv_MainTex);
			o.Metallic = metallic.r;
			o.Smoothness = metallic.a * _Glossiness;
			// o.Alpha = 0.5;
			
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Occlusion = LerpOneTo(baseocc.a, _OcclusionStrength);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
