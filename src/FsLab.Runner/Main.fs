namespace FsLab

open FsLab.Runner
open System.IO
open System.Reflection
open FSharp.Literate

/// Module with public functions for generating FsLab journals
module Journal =
  /// Process journals as specified by the provided `ProcessingContext`
  /// and return a list of names and headings of the journals
  let processJournals ctx = Runner.processScriptFiles true ctx

  /// Update all journals. Call this after `processJournals` to 
  /// regenerate journals that have been changed.
  let updateJournals ctx = Runner.processScriptFiles false ctx |> ignore

  /// Get default "index" journal. If there are multiple journals,
  /// this generates a new index file. If there is one, returns it.
  let getIndexJournal ctx files = Runner.getDefaultFile ctx files