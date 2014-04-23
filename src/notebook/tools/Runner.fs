module Runner

open System.IO
open FSharp.Literate
open FSharp.Markdown
open System.Reflection

// ----------------------------------------------------------------------------
// Directory helpers
// ----------------------------------------------------------------------------

/// Correctly combine two paths
let (@@) a b = Path.Combine(a, b)

/// Ensure that directory exists
let ensureDirectory path =
  let dir = DirectoryInfo(path)
  if not dir.Exists then dir.Create()

/// Copy all files from source to target
let rec copyFiles source target =
  ensureDirectory target
  for f in Directory.GetDirectories(source) do
    copyFiles f (target @@ Path.GetFileName(f))
  for f in Directory.GetFiles(source) do
    File.Copy(f, (target @@ Path.GetFileName(f)), true)

// ----------------------------------------------------------------------------
// Markdown document processing tools
// ----------------------------------------------------------------------------

/// Extract text from a list of spans such as document heading
let rec extractText (spans:MarkdownSpans) =
  spans |> List.collect (function
    | Matching.SpanNode(_, spans) -> extractText spans
    | Literal text | InlineCode text -> [text]
    | _ -> [])

/// Extract the text of the <h1> element 
let rec extractTitle (pars:MarkdownParagraphs) =
  pars |> List.tryPick (function
    | Heading(1, text) -> Some(String.concat " " (extractText text))
    | Matching.ParagraphNested(_, pars) -> extractTitle (List.concat pars)
    | _ -> None )

/// Generate file for the specified document, using a given template and title
let generateFile (template:string) path (doc:LiterateDocument) title = 
  let html = 
    template.Replace("{tooltips}", doc.FormattedTips)
            .Replace("{document}", Literate.WriteHtml(doc))
            .Replace("{page-title}", title)
  File.WriteAllText(path, html)

// ----------------------------------------------------------------------------
// Process script files in the root folder & generate HTML files
// ----------------------------------------------------------------------------

// Get the root directory where the script files are.
//
// This is tricky. As a dirty hack, we use 'packages' folder as the output
// so that all libraries are loaded from 'packages' using 'probing' in App.config.
// (otherwise, they are loaded twice from different directories and things break).
//
// If 'packages' are in project directory, this is just '..', but if they are
// in solution directory, this is '../UnknownProjectName'. So we just try looking 
// for templates..
let root = 
  let app = Assembly.GetExecutingAssembly().Location
  let appDir = Path.GetDirectoryName(app)
  let probing = (appDir @@ "..")::(List.ofSeq (Directory.GetDirectories(appDir @@ "..")))
  probing |> Seq.find (fun dir -> File.Exists(dir @@ "styles" @@ "template.html"))

// Process scripts in the 'root' directory and put them into output
let template() = File.ReadAllText(root @@ "output" @@ "styles" @@ "template.html")

/// Creates the 'output' directory and puts all formatted script files there
let processScriptFiles () =
  // Ensure 'output' directory exists
  ensureDirectory (root @@ "output")
  // Copy content of 'styles' to the output
  copyFiles (root @@ "styles") (root @@ "output" @@ "styles")

  // FSI evaluator will put images into 'output/images' and 
  // refernece them as './images/image1.png' in the HTML
  let fsi = Formatters.createFsiEvaluator "." (root @@ "output")

  /// Recursively process all files in the directory tree
  let processDirectory indir outdir = 
    ensureDirectory outdir
    // Get all *.fsx and *.md files and yield functions to parse them
    let files = 
      [ for f in Directory.GetFiles(indir, "*.fsx") do
          yield f, fun () -> Literate.ParseScriptFile(f, fsiEvaluator=fsi)
        for f in Directory.GetFiles(indir, "*.md") do
          yield f, fun () -> Literate.ParseMarkdownFile(f, fsiEvaluator=fsi) ]
    
    // Process all the files that have not changed since the last time
    [ for file, func in files do
        let name = Path.GetFileNameWithoutExtension(file)
        let output = outdir @@ (name + ".html")
        
        // We always need the title, so for now, just always regenerate the files 
        //
        //   let changeTime = File.GetLastWriteTime(file)
        //   let generateTime = File.GetLastWriteTime(output)
        //   if changeTime > generateTime then
        
        // Parse the document, save it & return its name with a title
        printfn "Generating '%s.html'" name
        let doc = func ()
        let title = defaultArg (extractTitle doc.Paragraphs) "Untitled"
        generateFile (template()) output doc title
        yield (name + ".html"), title ]

  processDirectory root (root @@ "output")


// ----------------------------------------------------------------------------
// Startup script to open browser
// ----------------------------------------------------------------------------

/// Find or generate a default file that we want to show in browser
let getDefaultFile = function
  | [] -> failwith "No script files found!"
  | [file, _] -> file // If there is just one file, return it
  | generated ->
      // If there is custom default or index file, use it
      let existingDefault =
        Directory.GetFiles(root @@ "output") |> Seq.tryPick (fun f ->
          match Path.GetFileName(f).ToLower() with 
          | "default.html" | "index.html" -> Some(Path.GetFileName(f)) | _ -> None)
      match existingDefault with
      | None ->
          // Otherwise, generate simple page with list of all files
          let items = 
            [ for file, title in generated ->
                [Paragraph [ DirectLink([Literal title], (file,None)) ]] ]
          let pars = 
            [ Heading(1, [Literal "FsLab Notebooks"])
              ListBlock(Unordered, items) ]
          let doc = LiterateDocument(pars, "", dict[], LiterateSource.Markdown "", "", Seq.empty)
          generateFile (template()) (root @@ "output" @@ "index.html") doc "FsLab Notebooks"
          "index.html"
      | Some fn -> fn

// Process all script files and get a list of produced files
let builtFiles = processScriptFiles()
let file = getDefaultFile builtFiles
System.Diagnostics.Process.Start(root @@ "output" @@ file) |> ignore