// GroundFromAtmosphere

// constants
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float4 xClipPlane;
float xAmbient;
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

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

struct GroundFromAtmosphere_ToVertex
{
	float4 Position : SV_POSITION;
	float3 Normal : NORMAL;
	float2 TexCoords : TEXCOORD0;
};

struct GroundFromAtmosphere_VertexToPixel
{
	float4 Position : SV_POSITION;
	float3 ScatteringColour : COLOR0;
	float3 Attenuation : COLOR1;
	float LightingFactor : TEXCOORD0;
	float2 TextureCoords : TEXCOORD1;
	float ClipDistance : TEXCOORD2;
	float Depth : TEXCOORD4;
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

GroundFromAtmosphere_VertexToPixel GroundFromAtmosphereVS(GroundFromAtmosphere_ToVertex VSInput)
{
	GroundFromAtmosphere_VertexToPixel output = (GroundFromAtmosphere_VertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	float4 worldPosition = mul(VSInput.Position, xWorld);
	output.Position = mul(VSInput.Position, preWorldViewProjection);
	output.TextureCoords = VSInput.TexCoords;

	float3 normal = normalize(mul(float4(normalize(VSInput.Normal), 0.0), xWorld)).xyz;

	output.LightingFactor = dot(normal, -xLightDirection);

	output.ClipDistance = dot(worldPosition, xClipPlane);
	output.Depth = output.Position.z / output.Position.w;

	float3 cameraInPlanetSpace = xCameraPosition + float3(0.0, xInnerRadius, 0.0);
	float3 vertexInPlanetSpace = worldPosition.xyz + float3(0.0, xInnerRadius, 0.0);

	float3 viewDirection = normalize(vertexInPlanetSpace - cameraInPlanetSpace);
	float distanceToVertex = length(vertexInPlanetSpace - cameraInPlanetSpace);
	float cameraHeight = length(cameraInPlanetSpace);
	float startDepth = exp((xInnerRadius - cameraHeight) * xScaleOverScaleDepth);

	float cameraAngle = dot(-viewDirection, vertexInPlanetSpace) / length(vertexInPlanetSpace);
	float lightAngle = -dot(xLightDirection, vertexInPlanetSpace) / length(vertexInPlanetSpace);
	float cameraScale = scale(cameraAngle);
	float lightScale = scale(lightAngle);
	float cameraOffset = startDepth * cameraScale;
	float totalScale = lightScale + cameraScale;

	float sampleLength = distanceToVertex / xSamples;
	float scaledLength = sampleLength * xScale;
	float3 sampleRay = viewDirection * sampleLength;
	float3 samplePoint = cameraInPlanetSpace + sampleRay * 0.5;
	float3 attenuate;

	float3 accumulatedColour = float3(0.0, 0.0, 0.0);
	for (int i = 0; i < xSamples; i++)
	{
		float height = length(samplePoint);
		float depth = exp(xScaleOverScaleDepth * (xInnerRadius - height));
		float scatter = depth * totalScale - cameraOffset;
		attenuate = exp(-scatter * (xInvWavelength4 * xKr4Pi + xKm4Pi));

		accumulatedColour += attenuate * (depth * scaledLength);
		samplePoint += sampleRay;
	}

	float finalHeight = length(vertexInPlanetSpace);
	float finalDepth = exp(xScaleOverScaleDepth * (xInnerRadius - finalHeight));
	float finalScatter = finalDepth * totalScale - cameraOffset;
	float3 finalAttenuate = exp(-finalScatter * (xInvWavelength4 * xKr4Pi + xKm4Pi));

	output.ScatteringColour = accumulatedColour * (xInvWavelength4 * xKrESun + xKmESun);
	output.Attenuation = finalAttenuate;

	return output;
}

PixelToFrame GroundFromAtmospherePS(GroundFromAtmosphere_VertexToPixel PSInput)
{
	clip(PSInput.ClipDistance);

	PixelToFrame output = (PixelToFrame)0;

	float4 farColour = tex2D(TextureSampler, PSInput.TextureCoords);
	float4 nearColour = tex2D(TextureSampler, PSInput.TextureCoords * 3.0);
	float blendFactor = clamp((PSInput.Depth - 0.95) / 0.05, 0, 1);

	output.Color = lerp(nearColour, farColour, blendFactor);
	output.Color.rgb *= saturate(PSInput.LightingFactor) + xAmbient;
	output.Color.rgb *= PSInput.Attenuation;
	output.Color.rgb += PSInput.ScatteringColour;
	
	return output;
}

technique GroundFromAtmosphere
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 GroundFromAtmosphereVS();
		PixelShader = compile ps_4_0 GroundFromAtmospherePS();
	}
}