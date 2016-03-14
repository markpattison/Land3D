namespace Game1

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Sphere
open FreeCamera
open Input
open Terrain
open Environment

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable effect = Unchecked.defaultof<Effect>
    let mutable environment = Unchecked.defaultof<Environment>
    let mutable skyFromAtmosphere = Unchecked.defaultof<Effect>
    let mutable groundFromAtmosphere = Unchecked.defaultof<Effect>
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
    let mutable lightDirection = Unchecked.defaultof<Vector3>
    let mutable refractionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable reflectionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable noClipPlane = Unchecked.defaultof<Vector4>
    let mutable camera = Unchecked.defaultof<FreeCamera>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    let mutable perlinTexture3D = Unchecked.defaultof<Texture3D>
    let mutable sphereVertices = Unchecked.defaultof<VertexPositionNormal[]>
    let mutable sphereIndices = Unchecked.defaultof<int[]>
    do graphics.PreferredBackBufferWidth <- 640 //1440
    do graphics.PreferredBackBufferHeight <- 480 //900
    do graphics.IsFullScreen <- false //true
    do graphics.ApplyChanges()
    do base.Content.RootDirectory <- "Content"

    let createTerrain =
        terrain <- Terrain 128
        do terrain.DeformCircularFaults 300 2.0f 20.0f 100.0f
        do terrain.Normalize 0.5f 2.0f
        do terrain.Stretch 2.5f
        do terrain.Normalize -5.0f 10.0f
        vertices <- GetVertices terrain
        indices <- GetIndices terrain.Size

    override _this.Initialize() =
        device <- base.GraphicsDevice

        base.Initialize()
        ()

    override _this.LoadContent() =
        Sphere.Icosahedron |> ignore

        environment <-
            {
                Atmosphere =
                    {
                        InnerRadius = 10000.0f;
                        OuterRadius = 10250.0f;
                        ScaleDepth = 0.25f;
                        KR = 0.0025f;
                        KM = 0.0010f;
                        ESun = 20.0f;
                        G = -0.95f;
                        Wavelengths = Vector3(0.650f, 0.570f, 0.440f);
                    };
                Water =
                    {
                        WaterHeight = 0.0f;
                        WindDirection = Vector3(0.5f, 0.0f, 0.0f);
                        WindForce = 0.0015f;
                        WaveLength = 0.1f;
                        WaveHeight = 0.2f;
                    };
            }

        createTerrain
        world <- Matrix.Identity
        projection <- Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 1000.0f)

        grassTexture <- _this.Content.Load<Texture2D>("Textures/grass")
        effect <- _this.Content.Load<Effect>("Effects/effects")
        skyFromAtmosphere <- _this.Content.Load<Effect>("Effects/skyFromAtmosphere")
        groundFromAtmosphere <- _this.Content.Load<Effect>("Effects/groundFromAtmosphere")

        let dir = Vector3(0.0f, -0.5f, -1.0f)
        dir.Normalize()
        lightDirection <- dir

        let pp = device.PresentationParameters
        refractionRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24)
        reflectionRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24)
        noClipPlane <- Vector4.Zero

        let waterSize = 5.0f * single terrain.Size

        let startPosition = Vector3(0.0f, 10.0f, -(single terrain.Size) / 2.0f)

        camera <- FreeCamera(startPosition, 0.0f, 0.0f)
        Mouse.SetPosition(_this.Window.ClientBounds.Width / 2, _this.Window.ClientBounds.Height / 2)
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), _this.Window, originalMouseState, 0, 0)

        waterVertices <-
            [|
                VertexPositionTexture(Vector3(-waterSize, environment.Water.WaterHeight, -waterSize), new Vector2(0.0f, 0.0f));
                VertexPositionTexture(Vector3( waterSize, environment.Water.WaterHeight, -waterSize), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3(-waterSize, environment.Water.WaterHeight,  waterSize), new Vector2(0.0f, 1.0f));

                VertexPositionTexture(Vector3( waterSize, environment.Water.WaterHeight, -waterSize), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3( waterSize, environment.Water.WaterHeight,  waterSize), new Vector2(1.0f, 1.0f));
                VertexPositionTexture(Vector3(-waterSize, environment.Water.WaterHeight,  waterSize), new Vector2(0.0f, 1.0f));
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

        // perlin noise texture

        perlinTexture3D <- new Texture3D(device, 16, 16, 16, false, SurfaceFormat.Color)
        let random = new Random()

        let randomVectorColour x =
            let v = Vector3(single (random.NextDouble() * 2.0 - 1.0),
                            single (random.NextDouble() * 2.0 - 1.0),
                            single (random.NextDouble() * 2.0 - 1.0))
            v.Normalize()
            Color(v)

        let randomVectors = Array.init (16 * 16 * 16) randomVectorColour
        let sphere = Sphere.create 4

        let (sphereVerts, sphereInds) = Sphere.getVerticesAndIndices Smooth InwardFacing sphere
        sphereVertices <- sphereVerts
        sphereIndices <- sphereInds

        perlinTexture3D.SetData<Color>(randomVectors)


    override _this.Update(gameTime) =
        let time = float32 gameTime.TotalGameTime.TotalSeconds

        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), _this.Window)

        if input.Quit then _this.Exit()

        camera <- camera.Updated(input, time)

        view <- camera.ViewMatrix

        let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y + 2.0f * environment.Water.WaterHeight, camera.Position.Z)
        let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y + 2.0f * environment.Water.WaterHeight, camera.LookAt.Z)
        let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
        reflectionView <- Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

        if input.PageDown then lightDirection <- Vector3.Transform(lightDirection, Matrix.CreateRotationX(0.003f))
        if input.PageUp then lightDirection <- Vector3.Transform(lightDirection, Matrix.CreateRotationX(-0.003f))

        do base.Update(gameTime)

    member _this.DrawRefractionMap =
        let clipPlane = Vector4(Vector3.Down, environment.Water.WaterHeight - 0.00001f)
        device.SetRenderTarget(refractionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
        _this.DrawTerrain view clipPlane
        device.SetRenderTarget(null)

    member _this.DrawReflectionMap =
        let clipPlane = Vector4(Vector3.Up, environment.Water.WaterHeight + 0.00001f)
        device.SetRenderTarget(reflectionRenderTarget)
        device.Clear(ClearOptions.Target ||| ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0)
        _this.DrawSkyDome reflectionView world
        _this.DrawTerrain reflectionView clipPlane
        device.SetRenderTarget(null)

    override _this.Draw(gameTime) =
        let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

        _this.DrawRefractionMap
        _this.DrawReflectionMap
        do device.Clear(Color.Black)
        _this.DrawSkyDome view world
        _this.DrawTerrain view noClipPlane
        _this.DrawWater time
        //_this.DrawDebug refractionRenderTarget
        do base.Draw(gameTime)

    member _this.DrawTerrain (viewMatrix: Matrix) (clipPlane: Vector4) =
        groundFromAtmosphere.CurrentTechnique <- groundFromAtmosphere.Techniques.["GroundFromAtmosphere"]
        groundFromAtmosphere.Parameters.["xWorld"].SetValue(world)
        groundFromAtmosphere.Parameters.["xView"].SetValue(viewMatrix)
        groundFromAtmosphere.Parameters.["xProjection"].SetValue(projection)
        groundFromAtmosphere.Parameters.["xCameraPosition"].SetValue(camera.Position)
        groundFromAtmosphere.Parameters.["xLightDirection"].SetValue(lightDirection)
        groundFromAtmosphere.Parameters.["xTexture"].SetValue(grassTexture)
        groundFromAtmosphere.Parameters.["xClipPlane"].SetValue(clipPlane)
        groundFromAtmosphere.Parameters.["xAmbient"].SetValue(0.5f)

        environment.Atmosphere.ApplyToEffect groundFromAtmosphere

//        let state = new RasterizerState()
//        state.FillMode <- FillMode.WireFrame
//        device.RasterizerState <- state

        groundFromAtmosphere.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3)
            )

    member _this.DrawSphere (viewMatrix: Matrix) =
        let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.0f, 0.0f), Matrix.CreateScale(10.0f))
        effect.CurrentTechnique <- effect.Techniques.["Coloured"]
        effect.Parameters.["xWorld"].SetValue(sphereWorld)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xAmbient"].SetValue(0.5f)

