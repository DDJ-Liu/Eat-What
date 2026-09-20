[CmdletBinding()]
param([string]$EditorPath)
$ErrorActionPreference='Stop'
$project='D:/GameProject/Eat-What-U6'
$version='6000.3.24f1'
if([string]::IsNullOrWhiteSpace($EditorPath)){
    $hub=Join-Path $env:APPDATA 'UnityHub/editors-v2.json'
    if(Test-Path -LiteralPath $hub){
        $editors=(Get-Content -LiteralPath $hub -Raw | ConvertFrom-Json).data
        $match=@($editors | Where-Object {$_.version -eq $version})
        if($match.Count -gt 0){$EditorPath=@($match[0].location)[0]}
    }
}
if([string]::IsNullOrWhiteSpace($EditorPath) -or -not(Test-Path -LiteralPath $EditorPath -PathType Leaf)){throw "Install Unity $version first, or pass -EditorPath pointing to its Editor/Unity.exe"}
$actual=(Get-Item -LiteralPath $EditorPath).VersionInfo.ProductVersion
if(-not $actual.StartsWith($version,[StringComparison]::Ordinal)){throw "Wrong Editor version: $actual; expected $version"}
$branch=(git -C $project branch --show-current).Trim()
if($LASTEXITCODE -ne 0 -or $branch -ne 'migration/unity-6.3'){throw 'Migration branch verification failed'}
$dirty=@(git -C $project status --porcelain=v1)
if($LASTEXITCODE -ne 0 -or $dirty.Count -gt 0){throw 'Migration worktree has changes: preserve them and ask the migration task to inspect before this first-import launcher runs'}
if(Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile')){throw 'Migration Editor lock exists; do not launch another instance'}
$out=Join-Path $project '.ai-workspace/outputs/MIG63/M1'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$log=Join-Path $out ("Editor-first-import-$stamp.log")
$head=(git -C $project rev-parse HEAD).Trim()
[pscustomobject]@{startedAt=(Get-Date -Format o);editor=$EditorPath;version=$actual;project=$project;branch=$branch;beforeSha=$head;log=$log;mode='User-invoked interactive Editor, not batchmode'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out ("first-open-$stamp.json")) -Encoding utf8
$arguments=@('-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'))
Write-Host "Opening migration project with Unity $version. Log: $log"
Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Normal
