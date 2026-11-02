@echo off
setlocal
pushd "%~dp0.."
dotnet build "tools/POS.InvoicePreview/POS.InvoicePreview.csproj" -m:1 -nr:false -p:UseSharedCompilation=false -p:IntermediateOutputPath=obj/T52Preview/ -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false -v minimal
if errorlevel 1 (
    popd
    exit /b 1
)
start "" "tools\POS.InvoicePreview\bin\Debug\net10.0-windows\POS.InvoicePreview.exe"
popd
endlocal
