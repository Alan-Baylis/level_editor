Shader "Unlit/BlobPost"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MainTexLow ("Texture Low", 2D) = "white" {}
		_Noise ("Noise", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _MainTexLow;
			sampler2D _Noise;
			float4 _MainTex_ST;
			float4 _Noise_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.zw = TRANSFORM_TEX(v.uv, _Noise);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 tex = tex2D(_MainTex, i.uv.xy);
				fixed4 texlow = tex2D(_MainTexLow, i.uv.xy);
				fixed noise = tex2D(_Noise, i.uv.zw);
				fixed4 col = lerp(tex,texlow,noise);

				return col;
			}
			ENDCG
		}
	}
}
