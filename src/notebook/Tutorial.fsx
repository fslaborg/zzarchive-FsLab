(*** hide ***)
(* BUILD (Ctrl+B) the project to restore NuGet packages first! *)
#load "packages/FsLab.0.0.13-beta/FsLab.fsx"
(**

FsLab Notebook demo
===================

FsLab notebook is a simple Visual Studio template that makes it easy to do 
interactive data analysis using F# Interactive and produce nice HTML to 
document you research.

The FsLab notebook template automatically includes a reference to the [FsLab 
NuGet package][fslab], so you can use all the grata data science F# packages.
The template also contains a runner that formats your script files nicely using
[F# Formatting][fsfmt] and generates nice reports. To write your reports, you 
can include Markdown-formatted text in comments starting with `**` such as 
this one. The report is generated and opened automatically when you hit **F5**. 

When you generate a report, your code is nicely formatted, executed and 
the resulting charts and frames are embedded in the report. The rest of this 
notebook shows how this is done.

FsLab libraries
---------------

The FsLab package automatically references the following F# libraries:

 * [Deedle][deedle] for working with data frames and data series
 * [F# R type provider][rprovider] for interoperating with R
 * [F# Charting][fschart] for building interactive charts
 * [F# Data][fsdata] with data-access with F# type providers
 * [Math.NET Numerics][mathnet] for writing numerical calculations

Data access with F# Data
------------------------

The following snippet builds a simple Deedle data frame using data obtained 
from the WorldBank type provider:

*)
open Deedle
open FSharp.Data

// Get countries in the Euro area
let wb = WorldBankData.GetDataContext()
let countries = wb.Regions.``Euro area``

// Get a frame with debts as a percentage of GDP 
let debts = 
  [ for c in countries.Countries ->
      let debts = c.Indicators.``Central government debt, total (% of GDP)``
      c.Name => series debts ] |> frame
(**
The above snippet defines a `debt` value, which is a data frame with years as 
the row index and country names as the column index. You can use the 
`include-value` command to include a table summarizing the frame data:
*)

(*** include-value:round(debts*100.0)/100.0 ***)

(**
As you can see, you can even include simple F# expressions in the command. Here,
we use `round(debts*100.0)/100.0)` to round the debt values to two decimal points
for a nicer presentation.

Data analysis with Deedle
-------------------------

You can also use `define-output` to give a name to a code block. When the code 
block is an expression that returns a value, you can use `include-it` to 
include the formatted result:

*)
(*** define-output:top8 ***)
let recent = debts.Rows.[2005 ..]

recent
|> Stats.mean
|> Series.sort
|> Series.rev
|> Series.take 8
|> round
(*** include-it:top8 ***)

(**
Here, we calculate means of debts over years starting with 2005, take the 8
countries with the greatest average debt and round the debts.

Embedding F# Charting charts
----------------------------

The generated report chan also automatically embed charts created using the 
F# Charting library. Here, we plot the debts of the 3 countries with the largest
debt based on the previous table:

*)
(*** define-output:chart ***)
open FSharp.Charting

// Combine three line charts and add a legend
Chart.Combine(
  [ Chart.Line(recent?Cyprus, Name="Cyprus")
    Chart.Line(recent?Malta, Name="Malta")
    Chart.Line(recent?Greece, Name="Greece") ])
  .WithLegend()
(*** include-it:chart ***)

(**
Interoperating with R 
---------------------

If you want to do some advanced calculation and you have R installed on your
machine, you can use the R type provider too. Embedding graphical output is not
directly supported yet, but you can already do it by defining the following 
simple function that turns the last R output into a `Bitmap` object.

> **NOTE**: To make this tutorial work on machines that do not have R installed,
> the following code is embedded as a snippet in a comment. Uncomment the code
> to run it if you have R installed!

    open RProvider
    open RProvider.``base``
    open RProvider.grDevices

    module R = 
      let last_dev() =
        let png = R.eval(R.parse(text="png"))
        let file = System.IO.Path.GetTempFileName() + ".png"
        let args = namedParams [ "device", box png; "filename", box file ]
        R.dev_off(R.dev_copy(args)) |> ignore
        R.graphics_off() |> ignore
        System.Drawing.Bitmap.FromFile(file)

Then you can call R functions to perform advanced statistics (to install a 
package, just install it in your standard R environment). Here, we use `R.mean` 
and `R.plot`. You can then embed the result of the plot using `include-value` 
and the `R.last_dev()` helper:

    open RProvider.graphics

    R.mean(debts?Germany).Value
    R.plot(debts?Germany)
*)

(*** include-value:R.last_dev() ***)

(**
FsLab notebook runner
---------------------

When you hit **F5**, the FsLab notebook runner automatically processes all 
`*.fsx` files in the root directory of your project. In this template, there is
just a single sample, which is `Tutorial.fsx`. The generated files are placed
in the `output` folder (together with all the styles and JavaScript files that
it requires). Then, the runner opens your default web browser with the generated
file.

If you have multiple files, the runner automatically generates index file with
links to all your notebooks and opens this instead. You can also create your 
own index file by adding a file named `Index.fsx` or `Index.md` (if you only 
want to write Markdown text in your index).

 [fslab]: http://www.nuget.org/packages/FsLab
 [fsfmt]: http://tpetricek.github.io/FSharp.Formatting/
 [rprovider]: http://bluemountaincapital.github.io/FSharpRProvider/
 [deedle]: http://bluemountaincapital.github.io/Deedle/
 [fschart]: http://fsharp.github.io/FSharp.Charting/
 [fsdata]: http://fsharp.github.io/FSharp.Data/
 [mathnet]: http://numerics.mathdotnet.com/

*)