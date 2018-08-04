module FsLab.Formatters.Server
open Suave

// --------------------------------------------------------------------------------------
// Lightweight HTTP server that can host Suave WebParts or strings in background
// --------------------------------------------------------------------------------------

type SimpleServer() =
  let pages = ResizeArray<_>()

  /// Drop the <n> part from http://localhost:123/<n>/something
  let dropPrefix part ctx = 
    let u = ctx.request.url
    let local = 
      match List.ofArray (u.LocalPath.Substring(1).Split('/')) with
      | _::rest -> String.concat "/" rest
      | [] -> ""
    let url = System.Uri(u.Scheme + "://" + u.Authority + "/" + local)
    { ctx with request = { ctx.request with url = url }} |> part

  // Server that serves pages from the given array
  let handlePage n =
    if n < pages.Count then pages.[n]
    else RequestErrors.NOT_FOUND "Page not found"
  let app =
    choose [ Filters.pathScan "/%d" handlePage
             Filters.pathScan "/%d/%s" (fst >> handlePage) ]

  /// Start server on the first available port in the range 8000..10000
  /// and return the port number once the server is started (asynchronously)
  let startServer () =
    Async.FromContinuations(fun (cont, _, _) ->
      let startedEvent = Event<_>()
      startedEvent.Publish.Add(cont)
      async {
        // Try random ports until we find one that works
        let rnd = System.Random()
        while true do
          let port = 8000 + rnd.Next(2000)
          let local = Suave.Http.HttpBinding.mkSimple HTTP "127.0.0.1" port
          let logger = Suave.Logging.Loggers.saneDefaultsFor Logging.LogLevel.Error
          let config = { defaultConfig with bindings = [local]; logger = logger }
          let started, start = startWebServerAsync config app
          // If it starts OK, we get TCP binding & report success via event
          async { let! running = started
                  startedEvent.Trigger(running) } |> Async.Start
          // Try starting the server and handle SocketException
          try do! start
          with :? System.Net.Sockets.SocketException -> () }
      |> Async.Start )

  // Start the server and wait for the port as task, while other things happen
  let port =
    async { let! pts = startServer()
            let first = pts |> Seq.choose id |> Seq.head
            return first.binding.port }
    |> Async.StartAsTask

  /// Returns the port where the server is running
  member x.Port = port.Result

  /// Add web part to the server. Returns the URL prefix where it's hosted
  member x.AddPart(part) =
    pages.Add(dropPrefix part)
    sprintf "http://localhost:%d/%d" port.Result (pages.Count - 1)

  /// Add page to the server. Returns the URL where it's hosted
  member x.AddPage(page) =
    pages.Add(Successful.OK(page))
    sprintf "http://localhost:%d/%d" port.Result (pages.Count - 1)

// --------------------------------------------------------------------------------------

let instance = Lazy.Create(fun () -> SimpleServer())

#if HAS_FSI_ADDHTMLPRINTER
fsi.HtmlPrinterParameters.["background-server"] <- 
  System.Func<string, string>(fun s -> instance.Value.AddPage(s))
#endif