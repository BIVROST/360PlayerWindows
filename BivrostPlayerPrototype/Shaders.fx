float4x4 WorldViewProjection;

struct VertexShaderColorPosition
{
	float4 position :POSITION;
	float4 color	:COLOR;
};

struct PixelShaderColorPosition
{
	float4 position :SV_POSITION;
	float4 color	:COLOR;
};

PixelShaderColorPosition VertexShaderPositionColor(VertexShaderColorPosition input)
{
	PixelShaderColorPosition output = (PixelShaderColorPosition) 0;
	
	output.position = mul(input.position, WorldViewProjection);
	output.color = input.color;
	
	return output;
}

float4 PixelShaderPositionColor(PixelShaderColorPosition input) :SV_Target
{
	return input.color;
}
