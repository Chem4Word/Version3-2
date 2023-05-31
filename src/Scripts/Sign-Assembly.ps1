#
# Source: https://www.reddit.com/r/csharp/comments/9d8uxb/vs2017_msbuild_helpful_tricks/
#
# TIP: Use [IO.Path]::GetDirectoryName($YOUR_PATH_VARIABLE) to retrieve the directory of a path.
# TIP: Use [IO.Path]::GetFileName($YOUR_PATH_VARIABLE) to retrieve the filename of a path.
#
# -- Post-build Event :-
#
# powershell.exe -NoLogo -NonInteractive -ExecutionPolicy Unrestricted -Command ^
#   .'$(ProjectDir)Scripts\Sign-Assembly.ps1' ^
#   -TargetFileName $(TargetFileName) ^
#   -TargetPath $(TargetPath)

# .\SignAssemby.ps1 -TargetFileName "Chem4Word-Setup.exe" -TargetPath "C:\Dev\vso\chem4word\Version3\src\Installer\Chem4WordSetup\bin\Debug\Chem4Word-Setup.exe"

param
(
	[string]$TargetFileName,
	[string]$TargetPath
)

try
{
	$signToolPath = "C:\Tools\Azure\SignTool"
	$signToolExe = "$($signToolPath)\Sign.exe"

	if (Test-Path $signToolExe)
	{
		# Call .Net Foundation Code Signing Service
		Write-Output "Signing $($TargetFileName) ..."
		& $signToolExe code azure-key-vault $TargetPath `
			-kvt $env:SignToolTenantId -kvi $env:SignToolClientId -kvs $env:SignToolClientSecret -kvu $env:SignToolVaultUrl -kvc $env:SignToolCertificate `
			-t "http://timestamp.digicert.com" -pn "Chem4Word" -d "Chem4Word installer" -u "https://www.chem4word.co.uk" -v Information
	}

	# Check that the file was signed
	Write-Output "Checking if $($TargetFileName) is signed ..."
	$sig = Get-AuthenticodeSignature -FilePath $TargetPath
	if ($sig.Status -eq "Valid")
	{
		Write-Output "File $($TargetFileName) is signed by $($sig.SignerCertificate.Subject)."
		exit 0
	}
	else
	{
		Write-Output "File $($TargetFileName) is not signed !"
		exit 1
	}
}
catch
{
	Write-Error $_.Exception.Message
	exit 2
}

# Should never get here !
exit 3
