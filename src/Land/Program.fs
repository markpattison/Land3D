module Program

open Land

[<EntryPoint>]
let Main args =
    let game = new LandGame()
    do game.Run()
    0
