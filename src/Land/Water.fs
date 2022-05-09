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

type WaterEffect =
    {
        Effect: Effect
        SetWorld: Matrix -> unit
        SetView: Matrix -> unit
        SetReflectionView: Matrix -> unit
        SetCameraPosition: Vector3 -> unit
        SetLightDirection: Vector3 -> unit
        SetShadowMap: RenderTarget2D -> unit
        SetReflectionMap: RenderTarget2D -> unit
        SetRefractionMap: RenderTarget2D -> unit
        SetTime: single -> unit
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
        Effect: WaterEffect
        Device: GraphicsDevice
    }

let applyToEffect waterParameters (effect: Effect) =
    effect.Parameters.["xWaveLength"].SetValue(waterParameters.WaveLength)
    effect.Parameters.["xWaveHeight"].SetValue(waterParameters.WaveHeight)
    effect.Parameters.["xWindForce"].SetValue(waterParameters.WindForce)
    effect.Parameters.["xWindDirection"].SetValue(waterParameters.WindDirection)
    effect.Parameters.["xWaterOpacity"].SetValue(waterParameters.Opacity)

let waterEffect (effect: Effect) (perlinTexture3D: Texture3D) =
    effect.Parameters.["xRandomTexture3D"].SetValue(perlinTexture3D)
    effect.Parameters.["xPerlinSize3D"].SetValue(single (perlinTexture3D.Depth - 1))
    {
        Effect = effect
        SetWorld = effect.Parameters.["xWorld"].SetValue
        SetView = effect.Parameters.["xView"].SetValue
        SetReflectionView = effect.Parameters.["xReflectionView"].SetValue
        SetCameraPosition = effect.Parameters.["xCameraPosition"].SetValue
        SetLightDirection = effect.Parameters.["xLightDirection"].SetValue
        SetShadowMap = effect.Parameters.["xShadowMap"].SetValue
        SetReflectionMap = effect.Parameters.["xReflectionMap"].SetValue
        SetRefractionMap = effect.Parameters.["xRefractionMap"].SetValue
        SetTime = effect.Parameters.["xTime"].SetValue
    }

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
        Effect = waterEffect effect perlinTexture3D
        Device = device
    }

let drawRefractionMap water drawTerrain view =
    water.Device.SetRenderTarget(water.RefractionRenderTarget)
    water.Device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.Transparent, 1.0f, 0)
    drawTerrain view water.RefractionClipPlane
    water.Device.SetRenderTarget(null)

let drawReflectionMap water drawTerrain drawSkyDome reflectionView =
    water.Device.SetRenderTarget(water.ReflectionRenderTarget)
    water.Device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.Transparent, 1.0f, 0)
    drawTerrain reflectionView water.ReflectionClipPlane
    drawSkyDome reflectionView
    water.Device.SetRenderTarget(null)

let calculateReflectionView (camera: FreeCamera) =
    let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
    let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
    let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
    Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

let prepareFrameAndReturnReflectionView water view camera drawTerrain drawSkyDome =
    let reflectionView = calculateReflectionView camera
    drawRefractionMap water (drawTerrain true) view
    drawReflectionMap water (drawTerrain true) drawSkyDome reflectionView
    reflectionView

let drawWater water (time: single) (world: Matrix) (view: Matrix) (lightDirection: Vector3) (camera: FreeCamera) (reflectionView: Matrix) =
    let effect = water.Effect
    effect.Effect.CurrentTechnique <- effect.Effect.Techniques.["Water"]

    effect.SetWorld world
    effect.SetView view
    effect.SetReflectionView reflectionView
    effect.SetLightDirection lightDirection
    effect.SetCameraPosition camera.Position
    effect.SetReflectionMap water.ReflectionRenderTarget
    effect.SetRefractionMap water.RefractionRenderTarget
    effect.SetTime time

    effect.Effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            water.Device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, water.Vertices, 0, water.Vertices.Length / 3)
        )

