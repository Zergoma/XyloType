<#
.SYNOPSIS
    Builds the word packs from a XyloType database and publishes them on the "words" release of GitHub.

.DESCRIPTION
    The packs (one gzip file per language, the excluded words left out) and their catalog word-packs.json are
    the assets of the release "words": the app downloads them from a fixed address
    (https://github.com/Zergoma/XyloType/releases/download/words/...), whatever the releases of the app.
    The release is created the first time as a pre-release (never the latest one, which is the app); then its assets are replaced.
    Needs the GitHub CLI (gh), signed in.

.EXAMPLE
    .\tools\publish-word-packs.ps1

.EXAMPLE
    .\tools\publish-word-packs.ps1 -DryRun    # builds the packs only, in the temp folder
#>
param(
    [string] $Database = (Join-Path $env:LOCALAPPDATA 'User Name\de.xylocopadream.xylotype\Data\dactylo.db3'),
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
$repo = 'Zergoma/XyloType'
$tag = 'words'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path ([IO.Path]::GetTempPath()) 'XyloType-word-packs'

if (-not (Test-Path $Database)) {
    throw "Database not found: $Database"
}

$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $gh) { $gh = Join-Path $env:ProgramFiles 'GitHub CLI\gh.exe' }
if (-not $DryRun -and -not (Test-Path $gh)) {
    throw 'GitHub CLI (gh) not found: https://cli.github.com'
}

Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
dotnet run --project (Join-Path $root 'tools\XyloType.WordPackBuilder') -c Release -- --db $Database --out $out
if ($LASTEXITCODE -ne 0) { throw 'Building the word packs failed.' }

if ($DryRun) {
    Write-Host "Packs built in $out (not published)"
    return
}

# "release not found" is an answer here, not an error (Windows PowerShell turns any message of gh into one)
$ErrorActionPreference = 'Continue'
& $gh release view $tag --repo $repo *> $null
$exists = $LASTEXITCODE -eq 0
$ErrorActionPreference = 'Stop'

if (-not $exists) {
    & $gh release create $tag --repo $repo --title 'Packs de mots' --prerelease --latest=false `
        --notes "Mots prêts à l'emploi pour XyloType, téléchargés depuis l'application (Import > Mots). Mis à jour par tools/publish-word-packs.ps1."
    if ($LASTEXITCODE -ne 0) { throw 'Creating the release failed.' }
}

# the packs first, the catalog last: the app never sees a catalog pointing to a missing pack
$packs = Get-ChildItem $out -Filter 'words-*.tsv.gz'
& $gh release upload $tag $packs.FullName --repo $repo --clobber
if ($LASTEXITCODE -ne 0) { throw 'Uploading the packs failed.' }

& $gh release upload $tag (Join-Path $out 'word-packs.json') --repo $repo --clobber
if ($LASTEXITCODE -ne 0) { throw 'Uploading the catalog failed.' }

Write-Host "Published $($packs.Count) word pack(s) on https://github.com/$repo/releases/tag/$tag"
