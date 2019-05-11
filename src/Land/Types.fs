namespace Game1.Types

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
    }

