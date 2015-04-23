#nowarn "211"
#I "."
#I "../Deedle/lib/net40"
#I "../Deedle.1.0.6/lib/net40"
#I "../Deedle.RPlugin/lib/net40"
#I "../Deedle.RPlugin.1.0.6/lib/net40"
#I "../FSharp.Charting/lib/net40"
#I "../FSharp.Charting.0.90.9/lib/net40"
#I "../FSharp.Data/lib/net40"
#I "../FSharp.Data.2.0.14/lib/net40"
#I "../Foogle.Charts/lib/net40"
#I "../Foogle.Charts.0.0.4/lib/net40"
#I "../MathNet.Numerics/lib/net40"
#I "../MathNet.Numerics.3.0.0/lib/net40"
#I "../MathNet.Numerics.FSharp/lib/net40"
#I "../MathNet.Numerics.FSharp.3.0.0/lib/net40"
#I "../RProvider/lib/net40"
#I "../RProvider.1.0.17/lib/net40"
#I "../R.NET.Community/lib/net40"
#I "../R.NET.Community.1.5.15/lib/net40"
#I "../R.NET.Community.FSharp/lib/net40"
#I "../R.NET.Community.FSharp.0.1.8/lib/net40"
#r "Deedle.dll"
#r "Deedle.RProvider.Plugin.dll"
#r "System.Windows.Forms.DataVisualization.dll"
#r "FSharp.Charting.dll"
#r "FSharp.Data.dll"
#r "Foogle.Charts.dll"
#r "MathNet.Numerics.dll"
#r "MathNet.Numerics.FSharp.dll"
#r "RProvider.Runtime.dll"
#r "RProvider.dll"
#r "RDotNet.dll"
#r "RDotNet.NativeLibrary.dll"
#r "RDotNet.FSharp.dll"

// ***FsLab.fsx*** (DO NOT REMOVE THIS COMMENT, everything below is copied to the output)
namespace FsLab

#if NO_FSI_ADDPRINTER
#else
module FsiAutoShow =
  open FSharp.Charting
  open RProvider

  fsi.AddPrinter(fun (printer:Deedle.Internal.IFsiFormattable) ->
    "\n" + (printer.Format()))
  fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) ->
    ch.ShowChart() |> ignore; "(Chart)")
  fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) ->
    synexpr.Print())

  open System.IO
  open Foogle
  open Foogle.SimpleHttp

  let server = ref None
  let tempDir = Path.GetTempFileName()
  let pid = System.Diagnostics.Process.GetCurrentProcess().Id
  let counter = ref 1

  do File.Delete(tempDir)
  do Directory.CreateDirectory(tempDir) |> ignore

  fsi.AddPrinter(fun (chart:FoogleChart) ->
    match server.Value with
    | None -> server := Some (HttpServer.Start("http://localhost:8084/", tempDir))
    | _ -> ()
    let file = sprintf "chart_%d_%d.html" pid counter.Value
    let html =
      chart
      |> Foogle.Formatting.Google.CreateGoogleChart
      |> Foogle.Formatting.Google.GoogleChartHtml
    File.WriteAllText(Path.Combine(tempDir, file), html)
    System.Diagnostics.Process.Start("http://localhost:8084/" + file) |> ignore
    incr counter
    "(Foogle Chart)" )
#endif

namespace FSharp.Charting
open FSharp.Charting
open Deedle

[<AutoOpen>]
module FsLabExtensions =
  type FSharp.Charting.Chart with
    static member Line(data:Series<'K, 'V>, ?Name, ?Title, ?Labels, ?Color, ?XTitle, ?YTitle) =
      Chart.Line(Series.observations data, ?Name=Name, ?Title=Title, ?Labels=Labels, ?Color=Color, ?XTitle=XTitle, ?YTitle=YTitle)
    static member Column(data:Series<'K, 'V>, ?Name, ?Title, ?Labels, ?Color, ?XTitle, ?YTitle) =
      Chart.Column(Series.observations data, ?Name=Name, ?Title=Title, ?Labels=Labels, ?Color=Color, ?XTitle=XTitle, ?YTitle=YTitle)
    static member Pie(data:Series<'K, 'V>, ?Name, ?Title, ?Labels, ?Color, ?XTitle, ?YTitle) =
      Chart.Pie(Series.observations data, ?Name=Name, ?Title=Title, ?Labels=Labels, ?Color=Color, ?XTitle=XTitle, ?YTitle=YTitle)
    static member Area(data:Series<'K, 'V>, ?Name, ?Title, ?Labels, ?Color, ?XTitle, ?YTitle) =
      Chart.Area(Series.observations data, ?Name=Name, ?Title=Title, ?Labels=Labels, ?Color=Color, ?XTitle=XTitle, ?YTitle=YTitle)
    static member Bar(data:Series<'K, 'V>, ?Name, ?Title, ?Labels, ?Color, ?XTitle, ?YTitle) =
      Chart.Bar(Series.observations data, ?Name=Name, ?Title=Title, ?Labels=Labels, ?Color=Color, ?XTitle=XTitle, ?YTitle=YTitle)

namespace Foogle
open Deedle

[<AutoOpen>]
module FoogleExtensions =

  type Foogle.Chart with
    static member PieChart(frame:Frame<_, _>, column, ?Label, ?PieHole) =
      Foogle.Chart.PieChart
        ( frame.GetColumn<float>(column) |> Series.observations,
          ?Label=Label, ?PieHole=PieHole)
    static member GeoChart(frame:Frame<_, _>, column, ?Label, ?Region, ?DisplayMode) =
      Foogle.Chart.GeoChart
        ( frame.GetColumn<float>(column) |> Series.observations,
          ?Label=Label, ?Region=Region, ?DisplayMode=DisplayMode)

namespace MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra
open Deedle

module Matrix =
  let inline toFrame matrix = matrix |> Matrix.toArray2 |> Frame.ofArray2D
module DenseMatrix =
  let inline ofFrame frame = frame |> Frame.toArray2D |> DenseMatrix.ofArray2
module SparseMatrix =
  let inline ofFrame frame = frame |> Frame.toArray2D |> SparseMatrix.ofArray2
module Vector =
  let inline toSeries vector = vector |> Vector.toSeq |> Series.ofValues
module DenseVector =
  let inline ofSeries series = series |> Series.values |> Seq.map (float) |> DenseVector.ofSeq
module SparseVector =
  let inline ofSeries series = series |> Series.values |> Seq.map (float) |> SparseVector.ofSeq


namespace Deedle
open Deedle
open MathNet.Numerics.LinearAlgebra

module Frame =
  let inline ofMatrix matrix = matrix |> Matrix.toArray2 |> Frame.ofArray2D
  let inline toMatrix frame = frame |> Frame.toArray2D |> DenseMatrix.ofArray2

  let ofCsvRows (data:FSharp.Data.Runtime.CsvFile<'T>) =
    match data.Headers with
    | None -> Frame.ofRecords data.Rows
    | Some names -> Frame.ofRecords data.Rows |> Frame.indexColsWith names

module Series =
  let inline ofVector vector = vector |> Vector.toSeq |> Series.ofValues
  let inline toVector series = series |> Series.values |> Seq.map (float) |> DenseVector.ofSeq
