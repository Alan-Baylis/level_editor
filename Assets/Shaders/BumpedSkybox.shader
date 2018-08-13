// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "World Normal Bumped" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 300

CGPROGRAM
#pragma surface surf SimpleLambert

struct SurfaceOutputCustom {
    fixed3 Albedo;
	fixed3 Normal;
    half3 NormalCustom;
    fixed3 Emission;
    half Specular;
    fixed Gloss;
    fixed Alpha;
    fixed Intensity;
};

half4 LightingSimpleLambert (SurfaceOutputCustom s, half3 lightDir, half atten) {

half3 normal = s.NormalCustom;

    half NdotL = dot (normal, lightDir);
    half4 c;
    c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
    c.a = 1;//s.Alpha;
    
	//return half4(normal,1);
	
	return c;
}

sampler2D _MainTex;
sampler2D _BumpMap;
fixed4 _Color;

      struct Input {
          float2 uv_MainTex;
          float3 customColor;
      };

fixed3 myUnpackNormal(half4 packednormal)
{
	return packednormal.xyz * 2 - 1;
}
	  

void surf (Input IN, inout SurfaceOutputCustom o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb;
    o.NormalCustom = myUnpackNormal(tex2D(_BumpMap, IN.uv_MainTex)).xyz;
	//o.NormalCustom = tex2D(_BumpMap, IN.uv_MainTex).xyz;
    //o.Normal = half3(0,0,1);//UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	//o.Emission = o.NormalCustom;
}
ENDCG
}

FallBack "Legacy Shaders/Diffuse"
}
