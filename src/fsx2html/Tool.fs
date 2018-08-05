module Tool

open FSharp.Literate
open FSharp.Literate.Scripts
open System
open System.Diagnostics
open System.Text
open System.IO

open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Suave.Operators
open Suave.Filters

// --------------------------------------------------------------------------------------
// Runner configuration - You can change some basic settings of ProcessingContext here
// --------------------------------------------------------------------------------------

let handleError(err:FsiEvaluationFailedInfo) =
    printfn "Evaluating F# snippet failed:\n%s\nThe snippet evaluated:\n%s" err.StdErr err.Text

let createContext source output = 
  { ProcessingContext.Create(source) with
      //OutputKind = OutputKind.Html
      Output = output;
      //Styles = ...
      FailedHandler = handleError }

// --------------------------------------------------------------------------------------
// Processes journals and runs Suave server to host them on localhost
// --------------------------------------------------------------------------------------


let localPort = 8901

let generateJournals ctx =
  let builtFiles = ScriptProcessing.processJournals ctx
  printfn "All journals updated."
  ScriptProcessing.getIndexJournal ctx builtFiles

let refreshEvent = new Event<_>()

let socketHandler (webSocket : WebSocket) _ = socket {
  while true do
    do!
      refreshEvent.Publish
      |> Control.Async.AwaitEvent
      |> Suave.Sockets.SocketOp.ofAsync
    do! webSocket.send Text (ByteSegment (Encoding.UTF8.GetBytes "refreshed")) true }

let startWebServer ctx =
    let defaultBinding = defaultConfig.bindings.[0]
    let withPort = { defaultBinding.socketBinding with port = uint16 localPort }
    let serverConfig =
        { defaultConfig with
            //logger = Suave.Logging.Log.create "fsx2html"
            //logger = Logging.Logger.
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

// --------------------------------------------------------------------------------------
// Build targets - for example, run `build GenerateLatex` to produce latex output
// --------------------------------------------------------------------------------------

let generateLatex ctx = 
    generateJournals { ctx with OutputKind = OutputKind.Latex }

let generateHtml ctx = 
    generateJournals { ctx with Standalone = true }

let startServer ctx = 
    printfn "watching in %s" ctx.Root
    let handleWatcherEvents (e:IO.FileSystemEventArgs) =
        printfn "%s" <| sprintf "%s was changed." e.FullPath
        let fi = FileInfo e.FullPath
        printfn "%s" <| sprintf "%s was changed ok." fi.Name
        if fi.Attributes.HasFlag IO.FileAttributes.Hidden ||
           fi.Attributes.HasFlag IO.FileAttributes.Directory then ()
        else ScriptProcessing.updateJournals ctx |> ignore
        refreshEvent.Trigger()

    let watcher = new System.IO.FileSystemWatcher(ctx.Root, "*.fsx")
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    watcher.EnableRaisingEvents <- true
    startWebServer ctx
    watcher

let showHtml(fileName) = 
    Diagnostics.Process.Start(sprintf "http://localhost:%d/%s" localPort fileName) |> ignore

let readkey() = 
    System.Console.ReadKey() |> ignore

let exec shell workingDir exe args =
    let info = ProcessStartInfo (exe, UseShellExecute = shell, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = workingDir, Arguments = args)
    let proc = new Process(StartInfo = info)

    proc.Start() |> ignore
    proc.WaitForExit()
    let code = proc.ExitCode
    if code <> 0 then failwithf "%s %s failed, error code %d" exe args code

let help() = 
    printfn "usage: fsx2html.exe (--help|--html|--latex|--run|--watch|--pdf|--output directory|--source directory|directory)"
    printfn "Use 'build run' to produce HTML journals in the background "
    printfn "and host them locally using a simple web server."
    printfn ""
    printfn "Other usage options:"
    printfn "  fsx2html.exe --html   - Generate HTML output for all scripts"
    printfn "  fsx2html.exe --run    - Generate HTML, host it and keep it up-to-date"
    printfn "  fsx2html.exe --latex  - Generate LaTeX output for all scripts"
    printfn "  fsx2html.exe --pdf    - Generate LaTeX output & compile using 'pdflatex'"
    exit 1

[<EntryPoint>]
let main argv = 
    let mutable html = None
    let mutable latex = false
    let mutable show = false
    let mutable watch = false
    let mutable pdf = false
    let mutable source = None
    let mutable sourceFlag = false
    let mutable output = None
    let mutable outputFlag = false
    for arg in argv do 
        match arg with 
        | _ when outputFlag -> 
            if output.IsSome then help() 
            output <- Some arg
            outputFlag <- false
        | _ when sourceFlag -> 
            if source.IsSome then help() 
            source <- Some arg
            sourceFlag <- false
        | "--help" -> help()
        | "--html"  | "/html" -> html <- Some true
        | "--latex"  | "/latex" -> latex <- true
        | "--show" | "/show" -> show <- true
        | "--run" | "/run" -> show <- true; watch <- true
        | "--watch" | "/watch" -> watch <- true
        | "--pdf" | "/pdf" -> pdf <- true
        | "--output" | "/output" -> outputFlag <- true
        | "--source" | "/source" -> sourceFlag <- true
        | dir -> 
            if source.IsSome then help() 
            source <- Some dir
        | _ -> help()
    if outputFlag then help()
    if sourceFlag then help()
    let html = defaultArg html (not latex && not pdf)

    let source = defaultArg source "."
    let source = Path.GetFullPath(source)
    let output = defaultArg output (source + "/output")
    let output = Path.GetFullPath(output)

    printfn "Reading scripts from '%s'" source
    printfn "Writing output to '%s'" output
    let ctx = createContext source output
    if html && not watch then 
        generateHtml ctx |> ignore
        
    elif latex && not watch then 
        generateLatex ctx |> ignore

    else
        let htmlFileName = if html then generateHtml ctx else ""
        let latexFileName = if latex then generateLatex ctx else ""
        let fileNameToShow = if html then htmlFileName else latexFileName
        
        let mutable watcher = null
        if watch then 
            if pdf then 
                exec true ctx.Output "pdflatex" ("-interaction=nonstopmode \"" + (Path.GetFileName(latexFileName)) + "\"")
            else
               printfn "%s" "Starting server...."
               watcher <- startServer ctx
        else
            if pdf then 
                exec true ctx.Output "pdflatex" ("\"" + Path.GetFileName(latexFileName) + "\"")

        if show then 
            showHtml(fileNameToShow)

        if watch then 
            printfn "%s" "Waiting for journal edits. Press any key to stop."
            readkey()

        System.GC.KeepAlive(watcher)
    0

