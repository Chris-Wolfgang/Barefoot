#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester 5 tests for scripts/report-failure.ps1.

.DESCRIPTION
    Covers the pure parts (signature, marker, outcome mapping, the Create / Comment / Close / None
    decision, issue and summary text) and the orchestration with `gh` mocked. No test reaches
    GitHub: every gh call in the script goes through Invoke-Gh, and a catch-all mock throws on any
    call a test did not expect.

    Run from the repository root:
        Invoke-Pester -Path ./scripts/report-failure.Tests.ps1 -Output Detailed
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    # Dot-sourcing loads the functions and skips the script's entry point.
    . (Join-Path $PSScriptRoot 'report-failure.ps1')

    function New-TestIssue
    {
        param
        (
            [int]$Number,
            [string]$Body,
            [string]$Title = "Issue $Number",
            [string]$CreatedAt = '2026-11-01T00:00:00Z',
            [string[]]$LabelNames = @('automated')
        )

        return [pscustomobject]@{
            number    = $Number
            title     = $Title
            body      = $Body
            createdAt = $CreatedAt
            labels    = @($LabelNames | ForEach-Object { [pscustomobject]@{ name = $_ } })
        }
    }
}

Describe 'ConvertTo-SignatureSlug' {
    It 'turns <Text> into <Expected>' -ForEach @(
        @{ Text = 'Findings'; Expected = 'findings' }
        @{ Text = '  exit 5  '; Expected = 'exit-5' }
        @{ Text = 'a|b --> c'; Expected = 'a-b-c' }
        @{ Text = 'v1.2_rc-3'; Expected = 'v1.2_rc-3' }
        @{ Text = '***'; Expected = 'default' }
        @{ Text = ''; Expected = 'default' }
    ) {
        ConvertTo-SignatureSlug -Text $Text | Should -BeExactly $Expected
    }

    It 'cuts a long text at 64 characters' {
        (ConvertTo-SignatureSlug -Text ('a' * 200)).Length | Should -Be 64
    }
}

Describe 'Get-NormalizedFailureText' {
    It 'removes timestamps, identifiers, numbers, colour codes and spacing' {
        $text = "2026-11-20T03:17:45Z  Build FAILED`r`n`r`n`e[31merror`e[0m in commit 3d3c42e5aac5 after 125 ms"
        Get-NormalizedFailureText -Text $text | Should -BeExactly "<time> build failed`nerror in commit <id> after # ms"
    }

    It 'replaces a GUID with one placeholder' {
        Get-NormalizedFailureText -Text 'run 0f4a354d-d554-7a28-c70b-7853dbd95d5b' | Should -BeExactly 'run <id>'
    }

    It 'keeps a word that only looks hexadecimal' {
        Get-NormalizedFailureText -Text 'the facade defaced' | Should -BeExactly 'the facade defaced'
    }

    It 'returns an empty string for blank input' {
        Get-NormalizedFailureText -Text "  `n `n" | Should -BeExactly ''
    }
}

Describe 'Get-FailureSignature' {
    It 'uses the explicit signature, reduced to a slug' {
        Get-FailureSignature -Signature 'Exit 5' -Text 'anything' | Should -BeExactly 'exit-5'
    }

    It 'is "default" when there is neither a signature nor output' {
        Get-FailureSignature | Should -BeExactly 'default'
        Get-FailureSignature -Signature '   ' -Text "`n" | Should -BeExactly 'default'
    }

    It 'hashes the output to "h-" plus 12 hex digits' {
        Get-FailureSignature -Text 'error CS1002: ; expected' | Should -Match '^h-[0-9a-f]{12}$'
    }

    It 'is the same for two runs that differ only in time, identifiers and numbers' {
        $first = Get-FailureSignature -Text "2026-11-20 03:17:45 test Foo failed after 12 s (run 4411, sha 3d3c42e5aac5)"
        $second = Get-FailureSignature -Text "2026-11-24 03:18:02 test Foo failed after 9 s (run 4502, sha 0f4a354dd554)"
        $second | Should -BeExactly $first
    }

    It 'differs for two different failures' {
        $first = Get-FailureSignature -Text 'test Foo failed'
        $second = Get-FailureSignature -Text 'test Bar failed'
        $second | Should -Not -Be $first
    }
}

