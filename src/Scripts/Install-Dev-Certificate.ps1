# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

$pwd = Split-Path -Path $MyInvocation.MyCommand.Path

CD "$($pwd)\Certificates\Developer"

$certificate = "Chem4WordAddIn.pfx"

Write-Host "Importing Certificate " $certificate

$password = ConvertTo-SecureString -String "Password_123" -Force -AsPlainText

Import-PfxCertificate -FilePath $certificate -CertStoreLocation Cert:\CurrentUser\My -Exportable -Password $password

# You need torce git to add the file
# CD .\src\Scripts\Certificates\Developer
# git add -f Chem4WordAddIn.pfx