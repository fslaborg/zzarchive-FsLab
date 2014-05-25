@echo off
if not exist build\FAKE\tools\Fake.exe ( 
  build\NuGet.exe install FAKE -OutputDirectory build -ExcludeVersion  
  )
  build\FAKE\tools\FAKE.exe build.fsx %*
)