Describe 'Get-FailureMarker' {
    It 'formats the hidden HTML comment' {
        Get-FailureMarker -Workflow 'codeql' -Signature 'default' | Should -BeExactly '<!-- automated-failure: codeql|default -->'
    }

    It 'reduces both parts to slugs, so neither can end the comment or add a separator' {
        Get-FailureMarker -Workflow 'License Audit' -Signature 'a|b -->' | Should -BeExactly '<!-- automated-failure: license-audit|a-b -->'
    }

    It 'starts with the workflow prefix' {
        $marker = Get-FailureMarker -Workflow 'stryker' -Signature 'exit-1'
        $marker.StartsWith((Get-WorkflowMarkerPrefix -Workflow 'stryker')) | Should -BeTrue
    }

    It 'gives a prefix that does not match a longer workflow name' {
        $other = Get-FailureMarker -Workflow 'codeql-actions' -Signature 'default'
        $other.Contains((Get-WorkflowMarkerPrefix -Workflow 'codeql')) | Should -BeFalse
    }
}

Describe 'ConvertTo-ReportOutcome' {
    It 'maps -Outcome "<Outcome>" -Result "<Result>" to <Expected>' -ForEach @(
        @{ Outcome = 'failed'; Result = ''; Expected = 'failed' }
        @{ Outcome = 'succeeded'; Result = ''; Expected = 'succeeded' }
        @{ Outcome = ''; Result = 'failure'; Expected = 'failed' }
        @{ Outcome = ''; Result = 'success'; Expected = 'succeeded' }
        @{ Outcome = ''; Result = 'cancelled'; Expected = 'none' }
        @{ Outcome = ''; Result = 'skipped'; Expected = 'none' }
        @{ Outcome = 'failed'; Result = 'failure'; Expected = 'failed' }
    ) {
        ConvertTo-ReportOutcome -Outcome $Outcome -Result $Result | Should -BeExactly $Expected
    }

    It 'throws when neither is given' {
        { ConvertTo-ReportOutcome } | Should -Throw '*-Outcome*'
    }

    It 'throws when the two contradict each other' {
        { ConvertTo-ReportOutcome -Outcome 'succeeded' -Result 'failure' } | Should -Throw '*contradicts*'
    }

    It 'throws on an unknown job result' {
        { ConvertTo-ReportOutcome -Result 'neutral' } | Should -Throw '*Unknown job result*'
    }
}

Describe 'Resolve-ReportAction' {
    BeforeAll {
        $script:SameSignature = New-TestIssue -Number 12 -Body "$(Get-FailureMarker -Workflow 'codeql' -Signature 'default')`ntext"
        $script:OtherSignature = New-TestIssue -Number 7 -Body "$(Get-FailureMarker -Workflow 'codeql' -Signature 'build')`ntext"
        $script:OtherWorkflow = New-TestIssue -Number 3 -Body "$(Get-FailureMarker -Workflow 'stryker' -Signature 'default')`ntext"
        $script:NoBody = New-TestIssue -Number 2 -Body $null
    }

    It 'creates when no open issue carries the marker' {
        $decision = Resolve-ReportAction -Outcome failed -Workflow 'codeql' -Signature 'default' -OpenIssues @($script:OtherSignature, $script:OtherWorkflow, $script:NoBody)
        $decision.Action | Should -BeExactly 'Create'
        $decision.Numbers.Count | Should -Be 0
    }

    It 'creates when there are no open issues at all' {
        (Resolve-ReportAction -Outcome failed -Workflow 'codeql' -Signature 'default' -OpenIssues @()).Action | Should -BeExactly 'Create'
    }

    It 'comments when an open issue carries the marker' {
        $decision = Resolve-ReportAction -Outcome failed -Workflow 'codeql' -Signature 'default' -OpenIssues @($script:OtherWorkflow, $script:SameSignature, $script:OtherSignature)
        $decision.Action | Should -BeExactly 'Comment'
        $decision.Numbers | Should -Be @(12)
    }

    It 'comments on the lowest number when two issues carry the marker' {
        $duplicate = New-TestIssue -Number 40 -Body (Get-FailureMarker -Workflow 'codeql' -Signature 'default')
        $decision = Resolve-ReportAction -Outcome failed -Workflow 'codeql' -Signature 'default' -OpenIssues @($duplicate, $script:SameSignature)
        $decision.Numbers | Should -Be @(12)
    }

    It 'closes every issue of the workflow on success, whatever the signature' {
        $decision = Resolve-ReportAction -Outcome succeeded -Workflow 'codeql' -Signature 'default' -OpenIssues @($script:SameSignature, $script:OtherSignature, $script:OtherWorkflow, $script:NoBody)
        $decision.Action | Should -BeExactly 'Close'
        $decision.Numbers | Should -Be @(7, 12)
    }

    It 'does nothing on success when the workflow has no open issue' {
        $decision = Resolve-ReportAction -Outcome succeeded -Workflow 'codeql' -Signature 'default' -OpenIssues @($script:OtherWorkflow)
        $decision.Action | Should -BeExactly 'None'
        $decision.Numbers.Count | Should -Be 0
    }

    It 'does nothing for a cancelled or skipped result' {
        (Resolve-ReportAction -Outcome none -Workflow 'codeql' -Signature 'default' -OpenIssues @($script:SameSignature)).Action | Should -BeExactly 'None'
    }
}

