# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

$pwd = Split-Path -Path $MyInvocation.MyCommand.Path

$source = "$($pwd)\Chem4Word-Setup.3.1.16.Release.6.msi";

$targets = @();

$targets += "Chem4Word.Setup.3.1.0.Alpha.1.msi"

$targets += "Chem4Word.Setup.3.1.1.Beta.1.msi"
$targets += "Chem4Word.Setup.3.1.2.Beta.2.msi"
$targets += "Chem4Word.Setup.3.1.3.Beta.3.msi"
$targets += "Chem4Word.Setup.3.1.4.Beta.4.msi"
$targets += "Chem4Word.Setup.3.1.5.Beta.5.msi"
$targets += "Chem4Word.Setup.3.1.6.Beta.6.msi"
$targets += "Chem4Word.Setup.3.1.7.Beta.7.msi"
$targets += "Chem4Word.Setup.3.1.8.Beta.8.msi"
$targets += "Chem4Word.Setup.3.1.9.Beta.9.msi"
$targets += "Chem4Word.Setup.3.1.10.Beta.10.msi"

$targets += "Chem4Word.Setup.3.1.11.Release.1.msi"
$targets += "Chem4Word.Setup.3.1.12.Release.2.msi"
$targets += "Chem4Word.Setup.3.1.13.Release.3.msi"
$targets += "Chem4Word.Setup.3.1.14.Release.4.msi"
$targets += "Chem4Word.Setup.3.1.15.Release.5.msi"

foreach ($target in $targets)
{
    Copy-Item $source -Destination "$($pwd)\$($target)"; 
}