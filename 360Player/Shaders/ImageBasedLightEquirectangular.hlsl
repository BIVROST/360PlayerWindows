//reuse//code//ApplyAnEffectFxFile//
uniform extern float4x4 WorldViewProj : WORLDVIEWPROJECTION;
uniform extern float4x4 World : WORLD;
extern float gammaFactor;

/////////////
// GLOBALS //
/////////////
//matrix worldMatrix;
//matrix viewMatrix;
//matrix projectionMatrix;

//float4 AmbientColor = float4(1, 1, 1, 1);

//////////////
// TYPEDEFS //
//////////////
struct VertexInputType
{
    float4 Position : SV_Position;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PixelInputType
{
	float4 position : SV_POSITION;
	float4 rawPosition : TEXCOORD1;
	//float4 color : COLOR;
    float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL;
};

Texture2D<float4> UserTex : register(t0);
SamplerState UserTexSampler : register(s0);


////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
PixelInputType ColorVertexShader(VertexInputType input)
{
    PixelInputType output;
    
    
    // Change the position vector to be 4 units for proper matrix calculations.
    input.Position.w = 1.0f;

    // Calculate the position of the vertex against the world, view, and projection matrices.
    //output.position = mul(input.position, worldMatrix);
    //output.position = mul(output.position, viewMatrix);
    //output.position = mul(output.position, projectionMatrix);
    output.position = mul(input.Position, WorldViewProj);
	output.rawPosition = mul(input.Position, World);
    
    // Store the input color for the pixel shader to use.
    //output.color = input.color;
    //output.color = input.Textoord;
    output.TexCoord = input.TexCoord;

	output.Normal = input.Normal;

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


////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
float4 ColorPixelShaderReflection(PixelInputType input) : SV_Target
{
	float3 normal = input.Normal;
	float3 v = normalize(input.rawPosition.xyz);
	float3 reflection = v - 2 * dot(v, normal) * normal;
	float2 TexCoord = UnitVectorToEquirectangularTexCoord(reflection);
	return pow(UserTex.Sample(UserTexSampler, TexCoord), gammaFactor);
}

// TODO:
//float imageBasedLight = saturate(normal.y);
//if (imageBasedLight > .8) imageBasedLight = 1;
//else imageBasedLight = 0;


float4 ColorPixelShaderDiffusion(PixelInputType input) : SV_Target
{
	float3 normal = input.Normal;
	float2 TexCoord = UnitVectorToEquirectangularTexCoord(normal);
	return pow(UserTex.Sample(UserTexSampler, TexCoord), gammaFactor);
}



////////////////////////////////////////////////////////////////////////////////
// Technique
////////////////////////////////////////////////////////////////////////////////
technique10 ColorTechniqueReflection
{
	pass pass0
	{
		SetVertexShader(CompileShader(vs_4_0, ColorVertexShader()));
		SetPixelShader(CompileShader(ps_4_0, ColorPixelShaderReflection()));
		SetGeometryShader(NULL);
	}
}

technique10 ColorTechnique	// Diffusion
{
	pass pass0
	{
		SetVertexShader(CompileShader(vs_4_0, ColorVertexShader()));
		SetPixelShader(CompileShader(ps_4_0, ColorPixelShaderDiffusion()));
		SetGeometryShader(NULL);
	}
}

