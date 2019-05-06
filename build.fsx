#r "paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
#endif

#load "MonoGameContent.fsx"

open System
open Fake.Core
open Fake.IO.Globbing.Operators

// Directories
let intermediateContentDir = "./intermediateContent"
let contentDir = "./Land"
let deployDir = "./deploy/"

// Filesets
let appReferences = 
    !! "**/*.fsproj"

let contentFiles =
    !! "**/*.fx"
        ++ "**/*.spritefont"
        ++ "**/*.dds"

// Targets
Target.description "Cleaning directories"
Target.create "Clean" (fun _ -> 
    Fake.IO.Shell.cleanDirs [ deployDir ]
)

Target.description "Building MonoGame content"
Target.create "BuildContent" (fun _ ->
    contentFiles
        |> MonoGameContent.buildMonoGameContent (fun p ->
            { p with
                OutputDir = contentDir;
                IntermediateDir = intermediateContentDir;
            }))

Target.description "Building application"
Target.create "BuildApp" (fun _ ->
    appReferences
        |> Seq.iter (Fake.DotNet.DotNet.build id)
)

Target.description "Running application"
Target.create "RunApp" (fun _ ->
    CreateProcess.fromRawCommand "Land/bin/Release/net471/Land.exe" []
    |> Proc.startRawSync
    |> ignore
    Fake.Core.Process.setKillCreatedProcesses false)

// Build order

open Fake.Core.TargetOperators

"Clean"
    ==> "BuildContent"
    ==> "BuildApp"
    ==> "RunApp"

// start build
Target.runOrDefault "BuildApp"