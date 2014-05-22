// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq"
open System
open System.IO
open System.Xml.Linq
open System.Linq
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System.Text.RegularExpressions

setEnvironVar "MSBuild" (ProgramFilesX86 @@ @"\MSBuild\12.0\Bin\MSBuild.exe")

// --------------------------------------------------------------------------------------
// FsLab packages and configuration
// --------------------------------------------------------------------------------------

let project = "FsLab"
let authors = ["F# Data Science Working Group"]
let summary = "F# Data science package"
let description = """
  FsLab is a single package that gives you all you need for doing data science with
  F#. FsLab includes explorative data manipulation library, type providers for easy
  data access, simple charting library, support for integration with R and numerical
  computing libraries. All available in a single package and ready to use!"""
let tags = "F# fsharp deedle series statistics data science r type provider mathnet"

/// List of packages included in FsLab
/// (Version information is generated automatically based on 'FsLab.nuspec')
let packages = 
  [ "Deedle", "1.0.0"
    "Deedle.RPlugin", "1.0.0"
    "FSharp.Charting", "0.90.6"
    "FSharp.Data", "2.0.8"
    "MathNet.Numerics", "3.0.0-beta01"
    "MathNet.Numerics.FSharp", "3.0.0-beta01"
    "RProvider", "1.0.9"
    "R.NET", "1.5.5" 
    "RDotNet.FSharp", "0.1.2.1" ]

let notebookPackages = 
  [ "FSharp.Compiler.Service", "0.0.44"
    "FSharp.Formatting", "2.4.8" 
    "Microsoft.AspNet.Razor", "2.0.30506.0"
    "RazorEngine", "3.3.0" ]

/// Returns assemblies that should be referenced for each package
let getAssemblies package = 
    match package with
    | "Deedle.RPlugin" -> ["Deedle.RProvider.Plugin.dll"]
    | "FSharp.Charting" -> ["System.Windows.Forms.DataVisualization.dll"; "FSharp.Charting.dll"]
    | "RProvider" -> ["RDotNet.dll"; "RDotNet.NativeLibrary.dll"; "RProvider.Runtime.dll"; "RProvider.dll"]
    | "R.NET" -> []
    | package -> [package + ".dll"]

// Generate #I directive for the following folders:
let folders = 
  [ "packages/"           // new F# project in VS with create directory for solution disabled
    "../packages/"        // new F# project in VS with create directory for solution enabled
    "../../packages/"     // fsharp-project-scaffold template
    "../../../packages/"] // just in case

// --------------------------------------------------------------------------------------
// FAKE build targets
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")
let packageVersions = dict (packages @ notebookPackages)

Target "Clean" (fun _ ->
    CleanDirs ["temp"; "nuget"; "bin"]
)

Target "UpdateVersions" (fun _ ->
  let (!) n = XName.Get(n)

  // Helpers for generating "packages.config" file
  let makePackage (name, ver) = 
    XElement(!"package", XAttribute(!"id", name), XAttribute(!"version", ver))
  let makePackages packages = 
    XDocument(XElement(! "packages", packages |> Seq.map makePackage))

  // "src/packages.config" is used just for development (so that we can
  // edit the "FsLab.fsx" file and get decent autocomplete)
  makePackages(packages).Save("src/packages.config")
 
  // "src/notebook/packages.config" lists the packages that 
  // are referenced in the FsLab Notebook project
  let allPackages = 
    ["FsLab", release.NugetVersion] @ 
    packages @ notebookPackages
  makePackages(allPackages).Save("src/notebook/packages.config")

  // "src/notebook/Tutorial.fsx" needs to be updated to 
  // reference correct version of FsLab in the #load command
  let pattern = "packages/FsLab.(.*)/FsLab.fsx"
  let replacement = sprintf "packages/FsLab.%s/FsLab.fsx" release.NugetVersion
  let path = "./src/notebook/Tutorial.fsx"
  let text = File.ReadAllText(path)
  let text = Regex.Replace(text, pattern, replacement)
  File.WriteAllText(path, text)

  // "src\notebook\FsLab.Notebook.fsproj" contains <HintPath> elements
  // that points to the specific version in packages directory
  // This bit goes over all the <HintPath> elements & updates them
  let (!) n = XName.Get(n, "http://schemas.microsoft.com/developer/msbuild/2003")
  let path = "src/notebook/FsLab.Notebook.fsproj"
  let fsproj = XDocument.Load(path)
  let reg = Regex(@"\$\(SolutionDir\)\\packages\\([a-zA-Z\.]*)\.[^\\]*\\(.*)")
  for hint in fsproj.Descendants(!"HintPath") do
    let res = reg.Match(hint.Value)
    if res.Success then
      let package = res.Groups.[1].Value
      let rest = res.Groups.[2].Value
      let version = packageVersions.[package]
      hint.Value <- sprintf @"$(SolutionDir)\packages\%s.%s\%s" package version rest
  fsproj.Save(path + ".updated")  
  DeleteFile path
  Rename path (path + ".updated")
)