//        let state = new RasterizerState()
//        state.FillMode <- FillMode.WireFrame
//        device.RasterizerState <- state
        
        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, sphereVertices, 0, sphereVertices.Length, sphereIndices, 0, sphereIndices.Length / 3)
            )

    member _this.DrawWater time =
        effect.CurrentTechnique <- effect.Techniques.["Water"]
        effect.Parameters.["xWorld"].SetValue(world)
        effect.Parameters.["xView"].SetValue(view)
        effect.Parameters.["xReflectionView"].SetValue(reflectionView)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xCamPos"].SetValue(camera.Position)
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

    member _this.DrawDebug (texture: Texture2D) =
        effect.CurrentTechnique <- effect.Techniques.["Debug"]
        effect.Parameters.["xDebugTexture"].SetValue(texture)

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, debugVertices, 0, debugVertices.Length / 3)
            )

    member _this.DrawSkyDome (viewMatrix: Matrix) (world: Matrix) =
        device.DepthStencilState <- DepthStencilState.DepthRead

        let wMatrix = world * Matrix.CreateScale(500.0f) * Matrix.CreateTranslation(camera.Position)

        skyFromAtmosphere.CurrentTechnique <- skyFromAtmosphere.Techniques.["SkyFromAtmosphere"]
        skyFromAtmosphere.Parameters.["xWorld"].SetValue(wMatrix)
        skyFromAtmosphere.Parameters.["xView"].SetValue(viewMatrix)
        skyFromAtmosphere.Parameters.["xProjection"].SetValue(projection)
        skyFromAtmosphere.Parameters.["xCameraPosition"].SetValue(camera.Position)
        skyFromAtmosphere.Parameters.["xLightDirection"].SetValue(lightDirection)

        environment.Atmosphere.ApplyToEffect skyFromAtmosphere

        skyFromAtmosphere.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, sphereVertices, 0, sphereVertices.Length, sphereIndices, 0, sphereIndices.Length / 3)
            )
        device.DepthStencilState <- DepthStencilState.Default
