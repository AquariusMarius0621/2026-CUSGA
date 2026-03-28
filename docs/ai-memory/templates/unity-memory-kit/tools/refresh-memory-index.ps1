param(
    [string]$ConfigPath = 'docs/ai-memory/memory-index.paths.txt',
    [string]$MainDocPath = 'docs/ai-memory/td-memory-main.md',
    [switch]$UpdateMainDoc,
    [switch]$FailOnMissing
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ProjectRoot {
    return (Resolve-Path '.').Path
}

function Get-AbsolutePath([string]$ProjectRoot, [string]$RelativePath) {
    return Join-Path $ProjectRoot $RelativePath
}

function Get-FileLineCount([string]$AbsolutePath) {
    if (-not (Test-Path $AbsolutePath)) {
        return $null
    }

    return (Get-Content -LiteralPath $AbsolutePath -Encoding UTF8 | Measure-Object -Line).Lines
}

function Get-IndexEntries([string]$ProjectRoot, [string]$ConfigPath, [bool]$FailOnMissing) {
    $absoluteConfigPath = Get-AbsolutePath $ProjectRoot $ConfigPath
    if (-not (Test-Path $absoluteConfigPath)) {
        throw "Index config not found: $ConfigPath"
    }

    $entries = New-Object System.Collections.Generic.List[object]
    foreach ($rawLine in Get-Content -LiteralPath $absoluteConfigPath -Encoding UTF8) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) {
            continue
        }

        $parts = $line.Split('|', 2)
        if ($parts.Count -ne 2) {
            throw "Invalid config line: $line"
        }

        $relativePath = $parts[0].Trim()
        $role = $parts[1].Trim()
        $absolutePath = Get-AbsolutePath $ProjectRoot $relativePath
        $lineCount = Get-FileLineCount $absolutePath

        if ($null -eq $lineCount) {
            if ($FailOnMissing) {
                throw "Indexed file missing: $relativePath"
            }

            $lineCountDisplay = 'MISSING'
        }
        else {
            $lineCountDisplay = [string]$lineCount
        }

        $entries.Add([pscustomobject]@{
            Path = $relativePath
            Lines = $lineCountDisplay
            Role = $role
        })
    }

    return $entries
}

function ConvertTo-MarkdownTable($Entries) {
    $rows = New-Object System.Collections.Generic.List[string]
    $rows.Add('| Path | Lines | Role |')
    $rows.Add('| --- | ---: | --- |')

    foreach ($entry in $Entries) {
        $rows.Add("| ``$($entry.Path)`` | $($entry.Lines) | $($entry.Role) |".Replace('``','`'))
    }

    return ($rows -join [Environment]::NewLine)
}

function Update-MainDoc([string]$ProjectRoot, [string]$MainDocPath, [string]$MarkdownTable) {
    $absoluteMainDocPath = Get-AbsolutePath $ProjectRoot $MainDocPath
    if (-not (Test-Path $absoluteMainDocPath)) {
        throw "Main memory doc not found: $MainDocPath"
    }

    $content = [System.IO.File]::ReadAllText($absoluteMainDocPath, [System.Text.Encoding]::UTF8)
    $startMarker = '<!-- MEMORY_INDEX:START -->'
    $endMarker = '<!-- MEMORY_INDEX:END -->'

    $startIndex = $content.IndexOf($startMarker)
    $endIndex = $content.IndexOf($endMarker)
    if ($startIndex -lt 0 -or $endIndex -lt 0 -or $endIndex -le $startIndex) {
        throw 'Main memory doc is missing MEMORY_INDEX markers.'
    }

    $before = $content.Substring(0, $startIndex + $startMarker.Length)
    $after = $content.Substring($endIndex)
    $newContent = $before + [Environment]::NewLine + $MarkdownTable + [Environment]::NewLine + $after
    [System.IO.File]::WriteAllText($absoluteMainDocPath, $newContent, [System.Text.Encoding]::UTF8)
}

$projectRoot = Get-ProjectRoot
$entries = Get-IndexEntries -ProjectRoot $projectRoot -ConfigPath $ConfigPath -FailOnMissing:$FailOnMissing
$table = ConvertTo-MarkdownTable -Entries $entries

if ($UpdateMainDoc) {
    Update-MainDoc -ProjectRoot $projectRoot -MainDocPath $MainDocPath -MarkdownTable $table
    Write-Output "Updated memory index in $MainDocPath"
}
else {
    Write-Output $table
}