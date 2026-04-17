<#
.SYNOPSIS
    Erstellt ein Release: setzt die Version in der .csproj, erstellt einen PR von develop nach main,
    tagged main und erstellt ein GitHub Release mit Build-Artefakten.

.PARAMETER Version
    Die Release-Version im Format Major.Minor.Patch (z.B. 1.2.3).

.EXAMPLE
    .\Scripts\Release.ps1 -Version 1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$solutionRoot = Split-Path -Parent $PSScriptRoot
$csprojPath   = Join-Path $solutionRoot 'InvoiceSearch' 'InvoiceSearch.csproj'
$branch       = "release/$Version"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) ist nicht installiert. Bitte installieren: https://cli.github.com/"
}

if (-not (Test-Path $csprojPath)) {
    Write-Error "Projektdatei nicht gefunden: $csprojPath"
}

Push-Location $solutionRoot
try {
    $currentBranch = git rev-parse --abbrev-ref HEAD
    if ($currentBranch -ne 'develop') {
        Write-Error "Bitte zuerst auf den Branch 'develop' wechseln. Aktueller Branch: $currentBranch"
    }

    Write-Host "Aktualisiere develop..." -ForegroundColor Cyan
    git pull origin develop
    if ($LASTEXITCODE -ne 0) { Write-Error "git pull fehlgeschlagen." }

    Write-Host "Setze Version $Version in $csprojPath ..." -ForegroundColor Cyan

    [xml]$csproj = Get-Content $csprojPath -Raw
    $propertyGroup = $csproj.Project.PropertyGroup | Select-Object -First 1

    function Set-OrCreateElement {
        param(
            [System.Xml.XmlElement]$Parent,
            [System.Xml.XmlDocument]$Document,
            [string]$Name,
            [string]$Value
        )
        $node = $Parent.SelectSingleNode($Name)
        if ($null -eq $node) {
            $node = $Document.CreateElement($Name)
            $Parent.AppendChild($node) | Out-Null
        }
        $node.InnerText = $Value
    }

    Set-OrCreateElement -Parent $propertyGroup -Document $csproj -Name 'Version'         -Value $Version
    Set-OrCreateElement -Parent $propertyGroup -Document $csproj -Name 'AssemblyVersion'  -Value $Version
    Set-OrCreateElement -Parent $propertyGroup -Document $csproj -Name 'FileVersion'      -Value $Version

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $writer    = [System.IO.StreamWriter]::new($csprojPath, $false, $utf8NoBom)
    try {
        $csproj.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "Version erfolgreich gesetzt." -ForegroundColor Green

    Write-Host "Erstelle Release-Branch '$branch' ..." -ForegroundColor Cyan
    git checkout -b $branch
    if ($LASTEXITCODE -ne 0) { Write-Error "Branch '$branch' konnte nicht erstellt werden." }

    git add $csprojPath
    git commit -m "chore: set release version $Version"
    if ($LASTEXITCODE -ne 0) { Write-Error "Commit fehlgeschlagen." }

    git push -u origin $branch
    if ($LASTEXITCODE -ne 0) { Write-Error "Push fehlgeschlagen." }

    Write-Host "Erstelle Pull Request von '$branch' nach 'main' ..." -ForegroundColor Cyan
    $prUrl = gh pr create --base main --head $branch --title "Release $Version" --body "Release-Version **$Version** - automatisch erstellt durch Release.ps1."
    if ($LASTEXITCODE -ne 0) { Write-Error "PR-Erstellung fehlgeschlagen." }
    Write-Host "Pull Request erstellt: $prUrl" -ForegroundColor Green

    Write-Host "Merge Pull Request ..." -ForegroundColor Cyan
    gh pr merge $branch --merge --delete-branch
    if ($LASTEXITCODE -ne 0) { Write-Error "PR-Merge fehlgeschlagen." }
    Write-Host "Pull Request erfolgreich gemerged." -ForegroundColor Green

    Write-Host "Setze Tag 'v$Version' auf main ..." -ForegroundColor Cyan
    git fetch origin main
    git tag "v$Version" origin/main
    git push origin "v$Version"
    if ($LASTEXITCODE -ne 0) { Write-Error "Tag konnte nicht gepusht werden." }
    Write-Host "Tag 'v$Version' erfolgreich gesetzt." -ForegroundColor Green

    # --- Artefakte erstellen und GitHub Release anlegen ---
    $artifactName = "InvoiceSearch_$Version"
    $publishDir   = Join-Path $solutionRoot 'artifacts' 'publish' $artifactName
    $zipPath      = Join-Path $solutionRoot 'artifacts' "$artifactName.zip"

    Write-Host "Erstelle Release-Build ..." -ForegroundColor Cyan
    dotnet publish $csprojPath -c Release -o $publishDir --self-contained false
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish fehlgeschlagen." }

    Write-Host "Erstelle ZIP-Archiv '$artifactName.zip' ..." -ForegroundColor Cyan
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Archiv erstellt: $zipPath" -ForegroundColor Green

    Write-Host "Erstelle GitHub Release 'v$Version' mit Artefakt ..." -ForegroundColor Cyan
    gh release create "v$Version" $zipPath --title "Release $Version" --notes "Release-Version **$Version**`n`nArtefakt: ``$artifactName.zip``" --latest
    if ($LASTEXITCODE -ne 0) { Write-Error "GitHub Release konnte nicht erstellt werden." }
    Write-Host "GitHub Release 'v$Version' erfolgreich erstellt." -ForegroundColor Green

    git checkout develop
    git pull origin develop

    Write-Host "`nRelease $Version erfolgreich abgeschlossen!" -ForegroundColor Green
}
finally {
    Pop-Location
}