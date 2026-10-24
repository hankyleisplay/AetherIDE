$rootDir = Split-Path $PSScriptRoot -Parent
$targetDir = Get-ChildItem -Path $rootDir -Directory | Where-Object { $_.Name -like "*IDE*" -and $_.Name -notlike "*.sln*" } | Select-Object -First 1

if ($targetDir) {
    $files = Get-ChildItem -Path $targetDir.FullName -Recurse -File | Where-Object {
        $_.Extension -in @(".cs", ".xaml", ".csproj", ".json", ".md")
    }
    foreach ($f in $files) {
        $text = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
        $text = $text -replace "\r?\n", "`r`n"
        [System.IO.File]::WriteAllText($f.FullName, $text, (New-Object System.Text.UTF8Encoding($true)))
    }
    Write-Output "Successfully normalized $($files.Count) files to CRLF in $($targetDir.FullName)"
} else {
    Write-Output "Directory not found!"
}

