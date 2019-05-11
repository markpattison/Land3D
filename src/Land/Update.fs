module Land.Update

open Microsoft.Xna.Framework

open Input

let update (gameTime: GameTime) (input: Input) gameState =
    let time = float32 gameTime.TotalGameTime.TotalSeconds

    let camera = gameState.Camera.Updated(input, time)

    let lightDirection =
        match input.PageDown, input.PageUp with
        | true, false -> Vector3.Transform(gameState.LightDirection, Matrix.CreateRotationX(0.003f))
        | false, true -> Vector3.Transform(gameState.LightDirection, Matrix.CreateRotationX(-0.003f))
        | _ -> gameState.LightDirection
        
    {
        Camera = camera
        LightDirection = lightDirection
        Exiting = input.Quit
    }

