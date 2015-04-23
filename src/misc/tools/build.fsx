#nowarn "40"
// --------------------------------------------------------------------------------------
// Reference FsLab together with FAKE for building & Suave for hosting Journals
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#I "packages/Suave/lib/net40"
#I "packages/FsLab.Runner/lib/net40"

#load "packages/FsLab/FsLab.fsx"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#r "FSharp.Literate.dll"
#r "FsLab.Runner.dll"
#r "FakeLib.dll"
#r "Suave.dll"

open Fake 
open FsLab
open System
open FSharp.Literate

// --------------------------------------------------------------------------------------
// Runner configuration - You can change some basic settings of ProcessingContext here
// --------------------------------------------------------------------------------------

let localPort = 8089

let handleError(err:FsiEvaluationFailedInfo) =
    sprintf "Evaluating F# snippet failed:\n%s\nThe snippet evaluated:\n%s" err.StdErr err.Text
    |> traceImportant 

let ctx = ProcessingContext.Create(__SOURCE_DIRECTORY__).With(fun p ->
  { p with  
      OutputKind = OutputKind.Html
      Output = __SOURCE_DIRECTORY__ @@ "output";
      TemplateLocation = Some(__SOURCE_DIRECTORY__ @@ "packages/FsLab.Runner")
      FailedHandler = handleError })

// --------------------------------------------------------------------------------------
// Processes journals and runs Suave server to host them on localhost
// --------------------------------------------------------------------------------------

open Suave
open Suave.Web
open Suave.Http
open Suave.Http.Files

let generateJournals ctx =
    let builtFiles = Journal.processJournals ctx
    traceImportant "All journals updated."
    Journal.getIndexJournal ctx builtFiles

let startWebServer fileName =
    let defaultBinding = defaultConfig.bindings.[0]
    let withPort = { defaultBinding.socketBinding with port = 1us }
    let serverConfig = 
        { defaultConfig with 
            bindings = [ { defaultBinding with socketBinding = withPort } ]
            homeFolder = Some ctx.Output }
    let app =
        Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
        >>= Writers.setHeader "Pragma" "no-cache"
        >>= Writers.setHeader "Expires" "0"
        >>= browseHome
    startWebServerAsync serverConfig app |> snd |> Async.Start
    Diagnostics.Process.Start(sprintf "http://localhost:%d/%s" localPort fileName) |> ignore

let handleWatcherEvents (e:IO.FileSystemEventArgs) =
    let fi = fileInfo e.FullPath 
    traceImportant <| sprintf "%s was changed." fi.Name
    if fi.Attributes.HasFlag IO.FileAttributes.Hidden || 
       fi.Attributes.HasFlag IO.FileAttributes.Directory then ()
    else Journal.updateJournals ctx |> ignore

// --------------------------------------------------------------------------------------
// Build targets - for example, run `build GenerateLatex` to produce latex output
// --------------------------------------------------------------------------------------

let indexJournal = ref None

Target "GenerateHtml" (fun _ ->
    indexJournal.Value <- Some(generateJournals ctx)
)

Target "GenerateLatex" (fun _ ->
    { ctx with OutputKind = OutputKind.Latex }
    |> generateJournals 
    |> ignore
)

Target "KeepRunning" (fun _ ->
    use watcher = new System.IO.FileSystemWatcher(ctx.Root, "*.fsx")
    watcher.EnableRaisingEvents <- true
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    startWebServer(defaultArg indexJournal.Value "")

    traceImportant "Waiting for journal edits. Press any key to stop."
    System.Console.ReadKey() |> ignore
    watcher.EnableRaisingEvents <- false
)


// By default, we run `KeepRunning` which produces HTML output and
// keeps updating it every time the source FSX files change

"GenerateHtml" 
==> "KeepRunning"

RunTargetOrDefault "KeepRunning"
