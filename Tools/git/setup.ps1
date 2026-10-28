# Configura este clone para trabalhar em equipe (rode uma vez por clone):
#   powershell -ExecutionPolicy Bypass -File Tools\git\setup.ps1
# - instala os hooks do Git LFS
# - liga o Smart Merge da Unity (UnityYAMLMerge) para cenas, prefabs e assets
# Detalhes em CONTRIBUTING.md.
$ErrorActionPreference = 'Stop'
# A raiz vem do caminho do script: a saida do git embaralha caminhos com acento.
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $root
if ($root -match '[^\x00-\x7F]') {
    Write-Warning "O caminho do clone tem acento ou caractere especial ($root). O UnityYAMLMerge nao funciona nesse caso; clone numa pasta so com letras sem acento (ex.: C:\dev\theRiseOfFreny)."
}

git lfs version | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Git LFS nao encontrado. Instale o Git for Windows (ja vem com LFS) ou https://git-lfs.com' }
git lfs install | Out-Null
git config lfs.locksverify true

$version = (Select-String -Path 'ProjectSettings\ProjectVersion.txt' -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value.Trim()

$editorRoots = @("$env:ProgramFiles\Unity\Hub\Editor")
$secondary = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
if (Test-Path $secondary) {
    $p = (Get-Content $secondary -Raw | ConvertFrom-Json)
    if ($p) { $editorRoots = @($p) + $editorRoots }
}
$merge = $null
foreach ($r in $editorRoots) {
    $candidate = Join-Path $r "$version\Editor\Data\Tools\UnityYAMLMerge.exe"
    if (Test-Path $candidate) { $merge = $candidate; break }
}
if (-not $merge) {
    Write-Warning "UnityYAMLMerge.exe da Unity $version nao encontrado. Instale essa versao pelo Unity Hub e rode este script de novo."
} else {
    $m = $merge -replace '\\', '/'
    git config merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
    git config merge.unityyamlmerge.driver "'$m' merge -h -p --force --fallback none %O %B %A %A"
    git config merge.unityyamlmerge.recursive binary
    Write-Host "Smart Merge: $merge"
}
Write-Host "Pronto. Unity esperada: $version"
