module Land.Sky

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Effects
open Sphere

type Sky =
    {
        Vertices: VertexPositionNormal[]
        Indices: int[]
        Effect: Effect
        Atmosphere: Atmosphere.Atmosphere
        Device: GraphicsDevice
    }

let prepare effect atmosphere device =

    let skySphere = Sphere.create 4
    let (skySphereVertices, skySphereIndices) = Sphere.getVerticesAndIndices Smooth InwardFacing Concentrated skySphere

    {
        Vertices = skySphereVertices
        Indices = skySphereIndices
        Effect = effect
        Atmosphere = atmosphere
        Device = device
    }

let drawSkyDome sky (world: Matrix) (lightDirection: Vector3) (cameraPosition: Vector3) (view: Matrix) =
    
    let wireframe = false

    let device = sky.Device
    let effect = sky.Effect

    device.DepthStencilState <- DepthStencilState.DepthRead
    let rs = device.RasterizerState
        
    let dot = Vector3.Dot(Vector3.UnitY, -lightDirection)
    let rot =
        if dot > 0.99999f then
            Matrix.Identity
        else if dot < -0.99999f then
            Matrix.CreateRotationX(MathHelper.Pi)
        else
            let cross = Vector3.Cross(Vector3.UnitY, -lightDirection)
            let crossLength = cross.Length()
            let angle = single(Math.Atan2(float crossLength, float dot))
            cross.Normalize()
            Matrix.CreateFromAxisAngle(cross, angle)
    
    let skyWorld = world * Matrix.CreateScale(20000.0f) * rot * Matrix.CreateTranslation(cameraPosition)

    effect.CurrentTechnique <- effect.Techniques.["SkyFromAtmosphere"]
    effect.Parameters.["xWorld"].SetValue(skyWorld)
    effect.Parameters.["xView"].SetValue(view)
    effect.Parameters.["xCameraPosition"].SetValue(cameraPosition)
    effect.Parameters.["xLightDirection"].SetValue(lightDirection)

    if wireframe then
        let rs' = new RasterizerState()
        rs'.FillMode <- FillMode.WireFrame
        device.RasterizerState <- rs'

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, sky.Vertices, 0, sky.Vertices.Length, sky.Indices, 0, sky.Indices.Length / 3)
        )
    
    device.DepthStencilState <- DepthStencilState.Default
    device.RasterizerState <- rs
