module Land.Atmosphere

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type AtmosphereParameters =
    {
        InnerRadius: single
        OuterRadius: single
        ScaleDepth: single
        KR: single
        KM: single
        ESun: single
        G: single
        Wavelengths: Vector3
    }

type Atmosphere =
    {
        AtmosphereParameters: AtmosphereParameters
        OuterRadiusSquared: single
        Scale: single
        ScaleOverScaleDepth: single
        KrESun: single
        KmESun: single
        Kr4Pi: single
        Km4Pi: single
        GSquared: single
        Inverse4thPowerWavelengths: Vector3
    }

let prepare atmosphereParameters =
    let scale = 1.0f / (atmosphereParameters.OuterRadius - atmosphereParameters.InnerRadius)
    {
        AtmosphereParameters = atmosphereParameters
        OuterRadiusSquared = atmosphereParameters.OuterRadius * atmosphereParameters.OuterRadius
        Scale = scale
        ScaleOverScaleDepth = scale / atmosphereParameters.ScaleDepth
        KrESun = atmosphereParameters.KR * atmosphereParameters.ESun
        KmESun = atmosphereParameters.KM * atmosphereParameters.ESun
        Kr4Pi = atmosphereParameters.KR * 4.0f * single Math.PI
        Km4Pi = atmosphereParameters.KM * 4.0f * single Math.PI
        GSquared = atmosphereParameters.G * atmosphereParameters.G
        Inverse4thPowerWavelengths = Vector3(atmosphereParameters.Wavelengths.X ** -4.0f, atmosphereParameters.Wavelengths.Y ** -4.0f, atmosphereParameters.Wavelengths.Z ** -4.0f)
    }

let applyToEffect atmosphere (effect: Effect) =
    effect.Parameters.["xInnerRadius"].SetValue(atmosphere.AtmosphereParameters.InnerRadius)
    effect.Parameters.["xOuterRadius"].SetValue(atmosphere.AtmosphereParameters.OuterRadius)
    effect.Parameters.["xOuterRadiusSquared"].SetValue(atmosphere.OuterRadiusSquared)
    effect.Parameters.["xScale"].SetValue(atmosphere.Scale)
    effect.Parameters.["xScaleDepth"].SetValue(atmosphere.AtmosphereParameters.ScaleDepth)
    effect.Parameters.["xScaleOverScaleDepth"].SetValue(atmosphere.ScaleOverScaleDepth)
    effect.Parameters.["xKrESun"].SetValue(atmosphere.KrESun)
    effect.Parameters.["xKmESun"].SetValue(atmosphere.KmESun)
    effect.Parameters.["xKr4Pi"].SetValue(atmosphere.Kr4Pi)
    effect.Parameters.["xKm4Pi"].SetValue(atmosphere.Km4Pi)
    effect.Parameters.["xG"].SetValue(atmosphere.AtmosphereParameters.G)
    effect.Parameters.["xGSquared"].SetValue(atmosphere.GSquared)
    effect.Parameters.["xInvWavelength4"].SetValue(atmosphere.Inverse4thPowerWavelengths)
