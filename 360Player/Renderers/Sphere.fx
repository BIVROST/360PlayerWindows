uniform extern float4x4 worldViewProj : WORLDVIEWPROJECTION;
uniform extern float4x4 world : WORLD;
uniform float time;

SamplerState Sampler : register(s0);
Texture2D<float4> diffuseTex : register(t0);


struct VS_IN
{
	float4 pos : POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	float3 tangent : TANGENT0;
	float3 binormal : BINORMAL0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	//float4 color : COLOR;
	float2 uv : TEXCOORD0;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	input.pos.w = 1;
	output.pos = mul(input.pos, worldViewProj);
	//output.pos = input.pos;
	//output.color = float4(input.color, 1);
	output.uv = input.uv;
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return diffuseTex.Sample(Sampler, input.uv);
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}