module Water

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open FreeCamera
open EnvironmentParameters

type Water(effect: Effect, perlinTexture3D: Texture3D, environment: EnvironmentParameters, device: GraphicsDevice) as _this =
    let pp = device.PresentationParameters
    let refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
    let reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
    let waterSize = 3000.0f
    let waterVertices =
        [|
            VertexPositionTexture(Vector3(-waterSize, 0.0f, -waterSize), new Vector2(0.0f, 0.0f));
            VertexPositionTexture(Vector3( waterSize, 0.0f, -waterSize), new Vector2(1.0f, 0.0f));
            VertexPositionTexture(Vector3(-waterSize, 0.0f,  waterSize), new Vector2(0.0f, 1.0f));

            VertexPositionTexture(Vector3( waterSize, 0.0f, -waterSize), new Vector2(1.0f, 0.0f));
            VertexPositionTexture(Vector3( waterSize, 0.0f,  waterSize), new Vector2(1.0f, 1.0f));
            VertexPositionTexture(Vector3(-waterSize, 0.0f,  waterSize), new Vector2(0.0f, 1.0f));
        |]
    let mutable reflectionView = Unchecked.defaultof<Matrix>

    let drawRefractionMap drawTerrain view =
        let clipPlane = Vector4(Vector3.Down, -0.00001f)
        device.SetRenderTarget(refractionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
        drawTerrain view clipPlane
        device.SetRenderTarget(null)

    let drawReflectionMap drawTerrain drawSkyDome reflectionView =
        let clipPlane = Vector4(Vector3.Up, 0.00001f)
        device.SetRenderTarget(reflectionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
        drawTerrain reflectionView clipPlane
        drawSkyDome reflectionView
        device.SetRenderTarget(null)

    let calculateReflectionView (camera: FreeCamera) =
        let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
        let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
        let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
        Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

    member _this.RefractionTarget = refractionRenderTarget
    member _this.ReflectionTarget = reflectionRenderTarget

    member _this.Prepare view camera drawTerrain drawSkyDome =
        reflectionView <- calculateReflectionView camera
        drawRefractionMap drawTerrain view
        drawReflectionMap drawTerrain drawSkyDome reflectionView

    member _this.DrawWater (time: single) (world: Matrix) (view: Matrix) (projection: Matrix) (lightDirection: Vector3) (camera: FreeCamera) =
        effect.CurrentTechnique <- effect.Techniques.["Water"]
        effect.Parameters.["xWorld"].SetValue(world)
        effect.Parameters.["xView"].SetValue(view)
        effect.Parameters.["xReflectionView"].SetValue(reflectionView)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xCameraPosition"].SetValue(camera.Position)
        effect.Parameters.["xReflectionMap"].SetValue(reflectionRenderTarget)
        effect.Parameters.["xRefractionMap"].SetValue(refractionRenderTarget)
        effect.Parameters.["xTime"].SetValue(time)
        effect.Parameters.["xRandomTexture3D"].SetValue(perlinTexture3D)
        effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)

        environment.Water.ApplyToEffect effect

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, waterVertices, 0, waterVertices.Length / 3)
            )

