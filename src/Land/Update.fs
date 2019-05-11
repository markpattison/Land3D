module Game1.Update

open Microsoft.Xna.Framework

open Input
open Game1.Types

let update (gameTime: GameTime) (input: Input) gameState =
    let time = float32 gameTime.TotalGameTime.TotalSeconds

    let camera = gameState.Camera.Updated(input, time)

    let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
    let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
    let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)

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

