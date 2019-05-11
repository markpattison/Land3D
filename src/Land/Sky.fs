module Sky

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Sphere
open EnvironmentParameters
open FreeCamera

type Sky(effect: Effect, environment: EnvironmentParameters, device: GraphicsDevice) as _this =
    let skySphere = Sphere.create 4
    let (skySphereVertices, skySphereIndices) = Sphere.getVerticesAndIndices Smooth InwardFacing Concentrated skySphere

    member _this.DrawSkyDome (world: Matrix) (projection: Matrix) (lightDirection: Vector3) (camera: FreeCamera) (viewMatrix: Matrix) =
        device.DepthStencilState <- DepthStencilState.DepthRead
        //let wMatrix = world * Matrix.CreateScale(20000.0f) * Matrix.CreateTranslation(camera.Position)
        
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
        let wMatrix2 = world * Matrix.CreateScale(20000.0f) * rot * Matrix.CreateTranslation(camera.Position)

        effect.CurrentTechnique <- effect.Techniques.["SkyFromAtmosphere"]
        effect.Parameters.["xWorld"].SetValue(wMatrix2)
        effect.Parameters.["xView"].SetValue(viewMatrix)
        effect.Parameters.["xProjection"].SetValue(projection)
        effect.Parameters.["xCameraPosition"].SetValue(camera.Position)
        effect.Parameters.["xLightDirection"].SetValue(lightDirection)

        environment.Atmosphere.ApplyToEffect effect

    //    let rs = device.RasterizerState
    //    let rs' = new RasterizerState()
    //    rs'.FillMode <- FillMode.WireFrame
    //    device.RasterizerState <- rs'

        effect.CurrentTechnique.Passes |> Seq.iter
            (fun pass ->
                pass.Apply()
                device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, skySphereVertices, 0, skySphereVertices.Length, skySphereIndices, 0, skySphereIndices.Length / 3)
            )
        device.DepthStencilState <- DepthStencilState.Default

    //    device.RasterizerState <- rs
