namespace Land

open Microsoft.Xna.Framework

open FreeCamera

type State =
    {
        LightDirection: Vector3
        Camera: FreeCamera
        Exiting: bool
    }
