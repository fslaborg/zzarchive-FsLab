namespace FsLab

open System.IO
open FSharp.Literate

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
    /// Template location (see comment on `ProcessingContext.Create`)
    TemplateLocation : string option 
    /// Should the output be standalone (without any background servers)?
    Standalone : bool

    /// Set this if you only want to process files in this list.
    /// (For example, use `["MyJournal.fsx"]` to only generate this one jounral)
    FileWhitelist : string list option
    /// Specify an error handler when evaluation fails
    FailedHandler : FsiEvaluationFailedInfo -> unit }

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
    { Root = root 
      Output = Path.Combine(root, "output")
      OutputKind = OutputKind.Html 
      TemplateLocation = Some(Path.Combine(root, "packages/FsLab.Runner"))
      FileWhitelist = None 
      Standalone = false
      FailedHandler = ignore }