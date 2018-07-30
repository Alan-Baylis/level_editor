Shader "Standard Triplanar"
{
	Properties
	{
	_Color("Color", Color) = (1, 1, 1, 1)
	_MainTex("Main Texture", 2D) = "white" {}
	_BumpScale("Normal Scale", Float) = 1
	_BumpMap("Normal Map", 2D) = "bump" {}
	_MapScale("Mas Scale", Float) = 1
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		CGPROGRAM

		#pragma surface surf Lambert vertex:vert fullforwardshadows addshadow
		#pragma target 3.0

		half4 _Color;
		sampler2D _MainTex;

		half _Glossiness;
		half _Metallic;

		half _BumpScale;
		sampler2D _BumpMap;

		half _OcclusionStrength;
		sampler2D _OcclusionMap;

		half _MapScale;

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
		};

		struct Input
		{
			float3 texcoord;
			float3 blend;
			float3 worldnormal;
			float4 tangent;
		};

		void vert(inout appdata v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.worldnormal = UnityObjectToWorldNormal(v.normal).xyz;

			o.tangent = v.tangent;// float4(1, 1, 0, 1);//

			float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;

			o.blend = normalize(abs(o.worldnormal));
			o.blend /= dot(o.blend, (float3)1);

			//o.blend = saturate(pow(o.worldnormal * 1.4, 4));
			//o.blend /= o.blend.x + o.blend.y + o.blend.z;


			o.texcoord.zy = worldpos.zy * _MapScale;
			o.texcoord.zx = worldpos.zx * _MapScale;
			o.texcoord.xy = worldpos.xy * _MapScale;

		}

		void surf(Input i, inout SurfaceOutput o)
		{
			// Base color
			//half4 cx = tex2D(_MainTex, i.localCoord.yz) * bf.x;
			//half4 cy = tex2D(_MainTex, i.localCoord.zx) * bf.y;
			//half4 cz = tex2D(_MainTex, i.localCoord.xy) * bf.z;
			//half4 color = (cx + cy + cz) * _Color;
			o.Albedo = 0.9;// color.rgb;
			o.Alpha = _Color.a;// color.a;

			// Normal map
			float3 normalsign = sign(i.worldnormal);

			half3 nx = UnpackNormal(tex2D(_BumpMap, i.texcoord.zy));
			half3 ny = UnpackNormal(tex2D(_BumpMap, i.texcoord.zx));
			half3 nz = UnpackNormal(tex2D(_BumpMap, i.texcoord.xy * float2(-normalsign.z, 1.0)));

			//nx = normalize(half3(nx.xy * float2(normalsign.x, 1.0) + i.worldnormal.zy, i.worldnormal.x));
			//ny = normalize(half3(ny.xy + i.worldnormal.zx, i.worldnormal.y));
			//nz = normalize(half3(nz.xy * float2(-normalsign.z, 1.0) + i.worldnormal.xy, i.worldnormal.z));

			//nx = nx.zyx;
			ny = ny.yzx;

			o.Normal = nx;// *i.blend.x + ny * i.blend.y + nz * i.blend.z;

			// Misc parameters
			//o.Metallic = 0;// _Metallic;
			//o.Smoothness = 0.5;// _Glossiness;
		}
	ENDCG
	}
		FallBack "Diffuse"
}
