// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.


open System.IO
open System.Diagnostics

[<EntryPoint>]
let main argv = 
  let (@@) p1 p2 = Path.Combine(p1, p2)
  let currentDir = __SOURCE_DIRECTORY__

  if File.ReadAllBytes(currentDir @@ "build.fsx").Length < 10 then
    //
    for f in Directory.GetFiles(currentDir @@ "packages/FsLab.Runner/tools") do
      File.Copy(f, currentDir @@ Path.GetFileName(f), true)

  let info = new ProcessStartInfo("cmd", "/c build.cmd run", UseShellExecute = false, WorkingDirectory = currentDir)

  let proc = Process.Start(info)
  proc.WaitForExit()
  proc.ExitCode