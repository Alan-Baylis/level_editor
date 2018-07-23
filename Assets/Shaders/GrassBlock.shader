// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "GrassBlock" {
	Properties {
		_GrassColor ("Grass Color", Color) = (1,1,1,1)
		_MudColor ("Mud Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB) Occlusion (A)", 2D) = "white" {}
		//_MetallicGlossMap ("Metallic (RGB)", 2D) = "white" {}
		//_BumpMap ("Normalmap", 2D) = "bump" {}
		//_Glossiness ("Smoothness", Range(0,1)) = 0.5
		//_OcclusionStrength ("Occlusion Strength", Range(0,1)) = 0.0
		//_Displacement ("Displacement", Range(0, 1.0)) = 0.3
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D 	_MainTex;
		sampler2D 	_MetallicGlossMap;
		sampler2D 	_BumpMap;
		//sampler2D 	_OcclusionMap;
		half        _OcclusionStrength;


		struct Input {
			float2 uv_MainTex;
			float customMask;
			float3 worldPos;
		};

		half _Glossiness;
		fixed4 _GrassColor;
		fixed4 _MudColor;
		float _Displacement;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.customMask = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).y;
		}
		
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 leafs = tex2D(_MainTex, IN.worldPos.xz);
			fixed mask = saturate(leafs * IN.customMask * 17 - 1.5);
			fixed mask2 = 1 - saturate(leafs * IN.customMask * 15) * 0.6;
			o.Albedo = lerp(_MudColor * mask2,_GrassColor * leafs,mask);//IN.customMask;//baseocc.rgb;
			// Metallic and smoothness come from slider variables
			//half4 metallic = tex2D (_MetallicGlossMap, IN.uv_MainTex);
			o.Metallic = 0;//metallic.r;
			o.Smoothness = 0.5;//metallic.a * _Glossiness;
			// o.Alpha = 0.5;
			
			//o.Normal = UnpackNormal(lerp(fixed4(0.5, 0.5, 1, 1), tex2D(_BumpMap, IN.worldPos.xz), mask));
			//o.Occlusion = LerpOneTo(baseocc.a, _OcclusionStrength);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
