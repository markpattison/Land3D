module Land.Effects

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

type GroundFromAtmosphereEffect =
    {
        Effect: Effect
        SetWorld: Matrix -> unit
        SetView: Matrix -> unit
        SetCameraPosition: Vector3 -> unit
        SetLightDirection: Vector3 -> unit
        SetLightViewProjection: Matrix -> unit
        SetShadowMap: RenderTarget2D -> unit
        SetClipPlane: Vector4 -> unit
        SetAmbient: single -> unit
        SetAlphaAfterWaterDepthWeighting: bool -> unit
    }

type ShadowMapEffect =
    {
        Effect: Effect
        SetWorld: Matrix -> unit
        SetLightViewProjection: Matrix -> unit
    }

type Effects =
    {
        Effect: Effect
        Hdr: Effect
        SkyFromAtmosphere: Effect
        GroundFromAtmosphere: GroundFromAtmosphereEffect
        ShadowMap: ShadowMapEffect
    }

let private groundFromAtmosphere (contentManager: ContentManager) (atmosphere: Atmosphere.Atmosphere) (water: Water.WaterParameters) (projection: Matrix) (terrainMinMax: Vector2) =
    let effect = contentManager.Load<Effect>("Effects/groundFromAtmosphere")

    Atmosphere.applyToGroundEffect atmosphere effect
    Water.applyToEffect water effect

    effect.Parameters.["xProjection"].SetValue(projection)
    effect.Parameters.["xGrassTexture"].SetValue(contentManager.Load<Texture2D>("Textures/grass"))
    effect.Parameters.["xRockTexture"].SetValue(contentManager.Load<Texture2D>("Textures/rock"))
    effect.Parameters.["xSandTexture"].SetValue(contentManager.Load<Texture2D>("Textures/sand"))
    effect.Parameters.["xSnowTexture"].SetValue(contentManager.Load<Texture2D>("Textures/snow"))
    effect.Parameters.["xMinMaxHeight"].SetValue(terrainMinMax)

    {
        Effect = effect
        SetWorld = effect.Parameters.["xWorld"].SetValue
        SetView = effect.Parameters.["xView"].SetValue
        SetCameraPosition = effect.Parameters.["xCameraPosition"].SetValue
        SetLightDirection = effect.Parameters.["xLightDirection"].SetValue
        SetLightViewProjection = effect.Parameters.["xLightsViewProjection"].SetValue
        SetShadowMap = effect.Parameters.["xShadowMap"].SetValue
        SetClipPlane = effect.Parameters.["xClipPlane"].SetValue
        SetAmbient = effect.Parameters.["xAmbient"].SetValue
        SetAlphaAfterWaterDepthWeighting = effect.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue
    }

let private shadowMapEffect (contentManager: ContentManager) =
    let effect = contentManager.Load<Effect>("Effects/shadowMap")
    effect.CurrentTechnique <- effect.Techniques.["ShadowMap"]

    {
        Effect = effect
        SetWorld = effect.Parameters.["xWorld"].SetValue
        SetLightViewProjection = effect.Parameters.["xLightViewProjection"].SetValue
    }

let load (contentManager: ContentManager) (atmosphere: Atmosphere.Atmosphere) (water: Water.WaterParameters) (projection: Matrix) (terrainMinMax: Vector2) =

    let skyFromAtmosphere = contentManager.Load<Effect>("Effects/skyFromAtmosphere")
    Atmosphere.applyToSkyEffect atmosphere skyFromAtmosphere
    skyFromAtmosphere.Parameters.["xProjection"].SetValue(projection)

    let shadowMap = contentManager.Load<Effect>("Effects/shadowMap")
    shadowMap.CurrentTechnique <- shadowMap.Techniques.["ShadowMap"]

    {
        Effect = contentManager.Load<Effect>("Effects/effects")
        Hdr = contentManager.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = skyFromAtmosphere
        GroundFromAtmosphere = groundFromAtmosphere contentManager atmosphere water projection terrainMinMax
        ShadowMap = shadowMapEffect contentManager
    }
