FsLab 
=====

FsLab is a single package that gives you all you need for doing data science with
F#. FsLab includes explorative data manipulation library, type providers for easy
data access, simple charting library, support for integration with R and numerical
computing libraries. All available in a single package and ready to use!

Developer notes
---------------

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
