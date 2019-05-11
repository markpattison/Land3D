module Land.EnvironmentParameters

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type Atmosphere =
    {
        InnerRadius: single;
        OuterRadius: single;
        ScaleDepth: single;
        KR: single;
        KM: single;
        ESun: single;
        G: single;
        Wavelengths: Vector3;
    } with
    member _this.OuterRadiusSquared = _this.OuterRadius * _this.OuterRadius
    member _this.Scale = 1.0f / (_this.OuterRadius - _this.InnerRadius)
    member _this.ScaleOverScaleDepth = _this.Scale / _this.ScaleDepth
    member _this.KrESun = _this.KR * _this.ESun
    member _this.KmESun = _this.KM * _this.ESun
    member _this.Kr4Pi = _this.KR * 4.0f * single Math.PI
    member _this.Km4Pi = _this.KM * 4.0f * single Math.PI
    member _this.GSquared = _this.G * _this.G
    member _this.Inverse4thPowerWavelengths = Vector3(_this.Wavelengths.X ** -4.0f, _this.Wavelengths.Y ** -4.0f, _this.Wavelengths.Z ** -4.0f)

    member _this.ApplyToEffect (effect: Effect) =
        effect.Parameters.["xInnerRadius"].SetValue(_this.InnerRadius)
        effect.Parameters.["xOuterRadius"].SetValue(_this.OuterRadius)
        effect.Parameters.["xOuterRadiusSquared"].SetValue(_this.OuterRadiusSquared)
        effect.Parameters.["xScale"].SetValue(_this.Scale)
        effect.Parameters.["xScaleDepth"].SetValue(_this.ScaleDepth)
        effect.Parameters.["xScaleOverScaleDepth"].SetValue(_this.ScaleOverScaleDepth)
        effect.Parameters.["xKrESun"].SetValue(_this.KrESun)
        effect.Parameters.["xKmESun"].SetValue(_this.KmESun)
        effect.Parameters.["xKr4Pi"].SetValue(_this.Kr4Pi)
        effect.Parameters.["xKm4Pi"].SetValue(_this.Km4Pi)
        effect.Parameters.["xG"].SetValue(_this.G)
        effect.Parameters.["xGSquared"].SetValue(_this.GSquared)
        effect.Parameters.["xInvWavelength4"].SetValue(_this.Inverse4thPowerWavelengths)

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
        Atmosphere: Atmosphere;
        Water: Water;
    }