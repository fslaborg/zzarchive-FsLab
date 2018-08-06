#nowarn "40"
#nowarn "211"
#I "packages/FAKE/tools"
#I "../../packages/FAKE/tools"
#r "FakeLib"
open Fake

let scriptDir =  __SOURCE_DIRECTORY__

let exec exe args workingDir = 
  printfn "%s %s (in %s)" exe args workingDir
  Shell.Exec(exe, args, workingDir)

let fsx2html fail args = 
  let code = 
    if System.Type.GetType("Mono.Runtime") <> null then
      exec "mono" ("packages/FSharp.Literate.Scripts/tools/fsx2html.exe " + args) scriptDir
    else
      exec "packages/FSharp.Literate.Scripts/tools/fsx2html.exe" args scriptDir
  if fail && code <> 0 then 
      printfn  "fsx2html.exe %s failed" args
      exit code

Target "help" (fun _ ->
    printfn "  build run      - Generate HTML, show and update dynamically"
    printfn "  build html     - Generate HTML output for all scripts"
    printfn "  build show     - Generate HTML output for all scripts and show it"
    printfn "  build latex    - Generate LaTeX output for all scripts"
    printfn "  build pdf      - Generate PDF output for all scripts"
    exit 1
)

Target "html" (fun _ ->    
  fsx2html true "--html"
)

Target "show" (fun _ ->    
  fsx2html true "--html --show"
)


Target "latex" (fun _ ->
  fsx2html true "--latex"
)

Target "run" (fun _ ->
  fsx2html false "--html --watch --show"
  while true do 
      printfn "restarting fsx2html, perhaps due to OutOfMemoryException"
      fsx2html false "--html --watch"
)

Target "watch" (fun _ ->
  fsx2html true "--html --watch"
)

Target "pdf" (fun _ ->
  fsx2html true "--pdf --watch"
)

RunTargetOrDefault "help"
