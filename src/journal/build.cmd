@echo off
cls

if "%1" == "quickrun" (
  packages\FAKE\tools\FAKE.exe run --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
) else (
  .paket\paket.bootstrapper.exe
  if errorlevel 1 (
    exit /b %errorlevel%
  )
  if not exist paket.lock (
    .paket\paket.exe install
  ) else (
    .paket\paket.exe restore
  )
  if errorlevel 1 (
    exit /b %errorlevel%
  )
  packages\FAKE\tools\FAKE.exe %* --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
)
