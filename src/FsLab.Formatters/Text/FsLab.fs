module FsLab.Formatters.TextPrinters
open FsLab.Formatters
open FSharp.Charting
open RProvider

// --------------------------------------------------------------------------------------
// Text-based printers using standard fsi.AddPrinter
// --------------------------------------------------------------------------------------

let private displayHtml html = 
  let url = Server.instance.Value.AddPage(html)
  System.Diagnostics.Process.Start(url) |> ignore

fsi.AddPrinter(fun (chart:XPlot.GoogleCharts.GoogleChart) ->
  let ch = chart |> XPlot.GoogleCharts.Chart.WithSize (800, 600)
  ch.GetHtml() |> displayHtml
  "(Google Chart)")

fsi.AddPrinter(fun (chart:XPlot.Plotly.PlotlyChart) ->
  """<!DOCTYPE html>
  <html>
  <head>
      <title>Plotly Chart</title>
      <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>
  </head>
  <body>""" + chart.GetInlineHtml() + "</body></html>" |> displayHtml
  "(Plotly Chart)" )

fsi.AddPrinter(fun (printer:Deedle.Internal.IFsiFormattable) ->
  "\n" + (printer.Format()))
fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) ->
  ch.ShowChart() |> ignore; "(Chart)")
fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) ->
  synexpr.Print())
