extern float4x4 worldViewProj : WORLDVIEWPROJECTION;
extern float4x4 world : WORLD;
extern float time;

SamplerState Sampler : register(s0);
Texture2D<float4> diffuseTex : register(t0);
Texture2D<float4> normalTex : register(t1);

struct VS_IN
{
	float4 pos : POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	input.pos.w = 1;
	output.pos = mul(input.pos, worldViewProj);
	output.color = input.color;
	output.normal = input.normal;
	output.uv = input.uv;
	output.worldPos = mul(input.pos, world).xyz;


	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	//return float4(normalize(mul(input.normal, world)).xyz, 1);



	float4 diffuse = diffuseTex.Sample(Sampler, input.uv);
	float4 normalMap = normalTex.Sample(Sampler, input.uv) * 2 - float4(1,1,1,1);
	normalMap=normalize(normalMap); 

	//float3 DirToLight = normalize(float3(0, 1, 0));
	//float3 DirLightColor = float3(1, 1, 1);

	//float NDotL = dot(DirToLight, normal);

	//float3 EyePosition = float3(0, 0, 0);

	//float specExp = 0.5f;

	//float3 finalColor = DirLightColor.rgb * saturate(NDotL);
	//// calculate specular light and add to diffuse
	//float3 toEye = EyePosition.xyz - input.pos;
	//toEye = normalize(toEye);
	//float3 halfway = normalize(toEye + DirToLight);
	//float NDotH = saturate(dot(halfway, normal));
	//finalColor += DirLightColor.rgb * pow(NDotH, specExp);
	//// scale light color by material color
	//return float4(finalColor * diffuse.rgb, 1);

	//return float4(input.normal, 1);





	//return float4(input.normal, 1);

	float3 normal = normalMap.xyz + input.normal.xyz; 

	//return float4(normalMap.xyz + input.normal.xyz, 1);


	// right, up, 
	float3 light = normalize(float3(0,0,-1));

	//light = light * sin(time);

	//return float4(light,1);

	light = normalize(mul(light, world) - mul(float4(0, 0, 0, 0), world)) ;

	//light = mul(light, worldViewProj); inverse?
	//float4 normalL = saturate(mul(normal, world));

	float brightness = dot(light, normal);

	//return normal;
	//return normal;
	return float4(brightness, brightness, brightness, 1);
	//return diffuse; // *brightness * input.color;
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