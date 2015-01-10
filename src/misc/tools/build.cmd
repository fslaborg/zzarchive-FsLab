@echo off 
build\paket.bootstrapper.exe
build\paket.exe
build\FAKE\tools\FAKE.exe build.fsx %*