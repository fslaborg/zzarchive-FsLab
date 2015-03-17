// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r "../lib/FSharp.Compiler.Interactive.Settings.dll"

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
#load "packages/FsLab/FsLab.fsx"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "../src/FsLab.Runner/Formatters.fs"
open FsLab
#load "../src/FsLab.Runner/Runner.fs"
open System
open Fake 

open FSharp.Literate
open FsLab.Formatters
open FsLab.Runner

Target "Generate" (fun _ ->
    let ctx = 
      { Root = __SOURCE_DIRECTORY__ @@ "journals"
        Output = __SOURCE_DIRECTORY__ @@ "output"
        OutputKind = OutputKind.Html 
        FloatFormat = "G4"
        TemplateLocation = Some(__SOURCE_DIRECTORY__ @@ "packages/FsLab.Runner")
        FileWhitelist = None }
    let builtFiles = processScriptFiles ctx
    let file = getDefaultFile ctx builtFiles
    System.Diagnostics.Process.Start(ctx.Root @@ "output" @@ file) |> ignore
)

Target "All" DoNothing

"Generate" ==> "All" 
RunTargetOrDefault "All"
