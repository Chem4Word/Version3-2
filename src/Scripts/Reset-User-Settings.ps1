#########################
# ResetUserSettings.ps1 #
#########################

CLS

Write-Host "Clearing Chem4Word user settings"

# Clear Settings @ HKEY_CURRENT_USER\Software\Chem4Word V3

$Key = "HKCU:\SOFTWARE\Chem4Word V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
if ($k -ne $null)
{
    Write-Host " Registry Key '$($Key)' found ..."
    Write-Host "  Last Update Check: $($k.'Last Update Check')"
    Write-Host "  Versions Behind: $($k.'Versions Behind')"

    Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
    Remove-Item -Path $Key -Recurse
}
else
{
    # Write-Host "Registry Key '$($Key)' not found."
}

# -----------------------------------------------

Write-Host "Clearing Microsoft Word disabled Add-Ins"

# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\14.0\Word\Resiliency\DisabledItems
# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\15.0\Word\Resiliency\DisabledItems
# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\16.0\Word\Resiliency\DisabledItems

for ($i=14; $i -le 16; $i++)
{
    $key = "HKCU:\SOFTWARE\Microsoft\Office\$($i).0\Word\Resiliency\DisabledItems";
    $k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
    if ($k -ne $null)
    {
        Write-Host "Clearing $($Key)"
        foreach ($kvp in $k.PSObject.Properties)
        {
            if (!$kvp.Name.StartsWith("PS"))
            {
                Write-Host "  Removing $($kvp.Name)" -ForegroundColor Cyan
                Remove-ItemProperty -Path $Key -Name $kvp.Name
            }
        }
    }
}

# -----------------------------------------------

Write-Host "Clearing Chem4Word user data"

$folders = @()
$folders += "$($env:ProgramData)\Chem4Word.V3"
$folders += "$($env:USERPROFILE)\AppData\Local\Chem4Word.V3"
$folders += "$($env:USERPROFILE)\AppData\Local\Chemistry Add-in for Word"
$folders += "$($env:USERPROFILE)\AppData\Local\assembly\dl3"

foreach ($folder in $folders)
{
    if (Test-Path $folder)
    {
        Write-Host "Deleting folder tree '$($folder)'"
        Get-ChildItem -Path $folder -Recurse | Remove-Item -force -recurse
        Remove-Item $folder
    }
}