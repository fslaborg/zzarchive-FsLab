namespace FsLab.HtmlPrinters

/// Text printers using standard fsi.AddPrinter (when fsi.AddHtmlPrinter is also used)
module HtmlTextPrinters =

  fsi.AddPrinter(fun (chart:XPlot.GoogleCharts.GoogleChart) ->
    "(Google Chart)")

  fsi.AddPrinter(fun (chart:XPlot.Plotly.PlotlyChart) ->
    "(Plotly Chart)" )

  fsi.AddPrinter(fun (printer:Deedle.Internal.IFsiFormattable) ->
    "(Deedle Object)" )

#if PKG_FSHARP_CHARTING
  fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) ->
    "(F# Chart)")
#endif

#if PKG_RPROVIDER
  fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) ->
    synexpr.Print())
#endif
