module ContentLoader

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type Effects =
    {
        Effect: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

let loadEffects (game: Game) =
    {
        Effect = game.Content.Load<Effect>("Effects/effects")
        SkyFromAtmosphere = game.Content.Load<Effect>("Effects/skyFromAtmosphere")
        GroundFromAtmosphere = game.Content.Load<Effect>("Effects/groundFromAtmosphere")
    }