Describe 'Format-FailureIssueTitle' {
    It 'prefixes the title with the workflow' {
        Format-FailureIssueTitle -Workflow 'stryker' -Signature 'default' -Title ' Score below the threshold ' | Should -BeExactly '[stryker] Score below the threshold'
    }

    It 'falls back to the signature' {
        Format-FailureIssueTitle -Workflow 'stryker' -Signature 'exit-1' | Should -BeExactly '[stryker] Scheduled check failed (exit-1)'
    }
}

Describe 'Format-FailureIssueBody' {
    It 'starts with the marker and carries the run, the output and the fix' {
        $body = Format-FailureIssueBody -Workflow 'codeql' -Signature 'default' -Stamp '2026-11-20 03:17 UTC' `
            -RunUrl 'https://example.test/run/1' -Output 'boom' -SuggestedFix 'Fix the build.'
        $lines = $body.Split("`n")
        $lines[0] | Should -BeExactly '<!-- automated-failure: codeql|default -->'
        $body | Should -Match ([regex]::Escape('**Run:** https://example.test/run/1'))
        $body | Should -Match 'First seen:\*\* 2026-11-20 03:17 UTC'
        $body | Should -Match "## Failing output`n`nboom`n"
        $body | Should -Match "## Suggested fix`n`nFix the build\.`n"
        $body.Contains("`r") | Should -BeFalse
    }

    It 'uses placeholders when there is no output, fix or run link' {
        $body = Format-FailureIssueBody -Workflow 'codeql' -Signature 'default' -Stamp 'now'
        $body | Should -Match '_See the run log\._'
        $body | Should -Match 'never edits a workflow file'
        $body | Should -Match '\*\*Run:\*\* _not recorded_'
    }

    It 'cuts long output' {
        $body = Format-FailureIssueBody -Workflow 'codeql' -Signature 'default' -Stamp 'now' -Output ('x' * 500) -MaxOutputChars 100
        $body | Should -Match 'x{100}\n\.\.\. \(cut; the full output is in the run log\)'
        $body | Should -Not -Match 'x{101}'
    }

    It 'does not let the output carry a second marker' {
        $foreign = Get-FailureMarker -Workflow 'stryker' -Signature 'default'
        $body = Format-FailureIssueBody -Workflow 'codeql' -Signature 'default' -Stamp 'now' -Output "log line`n$foreign"
        $body.Contains((Get-WorkflowMarkerPrefix -Workflow 'stryker')) | Should -BeFalse
        ([regex]::Matches($body, '<!--')).Count | Should -Be 1
    }
}

Describe 'Format-RepeatComment and Format-ResolvedComment' {
    It 'names the time and the run of a repeat' {
        Format-RepeatComment -Workflow 'codeql' -Stamp '2026-11-22 00:04 UTC' -RunUrl 'https://example.test/run/2' |
            Should -Match ([regex]::Escape('Failed again at 2026-11-22 00:04 UTC: https://example.test/run/2'))
    }

    It 'names the run that resolved the issue' {
        Format-ResolvedComment -Workflow 'codeql' -Stamp '2026-11-29 00:04 UTC' -RunUrl 'https://example.test/run/3' |
            Should -Match ([regex]::Escape('Resolved: the run at 2026-11-29 00:04 UTC succeeded (https://example.test/run/3).'))
    }
}

