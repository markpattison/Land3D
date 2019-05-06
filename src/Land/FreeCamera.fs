module FreeCamera

open Microsoft.Xna.Framework

open Input

let maxLookUpDown = 1.5f;
let rotSpeed = 0.00005f;            // per millisecond
let moveSpeed = 0.02f;
let upDirection = Vector3(0.0f, 1.0f, 0.0f)

// members
type FreeCamera(position: Vector3,
                lookAroundX: single,
                lookAroundY: single) =
    let rotY = Matrix.CreateRotationY(-lookAroundY)
    let lookDirection = 
            let rot1 = Matrix.CreateRotationX(-lookAroundX)
            let combined = Matrix.Multiply(rot1, rotY)
            let temp2 = Vector3.Transform(Vector3.Backward, combined)
            temp2.Normalize()
            temp2
    let rightDirection =
            let temp2 = Vector3.Transform(Vector3.Left, rotY)
            temp2.Normalize()
            temp2
    let lookAt = position + lookDirection
    member _this.ViewMatrix = Matrix.CreateLookAt(position, lookAt, upDirection)
    member _this.Position = position
    member _this.LookAroundX = lookAroundX
    member _this.LookAroundY = lookAroundY
    member _this.LookAt = lookAt
    member _this.RightDirection = rightDirection
    member _this.Updated(input : Input, t) =
        let mutable newPosition = position
        if input.Left then newPosition <- newPosition - rightDirection
        if input.Right then newPosition <- newPosition + rightDirection
        if input.Up then newPosition <- newPosition + upDirection
        if input.Down then newPosition <- newPosition - upDirection
        if input.Forward then newPosition <- newPosition + lookDirection
        if input.Backward then newPosition <- newPosition - lookDirection
        let newLookAroundY = lookAroundY + rotSpeed * t * single input.MouseDX
        let newLookAroundX = MathHelper.Clamp(lookAroundX + rotSpeed * t * single input.MouseDY, -maxLookUpDown, maxLookUpDown)
        FreeCamera(newPosition, newLookAroundX, newLookAroundY)
