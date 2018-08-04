// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/Paket.Core/lib/net45/Paket.Core.dll"
#r "packages/DotNetZip/lib/net20/DotNetZip.dll"
#r "System.Xml.Linq"
open System.IO
open System.Xml.Linq
open System.Linq
open Fake
open Fake.ReleaseNotesHelper

// --------------------------------------------------------------------------------------
// FsLab packages and configuration
// --------------------------------------------------------------------------------------

let buildDir = "bin"
let project = "FsLab"
let projectRunner = "FsLab.Runner"
let authors = ["FsLab Contributors"]
let summary = "F# packages for data science"
let summaryRunner = "FsLab report generator"

let description = """
  FsLab is a combination package that supports doing data science with
  F#. FsLab includes an explorative data manipulation library, type providers for easy
  data access, a simple charting library, support for integration with R and numerical
  computing libraries. All available in a single package and ready to use!"""

let descriptionRunner = """
  This package contains a library for turning FsLab experiments written as script files
  into HTML and LaTeX reports. The easiest way to use the library is to use the
  'FsLab Journal' Visual Studio template."""
let tags = "F# fsharp deedle series statistics data science r type provider mathnet machine learning ML"

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

/// List of packages included in FsLab
/// (Version information is generated automatically based on 'FsLab.nuspec')
let packages =
  [ "Deedle"
    "Deedle.RPlugin"
    "FSharp.Charting"
    "FSharp.Data"
    "MathNet.Numerics"
    "MathNet.Numerics.FSharp"
    "DynamicInterop"
    "R.NET.Community"
    "R.NET.Community.FSharp"
    "RProvider"
    "Suave"
    // XPlot + dependencies
    "XPlot.Plotly"
    "XPlot.GoogleCharts"
    "XPlot.GoogleCharts.Deedle"
    "Google.DataTable.Net.Wrapper"
    "Newtonsoft.Json" ]
  |> List.map (fun p -> p, GetPackageVersion "packages" p)

let journalPackages =
  [ "FSharp.Compiler.Service"
    "FSharpVSPowerTools.Core"
    "FSharp.Formatting" ]
 |> List.map (fun p -> p, GetPackageVersion "packages" p)

/// Returns the subfolder where the DLLs are located
let getNetSubfolder package =
    match package with
    | "Google.DataTable.Net.Wrapper" -> "lib"
    | "FSharpVSPowerTools.Core" -> "lib/net45"
    | _ when package.StartsWith("XPlot") -> "lib/net45"
    | _ -> "lib/net40"

/// Returns assemblies that should be referenced for each package
let getAssemblies package =
    match package with
    | "Deedle.RPlugin" -> ["Deedle.RProvider.Plugin.dll"]
    | "FSharp.Charting" -> ["System.Windows.Forms.DataVisualization.dll"; "FSharp.Charting.dll"]
    | "RProvider" -> ["RProvider.Runtime.dll"; "RProvider.dll"]
    | "R.NET.Community" -> ["RDotNet.dll"; "RDotNet.NativeLibrary.dll"]
    | "R.NET.Community.FSharp" -> ["RDotNet.FSharp.dll"]
    | package -> [package + ".dll"]

let exec workingDir exe args =
    let code = Shell.Exec(exe, args, workingDir) 
    if code <> 0 then failwithf "%s %s failed, error code %d" exe args code

// --------------------------------------------------------------------------------------
// FAKE targets for building FsLab and FsLab.Runner NuGet packages
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
let release = LoadReleaseNotes "RELEASE_NOTES.md"
let packageVersions = dict (packages @ journalPackages @ ["FsLab.Runner", release.NugetVersion])

Target "Clean" (fun _ ->
  CleanDirs ["temp"; "nuget"; buildDir ]
)


