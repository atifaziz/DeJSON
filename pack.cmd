@echo off
pushd "%~dp0"
call build || goto :end
if not exist dist mkdir dist || goto :end
nuget pack -Symbols -OutputDirectory "dist" src\DeJson.csproj %*
:end
popd
