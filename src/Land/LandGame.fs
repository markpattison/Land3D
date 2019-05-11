namespace Land
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open FreeCamera
open Input

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable gameContent = Unchecked.defaultof<Content>
    let mutable gameState = Unchecked.defaultof<State>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    do graphics.GraphicsProfile <- GraphicsProfile.HiDef
    do graphics.PreferredBackBufferWidth <- 900
    do graphics.PreferredBackBufferHeight <- 700
    do graphics.IsFullScreen <- false
    do graphics.ApplyChanges()
    do base.Content.RootDirectory <- "Content"

    override _this.Initialize() =
        device <- base.GraphicsDevice

        base.Initialize()
        ()

    override _this.LoadContent() =

        Mouse.SetPosition(_this.Window.ClientBounds.Width / 2, _this.Window.ClientBounds.Height / 2) 
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), _this.Window, originalMouseState, 0, 0)

        gameContent <- ContentLoader.load device _this.Content

        gameState <- {
            LightDirection = Vector3.Normalize(Vector3(0.0f, -0.5f, -1.0f))
            Camera = FreeCamera(Vector3(0.0f, 10.0f, -(single gameContent.Terrain.Size) / 2.0f), 0.0f, 0.0f)
            Exiting = false
        }

    override _this.Update(gameTime) =
        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), _this.Window)

        gameState <- Update.update gameTime input gameState

        if gameState.Exiting then _this.Exit()

        do base.Update(gameTime)

    override _this.Draw(gameTime) =
        Draw.draw gameTime device gameState gameContent

        do base.Draw(gameTime)

