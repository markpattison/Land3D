module Sphere

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

let Icosahedron =
    let tao = 1.61803399f
    let vertices =
        [|
        Vector3(1.0f,tao,0.0f);
        Vector3(-1.0f,tao,0.0f);
        Vector3(1.0f,-tao,0.0f);
        Vector3(-1.0f,-tao,0.0f);
        Vector3(0.0f,1.0f,tao);
        Vector3(0.0f,-1.0f,tao);
        Vector3(0.0f,1.0f,-tao);
        Vector3(0.0f,-1.0f,-tao);
        Vector3(tao,0.0f,1.0f);
        Vector3(-tao,0.0f,1.0f);
        Vector3(tao,0.0f,-1.0f);
        Vector3(-tao,0.0f,-1.0f)
        |]
    let distances = Array2D.init 12 12 (fun i j -> (vertices.[i] - vertices.[j]).Length())
    distances
