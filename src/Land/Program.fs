module Program

open Game1

[<EntryPoint>]
let Main args =
    let game = new LandGame()
    do game.Run()
    0
