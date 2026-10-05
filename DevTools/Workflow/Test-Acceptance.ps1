[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$base=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatAcceptance-'+[guid]::NewGuid().ToString('N'))
$contract=Join-Path $base 'contract.json'
$root=Join-Path $base 'evidence'
$acceptance=Join-Path $root 'acceptance.json'
$evidence=Join-Path $root 'evidence.json'
[void][IO.Directory]::CreateDirectory($root)
$utf8=[Text.UTF8Encoding]::new($false)
function Save([string]$Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 15),$utf8)}
function Invoke-Comparison { & (Join-Path $PSScriptRoot 'Compare-Acceptance.ps1') -TaskId AI-000999 -ContractPath $contract -AcceptancePath $acceptance -EvidenceRoot $root | ConvertFrom-Json }
function Has($Result,[string]$Code){if($Code -notin @($Result.mismatches.code)){throw "Missing mismatch $Code"}}
try {
    $requirements=[pscustomobject]@{schemaVersion=1;taskId='AI-000999';checks=@(
        [pscustomobject]@{id='errors_zero';expected=0;evidencePath='evidence.json';valuePath='errors'},
        [pscustomobject]@{id='complete';expected=$true;evidencePath='evidence.json';valuePath='complete'}
    )}
    $submission=[pscustomobject]@{schemaVersion=1;taskId='AI-000999';checks=@(
        [pscustomobject]@{id='errors_zero';expected=0;actual=0;passed=$true;evidencePath='evidence.json';valuePath='errors'},
        [pscustomobject]@{id='complete';expected=$true;actual=$true;passed=$true;evidencePath='evidence.json';valuePath='complete'}
    )}
    Save $contract $requirements
    Save $acceptance $submission
    Save $evidence ([pscustomobject]@{taskId='AI-000999';errors=0;complete=$true})
    if((Invoke-Comparison).action -ne 'ACCEPTANCE_MATCHED'){throw 'Positive acceptance failed.'}
    $submission.checks=@($submission.checks[0]);Save $acceptance $submission
    Has (Invoke-Comparison) 'MISSING_ID'
    $submission.checks=@($submission.checks[0],$submission.checks[0]);Save $acceptance $submission
    $bad=Invoke-Comparison;Has $bad 'DUPLICATE_OR_INVALID_ID';Has $bad 'MISSING_ID'
    $submission.checks=@([pscustomobject]@{id='errors_zero';expected='0';actual='0';passed='true';evidencePath='evidence.json';valuePath='errors'},
        $requirements.checks[1] | Select-Object id,expected,@{n='actual';e={$true}},@{n='passed';e={$true}},evidencePath,valuePath)
    Save $acceptance $submission
    Has (Invoke-Comparison) 'INVALID_VALUE_TYPE'
    $submission.checks=@(
        [pscustomobject]@{id='errors_zero';expected=0;actual=0;passed=$true;evidencePath='evidence.json';valuePath='errors'},
        [pscustomobject]@{id='complete';expected=$true;actual=$true;passed=$true;evidencePath='evidence.json';valuePath='complete'})
    Save $acceptance $submission
    Save $evidence ([pscustomobject]@{taskId='OTHER';errors=1;complete=$false})
    $bad=Invoke-Comparison;Has $bad 'EVIDENCE_WRONG_TASK';Has $bad 'EVIDENCE_VALUE_MISMATCH'
    Save $evidence ([pscustomobject]@{taskId='AI-000999';errors=0;complete=$true})
    $submission.checks[0].evidencePath='../outside.json';Save $acceptance $submission
    Has (Invoke-Comparison) 'EVIDENCE_CONTRACT_TAMPERED'

    $real=Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/outputs/U1-PRE-R1/AI-000169/verification-summary.json'
    $oldRoot=Join-Path $base 'old'
    [void][IO.Directory]::CreateDirectory($oldRoot)
    Copy-Item -LiteralPath $real -Destination (Join-Path $oldRoot 'verification-summary.json')
    $oldContract=Join-Path $base 'old-contract.json'
    $oldAcceptance=Join-Path $oldRoot 'acceptance.json'
    Save $oldContract ([pscustomobject]@{schemaVersion=1;taskId='AI-000169';checks=@(
        [pscustomobject]@{id='anchors';expected=2;evidencePath='verification-summary.json';valuePath='audit.viewportAnchors'},
        [pscustomobject]@{id='console';expected=0;evidencePath='verification-summary.json';valuePath='unity.consoleErrors'},
        [pscustomobject]@{id='human_pending';expected=$true;evidencePath='verification-summary.json';valuePath='manualVerificationRequired'})})
    Save $oldAcceptance ([pscustomobject]@{schemaVersion=1;taskId='AI-000169';checks=@(
        [pscustomobject]@{id='anchors';expected=2;actual=2;passed=$true;evidencePath='verification-summary.json';valuePath='audit.viewportAnchors'},
        [pscustomobject]@{id='console';expected=0;actual=0;passed=$true;evidencePath='verification-summary.json';valuePath='unity.consoleErrors'},
        [pscustomobject]@{id='human_pending';expected=$true;actual=$true;passed=$true;evidencePath='verification-summary.json';valuePath='manualVerificationRequired'})})
    $reverse=& (Join-Path $PSScriptRoot 'Compare-Acceptance.ps1') -TaskId AI-000169 -ContractPath $oldContract -AcceptancePath $oldAcceptance -EvidenceRoot $oldRoot | ConvertFrom-Json
    if($reverse.action -ne 'ACCEPTANCE_MATCHED'){throw 'AI169 reverse example failed.'}
    [pscustomobject]@{result='PASS';checks=8;ai169='machine-report-only; original PENDING_USER untouched'}|ConvertTo-Json -Compress
}
finally {
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if((Test-Path $base)-and [IO.Path]::GetFullPath($base).StartsWith($temp,[StringComparison]::OrdinalIgnoreCase)-and
        (Split-Path -Leaf $base).StartsWith('EatWhatAcceptance-',[StringComparison]::Ordinal)){
        Remove-Item -LiteralPath $base -Recurse -Force
    }
}
