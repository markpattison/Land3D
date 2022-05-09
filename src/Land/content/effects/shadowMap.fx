float4x4 xWorld;
float4x4 xLightViewProjection;

struct SMapVertexToPixel
{
    float4 Position     : POSITION;
    float4 Position2D   : TEXCOORD0;
};

struct SMapPixelToFrame
{
    float Color : COLOR0;
};

SMapVertexToPixel ShadowMapVertexShader(float4 inPos : POSITION)
{
    float4x4 preLightWorldViewProjection = mul(xWorld, xLightViewProjection);
	
	SMapVertexToPixel Output = (SMapVertexToPixel)0;

    Output.Position = mul(inPos, preLightWorldViewProjection);
    Output.Position2D = Output.Position;

    return Output;
}

SMapPixelToFrame ShadowMapPixelShader(SMapVertexToPixel PSIn)
{
    SMapPixelToFrame Output = (SMapPixelToFrame)0;            

    Output.Color = PSIn.Position2D.z/PSIn.Position2D.w;

    return Output;
}

technique ShadowMap
{
    pass Pass0
    {
        VertexShader = compile vs_4_0 ShadowMapVertexShader();
        PixelShader = compile ps_4_0 ShadowMapPixelShader();
    }
}
