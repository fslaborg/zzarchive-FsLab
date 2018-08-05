namespace global

[<AutoOpen>]
module Mocks =
    type Microsoft.FSharp.Compiler.Interactive.InteractiveSession with 
          member x.HtmlPrinterParameters = dict<string,obj> []
          member x.AddHtmlPrinter<'T>(f:'T -> seq<string * string> * string) = ()
