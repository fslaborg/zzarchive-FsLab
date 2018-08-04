module FsLab.Formatters.XPlot

open XPlot
open FsLab.Formatters
open Microsoft.FSharp.Quotations

// --------------------------------------------------------------------------------------
// Formatting for Google charts
// --------------------------------------------------------------------------------------

// Hacky reflection-based mutator that sets chart styles to the default theme
let rec getValue (p:Expr) =
  match p with
  | Patterns.Value(o, _) -> o
  | Patterns.PropertyGet(Some o, pi, []) ->
      let target = getValue o
      try
        let res = pi.GetValue(target, [||])
        if res = null then failwith "Got null"
        res
      with _ ->
        let ctor = pi.PropertyType.GetConstructor([||])
        let newv = ctor.Invoke([||])
        pi.SetValue(target, newv, [||])
        newv
  | _ -> failwith "Getting value failed: Wrong quotation"

let (<--) (p:Expr<'T>) (v:'T) =
  match p with
  | Patterns.PropertyGet(Some o, pi, []) ->
      pi.SetValue(getValue o, v, [||])
  | _ -> failwith "Setting property failed: Wrong quotation"

/// Modify colors in a given chart - this does not change other styles if they are set
let applyTheme (ch:XPlot.GoogleCharts.GoogleChart) =
  let gridlines = Styles.getStyle "background-color-highlighted"
  let background = Styles.getStyle "background-color-alternate"
  let textcolor = Styles.getStyle "text-color"
  let palette = Styles.getStyle "chart-color-palette"
  let opts =
    ch.GetType().GetField(
        "options", System.Reflection.BindingFlags.NonPublic |||
        System.Reflection.BindingFlags.Instance ).GetValue(ch) :?> XPlot.GoogleCharts.Configuration.Options
  <@ opts.backgroundColor.fill @> <-- "transparent"
  <@ opts.hAxis.baselineColor @> <-- gridlines
  <@ opts.vAxis.baselineColor @> <-- gridlines
  <@ opts.hAxis.gridlines.color @> <-- gridlines
  <@ opts.vAxis.gridlines.color @> <-- gridlines
  <@ opts.hAxis.textStyle.color @> <-- textcolor
  <@ opts.vAxis.textStyle.color @> <-- textcolor
  <@ opts.chartArea.backgroundColor.fill @> <-- background
  <@ opts.legend.textStyle.color @> <-- textcolor
  <@ opts.titleTextStyle.color @> <-- textcolor
  <@ opts.datalessRegionColor @> <-- gridlines
  <@ opts.colors @> <-- palette.Split(',')
  ch

let googleJsapi = """<script type="text/javascript" src="https://www.google.com/jsapi"></script>"""
let googleLoad = """<script type="text/javascript">
    google.load("visualization", "1.1",
      { packages: ["corechart", "annotationchart", "calendar",
          "gauge", "geochart", "map", "sankey", "table", "timeline", "treemap"] })
    $(function() { if (window.fsiResizeContent) window.fsiResizeContent($("body").height() + 10); });
  </script>"""

fsi.AddHtmlPrinter(fun (chart:XPlot.GoogleCharts.GoogleChart) ->
  let ch = chart |> XPlot.GoogleCharts.Chart.WithSize (800, 450) |> applyTheme
  seq [ "script", googleJsapi; "script", googleLoad ], 
  ch.GetInlineHtml())


// --------------------------------------------------------------------------------------
// Formatting for Plotly charts
// --------------------------------------------------------------------------------------

open XPlot.Plotly

/// Create default Plotly layout with theme colours
let defaultLayout () =
  let gridlines = Styles.getStyle "background-color-highlighted"
  let background = Styles.getStyle "background-color-alternate"
  let textcolor = Styles.getStyle "text-color"

  let x = Xaxis(linecolor=gridlines, gridcolor=gridlines, tickfont=Font(color=textcolor))
  let y = Yaxis(linecolor=gridlines, gridcolor=gridlines, tickfont=Font(color=textcolor))
  Layout
    ( plot_bgcolor=background, paper_bgcolor="transparent",
      xaxis=x, yaxis=y, legend=Legend(font=Font(color=textcolor)) )

/// Resize the iframe when plotly chart is loaded
let plotlyLoadScript = """<script type="text/javascript">
  $(function() { 
    if (window.fsiResizeContent) 
      window.fsiResizeContent($("body").height() + 10); 
  });
</script>"""

/// Reference Plotly.js via a CDN
let plotlyCdn = 
  "<script src='https://cdn.plot.ly/plotly-latest.min.js'></script>"

fsi.AddHtmlPrinter(fun (ch:PlotlyChart) ->
  ch.WithLayout(defaultLayout())
  seq [ "script", plotlyCdn; "script", plotlyLoadScript ],
  ch.GetInlineHtml() )
