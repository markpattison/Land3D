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

type Effects =
    {
        Effect: Effect
        Hdr: Effect
        SkyFromAtmosphere: Effect
        GroundFromAtmosphere: GroundFromAtmosphereEffect
    }

let private groundFromAtmosphere (contentManager: ContentManager) (atmosphere: Atmosphere.Atmosphere) (water: Water.WaterParameters) (projection: Matrix) =
    let groundFromAtmosphere = contentManager.Load<Effect>("Effects/groundFromAtmosphere")

    Atmosphere.applyToEffect atmosphere groundFromAtmosphere
    Water.applyToEffect water groundFromAtmosphere

    groundFromAtmosphere.Parameters.["xProjection"].SetValue(projection)
    groundFromAtmosphere.Parameters.["xGrassTexture"].SetValue(contentManager.Load<Texture2D>("Textures/grass"))
    groundFromAtmosphere.Parameters.["xRockTexture"].SetValue(contentManager.Load<Texture2D>("Textures/rock"))
    groundFromAtmosphere.Parameters.["xSandTexture"].SetValue(contentManager.Load<Texture2D>("Textures/sand"))
    groundFromAtmosphere.Parameters.["xSnowTexture"].SetValue(contentManager.Load<Texture2D>("Textures/snow"))

    {
        Effect = groundFromAtmosphere
        SetWorld = groundFromAtmosphere.Parameters.["xWorld"].SetValue
        SetView = groundFromAtmosphere.Parameters.["xView"].SetValue
        SetCameraPosition = groundFromAtmosphere.Parameters.["xCameraPosition"].SetValue
        SetLightDirection = groundFromAtmosphere.Parameters.["xLightDirection"].SetValue
        SetLightViewProjection = groundFromAtmosphere.Parameters.["xLightsViewProjection"].SetValue
        SetShadowMap = groundFromAtmosphere.Parameters.["xShadowMap"].SetValue
        SetClipPlane = groundFromAtmosphere.Parameters.["xClipPlane"].SetValue
        SetAmbient = groundFromAtmosphere.Parameters.["xAmbient"].SetValue
        SetAlphaAfterWaterDepthWeighting = groundFromAtmosphere.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue
    }

let load (contentManager: ContentManager) (atmosphere: Atmosphere.Atmosphere) (water: Water.WaterParameters) (projection: Matrix) =

    let skyFromAtmosphere = contentManager.Load<Effect>("Effects/skyFromAtmosphere")
    Atmosphere.applyToEffect atmosphere skyFromAtmosphere
    skyFromAtmosphere.Parameters.["xProjection"].SetValue(projection)

    {
        Effect = contentManager.Load<Effect>("Effects/effects")
        Hdr = contentManager.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = skyFromAtmosphere
        GroundFromAtmosphere = groundFromAtmosphere contentManager atmosphere water projection
    }
