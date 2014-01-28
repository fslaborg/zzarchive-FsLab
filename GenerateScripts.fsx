#r "System.Xml.Linq"
open System.IO
open System.Xml.Linq
open System.Linq

let nuspec = XElement.Load(__SOURCE_DIRECTORY__ + "/FsLab.nuspec")
let xn s = XName.Get(s, "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd")
let packageVersions = 
    nuspec.Element(xn "metadata")
          .Element(xn "dependencies")
          .Elements()
    |> Seq.map (fun elem -> elem.Attribute(XName.Get "id").Value, 
                            elem.Attribute(XName.Get "version").Value)
    |> dict

let packages = ["Deedle"
                "Deedle.RPlugin"
                "FSharp.Charting"
                "FSharp.Data"
                "MathNet.Numerics"
                "MathNet.Numerics.FSharp"
                "RProvider"]
                
let getLibDir package =
    let baseDir = package + "." + packageVersions.[package]
    match package with
    | "RProvider" -> baseDir + "/lib"
    | _ -> baseDir + "/lib/net40"

let getAssemblies package = 
    match package with
    | "Deedle.RPlugin" -> ["Deedle.RProvider.Plugin.dll"]
    | "FSharp.Charting" -> ["System.Windows.Forms.DataVisualization.dll"; "FSharp.Charting.dll"]
    | "RProvider" -> ["RDotNet.dll"; "RDotNet.NativeLibrary.dll"; "RProvider.dll"]
    | package -> [package + ".dll"]

let folders = [ "packages/"           // new F# project in VS with create directory for solution disabled
                "../packages/"        // new F# project in VS with create directory for solution enabled
                "../../packages/"     // fsharp-project-scaffold template
                "../../../packages/"] // just in case

let nowarn = ["#nowarn \"211\""]

let extraInit = File.ReadLines(__SOURCE_DIRECTORY__ + "/ExtraInit.fsx") |> Seq.toList

let generateScript() = 

    let includes = 
        folders 
        |> List.collect (fun folder -> 
            packages 
            |> List.map (fun package -> folder + (getLibDir package)))
        |> List.map (sprintf "#I \"%s\"")

    let references = 
        packages
        |> List.collect getAssemblies
        |> List.map (sprintf "#r \"%s\"")

    File.WriteAllLines(__SOURCE_DIRECTORY__ + "/FsLab.fsx", nowarn @ includes @ references @ extraInit)

generateScript()
