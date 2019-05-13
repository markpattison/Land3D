module Land.Effects

open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

type Effects =
    {
        Effect: Effect;
        Hdr: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

let load (contentManager: ContentManager) =
    {
        Effect = contentManager.Load<Effect>("Effects/effects")
        Hdr = contentManager.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = contentManager.Load<Effect>("Effects/skyFromAtmosphere")
        GroundFromAtmosphere = contentManager.Load<Effect>("Effects/groundFromAtmosphere")
    }
