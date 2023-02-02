# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

cls

$obsolete = @()
$obsolete += "Microsoft.Azure.KeyVault.Core"
$obsolete += "Microsoft.Azure.Services.AppAuthentication"
$obsolete += "Microsoft.IdentityModel.Clients.ActiveDirectory"
$obsolete += "Microsoft.IdentityModel.JsonWebTokens"
$obsolete += "Microsoft.IdentityModel.Logging"
$obsolete += "Microsoft.IdentityModel.Tokens"
$obsolete += "Microsoft.Rest.ClientRuntime"
$obsolete += "Microsoft.Rest.ClientRuntime.Azure"
$obsolete += "System.IdentityModel.Tokens.Jwt"
$obsolete += "WindowsAzure.ServiceBus"
$obsolete += "System.Net.Http"


$targets = Get-ChildItem ..\. -include *.csproj -Recurse
$count = 0

foreach ($target in $targets)
{
    Write-Host "Folder $($target.DirectoryName)" -ForegroundColor Cyan

    $appConfig = "$($target.DirectoryName)\app.config"
    $foundAppConfig = Test-Path -path "$($appConfig)"

    if ($foundAppConfig)
    {
        Write-Host "  Found $($appConfig)" -ForegroundColor Yellow
        $flaws = 0
        foreach ($word in $obsolete)
        {
            $exists = Select-String -Path $appConfig -Pattern $word
            if ($exists -ne $null)
            {
                Write-Host "    app.config is flawed because it contains '$($word)'" -ForegroundColor Red
                $count++
                $flaws++
            }
        }
        if ($flaws -gt 0)
        {
            Write-Host "  $($flaws) flaws found in '$($appConfig)'" -ForegroundColor Yellow
        }
    }
}

Write-Host "$($count) flaws found"
Exit $count
