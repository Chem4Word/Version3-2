#
# Source: https://www.reddit.com/r/csharp/comments/9d8uxb/vs2017_msbuild_helpful_tricks/
#
# TIP: Use [IO.Path]::GetDirectoryName($YOUR_PATH_VARIABLE) to retrieve the directory of a path.
# TIP: Use [IO.Path]::GetFileName($YOUR_PATH_VARIABLE) to retrieve the filename of a path.
#
# -- Post-build Event :-
#
# powershell.exe -NoLogo -NonInteractive -ExecutionPolicy Unrestricted -Command ^
#   .'$(ProjectDir)Scripts\Sign-Multiple-Assemblies.ps1' ^
#   -FilesToSign $(TargetFiles) ^
#   -PathToFiles $(TargetPath)

# .\Sign-Multiple-Assemblies.ps1 -TargetPath 'C:\Dev\AzDo\Chem4Word\Version3-3\Version3-3\src\Chem4Word.V3\bin\Setup\PlugIns\' -TargetFiles 'Chem4Word*.dll'
# .\Sign-Multiple-Assemblies.ps1 -TargetPath 'C:\Dev\AzDo\Chem4Word\Version3-3\src\Chem4Word.V3\bin\Setup\' -TargetFiles 'Chem4Word*.dll'
# .\Sign-Multiple-Assemblies.ps1 -TargetPath 'C:\Dev\AzDo\Chem4Word\Version3-3\src\Chem4Word.V3\bin\Setup\' -TargetFiles 'Chem4Word*.exe'
# .\Sign-Multiple-Assemblies.ps1 -TargetPath 'C:\Dev\AzDo\Chem4Word\Version3-3\src\Chem4Word.V3\bin\Setup\' -TargetFiles 'Chem4Word.V3.dll.manifest'

param
(
	[string]$PathToFiles,
	[string]$FilesToSign
)

try
{
	$signToolPath = "C:\Tools\Azure\SignTool"
	$signToolExe = "$($signToolPath)\Sign.exe"

    if ($TargetPath -eq "" -or $TargetFiles -eq "")
    {
        Write-Host "Parameters not supplied"
        exit 1
    }

	if (Test-Path $signToolExe)
	{
		Write-Host "Signing files $($FilesToSign) in folder $($PathToFiles)"
		
        & $signtoolexe code azure-key-vault "$($FilesToSign)" `
		 -kvt $env:signtooltenantid -kvi $env:signtoolclientid -kvs $env:signtoolclientsecret -kvu $env:signtoolvaulturl -kvc $env:signtoolcertificate `
		 -t "http://timestamp.digicert.com" -pn "chem4word" -b "$($PathToFiles)" -d "chem4word installer" -u "https://www.chem4word.co.uk" -v information

        $files = Get-ChildItem -Path $PathToFiles -Filter $FilesToSign -File
		$count = $files.Length
        foreach ($file in $files)
		{
			Write-Output "Checking if $($file.Name) is signed ..."
			$sig = Get-AuthenticodeSignature -FilePath $file.FullName
			if ($sig.Status -eq "Valid")
			{
				Write-Output "File $($file.Name) is signed by $($sig.SignerCertificate.Subject)."
				$count--
			}
			else
			{
				Write-Output "***** File $($file.Name) is not signed ! *****"
			}
        }
		
		# Count should be zero
		exit $count
	}
}
catch
{
	Write-Error $_.Exception.Message
	exit 2
}

# Should never get here !
exit 3