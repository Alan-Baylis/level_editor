
float _WorldX;
float _WorldY;
float _WorldZ;
float _WaveAmount;
float _WaveDistance;
float _WaveSin;

float4 getNewVertPosition(float4 p)
{
	float distortscale = 20.0f;
	float distortamount = 0.3f;

	float3 sin1 = sin(p.xyz * distortscale) * distortamount * 0.02;
	float3 sin2 = sin(p.xyz * distortscale * 0.45) * distortamount * 0.04;
	float3 sin3 = sin(p.xyz * distortscale * 0.33) * distortamount * 0.01;

	p.xyz += sin1.x + sin1.y + sin1.z;
	p.xyz += sin2.x + sin2.y + sin2.z;
	p.xyz += sin3.x + sin3.y + sin3.z;

	float vertdistance = distance(p.xyz, float3(_WorldX, _WorldY, _WorldZ));

	float vertmask = saturate(_WaveDistance - vertdistance);

	float vertsin = sin(vertdistance * 6 + _WaveSin);

	p.xyz = lerp(p.xyz, float4(_WorldX, _WorldY, _WorldZ, 1), _WaveAmount * vertmask * vertsin);

	return p;
}
