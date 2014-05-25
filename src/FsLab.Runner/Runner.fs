module internal FsLab.Runner

open System.IO
open FSharp.Literate
open FSharp.Markdown
open System.Reflection

// ----------------------------------------------------------------------------
// Directory and location helpers
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

/// Represents state passed around during processing
type ProcessingContext = 
  { Root : string 
    OutputKind : OutputKind }

// Process scripts in the 'root' directory and put them into output
let htmlTemplate ctx = File.ReadAllText(ctx.Root @@ "output" @@ "styles" @@ "template.html")
let texTemplate ctx = File.ReadAllText(ctx.Root @@ "output" @@ "styles" @@ "template.tex")

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

let rec dropTitle (pars:MarkdownParagraphs) : MarkdownParagraphs = 
  pars |> List.collect (function
    | Heading(1, text) -> []
    | Matching.ParagraphNested(o, pars) -> 
        [ Matching.ParagraphNested(o, pars |> List.map dropTitle) ]
    | other -> [other] )

/// Generate file for the specified document, using a given template and title
let generateFile ctx path (doc:LiterateDocument) title = 
  Formatters.currentOutputKind <- ctx.OutputKind
  if ctx.OutputKind = OutputKind.Html then
    let template = htmlTemplate ctx
    let html = 
      template.Replace("{tooltips}", doc.FormattedTips)
              .Replace("{document}", Literate.WriteHtml(doc))
              .Replace("{page-title}", title)
    File.WriteAllText(path, html)
  else
    let template = texTemplate ctx
    let doc = doc.With(paragraphs=dropTitle doc.Paragraphs)
    let tex = 
      template.Replace("{tooltips}", doc.FormattedTips)
              .Replace("{contents}", Literate.WriteLatex(doc))
              .Replace("{page-title}", title)
    File.WriteAllText(Path.ChangeExtension(path, "tex"), tex)

// ----------------------------------------------------------------------------
// Process script files in the root folder & generate HTML files
// ----------------------------------------------------------------------------

/// Creates the 'output' directory and puts all formatted script files there
let processScriptFiles ctx =
  // Ensure 'output' directory exists
  let root = ctx.Root
  ensureDirectory (root @@ "output")

  // Copy content of 'styles' to the output (from the NuGet package source)
  let rootPackages = 
    if Directory.Exists(root @@ "packages") then root @@ "packages"
    else root @@ "../packages"
  let runnerRoot = 
    Directory.GetDirectories(rootPackages) |> Seq.find (fun p -> 
      Path.GetFileName(p).StartsWith "FsLab.Runner")
  copyFiles (runnerRoot @@ "styles") (root @@ "output" @@ "styles")

  // FSI evaluator will put images into 'output/images' and 
  // refernece them as './images/image1.png' in the HTML
  let fsi = Formatters.createFsiEvaluator "." (root @@ "output")

  /// Recursively process all files in the directory tree
  let processDirectory indir outdir = 
    ensureDirectory outdir
    // Get all *.fsx and *.md files and yield functions to parse them
    let files = 
      [ for f in Directory.GetFiles(indir, "*.fsx") do
          if Path.GetFileNameWithoutExtension(f).ToLower() <> "build" then
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
        generateFile ctx output doc title
        yield (name + ".html"), title ]

  processDirectory root (root @@ "output")


// ----------------------------------------------------------------------------
// Startup script to open browser
// ----------------------------------------------------------------------------

/// Find or generate a default file that we want to show in browser
let getDefaultFile ctx = function
  | [] -> failwith "No script files found!"
  | [file, _] -> file // If there is just one file, return it
  | generated ->
      // If there is custom default or index file, use it
      let existingDefault =
        Directory.GetFiles(ctx.Root @@ "output") |> Seq.tryPick (fun f ->
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
          generateFile ctx (ctx.Root @@ "output" @@ "index.html") doc "FsLab Notebooks"
          "index.html"
      | Some fn -> fn
