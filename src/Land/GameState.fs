namespace Land

open Microsoft.Xna.Framework

open FreeCamera

type GameState =
    {
        LightDirection: Vector3
        Camera: FreeCamera
        Exiting: bool
    }