Target "GenerateFsLab" (fun _ ->
  // Get directory with binaries for a given package
  let getLibDir package = package + "/" + (getNetSubfolder package)
  let getLibDirVer package = package + "." + packageVersions.[package] + "/" + (getNetSubfolder package)

  // Additional lines to be included in FsLab.fsx
  let nowarn = ["#nowarn \"211\""; "#I __SOURCE_DIRECTORY__"]
  let extraInitAll  = File.ReadLines(__SOURCE_DIRECTORY__ + "/src/FsLab.fsx")  |> Array.ofSeq
  let startIndex = extraInitAll |> Seq.findIndex (fun s -> s.Contains "***FsLab.fsx***")
  let extraInit = extraInitAll.[startIndex + 1 ..] |> List.ofSeq

  // Generate #I for all library, for all possible folder
  let includes =
    [ for package, _ in packages do
        yield sprintf "#I \"../%s\"" (getLibDir package)
        yield sprintf "#I \"../%s\"" (getLibDirVer package) ]

  // Generate #r for all libraries
  let references =
    packages
    |> List.collect (fst >> getAssemblies)
    |> List.map (sprintf "#r \"%s\"")

  // Copy formatter source files to the temp directory
  let formattersDir = "src/FsLab.Formatters"
  let copyAsFsx target fn =
    ensureDirectory target
    CopyFile (target </> (Path.GetFileNameWithoutExtension(fn) + ".fsx")) fn
  !! (formattersDir + "/Shared/*.*") -- "**/Mock.fs" |> Seq.iter (copyAsFsx "temp/Shared")
  !! (formattersDir + "/Text/*.*") |> Seq.iter (copyAsFsx "temp/Text")
  !! (formattersDir + "/Html/*.*") |> Seq.iter (copyAsFsx "temp/Html")
  !! (formattersDir + "/Themes/*.*") |> Seq.iter (copyAsFsx "temp/Themes")

  // Generate #load commands to load AddHtmlPrinter and AddPrinter calls
  let loads =
    [ yield ""
      for f in Directory.GetFiles("temp/Shared") do
        yield sprintf "#load \"Shared/%s\"" (Path.GetFileName f)
      yield "#if !NO_FSI_ADDPRINTER"
      yield "#if HAS_FSI_ADDHTMLPRINTER"
      for f in Directory.GetFiles("temp/Html") do
        yield sprintf "#load \"Html/%s\"" (Path.GetFileName f)
      yield "#else"
      for f in Directory.GetFiles("temp/Text") do
        yield sprintf "#load \"Text/%s\"" (Path.GetFileName f)
      yield "#endif"
      yield "#endif\n" ]

  // Write everything to the 'temp/FsLab.fsx' file
  let lines = nowarn @ includes @ references @ loads @ extraInit
  File.WriteAllLines(__SOURCE_DIRECTORY__ + "/temp/FsLab.fsx", lines)
)

