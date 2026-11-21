#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Turns the outcome of a scheduled workflow into exactly one GitHub issue per failure.

.DESCRIPTION
    One issue exists per (workflow, failure signature). The issue is found again by a hidden HTML
    marker in its body:

        <!-- automated-failure: <workflow>|<signature> -->

    A failed outcome:
      - no open issue carries the marker: creates one, with the failing output, the signature, a
        suggested fix and the run link, labelled `automated`, `scheduled` and any -Labels given;
      - an open issue carries the marker: adds a comment with the run link. No second issue.

    A succeeded outcome closes every open issue that carries this workflow's marker, whatever its
    signature, with a comment that names the run.

    A cancelled or skipped job result does nothing: it proves neither a failure nor a fix.

    -Summary creates or updates one issue that lists the automated issues open longer than
    -OlderThanDays. Issues labelled `critical` and open longer than two days are listed first and
    the title is prefixed "HIGH PRIORITY". The summary closes itself when nothing is listed.

    The signature decides how many issues one workflow can have open:
      - -Signature given: that text, reduced to lower-case letters, digits, '.', '_' and '-';
      - no -Signature but -BodyPath given: "h-" plus 12 hex digits of the SHA-256 of the failing
        output after timestamps, identifiers and numbers are removed, so two runs that fail the
        same way share one issue;
      - neither: "default", which means one issue per workflow.

    The script only reads and writes issues and labels. It never edits a workflow file or any
    other protected path: the issue is the prompt for a person to fix the cause through a normal
    pull request.

    Requirements: PowerShell 7 and an authenticated `gh` (in a workflow, GH_TOKEN with
    `issues: write`). There are no other dependencies.

    The functions can be loaded without running anything by dot-sourcing the file:
        . ./scripts/report-failure.ps1
    scripts/report-failure.Tests.ps1 does that.

.PARAMETER Repository
    owner/name. Default: the GITHUB_REPOSITORY environment variable.

.PARAMETER Workflow
    A short, stable key for the reporting workflow or job, for example `codeql` or `stryker`. It is
    part of the marker. Use a different key for each job that reports on its own, because a
    success closes every issue that carries the key.

.PARAMETER Outcome
    `failed` or `succeeded`. Give this or -Result.

.PARAMETER Result
    A GitHub Actions job result: `failure`, `success`, `cancelled` or `skipped`. A workflow passes
    `needs.<job>.result` here unchanged. `failure` means failed, `success` means succeeded, the
    other two do nothing.

.PARAMETER Signature
    Names the kind of failure. See the description for the default.

.PARAMETER Title
    Issue title, without the workflow prefix. Default: "Scheduled check failed (<signature>)".

.PARAMETER BodyPath
    A file holding the failing output. It is put in the issue body, cut at 60 000 characters.

.PARAMETER SuggestedFix
    One or two sentences for the "Suggested fix" section.

.PARAMETER Labels
    Labels to add besides `automated` and `scheduled`. A label that does not exist is created.

.PARAMETER Summary
    Create, update or close the summary issue instead of reporting an outcome.

.PARAMETER OlderThanDays
    -Summary only: the age, in days, from which an automated issue is listed. Default 7.

.PARAMETER RunUrl
    Link to the run. Default: built from GITHUB_SERVER_URL, GITHUB_REPOSITORY and GITHUB_RUN_ID.

.PARAMETER DryRun
    Read issues and labels, print what would be written, and write nothing.

.EXAMPLE
    pwsh ./scripts/report-failure.ps1 -Workflow codeql -Result $env:RESULT -Labels security

    In a job that runs after the checked job: opens or comments on the `codeql` issue when the
    result is `failure`, closes it when the result is `success`.

.EXAMPLE
    pwsh ./scripts/report-failure.ps1 -Repository Chris-Wolfgang/barefoot -Workflow semiweekly-job `
        -Outcome failed -Signature exit-5 -Title 'bf-scraper stopped with exit code 5' `
        -BodyPath run.log -SuggestedFix 'Update the side the message names, then run again.'

    From the home machine: one issue per exit code of the semiweekly job.

