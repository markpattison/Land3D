module Input

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
    do Mouse.SetPosition(gameWindow.ClientBounds.Width / 2, gameWindow.ClientBounds.Height / 2)

    member this.Quit = justPressed(Keys.Escape)
    member this.MouseX = mouseX
    member this.MouseY = mouseY
    member this.MouseDX = mouseDX
    member this.MouseDY = mouseDY
    member this.Up = keyboardState.IsKeyDown(Keys.Up)
    member this.Down = keyboardState.IsKeyDown(Keys.Down)
    member this.Left = keyboardState.IsKeyDown(Keys.Left)
    member this.Right = keyboardState.IsKeyDown(Keys.Right)
    member this.Forward = (mouseState.LeftButton = ButtonState.Pressed)
    member this.Backward = (mouseState.RightButton = ButtonState.Pressed)
    member this.Fire = (mouseState.LeftButton = ButtonState.Pressed) && (oldMouseState.LeftButton = ButtonState.Released)

    member this.Updated(keyboard: KeyboardState,
                        mouse: MouseState,
                        window : GameWindow) =
        Input(keyboard, keyboardState, mouse, mouseState, window, originalMouseState, mouseX, mouseY)
