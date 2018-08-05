// --------------------------------------------------------------------------------------
// Runner for processing FsLab Journals - this gets executed when you run the project.
// All work is deleagetd to "build.fsx" FAKE script - we just run it using cmd/bash.
// --------------------------------------------------------------------------------------
open System.IO
open System.Diagnostics

let scriptDir = __SOURCE_DIRECTORY__

let exec shell exe args workingDir =
    let info = ProcessStartInfo (exe, UseShellExecute = shell, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = workingDir, Arguments = args)
    let proc = new Process(StartInfo = info)

    proc.Start() |> ignore
    proc.WaitForExit()
    let code = proc.ExitCode
    if code <> 0 then failwithf "%s %s failed, error code %d" exe args code

let fsx2html args = 
  if System.Type.GetType("Mono.Runtime") <> null then
    exec false "mono" ("packages/FSharp.Literate.Scripts/tools/fsx2html.exe " + args) scriptDir
  else
    exec false "packages/FSharp.Literate.Scripts/tools/fsx2html.exe" args scriptDir

[<EntryPoint>]
let main argv = 
  // Start fx2html using the appropriate command
  fsx2html "--show --watch"
