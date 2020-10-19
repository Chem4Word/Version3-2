# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

$pwd = Split-Path -Path $MyInvocation.MyCommand.Path

$source = "$($pwd)\Chem4Word-Setup.3.1.16.Release.6.msi";

$targets = @();

$targets += "Chem4Word.Setup.2.0.1.0.Beta.2.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.5.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.6.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.8.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.7.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.9.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.10.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.11.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.06.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.15.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.16.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.29.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.08.03.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.09.01.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.03.31.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.10.18.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.10.25.msi"

foreach ($target in $targets)
{
    Copy-Item $source -Destination "$($pwd)\$($target)"; 
}