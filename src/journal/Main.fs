module Main
open FsLab
open FSharp.Literate

[<EntryPoint>]
let main args = 
  // Usage:
  //
  //  --latex              Generate output as LaTeX rather than the default HTML
  //  --non-interactive    Do not open the generated HTML document in web browser
  //  --path name          Root path where to look for script files to process
  //
  let latex = args |> Seq.exists ((=) "--latex")
  let browse = args |> Seq.exists ((=) "--non-interactive") |> not
  let path = 
    let idx = args |> Seq.tryFindIndex ((=) "--path")
    match idx with
    | None -> None
    | Some(i) -> Some args.[i+1]    // return next argument

  match path with
  | None -> 
      if latex then Journal.Process(outputKind = OutputKind.Latex)
      else Journal.Process(browse)
  | Some(root) ->
      if latex then Journal.Process(root = root, outputKind = OutputKind.Latex)
      else Journal.Process(root = root, browse = browse)
  0
