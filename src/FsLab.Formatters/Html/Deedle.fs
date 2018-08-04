module FsLab.Formatters.Deedle

// --------------------------------------------------------------------------------------
// Formatting for Deedle frames and series
// --------------------------------------------------------------------------------------

open Suave
open Suave.Operators
open Deedle
open Deedle.Internal
open FSharp.Data
open FsLab.Formatters

// --------------------------------------------------------------------------------------
// CSS styles for the table, scrollbars & c.
// --------------------------------------------------------------------------------------

let gridStyles = """
<style type="text/css">
  .grid {
    font-size:11pt;
    font-family:@font-family;
    color:@text-color;
  }

  .grid ::-webkit-scrollbar  {
    width:6px;
    height:6px;
    background:transparent;
  }

  .grid ::-webkit-scrollbar-track {
    background:transparent;
  }

  .grid ::-webkit-scrollbar-thumb {
    border-radius:3px;
    background-color:@scrollbar-color;
  }

  .grid .faded {
    color:@text-color-subtle;
  }

  .grid tr {
    background-color: @background-color-highlighted;
  }

  .grid tbody tr:nth-child(odd) {
    background-color: @background-color-alternate;
  }

  .grid thead tr {
    background: @background-color-highlighted;
  }

  .grid table {
    top:0px;
    left:0px;
    width:100%;
    border-spacing: 0;
    border-collapse: collapse;
  }

  .grid td, .grid th {
    border-bottom:1px solid @border-color;
    padding:4px 10px 4px 10px;
    min-width:50px;
  }

  .grid thead th {
    border-bottom:3px solid @border-color;
  }

  .grid th {
    padding:4px 20px 4px 10px;
    text-align:left;
    font-weight:bold;
  }

  .live-grid {
    position:relative;
    overflow:hidden;
  }

  .live-grid .scroller {
    overflow-y: scroll;
    position:absolute;
    width:100%;
  }

  .live-grid table {
    position:absolute;
  }
</style>"""

// --------------------------------------------------------------------------------------
// JavaScript (only when in online mode) that implements lazy loading
// --------------------------------------------------------------------------------------

let gridLiveScript = """
<script type="text/javascript">
  function setupGrid(id, viewHeight, serviceUrl) {

    // Create table with given column names & specified number of empty rows
    function createRows(rowCount, columns) {
      var head = $(id + " .head").empty();
      $("<th />").html("#").appendTo(head);
      for (var i = 0; i < columns.length; i++) {
        $("<th />").html(columns[i]).appendTo(head);
      }

      var rows = [];
      var body = $(id + " .body").empty();
      for (var i = 0; i < rowCount; i++) {
        var row = { columns: [] };
        var tr = $("<tr />").appendTo(body);
        var th = $("<th />").html("&nbsp;").appendTo(tr);
        for (var j = 0; j < columns.length; j++) {
          row.columns.push($("<td />").html("&nbsp;").appendTo(tr));
        }
        row.key = th;
        row.tr = tr;
        rows.push(row);
      }
      return rows;
    }

    // Once we receive meta-data about the grid from the servier, 
    // we create the grid, set height of scrollbar and register 
    // scroll event to update the data on change
    function initialize(meta) {
      var rowHeight = $(id + " tbody tr").height() - 1; // Magic constant
      var thHeight = $(id + " thead tr").height() + 2; // Magic constant 
      var totalRows = meta.rows;
      var viewCount = Math.ceil((viewHeight - thHeight) / rowHeight - 1);
      var tableHeight = rowHeight * Math.min(viewCount, totalRows);

      // Resize and report new size to FSI container (if defined)
      $(id + " .spacer").css("min-height", (rowHeight * totalRows) + "px");
      $(id).height(tableHeight + thHeight);
      $(id + " .scroller").css("margin-top", thHeight + "px");
      $(id + " .scroller").height(tableHeight);
      if (window.fsiResizeContent) window.fsiResizeContent();

      // Create table rows of the view
      var rows = createRows(viewCount, meta.columns);
      
      // Update that gets triggered once the current one is done
      var nextUpdate = null;

      // Update the displayed data on scroll
      function update(offset) {
        nextUpdate = offset;
        for (var i = 0; i < viewCount; i++) {
          rows[i].tr.show();
          rows[i].key.addClass("faded");
          for (var j = 0; j < rows[i].columns.length; j++)
            rows[i].columns[j].addClass("faded");
        }

        $.ajax({ url: serviceUrl + "/rows/" + offset + "?count=" + viewCount }).done(function (res) {
          var data = JSON.parse(res);
          for (var i = 0; i < viewCount; i++) {
            var point = data[i];
            if (point == null) rows[i].tr.hide();
            else {
              rows[i].tr.show();
              rows[i].key.removeClass("faded").html(point.key);
              for (var j = 0; j < rows[i].columns.length; j++)
                rows[i].columns[j].removeClass("faded").html(point.columns[j]);
            }
          }
          if (nextUpdate != null && nextUpdate != offset) {
            console.log("Next: {0}", nextUpdate);
            update(nextUpdate);
          }
          nextUpdate = null;
        });
      }

      // Setup scroll handler & call to load first block of data
      $(id + " .scroller").on("scroll", function () {
        var offset = Math.ceil($(id + " .scroller").scrollTop() / rowHeight);
        if (nextUpdate == null)
          update(offset);
        else
          nextUpdate = offset;
      });
      update(0);
    }

    $.ajax({ url: serviceUrl + "/metadata" }).done(function (res) {
      initialize(JSON.parse(res));
    });
  }
</script>"""

