namespace Game1

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Sphere
open FreeCamera
open Input
open Terrain
open ContentLoader
open Environment
open Water

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>
    let mutable effects = Unchecked.defaultof<Effects>
    let mutable environment = Unchecked.defaultof<Environment>
    let mutable water = Unchecked.defaultof<Water>
    let mutable vertices = Unchecked.defaultof<VertexPositionNormalTexture[]>
    let mutable debugVertices = Unchecked.defaultof<VertexPositionTexture[]>
    let mutable indices = Unchecked.defaultof<int[]>
    let mutable world = Unchecked.defaultof<Matrix>
    let mutable view = Unchecked.defaultof<Matrix>
    let mutable reflectionView = Unchecked.defaultof<Matrix>
    let mutable projection = Unchecked.defaultof<Matrix>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
    let mutable terrain = Unchecked.defaultof<Terrain>
    let mutable textures = Unchecked.defaultof<Textures>
    let mutable lightDirection = Unchecked.defaultof<Vector3>
    let mutable refractionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable reflectionRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable hdrRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable noClipPlane = Unchecked.defaultof<Vector4>
    let mutable camera = Unchecked.defaultof<FreeCamera>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    let mutable perlinTexture3D = Unchecked.defaultof<Texture3D>
    let mutable sphereVertices = Unchecked.defaultof<VertexPositionNormal[]>
    let mutable sphereIndices = Unchecked.defaultof<int[]>
    do graphics.PreferredBackBufferWidth <- 600
    do graphics.PreferredBackBufferHeight <- 400
    do graphics.IsFullScreen <- false
    do graphics.ApplyChanges()
    do base.Content.RootDirectory <- "Content"

    let createTerrain =
        terrain <- Terrain 128
        do terrain.DeformCircularFaults 300 2.0f 20.0f 100.0f
        do terrain.Normalize 0.5f 2.0f
        do terrain.Stretch 2.5f
        do terrain.Normalize -10.0f 10.0f
        vertices <- GetVertices terrain
        indices <- GetIndices terrain.Size

    override _this.Initialize() =
        device <- base.GraphicsDevice

        base.Initialize()
        ()

    override _this.LoadContent() =
        environment <- ContentLoader.loadEnvironment

        createTerrain
        world <- Matrix.Identity
        projection <- Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 5000.0f)

        effects <- ContentLoader.loadEffects _this
        textures <- ContentLoader.loadTextures _this

        let dir = Vector3(0.0f, -0.5f, -1.0f)
        dir.Normalize()
        lightDirection <- dir

        let pp = device.PresentationParameters
        hdrRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
        noClipPlane <- Vector4.Zero

        spriteBatch <- new SpriteBatch(device)

        let startPosition = Vector3(0.0f, 10.0f, -(single terrain.Size) / 2.0f)

        camera <- FreeCamera(startPosition, 0.0f, 0.0f)
        Mouse.SetPosition(_this.Window.ClientBounds.Width / 2, _this.Window.ClientBounds.Height / 2)
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), _this.Window, originalMouseState, 0, 0)

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
        perlinTexture3D.SetData<Color>(randomVectors)

        let sphere = Sphere.create 4

        let (sphereVerts, sphereInds) = Sphere.getVerticesAndIndices Smooth InwardFacing sphere
        sphereVertices <- sphereVerts
        sphereIndices <- sphereInds

        water <- new Water(effects.GroundFromAtmosphere, perlinTexture3D, environment, device)

    override _this.Update(gameTime) =
        let time = float32 gameTime.TotalGameTime.TotalSeconds

        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), _this.Window)

        if input.Quit then _this.Exit()

        camera <- camera.Updated(input, time)

        view <- camera.ViewMatrix

        let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
        let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
        let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)
        reflectionView <- Matrix.CreateLookAt(reflectionCameraAt, reflectionCameraLookAt, invUpVector)

        if input.PageDown then lightDirection <- Vector3.Transform(lightDirection, Matrix.CreateRotationX(0.003f))
        if input.PageUp then lightDirection <- Vector3.Transform(lightDirection, Matrix.CreateRotationX(-0.003f))

        do base.Update(gameTime)

    override _this.Draw(gameTime) =
        let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

        water.Prepare world view camera _this.DrawTerrain _this.DrawSkyDome

        device.SetRenderTarget(hdrRenderTarget)

        do device.Clear(Color.Black)
        _this.DrawTerrain view noClipPlane
        water.DrawWater time world view projection lightDirection camera
        _this.DrawSkyDome view world
        //_this.DrawDebug refractionRenderTarget

        device.SetRenderTarget(null)

        let effect = effects.Hdr
        effect.CurrentTechnique <- effect.Techniques.["Plain"]

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
            SamplerState.LinearClamp, DepthStencilState.Default, 
            RasterizerState.CullNone, effect)
 
        spriteBatch.Draw(hdrRenderTarget, new Rectangle(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight), Color.White);
 
        spriteBatch.End();

        do base.Draw(gameTime)

    member _this.DrawTerrain (viewMatrix: Matrix) (clipPlane: Vector4) =
        let effect = effects.GroundFromAtmosphere

        effect.CurrentTechnique <- effect.Techniques.["GroundFromAtmosphere"]
        effect.Parameters.["xWorld"].SetValue(world)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xCameraPosition"].SetValue(camera.Position)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xTexture"].SetValue(textures.Grass)
        effect.Parameters.["xClipPlane"].SetValue(clipPlane)
        effect.Parameters.["xAmbient"].SetValue(0.5f)

        environment.Atmosphere.ApplyToEffect effect
        environment.Water.ApplyToGroundEffect effect

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3)
            )

    member _this.DrawSphere (viewMatrix: Matrix) =
        let effect = effects.Effect

        let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.0f, 0.0f), Matrix.CreateScale(10.0f))
        effect.CurrentTechnique <- effect.Techniques.["Coloured"]
        effect.Parameters.["xWorld"].SetValue(sphereWorld)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)
        effect.Parameters.["xAmbient"].SetValue(0.5f)
        
        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, sphereVertices, 0, sphereVertices.Length, sphereIndices, 0, sphereIndices.Length / 3)
            )

    member _this.DrawDebug (texture: Texture2D) =
        let effect = effects.Effect
        effect.CurrentTechnique <- effect.Techniques.["Debug"]
        effect.Parameters.["xDebugTexture"].SetValue(texture)

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, debugVertices, 0, debugVertices.Length / 3)
            )

    member _this.DrawSkyDome (viewMatrix: Matrix) (world: Matrix) =
        let effect = effects.SkyFromAtmosphere

        device.DepthStencilState <- DepthStencilState.DepthRead
        let wMatrix = world * Matrix.CreateScale(20000.0f) * Matrix.CreateTranslation(camera.Position)

        effect.CurrentTechnique <- effect.Techniques.["SkyFromAtmosphere"]
        effect.Parameters.["xWorld"].SetValue(wMatrix)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xCameraPosition"].SetValue(camera.Position)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)

        environment.Atmosphere.ApplyToEffect effect

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, sphereVertices, 0, sphereVertices.Length, sphereIndices, 0, sphereIndices.Length / 3)
            )
        device.DepthStencilState <- DepthStencilState.Default
