[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatCoplay-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$utf8=[Text.UTF8Encoding]::new($false)
try {
    $script=Join-Path $PSScriptRoot 'Summarize-CoplayLog.ps1'
    $from=[DateTimeOffset]::Parse('2026-10-01T10:00:00+08:00')
    $to=$from.AddMinutes(10)
    $missing=& $script -TaskId AI-900001 -Attempt 1 -From $from -To $to -LogPath (Join-Path $root 'missing.jsonl')|ConvertFrom-Json
    if($missing.coverage -ne 'UNAVAILABLE' -or $null -ne $missing.requestCount -or $null -ne $missing.resultUtf8Bytes){throw 'Missing log must remain null.'}
    $path=Join-Path $root 'coplay.jsonl'
    $rows=@(
        @{taskId='AI-900001';attempt=1;requestId='r1';eventAt=$from.AddMinutes(1).ToString('o');response='{"ok":true}'},
        @{taskId='AI-900001';attempt=1;requestId='r1';eventAt=$from.AddMinutes(2).ToString('o');response='{"ok":true}'},
        @{taskId='AI-900001';attempt=1;requestId='r2';eventAt=$from.AddMinutes(3).ToString('o');response='中文'},
        @{taskId='AI-900002';attempt=1;requestId='r3';eventAt=$from.AddMinutes(3).ToString('o');response='ignore'},
        @{taskId='AI-900001';attempt=2;requestId='r4';eventAt=$from.AddMinutes(3).ToString('o');response='ignore'},
        @{taskId='AI-900001';attempt=1;requestId='r5';eventAt=$to.AddMinutes(1).ToString('o');response='ignore'}
    )
    [IO.File]::WriteAllLines($path,@($rows|ForEach-Object{$_|ConvertTo-Json -Compress}),$utf8)
    $result=& $script -TaskId AI-900001 -Attempt 1 -From $from -To $to -LogPath $path|ConvertFrom-Json
    $expected=[Text.Encoding]::UTF8.GetByteCount('{"ok":true}中文')
    if($result.coverage -ne 'EXACT_TAGGED_REQUEST_IDS' -or $result.requestCount -ne 2 -or $result.resultUtf8Bytes -ne $expected){throw 'Tagged/deduplicated count or UTF8 bytes incorrect.'}
    [pscustomobject]@{result='PASS';checks=5;count=$result.requestCount;bytes=$result.resultUtf8Bytes}|ConvertTo-Json -Compress
}
finally {
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if([IO.Path]::GetFullPath($root).StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -and
       (Split-Path -Leaf $root).StartsWith('EatWhatCoplay-',[StringComparison]::Ordinal)){
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
