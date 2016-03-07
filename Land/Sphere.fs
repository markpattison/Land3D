module Sphere

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open VertexPositionNormal

type Edge = int * int
type Face = int * int * int

type Sphere = { Vertices: Vector3 array; Edges: Edge array; Faces: Face array }

type SphereOrientation = | InwardFacing | OutwardFacing
type SphereNormals = | Flat | Smooth

let Icosahedron =
    let tao = 1.0f / 1.61803399f
    let vertices =
        [|
            Vector3(1.0f, tao, 0.0f);
            Vector3(-1.0f, tao, 0.0f);
            Vector3(1.0f, -tao, 0.0f);
            Vector3(-1.0f, -tao, 0.0f);
            Vector3(0.0f, 1.0f, tao);
            Vector3(0.0f, -1.0f, tao);
            Vector3(0.0f, 1.0f, -tao);
            Vector3(0.0f, -1.0f, -tao);
            Vector3(tao, 0.0f, 1.0f);
            Vector3(-tao, 0.0f, 1.0f);
            Vector3(tao, 0.0f, -1.0f);
            Vector3(-tao, 0.0f, -1.0f)
        |]

    let edges =
        [|
            (0, 2);
            (0, 4);
            (0, 6);
            (0, 8);
            (0, 10);
            (1, 3);
            (1, 4);
            (1, 6);
            (1, 9);
            (1, 11);

            (2, 5);
            (2, 7);
            (2, 8);
            (2, 10);
            (3, 5);
            (3, 7);
            (3, 9);
            (3, 11);
            (4, 6);
            (4, 8);

            (4, 9);
            (5, 7);
            (5, 8);
            (5, 9);
            (6, 10);
            (6, 11);
            (7, 10);
            (7, 11);
            (8, 9);
            (10, 11)
        |]

    let faces =
        [|
            (0, 3, 12);
            (0, 4, 13);
            (2, 4, 24);
            (1, 2, 18);
            (1, 3, 19);
            (5, 8, 16);
            (6, 8, 20);
            (6, 7, 18);
            (7, 9, 25);
            (5, 9, 17);
            
            (10, 11, 21);
            (10, 12, 22);
            (11, 13, 26);
            (14, 15, 21);
            (14, 16, 23);
            (15, 17, 27);
            (19, 20, 28);
            (22, 23, 28);
            (24, 25, 29);
            (26, 27, 29);
        |]

    { Vertices = vertices; Edges = edges; Faces = faces }

let getVerticesAndIndicesSmoothNormals orientation sphere =
    let factor =
        match orientation with
        | InwardFacing -> -1.0f
        | OutwardFacing -> 1.0f
    let vertices =
        sphere.Vertices
        |> Array.map (fun vertex ->
            let normal = vertex * factor
            normal.Normalize()
            new VertexPositionNormal(vertex, normal))
    let indices =
        sphere.Faces
        |> Array.map (fun (e1, e2, e3) ->
            let (v1, v2) = sphere.Edges.[e1]
            let (v3, v4) = sphere.Edges.[e2]
            let (v5, v6) = sphere.Edges.[e3]
            let (dv1, dv2, dv3) =
                match [ v1; v2; v3; v4; v5; v6 ] |> List.distinct with
                | [ dv1; dv2; dv3 ] -> (dv1, dv2, dv3)
                | _ -> failwith "must have 3 distinct vertices per face"
            let (vert1, vert2, vert3) = (sphere.Vertices.[dv1], sphere.Vertices.[dv2], sphere.Vertices.[dv3])
            let cross = Vector3.Cross(vert1 - vert2, sphere.Vertices.[dv1] - vert3)
            let dot = Vector3.Dot(cross, vert1) * factor
            if dot < 0.0f then [| dv1; dv2; dv3 |] else [| dv1; dv3; dv2 |])
        |> Array.concat
    (vertices, indices)

let getVerticesAndIndicesFlatNormals orientation sphere =
    let factor =
        match orientation with
        | InwardFacing -> -1.0f
        | OutwardFacing -> 1.0f
    let indicesVertices =
        sphere.Faces
        |> Array.mapi (fun n (e1, e2, e3) ->
            let (v1, v2) = sphere.Edges.[e1]
            let (v3, v4) = sphere.Edges.[e2]
            let (v5, v6) = sphere.Edges.[e3]
            let (vert1, vert2, vert3) =
                match [ v1; v2; v3; v4; v5; v6 ] |> List.distinct with
                | [ dv1; dv2; dv3 ] -> (sphere.Vertices.[dv1], sphere.Vertices.[dv2], sphere.Vertices.[dv3])
                | _ -> failwith "must have 3 distinct vertices per face"
            let cross = Vector3.Cross(vert1 - vert2, vert1 - vert3)
            let dot = Vector3.Dot(cross, vert1) * factor
            cross.Normalize()
            let offset = n * 3
            if dot < 0.0f then
                ([| offset; offset + 1; offset + 2 |],
                    [|
                        new VertexPositionNormal(vert1, -cross);
                        new VertexPositionNormal(vert2, -cross);
                        new VertexPositionNormal(vert3, -cross)
                    |])
            else
                ([| offset; offset + 2; offset + 1 |],
                    [|
                        new VertexPositionNormal(vert1, cross);
                        new VertexPositionNormal(vert2, cross);
                        new VertexPositionNormal(vert3, cross)
                    |]))
    let (i, v) = Array.unzip indicesVertices
    (Array.concat v, Array.concat i)

let getVerticesAndIndices normals orientation sphere =
    match normals with
    | Smooth -> getVerticesAndIndicesSmoothNormals orientation sphere
    | Flat -> getVerticesAndIndicesFlatNormals orientation sphere