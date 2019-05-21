module Land.Draw

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal

let drawTerrainVertices (effect: Effect) (worldMatrix: Matrix) (device: GraphicsDevice) content =
    effect.Parameters.["xWorld"].SetValue(worldMatrix)

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, content.Vertices, 0, content.Vertices.Length, content.Indices, 0, content.Indices.Length / 3)
        )

let drawTerrain (x: bool) (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) state content (lightViewProjection: Matrix) =
    let effect = content.Effects.GroundFromAtmosphere

    effect.CurrentTechnique <- effect.Techniques.["GroundFromAtmosphere"]
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(content.Projection)
    effect.Parameters.["xCameraPosition"].SetValue(state.Camera.Position)
    effect.Parameters.["xLightDirection"].SetValue(state.LightDirection)
    effect.Parameters.["xLightsViewProjection"].SetValue(lightViewProjection)
    effect.Parameters.["xShadowMap"].SetValue(content.ShadowMap)
    effect.Parameters.["xClipPlane"].SetValue(clipPlane)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
    effect.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue(x)
    effect.Parameters.["xMinMaxHeight"].SetValue(content.MinMaxTerrainHeight)
    effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)
    effect.Parameters.["xRandomTexture3D"].SetValue(content.PerlinTexture3D)

    device.BlendState <- BlendState.Opaque

    drawTerrainVertices effect worldMatrix device content

let drawSphereVertices (effect: Effect) (device: GraphicsDevice) content =
    let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.5f, 0.0f), Matrix.CreateScale(10.0f))

    effect.Parameters.["xWorld"].SetValue(sphereWorld)
        
    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, content.SphereVertices, 0, content.SphereVertices.Length, content.SphereIndices, 0, content.SphereIndices.Length / 3)
        )

let drawSphere (viewMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) state content (lightViewProjection: Matrix) =
    let effect = content.Effects.GroundFromAtmosphere

    effect.CurrentTechnique <- effect.Techniques.["Coloured"]
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(content.Projection)
    effect.Parameters.["xLightDirection"].SetValue(state.LightDirection)
    effect.Parameters.["xLightsViewProjection"].SetValue(lightViewProjection)
    effect.Parameters.["xShadowMap"].SetValue(content.ShadowMap)
    effect.Parameters.["xClipPlane"].SetValue(clipPlane)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
    
    drawSphereVertices effect device content

let drawDebug (texture: Texture2D) (device: GraphicsDevice) content (isShadow: bool) =
    let effect = content.Effects.Effect

    effect.CurrentTechnique <-
        if isShadow then effect.Techniques.["DebugShadow"] else effect.Techniques.["Debug"]
    
    effect.Parameters.["xDebugTexture"].SetValue(texture)

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, content.DebugVertices, 0, content.DebugVertices.Length / 3)
        )

let drawApartFromSky (device: GraphicsDevice) state content (lightViewProjection: Matrix) x (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) =
    drawTerrain x viewMatrix worldMatrix clipPlane device state content lightViewProjection
    drawSphere viewMatrix clipPlane device state content lightViewProjection

let drawShadowMap (device: GraphicsDevice) content (worldMatrix: Matrix) (lightViewProjection: Matrix) =
    device.SetRenderTarget(content.ShadowMap)
    device.Clear(Color.White)
    device.BlendState <- BlendState.Opaque

    let effect = content.Effects.Effect

    effect.CurrentTechnique <- effect.Techniques.["ShadowMap"]
    effect.Parameters.["xLightsViewProjection"].SetValue(lightViewProjection)

    drawSphereVertices effect device content
    drawTerrainVertices effect worldMatrix device content

let draw (gameTime: GameTime) (device: GraphicsDevice) state content =
    let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

    let view = state.Camera.ViewMatrix
    let world = Matrix.Identity

    let lightView = Matrix.CreateLookAt(-500.0f * state.LightDirection, Vector3.Zero, Vector3.Transform(state.LightDirection, Matrix.CreateRotationX(0.5f * MathHelper.Pi)))
    let lightViewProjection = lightView * content.LightsProjection

    drawShadowMap device content world lightViewProjection

    let waterReflectionView = Water.prepareFrameAndReturnReflectionView content.Water view world state.Camera (drawApartFromSky device state content lightViewProjection) (Sky.drawSkyDome content.Sky world content.Projection state.LightDirection state.Camera.Position)

    device.SetRenderTarget(content.HdrRenderTarget)

    do device.Clear(Color.Black)
    drawApartFromSky device state content lightViewProjection false view world Vector4.Zero // no clip plane
    Water.drawWater content.Water time world view content.Projection state.LightDirection state.Camera waterReflectionView
    Sky.drawSkyDome content.Sky world content.Projection state.LightDirection state.Camera.Position view
    
    match state.DebugOption with
    | None -> ()
    | ShadowMap -> drawDebug content.ShadowMap device content true
    | ReflectionMap -> drawDebug content.Water.ReflectionRenderTarget device content false
    | RefractionMap -> drawDebug content.Water.RefractionRenderTarget device content false

    device.SetRenderTarget(null)

    let effect = content.Effects.Hdr
    effect.CurrentTechnique <- effect.Techniques.["Plain"]

    content.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
        SamplerState.LinearClamp, DepthStencilState.Default, 
        RasterizerState.CullNone, effect)
 
    content.SpriteBatch.Draw(content.HdrRenderTarget, new Rectangle(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight), Color.White);
 
    content.SpriteBatch.End();
