module Land.Input

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Input

type Input(keyboardState: KeyboardState,
           oldKeyboardState : KeyboardState,
           mouseState: MouseState,
           oldMouseState: MouseState,
           gameWindow : GameWindow,
           originalMouseState: MouseState,
           oldMouseX,
           oldMouseY) =

    let justPressed key =
        keyboardState.IsKeyDown(key) && oldKeyboardState.IsKeyUp(key)

    let mouseDX = mouseState.X - originalMouseState.X
    let mouseDY = mouseState.Y - originalMouseState.Y
    let mouseX = oldMouseX + mouseDX
    let mouseY = oldMouseY + mouseDY
    do if not (obj.ReferenceEquals(gameWindow, null)) then Mouse.SetPosition(gameWindow.ClientBounds.Width / 2, gameWindow.ClientBounds.Height / 2)

    member _this.Quit = justPressed(Keys.Escape)
    member _this.MouseX = mouseX
    member _this.MouseY = mouseY
    member _this.MouseDX = mouseDX
    member _this.MouseDY = mouseDY
    member _this.Up = keyboardState.IsKeyDown(Keys.Up)
    member _this.Down = keyboardState.IsKeyDown(Keys.Down)
    member _this.Left = keyboardState.IsKeyDown(Keys.Left)
    member _this.Right = keyboardState.IsKeyDown(Keys.Right)
    member _this.Forward = (mouseState.LeftButton = ButtonState.Pressed)
    member _this.Backward = (mouseState.RightButton = ButtonState.Pressed)
    member _this.PageUp = keyboardState.IsKeyDown(Keys.PageUp)
    member _this.PageDown = keyboardState.IsKeyDown(Keys.PageDown)
    member _this.Fire = (mouseState.LeftButton = ButtonState.Pressed) && (oldMouseState.LeftButton = ButtonState.Released)
    member _this.Debug = justPressed(Keys.D)

    member _this.Updated(keyboard: KeyboardState, mouse: MouseState, window : GameWindow) =
        Input(keyboard, keyboardState, mouse, mouseState, window, originalMouseState, mouseX, mouseY)
