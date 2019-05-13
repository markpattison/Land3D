module Land.Update

open Microsoft.Xna.Framework

open Input

let update (gameTime: GameTime) (input: Input) state =
    let time = float32 gameTime.TotalGameTime.TotalSeconds

    let camera = state.Camera.Updated(input, time)

    let lightDirection =
        match input.PageDown, input.PageUp with
        | true, false -> Vector3.Transform(state.LightDirection, Matrix.CreateRotationX(0.003f))
        | false, true -> Vector3.Transform(state.LightDirection, Matrix.CreateRotationX(-0.003f))
        | _ -> state.LightDirection
        
    {
        Camera = camera
        LightDirection = lightDirection
        Exiting = input.Quit
        DebugOption = if input.Debug then state.DebugOption.Next else state.DebugOption
    }

