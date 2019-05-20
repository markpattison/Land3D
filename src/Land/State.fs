namespace Land

open Microsoft.Xna.Framework

open FreeCamera

type DebugOption =
    | None
    | ShadowMap
    | ReflectionMap
    | RefractionMap
    member _this.Next =
        match _this with
        | None -> ShadowMap
        | ShadowMap -> ReflectionMap
        | ReflectionMap -> RefractionMap
        | RefractionMap -> None

type State =
    {
        LightDirection: Vector3
        Camera: FreeCamera
        Exiting: bool
        DebugOption: DebugOption
    }
