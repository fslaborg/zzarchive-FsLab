namespace FsLab.HtmlPrinters

#if HAS_FSI_ADDHTMLPRINTER

module MathNetHtmlPrinters =

  open FsLab.HtmlPrinters
  open MathNet.Numerics.LinearAlgebra

  // --------------------------------------------------------------------------------------
  // HTML formatting for Math.NET matrices and vectors, for hosts that use HAS_FSI_ADDHTMLPRINTER
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

  let getEnumerator (s:seq<_>) = s.GetEnumerator()

    
  /// Given a sequence, returns `startCount` number of elements at the beginning 
  /// of the sequence (wrapped in `Choice1Of3`) followed by one `Choice2Of2()` value
  /// and then followed by `endCount` number of elements at the end of the sequence
  /// wrapped in `Choice3Of3`. If the input is shorter than `startCount + endCount`,
  /// then all values are returned and wrapped in `Choice1Of3`.
  let startAndEnd startCount endCount input = seq { 
    let lastItems = Array.zeroCreate endCount
    let lastPointer = ref 0
    let written = ref 0
    let skippedAny = ref false
    let writeNext(v) = 
      if !written < endCount then incr written; 
      lastItems.[!lastPointer] <- v; lastPointer := (!lastPointer + 1) % endCount
    let readNext() = let p = !lastPointer in lastPointer := (!lastPointer + 1) % endCount; lastItems.[p]
    let readRest() = 
      lastPointer := (!lastPointer + endCount - !written) % endCount
      seq { for i in 1 .. !written -> readNext() }

    use en = getEnumerator input 
    let rec skipToEnd() = 
      if en.MoveNext() then 
        writeNext(en.Current)
        skippedAny := true
        skipToEnd()
      else seq { if skippedAny.Value then 
                   yield Choice2Of3()
                   for v in readRest() -> Choice3Of3 v 
                 else for v in readRest() -> Choice1Of3 v }
    let rec fillRest count = 
      if count = endCount then skipToEnd()
      elif en.MoveNext() then 
        writeNext(en.Current)
        fillRest (count + 1)
      else seq { for v in readRest() -> Choice1Of3 v }
    let rec yieldFirst count = seq { 
      if count = 0 then yield! fillRest 0
      elif en.MoveNext() then 
        yield Choice1Of3 en.Current
        yield! yieldFirst (count - 1) }
    yield! yieldFirst startCount }

  let mapSteps (startCount, endCount) g input =
    input
    |> startAndEnd startCount endCount
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
  #endif
