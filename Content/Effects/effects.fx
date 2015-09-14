//------------------------------------------------------
//--                                                  --
//--		   www.riemers.net                    --
//--   		    Basic shaders                     --
//--		Use/modify as you like                --
//--                                                  --
//------------------------------------------------------

struct VertexToPixel
{
	float4 Position   	: SV_POSITION;
	float4 Color		: COLOR0;
	float LightingFactor : TEXCOORD0;
	float2 TextureCoords: TEXCOORD1;
	float ClipDistance : TEXCOORD2;
	float Depth : TEXCOORD4;
};

struct PixelToFrame
{
	float4 Color : COLOR0;
};

//------- Constants --------
float4x4 xView;
float4x4 xReflectionView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float xAmbient;
bool xEnableLighting;
bool xShowNormals;
float3 xCamPos;
float3 xCamUp;
float xPointSpriteSize;
float4 xClipPlane;
float xWaveLength;
float xWaveHeight;
float xTime;
float3 xWindDirection;
float xWindForce;

float xPerlinSize2D;
float xPerlinSize3D;

//------- Texture Samplers --------

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xReflectionMap;
sampler ReflectionSampler = sampler_state { texture = <xReflectionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xRefractionMap;
sampler RefractionSampler = sampler_state { texture = <xRefractionMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xDebugTexture;
sampler DebugTextureSampler = sampler_state { texture = <xDebugTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xWaterBumpMap;
sampler WaterBumpMapSampler = sampler_state { texture = <xWaterBumpMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xRandomTexture2D;
sampler RandomTextureSampler2D = sampler_state { texture = <xRandomTexture2D>; AddressU = WRAP; AddressV = WRAP; };

Texture xRandomTexture3D;
sampler RandomTextureSampler3D = sampler_state { texture = <xRandomTexture3D>; AddressU = WRAP; AddressV = WRAP; AddressW = WRAP; };

//------- Perlin noise functions -------

float Perlin2D(float2 pIn)
{
	float2 p = pIn * xPerlinSize2D;

		float2 posAA = floor(p);
		float2 pfrac = p - posAA;

		float2 posBA = posAA + float2(1.0, 0.0);
		float2 posAB = posAA + float2(0.0, 1.0);
		float2 posBB = posAA + float2(1.0, 1.0);

		float2 colAA = tex2D(RandomTextureSampler2D, posAA / xPerlinSize2D) * 2.0 - 1.0;
		float2 colBA = tex2D(RandomTextureSampler2D, posBA / xPerlinSize2D) * 2.0 - 1.0;
		float2 colAB = tex2D(RandomTextureSampler2D, posAB / xPerlinSize2D) * 2.0 - 1.0;
		float2 colBB = tex2D(RandomTextureSampler2D, posBB / xPerlinSize2D) * 2.0 - 1.0;

		float sAA = mul(colAA, p - posAA);
	float sBA = mul(colBA, p - posBA);
	float sAB = mul(colAB, p - posAB);
	float sBB = mul(colBB, p - posBB);

	float2 s = pfrac * pfrac * (3 - 2 * pfrac);

		float sPA = sAA + s.x * (sBA - sAA);
	float sPB = sAB + s.x * (sBB - sAB);

	return sPA + s.y * (sPB - sPA);
}

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

		float3 colAAA = tex3D(RandomTextureSampler3D, posAAA / xPerlinSize3D) * 2.0 - 1.0;
		float3 colBAA = tex3D(RandomTextureSampler3D, posBAA / xPerlinSize3D) * 2.0 - 1.0;
		float3 colABA = tex3D(RandomTextureSampler3D, posABA / xPerlinSize3D) * 2.0 - 1.0;
		float3 colBBA = tex3D(RandomTextureSampler3D, posBBA / xPerlinSize3D) * 2.0 - 1.0;
		float3 colAAB = tex3D(RandomTextureSampler3D, posAAB / xPerlinSize3D) * 2.0 - 1.0;
		float3 colBAB = tex3D(RandomTextureSampler3D, posBAB / xPerlinSize3D) * 2.0 - 1.0;
		float3 colABB = tex3D(RandomTextureSampler3D, posABB / xPerlinSize3D) * 2.0 - 1.0;
		float3 colBBB = tex3D(RandomTextureSampler3D, posBBB / xPerlinSize3D) * 2.0 - 1.0;

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

//------- Technique: Pretransformed --------

VertexToPixel PretransformedVS(float4 inPos : SV_POSITION, float4 inColor : COLOR)
{
	VertexToPixel Output = (VertexToPixel)0;

	Output.Position = inPos;
	Output.Color = inColor;

	return Output;
}

PixelToFrame PretransformedPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = PSIn.Color;

	return Output;
}

technique Pretransformed
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 PretransformedVS();
		PixelShader = compile ps_4_0 PretransformedPS();
	}
}

//------- Technique: Debug --------

VertexToPixel DebugVS(float4 inPos : SV_POSITION, float2 inTexCoords : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;

	Output.Position = inPos;
	Output.TextureCoords = inTexCoords;

	return Output;
}

PixelToFrame DebugPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = tex2D(DebugTextureSampler, PSIn.TextureCoords);

	return Output;
}

technique Debug
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 DebugVS();
		PixelShader = compile ps_4_0 DebugPS();
	}
}

//------- Technique: Textured --------