.EXAMPLE
    pwsh ./scripts/report-failure.ps1 -Repository Chris-Wolfgang/barefoot -Summary -OlderThanDays 7
#>
[CmdletBinding()]
param
(
    [string]$Repository = $env:GITHUB_REPOSITORY,

    [string]$Workflow = '',

    [ValidateSet('', 'failed', 'succeeded')]
    [string]$Outcome = '',

    [ValidateSet('', 'failure', 'success', 'cancelled', 'skipped')]
    [string]$Result = '',

    [string]$Signature = '',

    [string]$Title = '',

    [string]$BodyPath = '',

    [string]$SuggestedFix = '',

    [string[]]$Labels = @(),

    [switch]$Summary,

    [ValidateRange(1, 3650)]
    [int]$OlderThanDays = 7,

    [string]$RunUrl = '',

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:BaseLabels = @('automated', 'scheduled')
$script:LabelColor = 'bfd4f2'
$script:MaxBodyChars = 60000
$script:SummaryMarker = '<!-- automated-summary -->'
$script:CriticalAfterDays = 2



# ---------------------------------------------------------------------------------------------
# Pure functions: no gh, no file system, no clock. report-failure.Tests.ps1 covers these.
# ---------------------------------------------------------------------------------------------

function ConvertTo-SignatureSlug
{
    <#
    .SYNOPSIS
        Reduces a text to a marker-safe key: lower case, [a-z0-9._-], no repeated '-', at most
        64 characters. An empty result becomes "default".
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyString()]
        [string]$Text
    )

    $slug = (($Text.Trim().ToLowerInvariant() -replace '[^a-z0-9._-]+', '-') -replace '-{2,}', '-').Trim('-')
    if ($slug.Length -gt 64)
    {
        $slug = $slug.Substring(0, 64).TrimEnd('-')
    }
    if (-not $slug)
    {
        return 'default'
    }
    return $slug
}



function Get-NormalizedFailureText
{
    <#
    .SYNOPSIS
        Removes what changes from run to run (colour codes, timestamps, identifiers, numbers,
        spacing, letter case) so that two runs failing the same way give the same text.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyString()]
        [string]$Text
    )

    $value = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $value = $value -replace '\x1b\[[0-9;]*[A-Za-z]', ''
    $value = $value -replace '\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?', '<time>'
    $value = $value -replace '(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b', '<id>'
    # A hexadecimal run of 7 to 64 characters with at least one digit: a commit SHA or a hash.
    $value = $value -replace '(?i)\b(?=[0-9a-f]*\d)[0-9a-f]{7,64}\b', '<id>'
    $value = $value -replace '\d+', '#'

    $lines = foreach ($line in $value.Split("`n"))
    {
        $trimmed = ($line -replace '\s+', ' ').Trim()
        if ($trimmed)
        {
            $trimmed.ToLowerInvariant()
        }
    }
    return (@($lines) -join "`n")
}



function Get-FailureSignature
{
    <#
    .SYNOPSIS
        The signature of a failure: the explicit one, else a hash of the failing output, else "default".
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyString()]
        [string]$Signature = '',

        [AllowEmptyString()]
        [string]$Text = ''
    )

    if ($Signature.Trim())
    {
        return ConvertTo-SignatureSlug -Text $Signature
    }

    $normalized = Get-NormalizedFailureText -Text $Text
    if (-not $normalized)
    {
        return 'default'
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $hex = [System.Convert]::ToHexString($hash).ToLowerInvariant()
    return 'h-' + $hex.Substring(0, 12)
}



function Get-WorkflowMarkerPrefix
{
    <#
    .SYNOPSIS
        The start of every marker of one workflow; a success closes the issues that contain it.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow
    )

    return "<!-- automated-failure: $(ConvertTo-SignatureSlug -Text $Workflow)|"
}



function Get-FailureMarker
{
    <#
    .SYNOPSIS
        The hidden HTML comment that identifies the issue of one (workflow, signature).
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Signature
    )

    return "$(Get-WorkflowMarkerPrefix -Workflow $Workflow)$(ConvertTo-SignatureSlug -Text $Signature) -->"
}



