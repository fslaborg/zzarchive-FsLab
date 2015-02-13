namespace FsLab

open FsLab.Runner
open System.IO
open System.Reflection
open FSharp.Literate

type Journal() =

  // Get the root directory where the script files are.
  //
  // This is tricky. As a dirty hack, we use 'packages' folder as the output
  // so that all libraries are loaded from 'packages' using 'probing' in App.config.
  // (otherwise, they are loaded twice from different directories and things break).
  //
  // If 'packages' are in project directory, this is just '..', but if they are
  // in solution directory, this is '../UnknownProjectName'. So we just try looking 
  // for templates..
  static let defaultRoot () = 
    let app = Assembly.GetEntryAssembly().Location
    let appDir = Path.GetDirectoryName(app)
    let probing = (appDir @@ "..")::(List.ofSeq (Directory.GetDirectories(appDir @@ "..")))
    probing |> Seq.find (fun dir -> File.Exists(dir @@ "Main.fs")) 

  static member Process(?browse, ?root, ?outputKind, ?templateLocation, ?floatFormat, ?whiteList) =
    // Process all script files and get a list of produced files
    let ctx = 
      { Root = match root with Some r -> r | _ -> defaultRoot()
        OutputKind = defaultArg outputKind OutputKind.Html 
        FloatFormat = defaultArg floatFormat "G4"
        TemplateLocation = templateLocation
        FileWhitelist = whiteList }
    let builtFiles = processScriptFiles ctx
    let file = getDefaultFile ctx builtFiles
    if browse = Some true && ctx.OutputKind = OutputKind.Html then
      System.Diagnostics.Process.Start(ctx.Root @@ "output" @@ file) |> ignore