VertexToPixel TexturedVS(float4 inPos : SV_POSITION, float3 inNormal : NORMAL, float2 inTexCoords : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	Output.TextureCoords = inTexCoords;

	float3 Normal = normalize(mul(normalize(inNormal), xWorld));
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = dot(Normal, -xLightDirection);

	return Output;
}

PixelToFrame TexturedPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);
	Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;

	return Output;
}

technique Textured
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedVS();
		PixelShader = compile ps_4_0 TexturedPS();
	}
}

//------- Technique: TexturedClipped --------

VertexToPixel TexturedClippedVS(float4 inPos : SV_POSITION, float3 inNormal : NORMAL, float2 inTexCoords : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	float4 worldPosition = mul(inPos, xWorld);
	Output.Position = mul(inPos, preWorldViewProjection);
	Output.TextureCoords = inTexCoords;

	float3 Normal = normalize(mul(normalize(inNormal), xWorld));
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = dot(Normal, -xLightDirection);

	Output.ClipDistance = dot(worldPosition, xClipPlane);
	Output.Depth = Output.Position.z / Output.Position.w;

	return Output;
}

PixelToFrame TexturedClippedPS(VertexToPixel PSIn)
{
	clip(PSIn.ClipDistance);

	PixelToFrame Output = (PixelToFrame)0;

	float4 farColour = tex2D(TextureSampler, PSIn.TextureCoords);
	float4 nearColour = tex2D(TextureSampler, PSIn.TextureCoords * 3.0);
	float blendFactor = clamp((PSIn.Depth - 0.95) / 0.05, 0, 1);

	Output.Color = lerp(nearColour, farColour, blendFactor);
	Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;

	return Output;
}

technique TexturedClipped
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedClippedVS();
		PixelShader = compile ps_4_0 TexturedClippedPS();
	}
}

//------- Technique: Water --------

struct WVertexToPixel
{
	float4 Position                 : SV_POSITION;
	float4 ReflectionMapSamplingPos    : TEXCOORD1;
	float3 BumpMapSamplingPos        : TEXCOORD2;
	float4 RefractionMapSamplingPos : TEXCOORD3;
	float4 Position3D                : TEXCOORD4;
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
	float2 moveVector = float2(0, xTime * xWindForce);
	Output.BumpMapSamplingPos.xz = (inTex.xy + moveVector.xy) / xWaveLength;
	Output.BumpMapSamplingPos.y = xTime / 100.0f;
	Output.ReflectionMapSamplingPos = mul(inPos, preWorldReflectionViewProjection);
	Output.RefractionMapSamplingPos = mul(inPos, preWorldViewProjection);
	Output.Position3D = mul(inPos, xWorld);

	return Output;
}

float PerlinWater(float3 pos)
{
	float3 offset1 = (0.1f, 0.6f, 0.3f);
	float3 offset2 = (0.45f, 0.17f, 0.88f);
	float3 offset3 = (0.83f, 0.44f, 0.09f);
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

	float3 eyeVector = normalize(xCamPos - PSIn.Position3D);
	float fresnelTerm = dot(eyeVector, normalVector);
	
	float3 reflectionVector = reflect(xLightDirection, normalVector);
	float specular = max(0.0f, dot(normalize(reflectionVector), normalize(eyeVector)));
	specular = pow(specular, 1024);
	
	float4 combinedColor = lerp(reflectiveColor, refractiveColor, fresnelTerm);
	float4 dullColor = float4(0.3f, 0.35f, 0.45f, 1.0f);
	Output.Color = lerp(combinedColor, dullColor, 0.4f);

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

//------- Technique: TexturedNoShading --------

VertexToPixel TexturedNoShadingVS(float4 inPos : SV_POSITION, float3 inNormal : NORMAL, float2 inTexCoords : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
		float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
	Output.TextureCoords = inTexCoords;

	return Output;
}

PixelToFrame TexturedNoShadingPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);

	return Output;
}

technique TexturedNoShading
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 TexturedNoShadingVS();
		PixelShader = compile ps_4_0 TexturedNoShadingPS();
	}
}

//------- Technique: PointSprites --------

VertexToPixel PointSpriteVS(float3 inPos: SV_POSITION, float2 inTexCoord : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;

	float3 center = mul(inPos, xWorld);
		float3 eyeVector = center - xCamPos;

		float3 sideVector = cross(eyeVector, xCamUp);
		sideVector = normalize(sideVector);
	float3 upVector = cross(sideVector, eyeVector);
		upVector = normalize(upVector);

	float3 finalPosition = center;
		finalPosition += (inTexCoord.x - 0.5f)*sideVector*0.5f*xPointSpriteSize;
	finalPosition += (0.5f - inTexCoord.y)*upVector*0.5f*xPointSpriteSize;

	float4 finalPosition4 = float4(finalPosition, 1);

		float4x4 preViewProjection = mul(xView, xProjection);
		Output.Position = mul(finalPosition4, preViewProjection);

	Output.TextureCoords = inTexCoord;

	return Output;
}

PixelToFrame PointSpritePS(VertexToPixel PSIn) : COLOR0
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);

	return Output;
}

technique PointSprites
{
	pass Pass0
	{
		VertexShader = compile vs_4_0 PointSpriteVS();
		PixelShader = compile ps_4_0 PointSpritePS();
	}
}