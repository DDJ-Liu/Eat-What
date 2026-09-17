[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dataTablesRoot = Join-Path $repositoryRoot 'DataTables'
$validator = Join-Path $PSScriptRoot 'Validate-DataTables.ps1'
$fixturesRoot = Join-Path $PSScriptRoot 'Fixtures'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('eatwhat-datatables-' + [guid]::NewGuid().ToString('N'))

function New-StagedDataTables {
    $staged = Join-Path $temporaryRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $staged | Out-Null
    Copy-Item -Path (Join-Path $dataTablesRoot '*') -Destination $staged -Recurse -Force
    return $staged
}

function Apply-Fixture {
    param(
        [Parameter(Mandatory)][string]$StagedRoot,
        [Parameter(Mandatory)][string]$FixtureName
    )

    $fixture = Join-Path $fixturesRoot $FixtureName
    Copy-Item -Path (Join-Path $fixture '*') -Destination $StagedRoot -Recurse -Force
}

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

try {
    $liveRoot = New-StagedDataTables
    $liveReportDirectory = Join-Path $temporaryRoot 'live-reports'
    $liveReport = & $validator -DataTablesRoot $liveRoot -ReportDirectory $liveReportDirectory -NoExit -Quiet
    Assert-True $liveReport.isValid 'The checked-in DataTables source must pass static validation.'
    Assert-True ($liveReport.v24RuleCounts.'V-02'.warnings -eq 0 -and $liveReport.v24RuleCounts.'V-03'.warnings -eq 0) 'Checked-in v2.4 whitelist coverage/final-record warnings must both be zero.'
    foreach ($reportName in @(
        'CK01-B_recipe-whitelist-coverage-report.json',
        'CK01-B_multi-candidate-carrier-report.json',
        'CK01-B_forward-reachability-report.json',
        'CK01-B_reverse-reachability-report.json'
    )) {
        Assert-True (Test-Path -LiteralPath (Join-Path $liveReportDirectory $reportName) -PathType Leaf) "Missing v2.4 validator report: $reportName"
    }

    $quotedRoot = New-StagedDataTables
    Apply-Fixture -StagedRoot $quotedRoot -FixtureName 'ValidQuotedUtf8'
    $quotedReport = & $validator -DataTablesRoot $quotedRoot -NoExit -Quiet
    Assert-True $quotedReport.isValid 'Quoted CSV fields and UTF-8 fixture must pass static validation.'

    $cases = @(
        @{ Name = 'InvalidMissingRequired'; Expected = "required field 'areaName'" },
        @{ Name = 'InvalidDuplicateKeys'; Expected = 'duplicates primary key' },
        @{ Name = 'InvalidDuplicateKeys'; Expected = 'duplicates asset name' },
        @{ Name = 'InvalidEnum'; Expected = 'not a ContainerTag value' },
        @{ Name = 'InvalidInt'; Expected = 'is not an int' },
        @{ Name = 'InvalidDanglingReference'; Expected = 'does not exist in' }
    )

    foreach ($case in $cases) {
        $stagedRoot = New-StagedDataTables
        Apply-Fixture -StagedRoot $stagedRoot -FixtureName $case.Name
        $fixtureReport = & $validator -DataTablesRoot $stagedRoot -NoExit -Quiet
        Assert-True (-not $fixtureReport.isValid) "Fixture $($case.Name) unexpectedly passed validation."
        Assert-True (($fixtureReport.errors -join "`n") -match [regex]::Escape($case.Expected)) "Fixture $($case.Name) did not report '$($case.Expected)'."
    }

    [pscustomobject]@{
        success = $true
        validSource = 'PASS'
        quotedUtf8 = 'PASS'
        invalidFixtures = 'PASS'
        v24Reports = 4
    } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
