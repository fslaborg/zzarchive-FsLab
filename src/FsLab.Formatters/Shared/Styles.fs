module FsLab.Formatters.Styles
open Suave

// --------------------------------------------------------------------------------------
// Helpers for accessing styles & colors
// --------------------------------------------------------------------------------------

let private defaultStyles = 
  [ // Background colors
    "background-color", "transparent"
    "background-color-alternate", "#f4f4f4"
    "background-color-highlighted", "#fdfdfd"

    // Text & foreground colors
    "border-color", "#a0a0a0" 
    "scrollbar-color", "#c1c1c1" 
    "text-color", "black"
    "text-color-subtle", "#a0a0a0" 

    // Other visual styles
    "chart-color-palette", "#1f77b4,#aec7e8,#ff7f0e,#ffbb78,#2ca02c,"+
      "#98df8a,#d62728,#ff9896,#9467bd,#c5b0d5,#8c564b,#c49c94,#e377c2,"+
      "#f7b6d2,#7f7f7f,#c7c7c7,#bcbd22,#dbdb8d,#17becf,#9edae5"
    "font-family", "sans-serif" 

    // Grids, matrices and related
    "table-float-format", "F2"
    "math-float-format", "G2"
    "vector-item-counts", "6,3"
    "matrix-column-counts", "6,3"
    "matrix-row-counts", "10,4"
    "grid-row-counts", "8,4"
    "grid-column-counts", "3,3" ] |> dict


#if HAS_FSI_ADDHTMLPRINTER
/// Return value of a known style (or fail)
let getStyle key = 
  match fsi.HtmlPrinterParameters.TryGetValue(key), defaultStyles.TryGetValue(key) with
  | (true, (:? string as s)), _ 
  | _, (true, s) -> s
  | _ -> failwithf "Style '%s' not available" key

/// Replace '@name' with value for all known styles
let replaceStyles style = 
  defaultStyles.Keys 
  |> Seq.sortBy (fun s -> -s.Length)
  |> Seq.fold (fun (style:string) k -> style.Replace("@" + k, getStyle k)) style

/// Should the generated HTML be standalone?
let standaloneHtmlOutput () = 
  fsi.HtmlPrinterParameters.["html-standalone-output"] :?> bool

/// Parses numerical range specified by a given key
let getNumberRange key = 
  match (getStyle key).Split(',') with
  | [| lo; hi |] -> int lo, int hi
  | _ -> failwithf "Wrong numerical range for '%s'" key
#endif