function ConvertTo-ReportOutcome
{
    <#
    .SYNOPSIS
        Maps -Outcome or a GitHub job result to `failed`, `succeeded` or `none`.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyString()]
        [string]$Outcome = '',

        [AllowEmptyString()]
        [string]$Result = ''
    )

    $fromResult = switch ($Result)
    {
        'failure' { 'failed' }
        'success' { 'succeeded' }
        'cancelled' { 'none' }
        'skipped' { 'none' }
        '' { '' }
        default { throw "Unknown job result '$Result'. Expected failure, success, cancelled or skipped." }
    }

    if ($Outcome -and $Outcome -notin @('failed', 'succeeded'))
    {
        throw "Unknown outcome '$Outcome'. Expected failed or succeeded."
    }
    if ($Outcome -and $fromResult -and $Outcome -ne $fromResult)
    {
        throw "-Outcome '$Outcome' contradicts -Result '$Result'. Give one of them."
    }
    if ($Outcome)
    {
        return $Outcome
    }
    if ($fromResult)
    {
        return $fromResult
    }
    throw 'Give -Outcome (failed or succeeded) or -Result (a job result).'
}



function Select-IssuesContaining
{
    <#
    .SYNOPSIS
        The issues whose body contains the text, lowest issue number first.
    #>
    [CmdletBinding()]
    [OutputType([object[]])]
    param
    (
        [AllowEmptyCollection()]
        [object[]]$Issues = @(),

        [Parameter(Mandatory)]
        [string]$Text
    )

    $found = @($Issues | Where-Object { $_.body -and ([string]$_.body).Contains($Text, [System.StringComparison]::Ordinal) })
    return @($found | Sort-Object -Property { [int]$_.number })
}



function Resolve-ReportAction
{
    <#
    .SYNOPSIS
        Decides what to do with the issues: Create, Comment, Close or None.
    .OUTPUTS
        An object with Action (string) and Numbers (int[]): the issue to comment on, or the issues
        to close. Numbers is empty for Create and None.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param
    (
        [Parameter(Mandatory)]
        [ValidateSet('failed', 'succeeded', 'none')]
        [string]$Outcome,

        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Signature,

        [AllowEmptyCollection()]
        [object[]]$OpenIssues = @()
    )

    if ($Outcome -eq 'failed')
    {
        $marker = Get-FailureMarker -Workflow $Workflow -Signature $Signature
        $existing = @(Select-IssuesContaining -Issues $OpenIssues -Text $marker)
        if ($existing.Count -gt 0)
        {
            return [pscustomobject]@{ Action = 'Comment'; Numbers = [int[]]@([int]$existing[0].number) }
        }
        return [pscustomobject]@{ Action = 'Create'; Numbers = [int[]]@() }
    }

    if ($Outcome -eq 'succeeded')
    {
        $prefix = Get-WorkflowMarkerPrefix -Workflow $Workflow
        $open = @(Select-IssuesContaining -Issues $OpenIssues -Text $prefix)
        if ($open.Count -gt 0)
        {
            return [pscustomobject]@{ Action = 'Close'; Numbers = [int[]]@($open | ForEach-Object { [int]$_.number }) }
        }
    }

    return [pscustomobject]@{ Action = 'None'; Numbers = [int[]]@() }
}



function Format-FailureIssueTitle
{
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Signature,

        [AllowEmptyString()]
        [string]$Title = ''
    )

    if ($Title.Trim())
    {
        return "[$Workflow] $($Title.Trim())"
    }
    return "[$Workflow] Scheduled check failed ($Signature)"
}



