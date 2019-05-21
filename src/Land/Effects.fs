module Land.Effects

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

type Effects =
    {
        Effect: Effect;
        Hdr: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

let load (contentManager: ContentManager) (atmosphere: Atmosphere.Atmosphere) (water: Water.WaterParameters) (projection: Matrix) =

    let groundFromAtmosphere = contentManager.Load<Effect>("Effects/groundFromAtmosphere")
    Atmosphere.applyToEffect atmosphere groundFromAtmosphere
    Water.applyToEffect water groundFromAtmosphere
    groundFromAtmosphere.Parameters.["xProjection"].SetValue(projection)
    groundFromAtmosphere.Parameters.["xGrassTexture"].SetValue(contentManager.Load<Texture2D>("Textures/grass"))
    groundFromAtmosphere.Parameters.["xRockTexture"].SetValue(contentManager.Load<Texture2D>("Textures/rock"))
    groundFromAtmosphere.Parameters.["xSandTexture"].SetValue(contentManager.Load<Texture2D>("Textures/sand"))
    groundFromAtmosphere.Parameters.["xSnowTexture"].SetValue(contentManager.Load<Texture2D>("Textures/snow"))

    let skyFromAtmosphere = contentManager.Load<Effect>("Effects/skyFromAtmosphere")
    Atmosphere.applyToEffect atmosphere skyFromAtmosphere
    skyFromAtmosphere.Parameters.["xProjection"].SetValue(projection)

    {
        Effect = contentManager.Load<Effect>("Effects/effects")
        Hdr = contentManager.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = skyFromAtmosphere
        GroundFromAtmosphere = groundFromAtmosphere
    }
