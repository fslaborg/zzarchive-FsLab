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
  [ "Deedle"
    "Deedle.RPlugin"
    "FSharp.Charting"
    "FSharp.Data"
    "MathNet.Numerics"
    "MathNet.Numerics.FSharp"
    "RProvider" 
    "R.NET" 
    "RDotNet.FSharp" ]

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

Target "Clean" (fun _ ->
    CleanDirs ["temp"; "nuget"]
)

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "GenerateFsLab" (fun _ ->
  // Find package version information in 'FsLab.nuspec'
  let nuspec = XElement.Load(__SOURCE_DIRECTORY__ + "/src/FsLab.nuspec")
  let xn s = XName.Get(s, "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd")
  let packageVersions = 
    nuspec.Descendants(xn "dependency")
    |> Seq.map (fun elem -> 
        elem.Attribute(XName.Get "id").Value, elem.Attribute(XName.Get "version").Value)
    |> dict

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
        for package in packages do
          yield sprintf "#I \"%s%s\"" folder (getLibDir package) ]
  
  // Generate #r for all libraries
  let references = 
    packages
    |> List.collect getAssemblies
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
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "temp"
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
)

Target "All" DoNothing

"Clean"
  ==> "GenerateTemplate"
//  ==> "BuildTemplate"
  ==> "All"

"Clean" 
  ==> "RestorePackages"
  ==> "GenerateFsLab"
  ==> "NuGet"
  ==> "All"

RunTargetOrDefault "All"