#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Rewrites the documents that are generated from code.

.DESCRIPTION
    Some documents are written by the code they describe, so they cannot drift from it:

        docs/EXIT-CODES.md   from ExitCodes.ToMarkdown() in Wolfgang.Barefoot.Core

    Each is committed, and a test in Wolfgang.Barefoot.Tests.Architecture (GeneratedDocumentTests)
    fails when the committed file differs from what the code generates. When that test fails, run
    this script and commit the result.

    The script builds Wolfgang.Barefoot.Core in Release, loads the assembly and writes each document
    with LF line endings and no byte-order mark.

.PARAMETER NoBuild
    Use the Release build that is already there instead of building first.

.EXAMPLE
    pwsh ./scripts/Update-GeneratedDocs.ps1
#>
[CmdletBinding()]
param(
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj'
$assembly = Join-Path $root 'src/Wolfgang.Barefoot.Core/bin/Release/net10.0/Wolfgang.Barefoot.Core.dll'

if (-not $NoBuild) {
    dotnet build $project --configuration Release --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "The build of $project failed."
    }
}

if (-not (Test-Path -LiteralPath $assembly)) {
    throw "$assembly does not exist. Run without -NoBuild."
}

# Loaded from bytes so the file is not held open and a later build can replace it.
$loaded = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($assembly))
$exitCodes = $loaded.GetType('Wolfgang.Barefoot.Core.Hosting.ExitCodes', $true)

$documents = [ordered]@{
    'docs/EXIT-CODES.md' = $exitCodes.GetMethod('ToMarkdown').Invoke($null, @())
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
foreach ($entry in $documents.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    [System.IO.File]::WriteAllText($path, $entry.Value, $utf8)
    Write-Host "Wrote $($entry.Key)"
}
