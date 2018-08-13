Shader "Cursor"
{
	Properties
	{
		_PositiveColor("Positive Color", Color) = (0,1,0,0.5)
		_NegativeColor("Negative Color", Color) = (1,0,0,0.5)
		_CursorColor("Curson Color", Range(0,1)) = 1.0
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "WorldDisplacement.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _PositiveColor;
			fixed4 _NegativeColor;
			fixed _CursorColor;
			float _CursorAlpha;
			
			v2f vert (appdata v)
			{
				v2f o;

				// CONVERT VERTEX TO WORLD SPACE
				float4 worldvertex = mul(unity_ObjectToWorld, v.vertex);

				// DISTORT VERTEX IN WORLD SPACE
				float4 vertPosition = getNewVertPosition(worldvertex);

				// CONVERT VERTEX BACK TO OBJECT SPACE
				v.vertex = mul(unity_WorldToObject, vertPosition);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = lerp(_PositiveColor, _NegativeColor, _CursorColor) * 2;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed alpha = tex2D(_MainTex, i.uv).a * _CursorAlpha;
				fixed4 col = fixed4(i.color.r, i.color.g, i.color.b, alpha);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
