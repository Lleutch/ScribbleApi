@echo off

@echo "Installing all the dependencies"

call dependencies.cmd

@echo "Running web server through FAKE with build.local.fsx"
packages\FAKE\tools\FAKE.exe %* --fsiargs build.local.fsx