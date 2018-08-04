namespace FsLab.Formatters

// --------------------------------------------------------------------------------------
// Mock 'fsi' object so that the formatters in other files type-check
// --------------------------------------------------------------------------------------

[<AutoOpen>]
module MockFsiObject = 
  type FsiObject() =
    member x.HtmlPrinterParameters : System.Collections.Generic.IDictionary<string, obj> = failwith "Mock"
    member x.AddHtmlPrinter<'T>(f:'T -> seq<string * string> * string) : unit = failwith "Mock"
    member x.AddPrinter<'T>(f:'T -> string) : unit = failwith "Mock"  
  let fsi = FsiObject()
  
