module EffectReader

open System.IO
open System.Reflection
open Microsoft.Xna.Framework.Graphics

let GetEffect device filename =
        let s = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename)
        let reader = new BinaryReader(s)
        new Effect(device, reader.ReadBytes((int)reader.BaseStream.Length))
