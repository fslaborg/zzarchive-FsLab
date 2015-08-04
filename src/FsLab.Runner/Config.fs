namespace FsLab

open System.IO
open FSharp.Literate

// ----------------------------------------------------------------------------
// Formatter configuration
// ----------------------------------------------------------------------------

/// The type can be used to configure formatting of frames, series and 
/// matrices in FsLab journals. Use it if you want to override the default
/// numbers of columns and rows that are printed.
type FormatConfig = 
  { /// How many columns of a frame should be rendered at the start
    StartColumnCount : int
    /// How many columns of a frame should be rendered at the end
    EndColumnCount : int
    /// How many rows of a frame should be rendered at the start
    StartRowCount : int
    /// How many rows of a frame should be rendered at the end
    EndRowCount : int
    
    /// How many items from a series should be rendered at the start
    StartItemCount : int
    /// How many items from a series should be rendered at the end
    EndItemCount : int

    // How many columns from a matrix should be rendered at the start
    MatrixStartColumnCount : int
    // How many columns from a matrix should be rendered at the end
    MatrixEndColumnCount : int
    // How many rows from a matrix should be rendered at the start
    MatrixStartRowCount : int
    // How many rows from a matrix should be rendered at the end
    MatrixEndRowCount : int

    // How many items from a vector should be rendered at the start
    VectorStartItemCount : int
    // How many items from a vector should be rendered at the end
    VectorEndItemCount : int }
    static member Create () =
      { FormatConfig.StartColumnCount = 3
        EndColumnCount = 3
        StartRowCount = 8
        EndRowCount = 4

        StartItemCount = 5
        EndItemCount = 3
        
        MatrixStartColumnCount = 7
        MatrixEndColumnCount = 2
        MatrixStartRowCount = 10
        MatrixEndRowCount = 4
        
        VectorStartItemCount = 7
        VectorEndItemCount = 2 }

    /// Transform the context using the specified function
    member x.With(f:FormatConfig -> FormatConfig) = f x

    // Tuples with the counts, for easy use later on
    member internal x.fcols = x.StartColumnCount, x.EndColumnCount
    member internal x.frows = x.StartRowCount, x.EndRowCount
    member internal x.sitms = x.StartItemCount, x.EndItemCount
    member internal x.mcols = x.MatrixStartColumnCount, x.MatrixEndColumnCount
    member internal x.mrows = x.MatrixStartRowCount, x.MatrixEndRowCount
    member internal x.vitms = x.VectorStartItemCount, x.VectorEndItemCount


// ----------------------------------------------------------------------------
// Processing context
// ----------------------------------------------------------------------------

/// Represents state passed around during processing
type ProcessingContext = 
  { /// Root path with journals (see comment on `ProcessingContext.Create`)
    Root : string 
    /// Output folder (see comment on `ProcessingContext.Create`)
    Output : string
    /// What kind of output should be produced
    OutputKind : OutputKind 
    /// Format for floating poin numbers. The default is `G4`.
    FloatFormat : string
    /// Format information for Frames, matrices.
    FormatConfig : FormatConfig
    /// Template location (see comment on `ProcessingContext.Create`)
    TemplateLocation : string option 

    /// Set this if you only want to process files in this list.
    /// (For example, use `["MyJournal.fsx"]` to only generate this one jounral)
    FileWhitelist : string list option
    
    /// Specify an error handler when evaluation fails
    FailedHandler : FsiEvaluationFailedInfo -> unit

    /// Custom FSI evaluator - you can use this to override the default one
    /// (but you should call `Journal.wrapFsiEvaluator` on it to register FsLab
    /// transformations, otherwise it will not do anything useful)
    FsiEvaluator : IFsiEvaluator option }

  /// Creates a default processing context with all the 
  /// basic things needed to produce journals. 
  ///
  /// ## Parameters
  ///  - `root` is the root folder where you have your `*.fsx` journals
  ///  - `output` is an output folder where HTML files are placed
  ///  - `templateLocation` is the place with `styles` folder. A reasonable
  ///    default is `"packages/FsLab.Runner"`.
  /// 
  static member Create(root) = 
    { Root = root; 
      Output = Path.Combine(root, "output");
      OutputKind = OutputKind.Html; 
      FloatFormat = "G4";
      FormatConfig = FormatConfig.Create();
      TemplateLocation = Some(Path.Combine(root, "packages/FsLab.Runner"));
      FileWhitelist = None; 
      FailedHandler = ignore; 
      FsiEvaluator = None }
  
  /// Transform the context using the specified function
  member x.With(f:ProcessingContext -> ProcessingContext) = f x

