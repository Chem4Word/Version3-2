##########################
# GetBackToSquareOne.ps1 #
##########################

CLS

########################
# MUST BE RUN ELEVATED #
########################

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║ This script should be run as Administrator ! ║" -ForegroundColor Yellow
    Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Yellow

    Break Script
}

# -----------------------------------------------

$delete = $true

$filename = "C:\Program Files (x86)\Chem4Word V3\Chem4Word.V3.vsto"

$isIntalled = Test-Path -path $filename
if ($isIntalled -eq $true)
{
    Write-Host "Chem4Word V3 is installed, please un-install it then run this script again."

    Break Script
}
else
{
    # Write-Host "Chem4Word V3 is NOT installed."
}

# -----------------------------------------------

Write-Host "Clearing Chem4Word user settings"

# Clear Settings @ HKEY_CURRENT_USER\Software\Chem4Word V3

$Key = "HKCU:\SOFTWARE\Chem4Word V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
if ($k -ne $null)
{
    Write-Host " Registry Key '$($Key)' found ..."
    Write-Host "  Last Update Check: $($k.'Last Update Check')"
    Write-Host "  Versions Behind: $($k.'Versions Behind')"

    if ($isIntalled -eq $false -and $delete -eq $true)
    {
        Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
        Remove-Item -Path $Key -Recurse
    }
}
else
{
    # Write-Host "Registry Key '$($Key)' not found."
}

# -----------------------------------------------

Write-Host "Clearing Chem4Word installation settings"

# Clear Word Add-In keys
$baseKeys = @()
$baseKeys += "HKCU:\SOFTWARE\Microsoft\Office\Word"
$baseKeys += "HKLM:\SOFTWARE\Microsoft\Office\Word"
$baseKeys += "HKLM:\SOFTWARE\WOW6432NodeMicrosoft\Office\Word"

$keyNames = @()
$keyNames += "Chemistry Add-in for Word"
$keyNames += "Chem4Word"
$keyNames += "Chem4Word V3"
$keyNames += "Chem4Word.V3"

foreach ($baseKey in $baseKeys)
{
    foreach ($keyName in $keyNames)
    {
        $targetKey = "$($baseKey)\AddIns\$($keyName)"
        $k = Get-ItemProperty -Path $targetKey -ErrorAction SilentlyContinue
        if ($k -ne $null)
        {
            Write-Host " Registry Key '$($targetKey)' found ..."
            Write-Host "  FriendlyName: $($k.'FriendlyName')"
            Write-Host "  LoadBehavior: $($k.'LoadBehavior')"
            Write-Host "  Manifest: $($k.'Manifest')"

            if ($isIntalled -eq $false -and $delete -eq $true)
            {
                Write-Host "  Deleting '$($targetKey)' ..." -ForegroundColor Cyan
                Remove-Item -Path $targetKey -Recurse
            }
        }
        else
        {
            # Write-Host "Registry Key '$($targetKey)' not found."
        }
    }
}

foreach ($baseKey in $baseKeys)
{
    foreach ($keyName in $keyNames)
    {
        $targetKey = "$($baseKey)\AddInsData\$($keyName)"
        $k = Get-ItemProperty -Path $targetKey -ErrorAction SilentlyContinue
        if ($k -ne $null)
        {
            Write-Host " Registry Key '$($targetKey)' found ..."
            Write-Host "  LoadCount: $($k.'LoadCount')"

            if ($isIntalled -eq $false -and $delete -eq $true)
            {
                Write-Host "  Deleting '$($targetKey)' ..." -ForegroundColor Cyan
                Remove-Item -Path $targetKey -Recurse
            }
        }
        else
        {
            # Write-Host "Registry Key '$($targetKey)' not found."
        }
    }
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
                if ($isIntalled -eq $false -and $delete -eq $true)
                {
                    Write-Host "  Removing $($kvp.Name)" -ForegroundColor Cyan
                    Remove-ItemProperty -Path $Key -Name $kvp.Name
                }
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
    if ($isIntalled -eq $false -and $delete -eq $true)
    {
        if (Test-Path $folder)
        {
            Write-Host "Deleting folder tree '$($folder)'"
            Get-ChildItem -Path $folder -Recurse | Remove-Item -force -recurse
            Remove-Item $folder
        }
    }
}