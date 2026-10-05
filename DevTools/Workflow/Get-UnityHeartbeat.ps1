[CmdletBinding()]
param(
    [string]$Endpoint='http://localhost:8080/mcp',
    [string]$ExpectedDataPath=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'Assets'),
    [string]$ExpectedVersion='6000.3.24f1',
    [string]$StateRoot=(Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference='Stop'
$utf8=[Text.UTF8Encoding]::new($false)
$requests=[Collections.Generic.List[object]]::new()
$headers=@{Accept='application/json, text/event-stream'}
$script:rpcId=0
function Rpc([string]$Method,$Params,[switch]$Notification){
    $body=[ordered]@{jsonrpc='2.0';method=$Method;params=$Params}
    if(-not $Notification){$script:rpcId++;$body.id=$script:rpcId}
    $payload=$body|ConvertTo-Json -Depth 15 -Compress
    $r=Invoke-WebRequest -Uri $Endpoint -Method Post -ContentType 'application/json' -Headers $headers -Body $utf8.GetBytes($payload) -TimeoutSec 12
    if($r.Headers['Mcp-Session-Id']){$headers['Mcp-Session-Id']=[string]@($r.Headers['Mcp-Session-Id'])[0]}
    $raw=[string]$r.Content
    $requests.Add(@{method=$Method;request=$body;requestUtf8Bytes=$utf8.GetByteCount($payload);responseUtf8Bytes=$utf8.GetByteCount($raw);response=$raw})
    if($Notification){return}
    $reply=if($raw.TrimStart().StartsWith('{')){$raw|ConvertFrom-Json -DateKind String}else{
        @($raw -split '\r?\n'|Where-Object {$_ -like 'data:*'}|ForEach-Object {$_.Substring(5).Trim()|ConvertFrom-Json -DateKind String}|Where-Object {$_.id -eq $script:rpcId})[-1]
    }
    if($null -eq $reply -or $reply.error){throw "MCP $Method failed: $raw"}
    if($reply.result.isError){throw "MCP $Method returned tool error: $raw"}
    return $reply.result
}
function Body($Result){
    if($Result.structuredContent){return $Result.structuredContent}
    $c=if($Result.contents){@($Result.contents)}else{@($Result.content)}
    $text=@($c|Where-Object {$_.text}|Select-Object -First 1)[0].text
    if(-not $text){throw 'MCP response has no JSON content.'}
    return $text|ConvertFrom-Json -DateKind String
}
$sample=[ordered]@{schemaVersion=1;source='coplay-mcp-streamable-http';observedAt=[DateTimeOffset]::Now.ToString('o');taskId=$null;executionId=$null;available=$false;instanceId=$null;dataPath=$null;unityVersion=$null;isPlaying=$null;isCompiling=$null;sceneDirty=$null;scenePath=$null;unavailableReason=$null;sceneDirtyUnavailableReason=$null;rawPath="$OutputPath.raw.json";requestCount=0;requestUtf8Bytes=0;responseUtf8Bytes=0}
try{
    $uri=[uri]$Endpoint
    if(-not $uri.IsLoopback -or $uri.Scheme -ne 'http' -or $uri.Port -ne 8080 -or $uri.AbsolutePath.TrimEnd('/') -cne '/mcp'){throw 'Only the configured Coplay localhost:8080/mcp transport is allowed.'}
    $workers=Get-Content -LiteralPath (Join-Path $StateRoot 'worker-state.json') -Raw|ConvertFrom-Json
    $sample.taskId=$workers.workers.ENGINE_MCP.activeTaskId;$sample.executionId=$workers.workers.ENGINE_MCP.activeExecutionId
    $init=Rpc 'initialize' @{protocolVersion='2025-03-26';capabilities=@{};clientInfo=@{name='EatWhat-WATCH';version='PRE5c'}}
    $headers['MCP-Protocol-Version']=[string]$init.protocolVersion
    Rpc 'notifications/initialized' @{} -Notification
    $instances=Body (Rpc 'resources/read' @{uri='mcpforunity://instances'})
    $matches=@()
    foreach($inst in @($instances.instances|Where-Object {$_.unity_version -eq $ExpectedVersion})){
        $selected=Body (Rpc 'tools/call' @{name='set_active_instance';arguments=@{instance=$inst.id}})
        if($selected.success -eq $false){throw 'Instance selection failed.'}
        $info=Body (Rpc 'resources/read' @{uri='mcpforunity://project/info'})
        if($info.success -eq $true -and [IO.Path]::GetFullPath([string]$info.data.assetsPath).TrimEnd('\','/') -ieq [IO.Path]::GetFullPath($ExpectedDataPath).TrimEnd('\','/') -and $info.data.unityVersion -ceq $ExpectedVersion){
            $matches+=@{instance=$inst;info=$info.data}
        }
    }
    if($matches.Count -ne 1){throw "Expected exactly one verified dataPath/version instance; found $($matches.Count)."}
    $match=$matches[0]
    $null=Body (Rpc 'tools/call' @{name='set_active_instance';arguments=@{instance=$match.instance.id}})
    $sample.instanceId=$match.instance.id;$sample.dataPath=$match.info.assetsPath;$sample.unityVersion=$match.info.unityVersion
    $state=Body (Rpc 'resources/read' @{uri='mcpforunity://editor/state'})
    if($state.success -ne $true -or $state.data.staleness.is_stale -ne $false){throw 'Editor state disconnected or stale; fields remain unavailable.'}
    if($state.data.unity.unity_version -cne $ExpectedVersion){throw 'Editor state version mismatch.'}
    $sample.isPlaying=$state.data.editor.play_mode.is_playing;$sample.isCompiling=$state.data.compilation.is_compiling
    $sample.scenePath=$state.data.editor.active_scene.path;$sample.available=$true
    # editor/state assets.external_changes_dirty is NOT a scene dirty flag.
    try{
        $scene=Body (Rpc 'tools/call' @{name='manage_scene';arguments=@{action='get_active'}})
        if($scene.success -ne $true -or $scene.data.path -cne $sample.scenePath -or $null -eq $scene.data.isDirty){throw 'Active scene dirty source unavailable or scene changed.'}
        $sample.sceneDirty=[bool]$scene.data.isDirty
    }catch{$sample.sceneDirtyUnavailableReason=$_.Exception.Message}
}catch{
    $sample.available=$false;$sample.isPlaying=$null;$sample.isCompiling=$null;$sample.sceneDirty=$null;$sample.unavailableReason=$_.Exception.Message
}
$sample.requestCount=$requests.Count
$sample.requestUtf8Bytes=[long](($requests|Measure-Object requestUtf8Bytes -Sum).Sum)
$sample.responseUtf8Bytes=[long](($requests|Measure-Object responseUtf8Bytes -Sum).Sum)
$full=[IO.Path]::GetFullPath($OutputPath)
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $full))
[IO.File]::WriteAllText("$full.raw.json",(@{observedAt=$sample.observedAt;requests=@($requests);error=$sample.unavailableReason}|ConvertTo-Json -Depth 30),$utf8)
$tmp="$full.pending-$([guid]::NewGuid().ToString('N'))"
[IO.File]::WriteAllText($tmp,($sample|ConvertTo-Json -Depth 10),$utf8);[IO.File]::Move($tmp,$full,$true)
[IO.File]::AppendAllText("$full.jsonl",(($sample|ConvertTo-Json -Compress)+[Environment]::NewLine),$utf8)
@{action='UNITY_HEARTBEAT_SAMPLED';available=$sample.available;reason=$sample.unavailableReason;outputPath=$full;requestCount=$sample.requestCount}|ConvertTo-Json -Compress
