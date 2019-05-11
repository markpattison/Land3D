module ContentLoader

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open EnvironmentParameters
open Terrain
open Sphere
open Water
open Sky
open Game1.Types

let loadEffects (content: Content.ContentManager) =
    {
        Effect = content.Load<Effect>("Effects/effects")
        Hdr = content.Load<Effect>("Effects/hdr")
        SkyFromAtmosphere = content.Load<Effect>("Effects/skyFromAtmosphere")
        GroundFromAtmosphere = content.Load<Effect>("Effects/groundFromAtmosphere")
    }

let loadTextures (content: Content.ContentManager) =
    {
        Grass = content.Load<Texture2D>("Textures/grass")
        Rock = content.Load<Texture2D>("Textures/rock")
        Sand = content.Load<Texture2D>("Textures/sand")
        Snow = content.Load<Texture2D>("Textures/snow")
    }

let loadEnvironment =
    {
        Atmosphere =
            {
                InnerRadius = 100000.0f;
                OuterRadius = 102500.0f;
                ScaleDepth = 0.25f;
                KR = 0.0025f;
                KM = 0.0010f;
                ESun = 20.0f;
                G = -0.95f;
                Wavelengths = Vector3(0.650f, 0.570f, 0.440f);
            };
        Water =
            {
                WindDirection = Vector2(0.0f, 1.0f);
                WindForce = 0.0015f;
                WaveLength = 0.1f;
                WaveHeight = 0.8f;
                Opacity = 4.0f;
            };
    }

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

let load (device: GraphicsDevice) (content: Content.ContentManager) =
        let environment = loadEnvironment

        let terrain, vertices, indices, minMaxTerrainHeight = createTerrain

        let pp = device.PresentationParameters

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
        let random = new System.Random()

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

        let effects = loadEffects content

        {
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
            Textures = loadTextures content
            PerlinTexture3D = perlinTexture3D
            SphereVertices = sphereVerts
            SphereIndices = sphereInds
        }