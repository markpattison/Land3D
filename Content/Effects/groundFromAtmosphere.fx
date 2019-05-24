// GroundFromAtmosphere

// constants
float4x4 xView;
float4x4 xReflectionView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float4x4 xLightsViewProjection;
float4 xClipPlane;
float xAmbient;
float3 xCameraPosition;
float2 xMinMaxHeight;

float xWaterOpacity;
float xTime;
float xWindForce;
float2 xWindDirection;
float xWaveLength;
float xWaveHeight;
float xPerlinSize3D;
bool xAlphaAfterWaterDepthWeighting;

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

texture xGrassTexture;
sampler GrassTextureSampler = sampler_state
{
    texture = <xGrassTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = mirror;
    AddressV = mirror;
};

texture xRockTexture;
sampler RockTextureSampler = sampler_state
{
    texture = <xRockTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = mirror;
    AddressV = mirror;
};

texture xSandTexture;
sampler SandTextureSampler = sampler_state
{
    texture = <xSandTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = mirror;
    AddressV = mirror;
};

texture xSnowTexture;
sampler SnowTextureSampler = sampler_state
{
    texture = <xSnowTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = mirror;
    AddressV = mirror;
};

texture xReflectionMap;
sampler ReflectionSampler = sampler_state { texture = <xReflectionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

texture xRefractionMap;
sampler RefractionSampler = sampler_state { texture = <xRefractionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

texture xRandomTexture3D;
sampler RandomTextureSampler3D = sampler_state { texture = <xRandomTexture3D>; AddressU = WRAP; AddressV = WRAP; AddressW = WRAP; };

texture xShadowMap;
sampler ShadowMapSampler = sampler_state { texture = <xShadowMap> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = clamp; AddressV = clamp;};

struct GroundFromAtmosphere_ToVertex
{
	float4 Position : SV_POSITION;
	float3 Normal : NORMAL;
	float2 TexCoords : TEXCOORD0;
};

struct GroundFromAtmosphere_VertexToPixel
{
	float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
	float3 ScatteringColour : COLOR0;
	float3 Attenuation : COLOR1;
	float2 TextureCoords : TEXCOORD1;
	float ClipDistance : TEXCOORD2;
	float Depth : TEXCOORD4;
	float3 WorldPosition: TEXCOORD5;
	float4 PositionFromLight: TEXCOORD6;
};

struct PixelToFrame
{
	float4 Color : COLOR0;
};

float Perlin3D(float3 pIn)
{
	float3 p = (pIn + 0.5) * xPerlinSize3D;

	float3 posAAA = floor(p);
	float3 t = p - posAAA;

	float3 posBAA = posAAA + float3(1.0, 0.0, 0.0);
	float3 posABA = posAAA + float3(0.0, 1.0, 0.0);
	float3 posBBA = posAAA + float3(1.0, 1.0, 0.0);
	float3 posAAB = posAAA + float3(0.0, 0.0, 1.0);
	float3 posBAB = posAAA + float3(1.0, 0.0, 1.0);
	float3 posABB = posAAA + float3(0.0, 1.0, 1.0);
	float3 posBBB = posAAA + float3(1.0, 1.0, 1.0);

	float3 colAAA = tex3D(RandomTextureSampler3D, posAAA / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colBAA = tex3D(RandomTextureSampler3D, posBAA / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colABA = tex3D(RandomTextureSampler3D, posABA / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colBBA = tex3D(RandomTextureSampler3D, posBBA / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colAAB = tex3D(RandomTextureSampler3D, posAAB / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colBAB = tex3D(RandomTextureSampler3D, posBAB / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colABB = tex3D(RandomTextureSampler3D, posABB / xPerlinSize3D).xyz * 4.0 - 1.0;
	float3 colBBB = tex3D(RandomTextureSampler3D, posBBB / xPerlinSize3D).xyz * 4.0 - 1.0;

	float sAAA = dot(colAAA, p - posAAA);
	float sBAA = dot(colBAA, p - posBAA);
	float sABA = dot(colABA, p - posABA);
	float sBBA = dot(colBBA, p - posBBA);
	float sAAB = dot(colAAB, p - posAAB);
	float sBAB = dot(colBAB, p - posBAB);
	float sABB = dot(colABB, p - posABB);
	float sBBB = dot(colBBB, p - posBBB);

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

float4 Perlin3DwithDerivatives(float3 pIn)
{
    float3 p = (pIn + 0.5) * xPerlinSize3D;

    float3 posAAA = floor(p);
    float3 t = p - posAAA;

    float3 posBAA = posAAA + float3(1.0, 0.0, 0.0);
    float3 posABA = posAAA + float3(0.0, 1.0, 0.0);
    float3 posBBA = posAAA + float3(1.0, 1.0, 0.0);
    float3 posAAB = posAAA + float3(0.0, 0.0, 1.0);
    float3 posBAB = posAAA + float3(1.0, 0.0, 1.0);
    float3 posABB = posAAA + float3(0.0, 1.0, 1.0);
    float3 posBBB = posAAA + float3(1.0, 1.0, 1.0);

    float3 colAAA = tex3D(RandomTextureSampler3D, posAAA / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colBAA = tex3D(RandomTextureSampler3D, posBAA / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colABA = tex3D(RandomTextureSampler3D, posABA / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colBBA = tex3D(RandomTextureSampler3D, posBBA / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colAAB = tex3D(RandomTextureSampler3D, posAAB / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colBAB = tex3D(RandomTextureSampler3D, posBAB / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colABB = tex3D(RandomTextureSampler3D, posABB / xPerlinSize3D).xyz * 4.0 - 1.0;
    float3 colBBB = tex3D(RandomTextureSampler3D, posBBB / xPerlinSize3D).xyz * 4.0 - 1.0;

    float sAAA = dot(colAAA, p - posAAA);
    float sBAA = dot(colBAA, p - posBAA);
    float sABA = dot(colABA, p - posABA);
    float sBBA = dot(colBBA, p - posBBA);
    float sAAB = dot(colAAB, p - posAAB);
    float sBAB = dot(colBAB, p - posBAB);
    float sABB = dot(colABB, p - posABB);
    float sBBB = dot(colBBB, p - posBBB);

	//float3 s = t * t * (3.0 - 2.0 * t);
    float3 s = t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
    float3 ds = t * t * (t * (t * 30.0 - 60.0) + 30.0);

    float cx = sBAA - sAAA;
    float cy = sABA - sAAA;
    float cz = sAAB - sAAA;

    float cxy = sBBA - sABA - sBAA + sAAA;
    float cxz = sBAB - sAAB - sBAA + sAAA;
    float cyz = sABB - sAAB - sABA + sAAA;

    float cxyz = sBBB - sABB - sBAB + sAAB - sBBA + sABA + sBAA - sAAA;

    float sxy = s.x * s.y;
    float sxz = s.x * s.z;
    float syz = s.y * s.z;
    float sxyz = s.x * s.y * s.z;

    float noise = sAAA
        + cx * s.x + cy * s.y + cz * s.z
        + cxy * sxy + cxz * sxz + cyz * syz
        + cxyz * sxyz;

    float4 derivAndNoise = float4(
        ds.x * (cx + cxy * s.y + cxz * s.z + cxyz * syz),
        ds.y * (cy + cxy * s.x + cyz * s.z + cxyz * sxz),
        ds.z * (cz + cxz * s.x + cyz * s.y + cxyz * sxy),
        noise);

    return derivAndNoise;
}

struct ScatteringResult
{
	float3 ScatteringColour;
	float3 Attenuation;
};

float scale(float cos)
{
	float x = max(0.0, 1.0 - cos);
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
	float4x4 preLightsWorldViewProjection = mul(xWorld, xLightsViewProjection);

	float4 worldPosition = mul(VSInput.Position, xWorld);
	output.WorldPosition = worldPosition.xyz;
	output.PositionFromLight = mul(VSInput.Position, preLightsWorldViewProjection);
	output.Position = mul(VSInput.Position, preWorldViewProjection);
	output.TextureCoords = VSInput.TexCoords;

	float3 normal = normalize(mul(float4(normalize(VSInput.Normal), 0.0), xWorld)).xyz;
    output.Normal = normal;

	output.ClipDistance = dot(worldPosition, xClipPlane);
	output.Depth = output.Position.z / output.Position.w;

	ScatteringResult scattering = Scattering(worldPosition.xyz);

	output.ScatteringColour = scattering.ScatteringColour;
	output.Attenuation = scattering.Attenuation;

	return output;
}

float Turbulence(float3 pos, float f)
{
    float t = -.5;
    for (; f <= xPerlinSize3D / 12.0; f *= 2.0)
        t += abs(Perlin3D(pos) / f);
    return t;
}

float3 BumpMapNoiseGradient(float3 worldPosition)
{
    float3 pos = worldPosition / 10.0;

    return Perlin3DwithDerivatives(pos).xyz * 0.04;
}

PixelToFrame GroundFromAtmospherePS(GroundFromAtmosphere_VertexToPixel PSInput)
{
	clip(PSInput.ClipDistance);
    PixelToFrame output = (PixelToFrame) 0;

	// shadow map
    float2 ProjectedTexCoords;
    ProjectedTexCoords[0] = PSInput.PositionFromLight.x / PSInput.PositionFromLight.w / 2.0f + 0.5f;
    ProjectedTexCoords[1] = -PSInput.PositionFromLight.y / PSInput.PositionFromLight.w / 2.0f + 0.5f;
	float depthStoredInShadowMap = tex2D(ShadowMapSampler, ProjectedTexCoords).r;
	float realDistance = PSInput.PositionFromLight.z / PSInput.PositionFromLight.w;

    float4 weights;

    float sandTo = 0.21;
    float grassFrom = 0.26;
    float grassTo = 0.56;
    float rockFrom = 0.61;
    float rockTo = 0.79;
    float snowFrom = 0.86;

    float heightSpan = xMinMaxHeight.y - xMinMaxHeight.x;
    float3 posXY = float3(PSInput.WorldPosition.xy, 0.0);
    float normHeight = (PSInput.WorldPosition.y - xMinMaxHeight.x) / heightSpan;

    weights.x = (normHeight - grassFrom) / (sandTo - grassFrom);
    weights.y = min((normHeight - sandTo) / (grassFrom - sandTo), (normHeight - rockFrom) / (grassTo - rockFrom));
    weights.z = min((normHeight - grassTo) / (rockFrom - grassTo), (normHeight - snowFrom) / (rockTo - snowFrom));
    weights.w = (normHeight - rockTo) / (snowFrom - rockTo);
    weights = clamp(weights, 0.0, 1.0);

    float4 farColour =
        tex2D(SandTextureSampler, PSInput.TextureCoords) * weights.x +
        tex2D(GrassTextureSampler, PSInput.TextureCoords) * weights.y +
        tex2D(RockTextureSampler, PSInput.TextureCoords) * weights.z +
        tex2D(SnowTextureSampler, PSInput.TextureCoords) * weights.w;

    float4 nearColour =
        tex2D(SandTextureSampler, PSInput.TextureCoords * 3.0) * weights.x +
        tex2D(GrassTextureSampler, PSInput.TextureCoords * 3.0) * weights.y +
        tex2D(RockTextureSampler, PSInput.TextureCoords * 3.0) * weights.z +
        tex2D(SnowTextureSampler, PSInput.TextureCoords * 3.0) * weights.w;

	float blendFactor = clamp((PSInput.Depth - 0.95) / 0.05, 0, 1);

    float3 normal = normalize(PSInput.Normal);

    float3 reflectionVector = -reflect(xLightDirection, normal);
    float specular = dot(normalize(reflectionVector), normalize(PSInput.WorldPosition - xCameraPosition));
    specular = pow(max(specular, 0.0), 256) * weights.w; // specular on snow only

	float lightingFactor =
		((realDistance - 1.0f/100.0f) <= depthStoredInShadowMap)
		? lightingFactor = clamp(dot(normal, -xLightDirection), 0.0, 1.0)
		: 0.0f;

	output.Color = lerp(nearColour, farColour, blendFactor);
    output.Color.rgb *= (saturate(lightingFactor) + xAmbient);
    output.Color.rgb += specular;
	output.Color.rgb *= PSInput.Attenuation;
	output.Color.rgb += PSInput.ScatteringColour;
	
	// water depth if required
    float distanceAfterWater = abs (length(PSInput.WorldPosition.xyz - xCameraPosition) * PSInput.WorldPosition.y / (PSInput.WorldPosition.y - xCameraPosition.y));
    float distanceAfterWaterMax10 = clamp(distanceAfterWater / 10.0, 0.0, 1.0);
    output.Color.a = xAlphaAfterWaterDepthWeighting ? distanceAfterWaterMax10 : 1.0f;

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
	float4 Position                    : SV_POSITION;
	float4 ReflectionMapSamplingPos    : TEXCOORD1;
	float3 BumpMapSamplingPos          : TEXCOORD2;
	float4 RefractionMapSamplingPos    : TEXCOORD3;
	float3 WorldPosition               : TEXCOORD4;
	float3 ScatteringColour            : COLOR0;
	float3 Attenuation                 : COLOR1;
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
	float4 worldPosition = mul(inPos, xWorld);
	Output.WorldPosition = worldPosition.xyz;

	// need to use more triangles before enabling this...
	//ScatteringResult scattering = Scattering(worldPosition.xyz);

	//Output.ScatteringColour = scattering.ScatteringColour;
	//Output.Attenuation = scattering.Attenuation;

	return Output;
}

WPixelToFrame WaterPS(WVertexToPixel PSIn)
{
	WPixelToFrame Output = (WPixelToFrame)0;

    float4 gradientAndPerturbation = 0.03 * Perlin3DwithDerivatives(PSIn.BumpMapSamplingPos / 2.0)
        + 0.015 * Perlin3DwithDerivatives(PSIn.BumpMapSamplingPos);

    float3 normalVector = float3(0.0, 1.0, 0.0) + gradientAndPerturbation.xyz;
    normalize(normalVector);
    float2 perturbation = gradientAndPerturbation.a;

	float2 projectedReflTexCoords;
	projectedReflTexCoords.x = PSIn.ReflectionMapSamplingPos.x / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
	projectedReflTexCoords.y = -PSIn.ReflectionMapSamplingPos.y / PSIn.ReflectionMapSamplingPos.w / 2.0f + 0.5f;
    float4 reflectiveColorNoPerturb = tex2D(ReflectionSampler, projectedReflTexCoords);
    float distanceMax10 = reflectiveColorNoPerturb.a;
    float2 perturbatedReflTexCoords = projectedReflTexCoords + perturbation * distanceMax10 / 2.0f;
    float4 reflectiveColorPerturb = tex2D(ReflectionSampler, perturbatedReflTexCoords);
    float4 reflectiveColor = (reflectiveColorPerturb.a == 0.0f) ? reflectiveColorNoPerturb : reflectiveColorPerturb;

	float2 projectedRefrTexCoords;
	projectedRefrTexCoords.x = PSIn.RefractionMapSamplingPos.x / PSIn.RefractionMapSamplingPos.w / 2.0f + 0.5f;
	projectedRefrTexCoords.y = -PSIn.RefractionMapSamplingPos.y / PSIn.RefractionMapSamplingPos.w / 2.0f + 0.5f;
    float4 refractiveColorNoPerturb = tex2D(RefractionSampler, projectedRefrTexCoords);
    float distanceUnderwaterMax10 = refractiveColorNoPerturb.a;
    float2 perturbatedRefrTexCoords = projectedRefrTexCoords + perturbation * distanceUnderwaterMax10 / 2.0f;
    float4 refractiveColorPerturb = tex2D(RefractionSampler, perturbatedRefrTexCoords);
    float4 refractiveColor = (refractiveColorPerturb.a == 0.0f) ? refractiveColorNoPerturb : refractiveColorPerturb;
	float4 dullColor = float4(0.0, 0.05, 0.1, 1.0);
    
    float dullWeighting = (refractiveColor.a == 0.0) ? 1.0 : distanceUnderwaterMax10;
    refractiveColor = lerp(refractiveColor, dullColor, 1.0 - exp(-dullWeighting * xWaterOpacity));

	float3 eyeVector = normalize(xCameraPosition - PSIn.WorldPosition);

	// Schlick's approximation
	float AirIOR = 1.0;
	float WaterIOR = 1.33;
	float R0 = (AirIOR - WaterIOR) / (AirIOR + WaterIOR);
	R0 *= R0;

	float fresnelTerm = R0 + (1.0 - R0) * pow(1.0 - dot(eyeVector, normalVector), 5.0);

    float4 combinedColor = lerp(refractiveColor, reflectiveColor, fresnelTerm);
    combinedColor.a = 1.0;
    Output.Color = combinedColor;

    //float foamWeight = 1.0 - clamp(distanceUnderwaterMax10 * 10.0, 0.0, 1.0);

    //Output.Color = lerp (combinedColor, float4(1.0, 1.0, 1.0, 1.0), foamWeight);

	//Output.Color.rgb += PSIn.ScatteringColour;

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

struct ColouredVertexToPixel
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float3 WorldPosition : TEXCOORD0;
    float3 ScatteringColour : COLOR0;
    float3 Attenuation : COLOR1;
	float ClipDistance : TEXCOORD1;
	float4 PositionFromLight: TEXCOORD2;
};

ColouredVertexToPixel ColouredVS(float4 inPos : SV_POSITION, float3 inNormal : NORMAL)
{
    ColouredVertexToPixel Output = (ColouredVertexToPixel) 0;

    float4x4 preViewProjection = mul(xView, xProjection);
    float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);
	float4x4 preLightsWorldViewProjection = mul(xWorld, xLightsViewProjection);

    float3 normal = normalize(mul(float4(normalize(inNormal), 0.0), xWorld)).xyz;
    Output.Normal = normal;

    float4 worldPosition = mul(inPos, xWorld);
    Output.Position = mul(inPos, preWorldViewProjection);
    Output.WorldPosition = worldPosition.xyz;
	Output.PositionFromLight = mul(inPos, preLightsWorldViewProjection);
	Output.ClipDistance = dot(worldPosition, xClipPlane);

    ScatteringResult scattering = Scattering(worldPosition.xyz);

    Output.ScatteringColour = scattering.ScatteringColour;
    Output.Attenuation = scattering.Attenuation;

    return Output;
}

PixelToFrame ColouredPS(ColouredVertexToPixel PSInput)
{
	clip(PSInput.ClipDistance);
    PixelToFrame Output = (PixelToFrame) 0;

	// shadow map
    float2 ProjectedTexCoords;
    ProjectedTexCoords[0] = PSInput.PositionFromLight.x / PSInput.PositionFromLight.w / 2.0f + 0.5f;
    ProjectedTexCoords[1] = -PSInput.PositionFromLight.y / PSInput.PositionFromLight.w / 2.0f + 0.5f;
	float depthStoredInShadowMap = tex2D(ShadowMapSampler, ProjectedTexCoords).r;
	float realDistance = PSInput.PositionFromLight.z / PSInput.PositionFromLight.w;

    Output.Color = float4(1.0, 1.0, 1.0, 1.0);

    float3 normal = PSInput.Normal;

    float3 reflectionVector = -reflect(xLightDirection, normal);
    float specular = dot(normalize(reflectionVector), normalize(PSInput.WorldPosition - xCameraPosition));
    specular = pow(max(specular, 0.0), 256);

	float lightingFactor =
		((realDistance - 1.0f/100.0f) <= depthStoredInShadowMap)
		? lightingFactor = clamp(dot(normal, -xLightDirection), 0.0, 1.0)
		: 0.0f;

    Output.Color.rgb *= (saturate(lightingFactor) + xAmbient);
    Output.Color.rgb += specular;
    Output.Color.rgb *= PSInput.Attenuation;
    Output.Color.rgb += PSInput.ScatteringColour;

	// water depth if required
    float distanceAfterWater = abs(length(PSInput.WorldPosition.xyz - xCameraPosition) * PSInput.WorldPosition.y / (PSInput.WorldPosition.y - xCameraPosition.y));
    float distanceAfterWaterMax10 = clamp(distanceAfterWater / 10.0, 0.0, 1.0);
    Output.Color.a = xAlphaAfterWaterDepthWeighting ? distanceAfterWaterMax10 : 1.0f;

    return Output;
}

technique Coloured
{
    pass Pass0
    {
        VertexShader = compile vs_4_0 ColouredVS();
        PixelShader = compile ps_4_0 ColouredPS();
    }
}