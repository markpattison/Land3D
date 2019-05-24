module Land.Draw

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open Effects
open VertexPositionNormal

let drawTerrainVertices (effect: Effect) (device: GraphicsDevice) content =
    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, content.Vertices, 0, content.Vertices.Length, content.Indices, 0, content.Indices.Length / 3)
        )

let drawTerrain (x: bool) (viewMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) state content (lightViewProjection: Matrix) =
    let ground = content.Effects.GroundFromAtmosphere

    ground.Effect.CurrentTechnique <- ground.Effect.Techniques.["GroundFromAtmosphere"]
    ground.SetWorld Matrix.Identity
    ground.SetView viewMatrix
    ground.SetCameraPosition state.Camera.Position
    ground.SetLightDirection state.LightDirection
    ground.SetLightViewProjection lightViewProjection
    ground.SetShadowMap content.ShadowMap
    ground.SetClipPlane clipPlane
    ground.SetAmbient 0.5f
    ground.SetAlphaAfterWaterDepthWeighting x

    device.BlendState <- BlendState.Opaque

    drawTerrainVertices ground.Effect device content

let drawSphereVertices (effect: Effect) (device: GraphicsDevice) content =
    let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.5f, 0.0f), Matrix.CreateScale(10.0f))

    effect.Parameters.["xWorld"].SetValue(sphereWorld)
        
    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, content.SphereVertices, 0, content.SphereVertices.Length, content.SphereIndices, 0, content.SphereIndices.Length / 3)
        )

let drawSphere (viewMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) state content (lightViewProjection: Matrix) =
    let ground = content.Effects.GroundFromAtmosphere

    ground.Effect.CurrentTechnique <- ground.Effect.Techniques.["Coloured"]
    ground.SetView viewMatrix
    ground.SetLightDirection state.LightDirection
    ground.SetLightViewProjection lightViewProjection
    ground.SetShadowMap content.ShadowMap
    ground.SetClipPlane clipPlane
    ground.SetAmbient 0.5f
    
    drawSphereVertices ground.Effect device content

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

let drawApartFromSky (device: GraphicsDevice) state content (lightViewProjection: Matrix) x (viewMatrix: Matrix) (clipPlane: Vector4) =
    drawTerrain x viewMatrix clipPlane device state content lightViewProjection
    drawSphere viewMatrix clipPlane device state content lightViewProjection

let drawShadowMap (device: GraphicsDevice) content (lightViewProjection: Matrix) =
    device.SetRenderTarget(content.ShadowMap)
    device.Clear(Color.White)
    device.BlendState <- BlendState.Opaque

    let effect = content.Effects.Effect

    effect.CurrentTechnique <- effect.Techniques.["ShadowMap"]
    effect.Parameters.["xLightsViewProjection"].SetValue(lightViewProjection)

    drawSphereVertices effect device content
    effect.Parameters.["xWorld"].SetValue(Matrix.Identity)
    drawTerrainVertices effect device content

let draw (gameTime: GameTime) (device: GraphicsDevice) state content =
    let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

    let view = state.Camera.ViewMatrix
    let world = Matrix.Identity

    let lightView = Matrix.CreateLookAt(-500.0f * state.LightDirection, Vector3.Zero, Vector3.Transform(state.LightDirection, Matrix.CreateRotationX(0.5f * MathHelper.Pi)))
    let lightViewProjection = lightView * content.LightsProjection

    drawShadowMap device content lightViewProjection

    let waterReflectionView = Water.prepareFrameAndReturnReflectionView content.Water view state.Camera (drawApartFromSky device state content lightViewProjection) (Sky.drawSkyDome content.Sky world state.LightDirection state.Camera.Position)

    device.SetRenderTarget(content.HdrRenderTarget)

    do device.Clear(Color.Black)
    drawApartFromSky device state content lightViewProjection false view Vector4.Zero // no clip plane
    Water.drawWater content.Water time world view state.LightDirection state.Camera waterReflectionView
    Sky.drawSkyDome content.Sky world state.LightDirection state.Camera.Position view
    
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
