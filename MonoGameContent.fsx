module MonoGameContent

#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.Text
open System.IO
open Fake

type Platform =
    | Windows
    | Xbox360
    | WindowsPhone
    | IOS
    | Android
    | Linux
    | MacOSX
    | WindowsStoreApp
    | NativeClient
    | Ouya
    | PlayStationMobile
    | PlayStation4
    | WindowsPhone8
    | RaspberryPi with
    member x.ParamString =
        match x with
        | Windows -> "Windows"
        | Xbox360 -> "Xbox360"
        | WindowsPhone -> "WindowsPhone"
        | IOS -> "iOS"
        | Android -> "Android"
        | Linux -> "Linux"
        | MacOSX -> "MacOSX"
        | WindowsStoreApp -> "WindowsStoreApp"
        | NativeClient -> "NativeClient"
        | Ouya -> "Ouya"
        | PlayStationMobile -> "PlayStationMobile"
        | PlayStation4 -> "PlayStation4"
        | WindowsPhone8 -> "WindowsPhone8"
        | RaspberryPi -> "RaspberryPi"

type MonoGameContentParams =
    {
        ToolPath: string
        OutputDir: string
        IntermediateDir: string
        WorkingDir: string
        Platform: Platform
        TimeOut: TimeSpan
    }

let MonoGameContentDefaults =
    {
        ToolPath = @"C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\MGCB.exe" // is there a better way to set default?
        OutputDir = ""
        IntermediateDir = ""
        WorkingDir = "."
        Platform = Windows
        TimeOut = TimeSpan.FromMilliseconds((float)Int32.MaxValue)
    }

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters = 
    Seq.find isNotNullOrEmpty [ parameters.WorkingDir
                                environVar ("teamcity.build.workingDir")
                                "." ]
    |> Path.GetFullPath

let buildMonoGameContentArgs parameters contentFiles =
    new StringBuilder()
    |> appendQuotedIfNotNull parameters.OutputDir @"/outputDir:"
    |> appendQuotedIfNotNull parameters.IntermediateDir @"/intermediateDir:"
    |> appendWithoutQuotesIfNotNull parameters.Platform.ParamString @"/platform:"
    |> appendWithoutQuotes (contentFiles |> Seq.map (fun cf -> @" /build:" + "\"" + cf + "\"") |> String.concat "")
    |> toText

let MonoGameContent (setParams : MonoGameContentParams -> MonoGameContentParams) (content : string seq) =
    let details = content |> separated ", "
    let parameters = MonoGameContentDefaults |> setParams
    let tool = parameters.ToolPath
    let args = buildMonoGameContentArgs parameters content
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- tool
            info.WorkingDirectory <- getWorkingDir parameters
            info.Arguments <- args) parameters.TimeOut
    let errorDescription error = 
        match error with
        | FatalError x -> sprintf "MonoGame content building failed. Process finished with exit code %s (%d)." x error
        | _ -> "OK"
    match result with
    | OK -> ()
    | _ -> raise (BuildException(errorDescription result, content |> List.ofSeq))