//reuse//code//ApplyAnEffectFxFile//
uniform extern float4x4 WorldViewProj : WORLDVIEWPROJECTION;

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
    //float4 color : COLOR;
    float2 TexCoord : TEXCOORD0;
};


Texture tex1 : register(s1);
texture tex2 : register(s2);
uniform extern Texture tex3 : register(s3);
Texture2D<float> tex4 : register(s4);
Texture2D tex5 : register(s5);
texture2D tex6 : register(s6);

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
    
    // Store the input color for the pixel shader to use.
    //output.color = input.color;
    //output.color = input.Textoord;
    output.TexCoord = input.TexCoord;

    return output;
}

////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
float4 ColorPixelShader(PixelInputType input) : SV_Target
{
    return tex2D(UserTexSampler, input.TexCoord).rgba;
    //return UserTex.Sample(UserTexSampler, input.TexCoord);
}

////////////////////////////////////////////////////////////////////////////////
// Technique
////////////////////////////////////////////////////////////////////////////////
technique10 ColorTechnique
{
    pass pass0
    {
        SetVertexShader(CompileShader(vs_4_0, ColorVertexShader()));
        SetPixelShader(CompileShader(ps_4_0, ColorPixelShader()));
        SetGeometryShader(NULL);
    }
}

