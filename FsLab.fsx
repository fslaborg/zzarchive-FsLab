#r "System.Windows.Forms.DataVisualization.dll"
#nowarn "211"
#I "../bin"
#I "../../packages/FSharp.Charting.0.87/lib/net40"
#I "../../packages/FSharp.Data.1.1.10/lib/net40"
#I "../../packages/FSharp.DataFrame.0.9.3-beta/lib/net40"
#I "../../packages/RProvider.1.0.3/lib"
#I "../../../packages/FSharp.Charting.0.87/lib/net40"
#I "../../../packages/FSharp.Data.1.1.10/lib/net40"
#I "../../../packages/FSharp.DataFrame.0.9.3-beta/lib/net40"
#I "../../../packages/RProvider.1.0.3/lib"
#r "FSharp.Charting.dll"
#r "FSharp.Data.dll"
#r "FSharp.DataFrame.dll"
#r "RProvider.dll"
#r "RDotNet.dll"

open FSharp.Charting
module FsiAutoShow = 
  fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) -> 
    ch.ShowChart(); "(Chart)")