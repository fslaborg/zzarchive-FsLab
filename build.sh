#!/bin/bash
if test "$OS" = "Windows_NT"
then
  MONO=""
else
  MONO="mono"
fi

if test "$1" = "quickrun"
then
  $MONO packages/FAKE/tools/FAKE.exe run --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
else
  $MONO .paket/paket.bootstrapper.exe
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi
  if [ -e "paket.lock" ]
  then
    $MONO .paket/paket.exe restore
  else
    $MONO .paket/paket.exe install
  fi
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
	  exit $exit_code
  fi
  $MONO packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:NO_FSI_ADDPRINTER build.fsx
fi