function Format-FailureIssueBody
{
    <#
    .SYNOPSIS
        The body of a new failure issue. The marker is its first line.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Signature,

        [Parameter(Mandatory)]
        [string]$Stamp,

        [AllowEmptyString()]
        [string]$RunUrl = '',

        [AllowEmptyString()]
        [string]$Output = '',

        [AllowEmptyString()]
        [string]$SuggestedFix = '',

        [int]$MaxOutputChars = 60000
    )

    # The output must not be able to carry a marker of its own into the body.
    $safeOutput = $Output.Replace('<!--', '&lt;!--').Trim()
    if ($safeOutput.Length -gt $MaxOutputChars)
    {
        $safeOutput = $safeOutput.Substring(0, $MaxOutputChars) + "`n... (cut; the full output is in the run log)"
    }
    if (-not $safeOutput)
    {
        $safeOutput = '_See the run log._'
    }

    $fix = $SuggestedFix.Trim()
    if (-not $fix)
    {
        $fix = 'Read the run log, find the cause, and fix it through a normal pull request. This script never edits a workflow file or any other protected path.'
    }

    $run = if ($RunUrl) { $RunUrl } else { '_not recorded_' }

    $lines = @(
        (Get-FailureMarker -Workflow $Workflow -Signature $Signature)
        "**Workflow:** ``$Workflow`` | **Signature:** ``$Signature`` | **First seen:** $Stamp"
        "**Run:** $run"
        ''
        '## Failing output'
        ''
        $safeOutput
        ''
        '## Suggested fix'
        ''
        $fix
        ''
        'The next successful run closes this issue. A repeat of the same failure adds a comment with the run link.'
        ''
        "Posted by ``scripts/report-failure.ps1`` for ``$Workflow``."
    )
    return ($lines -join "`n")
}



function Format-RepeatComment
{
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Stamp,

        [AllowEmptyString()]
        [string]$RunUrl = ''
    )

    $run = if ($RunUrl) { $RunUrl } else { '_run link not recorded_' }
    return "Failed again at ${Stamp}: $run`n`nPosted by ``scripts/report-failure.ps1`` for ``$Workflow``."
}



function Format-ResolvedComment
{
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [string]$Stamp,

        [AllowEmptyString()]
        [string]$RunUrl = ''
    )

    $run = if ($RunUrl) { $RunUrl } else { '_run link not recorded_' }
    return "Resolved: the run at $Stamp succeeded ($run).`n`nPosted by ``scripts/report-failure.ps1`` for ``$Workflow``."
}



function Get-IssueLabelNames
{
    [CmdletBinding()]
    [OutputType([string[]])]
    param
    (
        [Parameter(Mandatory)]
        [object]$Issue
    )

    $property = $Issue.PSObject.Properties['labels']
    if (-not $property -or -not $property.Value)
    {
        return [string[]]@()
    }
    return [string[]]@($property.Value | ForEach-Object { [string]$_.name })
}



function Format-SummaryIssue
{
    <#
    .SYNOPSIS
        Title and body of the summary issue, and how many issues each list holds.
    .OUTPUTS
        An object with Title, Body, StaleCount and CriticalCount.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param
    (
        [AllowEmptyCollection()]
        [object[]]$OpenIssues = @(),

        [Parameter(Mandatory)]
        [datetime]$NowUtc,

        [int]$OlderThanDays = 7,

        [int]$CriticalAfterDays = 2,

        [string]$SummaryMarker = '<!-- automated-summary -->'
    )

    $candidates = @(
        $OpenIssues |
            Where-Object { -not ($_.body -and ([string]$_.body).Contains($SummaryMarker, [System.StringComparison]::Ordinal)) } |
            Sort-Object -Property { [int]$_.number }
    )
    $ageInDays = { param($issue) ($NowUtc - ([datetime]$issue.createdAt).ToUniversalTime()).TotalDays }
    $stale = @($candidates | Where-Object { (& $ageInDays $_) -ge $OlderThanDays })
    $critical = @($candidates | Where-Object { ((Get-IssueLabelNames -Issue $_) -contains 'critical') -and ((& $ageInDays $_) -ge $CriticalAfterDays) })

    $describe = { param($issue) "- #$($issue.number) $($issue.title) (opened $(([datetime]$issue.createdAt).ToUniversalTime().ToString('yyyy-MM-dd')))" }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add($SummaryMarker)
    $lines.Add("Automated issues open longer than $OlderThanDays days, as of $($NowUtc.ToString('yyyy-MM-dd')) UTC.")
    $lines.Add('')
    if ($critical.Count -gt 0)
    {
        $lines.Add("## Escalation: critical and open longer than $CriticalAfterDays days")
        $lines.Add('')
        foreach ($issue in $critical) { $lines.Add((& $describe $issue)) }
        $lines.Add('')
    }
    $lines.Add("## Open longer than $OlderThanDays days")
    $lines.Add('')
    if ($stale.Count -eq 0)
    {
        $lines.Add('_None._')
    }
    foreach ($issue in $stale) { $lines.Add((& $describe $issue)) }
    $lines.Add('')
    $lines.Add('Posted by `scripts/report-failure.ps1 -Summary`.')

    $prefix = if ($critical.Count -gt 0) { 'HIGH PRIORITY: ' } else { '' }
    return [pscustomobject]@{
        Title         = "${prefix}Automated issues open longer than $OlderThanDays days"
        Body          = ($lines -join "`n")
        StaleCount    = $stale.Count
        CriticalCount = $critical.Count
    }
}



