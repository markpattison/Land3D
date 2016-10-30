texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; };

float Expose(float light)
{
	return (1.0 - exp(-1.0 * light));
}

float4 PixelShaderFunction(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float4 tex = tex2D(TextureSampler, texCoord);

	float4 output;
	output.r = Expose(tex.r);
	output.g = Expose(tex.g);
	output.b = Expose(tex.b);

	return output;
}

technique Plain
{
	pass Pass1
	{
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}

