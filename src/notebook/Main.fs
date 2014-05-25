module Main
open FsLab
open FSharp.Literate

[<EntryPoint>]
let main args = 
  // Usage:
  //
  //  --latex              Generate output as LaTeX rather than the default HTML
  //  --non-interactive    Do not open the generated HTML document in web browser
  //
  let latex = args |> Seq.exists ((=) "--latex")
  let browse = args |> Seq.exists ((=) "--non-interactive") |> not
  if latex then Journal.Process(outputKind = OutputKind.Latex)
  else Journal.Process(browse)
  0
