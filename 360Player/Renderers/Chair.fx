// based mostly on https://digitalerr0r.wordpress.com/2012/03/03/xna-4-0-shader-programming-4normal-mapping/

extern float4x4 worldViewProj : WORLDVIEWPROJECTION;
extern float4x4 world : WORLD;
extern float time;

SamplerState Sampler : register(s0);
Texture2D<float4> diffuseTex : register(t0);
Texture2D<float4> normalTex : register(t1);
Texture2D<float4> eqrTex : register(t2);

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
	float4 rawPos : TEXCOORD5;
	float3 normal : NORMAL;
	float4 color : COLOR;
	float2 uv : TEXCOORD0;
	float4 view : TEXCOORD1;
	float3x3 WorldToTangentSpace : TEXCOORD2;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	input.pos.w = 1;
	output.pos = mul(input.pos, worldViewProj);
	output.rawPos = mul(input.pos, world);
	output.color = input.color;
	output.uv = input.uv;
	output.WorldToTangentSpace[0] = mul(normalize(input.tangent), world);
	output.WorldToTangentSpace[1] = mul(normalize(input.binormal), world);
	output.WorldToTangentSpace[2] = mul(normalize(input.normal), world);
	output.normal = input.normal;
	output.view = normalize(-mul(input.pos, world));	// assumes camera at 0,0,0
	return output;
}





#define MPI_2 1.57079632679
#define MPI 3.14159265359
#define M1_2PI 0.15915494309
#define M1_PI 0.31830988618

float2 UnitVectorToEquirectangularTexCoord(float3 normal)
{
	//float pitch = -(normal.y-1)/2;	// approximate
	float pitch = 1 - (asin(normal.y) + MPI_2) * M1_PI;	// precise

	float yaw = (atan2(normal.x, -normal.z) + MPI) * M1_2PI;

	return float2(yaw, pitch);
}


//struct PointLight
//{
//	float3 position;
//	float3 diffuseColor;
//	float  diffusePower;
//	float3 specularColor;
//	float  specularPower;
//};

float4 PS(PS_IN input) : SV_Target
{
	float4 diffuseMap = diffuseTex.Sample(Sampler, input.uv);
	float3 normalMap = normalTex.Sample(Sampler, input.uv) * 2.0 - 1.0;

	float3 light = normalize(float3(1, -1, -1));	// left down forward
	light = mul(light, world);
	light = mul(light, world);

	// Light related
	float4 AmbientColor = float4(1, 1, 1, 1);
	float AmbientIntensity = 0.1;
	float4 DiffuseColor = float4(1,1,1,1);
	float DiffuseIntensity = 1;
	float4 SpecularColor = float4(1,1,1,1);
	float SpecularIntensity = 8;

	normalMap = normalize(mul(normalMap, input.WorldToTangentSpace));
	float4 normal = float4(normalMap, 1.0);
	//float4 normal = float4(normalize(input.normal), 1);

	float4 diffuse = saturate(dot(-light, normal));
	float4 reflect = normalize(2 * diffuse*normal - float4(light, 1.0));
	float4 specular = pow(saturate(dot(reflect, input.view)), SpecularIntensity);
	
//return normal;


	// reflection
	//float3 v = normalize(input.rawPos.xyz);
	//float3 reflection = v - 2 * dot(v, normal) * normal;
	//float2 TexCoord = UnitVectorToEquirectangularTexCoord(reflection);
	//float4 eqrMap = pow(eqrTex.Sample(Sampler, TexCoord), 2.2);
	//return eqrMap;

	// end refl

	return float4((
		diffuseMap * AmbientColor * AmbientIntensity +
		diffuseMap * DiffuseIntensity * DiffuseColor * diffuse +
		diffuseMap * SpecularColor*specular
	).xyz, 1);
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