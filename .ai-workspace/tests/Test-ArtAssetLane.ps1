[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$source=Split-Path -Parent $PSScriptRoot
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('EatWhatArtAudit-'+[guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path (Join-Path $fixture 'runtime') -Force
$art=Join-Path $fixture 'art-source';$null=New-Item -ItemType Directory -Path $art
$utf8=New-Object Text.UTF8Encoding($false)
foreach ($name in @('queue.ps1','supervised.ps1','art-audit.ps1','art-audit-scan.ps1','Invoke-ArtAudit.ps1')) {Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $fixture $name)}
function Save($Name,$Data) {[IO.File]::WriteAllText((Join-Path $fixture ('runtime/'+$Name+'-state.json')),($Data | ConvertTo-Json -Depth 30),$utf8)}
function Q([hashtable]$ArgsTable) { & (Join-Path $fixture 'queue.ps1') @ArgsTable | Out-String | ConvertFrom-Json }
$script:count=0
function Check($Actual,$Expected,$Name) {if ($Actual -ne $Expected){throw "$Name expected=$Expected actual=$Actual"};$script:count++}
function Throws([scriptblock]$Code,$Name) {$ok=$false;try {& $Code | Out-Null}catch{$ok=$true};Check $ok $true $Name}
function RequestBaseline {Q @{Action='RequestArtAudit';AuditId='ASSET-AUDIT-BASELINE'} | Out-Null}
function AddTask($Lane) {(Q @{Action='Enqueue';Pipeline=$Lane;Title='fixture';Instruction='fixture'}).task.id}
function Done($Id,$Lane) {Q @{Action='Complete';TaskId=$Id;Pipeline=$Lane;Summary='fixture';Verification='fixture'}}
function Scan {& (Join-Path $fixture 'Invoke-ArtAudit.ps1') | Out-String | ConvertFrom-Json}
$workers=@{}
foreach ($lane in @('CODE','ENGINE_MCP','ART_AIGC')) {$workers[$lane]=@{status='IDLE';activeTaskId=$null;lastPollAt=$null;leaseUntil=$null;circuit=@{state='CLOSED';openUntil=$null;reason=$null}}}
Save worker @{schemaVersion=1;workers=$workers}
Save queue @{schemaVersion=1;nextSequence=1;tasks=@()}
Save mode @{schemaVersion=1;mode='MANUAL';updatedAt='2026-09-08T00:00:00Z';updatedBy='TEST';automatic=@{chainId=$null;status='IDLE';createdAt=$null;completedAt=$null;taskIds=@();currentIndex=0;currentTaskId=$null;completedTaskIds=@();blockedTaskId=$null;lastError=$null}}
[IO.File]::WriteAllText((Join-Path $fixture 'INSTRUCTIONS.md'),'# fixture',$utf8)
$queueHash=(Get-FileHash -LiteralPath (Join-Path $fixture 'runtime/queue-state.json')).Hash
Q @{Action='RegisterArtAssetLane';PipelineThreadId='fixture-art-thread';AuditSourcePath=$art} | Out-Null
Check (Q @{Action='Status'}).workers.workers.ART_ASSET.status 'IDLE' 'fourth lane migration'
RequestBaseline; RequestBaseline
Check @((Q @{Action='ArtAuditStatus'}).state.jobs).Count 1 'daily dedupe'
[IO.File]::WriteAllText((Join-Path $art '猫[1].txt'),'cat',$utf8)
[IO.File]::WriteAllText((Join-Path $art 'old.txt'),'old',$utf8)
[IO.File]::WriteAllText((Join-Path $art '.hidden'),'hidden',$utf8)
$result=Scan
Check $result.action 'ART_AUDIT_SUCCEEDED' 'baseline succeeds'
Check $result.files 3 'hidden and literal bracket paths included'
Check $result.added 0 'baseline is not all-added'
Check (Get-FileHash -LiteralPath (Join-Path $fixture 'runtime/queue-state.json')).Hash $queueHash 'service never mutates numbered queue'
Check (Scan).action 'ART_AUDIT_EMPTY' 'duplicate trigger no scan'
Q @{Action='PauseArtAudit';Reason='fixture user pause'} | Out-Null
$pausedState=(Q @{Action='ArtAuditStatus'}).state
Check $pausedState.enabled $false 'service pause persists'
Check (Scan).action 'ART_AUDIT_PAUSED' 'paused service cannot scan'
Q @{Action='ResumeArtAudit';Reason='fixture user resume'} | Out-Null
Check (Q @{Action='ArtAuditStatus'}).state.enabled $true 'service resume persists'
. (Join-Path $fixture 'art-audit-scan.ps1')
$before=Get-Content -LiteralPath $result.snapshotPath -Raw -Encoding UTF8 | ConvertFrom-Json
[IO.File]::WriteAllText((Join-Path $art '猫[1].txt'),'dog',$utf8)
[IO.File]::SetLastWriteTimeUtc((Join-Path $art '猫[1].txt'),[datetime]::Parse($before.files[2].lastWriteUtc))
Move-Item -LiteralPath (Join-Path $art 'old.txt') -Destination (Join-Path $art 'moved.txt')
[IO.File]::WriteAllText((Join-Path $art 'new.txt'),'new',$utf8)
$diff=Compare-ArtInventory $before.files @(Get-ArtInventory $art) $true
Check @($diff.modified).Count 1 'content hash detects same length edit'
Check @($diff.added).Count 2 'added list'
Check @($diff.removed).Count 1 'removed list'
Check @($diff.renameCandidates).Count 1 'unique hash move candidate'
# Pure date helper: local Tuesday activation followed by Saturday catches Wed/Thu/Fri only.
$workspaceRoot=$fixture
function Read-JsonFile($Path) {Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json}
function Save-JsonFile($Path,$Data) {[IO.File]::WriteAllText($Path,($Data|ConvertTo-Json -Depth 30),$utf8)}
function Get-NowIso {[DateTimeOffset]::Now.ToString('o')}
. (Join-Path $fixture 'art-audit.ps1')
$state=Read-ArtAuditState;$state.scheduledThrough='2026-09-08';$state.jobs=@()
Sync-ArtAuditDue $state ([DateTimeOffset]::Parse('2026-09-12T00:00:00+08:00'))
Check ($state.jobs[0].scheduledDates -join ',') '2026-09-09,2026-09-10,2026-09-11' 'weekday midnight plus disclosed catchup'
$state.scheduledThrough=(Get-ArtAuditNow).ToString('yyyy-MM-dd');Save-ArtAuditState $state
$id=AddTask ART_ASSET
Check (Q @{Action='Poll';Pipeline='ART_ASSET';TaskId=$id;TriggerSource='MANUAL'}).action 'DEFERRED_ART_AUDIT' 'pending audit takes next idle slot'
$second=Scan
Check $second.modified 1 'full second report modified'
Check $second.removed 1 'full second report deleted'
Q @{Action='Poll';Pipeline='ART_ASSET';TaskId=$id;TriggerSource='MANUAL'} | Out-Null
$state=Read-ArtAuditState;Add-ArtAuditJob $state 'ASSET-AUDIT-TEST-BUSY' @();Save-ArtAuditState $state
Check (Scan).action 'ART_AUDIT_DEFERRED' 'audit waits for running business'
Check (Done $id ART_ASSET).artAuditHandoff.action 'ART_AUDIT_DUE' 'completion drains deferred service'
Check (Scan).action 'ART_AUDIT_SUCCEEDED' 'deferred audit proceeds after completion'
$state=Read-ArtAuditState;Add-ArtAuditJob $state 'ASSET-AUDIT-TEST-LOCK' @();Save-ArtAuditState $state
$claim=Q @{Action='BeginArtAudit';Verification='fixture idle'}
$aigc=AddTask ART_AIGC
Check (Q @{Action='Poll';Pipeline='ART_AIGC';TaskId=$aigc;TriggerSource='MANUAL'}).action 'DEFERRED_ART_AUDIT' 'daily locks art source across lanes'
Throws {Q @{Action='AbandonArtAudit';AuditId=$claim.job.id;Verification='fixture'}} 'active scan not orphaned by age'
Throws {Q @{Action='CompleteArtAudit';AuditId=$claim.job.id;AuditToken='wrong';SnapshotPath=$second.snapshotPath;ReportPath=$second.reportPath}} 'wrong token cannot publish'
$successfulBeforeFailure=(Read-ArtAuditState).lastSuccessfulSnapshot
Q @{Action='FailArtAudit';AuditId=$claim.job.id;AuditToken=$claim.job.token;ErrorMessage='fixture failure'} | Out-Null
Check (Read-ArtAuditState).lastSuccessfulSnapshot $successfulBeforeFailure 'failed scan retains baseline'
$state=Read-ArtAuditState;$last=$state.lastSuccessfulSnapshot;Add-ArtAuditJob $state 'ASSET-AUDIT-TEST-OFFLINE' @();Save-ArtAuditState $state
Move-Item -LiteralPath $art -Destination ($art+'-offline')
Throws {Scan} 'missing source fails closed'
Check (Read-ArtAuditState).lastSuccessfulSnapshot $last 'offline never becomes empty baseline'
Check (Read-ArtAuditState).jobs[-1].status 'FAILED' 'failure retained outside task queue'
Move-Item -LiteralPath ($art+'-offline') -Destination $art
# SUPERVISED reservation issued before daily request may finish; pending blocks new reservation.
$next=AddTask ART_ASSET
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($next);ControllerThreadId='test-control'} | Out-Null
$dispatch=Q @{Action='DispatchSupervision';IdlePipelines=@('ART_ASSET')}
Check $dispatch.dispatches[0].pipeline 'ART_ASSET' 'fourth lane supervised dispatch'
$state=Read-ArtAuditState;Add-ArtAuditJob $state 'ASSET-AUDIT-TEST-RESERVED' @();Save-ArtAuditState $state
Check (Scan).action 'ART_AUDIT_DEFERRED' 'daily waits existing reservation'
Check (Q @{Action='Poll';Pipeline='ART_ASSET';TaskId=$next;TriggerSource='SUPERVISED';DispatchToken=$dispatch.dispatches[0].token}).action 'CLAIMED' 'existing ticket not deadlocked by audit'
Done $next ART_ASSET | Out-Null
Check (Q @{Action='ModeStatus'}).mode.mode 'MANUAL' 'finished business scope exits without cancelling pending daily'
$third=AddTask ART_ASSET
Q @{Action='SetMode';Mode='SUPERVISED';TaskIds=@($third);ControllerThreadId='test-control'} | Out-Null
Check @((Q @{Action='DispatchSupervision';IdlePipelines=@('ART_ASSET')}).dispatches).Count 0 'no new business reservations while daily pending'
Scan | Out-Null
Check @((Q @{Action='DispatchSupervision';IdlePipelines=@('ART_ASSET')}).dispatches).Count 1 'business dispatch resumes after daily'
[pscustomobject]@{success=$true;checks=$script:count;fixturePath=$fixture} | ConvertTo-Json
