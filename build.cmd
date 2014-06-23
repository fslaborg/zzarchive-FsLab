@echo off
cls
if not exist packages\FAKE\tools\Fake.exe ( 
  .nuget\nuget.exe install FAKE -OutputDirectory packages -Version 3.0.0-alpha7 -ExcludeVersion  
)
packages\FAKE\tools\FAKE.exe build.fsx %*
pause