@echo off

set release=Chem4Word-Setup.3.2.21.Release.17.msi
set working=C:\Temp
set signtoolpath=C:\Tools\Azure\SignTool\sign.exe

echo Copying files to %working%
copy ..\Chem4Word.V3\Data\Chem4Word-Versions.xml %working% > nul
copy ..\Chem4Word.V3\Data\index.html %working% > nul

copy ..\Installer\Chem4WordSetup\bin\Setup\Chem4Word-Setup.exe %working% > nul
copy ..\Installer\WiXInstaller\bin\Setup\%release% %working% > nul

pushd %working%
dir

echo Signing Chem4Word-Setup.exe
%signtoolpath% code azure-key-vault Chem4Word-Setup.exe ^
    -kvt %SignToolTenantId% -kvi %SignToolClientId% -kvs %SignToolClientSecret% -kvu %SignToolVaultUrl% -kvc %SignToolCertificate% ^
    -t "http://timestamp.digicert.com" -pn "Chem4Word" -d "Chem4Word installer" -u "https://www.chem4word.co.uk" -v Information

echo Signing %release%
%signtoolpath% code azure-key-vault %release% ^
    -kvt %SignToolTenantId% -kvi %SignToolClientId% -kvs %SignToolClientSecret% -kvu %SignToolVaultUrl% -kvc %SignToolCertificate% ^
    -t "http://timestamp.digicert.com" -pn "Chem4Word" -d "Chem4Word installer" -u "https://www.chem4word.co.uk" -v Information

dir
popd

echo FTP these files to server
pause
