module Land.Draw

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open VertexPositionNormal

let drawTerrain (x: bool) (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4) (device: GraphicsDevice) (gameState: State) (gameContent: Content) =
    let effect = gameContent.Effects.GroundFromAtmosphere

    effect.CurrentTechnique <- effect.Techniques.["GroundFromAtmosphere"]
    effect.Parameters.["xWorld"].SetValue(worldMatrix)
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(gameContent.Projection)
    effect.Parameters.["xCameraPosition"].SetValue(gameState.Camera.Position)
    effect.Parameters.["xLightDirection"].SetValue(gameState.LightDirection)
    effect.Parameters.["xGrassTexture"].SetValue(gameContent.Textures.Grass)
    effect.Parameters.["xRockTexture"].SetValue(gameContent.Textures.Rock)
    effect.Parameters.["xSandTexture"].SetValue(gameContent.Textures.Sand)
    effect.Parameters.["xSnowTexture"].SetValue(gameContent.Textures.Snow)
    effect.Parameters.["xClipPlane"].SetValue(clipPlane)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
    effect.Parameters.["xAlphaAfterWaterDepthWeighting"].SetValue(x)
    effect.Parameters.["xMinMaxHeight"].SetValue(gameContent.MinMaxTerrainHeight)
    effect.Parameters.["xPerlinSize3D"].SetValue(15.0f)
    effect.Parameters.["xRandomTexture3D"].SetValue(gameContent.PerlinTexture3D)

    gameContent.Environment.Atmosphere.ApplyToEffect effect
    gameContent.Environment.Water.ApplyToGroundEffect effect

    device.BlendState <- BlendState.Opaque

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, gameContent.Vertices, 0, gameContent.Vertices.Length, gameContent.Indices, 0, gameContent.Indices.Length / 3)
        )

let drawSphere (viewMatrix: Matrix) (device: GraphicsDevice) (gameState: State) (gameContent: Content) =
    let effect = gameContent.Effects.GroundFromAtmosphere

    let sphereWorld = Matrix.Multiply(Matrix.CreateTranslation(0.0f, 1.5f, 0.0f), Matrix.CreateScale(10.0f))
    effect.CurrentTechnique <- effect.Techniques.["Coloured"]
    effect.Parameters.["xWorld"].SetValue(sphereWorld)
    effect.Parameters.["xView"].SetValue(viewMatrix)
    effect.Parameters.["xProjection"].SetValue(gameContent.Projection)
    effect.Parameters.["xLightDirection"].SetValue(gameState.LightDirection)
    effect.Parameters.["xAmbient"].SetValue(0.5f)
        
    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserIndexedPrimitives<VertexPositionNormal>(PrimitiveType.TriangleList, gameContent.SphereVertices, 0, gameContent.SphereVertices.Length, gameContent.SphereIndices, 0, gameContent.SphereIndices.Length / 3)
        )

let drawDebug (texture: Texture2D) (device: GraphicsDevice) (gameState: State) (gameContent: Content) =
    let effect = gameContent.Effects.Effect
    effect.CurrentTechnique <- effect.Techniques.["Debug"]
    effect.Parameters.["xDebugTexture"].SetValue(texture)

    effect.CurrentTechnique.Passes |> Seq.iter
        (fun pass ->
            pass.Apply()
            device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, gameContent.DebugVertices, 0, gameContent.DebugVertices.Length / 3)
        )

let drawApartFromSky (device: GraphicsDevice) (gameState: State) (gameContent: Content) x (viewMatrix: Matrix) (worldMatrix: Matrix) (clipPlane: Vector4)  =
    drawTerrain x viewMatrix worldMatrix clipPlane device gameState gameContent
    drawSphere viewMatrix device gameState gameContent

let draw (gameTime: GameTime) (device: GraphicsDevice) (gameState: State) (gameContent: Content) =
    let time = (single gameTime.TotalGameTime.TotalMilliseconds) / 100.0f

    let view = gameState.Camera.ViewMatrix
    let world = Matrix.Identity

    let waterReflectionView = gameContent.Water.Prepare view world gameState.Camera (drawApartFromSky device gameState gameContent) (Sky.drawSkyDome gameContent.Sky world gameContent.Projection gameState.LightDirection gameState.Camera.Position)

    device.SetRenderTarget(gameContent.HdrRenderTarget)

    do device.Clear(Color.Black)
    drawApartFromSky device gameState gameContent false view world Vector4.Zero // no clip plane
    gameContent.Water.DrawWater time world view gameContent.Projection gameState.LightDirection gameState.Camera waterReflectionView
    Sky.drawSkyDome gameContent.Sky world gameContent.Projection gameState.LightDirection gameState.Camera.Position view
    //_this.DrawDebug perlinTexture3D

    device.SetRenderTarget(null)

    let effect = gameContent.Effects.Hdr
    effect.CurrentTechnique <- effect.Techniques.["Plain"]

    gameContent.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
        SamplerState.LinearClamp, DepthStencilState.Default, 
        RasterizerState.CullNone, effect)
 
    gameContent.SpriteBatch.Draw(gameContent.HdrRenderTarget, new Rectangle(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight), Color.White);
 
    gameContent.SpriteBatch.End();
