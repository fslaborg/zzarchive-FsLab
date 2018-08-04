module FsLab.Formatters.HtmlTextPrinters
open FsLab.Formatters
open FSharp.Charting
open RProvider

// --------------------------------------------------------------------------------------
// Text printers using standard fsi.AddPrinter (when fsi.AddHtmlPrinter is also used)
// --------------------------------------------------------------------------------------

fsi.AddPrinter(fun (chart:XPlot.GoogleCharts.GoogleChart) ->
  "(Google Chart)")

fsi.AddPrinter(fun (chart:XPlot.Plotly.PlotlyChart) ->
  "(Plotly Chart)" )

fsi.AddPrinter(fun (printer:Deedle.Internal.IFsiFormattable) ->
  "(Deedle Object)" )

fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) ->
  "(F# Chart)")

fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) ->
  synexpr.Print())
