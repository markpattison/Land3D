module Land.Effects

open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

open Atmosphere

type Effects =
    {
        Effect: Effect;
        Hdr: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

let load (contentManager: ContentManager) (atmosphere: Atmosphere) =

    let groundFromAtmosphere = contentManager.Load<Effect>("Effects/groundFromAtmosphere")
    applyToEffect atmosphere groundFromAtmosphere
    groundFromAtmosphere.Parameters.["xGrassTexture"].SetValue(contentManager.Load<Texture2D>("Textures/grass"))
    groundFromAtmosphere.Parameters.["xRockTexture"].SetValue(contentManager.Load<Texture2D>("Textures/rock"))
    groundFromAtmosphere.Parameters.["xSandTexture"].SetValue(contentManager.Load<Texture2D>("Textures/sand"))
    groundFromAtmosphere.Parameters.["xSnowTexture"].SetValue(contentManager.Load<Texture2D>("Textures/snow"))

    let skyFromAtmosphere = contentManager.Load<Effect>("Effects/skyFromAtmosphere")
    applyToEffect atmosphere skyFromAtmosphere

    {
        Effect = contentManager.Load<Effect>("Effects/effects")
        Hdr = contentManager.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = skyFromAtmosphere
        GroundFromAtmosphere = groundFromAtmosphere
    }
