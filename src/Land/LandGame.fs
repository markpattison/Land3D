namespace Game1

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open Sphere
open FreeCamera
open Input
open Terrain
open Water
open Sky
open Game1.Types

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable gameContent = Unchecked.defaultof<Content>
    let mutable gameState = Unchecked.defaultof<State>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
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

        let pp = device.PresentationParameters

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
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 5000.0f)
            HdrRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
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
        Draw.draw gameTime device gameState gameContent

        do base.Draw(gameTime)

