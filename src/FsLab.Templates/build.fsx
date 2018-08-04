#nowarn "40"
// --------------------------------------------------------------------------------------
// Reference FsLab together with FAKE for building & Suave for hosting Journals
// --------------------------------------------------------------------------------------

#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "packages/FsLab/FsLab.fsx"
#I "packages/FAKE/tools"
#I "packages/Suave/lib/net40"
#I "packages/FSharp.Formatting/lib/net40"
#I "packages/FsLab.Runner/lib/net40"
#r "FsLab.Runner.dll"
#r "FakeLib.dll"
#r "Suave.dll"

open Fake
open FsLab
open System
open System.Text
open FSharp.Literate

// --------------------------------------------------------------------------------------
// Runner configuration - You can change some basic settings of ProcessingContext here
// --------------------------------------------------------------------------------------

let handleError(err:FsiEvaluationFailedInfo) =
    sprintf "Evaluating F# snippet failed:\n%s\nThe snippet evaluated:\n%s" err.StdErr err.Text
    |> traceImportant

let ctx = 
  { ProcessingContext.Create(__SOURCE_DIRECTORY__) with
      OutputKind = OutputKind.Html
      Output = __SOURCE_DIRECTORY__ </> "output";
      TemplateLocation = Some(__SOURCE_DIRECTORY__ </> "packages/FsLab.Runner")
      FailedHandler = handleError }

// --------------------------------------------------------------------------------------
// Processes journals and runs Suave server to host them on localhost
// --------------------------------------------------------------------------------------

open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Suave.Operators
open Suave.Filters

let localPort = 8901

let generateJournals ctx =
  let builtFiles = Journal.processJournals ctx
  traceImportant "All journals updated."
  Journal.getIndexJournal ctx builtFiles

let refreshEvent = new Event<_>()

let socketHandler (webSocket : WebSocket) ctx = socket {
  while true do
    let! refreshed =
      refreshEvent.Publish
      |> Control.Async.AwaitEvent
      |> Suave.Sockets.SocketOp.ofAsync
    do! webSocket.send Text (Encoding.UTF8.GetBytes "refreshed") true }

let startWebServer fileName =
    let defaultBinding = defaultConfig.bindings.[0]
    let withPort = { defaultBinding.socketBinding with port = uint16 localPort }
    let serverConfig =
        { defaultConfig with
            bindings = [ { defaultBinding with socketBinding = withPort } ]
            homeFolder = Some ctx.Output }
    let app =
      choose [
        path "/websocket" >=> handShake socketHandler
        Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
        >=> Writers.setHeader "Pragma" "no-cache"
        >=> Writers.setHeader "Expires" "0"
        >=> Files.browseHome ]
    startWebServerAsync serverConfig app |> snd |> Async.Start

let handleWatcherEvents (e:IO.FileSystemEventArgs) =
    let fi = fileInfo e.FullPath
    traceImportant <| sprintf "%s was changed." fi.Name
    if fi.Attributes.HasFlag IO.FileAttributes.Hidden ||
       fi.Attributes.HasFlag IO.FileAttributes.Directory then ()
    else Journal.updateJournals ctx |> ignore
    refreshEvent.Trigger()

// --------------------------------------------------------------------------------------
// Build targets - for example, run `build GenerateLatex` to produce latex output
// --------------------------------------------------------------------------------------

let indexJournal = ref None

Target "help" (fun _ ->
  printfn "Use 'build run' to produce HTML journals in the background "
  printfn "and host them locally using a simple web server."
  printfn ""
  printfn "Other usage options:"
  printfn "  build html   - Generate HTML output for all scripts"
  printfn "  build run    - Generate HTML, host it and keep it up-to-date"
  printfn "  build latex  - Generate LaTeX output for all scripts"
  printfn "  build pdf    - Generate LaTeX output & compile using 'pdflatex'"
)

Target "livehtml" (fun _ ->
    indexJournal.Value <- Some(generateJournals { ctx with Standalone = false })
)

Target "html" (fun _ ->    
    indexJournal.Value <- Some(generateJournals { ctx with Standalone = true })
)

Target "latex" (fun _ ->
    { ctx with OutputKind = OutputKind.Latex }
    |> generateJournals
    |> ignore
)

Target "run" (fun _ ->
    use watcher = new System.IO.FileSystemWatcher(ctx.Root, "*.fsx")
    watcher.EnableRaisingEvents <- true
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    let fileName = (defaultArg indexJournal.Value "")
    startWebServer fileName

    Diagnostics.Process.Start(sprintf "http://localhost:%d/%s" localPort fileName) |> ignore
    System.Console.ReadKey() |> ignore
)

Target "webpreview" (fun _ ->
    use watcher = new System.IO.FileSystemWatcher(ctx.Root, "*.fsx")
    watcher.EnableRaisingEvents <- true
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    startWebServer (defaultArg indexJournal.Value "")

    traceImportant "Waiting for journal edits. Press any key to stop."
    System.Threading.Thread.Sleep -1
)

Target "pdf" (fun _ ->
  for tex in !! (ctx.Output </> "*.tex" ) do
    ExecProcess (fun info ->
      info.Arguments <- "-interaction=nonstopmode \"" + (IO.Path.GetFileName(tex)) + "\""
      info.WorkingDirectory <- ctx.Output
      info.FileName <- "pdflatex" ) TimeSpan.MaxValue |> ignore
)

"livehtml" ==> "run"
"latex" ==> "pdf"
"livehtml" ==> "webpreview"

RunTargetOrDefault "help"
