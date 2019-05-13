module Land.Textures

open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics

type Textures =
    {
        Grass: Texture2D;
        Rock: Texture2D;
        Sand: Texture2D;
        Snow: Texture2D;
    }

let load (contentManager: ContentManager) =
    {
        Grass = contentManager.Load<Texture2D>("Textures/grass")
        Rock = contentManager.Load<Texture2D>("Textures/rock")
        Sand = contentManager.Load<Texture2D>("Textures/sand")
        Snow = contentManager.Load<Texture2D>("Textures/snow")
    }
