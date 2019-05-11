namespace Game1.Types

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open FreeCamera
open Terrain
open EnvironmentParameters
open Water
open Sky

type Effects =
    {
        Effect: Effect;
        Hdr: Effect;
        SkyFromAtmosphere: Effect;
        GroundFromAtmosphere: Effect
    }

type Textures =
    {
        Grass: Texture2D;
        Rock: Texture2D;
        Sand: Texture2D;
        Snow: Texture2D;
    }

type Content =
    {
        SpriteBatch: SpriteBatch
        Effects: Effects
        Projection: Matrix
        HdrRenderTarget: RenderTarget2D
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
        Exiting: bool
    }

