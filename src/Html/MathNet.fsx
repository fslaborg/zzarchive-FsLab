module formatters.MathNet

open formatters
open MathNet.Numerics.LinearAlgebra

// --------------------------------------------------------------------------------------
// Formatting for Math.NET matrices and vectors
// --------------------------------------------------------------------------------------

// Formatting of primitive numerical values as latex & helpers
let (|Float|_|) (v:obj) = if v :? float then Some(v :?> float) else None
let (|Float32|_|) (v:obj) = if v :? float32 then Some(v :?> float32) else None

let inline (|PositiveInfinity|_|) (v: ^T) =
  if (^T : (static member IsPositiveInfinity: 'T -> bool) (v)) then Some PositiveInfinity else None
let inline (|NegativeInfinity|_|) (v: ^T) =
  if (^T : (static member IsNegativeInfinity: 'T -> bool) (v)) then Some NegativeInfinity else None
let inline (|NaN|_|) (v: ^T) =
  if (^T : (static member IsNaN: 'T -> bool) (v)) then Some NaN else None

let mapSteps (startCount, endCount) g input =
  input
  |> Deedle.Internal.Seq.startAndEnd startCount endCount
  |> Seq.map (function Choice1Of3 v | Choice3Of3 v -> g (Some v) | _ -> g None)
  |> List.ofSeq

let inline formatMathValue (floatFormat:string) = function
  | PositiveInfinity -> "\\infty"
  | NegativeInfinity -> "-\\infty"
  | NaN -> "\\times"
  | Float v -> v.ToString(floatFormat)
  | Float32 v -> v.ToString(floatFormat)
  | v -> v.ToString()

/// Format Matrix using row/column counts specified in Style configuration
let formatMatrix (formatValue: 'T -> string) (matrix: Matrix<'T>) =
  let mrows = Styles.getNumberRange "matrix-row-counts"
  let (scols, ecols) as mcols = Styles.getNumberRange "matrix-column-counts"

  let mappedColumnCount = min (scols + ecols + 1) matrix.ColumnCount
  String.concat System.Environment.NewLine
    [ "\\begin{bmatrix}"
      matrix.EnumerateRows()
      |> mapSteps mrows (function
        | Some row -> row |> mapSteps mcols (function Some v -> formatValue v | _ -> "\\cdots") |> String.concat " & "
        | None -> Array.zeroCreate matrix.ColumnCount |> mapSteps mcols (function Some v -> "\\vdots" | _ -> "\\ddots") |> String.concat " & ")
      |> String.concat ("\\\\ " + System.Environment.NewLine)
      "\\end{bmatrix}" ]

/// Format Vector using row/column counts specified in Style configuration
let formatVector (formatValue: 'T -> string) (vector: Vector<'T>) =
  let vitms = Styles.getNumberRange "vector-item-counts"
  String.concat System.Environment.NewLine
    [ "\\begin{bmatrix}"
      vector.Enumerate()
        |> mapSteps vitms (function | Some v -> formatValue v | _ -> "\\cdots")
        |> String.concat " & "
      "\\end{bmatrix}" ]

/// Configuration for MathJax specifying how Math is delimited
let mathJaxConfig = """
  <script type="text/x-mathjax-config">
    MathJax.Hub.Config({ tex2jax: {inlineMath: [["$","$"],["\\(","\\)"]]} });
  </script>"""

/// MathJax script - points to the latest version on CDN
let mathJaxScript = """
  <script src='https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.1/MathJax.js?config=TeX-AMS-MML_HTMLorMML'></script>"""

/// MathJax load script - resizes Atom window after Math is formatted
let mathJaxLoadScript = """
  <script type="text/javascript">
    MathJax.Hub.Queue(function() {
      $(".mathnet").show();
      if (window.fsiResizeContent) window.fsiResizeContent($(".mathnet").outerHeight() + 20);
    });
  </script>
"""

/// Tweaking the styles (smaller margin & colors based on styles)
let mathStyles = """
  <style type="text/css">
    .mathnet .MathJax_Display {
      margin:0px;
    }
    .mathnet {
      color:@text-color;
      display:none;
    }
  </style>"""

// Register printers for Matrix and Vector for both float and float32 types
let floatFormat = Styles.getStyle "math-float-format"
let headElements = 
  seq [ "script", mathJaxConfig;
        "script", mathJaxScript;
        "script", mathJaxLoadScript;
        "style", Styles.replaceStyles mathStyles ]

fsi.AddHtmlPrinter(fun (m:Matrix<float>) ->
  headElements,
  sprintf "<div class='mathnet'>$$%s$$</div>" (formatMatrix (formatMathValue floatFormat) m) )

fsi.AddHtmlPrinter(fun (m:Matrix<float32>) ->
  headElements,
  sprintf "<div class='mathnet'>$$%s$$</div>" (formatMatrix (formatMathValue floatFormat) m) )

fsi.AddHtmlPrinter(fun (v:Vector<float>) ->
  headElements,
  sprintf "<div class='mathnet'>$$%s$$</div>" (formatVector (formatMathValue floatFormat) v) )

fsi.AddHtmlPrinter(fun (v:Vector<float32>) ->
  headElements,
  sprintf "<div class='mathnet'>$$%s$$</div>" (formatVector (formatMathValue floatFormat) v) )
