// --------------------------------------------------------------------------------------
// Runner for processing FsLab Journals - this gets executed when you run the project.
// All work is deleagetd to "build.fsx" FAKE script - we just run it using cmd/bash.
// --------------------------------------------------------------------------------------
open System
open System.Diagnostics

let scriptDir = __SOURCE_DIRECTORY__

let exec fail shell exe args workingDir =
    printfn "%s %s (in %s)" exe args workingDir
    let info = ProcessStartInfo (exe, UseShellExecute = shell, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = workingDir, Arguments = args)
    let proc = new Process(StartInfo = info)

    proc.Start() |> ignore
    proc.WaitForExit()
    let code = proc.ExitCode
    if fail && code <> 0 then 
       printfn "%s %s failed, error code %d. Press ENTER to continue." exe args code
       Console.ReadLine() |> ignore
       exit code

let fsx2html fail args = 
  if System.Type.GetType("Mono.Runtime") <> null then
    exec fail true "mono" (__SOURCE_DIRECTORY__ + "/packages/FSharp.Literate.Scripts/tools/fsx2html.exe " + args) scriptDir
  else
    exec fail false (__SOURCE_DIRECTORY__ + @"\packages\FSharp.Literate.Scripts\tools\fsx2html.exe") args scriptDir

[<EntryPoint>]
let main argv = 
  // Start fx2html using the appropriate command
  printfn "starting fsx2html..."
  fsx2html false "--html --show --watch"
  while true do 
      printfn "restarting fsx2html, perhaps due to OutOfMemoryException"
      fsx2html false "--html --watch"
  0
