// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

Shader "Standard Triplanar"
{
    Properties
    {
        _Color("", Color) = (1, 1, 1, 1)
        _MainTex("", 2D) = "white" {}

        _Glossiness("", Range(0, 1)) = 0.5
        [Gamma] _Metallic("", Range(0, 1)) = 0

        _BumpScale("", Float) = 1
        _BumpMap("", 2D) = "bump" {}

        _OcclusionStrength("", Range(0, 1)) = 1
        _OcclusionMap("", 2D) = "white" {}

        _MapScale("", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        #pragma shader_feature _NORMALMAP
        //#pragma shader_feature _OCCLUSIONMAP

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

        struct Input
        {
            float3 worldPos;
            float3 myNormal;
			//float2 tx;
            //float2 ty;
            //float2 tz;
			float3 bf;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldnormals = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
			o.myNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
			
			// Blending factor of triplanar mapping
            o.bf = normalize(abs(worldnormals));
            o.bf /= dot(o.bf, (float3)1);

			//o.worldPos *= _MapScale;
			
            // Triplanar mapping
            //float2 tx = worldpos.yz * _MapScale;
            //float2 ty = worldpos.zx * _MapScale;
            //float2 tz = worldpos.xy * _MapScale;
			
        }

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            //float3 bf = normalize(abs(IN.localNormal));
            //bf /= dot(bf, (float3)1);

            // Triplanar mapping
            //float2 tx = IN.localCoord.yz * _MapScale;
           // float2 ty = IN.localCoord.zx * _MapScale;
            //float2 tz = IN.localCoord.xy * _MapScale;

			float2 uvX = i.worldPos.zy;
			float2 uvY = i.worldPos.xz;
			float2 uvZ = i.worldPos.xy;
			
            // Base color
            half4 cx = tex2D(_MainTex, uvX) * i.bf.x;
            half4 cy = tex2D(_MainTex, uvY) * i.bf.y;
            half4 cz = tex2D(_MainTex, uvZ) * i.bf.z;
            half4 color = (cx + cy + cz) * _Color;
            
            o.Alpha = color.a;

        #ifdef _NORMALMAP
            // Normal map
            //half4 tnormalX = tex2D(_BumpMap, uvX) * i.bf.x;
            //half4 tnormalY = tex2D(_BumpMap, uvY) * i.bf.y;
            //half4 tnormalZ = tex2D(_BumpMap, uvZ) * i.bf.z;
			
			half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
			half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
			half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));
			
			//float border = 1 - saturate(i.bf.x * i.bf.x * i.bf.y * i.bf.y * i.bf.y * i.bf.y);
			
			//half3 axisSign = sign(i.myNormal);
			// Flip tangent normal z to account for surface normal facing
			//tnormalX.x *= axisSign.x * -1;
			//tnormalX.x *= axisSign.x ;
			//tnormalY.x *= axisSign.y;
			//tnormalY.y *= axisSign.y * -1;
			//tnormalZ.x *= axisSign.z * -1;
			//tnormalZ.y *= axisSign.z * -1;
			
			//tnormalX = half3(tnormalX.xy + i.myNormal.zy, i.myNormal.x);
			//tnormalY = half3(tnormalY.xy + i.myNormal.xz, i.myNormal.y);
			//tnormalZ = half3(tnormalZ.xy + i.myNormal.xy, i.myNormal.z);

            o.Normal = normalize(
				tnormalX.xyz * i.bf.x +
				tnormalY.xyz * i.bf.y +
				tnormalZ.xyz * i.bf.z
			);
			
			o.Albedo = color.rgb;
			
        #endif

        #ifdef _OCCLUSIONMAP
            // Occlusion map
            //half ox = tex2D(_OcclusionMap, i.tx).g * i.bf.x;
            //half oy = tex2D(_OcclusionMap, i.ty).g * i.bf.y;
            //half oz = tex2D(_OcclusionMap, i.tz).g * i.bf.z;
            //o.Occlusion = lerp((half4)1, ox + oy + oz, _OcclusionStrength);
        #endif

            // Misc parameters
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "StandardTriplanarInspector"
}
