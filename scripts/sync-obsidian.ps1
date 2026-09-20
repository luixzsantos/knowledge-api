<#
Importa conhecimento do Segundo Cerebro (vault Obsidian) para o SecondBrain.

O que faz:
  1. Le os arquivos .md de "05-STACK TECNOLOGICA" e "08-CONCEITOS FUNDAMENTAIS" no vault.
  2. Para cada arquivo, cria (ou atualiza, se ja existir) um Concept no SecondBrain com
     uma definicao curta (extraida da secao "## O que e" da nota, quando existe).
  3. Cria (ou atualiza) uma Note com o conteudo completo da nota, ja relacionada ao Concept.

Idempotente: pode rodar quantas vezes quiser, so cria duplicado nunca (casa por nome/titulo).
Requer a API do SecondBrain no ar (rode start.bat primeiro).
#>

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$ApiBase = "http://localhost:5080/api"
$VaultRoot = "$env:USERPROFILE\OneDrive\Documents\SEGUNDO CÉREBRO\SEGUNDO CÉREBRO"
$FoldersToSync = @("05-STACK TECNOLÓGICA", "08-CONCEITOS FUNDAMENTAIS")
$SkipFiles = @("Leia-me.md")

function Invoke-JsonApi {
    param([string]$Uri, [string]$Method, [hashtable]$Payload)

    $json = $Payload | ConvertTo-Json -Depth 5
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    return Invoke-RestMethod -Uri $Uri -Method $Method -Body $bytes -ContentType "application/json; charset=utf-8"
}

function Test-ApiUp {
    try {
        Invoke-RestMethod -Uri "$ApiBase/concepts" -Method Get -TimeoutSec 3 | Out-Null
        return $true
    } catch {
        return $false
    }
}

function Remove-Frontmatter([string]$Text) {
    if ($Text -match "(?s)^---\r?\n.*?\r?\n---\r?\n") {
        return $Text.Substring($Matches[0].Length)
    }
    return $Text
}

function Clean-MarkdownLine([string]$Line) {
    $Line = $Line -replace '\[\[([^\|\]]+)\|([^\]]+)\]\]', '$2'
    $Line = $Line -replace '\[\[([^\]]+)\]\]', '$1'
    $Line = $Line -replace '\*\*([^\*]+)\*\*', '$1'
    $Line = $Line -replace '`([^`]+)`', '$1'
    return $Line.Trim()
}

function Get-ShortDefinition([string]$Body) {
    $lines = $Body -split "`r?`n"
    $inSection = $false
    foreach ($line in $lines) {
        if ($line -match "^##\s+O que é") { $inSection = $true; continue }
        if ($inSection) {
            if ($line -match "^##\s") { break }
            $clean = Clean-MarkdownLine $line
            if ($clean.Length -gt 0) { return $clean }
        }
    }
    foreach ($line in $lines) {
        $clean = Clean-MarkdownLine $line
        if ($clean.Length -eq 0 -or $clean.StartsWith("#")) { continue }
        return $clean
    }
    return "Importado do Segundo Cérebro."
}

function Get-CleanBody([string]$Body) {
    $lines = $Body -split "`r?`n" | ForEach-Object {
        $l = $_ -replace '^\s*#+\s*', ''
        $l = $l -replace '^\s*[-*]\s+', '• '
        Clean-MarkdownLine $l
    }
    return (($lines -join "`n").Trim())
}

if (-not (Test-ApiUp)) {
    Write-Host "[ERRO] A API do SecondBrain nao esta respondendo em $ApiBase." -ForegroundColor Red
    Write-Host "Rode start.bat primeiro e tente de novo." -ForegroundColor Red
    exit 1
}

$existingConcepts = Invoke-RestMethod -Uri "$ApiBase/concepts" -Method Get
$conceptByName = @{}
foreach ($c in $existingConcepts) { $conceptByName[$c.name.ToLower()] = $c }

$existingNotes = Invoke-RestMethod -Uri "$ApiBase/notes" -Method Get
$noteByTitle = @{}
foreach ($n in $existingNotes) { $noteByTitle[$n.title.ToLower()] = $n }

$created = 0
$updated = 0
$failed = 0

foreach ($folder in $FoldersToSync) {
    $path = Join-Path $VaultRoot $folder
    if (-not (Test-Path $path)) {
        Write-Host "[AVISO] Pasta nao encontrada, pulando: $path" -ForegroundColor Yellow
        continue
    }

    Get-ChildItem -Path $path -Filter "*.md" | Where-Object { $SkipFiles -notcontains $_.Name } | ForEach-Object {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        try {
            $raw = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
            $body = Remove-Frontmatter $raw
            $shortDefinition = Get-ShortDefinition $body
            $cleanBody = Get-CleanBody $body

            $key = $name.ToLower()
            if ($conceptByName.ContainsKey($key)) {
                $conceptId = $conceptByName[$key].id
                Invoke-JsonApi -Uri "$ApiBase/concepts/$conceptId" -Method Put -Payload @{ name = $name; description = $shortDefinition } | Out-Null
                $script:updated++
            } else {
                $resp = Invoke-JsonApi -Uri "$ApiBase/concepts" -Method Post -Payload @{ name = $name; description = $shortDefinition }
                $conceptId = $resp.id
                $script:created++
            }

            $noteTitle = "$name (Segundo Cérebro)"
            $noteKey = $noteTitle.ToLower()
            if ($noteByTitle.ContainsKey($noteKey)) {
                $noteId = $noteByTitle[$noteKey].id
                Invoke-JsonApi -Uri "$ApiBase/notes/$noteId" -Method Put -Payload @{ title = $noteTitle; content = $cleanBody } | Out-Null
            } else {
                $noteResp = Invoke-JsonApi -Uri "$ApiBase/notes" -Method Post -Payload @{ title = $noteTitle; content = $cleanBody }
                Invoke-RestMethod -Uri "$ApiBase/concepts/$conceptId/notes/$($noteResp.id)" -Method Post | Out-Null
            }

            Write-Host "OK  $name" -ForegroundColor Green
        } catch {
            Write-Host "FALHOU  $name -- $_" -ForegroundColor Red
            $script:failed++
        }
    }
}

Write-Host ""
Write-Host "Sync concluido: $created novo(s), $updated atualizado(s), $failed falha(s)."
