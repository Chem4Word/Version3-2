# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

$pwd = Split-Path -Path $MyInvocation.MyCommand.Path

$source = "$($pwd)\Chem4Word-Setup.3.0.35.Release.23.msi";

$targets = @();

$targets += "Chem4Word-Setup.3.0.14.Release.1.msi"
$targets += "Chem4Word-Setup.3.0.15.Release.2.msi"
$targets += "Chem4Word-Setup.3.0.16.Release.3.msi"
$targets += "Chem4Word-Setup.3.0.17.Release.4.msi"
$targets += "Chem4Word-Setup.3.0.18.Release.5.msi"
$targets += "Chem4Word-Setup.3.0.19.Release.6.msi"
$targets += "Chem4Word-Setup.3.0.20.Release.7.msi"
$targets += "Chem4Word-Setup.3.0.21.Release.8.msi"
$targets += "Chem4Word-Setup.3.0.22.Release.9.msi"
$targets += "Chem4Word-Setup.3.0.23.Release.10.msi"
$targets += "Chem4Word-Setup.3.0.24.Release.11.msi"
$targets += "Chem4Word-Setup.3.0.25.Release.12.msi"
$targets += "Chem4Word-Setup.3.0.26.Release.14.msi"
$targets += "Chem4Word-Setup.3.0.27.Release.15.msi"
$targets += "Chem4Word-Setup.3.0.28.Release.16.msi"
$targets += "Chem4Word-Setup.3.0.29.Release.17.msi"
$targets += "Chem4Word-Setup.3.0.30.Release.18.msi"
$targets += "Chem4Word-Setup.3.0.31.Release.19.msi"
$targets += "Chem4Word-Setup.3.0.32.Release.20.msi"
$targets += "Chem4Word-Setup.3.0.33.Release.21.msi"
$targets += "Chem4Word-Setup.3.0.34.Release.22.msi"

foreach ($target in $targets)
{
    Copy-Item $source -Destination "$($pwd)\$($target)"; 
}