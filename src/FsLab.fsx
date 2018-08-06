#nowarn "211"
#I __SOURCE_DIRECTORY__
#I "../packages/Deedle/lib/net40"
#I "../Deedle/lib/net40"
#I "../packages/Deedle.1.2.5/lib/net40"
#I "../Deedle.1.2.5/lib/net40"
#I "../packages/FSharp.Data/lib/net45"
#I "../FSharp.Data/lib/net45"
#I "../packages/FSharp.Data.2.4.6/lib/net45"
#I "../FSharp.Data.2.4.6/lib/net45"
#I "../packages/MathNet.Numerics/lib/net40"
#I "../MathNet.Numerics/lib/net40"
#I "../packages/MathNet.Numerics.3.20.2/lib/net40"
#I "../MathNet.Numerics.3.20.2/lib/net40"
#I "../packages/MathNet.Numerics.FSharp/lib/net40"
#I "../MathNet.Numerics.FSharp/lib/net40"
#I "../packages/MathNet.Numerics.FSharp.3.20.2/lib/net40"
#I "../MathNet.Numerics.FSharp.3.20.2/lib/net40"
#I "../packages/Suave/lib/net40"
#I "../Suave/lib/net40"
#I "../packages/Suave.2.1.1/lib/net40"
#I "../Suave.2.1.1/lib/net40"
#I "../packages/XPlot.Plotly/lib/net45"
#I "../XPlot.Plotly/lib/net45"
#I "../packages/XPlot.Plotly.1.5.0/lib/net45"
#I "../XPlot.Plotly.1.5.0/lib/net45"
#I "../packages/XPlot.GoogleCharts/lib/net45"
#I "../XPlot.GoogleCharts/lib/net45"
#I "../packages/XPlot.GoogleCharts.1.5.0/lib/net45"
#I "../XPlot.GoogleCharts.1.5.0/lib/net45"
#I "../packages/XPlot.GoogleCharts.Deedle/lib/net45"
#I "../XPlot.GoogleCharts.Deedle/lib/net45"
#I "../packages/XPlot.GoogleCharts.Deedle.1.5.0/lib/net45"
#I "../XPlot.GoogleCharts.Deedle.1.5.0/lib/net45"
#I "../packages/Google.DataTable.Net.Wrapper/lib"
#I "../Google.DataTable.Net.Wrapper/lib"
#I "../packages/Google.DataTable.Net.Wrapper.3.1.2.0/lib"
#I "../Google.DataTable.Net.Wrapper.3.1.2.0/lib"
#I "../packages/Newtonsoft.Json/lib/net40"
#I "../Newtonsoft.Json/lib/net40"
#I "../packages/Newtonsoft.Json.11.0.2/lib/net40"
#I "../Newtonsoft.Json.11.0.2/lib/net40"
#r "Deedle.dll"
#r "FSharp.Data.dll"
#r "MathNet.Numerics.dll"
#r "MathNet.Numerics.FSharp.dll"
#r "Suave.dll"
#r "XPlot.Plotly.dll"
#r "XPlot.GoogleCharts.dll"
#r "XPlot.GoogleCharts.Deedle.dll"
#r "Google.DataTable.Net.Wrapper.dll"
#r "Newtonsoft.Json.dll"

#load "Shared/Server.fsx"
#load "Shared/Styles.fsx"
#if !NO_FSI_ADDPRINTER
#if HAS_FSI_ADDHTMLPRINTER
#load "Html/Charting.fsx"
#load "Html/Deedle.fsx"
#load "Html/MathNet.fsx"
#load "Html/Text.fsx"
#load "Html/XPlot.fsx"
#else
#load "Text/TextDisplay.fsx"
#endif
#endif

// ***FsLab.fsx*** (DO NOT REMOVE THIS COMMENT, everything below is copied to the output)

#if PKG_FSHARP_CHARTING
// TODO: Move these to the FSharp.Charting package somehow. If necessary have that pacakge dynamically probe
// for Series-like functionality. Or make Series implement IEnumerable, yielding observations.
namespace FSharp.Charting
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
#endif

// TODO: There is an XPlot.GoogleCharts.Deedle package.  Move its contents XPlot.GoogleCharts and/or Deedle somehow. 
// If necessary have XPlot.GoogleCharts pacakge dynamically probe for Series-like functionality. 
// Or make Series implement IEnumerable, yielding observations.

// TODO: Move these to the XPlot.Plotly package somehow. If necessary have that pacakge dynamically probe
// for Series-like functionality. Or make Series implement IEnumerable, yielding observations.
namespace XPlot.Plotly
  open Deedle

  [<AutoOpen>]
  module FsLabExtensions =
    type XPlot.Plotly.Chart with
      static member Line(data:Series<'K, 'V>) =
        Chart.Line(Series.observations data)
      static member Column(data:Series<'K, 'V>) =
        Chart.Column(Series.observations data)
      static member Pie(data:Series<'K, 'V>) =
        Chart.Pie(Series.observations data)
      static member Area(data:Series<'K, 'V>) =
        Chart.Area(Series.observations data)
      static member Bar(data:Series<'K, 'V>) =
        Chart.Bar(Series.observations data)

// TODO: Consider how to move these to Deedle or MathNet.Numerics, i.e. magically make these packages work
// nicely together through an IFrame or ISeries etc.
namespace MathNet.Numerics.LinearAlgebra
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

// TODO: Consider how to move these to Deedle or MathNet.Numerics or FSharp.Data, i.e. magically make these packages work
// nicely together through an IFrame or ISeries etc.
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