Target "BuildProjects" (fun _ ->
    !! (project + ".sln")
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

Target "UpdateNuSpec" (fun _ ->
    // Update included files in FsLab.nuspec to include formatting scripts
    let (!) n = XName.Get(n)
    let path = "src/FsLab.nuspec"
    let doc = XDocument.Load(path)
    let files = doc.Descendants(XName.Get "files").First()
    files.RemoveAll()
    files.Add(XElement(!"file", XAttribute(!"src", "../temp/FsLab.fsx"), XAttribute(!"target", ".")))
    let includes =
      [ "temp/Shared"; "temp/Text"; "temp/Html"; "temp/Themes" ]
      |> Seq.collect Directory.GetFiles
    for f in includes do
      let subdir = Path.GetDirectoryName(f).Substring(5)
      files.Add(XElement(!"file", XAttribute(!"src", "../" + f.Replace("\\", "/")), XAttribute(!"target", subdir)))
    doc.Save(path + ".updated")
    DeleteFile path
    Rename path (path + ".updated")
)

//Target "TestInDirectoryBuildOfTemplates" (fun _ ->
//    exec @"src/journal" @".paket/paket.exe" "install" 
//    exec @"src/journal" @"src/journal/packages/FAKE/tools/FAKE.exe" "html --fsiargs -d:NO_FSI_ADDPRINTER build.fsx" 
//    exec @"src/journal" @"src/journal/packages/FAKE/tools/FAKE.exe" "latex --fsiargs -d:NO_FSI_ADDPRINTER build.fsx" 
//)

Target "NuGet" (fun _ ->
    let specificVersion (name, version) = name, sprintf "[%s]" version
    NuGet (fun p ->
        { p with
            Dependencies = packages |> List.map specificVersion
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = release.Notes |> toLines
            Tags = tags
            OutputPath = buildDir
            WorkingDir = "nuget"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        ("src/" + project + ".nuspec")
    NuGet (fun p ->
        { p with
            Dependencies = packages @ journalPackages |> List.map specificVersion
            Authors = authors
            Project = projectRunner
            Summary = summaryRunner
            Description = descriptionRunner
            Version = release.NugetVersion
            ReleaseNotes = release.Notes |> toLines
            Tags = tags
            OutputPath = buildDir
            WorkingDir = "nuget"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        ("src/" + project + ".Runner.nuspec")
)


// --------------------------------------------------------------------------------------
// FAKE targets for building the FsLab template project
// --------------------------------------------------------------------------------------

Target "UpdateVSIXManifest" (fun _ ->
  /// Update version number in the VSIX manifest file of the template
  let (!) n = XName.Get(n, "http://schemas.microsoft.com/developer/vsx-schema/2011")
  let path = "src/vstemplates/source.extension.vsixmanifest"
  let vsix = XDocument.Load(path)
  let ident = vsix.Descendants(!"Identity").First()
  ident.Attribute(XName.Get "Version").Value <- release.AssemblyVersion
  vsix.Save(path + ".updated")
  DeleteFile path
  Rename path (path + ".updated")
)

Target "PlaceFiles" (fun _ ->
  // Generate ZIPs with item templates
  ensureDirectory "temp/experiments"

  for experiment in [(*"walkthrough-with-r";*) "walkthrough"; "experiment"] do
    ensureDirectory ("temp/experiments/" + experiment)
    CopyRecursive ("src/experiments/" + experiment) ("temp/experiments/" + experiment)  true |> ignore
    "misc/item.png" |> CopyFile ("temp/experiments/" + experiment + "/__TemplateIcon.png")
    "misc/preview.png" |> CopyFile ("temp/experiments/" + experiment + "/__PreviewImage.png")
    !! ("temp/experiments/" + experiment + "/**")
    |> Zip ("temp/experiments/" + experiment) ("temp/experiments/" + experiment + ".zip")

  // Generate ZIP with project template
  ensureDirectory "temp/journal"
  ensureDirectory "temp/journal/styles"

  !! "src/journal/build.*"
  ++ "src/journal/*.dependencies"
  ++ "src/journal/*.lock"
  ++ "src/journal/*.fs*"
  |> CopyFiles "temp/journal"

  CopyRecursive "src/styles" "temp/journal/styles" true |> ignore

  ensureDirectory "temp/journal/.paket"
  ".paket/paket.bootstrapper.exe" |> CopyFile "temp/journal/.paket/paket.exe"
  ".paket/paket.bootstrapper.exe" |> CopyFile "temp/journal/.paket/paket.bootstrapper.exe"
  ".paket/Paket.Restore.targets" |> CopyFile "temp/journal/.paket/Paket.Restore.targets"

)
  // Build Zip templates
Target "BuildZipTemplates" (fun _ ->
  !! "temp/journal/**" |> Zip "temp/journal" "temp/journal.zip"
)

Target "BuildVsTemplates" (fun _ ->

  // Create directory for the Template project
  CopyRecursive "temp/journal" "temp/vsjournal/" true |> ignore
  CopyRecursive "src/vstemplates/journal" "temp/vsjournal/" true |> ignore
  !! "temp/vsjournal/**" |> Zip "temp/vsjournal" "temp/vsjournal.zip"

  "misc/item.png" |> CopyFile "temp/vsjournal/__TemplateIcon.png"
  "misc/preview.png" |> CopyFile "temp/vsjournal/__PreviewImage.png"

  // Zip it up
  CopyRecursive "src/vstemplates" "temp/vstemplates/" true |> ignore

  // Copy ItemTemplates
  ensureDirectory "temp/vstemplates/ItemTemplates"
  !! "temp/experiments/*.zip"
  |> CopyFiles "temp/vstemplates/ItemTemplates"

  // Copy ProjectTemplates
  ensureDirectory "temp/vstemplates/ProjectTemplates"
  "temp/vsjournal.zip" |> CopyFile "temp/vstemplates/FsLab Journal.zip"
  "temp/vsjournal.zip" |> CopyFile "temp/vstemplates/ProjectTemplates/FsLab Journal.zip"

  // Copy other files
  "misc/logo.png" |> CopyFile "temp/vstemplates/logo.png"
  "misc/preview.png" |> CopyFile "temp/vstemplates/preview.png"

  !! "temp/vstemplates/FsLab.VsTemplates.sln"
  |> MSBuildDebug "" "Rebuild"
  |> ignore
  "temp/vstemplates/bin/Debug/FsLab.VsTemplates.vsix" |> CopyFile (buildDir + "/FsLab.VsTemplates.vsix")
)

// Build a NuGet package containing "Dotnet new" templates
Target "BuildDotnetTemplates" (fun _ ->

  // Create directory for the Template project
  CopyRecursive "src/templates" "temp/templates/" true |> ignore
  // Copy ItemTemplates
  //ensureDirectory "temp/templates/ItemTemplates"
  //!! "temp/experiments/*.zip"
  //|> CopyFiles "temp/vstemplates/ItemTemplates"
  // Copy ProjectTemplates
  //ensureDirectory "temp/vstemplates/ProjectTemplates"
  CopyRecursive "temp/journal" "temp/templates/journal/" true |> ignore
  // Copy other files
  "misc/logo.png" |> CopyFile "temp/templates/logo.png"
  "misc/preview.png" |> CopyFile "temp/templates/preview.png"

  NuGetHelper.NuGetPack (fun p -> 
        { p with
            WorkingDir = "temp/templates"
            OutputPath = "./" + buildDir + "/"
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes}) @"temp/templates/FsLab.Templates.nuspec"
)
Target "TestDotnetTemplatesNuGet" (fun _ ->

    // Globally install the templates from the template nuget package we just built
    DotNetCli.RunCommand id ("new -i " + buildDir + "/journal." + release.NugetVersion + ".nupkg")

    let testAppName = "testapp2" + string (abs (hash System.DateTime.Now.Ticks) % 100)
    // Instantiate the template. TODO: additional parameters and variations
    CleanDir testAppName
    DotNetCli.RunCommand id (sprintf "new fslab-journal -n %s -lang F#" testAppName)

    let pkgs = Path.GetFullPath(buildDir)
    // When restoring, using the bin as a package source to pick up the package we just compiled
    DotNetCli.RunCommand id (sprintf "restore %s/%s.fsproj  --source https://api.nuget.org/v3/index.json --source %s" testAppName testAppName pkgs)
    
    let slash = if isUnix then "\\" else ""
    for c in ["Debug"; "Release"] do 
        for p in ["Any CPU"] do 
            exec "." "msbuild" (sprintf "%s/%s.fsproj /p:Platform=\"%s\" /p:Configuration=%s /p:PackageSources=%s\"https://api.nuget.org/v3/index.json%s;%s%s\"" testAppName testAppName p c  slash slash pkgs slash)

    let slash = if isUnix then "\\" else ""
    exec "." "fsc" (sprintf "%s/journal.fsx -r:FSharp.Compiler.Interactive.Settings.dll --nocopyfsharpcore" testAppName)
    exec testAppName @"packages/FAKE/tools/FAKE.exe" "html --fsiargs -d:NO_FSI_ADDPRINTER build.fsx" 
    exec testAppName @"packages/FAKE/tools/FAKE.exe" "latex --fsiargs -d:NO_FSI_ADDPRINTER build.fsx" 
// Also run F5    exec testAppName @"packages/FAKE/tools/FAKE.exe" "run --fsiargs -d:NO_FSI_ADDPRINTER build.fsx" 


    (* Manual steps without building nupkg
        .\build BuildDotnetTemplates
        dotnet new -i  bin/journal.*.nupkg
        rmdir /s /q testapp2
        dotnet new fslab-journal -n testapp2 -lang F#
        dotnet restore testapp2/testapp2.fsproj -s bin/
        dotnet restore testapp2/testapp2.fsproj -s bin/
        msbuild testapp2/testapp2.fsproj
        fsc testapp2\journal.fsx -r:FSharp.Compiler.Interactive.Settings.dll
        fsi  -r:FSharp.Compiler.Interactive.Settings.dll testapp2/Tutorial.fsx
        fsi  -r:FSharp.Compiler.Interactive.Settings.dll --use:testapp2/Tutorial.fsx
        *)

)


Target "Publish" DoNothing
Target "All" DoNothing

//"Clean"
//  ==> "TestInDirectoryBuildOfTemplates"
//  ==> "NuGet"

"Clean"
  ==> "BuildProjects"
  ==> "UpdateNuSpec"
  ==> "NuGet"
  ==> "All"

"Clean"
  ==> "GenerateFsLab"
  ==> "PlaceFiles"

"PlaceFiles"
  ==> "BuildZipTemplates"
  ==> "All"

"PlaceFiles"
  ==> "UpdateVSIXManifest"
  ==> "BuildVsTemplates"
  ==> "All"

"PlaceFiles"
  ==> "BuildZipTemplates"
  ==> "All"

"PlaceFiles"
  ==> "BuildDotnetTemplates"
  ==> "TestDotnetTemplatesNuGet"
  ==> "All"

RunTargetOrDefault "All"