# ---------------------------------------------------------------------------------------------
# gh access. Every call goes through Invoke-Gh, which the tests mock.
# ---------------------------------------------------------------------------------------------

function Invoke-Gh
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = & gh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        throw "gh $($Arguments -join ' ') failed (exit $LASTEXITCODE): $($output -join "`n")"
    }
    return $output
}



function Invoke-GhWithBody
{
    <#
    .SYNOPSIS
        Runs gh with the text passed as --body-file, so a long body never meets the command-line limit.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Body
    )

    $file = [System.IO.Path]::GetTempFileName()
    try
    {
        [System.IO.File]::WriteAllText($file, $Body, [System.Text.UTF8Encoding]::new($false))
        return Invoke-Gh -Arguments ($Arguments + @('--body-file', $file))
    }
    finally
    {
        Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
    }
}



function Get-OpenAutomatedIssues
{
    [CmdletBinding()]
    [OutputType([object[]])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Repository
    )

    $json = Invoke-Gh -Arguments @(
        'issue', 'list', '-R', $Repository, '--label', 'automated', '--state', 'open',
        '--limit', '1000', '--json', 'number,title,body,createdAt,labels'
    )
    $text = (@($json) -join "`n").Trim()
    if (-not $text)
    {
        return @()
    }
    return @($text | ConvertFrom-Json)
}



function Confirm-Label
{
    <#
    .SYNOPSIS
        Creates the label when it does not exist. `gh issue create --label` fails on a missing label.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string]$Repository,

        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Description,

        [switch]$DryRun
    )

    $existing = @(Invoke-Gh -Arguments @('label', 'list', '-R', $Repository, '--search', $Name, '--limit', '100', '--json', 'name', '--jq', '.[].name'))
    if ($existing -contains $Name)
    {
        return
    }
    if ($DryRun)
    {
        Write-Host "[dry-run] would create label '$Name'"
        return
    }
    Invoke-Gh -Arguments @('label', 'create', $Name, '-R', $Repository, '--color', $script:LabelColor, '--description', $Description) | Out-Null
}



