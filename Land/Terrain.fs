module Terrain

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type Random with
    member r.NextSingle = single (r.NextDouble())

let MinMax array =
    let mutable min = Single.MaxValue
    let mutable max = Single.MinValue
    for i = 0 to (Array2D.length1 array) - 1 do
        for j = 0 to (Array2D.length2 array) - 1 do
            let value = array.[i, j]
            if value < min then min <- value
            if value > max then max <- value
    (min, max)

type Terrain(size) =
    let sizeSingle = single size
    let sizeVertices = size + 1
    let height = Array2D.zeroCreate sizeVertices sizeVertices
    member _this.Size = size
    member _this.SizeVertices = sizeVertices
    member _this.NumberOfVertices = sizeVertices * sizeVertices
    member _this.Height x z = height.[x, z]
    member _this.DeformCircularFaults numFaults maxDelta minSize maxSize =
        let rand = new Random()
        for i = 1 to numFaults do
            let faultX = sizeSingle * rand.NextSingle
            let faultZ = sizeSingle * rand.NextSingle
            let faultSize = minSize + (maxSize - minSize) * rand.NextSingle
            let faultSizeSqd = faultSize * faultSize
            let faultDelta = -maxDelta + 2.0f * maxDelta * rand.NextSingle
            for x = 0 to size do
                for z = 0 to size do
                    let distX = (single x) - faultX
                    let distZ = (single z) - faultZ
                    let distSqd = distX * distX + distZ * distZ
                    if (distSqd < faultSizeSqd) then
                        height.[x, z] <- height.[x, z] + faultDelta
    member private _this.ApplyToHeights f =
        for x = 0 to size do
            for z = 0 to size do
                height.[x, z] <- f height.[x, z]       
    member this.Normalize newMin newMax =
        let (min, max) = MinMax height
        let scale = (newMax - newMin) / (max - min)
        this.ApplyToHeights (fun height -> newMin + scale * (height - min))
    member this.Stretch factor =
        this.ApplyToHeights (fun height -> height ** factor)

let Normals (terrain:Terrain) =
    let normals = Array2D.zeroCreate<Vector3> terrain.SizeVertices terrain.SizeVertices
    for x = 0 to (terrain.Size - 1) do
        for z = 0 to (terrain.Size - 1) do
            let triangle1Normal = Vector3(terrain.Height (x + 1) z - terrain.Height x z,
                                         1.0f,
                                         terrain.Height x (z + 1) - terrain.Height x z)
            triangle1Normal.Normalize()
            normals.[x, z] <- normals.[x, z] + triangle1Normal
            normals.[x, z + 1] <- normals.[x, z + 1] + triangle1Normal
            normals.[x + 1, z] <- normals.[x + 1, z] + triangle1Normal

            let triangle2Normal = Vector3(terrain.Height x (z + 1) - terrain.Height (x + 1) (z + 1),
                                          1.0f,
                                          terrain.Height (x + 1) (z + 1) - terrain.Height (x + 1) z)
            triangle2Normal.Normalize()
            normals.[x + 1, z] <- normals.[x + 1, z] + triangle2Normal
            normals.[x, z + 1] <- normals.[x, z + 1] + triangle2Normal
            normals.[x + 1, z + 1] <- normals.[x + 1, z + 1] + triangle2Normal
    normals

let GetVertices (terrain:Terrain) =
    let normals = Normals terrain
    let size = single terrain.Size
    let textureScale = 1.0f / size
    let halfSize = 0.5f * size
    Array.init terrain.NumberOfVertices
        ( fun i ->
            let x = i % terrain.SizeVertices
            let z = i / terrain.SizeVertices
            VertexPositionNormalTexture(Vector3(single x - halfSize, terrain.Height x z, single z - halfSize),
                                        normals.[x, z],
                                        Vector2(textureScale * single x, textureScale * single z))
        )

let GetIndices size =
    let stride = size + 1
    Array.init (size * size * 6)
        ( fun i ->
            let positionIndex = i / 6
            let x = positionIndex % size
            let z = positionIndex / size
            let vertIndex = x + z * (size + 1)
            match (i % 6) with
            | 0 -> vertIndex
            | 4 -> vertIndex + 1 + stride
            | 1 | 3 -> vertIndex + 1
            | 2 | 5 | _ -> vertIndex + stride
        )