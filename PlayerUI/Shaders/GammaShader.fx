//reuse//code//ApplyAnEffectFxFile//
uniform extern float4x4 WorldViewProj : WORLDVIEWPROJECTION;
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
    //float4 color : COLOR;
    float2 TexCoord : TEXCOORD0;
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
    
    // Store the input color for the pixel shader to use.
    //output.color = input.color;
    //output.color = input.Textoord;
    output.TexCoord = input.TexCoord;

    return output;
}

// http://gamedev.stackexchange.com/a/32688/44395
float2 rand_2_10(in float2 uv)
{
    float noiseX = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
    float noiseY = sqrt(1 - noiseX * noiseX);
    return float2(noiseX, noiseY);
}
float rand_1_05(in float2 uv)
{
    float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
    return abs(noise.x + noise.y) * 0.5;
}



float Pixels[13] =
{
    -6,
   -5,
   -4,
   -3,
   -2,
   -1,
    0,
    1,
    2,
    3,
    4,
    5,
    6,
};

float BlurWeights[13] =
{
    0.002216,
   0.008764,
   0.026995,
   0.064759,
   0.120985,
   0.176033,
   0.199471,
   0.176033,
   0.120985,
   0.064759,
   0.026995,
   0.008764,
   0.002216,
};

////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
float4 ColorPixelShader(PixelInputType input) : SV_Target
{
    if(input.TexCoord.x > 1 || input.TexCoord.x < 0)
    {
        float dx = input.TexCoord.x < 0 ? -input.TexCoord.x : input.TexCoord.x - 1;
        float dy = min(input.TexCoord.y, 1 - input.TexCoord.y);
        float d = min(dy, dx) * 2;

        // Pixel width
        float pixelWidth = 1.0 / (float) 512;

        float4 color = { 0, 0, 0, 1 };

        float2 blur;
        blur.x = input.TexCoord.x;

        for (int i = 0; i < 13; i++)
        {
            blur.y = input.TexCoord.y + Pixels[i] * pixelWidth * pow(1.5 - d, 0.5);
            color += UserTex.Sample(UserTexSampler, blur) * BlurWeights[i];
        }

        // top cap
        if (input.TexCoord.y < 0.333)
        {
            color = lerp(color, UserTex.Sample(UserTexSampler, float2(0.5, 0.02)), 1 - input.TexCoord.y * 3);
        }

        // bottom cap
        else if (input.TexCoord.y > 0.666)
        {
            color = lerp(color, UserTex.Sample(UserTexSampler, float2(0.5, 0.98)), 1 - (1 - input.TexCoord.y) * 3);
        }

        // merge in center
        if (input.TexCoord.x > 1.4)
        {
            float t = 10 * (input.TexCoord.x - 1.4);
            color = lerp(color, UserTex.Sample(UserTexSampler, float2(0.5, input.TexCoord.y < 0.5 ? 0.02 : 0.98)), t);
        }
        if (input.TexCoord.x < -0.4)
        {
            float t = 10 * -(input.TexCoord.x + 0.4);
            color = lerp(color, UserTex.Sample(UserTexSampler, float2(0.5, input.TexCoord.y < 0.5 ? 0.02 : 0.98)), t);
        }

        
        color = pow(pow(0.5 - d, 0.7) * color, gammaFactor);
        color.a = 1;
        return color;
    }


    return pow(UserTex.Sample(UserTexSampler, input.TexCoord), gammaFactor);
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

