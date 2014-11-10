module Program

open System
open Game1

[<EntryPoint>]
let Main args =
    let game = new LandGame()
    do game.Run()
    0
