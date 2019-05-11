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
open EnvironmentParameters
open Water
open Sky

type Content =
    {
        SpriteBatch: SpriteBatch
        Effects: Effects
        Environment: EnvironmentParameters
        Sky: Sky
        Water: Water
        Vertices: VertexPositionNormalTexture[]
        DebugVertices: VertexPositionTexture[]
        Indices: int[]
        Terrain: Terrain
        MinMaxTerrainHeight: Vector2
        Textures: Textures
        PerlinTexture3D: Texture3D
        SphereVertices: VertexPositionNormal[]
        SphereIndices: int[]
    }

type State =
    {
        LightDirection: Vector3
        Camera: FreeCamera
    }

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable gameContent = Unchecked.defaultof<Content>
    let mutable gameState = Unchecked.defaultof<State>
    let mutable projection = Unchecked.defaultof<Matrix>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
    let mutable hdrRenderTarget = Unchecked.defaultof<RenderTarget2D>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    do graphics.GraphicsProfile <- GraphicsProfile.HiDef
    do graphics.PreferredBackBufferWidth <- 900
    do graphics.PreferredBackBufferHeight <- 700
    do graphics.IsFullScreen <- false
    do graphics.ApplyChanges()
    do base.Content.RootDirectory <- "Content"

    let createTerrain =
        let terrain = Terrain 256
        do terrain.DeformCircularFaults 500 2.0f 20.0f 100.0f
        do terrain.Normalize 0.5f 2.0f
        do terrain.Stretch 4.0f
        do terrain.Normalize -5.0f 25.0f
        let vertices = GetVertices terrain
        let indices = GetIndices terrain.Size
        let minMaxTerrainHeight =
            let (min, max) = terrain.MinMax()
            new Vector2(min, max)
        (terrain, vertices, indices, minMaxTerrainHeight)

    override _this.Initialize() =
        device <- base.GraphicsDevice

        base.Initialize()
        ()

    override _this.LoadContent() =
        let environment = ContentLoader.loadEnvironment

        let terrain, vertices, indices, minMaxTerrainHeight = createTerrain
        projection <- Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 5000.0f)

        let pp = device.PresentationParameters
        hdrRenderTarget <- new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)

        Mouse.SetPosition(_this.Window.ClientBounds.Width / 2, _this.Window.ClientBounds.Height / 2)
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), _this.Window, originalMouseState, 0, 0)

        let debugVertices =
            [|
                VertexPositionTexture(Vector3(-0.9f, 0.5f, 0.0f), new Vector2(0.0f, 0.0f));
                VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 1.0f));
                VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 0.0f));

                VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 0.0f));
                VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 1.0f));
                VertexPositionTexture(Vector3(-0.5f, 0.9f, 0.0f), new Vector2(1.0f, 1.0f));
            |]

        // perlin noise texture

        let perlinTexture3D = new Texture3D(device, 16, 16, 16, false, SurfaceFormat.Color)
        let random = new Random()

        let randomVectorColour x =
            let v = Vector3(single (random.NextDouble() * 2.0 - 1.0),
                            single (random.NextDouble() * 2.0 - 1.0),
                            single (random.NextDouble() * 2.0 - 1.0))
            v.Normalize()
            Color(v)

        let randomVectors = Array.init (16 * 16 * 16) randomVectorColour
        perlinTexture3D.SetData<Color>(randomVectors)

        let sphere = Sphere.create 2

        let (sphereVerts, sphereInds) = Sphere.getVerticesAndIndices Smooth OutwardFacing Even sphere

        let effects = ContentLoader.loadEffects _this

        gameContent <- {
            SpriteBatch = new SpriteBatch(device)
            Effects = effects
            Environment = environment
            Sky = Sky(effects.SkyFromAtmosphere, environment, device)
            Water = Water(effects.GroundFromAtmosphere, perlinTexture3D, environment, device)
            Vertices = vertices
            DebugVertices = debugVertices
            Indices = indices
            Terrain = terrain
            MinMaxTerrainHeight = minMaxTerrainHeight
            Textures = ContentLoader.loadTextures _this
            PerlinTexture3D = perlinTexture3D
            SphereVertices = sphereVerts
            SphereIndices = sphereInds
        }

        gameState <- {
            LightDirection = Vector3.Normalize(Vector3(0.0f, -0.5f, -1.0f))
            Camera = FreeCamera(Vector3(0.0f, 10.0f, -(single terrain.Size) / 2.0f), 0.0f, 0.0f)
        }

    override _this.Update(gameTime) =
        let time = float32 gameTime.TotalGameTime.TotalSeconds

        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), _this.Window)

        if input.Quit then _this.Exit()

        let camera = gameState.Camera.Updated(input, time)

        let reflectionCameraAt = Vector3(camera.Position.X, -camera.Position.Y, camera.Position.Z)
        let reflectionCameraLookAt = Vector3(camera.LookAt.X, -camera.LookAt.Y, camera.LookAt.Z)
        let invUpVector = Vector3.Cross(camera.RightDirection, reflectionCameraLookAt - reflectionCameraAt)

        let lightDirection =
            match input.PageDown, input.PageUp with
            | true, false -> Vector3.Transform(gameState.LightDirection, Matrix.CreateRotationX(0.003f))
            | false, true -> Vector3.Transform(gameState.LightDirection, Matrix.CreateRotationX(-0.003f))
            | _ -> gameState.LightDirection
        
        gameState <- {
            Camera = camera
            LightDirection = lightDirection
        }

        do base.Update(gameTime)

    override _this.Draw(gameTime) =
        let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

        let view = gameState.Camera.ViewMatrix
        let world = Matrix.Identity

        let waterReflectionView = gameContent.Water.Prepare view world gameState.Camera _this.DrawApartFromSky (gameContent.Sky.DrawSkyDome world projection gameState.LightDirection gameState.Camera)

        device.SetRenderTarget(hdrRenderTarget)

        do device.Clear(Color.Black)
        _this.DrawApartFromSky false view world Vector4.Zero // no clip plane
        gameContent.Water.DrawWater time world view projection gameState.LightDirection gameState.Camera waterReflectionView
        gameContent.Sky.DrawSkyDome world projection gameState.LightDirection gameState.Camera view
        //_this.DrawDebug perlinTexture3D

        device.SetRenderTarget(null)

        let effect = gameContent.Effects.Hdr
        effect.CurrentTechnique <- effect.Techniques.["Plain"]

        gameContent.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
            SamplerState.LinearClamp, DepthStencilState.Default, 
            RasterizerState.CullNone, effect)
 
        gameContent.SpriteBatch.Draw(hdrRenderTarget, new Rectangle(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight), Color.White);
 
        gameContent.SpriteBatch.End();

        do base.Draw(gameTime)

    member _this.DrawApartFromSky x (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) =
        _this.DrawTerrain x viewMatrix worldMatrix clipPlane
        _this.DrawSphere viewMatrix

    member _this.DrawTerrain (x: bool) (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) =
        let effect = gameContent.Effects.GroundFromAtmosphere

        effect.CurrentTechnique <- effect.Techniques.["GroundFromAtmosphere"]
        effect.Parameters.["xWorld"].SetValue(worldMatrix)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xCameraPosition"].SetValue(gameState.Camera.Position)
        effect.Parameters.["xLightDirection"].SetValue(gameState.LightDirection)
        effect.Parameters.["xGrassTexture"].SetValue(gameContent.Textures.Grass)
        effect.Parameters.["xRockTexture"].SetValue(gameContent.Textures.Rock)
        effect.Parameters.["xSandTexture"].SetValue(gameContent.Textures.Sand)
        effect.Parameters.["xSnowTexture"].SetValue(gameContent.Textures.Snow)
        effect.Parameters.["xClipPlane"].SetValue(clipPlane)
        effect.Parameters.["xAmbient"].SetValue(0.5f)
        effect.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue(x)
        effect.Parameters.["xMinMaxHeight"].SetValue(gameContent.MinMaxTerrainHeight)
        effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)
        effect.Parameters.["xRandomTexture3D"].SetValue(gameContent.PerlinTexture3D)

        gameContent.Environment.Atmosphere.ApplyToEffect effect
        gameContent.Environment.Water.ApplyToGroundEffect effect

        device.BlendState <- BlendState.Opaque

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, gameContent.Vertices, 0, gameContent.Vertices.Length, gameContent.Indices, 0, gameContent.Indices.Length / 3)
            )

    member _this.DrawSphere (viewMatrix: Matrix) =
        let effect = gameContent.Effects.GroundFromAtmosphere

        let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.5f, 0.0f), Matrix.CreateScale(10.0f))
        effect.CurrentTechnique <- effect.Techniques.["Coloured"]
        effect.Parameters.["xWorld"].SetValue(sphereWorld)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xLightDirection"].SetValue(gameState.LightDirection)
        effect.Parameters.["xAmbient"].SetValue(0.5f)
        
        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, gameContent.SphereVertices, 0, gameContent.SphereVertices.Length, gameContent.SphereIndices, 0, gameContent.SphereIndices.Length / 3)
            )

    member _this.DrawDebug (texture: Texture2D) =
        let effect = gameContent.Effects.Effect
        effect.CurrentTechnique <- effect.Techniques.["Debug"]
        effect.Parameters.["xDebugTexture"].SetValue(texture)

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, gameContent.DebugVertices, 0, gameContent.DebugVertices.Length / 3)
            )
