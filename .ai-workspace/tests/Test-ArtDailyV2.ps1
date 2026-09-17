[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$source=Split-Path -Parent $PSScriptRoot
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatArtDailyV2-'+[guid]::NewGuid().ToString('N'))
foreach($dir in @('runtime','inputs/art','source','templates')){$null=New-Item -ItemType Directory -Path (Join-Path $fixture $dir) -Force}
foreach($name in @('queue.ps1','supervised.ps1','art-audit.ps1','art-audit-scan.ps1','art-daily-report.ps1','art-daily-png.cs','art-daily-control.ps1','Invoke-ArtAudit.ps1')){Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $fixture $name)}
foreach($name in @('ART_DAILY_CONFIG.json','ART_PENDING_ITEMS.json')){Copy-Item -LiteralPath (Join-Path $source ('inputs/art/'+$name)) -Destination (Join-Path $fixture ('inputs/art/'+$name))}
$utf8=New-Object Text.UTF8Encoding($false)
function Json($Path,$Data){[IO.File]::WriteAllText((Join-Path $fixture $Path),(ConvertTo-Json -InputObject $Data -Depth 30),$utf8)}
function Q([hashtable]$Arguments){& (Join-Path $fixture 'queue.ps1') @Arguments | Out-String | ConvertFrom-Json}
$script:n=0
function Check($Actual,$Expected,$Name){if($Actual -ne $Expected){throw "$Name expected=$Expected actual=$Actual"};$script:n++}
function Throws([scriptblock]$Body,$Name){$thrown=$false;try{& $Body | Out-Null}catch{$thrown=$true};Check $thrown $true $Name}
$workers=@{};foreach($p in @('CODE','ENGINE_MCP','ART_AIGC')){$workers[$p]=@{status='IDLE';activeTaskId=$null;lastPollAt=$null;leaseUntil=$null;circuit=@{state='CLOSED';openUntil=$null;reason=$null}}}
Json 'runtime/worker-state.json' @{schemaVersion=1;workers=$workers}
Json 'runtime/queue-state.json' @{schemaVersion=1;nextSequence=1;tasks=@()}
Json 'runtime/mode-state.json' @{schemaVersion=1;mode='MANUAL';updatedAt='2026-09-08T00:00:00Z';updatedBy='TEST';automatic=@{status='IDLE'}}
[IO.File]::WriteAllText((Join-Path $fixture 'INSTRUCTIONS.md'),'# fixture',$utf8)
. (Join-Path $fixture 'art-audit-scan.ps1')
. (Join-Path $fixture 'art-daily-report.ps1')
$art=Join-Path $fixture 'source'
Initialize-ArtPngReader $fixture
function Png($Name,$W,$H,$Rect,$Color){
    $bmp=New-Object Drawing.Bitmap($W,$H)
    try{foreach($y in $Rect[1]..($Rect[1]+$Rect[3]-1)){foreach($x in $Rect[0]..($Rect[0]+$Rect[2]-1)){$bmp.SetPixel($x,$y,$Color)}};$bmp.Save((Join-Path $art $Name),[Drawing.Imaging.ImageFormat]::Png)}finally{$bmp.Dispose()}
}
Png 'ui_shicai_test.png' 20 20 @(3,4,10,10) ([Drawing.Color]::Red)
Png 'rename-old.png' 12 12 @(0,0,12,12) ([Drawing.Color]::Blue)
Png 'move.png' 12 12 @(0,0,12,12) ([Drawing.Color]::Green)
$files=@(Get-ArtInventory $art);Add-ArtPngMeasurements $files $null $art $fixture
Check $files[2].png.visibleWidth 10 'alpha bbox width'
Check $files[2].png.x 3 'alpha bbox origin'
Check $files[2].png.ratio 1 'alpha ratio not canvas alias'
$seed=@{schemaVersion=2;auditId='AI-000057/fixture-sync';sourcePath=$art;capturedAt=[DateTimeOffset]::Now.ToString('o');complete=$true;files=$files;pendingItems=@();redFiles=@()}
Json 'inputs/art/AI000057_manifest.json' $seed
Png 'ui_shicai_test.png' 20 20 @(3,4,5,10) ([Drawing.Color]::Red)
Move-Item -LiteralPath (Join-Path $art 'rename-old.png') -Destination (Join-Path $art 'rename-new.png')
$null=New-Item -ItemType Directory -Path (Join-Path $art 'moved')
Move-Item -LiteralPath (Join-Path $art 'move.png') -Destination (Join-Path $art 'moved/move.png')
Png 'bad.png' 10 10 @(0,0,10,10) ([Drawing.Color]::Black)
[IO.File]::WriteAllText((Join-Path $art 'Thumbs.db'),'ignored',$utf8)
Q @{Action='RegisterArtAssetLane';PipelineThreadId='fixture-art';AuditSourcePath=$art} | Out-Null
$queueHash=(Get-FileHash -LiteralPath (Join-Path $fixture 'runtime/queue-state.json')).Hash
$before=Get-ArtInventoryDigest @(Get-ArtInventory $art)
$result=& (Join-Path $fixture 'Invoke-ArtAudit.ps1') -Baseline | Out-String | ConvertFrom-Json
Check $result.action 'ART_AUDIT_SUCCEEDED' 'v2 end to end'
Check $result.controllerMessage $null 'daily no longer sends controller callback'
Check $result.controllerThreadId $null 'disabled reception exposes no controller target'
Check (Get-ArtDailyControllerHandoff ([pscustomobject]@{controllerThreadId='legacy'}) 'ART-DAILY-20260909' 'report.md') $null 'legacy config without opt-in stays local'
$optIn=Get-ArtDailyControllerHandoff ([pscustomobject]@{controllerThreadId='explicit';controllerReceptionEnabled=$true}) 'ART-DAILY-20260909' 'report.md'
Check $optIn.threadId 'explicit' 'explicit future opt-in remains possible'
Check ($result.auditId -match '^ART-DAILY-\d{8}$') $true 'new id contract'
Check (Get-FileHash -LiteralPath (Join-Path $fixture 'runtime/queue-state.json')).Hash $queueHash 'daily never allocates AI id'
Check (Get-ArtInventoryDigest @(Get-ArtInventory $art)) $before 'source bytes and times unchanged'
$m=Get-Content -LiteralPath $result.manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$folder=Split-Path -Parent $result.manifestPath
foreach($f in @('report.md','manifest.json','diff.csv','dims.csv','triggers.json')){Check (Test-Path -LiteralPath (Join-Path $folder $f)) $true ('artifact '+$f)}
$diff=Get-Content -LiteralPath (Join-Path $folder 'changes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Check @($diff | Where-Object kind -eq '改名').Count 1 'rename counted once'
Check @($diff | Where-Object kind -eq '目录移动').Count 1 'move counted once'
Check @($diff | Where-Object kind -eq '新增').Count 1 'rename move and system not additions'
Check @($diff | Where-Object kind -eq '删除').Count 0 'rename move not deletions'
Check @($m.redFiles).Count 2 'ratio and black are red'
$report=Get-Content -LiteralPath $result.reportPath -Raw -Encoding UTF8
foreach($i in 1..7){Check ($report -match ('## '+$i+'\.')) $true ('section '+$i)}
Check ($report -match '修改 1') $true 'report counters use correct scope'
Check ($m.baselinePath -like '*AI000057_manifest.json') $true 'explicit sync seed is used'
Check @($m.pendingItems).Count 11 'fixed pending rows including v2.4 unknown product'
Check @($m.pendingItems | Where-Object id -eq 'prd_unknown').Count 1 'unknown product remains a single tracked pending item'
$status=Q @{Action='ArtDailyControlStatus'}
Check @($status.reports).Count 1 'controller receives unreviewed report'
Throws {Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='blocked';Instruction='fixture';ArtDailyIds=@($m.auditId);ArtSourcePaths=@('ui_shicai_test.png')}} 'red blocks enqueue'
Throws {Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='missing paths';Instruction='fixture';ArtDailyIds=@($m.auditId)}} 'daily deployment requires exact files'
$sync=Q @{Action='Enqueue';Pipeline='ART';Title='sync';Instruction='fixture';ArtDailyIds=@($m.auditId);ArtTriggerKey='fixture:sync'}
Check $sync.task.pipeline 'ART_ASSET' 'ART alias preserves existing lane'
Check (Q @{Action='Enqueue';Pipeline='ART';Title='sync repeat';Instruction='fixture';ArtDailyIds=@($m.auditId);ArtTriggerKey='fixture:sync'}).action 'ALREADY_ENQUEUED_ART_TRIGGER' 'repeated report dedupe'
Q @{Action='ApproveArtDailyFiles';AuditId=$m.auditId;ArtSourcePaths=@('ui_shicai_test.png');UserDecision='fixture explicit user review';Verification='fixture evidence'} | Out-Null
$engine=Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='reviewed';Instruction='fixture';ArtDailyIds=@($m.auditId);ArtSourcePaths=@('ui_shicai_test.png')}
Check $engine.action 'ENQUEUED' 'exact reviewed red may enqueue'
# A changed hash cannot reuse the approval; recheck an already queued task in every dispatch mode.
$approvedHash=$m.redFiles[0].sha256
$redEntry=@($m.redFiles | Where-Object path -eq 'ui_shicai_test.png')[0]
$savedHash=$redEntry.sha256
$redEntry.sha256='fixture-new-unapproved-sha'
Json ($result.manifestPath.Substring($fixture.Length+1)) $m
Check (Q @{Action='Poll';Pipeline='ENGINE_MCP';TaskId=$engine.task.id;TriggerSource='MANUAL'}).action 'ART_DEPLOYMENT_BLOCKED' 'queued task rechecks changed red SHA'
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($engine.task.id);ControllerThreadId='fixture-controller'} | Out-Null
$supervised=Q @{Action='SupervisionStatus'}
Check (@($supervised.tasks[0].blockers) -match 'ART_RED_PENDING_USER').Count 1 'supervision respects red gate'
Check @((Q @{Action='DispatchSupervision';IdlePipelines=@('ENGINE_MCP')}).dispatches).Count 0 'supervision does not dispatch blocked asset'
Q @{Action='ExitSupervision';Reason='fixture end'} | Out-Null
$redEntry.sha256=$savedHash
Json ($result.manifestPath.Substring($fixture.Length+1)) $m
Check ($report -match '不可比较') $true 'unknown historical ratio explicitly marked'
$noDiff=@(Get-ArtDailyDiff $m.files $m.files)
Check $noDiff.Count 0 'unchanged day diff empty'
$noAnalysis=Get-ArtDailyAnalysis $m $m $noDiff (Read-ArtDailyConfig $fixture) (Get-Content -LiteralPath (Join-Path $fixture 'inputs/art/ART_PENDING_ITEMS.json') -Raw -Encoding UTF8 | ConvertFrom-Json)
Check @($noAnalysis.dims).Count 0 'unchanged PNG excluded from regression table'
Check @($noAnalysis.redFiles).Count 2 'unchanged day retains red evidence'
Check @($noAnalysis.pending | Where-Object id -eq 'split')[0].ready $false 'missing E6 evidence cannot unlock'
Write-ArtDailyCsv (Join-Path $fixture 'empty.csv') @() @('path','ratio')
Check ((Get-Content -LiteralPath (Join-Path $fixture 'empty.csv') -Raw -Encoding UTF8).Trim()) '"path","ratio"' 'empty CSV still has headers'
Q @{Action='RecordArtDailyReview';AuditId=$m.auditId;Summary='fixture handled';Verification='read sections and links';TaskIds=@($sync.task.id)} | Out-Null
Check @((Q @{Action='ArtDailyControlStatus'}).reports).Count 0 'review receipt dedupe'
Throws {Q @{Action='Enqueue';Pipeline='ENGINE_MCP';Title='unknown';Instruction='fixture';ArtDailyIds=@('ART-DAILY-19990101');ArtSourcePaths=@('good.png')}} 'missing daily not accepted'
$workspaceRoot=$fixture
function Read-JsonFile($Path){Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json}
. (Join-Path $fixture 'art-audit.ps1')
. (Join-Path $fixture 'art-daily-control.ps1')
$s=Read-ArtAuditState
$s.jobs+=,[pscustomobject]@{id='ART-DAILY-20200101';status='PENDING';scheduledDates=@('2020-01-01');deferReasons=@('前序任务 AI-fixture');snapshotPath=$null}
Json 'runtime/art-audit-state.json' $s
Check @((Get-ArtDailyControlStatus ([DateTimeOffset]::Parse('2020-01-01T12:01:00+08:00'))).alerts).Count 1 'over 12h user notice'
[pscustomobject]@{result='PASS';checks=$script:n;fixture=$fixture;report=$result.reportPath} | ConvertTo-Json