function Invoke-OutcomeReport
{
    <#
    .SYNOPSIS
        Reports one outcome: reads the open automated issues, decides, and writes.
    .OUTPUTS
        The decision object of Resolve-ReportAction.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param
    (
        [Parameter(Mandatory)]
        [string]$Repository,

        [Parameter(Mandatory)]
        [string]$Workflow,

        [Parameter(Mandatory)]
        [ValidateSet('failed', 'succeeded', 'none')]
        [string]$Outcome,

        [AllowEmptyString()]
        [string]$Signature = '',

        [AllowEmptyString()]
        [string]$Title = '',

        [AllowEmptyString()]
        [string]$Output = '',

        [AllowEmptyString()]
        [string]$SuggestedFix = '',

        [AllowEmptyCollection()]
        [string[]]$Labels = @(),

        [AllowEmptyString()]
        [string]$RunUrl = '',

        [Parameter(Mandatory)]
        [string]$Stamp,

        [switch]$DryRun
    )

    $resolvedSignature = Get-FailureSignature -Signature $Signature -Text $Output

    if ($Outcome -eq 'none')
    {
        Write-Host "[$Workflow] the result is neither a failure nor a success; nothing to do."
        return (Resolve-ReportAction -Outcome 'none' -Workflow $Workflow -Signature $resolvedSignature)
    }

    $openIssues = @(Get-OpenAutomatedIssues -Repository $Repository)
    $decision = Resolve-ReportAction -Outcome $Outcome -Workflow $Workflow -Signature $resolvedSignature -OpenIssues $openIssues

    switch ($decision.Action)
    {
        'Comment'
        {
            $number = $decision.Numbers[0]
            $comment = Format-RepeatComment -Workflow $Workflow -Stamp $Stamp -RunUrl $RunUrl
            if ($DryRun)
            {
                Write-Host "[dry-run] would comment on #${number}:`n$comment"
            }
            else
            {
                Invoke-Gh -Arguments @('issue', 'comment', "$number", '-R', $Repository, '--body', $comment) | Out-Null
                Write-Host "[$Workflow] added this run to #$number (signature $resolvedSignature)."
            }
        }
        'Create'
        {
            $issueTitle = Format-FailureIssueTitle -Workflow $Workflow -Signature $resolvedSignature -Title $Title
            $body = Format-FailureIssueBody -Workflow $Workflow -Signature $resolvedSignature -Stamp $Stamp `
                -RunUrl $RunUrl -Output $Output -SuggestedFix $SuggestedFix -MaxOutputChars $script:MaxBodyChars
            $allLabels = @($script:BaseLabels + $Labels | Where-Object { $_ } | Select-Object -Unique)

            Confirm-Label -Repository $Repository -Name 'automated' -Description 'Opened by a scheduled check; closed by its next successful run' -DryRun:$DryRun
            Confirm-Label -Repository $Repository -Name 'scheduled' -Description 'Reported by a scheduled workflow or job' -DryRun:$DryRun
            foreach ($label in ($allLabels | Where-Object { $_ -notin $script:BaseLabels }))
            {
                Confirm-Label -Repository $Repository -Name $label -Description 'Used on automated failure issues' -DryRun:$DryRun
            }

            if ($DryRun)
            {
                Write-Host "[dry-run] would create '$issueTitle' with labels $($allLabels -join ', '):`n$body"
            }
            else
            {
                $arguments = @('issue', 'create', '-R', $Repository, '--title', $issueTitle)
                foreach ($label in $allLabels)
                {
                    $arguments += @('--label', $label)
                }
                $url = Invoke-GhWithBody -Arguments $arguments -Body $body
                Write-Host "[$Workflow] created $url (signature $resolvedSignature)."
            }
        }
        'Close'
        {
            $comment = Format-ResolvedComment -Workflow $Workflow -Stamp $Stamp -RunUrl $RunUrl
            foreach ($number in $decision.Numbers)
            {
                if ($DryRun)
                {
                    Write-Host "[dry-run] would close #$number"
                    continue
                }
                Invoke-Gh -Arguments @('issue', 'close', "$number", '-R', $Repository, '--comment', $comment) | Out-Null
                Write-Host "[$Workflow] closed #$number."
            }
        }
        default
        {
            Write-Host "[$Workflow] no open issue to close."
        }
    }

    return $decision
}



