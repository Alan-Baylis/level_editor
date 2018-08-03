Shader "BumpedTriplanar"
{
	Properties
	{
		[NoScaleOffset]	_TopAlbedo ("Top Albedo", 2D) = "white" {}
		[NoScaleOffset]	_TopNormal ("Top Normal", 2D) = "white" {}
		[NoScaleOffset]	_SideAlbedo("Side Albedo", 2D) = "white" {}
		[NoScaleOffset]	_SideNormal("Side Normal", 2D) = "white" {}
		[NoScaleOffset]	_BottomAlbedo("Bottom Albedo", 2D) = "white" {}
		[NoScaleOffset]	_BottomNormal("Bottom Normal", 2D) = "white" {}
		//[NoScaleOffset]	_MatCap("MatCap", 2D) = "white" {}
		//_MatCapColor("Mat Cap Color", Color) = (1,1,1,1)
		_MapScale ("Map Scale", float) = 1.0
		//_ChamferScale ("Chamfer Scale", float) = 1.0
		_MixMult ("Mix Mult", float) = 1.0
		_MixSub ("Mix Sub", float) = 1.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		CGINCLUDE
		#include "HLSLSupport.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"

		sampler2D _TopAlbedo, _TopNormal, _SideAlbedo, _SideNormal, _BottomAlbedo, _BottomNormal, _MatCap;
		float _MixMult;
		float _MixSub;
		fixed4 _MatCapColor;
		
		void GetTriplanarTextures(float3 worldPos, float3 worldNormal, float4 blend, out fixed4 albedo, out half3 normal)
		{
			// NORMAL SIGN
			float3 nsign = sign(worldNormal);

			// TOP
			fixed4 TopAlbedo = tex2D(_TopAlbedo, worldPos.zx);
			half3 TopNormal = UnpackNormal(tex2D(_TopNormal, worldPos.zx));

			// BOTTOM
			fixed4 BottomAlbedo = tex2D(_BottomAlbedo, worldPos.zx);
			half3 BottomNormal = UnpackNormal(tex2D(_BottomNormal, worldPos.zx));

			// SIDE X
			worldPos.z *= nsign.x;
			fixed4 xAlbedo = tex2D(_SideAlbedo, worldPos.zy);
			half3 xNorm = UnpackNormal(tex2D(_SideNormal, worldPos.zy));

			// SIDE Z
			worldPos.x *= -nsign.z;
			fixed4 zAlbedo = tex2D(_SideAlbedo, worldPos.xy);
			half3 zNorm = UnpackNormal(tex2D(_SideNormal, worldPos.xy));

			// use normal blending to wrap normal map to surface normal
			TopNormal = normalize(half3(TopNormal.xy + worldNormal.zx, worldNormal.y));
			BottomNormal = normalize(half3(BottomNormal.xy + worldNormal.zx, worldNormal.y));
			xNorm = normalize(half3(xNorm.xy * float2(nsign.x, 1.0) + worldNormal.zy, worldNormal.x));
			zNorm = normalize(half3(zNorm.xy * float2(-nsign.z, 1.0) + worldNormal.xy, worldNormal.z));

			// reorient normals to their world axis
			TopNormal = TopNormal.yzx;
			BottomNormal = BottomNormal.yzx;
			xNorm.xyz = xNorm.zyx;

			float topmask = saturate(TopAlbedo.a * blend.y * _MixMult - _MixSub);
			float topshadow = 1 - saturate(TopAlbedo.a * blend.y * 5 - 0.5);

			// blend normals together
			normal = xNorm * blend.x + TopNormal * topmask + BottomNormal * blend.w + zNorm * blend.z;

			// blend albedos together
			albedo = (xAlbedo * blend.x + zAlbedo * blend.z + BottomAlbedo * blend.w) * topshadow + TopAlbedo * topmask;
		}

		ENDCG

			Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			float _MapScale;
			float _ChamferScale;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD0;
				float4 blend : TEXCOORD1;
				//float3 c0 : TEXCOORD2;
				//float3 c1 : TEXCOORD3;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				
				//float3 chamfermap = o.worldNormal * abs(o.worldNormal * o.worldNormal) * _ChamferScale;
				
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz * _MapScale;

				float3 blend = normalize(abs(o.worldNormal));
				blend /= dot(blend, (float3)1);

				float3 nsign = sign(o.worldNormal);

				o.blend.x = blend.x;
				o.blend.y = saturate(blend.y * nsign.y);
				o.blend.w = saturate(blend.y * (1 - nsign.y));
				o.blend.z = blend.z;

				//v.normal = normalize(v.normal);
				//v.tangent = normalize(v.tangent);
				//TANGENT_SPACE_ROTATION;
				//o.c0 = mul(rotation, normalize(UNITY_MATRIX_IT_MV[0].xyz));
				//o.c1 = mul(rotation, normalize(UNITY_MATRIX_IT_MV[1].xyz));
				
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				fixed4 albedo;
				half3 normal;
				GetTriplanarTextures(i.worldPos, i.worldNormal, i.blend, albedo, normal);

				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				half3 lighting = saturate(dot(normalize(normal), _WorldSpaceLightPos0.xyz)) * _LightColor0.rgb * atten;
				lighting += ShadeSH9(half4(normal,1));

				//float3 matcoords = normalize(mul((float3x3)UNITY_MATRIX_V, normal));
				//fixed3 matcap = tex2D(_MatCap, matcoords.xy * 0.5 + 0.5).rgb;
				
				half3 col = albedo.rgb * lighting;// + matcap * _MatCapColor;

				UNITY_APPLY_FOG(i.fogCoord, col);
				return half4(col, 1);
			}
			ENDCG
		}

		/*Pass
		{
			Tags{ "LightMode" = "ForwardAdd" }
			ZWrite Off Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD0;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				fixed4 albedo;
				half3 normal;
				GetTriplanarTextures(i.worldPos, i.worldNormal, albedo, normal);

				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

				#ifndef USING_DIRECTIONAL_LIGHT
				fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				#else
				fixed3 lightDir = _WorldSpaceLightPos0.xyz;
				#endif
				half3 lighting = saturate(dot(normalize(normal), lightDir)) * _LightColor0.rgb * atten;

				half3 col = albedo.rgb * lighting;

				UNITY_APPLY_FOG(i.fogCoord, col);
				return half4(col, 1);
			}
			ENDCG
		}*/

		Pass
		{
			Tags{ "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f
			{
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}