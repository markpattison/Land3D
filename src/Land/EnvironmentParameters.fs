module Land.EnvironmentParameters

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type Water =
    {
        WindDirection: Vector2;
        WindForce: single;
        WaveLength: single;
        WaveHeight: single;
        Opacity: single;
    } with

    member _this.ApplyToEffect (effect: Effect) =
        effect.Parameters.["xWaveLength"].SetValue(_this.WaveLength)
        effect.Parameters.["xWaveHeight"].SetValue(_this.WaveHeight)
        effect.Parameters.["xWindForce"].SetValue(_this.WindForce)
        effect.Parameters.["xWindDirection"].SetValue(_this.WindDirection)

    member _this.ApplyToGroundEffect (effect: Effect) =
        effect.Parameters.["xWaterOpacity"].SetValue(_this.Opacity)

type EnvironmentParameters =
    {
        Water: Water;
    }