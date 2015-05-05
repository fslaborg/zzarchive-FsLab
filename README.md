FsLab 
=====

FsLab is a single package that gives you all you need for doing data science with
F#. FsLab includes an explorative data manipulation library, type providers for easy
data access, a simple charting library and support for integration with R and numerical
computing libraries. All available in a single package and ready to use!

Developer notes
---------------

### Project structure

The project produces three things:

 1. **FsLab** NuGet package with references to all FsLab libraries and `FsLab.fsx` load script
 2. **FsLab.Runner** NuGet package that is used by the Journal template and contains formatters
   (for embedding output into HTML), Journal generation code & styles and build scripts for Journals
 3. **FsLab Journal** Template for Visual Studio (which is a simple wrapper for the above)

The source files in the repository are organized as follows:

| Directory or file  | Comment
|--------------------|---------------
| `misc`             | Icons and other non-source-code things
| `src/FsLab.Runner` | Source for the DLL in the `FsLab.Runner` NuGet package
| `src/misc`         | Other files included in the `FsLab.Runner` NuGet package
| `src/experiments`  | Item templates for Visual Studio template
| `src/journal`      | Project template for Visual Studio template
| `src/template`     | Build files for Visual Studio template
| `src/FsLab.fsx`    | Script included in the `FsLab` NuGet package
| `src/*.nuspec`     | NuGet files for building the packages
| `build.fsx`        | FAKE script that does all the magic (below)

### Building FsLab

If you want to be able to build FsLab Journal template, you'll need Visual Studio 2013 SDK.
To update one or more dependencies, use the following steps:

 * Run `build Clean` to make sure that there are only source files around
 * Run `.paket/paket.exe update` to update the dependencies
 * Run `build` to build everything or `build NuGet` to build everything except for
   the FsLab Journal template (useful if you don't have the SDK installed)
 * Add new line with version information to `RELEASE_NOTES.md`!
 * Run `publish` from command line to upload NuGet package (if you have the rights)
 
After running `build NuGet` for the first time, you can also edit the
extensions in `src/FsLab.fsx`. 

If there were any changes in the Journal template, you also need to update the
[journal template](https://github.com/fslaborg/FsLab.Templates/tree/journal) in the
[FsLab.Templates](https://github.com/fslaborg/FsLab.Templates) repository. At some
point, these should be generated automatically too!
