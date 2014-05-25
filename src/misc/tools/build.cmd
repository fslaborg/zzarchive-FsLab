@echo off
cls
if exist packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe (
  if not exist packages\FAKE\tools\Fake.exe ( 
    packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion  
  )
  packages\FAKE\tools\FAKE.exe build.fsx %*
)
if exist ..\packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe (
  if not exist ..\packages\FAKE\tools\Fake.exe ( 
    ..\packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe install FAKE -OutputDirectory ..\packages -ExcludeVersion  
  )
  ..\packages\FAKE\tools\FAKE.exe build.fsx %*
)
pause