Describe 'Format-SummaryIssue' {
    BeforeAll {
        $script:Now = [datetime]::new(2026, 11, 20, 12, 0, 0, [System.DateTimeKind]::Utc)
        $script:Old = New-TestIssue -Number 5 -Title '[codeql] Old' -Body 'a' -CreatedAt '2026-11-01T00:00:00Z'
        $script:Recent = New-TestIssue -Number 6 -Title '[stryker] Recent' -Body 'b' -CreatedAt '2026-11-19T00:00:00Z'
        $script:Critical = New-TestIssue -Number 8 -Title '[semiweekly-job] Critical' -Body 'c' -CreatedAt '2026-11-17T00:00:00Z' -LabelNames @('automated', 'critical')
        $script:PreviousSummary = New-TestIssue -Number 9 -Title 'Automated issues open longer than 7 days' -Body "<!-- automated-summary -->`nold" -CreatedAt '2026-10-01T00:00:00Z'
        $script:NoLabels = [pscustomobject]@{ number = 10; title = 'No labels'; body = 'd'; createdAt = '2026-11-19T00:00:00Z'; labels = @() }
    }

    It 'lists only the issues older than the threshold and never the summary itself' {
        $summary = Format-SummaryIssue -OpenIssues @($script:Recent, $script:Old, $script:PreviousSummary, $script:NoLabels) -NowUtc $script:Now -OlderThanDays 7
        $summary.StaleCount | Should -Be 1
        $summary.CriticalCount | Should -Be 0
        $summary.Title | Should -BeExactly 'Automated issues open longer than 7 days'
        $summary.Body | Should -Match ([regex]::Escape('- #5 [codeql] Old (opened 2026-11-01)'))
        $summary.Body | Should -Not -Match '#6 '
        $summary.Body | Should -Not -Match '#9 '
        $summary.Body.Split("`n")[0] | Should -BeExactly '<!-- automated-summary -->'
    }

    It 'escalates a critical issue open longer than two days' {
        $summary = Format-SummaryIssue -OpenIssues @($script:Critical, $script:Recent) -NowUtc $script:Now -OlderThanDays 7
        $summary.CriticalCount | Should -Be 1
        $summary.StaleCount | Should -Be 0
        $summary.Title | Should -BeExactly 'HIGH PRIORITY: Automated issues open longer than 7 days'
        $summary.Body | Should -Match '## Escalation: critical and open longer than 2 days'
        $summary.Body | Should -Match '_None\._'
    }

    It 'reports nothing when every issue is recent' {
        $summary = Format-SummaryIssue -OpenIssues @($script:Recent) -NowUtc $script:Now -OlderThanDays 7
        $summary.StaleCount | Should -Be 0
        $summary.CriticalCount | Should -Be 0
    }
}

