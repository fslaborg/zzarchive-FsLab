// Before running any code, invoke Paket to get the dependencies. 
//
// To do this, build the project or run '.paket/paket.bootstrap.exe' and
// then '.paket/paket.exe install' (on Mac run 'exe' files using 'mono').
//
// Once you have packages, use Alt+Enter or Ctrl+Enter to run the following
// snippets. In Ionide, outputs will be embedded in the F# Interactive window
// (and you can choose "theme" to match with your editor color settings).
#load "packages/FsLab/Themes/DefaultWhite.fsx"
#load "packages/FsLab/FsLab.fsx"

open Deedle
open FSharp.Data
open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle

// Connect to the WorldBank and access indicators EU and CZ
// Try changing the code to look at stats for your country!
let wb = WorldBankData.GetDataContext()
let cz = wb.Countries.``Czech Republic``.Indicators
let eu = wb.Countries.``European Union``.Indicators

// Use Deedle to get time-series with school enrollment data
let czschool = series cz.``Gross enrolment ratio, tertiary, both sexes (%)``
let euschool = series eu.``Gross enrolment ratio, tertiary, both sexes (%)``

// Get 5 years with the largest difference between EU and CZ
abs (czschool - euschool)
|> Series.sort
|> Series.rev
|> Series.take 5

// Plot a line chart comparing the two data sets 
// (Opens a web browser window with the chart)
[ czschool.[1975 .. 2010]; euschool.[1975 .. 2010] ]
|> Chart.Line
|> Chart.WithOptions (Options(legend=Legend(position="bottom")))
|> Chart.WithLabels ["CZ"; "EU"]
