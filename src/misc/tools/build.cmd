@echo off 
IF NOT EXIST packages\FAKE\tools\FAKE.exe (
  .paket\paket.exe add nuget FAKE
)  
packages\FAKE\tools\FAKE.exe build.fsx %*