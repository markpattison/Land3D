module VertexPositionNormal

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

[<StructAttribute>]
type VertexPositionNormal(position: Vector3, normal: Vector3) =
    member _this.Position = position
    member _this.Normal = normal
    static member VertexDeclaration =
        new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
        );
    interface IVertexType with
        member _this.VertexDeclaration = VertexPositionNormal.VertexDeclaration