/// JavaScript that calls the function defined in `gridLiveScript` for a given grid
let gridLiveScriptCustom id height serviceUrl =
  sprintf "<script type=\"text/javascript\">
      $(function () { setupGrid(\"#%s\", %d, \"%s\"); });
    </script>" id height serviceUrl

/// Default body for live loaded frames 
let gridLiveBody id = "<div class=\"grid live-grid\" id=\"" + id + """">
  <table>
    <thead>
      <tr class="head"><th>#</th><th>&nbsp;</th></tr>
    </thead>
    <tbody class="body">
      <tr><th>&nbsp;</th><td>&nbsp;</td></tr>
    </tbody>
  </table>
  <div class="scroller">
    <div class="spacer"></div>
  </div>
</div>
"""

// --------------------------------------------------------------------------------------
// Implementation of the grid formatting
// --------------------------------------------------------------------------------------

let (|Float|_|) (v:obj) = if v :? float then Some(v :?> float) else None
let (|Float32|_|) (v:obj) = if v :? float32 then Some(v :?> float32) else None

/// Format value as a single-literal paragraph
let formatValue (floatFormat:string) def = function
  | OptionalValue.Present(Float v) -> v.ToString(floatFormat)
  | OptionalValue.Present(Float32 v) -> v.ToString(floatFormat)
  | OptionalValue.Present(v) -> v.ToString()
  | _ -> def

/// Returns unique ID
let nextGridId =
  let counter = ref 0
  let pid = System.Diagnostics.Process.GetCurrentProcess().Id
  fun () -> incr counter; sprintf "fslab-grid-%d-%d" pid counter.Value



// Formatting in offline mode
let mapSteps (startCount, endCount) g input =
  input
  |> Seq.startAndEnd startCount endCount
  |> Seq.map (function Choice1Of3 v | Choice3Of3 v -> g (Some v) | _ -> g None)
  |> List.ofSeq

let mapStepsIndexed (startCount, endCount) g count =
  if count >= startCount + endCount then
    [ for i in 1 .. startCount do yield g(Some i)
      yield g None
      for i in count-endCount .. count-1 do yield g(Some i) ]
  else
    [ for i in 0 .. count-1 do yield g(Some i) ]

let registerStandaloneGrid colKeys rowCount getRows =
  let id = nextGridId()
  let frows = Styles.getNumberRange "grid-row-counts"
  let fcols = Styles.getNumberRange "grid-column-counts"

  // Generate the table head
  let sb = System.Text.StringBuilder()
  sb.Append("<table><thead><tr class=\"head\"><th>#</th>") |> ignore
  colKeys |> mapSteps fcols (function
    | Some (k:string) -> sb.Append("<th>").Append(k).Append("</th>")
    | _ -> sb.Append("<th>...</th>")) |> ignore
  sb.Append("</tr></thead><tbody>") |> ignore

  let rows =
    rowCount |> mapStepsIndexed frows (fun rowIndex ->
      // Get row or generate row consisting of ... for all columns
      let rowKey, row =
        match rowIndex with
        | Some i -> getRows i 1 |> Seq.head
        | _ -> box "...", colKeys |> Array.map (fun _ -> "...")

      // Generate row, using ... for one column if there are too many
      sb.Append("<tr><th>" + rowKey.ToString() + "</th>") |> ignore
      colKeys.Length |> mapStepsIndexed fcols (function
        | None -> sb.Append("<td>...</td>")
        | Some i -> sb.Append("<td>" + row.[i] + "</td>")) |> ignore
      sb.Append("</tr>") )
  
  let table = sb.Append("</tbody></table>").ToString()
  seq [ "style", Styles.replaceStyles gridStyles ],
  "<div class=\"grid\" id=\"" + id + "\">" + table + "</div>"



// Background server for live grids
type GridJson = JsonProvider<"""{
    "metadata":{"columns":["Foo","Bar"], "rows":100},
    "row":{"key":"Foo","columns":["Foo","Bar"]}
  }""">

let registerLiveGrid colKeys rowCount getRows =
  let metadata = GridJson.Metadata(colKeys, rowCount).ToString()
  let app =
    Writers.setHeader  "Access-Control-Allow-Origin" "*" >=>
    Writers.setHeader "Access-Control-Allow-Headers" "content-type" >=>
    choose [
      Filters.path "/metadata" >=> Successful.OK (metadata) 
      Filters.pathScan "/rows/%d" (fun row -> request (fun r ->
          let count = int (Utils.Choice.orDefault "100" (r.queryParam("count")))
          let count = min rowCount (row + count) - row
          let rows =
            getRows row count |> Array.map (fun (key, cols) ->
              GridJson.Row(string key, cols).JsonValue)
          JsonValue.Array(rows).ToString()
          |> Successful.OK ))
    ]
  let url = Server.instance.Value.AddPart(app)
  let id = nextGridId()
  seq [ "style", Styles.replaceStyles gridStyles;
        "script", gridLiveScript
        "script", gridLiveScriptCustom id 500 url ], 
  gridLiveBody id


// --------------------------------------------------------------------------------------
// Register printers using the fsi object
// --------------------------------------------------------------------------------------

type ISeriesOperation<'R> =
  abstract Invoke<'K, 'V when 'K : equality> : Series<'K, 'V> -> 'R

let (|Series|_|) (value:obj) =
  value.GetType()
  |> Seq.unfold (fun t -> if t = null then None else Some(t, t.BaseType))
  |> Seq.tryFind (fun t -> t.Name = "Series`2")
  |> Option.map (fun t -> t.GetGenericArguments())

let invokeSeriesOperation tys obj (op:ISeriesOperation<'R>) =
  typeof<ISeriesOperation<'R>>.GetMethod("Invoke")
    .MakeGenericMethod(tys).Invoke(op, [| obj |]) :?> 'R

// Helpers for estimating positions inside a BigDeedle frame
let midKeyFuncs = System.Collections.Generic.Dictionary<System.Type, obj -> obj -> float -> obj>()
let addMidKeyFunc (f:'T -> 'T -> float -> 'T) =
  midKeyFuncs.Add(typeof<'T>, fun a b r -> box (f (unbox a) (unbox b) r))
let midKeyFunc (k1:'T) (k2:'T) ratio : 'T =
  let f = midKeyFuncs.[typeof<'T>]
  f k1 k2 ratio |> unbox

// How to calculate middle key for various types
addMidKeyFunc(fun (dt1:System.DateTimeOffset) (dt2:System.DateTimeOffset) ratio ->
  dt1 + System.TimeSpan.FromMilliseconds((dt2 - dt1).TotalMilliseconds * ratio))

// Helper for BigDeedle frames, that returns key range within big frame
// based on a ratio which is a value between 0 and 1
let getKeyRange (rowIndex:Deedle.Indices.IIndex<_>) count ratio =
  let first = rowIndex.KeyAt(rowIndex.AddressOperations.FirstElement)
  let last = rowIndex.KeyAt(rowIndex.AddressOperations.LastElement)
  let midKey = midKeyFunc first last ratio
  let midAddr = snd (rowIndex.Lookup(midKey, Lookup.ExactOrSmaller, fun _ -> true).Value)

  // Get rows within a range around the estimated location
  try
    // Try to get the next 10 items (this may be out of range)
    rowIndex.KeyAt midAddr,
    rowIndex.KeyAt(rowIndex.AddressOperations.AdjustBy(midAddr, int64 (count-1)))
  with :? System.IndexOutOfRangeException ->
    // If it is, try to get the last 10 items (could fail for frames with less than 10 items...)
    rowIndex.KeyAt(rowIndex.AddressOperations.AdjustBy(midAddr, int64 (-count+1))),
    rowIndex.KeyAt(rowIndex.AddressOperations.LastElement)

// Register formatters for Deedle objects
let registerFormattable (obj:IFsiFormattable) =
  let floatFormat = Styles.getStyle "table-float-format"
  let registerGrid =
    if Styles.standaloneHtmlOutput() then registerStandaloneGrid
    else registerLiveGrid

  match obj with
  | Series tys ->
    { new ISeriesOperation<_> with
        member x.Invoke(s) =
          if s.Index.GetType().Name = "VirtualOrderedIndex`1" then
            // Experimental BigDeedle support (without accessing RowCount)
            let colKeys = [| "Value" |]
            let rowCount = 1000000
            let getRows index count =
              let loKey, hiKey = getKeyRange s.Index count (float index / 1000000.0)
              let partialSeries = s.[loKey .. hiKey]
              [| for (KeyValue(k, v)) in partialSeries.ObservationsAll do
                  yield box k, [| formatValue floatFormat "N/A" v |] |] |> Array.truncate count
            registerGrid colKeys rowCount getRows
          else
            // Ordinary small Deedle series, use precise indexing
            let colKeys = [| "Value" |]
            let rowCount = s.KeyCount
            let getRows index count =
              Array.init count (fun i ->
                let index = index + i
                box (s.GetKeyAt(index)),
                [| formatValue floatFormat "N/A" (s.TryGetAt(index)) |] )
            registerGrid colKeys rowCount getRows }
    |> invokeSeriesOperation tys obj

  | :? IFrame as f ->
    { new IFrameOperation<_> with
        member x.Invoke(df) =
          if df.RowIndex.GetType().Name = "VirtualOrderedIndex`1" then
            // Experimental BigDeedle support (without accessing RowCount)
            let colKeys = df.ColumnKeys |> Seq.map (box >> string) |> Array.ofSeq
            let rowCount = 1000000
            let getRows index count =
              let loKey, hiKey = getKeyRange df.RowIndex count (float index / 1000000.0)
              let rows = df.Rows.[loKey .. hiKey]
              [| for (KeyValue(k, v)) in rows.Rows.Observations do
                  let values = v.Vector.DataSequence |> Seq.map string |> Array.ofSeq
                  yield box k, values |] |> Array.truncate count
            registerGrid colKeys rowCount getRows
          else
            // Ordinary small Deedle frame - use precise indexing
            let colKeys = df.ColumnKeys |> Seq.map (box >> string) |> Array.ofSeq
            let rowCount = df.RowCount
            let getRows index count =
              Array.init count (fun i ->
                let index = index + i
                box (df.GetRowKeyAt(int64 index)),
                df.GetRowAt(index).Vector.DataSequence
                |> Seq.map (formatValue floatFormat "N/A")
                |> Array.ofSeq)
            registerGrid colKeys rowCount getRows }
    |> f.Apply
  | _ -> Seq.empty, "(Error: Deedle object implements IFsiFormattable, but it's not a frame or series)"

fsi.AddHtmlPrinter(fun (obj:Deedle.Internal.IFsiFormattable) ->
  registerFormattable obj)

