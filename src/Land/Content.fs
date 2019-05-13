namespace Land

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Effects
open Textures
open Terrain
open Water
open Sky
open Atmosphere

type Content =
    {
        SpriteBatch: SpriteBatch
        Effects: Effects
        Projection: Matrix
        HdrRenderTarget: RenderTarget2D
        Atmosphere: Atmosphere
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
