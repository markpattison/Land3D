// GroundFromAtmosphere

// constants
float4x4 xView;
float4x4 xReflectionView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float4 xClipPlane;
float xAmbient;
float3 xCameraPosition;

float xWaterOpacity;
float xTime;
float xWindForce;
float2 xWindDirection;
float xWaveLength;
float xWaveHeight;
float xPerlinSize3D;

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

Texture xReflectionMap;
sampler ReflectionSampler = sampler_state { texture = <xReflectionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xRefractionMap;
sampler RefractionSampler = sampler_state { texture = <xRefractionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xRandomTexture3D;
sampler RandomTextureSampler3D = sampler_state { texture = <xRandomTexture3D>; AddressU = WRAP; AddressV = WRAP; AddressW = WRAP; };

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
	float3 WorldPosition: TEXCOORD5;
};

struct PixelToFrame
{
	float4 Color : COLOR0;
};

float Perlin3D(float3 pIn)
{
	float3 p = pIn * xPerlinSize3D;

	float3 posAAA = floor(p);
	float3 t = p - posAAA;

	float3 posBAA = posAAA + float3(1.0, 0.0, 0.0);
	float3 posABA = posAAA + float3(0.0, 1.0, 0.0);
	float3 posBBA = posAAA + float3(1.0, 1.0, 0.0);
	float3 posAAB = posAAA + float3(0.0, 0.0, 1.0);
	float3 posBAB = posAAA + float3(1.0, 0.0, 1.0);
	float3 posABB = posAAA + float3(0.0, 1.0, 1.0);
	float3 posBBB = posAAA + float3(1.0, 1.0, 1.0);

	float3 colAAA = tex3D(RandomTextureSampler3D, posAAA / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colBAA = tex3D(RandomTextureSampler3D, posBAA / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colABA = tex3D(RandomTextureSampler3D, posABA / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colBBA = tex3D(RandomTextureSampler3D, posBBA / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colAAB = tex3D(RandomTextureSampler3D, posAAB / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colBAB = tex3D(RandomTextureSampler3D, posBAB / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colABB = tex3D(RandomTextureSampler3D, posABB / xPerlinSize3D).xyz * 2.0 - 1.0;
	float3 colBBB = tex3D(RandomTextureSampler3D, posBBB / xPerlinSize3D).xyz * 2.0 - 1.0;

	float sAAA = mul(colAAA, p - posAAA);
	float sBAA = mul(colBAA, p - posBAA);
	float sABA = mul(colABA, p - posABA);
	float sBBA = mul(colBBA, p - posBBA);
	float sAAB = mul(colAAB, p - posAAB);
	float sBAB = mul(colBAB, p - posBAB);
	float sABB = mul(colABB, p - posABB);
	float sBBB = mul(colBBB, p - posBBB);

	//float3 s = t * t * (3 - 2 * t);
	float3 s = t * t * t * (t * (t * 6 - 15) + 10);

	float sPAA = sAAA + s.x * (sBAA - sAAA);
	float sPAB = sAAB + s.x * (sBAB - sAAB);
	float sPBA = sABA + s.x * (sBBA - sABA);
	float sPBB = sABB + s.x * (sBBB - sABB);

	float sPPA = sPAA + s.y * (sPBA - sPAA);
	float sPPB = sPAB + s.y * (sPBB - sPAB);

	float sPPP = sPPA + s.z * (sPPB - sPPA);
	return sPPP;
}

struct ScatteringResult
{
	float3 ScatteringColour;
	float3 Attenuation;
};

float scale(float cos)
{
	float x = 1.0 - cos;
	return xScaleDepth * exp(-0.00287 + x * (0.459 + x * (3.83 + x  *(-6.80 + x * 5.25))));
}

ScatteringResult Scattering(float3 worldPosition)
{
	ScatteringResult output = (ScatteringResult)0;

	float3 cameraInPlanetSpace = xCameraPosition + float3(0.0, xInnerRadius, 0.0);
	float3 vertexInPlanetSpace = worldPosition + float3(0.0, xInnerRadius, 0.0);

	float3 viewDirection = normalize(vertexInPlanetSpace - cameraInPlanetSpace);
	float distanceToVertex = length(vertexInPlanetSpace - cameraInPlanetSpace);
	float vertexHeight = length(vertexInPlanetSpace);
	float cameraHeight = length(cameraInPlanetSpace);
	float startDepth = exp((xInnerRadius - cameraHeight) * xScaleOverScaleDepth);

	float vertexHigher = (vertexHeight > cameraHeight) ? -1.0 : 1.0;

	float cameraAngle = vertexHigher * dot(-viewDirection, vertexInPlanetSpace) / vertexHeight;
	float lightAngle = -dot(xLightDirection, vertexInPlanetSpace) / vertexHeight;
	float cameraScale = vertexHigher * scale(cameraAngle);
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

GroundFromAtmosphere_VertexToPixel GroundFromAtmosphereVS(GroundFromAtmosphere_ToVertex VSInput)
{
	GroundFromAtmosphere_VertexToPixel output = (GroundFromAtmosphere_VertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	float4 worldPosition = mul(VSInput.Position, xWorld);
	output.WorldPosition = worldPosition.xyz;
	output.Position = mul(VSInput.Position, preWorldViewProjection);
	output.TextureCoords = VSInput.TexCoords;

	float3 normal = normalize(mul(float4(normalize(VSInput.Normal), 0.0), xWorld)).xyz;

	output.LightingFactor = dot(normal, -xLightDirection);

	output.ClipDistance = dot(worldPosition, xClipPlane);
	output.Depth = output.Position.z / output.Position.w;

	ScatteringResult scattering = Scattering(worldPosition.xyz);

	output.ScatteringColour = scattering.ScatteringColour;
	output.Attenuation = scattering.Attenuation;

	return output;
}

GroundFromAtmosphere_VertexToPixel GroundFromAtmosphereVSOld(GroundFromAtmosphere_ToVertex VSInput)
{
	GroundFromAtmosphere_VertexToPixel output = (GroundFromAtmosphere_VertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	float4 worldPosition = mul(VSInput.Position, xWorld);
	output.WorldPosition = worldPosition.xyz;
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
	float vertexHeight = length(vertexInPlanetSpace);
	float cameraHeight = length(cameraInPlanetSpace);
	float startDepth = exp((xInnerRadius - cameraHeight) * xScaleOverScaleDepth);

	float vertexHigher = (vertexHeight > cameraHeight) ? -1.0 : 1.0;

	float cameraAngle = vertexHigher * dot(-viewDirection, vertexInPlanetSpace) / vertexHeight;
	float lightAngle = -dot(xLightDirection, vertexInPlanetSpace) / vertexHeight;
	float cameraScale = vertexHigher * scale(cameraAngle);
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
	
	// water depth
	float distanceUnderwater = PSInput.WorldPosition.y >= 0.0 ? 0.0 : length(PSInput.WorldPosition.xyz - xCameraPosition) * PSInput.WorldPosition.y / (PSInput.WorldPosition.y - xCameraPosition.y);
	float4 dullColor = float4(0.0, 0.0, 0.0, 1.0);
	output.Color = lerp(output.Color, dullColor, 1.0 - exp(-distanceUnderwater * xWaterOpacity));

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

struct WVertexToPixel
{
	float4 Position                 : SV_POSITION;
	float4 ReflectionMapSamplingPos    : TEXCOORD1;
	float3 BumpMapSamplingPos        : TEXCOORD2;
	float4 RefractionMapSamplingPos : TEXCOORD3;
	float3 Position3D                : TEXCOORD4;
};

struct WPixelToFrame
{
	float4 Color : COLOR0;
};

WVertexToPixel WaterVS(float4 inPos : SV_POSITION, float2 inTex : TEXCOORD)
{
	WVertexToPixel Output = (WVertexToPixel)0;

	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);
	float4x4 preReflectionViewProjection = mul(xReflectionView, xProjection);
	float4x4 preWorldReflectionViewProjection = mul(xWorld, preReflectionViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	float2 moveVector = xTime * xWindForce * xWindDirection;
	Output.BumpMapSamplingPos.xz = (inTex.xy + moveVector.xy) / xWaveLength;
	Output.BumpMapSamplingPos.y = xTime / 100.0f;
	Output.ReflectionMapSamplingPos = mul(inPos, preWorldReflectionViewProjection);
	Output.RefractionMapSamplingPos = mul(inPos, preWorldViewProjection);
	Output.Position3D = mul(inPos, xWorld).xyz;

	return Output;
}

float PerlinWater(float3 pos)
{
	float3 offset1 = float3(0.1f, 0.6f, 0.3f);
	float3 offset2 = float3(0.45f, 0.17f, 0.88f);
	float3 offset3 = float3(0.83f, 0.44f, 0.09f);
	return 0.25f * Perlin3D(pos) + 0.25f * Perlin3D((pos + offset1) / 1.1f) + 0.25f * Perlin3D((pos + offset2) / 1.25f) + 0.25f * Perlin3D((pos + offset3) / 0.1f);
}

WPixelToFrame WaterPS(WVertexToPixel PSIn)
{
	WPixelToFrame Output = (WPixelToFrame)0;

	//float4 bumpColor = tex2D(WaterBumpMapSampler, PSIn.BumpMapSamplingPos);
	//float2 perturbation = xWaveHeight * (bumpColor.rg - 0.5f)*2.0f;
	//float3 normalVector = (bumpColor.rbg - 0.5f) * 2.0f;

	float epsilon = 1.0f;

	float3 pX = PSIn.BumpMapSamplingPos;
	pX.x += epsilon;
	float3 pZ = PSIn.BumpMapSamplingPos;
	pZ.z += epsilon;

	float noiseScale = 0.1f;

	float noise = PerlinWater(PSIn.BumpMapSamplingPos) * noiseScale;
	float3 dNoise = float3(PerlinWater(pX) * noiseScale - noise, 0.0f, PerlinWater(pZ) * noiseScale - noise);
	float3 modNormal = float3(dNoise.x, epsilon, dNoise.z);
	float3 normalVector = normalize(modNormal);
	float2 perturbation = xWaveHeight * normalVector.xz;

	float2 projectedReflTexCoords;
	projectedReflTexCoords.x = PSIn.ReflectionMapSamplingPos.x / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
	projectedReflTexCoords.y = -PSIn.ReflectionMapSamplingPos.y / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
	float2 perturbatedTexCoords = projectedReflTexCoords + perturbation;
	float4 reflectiveColor = tex2D(ReflectionSampler, perturbatedTexCoords);

	float2 projectedRefrTexCoords;
	projectedRefrTexCoords.x = PSIn.RefractionMapSamplingPos.x / PSIn.RefractionMapSamplingPos.w / 2.0f + 0.5f;
	projectedRefrTexCoords.y = -PSIn.RefractionMapSamplingPos.y / PSIn.RefractionMapSamplingPos.w / 2.0f + 0.5f;
	float2 perturbatedRefrTexCoords = projectedRefrTexCoords + perturbation;
	float4 refractiveColorPerturb = tex2D(RefractionSampler, perturbatedRefrTexCoords);
	float4 refractiveColorNoPerturb = tex2D(RefractionSampler, projectedRefrTexCoords);
	float alpha = refractiveColorPerturb.a;
	float4 refractiveColor = lerp(refractiveColorNoPerturb, refractiveColorPerturb, alpha);

	float3 eyeVector = normalize(xCameraPosition - PSIn.Position3D);

	// Schlick's approximation
	float AirIOR = 1.0;
	float WaterIOR = 1.33;
	float R0 = (AirIOR - WaterIOR) / (AirIOR + WaterIOR);
	R0 *= R0;

	float fresnelTerm = R0 + (1.0 - R0) * pow(1.0 - dot(eyeVector, normalVector), 5.0);

	float3 reflectionVector = reflect(xLightDirection, normalVector);
	float specular = max(0.0f, dot(normalize(reflectionVector), normalize(eyeVector)));
	specular = pow(specular, 1024);

	float4 combinedColor = lerp(reflectiveColor, refractiveColor, fresnelTerm);
	float4 dullColor = float4(0.3f, 0.35f, 0.45f, 1.0f);
	Output.Color = lerp(combinedColor, dullColor, 0.0f);
	Output.Color.rgb += specular;

	//Output.Color = lerp(Output.Color, float4(400.0f * noise, 0.0f, 0.0f, 1.0f), 0.999f);
	return Output;
}

technique Water
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 WaterVS();
		PixelShader = compile ps_4_0 WaterPS();
	}
}