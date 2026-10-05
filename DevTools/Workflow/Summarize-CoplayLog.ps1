[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TaskId,
    [Parameter(Mandatory)][int]$Attempt,
    [Parameter(Mandatory)][DateTimeOffset]$From,
    [Parameter(Mandatory)][DateTimeOffset]$To,
    [Parameter(Mandatory)][string]$LogPath
)
$ErrorActionPreference='Stop'
if(-not(Test-Path -LiteralPath $LogPath -PathType Leaf)){
    [pscustomobject]@{taskId=$TaskId;attempt=$Attempt;coverage='UNAVAILABLE';requestCount=$null;resultUtf8Bytes=$null;reason='No bound Coplay MCP log was available.'}|ConvertTo-Json -Compress
    return
}
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$requests=0L;$bytes=0L;$rejected=0L
foreach($line in [IO.File]::ReadLines([IO.Path]::GetFullPath($LogPath))){
    if(-not $line){continue}
    try{$row=$line|ConvertFrom-Json -DateKind String}catch{$rejected++;continue}
    if([string]$row.taskId -cne $TaskId -or [int]$row.attempt -ne $Attempt){continue}
    if(-not $row.requestId -or -not $row.eventAt -or $null -eq $row.response){
        $rejected++;continue
    }
    $at=[DateTimeOffset]::Parse([string]$row.eventAt)
    if($at -lt $From -or $at -gt $To){continue}
    if(-not $seen.Add([string]$row.requestId)){continue}
    $requests++
    $bytes += [Text.Encoding]::UTF8.GetByteCount([string]$row.response)
}
[pscustomobject]@{taskId=$TaskId;attempt=$Attempt;coverage='EXACT_TAGGED_REQUEST_IDS';requestCount=$requests;resultUtf8Bytes=$bytes;rejectedUnbound=$rejected;sourceSha256=(Get-FileHash -LiteralPath $LogPath -Algorithm SHA256).Hash}|ConvertTo-Json -Compress
