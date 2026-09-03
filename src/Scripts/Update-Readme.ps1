$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition

CD ..\Tools\Package-Scanner\bin\Debug

.\PackageScanner.exe

CD $($scriptPath)

Write-Host "Please check README.MD for any errors"