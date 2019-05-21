module Land.ContentLoader

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

open Terrain
open Sphere
open Atmosphere
open Water

let waterParameters =
    {
        WindDirection = Vector2(0.0f, 1.0f)
        WindForce = 0.0015f
        WaveLength = 0.1f
        WaveHeight = 0.8f
        Opacity = 4.0f
    }

let atmosphereParameters =
    {
        InnerRadius = 100000.0f
        OuterRadius = 102500.0f
        ScaleDepth = 0.25f
        KR = 0.0025f
        KM = 0.0010f
        ESun = 20.0f
        G = -0.95f
        Wavelengths = Vector3(0.650f, 0.570f, 0.440f)
    }

let createTerrain =
    let terrain = Terrain 256
    terrain.DeformCircularFaults 500 2.0f 20.0f 100.0f
    terrain.Normalize 0.5f 2.0f
    terrain.Stretch 4.0f
    terrain.Normalize -5.0f 25.0f
    let vertices = GetVertices terrain
    let indices = GetIndices terrain.Size
    let minMaxTerrainHeight =
        let (min, max) = terrain.MinMax()
        new Vector2(min, max)
    (terrain, vertices, indices, minMaxTerrainHeight)

let load (device: GraphicsDevice) (contentManager: ContentManager) =
    contentManager.RootDirectory <- "Content"

    let terrain, vertices, indices, minMaxTerrainHeight = createTerrain

    let pp = device.PresentationParameters

    let debugVertices =
        [|
            VertexPositionTexture(Vector3(-0.9f, 0.5f, 0.0f), new Vector2(0.0f, 1.0f));
            VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 0.0f));
            VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 1.0f));

            VertexPositionTexture(Vector3(-0.5f, 0.5f, 0.0f), new Vector2(1.0f, 1.0f));
            VertexPositionTexture(Vector3(-0.9f, 0.9f, 0.0f), new Vector2(0.0f, 0.0f));
            VertexPositionTexture(Vector3(-0.5f, 0.9f, 0.0f), new Vector2(1.0f, 0.0f));
        |]
    
    let perlinTexture3D = PerlinNoiseTexture3D.create device 16

    let sphere = Sphere.create 2

    let (sphereVerts, sphereInds) = Sphere.getVerticesAndIndices Smooth OutwardFacing Even sphere

    let projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 5000.0f)

    let atmosphere = atmosphereParameters |> Atmosphere.prepare

    let effects = Effects.load contentManager atmosphere waterParameters projection

    {
        SpriteBatch = new SpriteBatch(device)
        Effects = effects
        LightsProjection = Matrix.CreateOrthographic(200.0f, 200.0f, 10.0f, 1000.0f)
        HdrRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24)
        ShadowMap = new RenderTarget2D(device, 3200, 3200, false, SurfaceFormat.Single, DepthFormat.Depth16)
        Atmosphere = atmosphere
        Sky = Sky.prepare effects.SkyFromAtmosphere atmosphere device
        Water = Water.prepare effects.GroundFromAtmosphere perlinTexture3D waterParameters device 3000.0f
        Vertices = vertices
        DebugVertices = debugVertices
        Indices = indices
        Terrain = terrain
        MinMaxTerrainHeight = minMaxTerrainHeight
        PerlinTexture3D = perlinTexture3D
        SphereVertices = sphereVerts
        SphereIndices = sphereInds
    }