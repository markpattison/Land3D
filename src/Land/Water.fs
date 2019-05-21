module Land.Water

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open FreeCamera

type WaterParameters =
    {
        WindDirection: Vector2;
        WindForce: single;
        WaveLength: single;
        WaveHeight: single;
        Opacity: single;
    }

type Water =
    {
        WaterParameters: WaterParameters
        RefractionRenderTarget: RenderTarget2D
        ReflectionRenderTarget: RenderTarget2D
        Vertices: VertexPositionTexture[]
        PerlinTexture3D: Texture3D
        RefractionClipPlane: Vector4
        ReflectionClipPlane: Vector4
        Effect: Effect
        Device: GraphicsDevice
    }

let applyToEffect waterParameters (effect: Effect) =
    effect.Parameters.["xWaveLength"].SetValue(waterParameters.WaveLength)
    effect.Parameters.["xWaveHeight"].SetValue(waterParameters.WaveHeight)
    effect.Parameters.["xWindForce"].SetValue(waterParameters.WindForce)
    effect.Parameters.["xWindDirection"].SetValue(waterParameters.WindDirection)
    effect.Parameters.["xWaterOpacity"].SetValue(waterParameters.Opacity)

let waterVertices waterSize =
    [|
        VertexPositionTexture(Vector3(-waterSize, 0.0f, -waterSize), new Vector2(0.0f, 0.0f));
        VertexPositionTexture(Vector3( waterSize, 0.0f, -waterSize), new Vector2(1.0f, 0.0f));
        VertexPositionTexture(Vector3(-waterSize, 0.0f,  waterSize), new Vector2(0.0f, 1.0f));

        VertexPositionTexture(Vector3( waterSize, 0.0f, -waterSize), new Vector2(1.0f, 0.0f));
        VertexPositionTexture(Vector3( waterSize, 0.0f,  waterSize), new Vector2(1.0f, 1.0f));
        VertexPositionTexture(Vector3(-waterSize, 0.0f,  waterSize), new Vector2(0.0f, 1.0f));
    |]

let prepare (effect: Effect) (perlinTexture3D: Texture3D) (waterParameters: WaterParameters) (device: GraphicsDevice) waterSize =
    let pp = device.PresentationParameters
    {
        WaterParameters = waterParameters
        RefractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
        ReflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
        Vertices = waterVertices waterSize
        PerlinTexture3D = perlinTexture3D
        RefractionClipPlane = Vector4(Vector3.Down, -0.00001f)
        ReflectionClipPlane = Vector4(Vector3.Up, 0.00001f)
        Effect = effect
        Device = device
    }

let drawRefractionMap water drawTerrain view world =
    water.Device.SetRenderTarget(water.RefractionRenderTarget)
    water.Device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
    drawTerrain view world water.RefractionClipPlane
    water.Device.SetRenderTarget(null)

let drawReflectionMap water drawTerrain drawSkyDome reflectionView world =
    water.Device.SetRenderTarget(water.ReflectionRenderTarget)
    water.Device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
    drawTerrain reflectionView world water.ReflectionClipPlane
    drawSkyDome reflectionView
    water.Device.SetRenderTarget(null)

let calculateReflectionView (camera: FreeCamera) =
    let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
    let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
    let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
    Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

let prepareFrameAndReturnReflectionView water view world camera drawTerrain drawSkyDome =
    let reflectionView = calculateReflectionView camera
    drawRefractionMap water (drawTerrain true) view world
    drawReflectionMap water (drawTerrain true) drawSkyDome reflectionView world
    reflectionView

let drawWater water (time: single) (world: Matrix) (view: Matrix) (lightDirection: Vector3) (camera: FreeCamera) (reflectionView: Matrix) =
    water.Effect.CurrentTechnique <- water.Effect.Techniques.["Water"]
    water.Effect.Parameters.["xWorld"].SetValue(world)
    water.Effect.Parameters.["xView"].SetValue(view)
    water.Effect.Parameters.["xReflectionView"].SetValue(reflectionView)
    water.Effect.Parameters.["xLightDirection"].SetValue(lightDirection)
    water.Effect.Parameters.["xCameraPosition"].SetValue(camera.Position)
    water.Effect.Parameters.["xReflectionMap"].SetValue(water.ReflectionRenderTarget)
    water.Effect.Parameters.["xRefractionMap"].SetValue(water.RefractionRenderTarget)
    water.Effect.Parameters.["xTime"].SetValue(time)
    water.Effect.Parameters.["xRandomTexture3D"].SetValue(water.PerlinTexture3D)
    water.Effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)

    water.Effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            water.Device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, water.Vertices, 0, water.Vertices.Length / 3)
        )

