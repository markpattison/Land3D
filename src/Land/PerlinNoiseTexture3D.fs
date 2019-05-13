module Land.PerlinNoiseTexture3D

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

let create device size =
    let perlinTexture3D = new Texture3D(device, size, size, size, false, SurfaceFormat.Color)
    let random = new System.Random()

    let randomVectorColour _ =
        let v = Vector3(single (random.NextDouble() * 2.0 - 1.0),
                        single (random.NextDouble() * 2.0 - 1.0),
                        single (random.NextDouble() * 2.0 - 1.0))
        v.Normalize()
        let c = Color(v)
        c

    let randomVectors = Array.init (16 * 16 * 16) randomVectorColour
    perlinTexture3D.SetData<Color>(randomVectors)

    perlinTexture3D
