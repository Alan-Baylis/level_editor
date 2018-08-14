Shader "TriplanarBlocks"
{
	Properties
	{
		[NoScaleOffset]	_TopAlbedo ("Top Albedo", 2D) = "white" {}
		[NoScaleOffset]	_TopNormal ("Top Normal", 2D) = "white" {}
		[NoScaleOffset]	_SideAlbedo("Side Albedo", 2D) = "white" {}
		[NoScaleOffset]	_SideNormal("Side Normal", 2D) = "white" {}
		[NoScaleOffset]	_BottomAlbedo("Bottom Albedo", 2D) = "white" {}
		[NoScaleOffset]	_BottomNormal("Bottom Normal", 2D) = "white" {}
		[KeywordEnum(Opaque, Cutout)] _Blend("Blend Mode", Float) = 0
		_MapScale ("Map Scale", float) = 1.0
		_MixMult ("Mix Mult", float) = 1.0
		_MixSub ("Mix Sub", float) = 1.0
		_Cutoff ("Alpha cutoff", Range(0,1)) = 1.0
	}
	SubShader
	{
		Tags {"RenderType"="Opaque"}
		Cull Off

		CGINCLUDE
		#include "HLSLSupport.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "WorldDisplacement.cginc"

		sampler2D _TopAlbedo, _TopNormal, _SideAlbedo, _SideNormal, _BottomAlbedo, _BottomNormal;
		float _MixMult;
		float _MixSub;
		float _MapScale;
		fixed _Cutoff;
		
		// TRIPLANAR MAPPING
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
		
		// TRIPLANAR MAPPING FOR ALPHA
		void GetTriplanarAlpha(float3 worldPos, float3 worldNormal, float4 blend, out fixed alpha)
		{
			float3 nsign = sign(worldNormal);
		
			// TOP
			fixed4 TopAlbedo = tex2D(_TopAlbedo, worldPos.zx).a;

			// BOTTOM
			fixed4 BottomAlbedo = tex2D(_BottomAlbedo, worldPos.zx).a;

			// SIDE X
			worldPos.z *= nsign.x;
			fixed4 xAlbedo = tex2D(_SideAlbedo, worldPos.zy).a;

			// SIDE Z
			worldPos.x *= -nsign.z;
			fixed4 zAlbedo = tex2D(_SideAlbedo, worldPos.xy).a;

			float topmask = saturate(TopAlbedo * blend.y * _MixMult - _MixSub);

			// blend albedos together
			alpha = xAlbedo * blend.x + zAlbedo * blend.z + BottomAlbedo * blend.w + TopAlbedo * topmask;
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
			#pragma shader_feature _BLEND_OPAQUE _BLEND_CUTOUT
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float3 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD0;
				float4 blend : TEXCOORD1;
				SHADOW_COORDS(2)
				UNITY_FOG_COORDS(3)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_tan v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				
				// CONVERT VERTEX TO WORLD SPACE
				float4 worldvertex = mul(unity_ObjectToWorld, v.vertex);

				// DISTORT VERTEX IN WORLD SPACE
				float4 vertPosition = getNewVertPosition(worldvertex);
				
				// CONVERT VERTEX BACK TO OBJECT SPACE
				v.vertex = mul(unity_WorldToObject, vertPosition);

				// CREATE TRIPLANAR BLEND MASKS
				float3 blendnormal = UnityObjectToWorldNormal(v.normal);

				float3 blend = normalize(abs(blendnormal));
				blend /= dot(blend, (float3)1);

				float3 nsign = sign(blendnormal);

				o.blend.x = blend.x;
				o.blend.y = saturate(blend.y * nsign.y);
				o.blend.w = saturate(blend.y * (1 - nsign.y));
				o.blend.z = blend.z;

				// DISTORT NORMALS BASED ON THE NEW VERTEX POSITION
				//float4 bitangent = float4(cross(v.normal, v.tangent), 0);

				//float4 v1 = getNewVertPosition(worldvertex + v.tangent * 0.01) - vertPosition;
				//float4 v2 = getNewVertPosition(worldvertex + bitangent * 0.01) - vertPosition;

				//v.normal = cross(v1, v2);

				/////////////////
				
				o.pos = UnityObjectToClipPos(v.vertex);

				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				o.worldPos = worldvertex.xyz * _MapScale;

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
				
				half3 col = albedo.rgb * lighting;

				#ifdef _BLEND_CUTOUT 
				clip(albedo.a - _Cutoff);
				#endif
				
				UNITY_APPLY_FOG(i.fogCoord, col);
				return half4(col,albedo.a);
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
			#pragma shader_feature _BLEND_OPAQUE _BLEND_CUTOUT
			#include "UnityCG.cginc"

			struct v2f
			{
				V2F_SHADOW_CASTER;
				#ifdef _BLEND_CUTOUT
				float3 worldNormal : NORMAL;
				float3 worldPos : TEXCOORD0;
				float4 blend : TEXCOORD1;
				#endif
			};

			v2f vert(appdata_base v)
			{
				
				v2f o;
				
				// CONVERT VERTEX TO WORLD SPACE
				float4 worldvertex = mul(unity_ObjectToWorld, v.vertex);

				// DISTORT VERTEX IN WORLD SPACE
				float4 vertPosition = getNewVertPosition(worldvertex);
				
				// CONVERT VERTEX BACK TO OBJECT SPACE
				v.vertex = mul(unity_WorldToObject, vertPosition);

				// CREATE TRIPLANAR BLEND MASKS
				#ifdef _BLEND_CUTOUT

				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				float3 blend = normalize(abs(o.worldNormal));
				blend /= dot(blend, (float3)1);

				float3 nsign = sign(o.worldNormal);

				o.blend.x = blend.x;
				o.blend.y = saturate(blend.y * nsign.y);
				o.blend.w = saturate(blend.y * (1 - nsign.y));
				o.blend.z = blend.z;

				o.worldPos = worldvertex.xyz * _MapScale;

				#endif
				/////////////////
				
				o.pos = UnityObjectToClipPos(v.vertex);

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{
				#ifdef _BLEND_CUTOUT
				fixed alpha;
				GetTriplanarAlpha(i.worldPos, i.worldNormal, i.blend, alpha);
				clip(alpha - _Cutoff);
				#endif
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}