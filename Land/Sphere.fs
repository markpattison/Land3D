module Sphere

open System
open Microsoft.Xna.Framework
open VertexPositionNormal

type Vertex = Vector3

type Edge = Vertex * Vertex
type Face = Edge * Edge * Edge

type Sphere = { Vertices: Vertex array; Edges: Edge array; Faces: Face array }

type SphereOrientation =
    | InwardFacing
    | OutwardFacing

let getOrientationFactor orientation =
    match orientation with
    | InwardFacing -> -1.0f
    | OutwardFacing -> 1.0f

type SphereNormals = | Flat | Smooth

type SphereVertexDistribution = | Even | Concentrated

let distributeVertex distribution =
    match distribution with
    | Even -> id
    | Concentrated ->
        let mapY y = y + 1.0f * (y + 1.0f)
        (fun (v: Vertex) ->
            let v' = Vector3(v.X, mapY v.Y, v.Z)
            v'.Normalize()
            v')

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
        |> Array.map Vector3.Normalize

    let edges: Edge array =
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
        |> Array.map (fun (vertexIndex1, vertexIndex2) -> (vertices.[vertexIndex1], vertices.[vertexIndex2]))

    let faces: Face array =
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
        |> Array.map (fun (edgeIndex1, edgeIndex2, edgeIndex3) -> (edges.[edgeIndex1], edges.[edgeIndex2], edges.[edgeIndex3]))

    { Vertices = vertices; Edges = edges; Faces = faces }

let getThreeDistinctVertices (face: Face) orientationFactor =
    let ((v1, v2), (v3, v4), (v5, v6)) = face
    let distinctVertices = [ v1; v2; v3; v4; v5; v6 ] |> List.distinct
    let (vertex1, vertex2, vertex3) =
        match distinctVertices with
        | [ dv1; dv2; dv3 ] -> (dv1, dv2, dv3)
        | _ -> failwith "must have 3 distinct vertices per face"
    let cross = Vector3.Cross(vertex1 - vertex2, vertex1 - vertex3)
    let dot = Vector3.Dot(cross, vertex1) * orientationFactor
    let normCross = Vector3.Normalize(cross)
    if dot < 0.0f then (vertex1, vertex2, vertex3, -normCross) else (vertex1, vertex3, vertex2, normCross)

let getOrderedEdges (edge1, edge2) edgeNextTo =
    let (v1, v2) = edge1
    let (vn1, vn2) = edgeNextTo
    if v1 = vn1 || v2 = vn1 || v1 = vn2 || v2 = vn2 then (edge1, edge2) else (edge2, edge1)

let divide sphere =
    let intermediateVertex (v1: Vertex) (v2: Vertex) =
        Vector3.Normalize(2.0f * (v1 + v2))
    let edgesWithNewVertices: (Edge * Vertex) array =
        sphere.Edges
        |> Array.map (fun (v1, v2) -> ((v1, v2), intermediateVertex v1 v2))
    let (_, newVertices) =
        Array.unzip edgesWithNewVertices
    let edgesNewVerticesDict = edgesWithNewVertices |> dict
    let edgesWithNewEdges: (Edge * (Edge * Edge)) array =
        sphere.Edges
        |> Array.map (fun edge ->
            let (v1, v2) = edge
            let midVertex: Vertex = edgesNewVerticesDict.[edge]
            (edge, ((v1, midVertex), (midVertex, v2))))
    let edgesNewEdgesDict = edgesWithNewEdges |> dict
    let (_, newEdgeEdges) = Array.unzip edgesWithNewEdges
    let (allFaces', newFaceEdges') =
        sphere.Faces
        |> Array.map (fun (edge1, edge2, edge3) ->
            let (newEdge1a, newEdge1b) = getOrderedEdges edgesNewEdgesDict.[edge1] edge3
            let (newEdge2a, newEdge2b) = getOrderedEdges edgesNewEdgesDict.[edge2] edge1
            let (newEdge3a, newEdge3b) = getOrderedEdges edgesNewEdgesDict.[edge3] edge2
            let nv1 = edgesNewVerticesDict.[edge1]
            let nv2 = edgesNewVerticesDict.[edge2]
            let nv3 = edgesNewVerticesDict.[edge3]
            let newFaceEdge1 = (nv1, nv3)
            let newFaceEdge2 = (nv2, nv1)
            let newFaceEdge3 = (nv3, nv2)
            let faces =
                [|
                    (newEdge1a, newFaceEdge1, newEdge3b);
                    (newEdge1b, newFaceEdge2, newEdge2a);
                    (newEdge2b, newFaceEdge3, newEdge3a);
                    (newFaceEdge1, newFaceEdge2, newFaceEdge3)
                |]
            (faces, [| newFaceEdge1; newFaceEdge2; newFaceEdge3 |]))
        |> Array.unzip
    let allFaces = allFaces' |> Array.concat
    let newFaceEdges = newFaceEdges' |> Array.concat
    let allVertices = Array.append sphere.Vertices newVertices
    let allEdges =
        newEdgeEdges
        |> Array.map (fun (e1, e2) -> [|e1; e2|])
        |> Array.concat
        |> Array.append newFaceEdges
    { Vertices = allVertices; Edges = allEdges; Faces = allFaces }

let create levelOfDetail =
    {1 .. levelOfDetail}
    |> Seq.fold (fun sphere _ -> divide sphere) Icosahedron

let getVerticesAndIndicesSmoothNormals orientation distributeVertices sphere =
    let factor = getOrientationFactor orientation
    let vertices =
        sphere.Vertices
        |> Array.map distributeVertices
        |> Array.map (fun vertex -> new VertexPositionNormal(vertex, Vector3.Normalize(vertex * factor)))
    let indices =
        sphere.Faces
        |> Array.map (fun face ->
            let (vertex1, vertex2, vertex3, _) = getThreeDistinctVertices face factor
            [| vertex1; vertex2; vertex3 |])
        |> Array.concat
        |> Array.map (fun vertex -> Array.findIndex (fun v -> v = vertex) sphere.Vertices)
    (vertices, indices)

let getVerticesAndIndicesFlatNormals orientation distributeVertices sphere =
    let factor = getOrientationFactor orientation
    let (vertices, indices) =
        sphere.Faces
        |> Array.mapi (fun n face ->
            let (vertex1, vertex2, vertex3, cross) = getThreeDistinctVertices face factor
            [|
                (new VertexPositionNormal(distributeVertices vertex1, cross), 3 * n);
                (new VertexPositionNormal(distributeVertices vertex2, cross), 3 * n + 1);
                (new VertexPositionNormal(distributeVertices vertex3, cross), 3 * n + 2);
            |])
        |> Array.concat
        |> Array.unzip
    (vertices, indices)

let getVerticesAndIndices normals orientation distribution sphere =
    let distributeVertices = distributeVertex distribution
    match normals with
    | Smooth -> getVerticesAndIndicesSmoothNormals orientation distributeVertices sphere
    | Flat -> getVerticesAndIndicesFlatNormals orientation distributeVertices sphere