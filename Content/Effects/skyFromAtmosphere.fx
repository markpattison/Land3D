// SkyFromAtmosphere

//------- Constants --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float3 xCameraPosition;

float xG;
float xGSquared;
float3 xInvWavelength4;
float xKrESun;
float xKmESun;
float xKr4Pi;
float xKm4Pi;

float xInnerRadius;
float xOuterRadius;
float xOuterRadiusSquared;
float xScale;
float xScaleDepth;
float xScaleOverScaleDepth;

int xSamples = 4;

struct SkyFromAtmosphere_ToVertex
{
	float4 Position : SV_POSITION;
};

struct SkyFromAtmosphere_VertexToPixel
{
	float4 Position : SV_POSITION;
	float3 RayleighColour : COLOR0;
	float3 MieColour : COLOR1;
	float3 ViewDirection : TEXCOORD0;
};

struct PixelToFrame
{
	float4 Color : COLOR0;
};

float scale(float cos)
{
	float x = 1.0 - cos;
	return xScaleDepth * exp(-0.00287 + x * (0.459 + x * (3.83 + x  *(-6.80 + x * 5.25))));
}

float OuterAtmosphereIntersection(float3 cameraInPlanetSpace, float3 viewDirection)
{
	float a = 1.0;
	float b = 2.0 * dot(viewDirection, cameraInPlanetSpace);
	float c = dot(cameraInPlanetSpace, cameraInPlanetSpace) - xOuterRadiusSquared;

	float det = b * b - 4.0 * a * c;

	// biggest solution

	return (-b + sqrt(det)) / (2.0 * a);
}

SkyFromAtmosphere_VertexToPixel SkyFromAtmosphereVS(SkyFromAtmosphere_ToVertex VSInput)
{
	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	SkyFromAtmosphere_VertexToPixel output = (SkyFromAtmosphere_VertexToPixel)0;

	float4 worldPosition = mul(VSInput.Position, xWorld);
	float3 viewDirection = normalize(worldPosition.xyz - xCameraPosition);

	float3 cameraInPlanetSpace = xCameraPosition + float3(0.0, xInnerRadius, 0.0);

	float distanceToOuterAtmosphere = OuterAtmosphereIntersection(cameraInPlanetSpace, viewDirection);
	float3 intersectsOuterAtmosphere = xCameraPosition + distanceToOuterAtmosphere * viewDirection;

	float cameraHeight = length(cameraInPlanetSpace);
	float opticalDepth = exp(xScaleOverScaleDepth * (xInnerRadius - cameraHeight));
	float startAngle = dot(viewDirection, cameraInPlanetSpace) / cameraHeight;
	float startOffset = opticalDepth * scale(startAngle);

	float sampleLength = distanceToOuterAtmosphere / float(xSamples);
	float scaledLength = sampleLength * xScale;
	float3 sampleRay = viewDirection * sampleLength;
	float3 samplePoint = cameraInPlanetSpace + sampleRay * 0.5;
	float3 accumulatedColour = float3(0.0, 0.0, 0.0);

	for (int i = 0; i < xSamples; i++)
	{
		float sampleHeight = length(samplePoint);
		float depth = exp(xScaleOverScaleDepth * (xInnerRadius - sampleHeight));
		float lightAngle = -dot(xLightDirection, samplePoint) / sampleHeight;
		float cameraAngle = dot(viewDirection, samplePoint) / sampleHeight;
		float scatter = startOffset + depth * (scale(lightAngle) - scale(cameraAngle));
		float3 attenuate = exp(-scatter * (xInvWavelength4 * xKr4Pi + xKm4Pi));

		accumulatedColour += attenuate * (depth * scaledLength);
		samplePoint += sampleRay;
	}

	output.Position = mul(VSInput.Position, preWorldViewProjection);
	output.RayleighColour = accumulatedColour * (xInvWavelength4 * xKrESun);
	output.MieColour = accumulatedColour * xKmESun;
	output.ViewDirection = viewDirection;

	return output;
}

PixelToFrame SkyFromAtmospherePS(SkyFromAtmosphere_VertexToPixel PSInput)
{
	PixelToFrame Output = (PixelToFrame)0;

	float cos = dot(xLightDirection, PSInput.ViewDirection);
	float miePhase = 1.5 * ((1.0 - xGSquared) / (2.0 + xGSquared)) * (1.0 + cos * cos) / pow(1.0 + xGSquared - 2.0 * xG * cos, 1.5);

	Output.Color.rgb = PSInput.RayleighColour + miePhase * PSInput.MieColour;
	
	Output.Color.a = Output.Color.b;

	return Output;
}

technique SkyFromAtmosphere
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 SkyFromAtmosphereVS();
		PixelShader = compile ps_4_0 SkyFromAtmospherePS();
	}
}