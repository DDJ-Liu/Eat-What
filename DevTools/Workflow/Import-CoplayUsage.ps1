[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SamplePath,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$sample=Get-Content -LiteralPath $SamplePath -Raw|ConvertFrom-Json -DateKind String
if(-not $sample.taskId -or -not $sample.clientObservationId -or $sample.lane -ne 'CONTROL' -or
    [int]$sample.requestCount -ne 1 -or -not $sample.raw){throw 'Sample lacks an exact CONTROL observation.'}
$bytes=[Text.Encoding]::UTF8.GetByteCount((ConvertTo-Json -InputObject ([string]$sample.raw) -Compress))
if($bytes -ne [long]$sample.toolResultBytes){throw 'Observed UTF-8 bytes differ from source sample.'}
$existing=@(Read-TaskUsageRecords -RuntimeRoot $RuntimeRoot|Where-Object {
    $_.source -eq 'coplay-client-observation' -and $_.metadata.clientObservationId -eq $sample.clientObservationId
})
if($existing.Count -gt 0){
    [pscustomobject]@{action='COPLAY_ALREADY_IMPORTED';taskId=$sample.taskId;bytes=$bytes;count=1}|ConvertTo-Json -Compress
    return
}
$timeText=[string]$sample.recordedAt
if($timeText -match '^(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2}:\d{2}) UTC$'){$timeText="$($Matches[1])T$($Matches[2])Z"}
$at=[DateTimeOffset]::Parse($timeText)
$result=Write-TaskUsageRecord -TaskId ([string]$sample.taskId) -Lane CONTROL -Phase verify `
    -Event ([string]$sample.event) -ToolCallCount 1 -ToolResultBytes $bytes `
    -Source 'coplay-client-observation' -Coverage 'exact_client_tool_return_not_server_rpc' `
    -UnavailableReason @{inputTokens='MCP tool did not expose provider tokens.';outputTokens='MCP tool did not expose provider tokens.';model='CONTROL observation did not expose model.';cachedInputTokens='MCP tool did not expose provider tokens.';reasoningOutputTokens='MCP tool did not expose provider tokens.'} `
    -Metadata @{clientObservationId=[string]$sample.clientObservationId;serverRequestId=$null;serverRequestIdReason=[string]$sample.serverRequestIdUnavailableReason;sourceSha256=(Get-FileHash -LiteralPath $SamplePath -Algorithm SHA256).Hash;byteUnit=[string]$sample.byteUnit} `
    -RuntimeRoot $RuntimeRoot -RecordedAt $at
[pscustomobject]@{action='COPLAY_IMPORTED';taskId=$sample.taskId;bytes=$bytes;count=1;coverage='exact_client_tool_return_not_server_rpc'}|ConvertTo-Json -Compress
