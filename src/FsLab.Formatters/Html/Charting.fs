module FsLab.Formatters.FSharpCharting

open System.IO
open Suave
open Suave.Operators
open System.Drawing
open System.Windows.Forms
open FSharp.Charting
open FSharp.Charting.ChartTypes
open FsLab.Formatters

// --------------------------------------------------------------------------------------
// Formatting for F# Charting charts
// --------------------------------------------------------------------------------------

/// Add web part that serves bytes as PNG image
let servePngImage bytes = 
  let part = 
    Writers.addHeader "Content-Type" "image/png" >=>
    Successful.ok bytes
  Server.instance.Value.AddPart(part)

/// Apply chart styles specified by the theme
let applyChartStyle (ch:GenericChart) =
  let gridlines = Styles.getStyle "background-color-highlighted" |> ColorTranslator.FromHtml
  let background = Styles.getStyle "background-color-alternate" |> ColorTranslator.FromHtml
  let textcolor = Styles.getStyle "text-color" |> ColorTranslator.FromHtml
  let transparent = System.Drawing.Color.Transparent
  let grid = ChartTypes.Grid(LineColor=gridlines)
  ch
  |> Chart.WithStyle(Background=Background.Solid transparent)
  |> Chart.WithStyling(AreaBackground=Background.Solid background)  
  |> Chart.WithYAxis(MajorTickMark=TickMark(LineColor=gridlines), MajorGrid=grid,LabelStyle=LabelStyle(Color=textcolor))
  |> Chart.WithXAxis(MajorTickMark=TickMark(LineColor=gridlines), MajorGrid=grid,LabelStyle=LabelStyle(Color=textcolor))

/// Mutate the created chart to apply line color (not doable in `applyChartStyle`)
let applyChartStylePostCreation (ch:GenericChart) =
  let gridlines = Styles.getStyle "background-color-highlighted" |> ColorTranslator.FromHtml
  let chProp = typeof<GenericChart>.GetProperty("Chart", System.Reflection.BindingFlags.Instance ||| System.Reflection.BindingFlags.NonPublic)
  let wch = chProp.GetValue(ch) :?> System.Windows.Forms.DataVisualization.Charting.Chart
  for cha in wch.ChartAreas do
    for a in cha.Axes do
      a.LineColor <- gridlines

// Register HTML printer for charts
fsi.AddHtmlPrinter(fun (ch:GenericChart) ->
  use ms = new MemoryStream()
  ( let nch = applyChartStyle ch
    use ctl = new ChartControl(nch, Dock = DockStyle.Fill, Width=800, Height=450)
    applyChartStylePostCreation nch
    ch.CopyAsBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png) )
  let url = servePngImage (ms.ToArray())
  seq [], sprintf "<img src='%s' style='height:450px' />" url)

// Also register HTML printer for images
fsi.AddHtmlPrinter(fun (img:Image) ->
  use ms = new MemoryStream()
  img.Save(ms, Imaging.ImageFormat.Png)
  let url = servePngImage (ms.ToArray())
  seq [], sprintf "<img src='%s' style='width:%dpx; height:%dpx' />" url img.Width img.Height)
