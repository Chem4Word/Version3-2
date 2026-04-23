
cls

$targets = Get-ChildItem . -include bin,obj -Recurse
foreach ($target in $targets)
{
    if (Test-Path $target.Fullname -PathType Container)
    {
        if (-not ($target.Fullname.Contains("Tools")))
        {
            Write-Host "Purging $($target.Fullname)" -ForegroundColor Cyan
            Remove-Item $target.Fullname -Force -Recurse
        }
    }
}

$targets = Get-ChildItem . -include .vs -Hidden -Recurse
foreach ($target in $targets)
{
    if (Test-Path $target.Fullname -PathType Container)
    {
        if (-not ($target.Fullname.Contains("Tools")))
        {
            Write-Host "Purging $($target.Fullname)" -ForegroundColor Cyan
            Remove-Item $target.Fullname -Force -Recurse
        }
    }
}