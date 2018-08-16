Shader "CGBumpedLighting"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_Normal("Normal", 2D) = "white" {}
	}

	SubShader
	{
		Tags {"RenderType" = "Opaque"}
		Cull Off

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _Albedo, _Normal;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 lightDir : LIGHTDIR;
				float3x3 rotationInv : ROTATIONINV;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_tan v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				o.texcoord = v.texcoord;

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);

				float3 binormal = cross(v.tangent.xyz, worldNormal);

				float3x3 rotation = float3x3(v.tangent.xyz, binormal, worldNormal);
				o.rotationInv = transpose(rotation);

				o.lightDir = normalize(mul(rotation, _WorldSpaceLightPos0.xyz));

				o.pos = UnityObjectToClipPos(v.vertex);

				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o,o.pos);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				fixed4 albedo = tex2D(_Albedo, i.texcoord);
				half3 bumpmap = UnpackNormal(tex2D(_Normal, i.texcoord));

				fixed shadow = SHADOW_ATTENUATION(i);
				half cn = saturate(dot(i.lightDir, bumpmap)) * shadow;
				half3 lighting = cn * _LightColor0.rgb;

				half3 worldNormal = mul(i.rotationInv, bumpmap);
				lighting += ShadeSH9(half4(worldNormal, 1));

				half3 col = albedo.rgb * lighting;

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
		/*
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
		}*/
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}