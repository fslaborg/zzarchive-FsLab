#nowarn "40"
#nowarn "211"
#I "packages/FAKE/tools"
#I "../../packages/FAKE/tools"
#r "FakeLib"
open Fake

let scriptDir =  __SOURCE_DIRECTORY__

let fsx2html args = 
  let code = 
    if System.Type.GetType("Mono.Runtime") <> null then
      Shell.Exec("mono", "packages/FSharp.Literate.Scripts/tools/fsx2html.exe " + args, scriptDir)
    else
      Shell.Exec("packages/FSharp.Literate.Scripts/tools/fsx2html.exe", args, scriptDir)
  if code <> 0 then failwithf "fsx2html.exe %s failed" args

Target "help" (fun _ ->
  fsx2html "--help"
)

Target "html" (fun _ ->    
  fsx2html (sprintf "--html")
)

Target "latex" (fun _ ->
  fsx2html (sprintf "--latex")
)

Target "run" (fun _ ->
  fsx2html (sprintf "--html --serve --show")
)

Target "webpreview" (fun _ ->
  fsx2html (sprintf "--html --serve")
)

Target "pdf" (fun _ ->
  fsx2html (sprintf "--pdf --serve")
)

RunTargetOrDefault "help"
