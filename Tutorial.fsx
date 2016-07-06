(*** hide ***)
#load "packages/FsLab/Themes/DefaultWhite.fsx"
#load "packages/FsLab/FsLab.fsx"
(**

Welcome to FsLab journal
========================

FsLab journal is a simple Visual Studio template that makes it easy to do
interactive data analysis using F# Interactive and produce nice HTML or PDF
to document you research.

Next steps
----------

 * If you are using Visual Studio template, go to "Solution Explorer", right click
   on your newly created project, select "Add", "New item.." and choose
   "FsLab Walkthrough". This contains addtional examples you can explore.

 * To add new experiments to your project, just clone this file and rename it.
   In Visual Studio, you can got to "Add", "New item" and choose
   new "FsLab Experiment". You can have multiple scripts in a single project.

 * To see how things work, run `build run` from the terminal to start the journal
   runner in the background (or hit **F5** in Visual Studio). FsLab Journal will
   turn this Markdown document with simple F# script into a nice report!

 * To generate PDF from your experiments, you need to install `pdflatex` and
   have it accessible in the system `PATH` variable. Then you can run
   `build pdf` in the folder with this script (then check out `output` folder).

Sample experiment
-----------------

We start by referencing `Deedle` and `FSharp.Charting` libraries and then we
load the contents of *this* file:
*)

(*** define-output:loading ***)
open Deedle
open System.IO
open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle

let file = __SOURCE_DIRECTORY__ + "/Tutorial.fsx"
let contents = File.ReadAllText(file)
printfn "Loaded '%s' of length %d" file contents.Length
(*** include-output:loading ***)

(**
Now, we split the contents of the file into words, count the frequency of
words longer than 3 letters and turn the result into a Deedle series:
*)
let words =
  contents.Split(' ', '"', '\n', '\r', '*')
  |> Array.filter (fun s -> s.Length > 3)
  |> Array.map (fun s -> s.ToLower())
  |> Seq.countBy id
  |> series
(**
We can take the top 5 words occurring in this tutorial and see them in a chart:
*)
(*** define-output:grid ***)
words
|> Series.sort
|> Series.rev
|> Series.take 6
(*** include-it:grid ***)
(**
Finally, we can take the same 6 words and call `Chart.Column` to see them in a chart:
*)
(*** define-output:chart ***)
words
|> Series.sort
|> Series.rev
|> Series.take 6
|> Chart.Column
(*** include-it:chart ***)

(**
Summary
-------
An image is worth a thousand words:

![](http://imgs.xkcd.com/comics/hofstadter.png)
*)
