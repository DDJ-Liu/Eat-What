[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ThreadId,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [object[]]$Records,
    [Nullable[long]]$FreshBytes,
    [string]$FreshBytesObservedAt
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$items = if ($PSBoundParameters.ContainsKey('Records')) { @($Records) } else { @(Read-TaskUsageRecords -RuntimeRoot $RuntimeRoot) }
$latest = @($items | Where-Object {
    $_.source -eq 'codex-provider-token_usage_record' -and
    [string]$_.metadata.threadId -ceq $ThreadId -and
    $null -ne $_.inputTokens -and [string]$_.metadata.responseId
} | Sort-Object @{Expression={ [DateTimeOffset]::Parse([string]$_.recordedAt) };Descending=$true},
    @{Expression={ [string]$_.metadata.responseId };Descending=$true} | Select-Object -First 1)
$tokens=$null; $source='unknown'; $method=$null; $observedAt=$null; $responseId=$null; $reason=$null
if($latest.Count -gt 0){
    $record=$latest[0]
    $tokens=[long]$record.inputTokens
    $source='provider'
    $method='latest_exact_thread_provider_response_input_tokens'
    $observedAt=[string]$record.recordedAt
    $responseId=[string]$record.metadata.responseId
    $reason='This is one provider response input, not a complete live context-window reading.'
} elseif($null -ne $FreshBytes -and $FreshBytes -ge 0){
    $tokens=[long][Math]::Ceiling([double]$FreshBytes/4.0)
    $source='estimated'
    $method='fresh_utf8_bytes_divided_by_four'
    $observedAt=$FreshBytesObservedAt
    $reason='Rough magnitude from explicitly supplied fresh bytes; tokenization and retained context are not measured.'
} else {
    $reason='No exact associated provider response and no fresh byte observation were available.'
}
$warning='NONE'
if($null -ne $tokens){
    if($tokens -ge 200000){$warning='STRONG'}
    elseif($tokens -ge 150000){$warning='ADVISE'}
}
[pscustomobject]@{
    threadId=$ThreadId;contextTokens=$tokens;contextSource=$source;contextWarning=$warning
    observedAt=$observedAt;method=$method;responseId=$responseId;reason=$reason
} | ConvertTo-Json -Depth 5 -Compress
