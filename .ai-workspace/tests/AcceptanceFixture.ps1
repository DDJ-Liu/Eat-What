function Initialize-AcceptanceFixture {
    param([string]$WorkspaceRoot,[string]$TaskId,[string]$SourceRoot)
    $projectRoot=Split-Path -Parent $WorkspaceRoot
    $comparatorDirectory=Join-Path $projectRoot 'DevTools/Workflow'
    [void][IO.Directory]::CreateDirectory($comparatorDirectory)
    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $SourceRoot) 'DevTools/Workflow/Compare-Acceptance.ps1') -Destination (Join-Path $comparatorDirectory 'Compare-Acceptance.ps1') -Force
    $contract=Join-Path $WorkspaceRoot "inputs/acceptance/$TaskId.json"
    $output=Join-Path $WorkspaceRoot "outputs/test-acceptance/$TaskId"
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $contract))
    [void][IO.Directory]::CreateDirectory($output)
    $utf8=[Text.UTF8Encoding]::new($false)
    $required=[pscustomobject]@{schemaVersion=1;taskId=$TaskId;checks=@([pscustomobject]@{id='fixture';expected=$true;evidencePath='evidence.json';valuePath='verified'})}
    $accepted=[pscustomobject]@{schemaVersion=1;taskId=$TaskId;checks=@([pscustomobject]@{id='fixture';expected=$true;actual=$true;passed=$true;evidencePath='evidence.json';valuePath='verified'})}
    [IO.File]::WriteAllText($contract,($required|ConvertTo-Json -Depth 8),$utf8)
    [IO.File]::WriteAllText((Join-Path $output 'evidence.json'),([pscustomobject]@{taskId=$TaskId;verified=$true}|ConvertTo-Json),$utf8)
    $acceptance=Join-Path $output 'acceptance.json'
    [IO.File]::WriteAllText($acceptance,($accepted|ConvertTo-Json -Depth 8),$utf8)
    return $acceptance
}
