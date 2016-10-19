// include Fake libs
#r "packages/FAKE/tools/FakeLib.dll"

// note the MonoGame tools directory must be in the path

open System
open Fake

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

let mgcbArgs =
    @"/outputDir:" + quoted contentDir + @" /intermediateDir:" + quoted intermediateContentDir + @" /platform:Windows"

Target "BuildContent" (fun _ ->
    let contentFileList = contentFiles |> Seq.map (fun cf -> @" /build:" + "\"" + cf + "\"") |> String.concat ""
    ExecProcess (fun info ->
        info.FileName <- @"MGCB.exe"
        info.WorkingDirectory <- "."
        info.Arguments <- mgcbArgs + contentFileList)
        (TimeSpan.FromMinutes 5.0)
    |> ignore)

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