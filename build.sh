#!/bin/bash
if test "$OS" = "Windows_NT"
then
  # use .Net
  if test "$1" = "quickrun" 
  then
    packages/FAKE/tools/FAKE.exe run --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
  else
    .paket/paket.bootstrapper.exe
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
      exit $exit_code
    fi
    .paket/paket.exe restore
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
  	  exit $exit_code
    fi
    packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
  fi
else
  # use mono
  if test "$1" = "quickrun" 
  then
    mono packages/FAKE/tools/FAKE.exe run --fsiargs -d:NO_FSI_ADDPRINTER -d:MONO build.fsx
  else
    mono .paket/paket.bootstrapper.exe
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
      exit $exit_code
    fi
    mono .paket/paket.exe restore
    exit_code=$?
    if [ $exit_code -ne 0 ]; then
  	  exit $exit_code
    fi
    mono packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:NO_FSI_ADDPRINTER -d:MONO build.fsx 
  fi
fi
