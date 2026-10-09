<#
.SYNOPSIS
    Publishes the exercise packs of packs/exercises on the "exercises" release of GitHub.

.DESCRIPTION
    The packs (one JSON file each, written by hand in the repository) and their catalog exercise-packs.json are
    the assets of the release "exercises": the app downloads them from a fixed address
    (https://github.com/Zergoma/XyloType/releases/download/exercises/...), whatever the releases of the app.
    The packs are checked first by their tests (ExercisePackTests); the catalog is built from them.
    The release is created the first time as a pre-release (never the latest one, which is the app); then its assets are replaced.
    Change the "version" of a pack when its content changes: the app offers the update to the users who imported it.
    Needs the GitHub CLI (gh), signed in.

.EXAMPLE
    .\tools\publish-exercise-packs.ps1

.EXAMPLE
    .\tools\publish-exercise-packs.ps1 -DryRun    # checks the packs and builds the catalog only, in the temp folder
#>
param(
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
$repo = 'Zergoma/XyloType'
$tag = 'exercises'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'packs\exercises'
$out = Join-Path ([IO.Path]::GetTempPath()) 'XyloType-exercise-packs'

$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $gh) { $gh = Join-Path $env:ProgramFiles 'GitHub CLI\gh.exe' }
if (-not $DryRun -and -not (Test-Path $gh)) {
    throw 'GitHub CLI (gh) not found: https://cli.github.com'
}

# an invalid pack is never published
dotnet test (Join-Path $root 'XyloType.Tests') --filter 'FullyQualifiedName~ExercisePackTests' -v q
if ($LASTEXITCODE -ne 0) { throw 'The exercise packs are not valid (see the tests above).' }

Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $out | Out-Null

# the catalog: what the app shows before downloading, with the checksum of each file
$packs = @()
# shown in the order of the levels, from the first keys to whole texts
$levels = @('Débutant', 'Intermédiaire', 'Confirmé')
$rank = { $i = $levels.IndexOf($_.level); if ($i -lt 0) { $levels.Count } else { $i } }

foreach ($file in Get-ChildItem $source -Filter '*.json' | Sort-Object Name) {
    Copy-Item $file.FullName $out
    $pack = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8) | ConvertFrom-Json
    $count = ($pack.sections | ForEach-Object { $_.exercises.Count } | Measure-Object -Sum).Sum

    $packs += [ordered]@{
        id            = $pack.id
        title         = $pack.title
        level         = $pack.level
        description   = $pack.description
        layout        = $pack.layout
        version       = $pack.version
        fileName      = $file.Name
        sha256        = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        size          = $file.Length
        exerciseCount = $count
    }
}

$packs = @($packs | Sort-Object -Property @{ Expression = $rank }, @{ Expression = { $_.id } })
$catalog = [ordered]@{ format = 1; packs = $packs } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $out 'exercise-packs.json'), $catalog, (New-Object Text.UTF8Encoding $false))

if ($DryRun) {
    Write-Host "$($packs.Count) pack(s) and their catalog in $out (not published)"
    return
}

# "release not found" is an answer here, not an error (Windows PowerShell turns any message of gh into one)
$ErrorActionPreference = 'Continue'
& $gh release view $tag --repo $repo *> $null
$exists = $LASTEXITCODE -eq 0
$ErrorActionPreference = 'Stop'

if (-not $exists) {
    & $gh release create $tag --repo $repo --title "Packs d'exercices" --prerelease --latest=false `
        --notes "Exercices prêts à l'emploi pour XyloType, téléchargés depuis l'application (Exercices > Packs). Mis à jour par tools/publish-exercise-packs.ps1 depuis packs/exercises."
    if ($LASTEXITCODE -ne 0) { throw 'Creating the release failed.' }
}

# the packs first, the catalog last: the app never sees a catalog pointing to a missing pack
$files = Get-ChildItem $out -Filter '*.json' | Where-Object Name -ne 'exercise-packs.json'
& $gh release upload $tag $files.FullName --repo $repo --clobber
if ($LASTEXITCODE -ne 0) { throw 'Uploading the packs failed.' }

& $gh release upload $tag (Join-Path $out 'exercise-packs.json') --repo $repo --clobber
if ($LASTEXITCODE -ne 0) { throw 'Uploading the catalog failed.' }

Write-Host "Published $($files.Count) exercise pack(s) on https://github.com/$repo/releases/tag/$tag"
