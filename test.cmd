@echo off
setlocal
pushd "%~dp0"
build && packages\NUnit.ConsoleRunner.3.2.1\tools\nunit3-console.exe .\tests\bin\Debug\DeJson.Tests.dll
popd
