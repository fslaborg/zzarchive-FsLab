// --------------------------------------------------------------------------------------
// Build script that automatically produces HTML or PDF version of the Journal
// --------------------------------------------------------------------------------------
// 
// Usage:
//   build html   - Generate HTML output for all scripts
//   build latex  - Generate LaTeX output for all scripts
//   build pdf    - Generate LaTeX output & compile using 'pdflatex'
//
// --------------------------------------------------------------------------------------

#r "packages/FAKE/tools/FakeLib.dll"
open System
open System.IO
open Fake

// Set current directory to the one where the executable is located
Environment.CurrentDirectory <- 
  if Directory.Exists(__SOURCE_DIRECTORY__ @@ "packages") then __SOURCE_DIRECTORY__
  elif Directory.Exists(__SOURCE_DIRECTORY__ @@ "../packages") then __SOURCE_DIRECTORY__ @@ ".."
  else failwith "Build the journal in Visual Studio to install packages!"

// Rebuild the Journal project so that we can run it 
Target "BuildJournal" (fun _ ->
  !! "*.sln"
  |> MSBuildDebug "" "Rebuild"
  |> Log "Output: "
)

// Run journal.exe to generate HTML and do not open web browser
Target "html" (fun _ ->
  let journal = !! "packages/*.exe" |> Seq.head
  ExecProcess (fun info ->
    info.Arguments <- "--non-interactive"
    info.FileName <- journal ) TimeSpan.MaxValue |> ignore
)

// Run journal.exe to generate LaTeX 
Target "latex" (fun _ ->
  let journal = !! "packages/*.exe" |> Seq.head
  ExecProcess (fun info ->
    info.Arguments <- "--latex"
    info.FileName <- journal ) TimeSpan.MaxValue |> ignore
)

// Once we generate LaTeX, compile it using 'pdflatex'
Target "pdf" (fun _ ->
  for tex in !! (__SOURCE_DIRECTORY__ @@ "output/*.tex" ) do
    ExecProcess (fun info ->
      info.Arguments <- "-interaction=nonstopmode \"" + (Path.GetFileName(tex)) + "\""
      info.WorkingDirectory <- __SOURCE_DIRECTORY__ @@ "output"
      info.FileName <- "pdflatex" ) TimeSpan.MaxValue |> ignore
)

// Display basic help on how to use the build script
Target "help" (fun _ ->
  printfn "Usage:"
  printfn "  build html   - Generate HTML output for all scripts"
  printfn "  build latex  - Generate LaTeX output for all scripts"
  printfn "  build pdf    - Generate LaTeX output & compile using 'pdflatex'"
)

"BuildJournal" ==> "html"
"BuildJournal" ==> "latex" ==> "pdf"
RunTargetOrDefault "help"