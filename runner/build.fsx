#nowarn "40"
// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------
#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
#I "packages/Suave/lib/net40"
#r "Suave.dll"
#load "packages/FsLab/FsLab.fsx"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "../src/FsLab.Runner/Formatters.fs"
open FsLab
#load "../src/FsLab.Runner/Runner.fs"
open System
open System.IO
open Fake 
open FSharp.Literate
open FsLab.Formatters
open FsLab.Runner
open Suave
open Suave.Web
open Suave.Http
open Suave.Http.Files
open System.Diagnostics

// --------------------------------------------------------------------------------------
// Feel free to tweak the configuration here
// --------------------------------------------------------------------------------------

let rec ctx = 
  { Root = __SOURCE_DIRECTORY__ @@ "journals"
    Output = __SOURCE_DIRECTORY__ @@ "output"
    OutputKind = OutputKind.Html 
    FloatFormat = "G4"
    FailedHandler = handleError
    TemplateLocation = Some(__SOURCE_DIRECTORY__ @@ "packages/FsLab.Runner")
    FileWhitelist = None }

// --------------------------------------------------------------------------------------
// Calls FsLab to process journals
// --------------------------------------------------------------------------------------

and handleError(err:FsiEvaluationFailedInfo) =
    sprintf "Evaluating F# snippet failed:\n%s\nThe snippet evaluated:\n%s" err.StdErr err.Text
    |> traceImportant 

let generateJournals() =
    let builtFiles = processScriptFiles ctx
    traceImportant "All journals updated."
    getDefaultFile ctx builtFiles

let startWebServer fileName =
    let serverConfig = 
        { defaultConfig with 
           homeFolder = Some ctx.Output
           bindings = [for b in defaultConfig.bindings -> 
                         { b with socketBinding = {b.socketBinding with port=uint16 8084}} ] }
    let app =
        Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
        >>= Writers.setHeader "Pragma" "no-cache"
        >>= Writers.setHeader "Expires" "0"
        >>= browseHome
    startWebServerAsync serverConfig app |> snd |> Async.Start
    Process.Start ("http://localhost:8084/" + fileName) |> ignore

let handleWatcherEvents (e:FileSystemEventArgs) =
    let fi = fileInfo e.FullPath 
    traceImportant <| sprintf "%s was changed." fi.Name
    if fi.Attributes.HasFlag FileAttributes.Hidden || 
       fi.Attributes.HasFlag FileAttributes.Directory then ()
    else generateJournals() |> ignore

let defaultFile = ref None

// --------------------------------------------------------------------------------------
// FAKE build targets
// --------------------------------------------------------------------------------------

Target "Generate" (fun _ ->
    Fake.FileHelper.CopyFile 
      (__SOURCE_DIRECTORY__ @@ "packages/FAKE/tools/FSharp.Compiler.Interactive.Settings.dll")
      (__SOURCE_DIRECTORY__ @@ "../lib/FSharp.Compiler.Interactive.Settings.dll")
    defaultFile.Value <- Some(generateJournals())
)

Target "KeepRunning" (fun _ ->
    use watcher = new System.IO.FileSystemWatcher(ctx.Root, "*.*")
    watcher.EnableRaisingEvents <- true
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    startWebServer(defaultArg defaultFile.Value "")

    traceImportant "Waiting for journal edits. Press any key to stop."
    System.Console.ReadKey() |> ignore
    watcher.EnableRaisingEvents <- false
)

Target "All" DoNothing

"Generate" 
==> "KeepRunning"
==> "All" 

RunTargetOrDefault "All"
