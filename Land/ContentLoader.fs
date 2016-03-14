﻿module ContentLoader

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open Environment

type Effects =
    {
        Effect: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

type Textures =
    {
        Grass: Texture2D;
    }

let loadEffects (game: Game) =
    {
        Effect = game.Content.Load<Effect>("Effects/effects")
        SkyFromAtmosphere = game.Content.Load<Effect>("Effects/skyFromAtmosphere")
        GroundFromAtmosphere = game.Content.Load<Effect>("Effects/groundFromAtmosphere")
    }

let loadTextures (game: Game) =
    {
        Grass = game.Content.Load<Texture2D>("Textures/grass")
    }

let loadEnvironment =
    {
        Atmosphere =
            {
                InnerRadius = 10000.0f;
                OuterRadius = 10250.0f;
                ScaleDepth = 0.25f;
                KR = 0.0025f;
                KM = 0.0010f;
                ESun = 20.0f;
                G = -0.95f;
                Wavelengths = Vector3(0.650f, 0.570f, 0.440f);
            };
        Water =
            {
                WaterHeight = 0.0f;
                WindDirection = Vector3(0.5f, 0.0f, 0.0f);
                WindForce = 0.0015f;
                WaveLength = 0.1f;
                WaveHeight = 0.2f;
            };
    }
