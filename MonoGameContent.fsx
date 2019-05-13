#r "paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
#endif

open System
open System.Text
open System.IO
open Fake.Core
open Fake.Core

// MonoGameContent

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

let getArgs parameters contentFiles =
    [
        if Fake.Core.String.isNotNullOrEmpty parameters.OutputDir then yield sprintf "/outputDir:%s" parameters.OutputDir
        if Fake.Core.String.isNotNullOrEmpty parameters.IntermediateDir then yield sprintf "/intermediateDir:%s" parameters.IntermediateDir
        yield sprintf "/platform:%s" parameters.Platform.ParamString
        yield! contentFiles |> Seq.map (fun cf -> sprintf "/build:%s" cf)
    ] |> Arguments.OfArgs

let getWorkingDir parameters = 
    Seq.find (Fake.Core.String.isNotNullOrEmpty) [ parameters.WorkingDir;
                                Fake.Core.Environment.environVar ("teamcity.build.workingDir");
                                "." ]
    |> Path.GetFullPath

let buildMonoGameContent (setParams : MonoGameContentParams -> MonoGameContentParams) (content : string seq) =
    let parameters = MonoGameContentDefaults |> setParams
    let tool = parameters.ToolPath
    let args = getArgs parameters content
    let result =
        Command.RawCommand(tool, args)
        |>CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory (getWorkingDir parameters)
        |> Proc.run
    if result.ExitCode <> 0 then failwithf "MonoGame content building failed. Process finished with exit code %i." result.ExitCode
