#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Generates the repository-root THIRD-PARTY-NOTICES.md, or checks that the committed file is current.

.DESCRIPTION
    Writes one Markdown file that lists every NuGet package distributed with Barefoot: the full
    transitive closure of every project under src/, merged into one table of package, version(s),
    license, copyright, project URL and the projects that use it. The file is committed, so the
    attribution travels with the source and a reviewer sees in the diff what a dependency change
    adds.

    How this differs from scripts/third-party-notices.ps1
    -----------------------------------------------------
    scripts/third-party-notices.ps1 is the template's script. It writes one file PER PROJECT to
    <project>/obj/THIRD-PARTY-NOTICES.md at build time, so that file can be packed into a NuGet
    package. Those files are never committed.

    This script writes ONE file for the whole repository, at the root, and that file IS committed.
    Barefoot is an application: its hosts are published as a whole, not packed one project at a
    time, so the attribution that matters is the merged list.

    This script does not repeat the template script's logic. It calls it once per project
    (-Project, -OutputPath, -Strict) and merges the tables it writes. The template script therefore
    stays the only place that knows:
      - how nuget-license is called (the tool pinned in .config/dotnet-tools.json);
      - which policy files apply (.github/license-audit/*.json, the files license-audit.yaml uses);
      - which packages are build-time only and are left out (PrivateAssets="all", SourceLink).

    -Strict is passed on purpose. When nuget-license rejects a project's closure, the template
    script by default warns and skips the project. A committed notices file with a project missing
    would be wrong without saying so, so here the run fails instead.

    The output is deterministic: rows and versions are sorted by ordinal comparison, there is no
    timestamp, line endings are LF, the encoding is UTF-8 without a byte-order mark. Two runs on
    the same restored tree give the same bytes.

    When src/ holds no project, the file says that no third-party packages are distributed yet.
    Nothing is restored and no tool is run in that case, and -Check works the same way.

    Requirements when src/ has projects: the .NET SDK, network access for the restore (or
    -NoRestore on an already restored tree), and scripts/third-party-notices.ps1.

.PARAMETER Root
    Repository root. Default: the parent of this script's directory.

.PARAMETER OutputPath
    The file to write or check. Default: <Root>/THIRD-PARTY-NOTICES.md.

.PARAMETER PolicyDirectory
    Passed to scripts/third-party-notices.ps1. Relative to -Root. Default: .github/license-audit.

.PARAMETER ExcludeProject
    Names of projects under src/ that are not distributed (the file name without .csproj). They are
    left out and named in the output. Default: none, every project under src/ is distributed.

.PARAMETER NoRestore
    Do not run `dotnet restore` for each project first. Use it when the tree is already restored.

.PARAMETER Check
    Write nothing. Exit 1 when the file is missing or differs from what would be generated, 0 when
    it is current. Line endings are compared as LF, so a CRLF checkout does not fail the check.

.EXAMPLE
    pwsh ./scripts/Generate-ThirdPartyNotices.ps1

    Regenerates THIRD-PARTY-NOTICES.md. Commit the result.

.EXAMPLE
    pwsh ./scripts/Generate-ThirdPartyNotices.ps1 -Check

    For CI and the release skill: fails when the committed file is out of date.
#>
[CmdletBinding()]
param
(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),

    [string]$OutputPath = '',

    [string]$PolicyDirectory = '.github/license-audit',

    [string[]]$ExcludeProject = @(),

    [switch]$NoRestore,

    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:PerProjectScript = 'scripts/third-party-notices.ps1'
$script:ExpectedHeader = @('Package', 'Version(s)', 'License', 'Copyright', 'Project')
$script:NoDependenciesSentence = 'This package has no third-party NuGet dependencies.'



function Split-TableRow
{
    <#
    .SYNOPSIS
        Splits one Markdown table row into its cells. A pipe escaped as \| stays inside its cell.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Line
    )

    $inner = $Line.Trim()
    if ($inner.StartsWith('|'))
    {
        $inner = $inner.Substring(1)
    }
    if ($inner.EndsWith('|') -and -not $inner.EndsWith('\|'))
    {
        $inner = $inner.Substring(0, $inner.Length - 1)
    }
    return [string[]]@([regex]::Split($inner, '(?<!\\)\|') | ForEach-Object { $_.Trim() })
}



function Read-ProjectNotices
{
    <#
    .SYNOPSIS
        Reads the table scripts/third-party-notices.ps1 wrote for one project.
    .DESCRIPTION
        Fails when the file has neither the expected table header nor the "no dependencies"
        sentence: that means the template script's format changed and this script must follow.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject[]])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Path
    )

    $lines = @([System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Split("`n"))
    $headerIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++)
    {
        if ($lines[$i].StartsWith('|'))
        {
            $headerIndex = $i
            break
        }
    }

    if ($headerIndex -lt 0)
    {
        if ($lines -contains $script:NoDependenciesSentence)
        {
            return [pscustomobject[]]@()
        }
        throw "$Path has no package table and does not say that there are no dependencies. The format written by $($script:PerProjectScript) has changed; update Read-ProjectNotices."
    }

    $header = @(Split-TableRow -Line $lines[$headerIndex])
    if (($header -join '|') -cne ($script:ExpectedHeader -join '|'))
    {
        throw "$Path has the table header '$($header -join ' | ')', expected '$($script:ExpectedHeader -join ' | ')'. The format written by $($script:PerProjectScript) has changed; update Read-ProjectNotices."
    }

    $rows = [System.Collections.Generic.List[pscustomobject]]::new()
    for ($i = $headerIndex + 2; $i -lt $lines.Count; $i++)
    {
        if (-not $lines[$i].StartsWith('|'))
        {
            break
        }
        $cells = @(Split-TableRow -Line $lines[$i])
        if ($cells.Count -ne $script:ExpectedHeader.Count)
        {
            throw "${Path}: line $($i + 1) has $($cells.Count) cells, expected $($script:ExpectedHeader.Count): $($lines[$i])"
        }
        $rows.Add([pscustomobject]@{
                Package   = $cells[0]
                Versions  = [string[]]@($cells[1].Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
                License   = $cells[2]
                Copyright = $cells[3]
                Url       = $cells[4]
            })
    }
    return [pscustomobject[]]$rows.ToArray()
}



function Merge-ProjectNotices
{
    <#
    .SYNOPSIS
        Merges the rows of every project into one row per (package, license).
    .PARAMETER ProjectRows
        Ordered dictionary: project name -> the rows Read-ProjectNotices returned for it.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject[]])]
    param
    (
        [Parameter(Mandatory)]
        [System.Collections.Specialized.OrderedDictionary]$ProjectRows
    )

    $merged = [System.Collections.Generic.SortedDictionary[string, pscustomobject]]::new([System.StringComparer]::Ordinal)
    foreach ($projectName in $ProjectRows.Keys)
    {
        foreach ($row in @($ProjectRows[$projectName]))
        {
            # Package ids are case-insensitive on NuGet; the lower-case key keeps the order stable.
            $key = "$($row.Package.ToLowerInvariant())|$($row.License)"
            if (-not $merged.ContainsKey($key))
            {
                $merged[$key] = [pscustomobject]@{
                    Package   = $row.Package
                    Versions  = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
                    License   = $row.License
                    Copyright = $row.Copyright
                    Url       = $row.Url
                    Projects  = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
                }
            }
            $entry = $merged[$key]
            foreach ($version in $row.Versions)
            {
                [void]$entry.Versions.Add($version)
            }
            [void]$entry.Projects.Add($projectName)
            # Projects are visited in sorted order, so "first non-empty wins" is deterministic.
            if (-not $entry.Copyright -and $row.Copyright) { $entry.Copyright = $row.Copyright }
            if (-not $entry.Url -and $row.Url) { $entry.Url = $row.Url }
        }
    }
    return [pscustomobject[]]@($merged.Values)
}



function Format-Notices
{
    <#
    .SYNOPSIS
        Renders the file content: LF line endings, one trailing newline, no timestamp.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyCollection()]
        [string[]]$ProjectNames = @(),

        [AllowEmptyCollection()]
        [string[]]$ExcludedNames = @(),

        [AllowEmptyCollection()]
        [pscustomobject[]]$Rows = @()
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# Third-party notices')
    $lines.Add('')
    $lines.Add('Generated by `scripts/Generate-ThirdPartyNotices.ps1`. Do not edit this file by hand.')
    $lines.Add('Run the script after any change to a package reference and commit the result.')
    $lines.Add('`scripts/Generate-ThirdPartyNotices.ps1 -Check` fails when this file is out of date.')
    $lines.Add('')

    if ($ProjectNames.Count -eq 0)
    {
        $lines.Add('No project exists under `src/` yet, so no third-party packages are distributed yet.')
    }
    else
    {
        $lines.Add('Barefoot is distributed with the NuGet packages below: the full transitive closure of')
        $lines.Add('every project under `src/`. Each package is distributed under its own license.')
        $lines.Add('Build-time-only packages (analyzers, SourceLink) are not distributed and are not listed.')
        $lines.Add('')
        $covered = @($ProjectNames | ForEach-Object { '`' + $_ + '`' }) -join ', '
        $lines.Add("Projects covered: $covered.")
        if ($ExcludedNames.Count -gt 0)
        {
            $lines.Add('')
            $notCovered = @($ExcludedNames | ForEach-Object { '`' + $_ + '`' }) -join ', '
            $lines.Add("Projects not distributed and not covered: $notCovered.")
        }
        $lines.Add('')
        if ($Rows.Count -eq 0)
        {
            $lines.Add('These projects have no third-party NuGet dependencies.')
        }
        else
        {
            $lines.Add('| Package | Version(s) | License | Copyright | Project URL | Used by |')
            $lines.Add('|---|---|---|---|---|---|')
            foreach ($row in $Rows)
            {
                $lines.Add("| $($row.Package) | $($row.Versions -join ', ') | $($row.License) | $($row.Copyright) | $($row.Url) | $($row.Projects -join ', ') |")
            }
        }
    }

    return (($lines -join "`n") + "`n")
}



function Invoke-PerProjectScript
{
    <#
    .SYNOPSIS
        Runs scripts/third-party-notices.ps1 for one project in a child process and fails on a non-zero exit.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string]$Root,

        [Parameter(Mandatory)]
        [string]$RelativeProject,

        [Parameter(Mandatory)]
        [string]$OutputFile,

        [Parameter(Mandatory)]
        [string]$PolicyDirectory
    )

    $pwshPath = (Get-Process -Id $PID).Path
    $scriptPath = Join-Path $Root $script:PerProjectScript

    # The template script resolves src/ and the policy directory against the current directory.
    Push-Location -LiteralPath $Root
    try
    {
        $log = & $pwshPath -NoProfile -NonInteractive -File $scriptPath `
            -Project $RelativeProject -OutputPath $OutputFile -PolicyDirectory $PolicyDirectory -Strict 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally
    {
        Pop-Location
    }

    if ($exitCode -ne 0)
    {
        Write-Host (@($log) -join "`n")
        throw "$($script:PerProjectScript) failed for $RelativeProject (exit $exitCode). The license gate is license-audit.yaml; fix the finding there first."
    }
    if (-not (Test-Path -LiteralPath $OutputFile))
    {
        Write-Host (@($log) -join "`n")
        throw "$($script:PerProjectScript) reported success for $RelativeProject but wrote no file."
    }
}



function Get-NoticesContent
{
    <#
    .SYNOPSIS
        Builds the content of the notices file for the repository at -Root.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Root,

        [Parameter(Mandatory)]
        [string]$PolicyDirectory,

        [AllowEmptyCollection()]
        [string[]]$ExcludeProject = @(),

        [switch]$NoRestore
    )

    $sourceDirectory = Join-Path $Root 'src'
    $found = @()
    if (Test-Path -LiteralPath $sourceDirectory)
    {
        $found = @(Get-ChildItem -LiteralPath $sourceDirectory -Recurse -File -Filter '*.csproj')
    }

    $projects = [System.Collections.Generic.SortedDictionary[string, string]]::new([System.StringComparer]::Ordinal)
    $excluded = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $found)
    {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        if ($ExcludeProject -contains $name)
        {
            [void]$excluded.Add($name)
            continue
        }
        if ($projects.ContainsKey($name))
        {
            throw "Two projects under src/ are named '$name'. Project names must be unique."
        }
        $projects[$name] = [System.IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
    }

    $unknown = @($ExcludeProject | Where-Object { -not $excluded.Contains($_) })
    if ($unknown.Count -gt 0)
    {
        throw "-ExcludeProject names no project under src/: $($unknown -join ', ')"
    }

    if ($projects.Count -eq 0)
    {
        return Format-Notices -ProjectNames @() -ExcludedNames @($excluded) -Rows @()
    }

    if (-not (Test-Path -LiteralPath (Join-Path $Root $script:PerProjectScript)))
    {
        throw "$($script:PerProjectScript) not found under $Root. This script calls it for each project."
    }

    $temporary = Join-Path ([System.IO.Path]::GetTempPath()) ("third-party-notices-" + [System.Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporary | Out-Null
    try
    {
        $projectRows = [ordered]@{}
        foreach ($name in $projects.Keys)
        {
            $relative = $projects[$name]
            Write-Host "== $name =="
            if (-not $NoRestore)
            {
                $restoreLog = & dotnet restore (Join-Path $Root $relative) 2>&1
                if ($LASTEXITCODE -ne 0)
                {
                    Write-Host (@($restoreLog) -join "`n")
                    throw "dotnet restore failed for $relative (exit $LASTEXITCODE)."
                }
            }
            $outputFile = Join-Path $temporary "$name.md"
            Invoke-PerProjectScript -Root $Root -RelativeProject $relative -OutputFile $outputFile -PolicyDirectory $PolicyDirectory
            $projectRows[$name] = @(Read-ProjectNotices -Path $outputFile)
            Write-Host "   $(@($projectRows[$name]).Count) package(s)"
        }
        $rows = @(Merge-ProjectNotices -ProjectRows $projectRows)
        return Format-Notices -ProjectNames @($projects.Keys) -ExcludedNames @($excluded) -Rows $rows
    }
    finally
    {
        Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue
    }
}



# Skipped when the file is dot-sourced, so the functions can be loaded alone.
if ($MyInvocation.InvocationName -eq '.')
{
    return
}

$Root = (Resolve-Path -LiteralPath $Root).Path
if (-not $OutputPath)
{
    $OutputPath = Join-Path $Root 'THIRD-PARTY-NOTICES.md'
}

$content = Get-NoticesContent -Root $Root -PolicyDirectory $PolicyDirectory -ExcludeProject $ExcludeProject -NoRestore:$NoRestore

if ($Check)
{
    if (-not (Test-Path -LiteralPath $OutputPath))
    {
        Write-Host "::error::$OutputPath does not exist. Run scripts/Generate-ThirdPartyNotices.ps1 and commit the result."
        exit 1
    }
    $committed = [System.IO.File]::ReadAllText($OutputPath).Replace("`r`n", "`n")
    if ($committed -cne $content)
    {
        Write-Host "::error file=THIRD-PARTY-NOTICES.md::THIRD-PARTY-NOTICES.md is out of date. Run scripts/Generate-ThirdPartyNotices.ps1 and commit the result."
        exit 1
    }
    Write-Host "$OutputPath is up to date."
    exit 0
}

[System.IO.File]::WriteAllText($OutputPath, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutputPath"
exit 0
