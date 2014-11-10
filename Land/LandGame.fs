namespace Game1

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open FreeCamera
open Input
open Terrain

type LandGame() as this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(this)
    let mutable effect = Unchecked.defaultof<Effect>
    let mutable vertices = Unchecked.defaultof<VertexPositionNormalTexture[]>
    let mutable waterVertices = Unchecked.defaultof<VertexPositionTexture[]>
    let mutable debugVertices = Unchecked.defaultof<VertexPositionTexture[]>
    let mutable indices = Unchecked.defaultof<int[]>
    let mutable world = Unchecked.defaultof<Matrix>
    let mutable view = Unchecked.defaultof<Matrix>
    let mutable reflectionView = Unchecked.defaultof<Matrix>
    let mutable projection = Unchecked.defaultof<Matrix>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
    let mutable terrain = Unchecked.defaultof<Terrain>
    let mutable grassTexture = Unchecked.defaultof<Texture2D>
    let mutable waterBumpMap = Unchecked.defaultof<Texture2D>
    let mutable lightDirection = Unchecked.defaultof<Vector3>
    let mutable waterHeight = Unchecked.defaultof<single>
    let mutable refractionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable reflectionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable noClipPlane = Unchecked.defaultof<Vector4>
    let mutable windDirection = Unchecked.defaultof<Vector3>
    let mutable skyDome = Unchecked.defaultof<Model>
    let mutable cloudMap = Unchecked.defaultof<Texture2D>
    let mutable camera = Unchecked.defaultof<FreeCamera>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    do graphics.PreferredBackBufferWidth <- 800
    do graphics.PreferredBackBufferHeight <- 600
    do graphics.ApplyChanges()
    do base.Content.RootDirectory <- "Content"

    let createTerrain =
        terrain <- Terrain 128
        do terrain.DeformCircularFaults 300 2.0f 20.0f 100.0f
        do terrain.Normalize 0.5f 2.0f
        do terrain.Stretch 2.5f
        do terrain.Normalize -1.0f 4.0f
        vertices <- GetVertices terrain
        indices <- GetIndices terrain.Size

    override this.Initialize() =
        device <- base.GraphicsDevice
        effect <- EffectReader.GetEffect device @"effects.mgfxo"

        base.Initialize()
        ()

    override this.LoadContent() =
        createTerrain
        world <- Matrix.Identity
        projection <- Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 1000.0f)
        grassTexture <- this.Content.Load<Texture2D>("grass")
        waterBumpMap <- this.Content.Load<Texture2D>("waterbump")

        let dir = Vector3(0.0f, -0.5f, -1.0f)
        dir.Normalize()
        lightDirection <- dir

        waterHeight <- 0.0f
        let pp = device.PresentationParameters
        refractionRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24)
        reflectionRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24)
        noClipPlane <- Vector4.Zero

        windDirection <- Vector3(1.0f, 0.0f, 0.0f)

        let halfSize = 0.5f * single terrain.Size

        let startPosition = Vector3(0.0f, 10.0f, -(single terrain.Size) / 2.0f)

        camera <- FreeCamera(startPosition, 0.0f, 0.0f)
        Mouse.SetPosition(this.Window.ClientBounds.Width / 2, this.Window.ClientBounds.Height / 2)
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), this.Window, originalMouseState, 0, 0)

        waterVertices <-
            [|
                VertexPositionTexture(Vector3(-halfSize, waterHeight, -halfSize), new Vector2(0.0f, 0.0f));
                VertexPositionTexture(Vector3( halfSize, waterHeight, -halfSize), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3(-halfSize, waterHeight,  halfSize), new Vector2(0.0f, 1.0f));

                VertexPositionTexture(Vector3( halfSize, waterHeight, -halfSize), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3( halfSize, waterHeight,  halfSize), new Vector2(1.0f, 1.0f));
                VertexPositionTexture(Vector3(-halfSize, waterHeight,  halfSize), new Vector2(0.0f, 1.0f));
            |]


        debugVertices <-
            [|
                VertexPositionTexture(Vector3(-0.9f, 0.5f, 0.0f), new Vector2(0.0f, 0.0f));
                VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 1.0f));
                VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 0.0f));

                VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 1.0f));
                VertexPositionTexture(Vector3(-0.5f, 0.9f, 0.0f), new Vector2(1.0f, 1.0f));
            |]

        cloudMap <- this.Content.Load<Texture2D>("cloudMap_0")
        skyDome <- this.Content.Load<Model>("dome")
        skyDome.Meshes.[0].MeshParts.[0].Effect <- effect.Clone()

    override this.Update(gameTime) =
        let time = float32 gameTime.TotalGameTime.TotalSeconds

        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), this.Window)

        if input.Quit then this.Exit()

        camera <- camera.Updated(input, time)

        view <- camera.ViewMatrix

        let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y + 2.0f * waterHeight, camera.Position.Z)
        let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y + 2.0f * waterHeight, camera.LookAt.Z)
        let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
        reflectionView <- Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

        do base.Update(gameTime)

    member this.DrawRefractionMap =
        let clipPlane = Vector4(Vector3.Down, waterHeight - 0.001f)
        device.SetRenderTarget(refractionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
        this.DrawTerrain view clipPlane
        device.SetRenderTarget(null)

    member this.DrawReflectionMap =
        let clipPlane = Vector4(Vector3.Up, waterHeight + 0.001f)
        device.SetRenderTarget(reflectionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0)
        this.DrawSkyDome reflectionView world
        this.DrawTerrain reflectionView clipPlane
        device.SetRenderTarget(null)

    override this.Draw(gameTime) =
        let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

        this.DrawRefractionMap
        this.DrawReflectionMap
        do device.Clear(Color.CornflowerBlue)
        this.DrawSkyDome view world
        this.DrawTerrain view noClipPlane
        this.DrawWater time
        //this.DrawDebug refractionRenderTarget
        do base.Draw(gameTime)

    member this.DrawTerrain (viewMatrix: Matrix) (clipPlane: Vector4) =
        effect.CurrentTechnique <- effect.Techniques.["TexturedClipped"]
        effect.Parameters.["xWorld"].SetValue(world)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xTexture"].SetValue(grassTexture)
        effect.Parameters.["xClipPlane"].SetValue(clipPlane)
        effect.Parameters.["xEnableLighting"].SetValue(true)
        effect.Parameters.["xAmbient"].SetValue(0.5f)

//        let state = new RasterizerState()
//        state.FillMode <- FillMode.WireFrame
//        device.RasterizerState <- state

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3)
            )

    member this.DrawWater time =
        effect.CurrentTechnique <- effect.Techniques.["Water"]
        effect.Parameters.["xWorld"].SetValue(world)
        effect.Parameters.["xView"].SetValue(view)
        effect.Parameters.["xReflectionView"].SetValue(reflectionView)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xCamPos"].SetValue(camera.Position)
        effect.Parameters.["xReflectionMap"].SetValue(reflectionRenderTarget)
        effect.Parameters.["xRefractionMap"].SetValue(refractionRenderTarget)
        effect.Parameters.["xWaterBumpMap"].SetValue(waterBumpMap)
        effect.Parameters.["xWaveLength"].SetValue(0.1f)
        effect.Parameters.["xWaveHeight"].SetValue(0.2f)
        effect.Parameters.["xTime"].SetValue(time)
        effect.Parameters.["xWindForce"].SetValue(0.001f)
        effect.Parameters.["xWindDirection"].SetValue(windDirection)

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, waterVertices, 0, waterVertices.Length / 3)
            )

    member this.DrawDebug (texture: Texture2D) =
        effect.CurrentTechnique <- effect.Techniques.["Debug"]
        effect.Parameters.["xDebugTexture"].SetValue(texture)

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, debugVertices, 0, debugVertices.Length / 3)
            )

    member this.DrawSkyDome (viewMatrix: Matrix) (world: Matrix) =
        device.DepthStencilState <- DepthStencilState.DepthRead

        let modelTransforms = Array.zeroCreate<Matrix> skyDome.Bones.Count
        skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms)
 
        let wMatrix = world * Matrix.CreateTranslation(0.0f, -0.3f, 0.0f) * Matrix.CreateScale(1000.0f) * Matrix.CreateTranslation(camera.Position)

        skyDome.Meshes |> Seq.iter
            (fun mesh ->
            mesh.Effects |> Seq.iter
                (fun effect ->
                    let worldMatrix = modelTransforms.[mesh.ParentBone.Index] * wMatrix
                    effect.CurrentTechnique <- effect.Techniques.["Textured"]
                    effect.Parameters.["xWorld"].SetValue(worldMatrix)
                    effect.Parameters.["xView"].SetValue(viewMatrix)
                    effect.Parameters.["xProjection"].SetValue(projection)
                    effect.Parameters.["xTexture"].SetValue(cloudMap)
                    effect.Parameters.["xEnableLighting"].SetValue(false)
                    mesh.Draw()
                )
            )

        device.DepthStencilState <- DepthStencilState.Default
