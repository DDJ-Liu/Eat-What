[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ThreadId,
    [string]$RuntimeRoot = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'),
    [object[]]$Records,
    [Nullable[long]]$FreshBytes,
    [string]$FreshBytesObservedAt,
    [string]$NativeRoot = (Join-Path $env:USERPROFILE '.codex/sessions'),
    [DateTimeOffset]$AsOf = [DateTimeOffset]::Now,
    [ValidateRange(1,120)][int]$MaxAgeMinutes = 30
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'UsageMetrics.ps1')
$utf8=[Text.UTF8Encoding]::new($false)
$maxTailBytes=1048576
function Valid-Time([string]$Text) {
    $date=[DateTimeOffset]::MinValue
    if(-not [DateTimeOffset]::TryParse($Text,[ref]$date)){return $null}
    if($date -gt $AsOf.AddMinutes(5) -or $date -lt $AsOf.AddMinutes(-$MaxAgeMinutes)){return $null}
    return $date
}
function Read-Segment([string]$Path,[long]$Start,[int]$Limit) {
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try {
        $length=$stream.Length
        $startAt=[Math]::Max(0,[Math]::Min($Start,$length))
        if($length-$startAt -gt $Limit){$startAt=$length-$Limit}
        [void]$stream.Seek($startAt,[IO.SeekOrigin]::Begin)
        $bytes=[byte[]]::new([int]($length-$startAt))
        $read=$stream.Read($bytes,0,$bytes.Length)
        if($read -eq 0){return [pscustomobject]@{lines=@();offset=$startAt;length=$length}}
        $lastNewline=[Array]::LastIndexOf($bytes,[byte]10,$read-1,$read)
        if($lastNewline -lt 0){return [pscustomobject]@{lines=@();offset=$startAt;length=$length}}
        $first=0
        if($startAt -gt $Start){
            $first=[Array]::IndexOf($bytes,[byte]10,0,$lastNewline+1)+1
        }
        $whole=$utf8.GetString($bytes,$first,$lastNewline+1-$first)
        $lines=@($whole -split "`n")
        return [pscustomobject]@{lines=$lines;offset=($startAt+$lastNewline+1);length=$length}
    } finally {$stream.Dispose()}
}
function Read-NativeMeta([string]$Path) {
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try {
        $bytes=[byte[]]::new([int][Math]::Min($stream.Length,524288))
        $read=$stream.Read($bytes,0,$bytes.Length)
        $text=$utf8.GetString($bytes,0,$read)
        $end=$text.IndexOf("`n")
        if($end -lt 0){return $false}
        $meta=$text.Substring(0,$end)|ConvertFrom-Json -DateKind String
        return ($meta.type -ceq 'session_meta' -and [string]$meta.payload.id -ceq $ThreadId)
    } finally {$stream.Dispose()}
}
function Find-NativePath([string]$Root) {
    if(-not [IO.Directory]::Exists($Root)){return $null}
    $dirs=[Collections.Generic.Stack[string]]::new()
    $dirs.Push($Root)
    $visited=0
    $candidateFiles=[Collections.Generic.List[IO.FileInfo]]::new()
    while($dirs.Count -gt 0){
        if(++$visited -gt 4096){throw 'Native session discovery exceeded 4096 directories.'}
        $dir=$dirs.Pop()
        foreach($file in [IO.Directory]::EnumerateFiles($dir,"*$ThreadId*.jsonl")){
            $info=[IO.FileInfo]::new($file)
            if($info.Name -match ('(?i)(?:^|[-_])'+[regex]::Escape($ThreadId)+'(?:_|\.jsonl$)')){
                $candidateFiles.Add($info)
                if($candidateFiles.Count -gt 32){throw 'Native session discovery exceeded 32 matching files.'}
            }
        }
        foreach($child in [IO.Directory]::EnumerateDirectories($dir)){$dirs.Push($child)}
    }
    return @($candidateFiles|Sort-Object LastWriteTimeUtc -Descending|Select-Object -First 1)[0]
}
function Get-NativeSample {
    if($ThreadId -notmatch '^[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$'){return $null}
    $root=[IO.Path]::GetFullPath($NativeRoot)
    $cacheDir=Join-Path $RuntimeRoot 'context-native-cache'
    $cachePath=Join-Path $cacheDir "$ThreadId.json"
    $cache=$null
    if([IO.File]::Exists($cachePath)){
        try{$cache=[IO.File]::ReadAllText($cachePath)|ConvertFrom-Json -DateKind String}catch{$cache=$null}
    }
    $path=$null
    $didDiscover=$false
    $discoveryAt=[DateTimeOffset]::MinValue
    if($cache -and [DateTimeOffset]::TryParse([string]$cache.discoveredAt,[ref]$discoveryAt) -and
        $discoveryAt -le $AsOf -and $discoveryAt -ge $AsOf.AddMinutes(-1) -and
        [string]$cache.path -and [IO.File]::Exists([string]$cache.path)){
        $path=[string]$cache.path
    } else {
        $didDiscover=$true
        $file=Find-NativePath $root
        if($file){$path=$file.FullName}
    }
    if(-not $path){return $null}
    $full=[IO.Path]::GetFullPath($path)
    if(-not $full.StartsWith($root.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
        throw 'Cached session path escaped NativeRoot.'
    }
    if(-not (Read-NativeMeta $full)){throw 'Native session metadata does not match the exact thread ID.'}
    $same=($cache -and [string]$cache.path -ceq $full)
    $offset=if($same){[long]$cache.offset}else{0L}
    $length=([IO.FileInfo]::new($full)).Length
    $sample=if($cache){$cache.sample}else{$null}
    if($same -and $length -lt $offset){$offset=0;$sample=$null}
    $segment=Read-Segment $full $offset $maxTailBytes
    $position=$segment.offset
    foreach($line in $segment.lines){
        if([string]::IsNullOrWhiteSpace($line)){continue}
        try{$entry=$line|ConvertFrom-Json -DateKind String}catch{continue}
        $value=0L
        $id=$null
        $exact=$false
        if($entry.type -ceq 'token_usage_record' -and [string]$entry.payload.thread_id -ceq $ThreadId -and
            [string]$entry.payload.response_id){
            $exact=$true
            $id=[string]$entry.payload.response_id
            $raw=[string]$entry.payload.usage.input_tokens
        } elseif($entry.type -ceq 'event_msg' -and $entry.payload.type -ceq 'token_count'){
            $raw=[string]$entry.payload.info.last_token_usage.input_tokens
            $id=('native-event:{0}:{1}' -f $ThreadId,$entry.timestamp)
        } else {continue}
        if(-not [long]::TryParse($raw,[ref]$value) -or $value -le 0){continue}
        $at=Valid-Time ([string]$entry.timestamp)
        if(-not $at){continue}
        if($sample -and ([DateTimeOffset]$at -lt [DateTimeOffset]::Parse([string]$sample.observedAt))){continue}
        if($sample -and $sample.exact -and -not $exact -and
            ([DateTimeOffset]$at - [DateTimeOffset]::Parse([string]$sample.observedAt)).TotalMinutes -lt 2){continue}
        $sample=[pscustomobject]@{contextTokens=$value;observedAt=$at.ToString('o');
            responseId=$id;path=$full;exact=$exact}
    }
    [void][IO.Directory]::CreateDirectory($cacheDir)
    $discoveredAt=if($didDiscover){$AsOf.ToString('o')}else{[string]$cache.discoveredAt}
    $next=[pscustomobject]@{path=$full;offset=$position;discoveredAt=$discoveredAt;sample=$sample}
    $tmp="$cachePath.pending-$([guid]::NewGuid().ToString('N'))"
    [IO.File]::WriteAllText($tmp,($next|ConvertTo-Json -Depth 6),$utf8)
    [IO.File]::Move($tmp,$cachePath,$true)
    if($sample -and (Valid-Time ([string]$sample.observedAt))){return $sample}
    return $null
}
$native=$null;$nativeError=$null
$productionRuntime=[IO.Path]::GetFullPath((Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.ai-workspace/runtime'))
$nativeAllowed=$PSBoundParameters.ContainsKey('NativeRoot') -or
    ([IO.Path]::GetFullPath($RuntimeRoot) -eq $productionRuntime)
if(-not $PSBoundParameters.ContainsKey('Records') -and $nativeAllowed){
    try{$native=Get-NativeSample}catch{$nativeError=$_.Exception.Message}
}
if($PSBoundParameters.ContainsKey('Records')){$items=@($Records)}
elseif($native){$items=@()}
else {
    $items=@(foreach($month in @($AsOf.AddMonths(-1),$AsOf)){
        $usagePath=Join-Path $RuntimeRoot ("usage-{0}.jsonl" -f $month.ToString('yyyyMM'))
        if(-not [IO.File]::Exists($usagePath)){continue}
        $usageSegment=Read-Segment $usagePath 0 $maxTailBytes
        foreach($line in $usageSegment.lines){
            try{$line|ConvertFrom-Json -DateKind String}catch{}
        }
    })
}
$provider=@($items|Where-Object{
    $_.source -eq 'codex-provider-token_usage_record' -and [string]$_.metadata.threadId -ceq $ThreadId -and
    $null -ne $_.inputTokens -and [string]$_.metadata.responseId -and (Valid-Time ([string]$_.recordedAt))
}|Sort-Object @{Expression={[DateTimeOffset]::Parse([string]$_.recordedAt)};Descending=$true}|Select-Object -First 1)
$tokens=$null; $source='unknown'; $method=$null; $observedAt=$null; $responseId=$null; $reason=$null
$selected=$null
if($native){$selected=[pscustomobject]@{tokens=$native.contextTokens;at=$native.observedAt;id=$native.responseId;method='native_exact_thread_last_response_input_tokens'}}
if($provider.Count -gt 0 -and (-not $selected -or [DateTimeOffset]::Parse([string]$provider[0].recordedAt) -gt [DateTimeOffset]::Parse([string]$selected.at))){
    $selected=[pscustomobject]@{tokens=$provider[0].inputTokens;at=$provider[0].recordedAt;id=$provider[0].metadata.responseId;method='latest_exact_thread_provider_response_input_tokens'}
}
if($selected){
    $tokens=[long]$selected.tokens;$source='provider';$method=$selected.method
    $observedAt=[string]$selected.at;$responseId=[string]$selected.id
    $reason='One response input-token sample, not the complete live context window or task billing.'
} elseif($null -ne $FreshBytes -and $FreshBytes -ge 0 -and (Valid-Time $FreshBytesObservedAt)){
    $tokens=[long][Math]::Ceiling([double]$FreshBytes/4.0)
    $source='estimated';$method='fresh_utf8_bytes_divided_by_four';$observedAt=$FreshBytesObservedAt
    $reason='Rough magnitude from fresh bytes; tokenization and retained context are not measured.'
} else {
    $reason=if($nativeError){"Native sample unavailable: $nativeError"}else{'No fresh exact response or valid fresh-byte observation was available.'}
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
