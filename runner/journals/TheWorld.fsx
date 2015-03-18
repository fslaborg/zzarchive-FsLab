(*** hide ***)
#load "../packages/FsLab/FsLab.fsx"
open FsLab
open Deedle
open Foogle
open FSharp.Data

(**
Understanding the World with F#
===============================

This journal demonstrates how to generate elegant reports from your FsLab
data analysis. In this demo, we use WorldBank type provider to obtain
various information about countries, then we analyze the data using Deedle
and we create a chart using Foogle Charts.

Population growth
-----------------
The following snippet reads population information in year 2000 and 2010 
for all countries of the world into a data frame:

*)
let wb = WorldBankData.GetDataContext()

let pop2000 = series [ for c in wb.Countries -> c.Name, c.Indicators.``Population, total``.[2000] ]
let pop2010 = series [ for c in wb.Countries -> c.Name, c.Indicators.``Population, total``.[2010] ]
let all = 
  frame [ "Pop2000" => round pop2000
          "Pop2010" => round pop2010 ]
(*** include-value:all ***)

(**
Now we can display the population in 2010 using a geo chart:
*)
(*** define-output: geo1 ***)
Chart.GeoChart(all, "Pop2010")
(*** include-it: geo1 ***)

(**
This shows the expected results. More interestingly, we can calculate and 
visualize the population growth between years 2000 and 2010:
*)
(*** define-output: geo2 ***)
all?PopChange <- (all?Pop2010 - all?Pop2000) / all?Pop2010 * 100.0
Chart.GeoChart(all, "PopChange")
(*** include-it: geo2 ***)

(**
Indicator correlation
---------------------

Another interesting thing we can do is to look at correlation between 
different indicators that we can get from the WorldBank. The following
snippet adds GDP, GDP growth, carbon emissions and gender equality 
indicators:
*)
let small = all |> Frame.dropCol "Pop2010" |> Frame.dropCol "Pop2000"
small?GDP <- [ for c in wb.Countries -> c.Indicators.``GDP (current US$)``.[2000] ]
small?Growth <- [ for c in wb.Countries -> c.Indicators.``GDP per capita growth (annual %)``.[2000] ]
small?Emissions <- [ for c in wb.Countries -> c.Indicators.``CO2 emissions (kg per PPP $ of GDP)``.[2000] ]
small?Gender <- [ for c in wb.Countries -> c.Indicators.``Employment to population ratio, 15+, female (%) (modeled ILO estimate)``.[2000] ]
(**
To display the correlation, we can use the `plot` function from R using
the R type provider
*)
(*** define-output: cor ***)
open RProvider.graphics
R.plot(small)
(*** include-output: cor ***)
