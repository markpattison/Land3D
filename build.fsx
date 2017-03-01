// include Fake libs
#r "packages/FAKE/tools/FakeLib.dll"
#load "MonoGameContent.fsx"

open System
open Fake
open MonoGameContent

// Directories
let intermediateContentDir = "./intermediateContent"
let contentDir = "./Land"
let buildDir  = "./build/"
let deployDir = "./deploy/"

// Filesets
let appReferences = 
    !! "**/*.fsproj"

let contentFiles =
    !! "**/*.fx"
        ++ "**/*.spritefont"
        ++ "**/*.dds"

// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; deployDir]
)

let quoted dir = "\"" + dir + "\""

Target "BuildContent" (fun _ ->
    contentFiles
        |> MonoGameContent (fun p ->
            { p with
                OutputDir = contentDir;
                IntermediateDir = intermediateContentDir;
            }))

Target "BuildApp" (fun _ ->
    appReferences
        |> MSBuildDebug buildDir "Build"
        |> Log "AppBuild-Output: "
)

Target "RunApp" (fun _ ->
    ExecProcess (fun info ->
        info.FileName <- buildDir + @"Land.exe"
        info.WorkingDirectory <- buildDir)
        (TimeSpan.FromDays 1.0)
    |> ignore)

// Build order
"Clean"
    ==> "BuildContent"
    ==> "BuildApp"
    ==> "RunApp"

// start build
RunTargetOrDefault "BuildApp"