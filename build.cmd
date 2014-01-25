@"%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0\fsi.exe" GenerateScripts.fsx
del *.nupkg
nuget\NuGet.exe pack FsLab.nuspec