function Invoke-SummaryReport
{
    <#
    .SYNOPSIS
        Creates, updates or closes the one summary issue.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string]$Repository,

        [Parameter(Mandatory)]
        [datetime]$NowUtc,

        [int]$OlderThanDays = 7,

        [switch]$DryRun
    )

    $openIssues = @(Get-OpenAutomatedIssues -Repository $Repository)
    $summary = Format-SummaryIssue -OpenIssues $openIssues -NowUtc $NowUtc -OlderThanDays $OlderThanDays `
        -CriticalAfterDays $script:CriticalAfterDays -SummaryMarker $script:SummaryMarker
    $existing = @(Select-IssuesContaining -Issues $openIssues -Text $script:SummaryMarker)

    if ($summary.StaleCount -eq 0 -and $summary.CriticalCount -eq 0)
    {
        foreach ($issue in $existing)
        {
            if ($DryRun)
            {
                Write-Host "[dry-run] would close summary #$($issue.number)"
                continue
            }
            Invoke-Gh -Arguments @('issue', 'close', "$($issue.number)", '-R', $Repository, '--comment', 'Nothing is open longer than the threshold. Closing the summary.') | Out-Null
            Write-Host "[summary] closed #$($issue.number)."
        }
        Write-Host '[summary] nothing to list.'
        return
    }

    if ($existing.Count -gt 0)
    {
        $number = [int]$existing[0].number
        if ($DryRun)
        {
            Write-Host "[dry-run] would update summary #${number}: $($summary.Title)`n$($summary.Body)"
            return
        }
        Invoke-GhWithBody -Arguments @('issue', 'edit', "$number", '-R', $Repository, '--title', $summary.Title) -Body $summary.Body | Out-Null
        Write-Host "[summary] updated #$number."
        return
    }

    Confirm-Label -Repository $Repository -Name 'automated' -Description 'Opened by a scheduled check; closed by its next successful run' -DryRun:$DryRun
    if ($DryRun)
    {
        Write-Host "[dry-run] would create summary '$($summary.Title)':`n$($summary.Body)"
        return
    }
    $url = Invoke-GhWithBody -Arguments @('issue', 'create', '-R', $Repository, '--title', $summary.Title, '--label', 'automated') -Body $summary.Body
    Write-Host "[summary] created $url."
}



function Read-FailureOutput
{
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [AllowEmptyString()]
        [string]$Path = ''
    )

    if (-not $Path)
    {
        return ''
    }
    if (-not (Test-Path -LiteralPath $Path))
    {
        return "(no output file at $Path)"
    }
    $text = Get-Content -LiteralPath $Path -Raw
    if ($null -eq $text)
    {
        return ''
    }
    return [string]$text
}



# ---------------------------------------------------------------------------------------------
# Entry point. Skipped when the file is dot-sourced, so tests can load the functions alone.
# ---------------------------------------------------------------------------------------------

if ($MyInvocation.InvocationName -eq '.')
{
    return
}

if (-not $Repository)
{
    throw 'Give -Repository owner/name, or set GITHUB_REPOSITORY.'
}

if (-not $RunUrl -and $env:GITHUB_SERVER_URL -and $env:GITHUB_REPOSITORY -and $env:GITHUB_RUN_ID)
{
    $RunUrl = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
}

$nowUtc = [System.DateTime]::UtcNow
$stamp = $nowUtc.ToString('yyyy-MM-dd HH:mm', [System.Globalization.CultureInfo]::InvariantCulture) + ' UTC'

if ($Summary)
{
    if ($Workflow -or $Outcome -or $Result)
    {
        throw '-Summary does not take -Workflow, -Outcome or -Result.'
    }
    Invoke-SummaryReport -Repository $Repository -NowUtc $nowUtc -OlderThanDays $OlderThanDays -DryRun:$DryRun
    return
}

if (-not $Workflow.Trim())
{
    throw 'Give -Workflow, or -Summary.'
}

$reportOutcome = ConvertTo-ReportOutcome -Outcome $Outcome -Result $Result
$failureOutput = if ($reportOutcome -eq 'failed') { Read-FailureOutput -Path $BodyPath } else { '' }

Invoke-OutcomeReport -Repository $Repository -Workflow $Workflow.Trim() -Outcome $reportOutcome -Signature $Signature `
    -Title $Title -Output $failureOutput -SuggestedFix $SuggestedFix -Labels $Labels -RunUrl $RunUrl -Stamp $stamp `
    -DryRun:$DryRun | Out-Null