Target "RestorePackages" (fun _ ->
    !! "./src/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "GenerateFsLab" (fun _ ->
  // Get directory with binaries for a given package
  let getLibDir package =
    let baseDir = package + "." + packageVersions.[package]
    match package with
    | "RProvider" -> baseDir + "/lib"
    | _ -> baseDir + "/lib/net40"

  // Additional lines to be included in FsLab.fsx
  let nowarn = ["#nowarn \"211\""]
  let extraInitAll  = File.ReadLines(__SOURCE_DIRECTORY__ + "/src/FsLab.fsx")  |> Array.ofSeq
  let startIndex = extraInitAll |> Seq.findIndex (fun s -> s.Contains "***FsLab.fsx***")
  let extraInit = extraInitAll .[startIndex + 1 ..] |> List.ofSeq

  // Generate #I for all library, for all possible folder
  let includes = 
    [ for folder in folders do
        for package, _ in packages do
          yield sprintf "#I \"%s%s\"" folder (getLibDir package) ]
  
  // Generate #r for all libraries
  let references = 
    packages
    |> List.collect (fst >> getAssemblies)
    |> List.map (sprintf "#r \"%s\"")

  // Write everything to the 'temp/FsLab.fsx' file
  let lines = nowarn @ includes @ references @ extraInit
  File.WriteAllLines(__SOURCE_DIRECTORY__ + "/temp/FsLab.fsx", lines)
)

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    NuGet (fun p -> 
        { p with   
            Dependencies = packages
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        ("src/" + project + ".nuspec")
)

// --------------------------------------------------------------------------------------
// Build the FsLab template project
// --------------------------------------------------------------------------------------

Target "GenerateTemplate" (fun _ ->
  CopyRecursive "src/notebook" "temp/notebook/" true |> ignore
  "misc/logo.png" |> CopyFile "temp/notebook/__TemplateIcon.png"
  "misc/preview.png" |> CopyFile "temp/notebook/__PreviewImage.png"
  !! "temp/notebook/**" |> Zip "temp/notebook" "temp/notebook.zip"

  CopyRecursive "src/template" "temp/template/" true |> ignore
  ensureDirectory "temp/template/ProjectTemplates"
  "temp/notebook.zip" |> CopyFile "temp/template/FsLab Notebook.zip" 
  "temp/notebook.zip" |> CopyFile "temp/template/ProjectTemplates/FsLab Notebook.zip" 
  "misc/logo.png" |> CopyFile "temp/template/logo.png"
  "misc/preview.png" |> CopyFile "temp/template/preview.png"
)

Target "BuildTemplate" (fun _ ->
  !! "temp/template/FsLab.Template.sln" 
  |> MSBuildDebug "" "Rebuild"
  |> ignore
  "temp/template/bin/Debug/FsLab.Template.vsix" |> CopyFile "bin/FsLab.Template.vsix"
)

Target "All" DoNothing

"Clean"
  ==> "GenerateTemplate"
  ==> "BuildTemplate"
  ==> "All"

"Clean" 
  ==> "UpdateVersions"
  ==> "RestorePackages"
  ==> "GenerateFsLab"
  ==> "NuGet"
  ==> "All"

RunTargetOrDefault "All"