Describe 'Invoke-OutcomeReport with gh mocked' {
    BeforeAll {
        $script:Common = @{
            Repository = 'owner/repo'
            Workflow   = 'codeql'
            Stamp      = '2026-11-20 03:17 UTC'
            RunUrl     = 'https://example.test/run/1'
        }
        $script:IsIssueList = { $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'list' }
        $script:IsLabelList = { $Arguments[0] -eq 'label' -and $Arguments[1] -eq 'list' }
        $script:IsLabelCreate = { $Arguments[0] -eq 'label' -and $Arguments[1] -eq 'create' }
        $script:IsIssueCreate = { $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'create' }
        $script:IsIssueComment = { $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'comment' }
        $script:IsIssueClose = { $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'close' }
    }

    BeforeEach {
        # Any gh call a test did not set up is an error, so no test can reach GitHub.
        Mock Invoke-Gh { throw "Unexpected gh call: $($Arguments -join ' ')" }
    }

    Context 'a failure with no open issue' {
        BeforeEach {
            Mock Invoke-Gh { '[]' } -ParameterFilter $script:IsIssueList
            Mock Invoke-Gh { 'automated' } -ParameterFilter $script:IsLabelList
            Mock Invoke-Gh { } -ParameterFilter $script:IsLabelCreate
            Mock Invoke-Gh { 'https://example.test/issues/1' } -ParameterFilter $script:IsIssueCreate
        }

        It 'creates one issue with the base labels and the caller labels' {
            $decision = Invoke-OutcomeReport @script:Common -Outcome failed -Labels @('security')

            $decision.Action | Should -BeExactly 'Create'
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter {
                $joined = $Arguments -join ' '
                $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'create' -and
                $joined -match '--title \[codeql\] Scheduled check failed \(default\)' -and
                $joined -match '--label automated' -and $joined -match '--label scheduled' -and $joined -match '--label security' -and
                $Arguments -contains '--body-file'
            }
        }

        It 'creates the labels that do not exist yet' {
            Invoke-OutcomeReport @script:Common -Outcome failed -Labels @('security') | Out-Null

            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter { $Arguments[0] -eq 'label' -and $Arguments[1] -eq 'create' -and $Arguments[2] -eq 'scheduled' }
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter { $Arguments[0] -eq 'label' -and $Arguments[1] -eq 'create' -and $Arguments[2] -eq 'security' }
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter { $Arguments[0] -eq 'label' -and $Arguments[1] -eq 'create' -and $Arguments[2] -eq 'automated' }
        }

        It 'writes nothing in a dry run' {
            $decision = Invoke-OutcomeReport @script:Common -Outcome failed -DryRun

            $decision.Action | Should -BeExactly 'Create'
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter $script:IsIssueCreate
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter $script:IsLabelCreate
        }
    }

    Context 'a failure with an open issue for the same signature' {
        BeforeEach {
            $issues = @(New-TestIssue -Number 12 -Body (Get-FailureMarker -Workflow 'codeql' -Signature 'default'))
            $script:IssueListJson = ConvertTo-Json -InputObject $issues -Depth 5
            Mock Invoke-Gh { $script:IssueListJson } -ParameterFilter $script:IsIssueList
            Mock Invoke-Gh { } -ParameterFilter $script:IsIssueComment
        }

        It 'adds a comment and creates nothing' {
            $decision = Invoke-OutcomeReport @script:Common -Outcome failed

            $decision.Action | Should -BeExactly 'Comment'
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter {
                $Arguments[0] -eq 'issue' -and $Arguments[1] -eq 'comment' -and $Arguments[2] -eq '12' -and
                ($Arguments -join ' ') -match 'Failed again at 2026-11-20 03:17 UTC: https://example\.test/run/1'
            }
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter $script:IsIssueCreate
        }

        It 'creates a new issue for a different signature' {
            Mock Invoke-Gh { @('automated', 'scheduled') } -ParameterFilter $script:IsLabelList
            Mock Invoke-Gh { 'https://example.test/issues/13' } -ParameterFilter $script:IsIssueCreate

            $decision = Invoke-OutcomeReport @script:Common -Outcome failed -Signature 'build'

            $decision.Action | Should -BeExactly 'Create'
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter $script:IsIssueCreate
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter $script:IsIssueComment
        }
    }

    Context 'a success' {
        It 'closes every open issue of the workflow with a comment' {
            $issues = @(
                New-TestIssue -Number 12 -Body (Get-FailureMarker -Workflow 'codeql' -Signature 'default')
                New-TestIssue -Number 14 -Body (Get-FailureMarker -Workflow 'codeql' -Signature 'build')
                New-TestIssue -Number 15 -Body (Get-FailureMarker -Workflow 'stryker' -Signature 'default')
            )
            $script:IssueListJson = ConvertTo-Json -InputObject $issues -Depth 5
            Mock Invoke-Gh { $script:IssueListJson } -ParameterFilter $script:IsIssueList
            Mock Invoke-Gh { } -ParameterFilter $script:IsIssueClose

            $decision = Invoke-OutcomeReport @script:Common -Outcome succeeded

            $decision.Action | Should -BeExactly 'Close'
            Should -Invoke Invoke-Gh -Times 2 -Exactly -ParameterFilter $script:IsIssueClose
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter { $Arguments[1] -eq 'close' -and $Arguments[2] -eq '12' -and ($Arguments -join ' ') -match 'Resolved: the run at' }
            Should -Invoke Invoke-Gh -Times 1 -Exactly -ParameterFilter { $Arguments[1] -eq 'close' -and $Arguments[2] -eq '14' }
            Should -Invoke Invoke-Gh -Times 0 -Exactly -ParameterFilter { $Arguments[1] -eq 'close' -and $Arguments[2] -eq '15' }
        }

        It 'does nothing when the workflow has no open issue' {
            Mock Invoke-Gh { '[]' } -ParameterFilter $script:IsIssueList

            $decision = Invoke-OutcomeReport @script:Common -Outcome succeeded

            $decision.Action | Should -BeExactly 'None'
            Should -Invoke Invoke-Gh -Times 1 -Exactly
        }
    }

    Context 'a cancelled or skipped result' {
        It 'does not call gh at all' {
            $decision = Invoke-OutcomeReport @script:Common -Outcome none

            $decision.Action | Should -BeExactly 'None'
            Should -Invoke Invoke-Gh -Times 0 -Exactly
        }
    }
}
