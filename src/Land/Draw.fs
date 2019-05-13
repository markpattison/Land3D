module Land.Draw

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal
open Effects
open Textures

let drawTerrain (x: bool) (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) state content =
    let effect = content.Effects.GroundFromAtmosphere

    effect.CurrentTechnique <- effect.Techniques.["GroundFromAtmosphere"]
    effect.Parameters.["xWorld"].SetValue(worldMatrix)
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(content.Projection)
    effect.Parameters.["xCameraPosition"].SetValue(state.Camera.Position)
    effect.Parameters.["xLightDirection"].SetValue(state.LightDirection)
    effect.Parameters.["xGrassTexture"].SetValue(content.Textures.Grass)
    effect.Parameters.["xRockTexture"].SetValue(content.Textures.Rock)
    effect.Parameters.["xSandTexture"].SetValue(content.Textures.Sand)
    effect.Parameters.["xSnowTexture"].SetValue(content.Textures.Snow)
    effect.Parameters.["xClipPlane"].SetValue(clipPlane)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
    effect.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue(x)
    effect.Parameters.["xMinMaxHeight"].SetValue(content.MinMaxTerrainHeight)
    effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)
    effect.Parameters.["xRandomTexture3D"].SetValue(content.PerlinTexture3D)

    Atmosphere.applyToEffect content.Atmosphere effect
    Water.applyToGroundEffect content.Water.WaterParameters effect

    device.BlendState <- BlendState.Opaque

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, content.Vertices, 0, content.Vertices.Length, content.Indices, 0, content.Indices.Length / 3)
        )

let drawSphere (viewMatrix: Matrix) (device: GraphicsDevice) state content =
    let effect = content.Effects.GroundFromAtmosphere

    let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.5f, 0.0f), Matrix.CreateScale(10.0f))
    effect.CurrentTechnique <- effect.Techniques.["Coloured"]
    effect.Parameters.["xWorld"].SetValue(sphereWorld)
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(content.Projection)
    effect.Parameters.["xLightDirection"].SetValue(state.LightDirection)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
        
    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, content.SphereVertices, 0, content.SphereVertices.Length, content.SphereIndices, 0, content.SphereIndices.Length / 3)
        )

let drawDebug (texture: Texture2D) (device: GraphicsDevice) content =
    let effect = content.Effects.Effect
    effect.CurrentTechnique <- effect.Techniques.["Debug"]
    effect.Parameters.["xDebugTexture"].SetValue(texture)

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, content.DebugVertices, 0, content.DebugVertices.Length / 3)
        )

let drawApartFromSky (device: GraphicsDevice) state content x (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4)  =
    drawTerrain x viewMatrix worldMatrix clipPlane device state content
    drawSphere viewMatrix device state content

let draw (gameTime: GameTime) (device: GraphicsDevice) state content =
    let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

    let view = state.Camera.ViewMatrix
    let world = Matrix.Identity

    let waterReflectionView = Water.prepareFrameAndReturnReflectionView content.Water view world state.Camera (drawApartFromSky device state content) (Sky.drawSkyDome content.Sky world content.Projection state.LightDirection state.Camera.Position)

    device.SetRenderTarget(content.HdrRenderTarget)

    do device.Clear(Color.Black)
    drawApartFromSky device state content false view world Vector4.Zero // no clip plane
    Water.drawWater content.Water time world view content.Projection state.LightDirection state.Camera waterReflectionView
    Sky.drawSkyDome content.Sky world content.Projection state.LightDirection state.Camera.Position view
    
    //drawDebug content.Water.RefractionRenderTarget device content

    device.SetRenderTarget(null)

    let effect = content.Effects.Hdr
    effect.CurrentTechnique <- effect.Techniques.["Plain"]

    content.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
        SamplerState.LinearClamp, DepthStencilState.Default, 
        RasterizerState.CullNone, effect)
 
    content.SpriteBatch.Draw(content.HdrRenderTarget, new Rectangle(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight), Color.White);
 
    content.SpriteBatch.End();
