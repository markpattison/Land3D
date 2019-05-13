module Land.Program

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open FreeCamera
open Input

type LandGame() as _this =
    inherit Game()
    let graphics = new GraphicsDeviceManager(_this)
    let mutable content = Unchecked.defaultof<Content>
    let mutable state = Unchecked.defaultof<State>
    let mutable device = Unchecked.defaultof<GraphicsDevice>
    let mutable input = Unchecked.defaultof<Input>
    let mutable originalMouseState = Unchecked.defaultof<MouseState>
    do graphics.GraphicsProfile <- GraphicsProfile.HiDef
    do graphics.PreferredBackBufferWidth <- 900
    do graphics.PreferredBackBufferHeight <- 700
    do graphics.IsFullScreen <- false
    do graphics.ApplyChanges()

    override _this.Initialize() =
        device <- base.GraphicsDevice

        base.Initialize()

    override _this.LoadContent() =

        Mouse.SetPosition(_this.Window.ClientBounds.Width / 2, _this.Window.ClientBounds.Height / 2) 
        originalMouseState <- Mouse.GetState()
        input <- Input(Keyboard.GetState(), Keyboard.GetState(), Mouse.GetState(), Mouse.GetState(), _this.Window, originalMouseState, 0, 0)

        content <- ContentLoader.load device _this.Content

        state <- {
            LightDirection = Vector3.Normalize(Vector3(0.0f, -0.5f, -1.0f))
            Camera = FreeCamera(Vector3(0.0f, 10.0f, -(single content.Terrain.Size) / 2.0f), 0.0f, 0.0f)
            Exiting = false
            DebugOption = None
        }

    override _this.Update(gameTime) =
        input <- input.Updated(Keyboard.GetState(), Mouse.GetState(), _this.Window)

        state <- Update.update gameTime input state

        if state.Exiting then _this.Exit()

        do base.Update(gameTime)

    override _this.Draw(gameTime) =
        Draw.draw gameTime device state content

        do base.Draw(gameTime)

[<EntryPoint>]
let Main _ =
    let game = new LandGame()
    do game.Run()
    0
