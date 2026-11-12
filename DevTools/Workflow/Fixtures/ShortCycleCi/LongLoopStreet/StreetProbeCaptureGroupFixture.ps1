# Existing StreetProbeCi: real delivered PowerShell host and original managed wrapper.
# Only SDK/Editor and the single outer app transport are explicitly synthetic.
function Test-StreetActualCaptureGroup($task,$project,$queue){
 $f=MakeRun 'group438';FinishFixture $f
 $ctx=New-StreetFixtureNativeContext $task $project $taskRoot $queue
 $ctx.runId=$f.context.runId;$ctx.target.osStartUtc=$f.context.editorStartedAt
 $f.authorization.cliExecutable=@{path=$ctx.cliPath;sha256=(HashFile $ctx.cliPath)}
 Save $f.context.authorizationPath $f.authorization
 $f.context.authorizationSha256=HashFile $f.context.authorizationPath
 $f.request.contextSha256=$f.context.authorizationSha256;Save $f.context.requestPath $f.request
 $f.context.requestSha256=HashFile $f.context.requestPath
 $f.report.contextSha256=$f.context.authorizationSha256;$f.report.requestSha256=$f.context.requestSha256
 Copy-Item -LiteralPath $f.context.requestPath -Destination (Join-Path $f.context.probeRoot 'request.json') -Force
 $ctx.identity=$f.context|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String
 Convert-StreetFixtureSdkTimes $ctx.identity
 $ctx.request=$f.request|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String
 $ctx.pngPath=Join-Path $f.context.probeRoot 'capture.png'
 $pre=Invoke-StreetNativeRole $ctx 'preflight'
 Check ($pre.localSeconds-gt0-and$ctx.localDurations.Count-eq1) 'Group forecast uses actual same-path full local managed role duration'
 $ledger=Join-Path $f.context.probeRoot 'samples.jsonl';$full=[IO.File]::ReadAllBytes($ledger)
 $rows=@(Get-Content $ledger|Where-Object {($_|ConvertFrom-Json).phaseIndex-le11});$last=$rows[-1]|ConvertFrom-Json
 [IO.File]::WriteAllText($ledger,($rows-join"`n")+"`n",$utf8)
 $notice=ReadJson (Join-Path $f.context.probeRoot 'capture-pending.json')
 $now=[DateTimeOffset]::UtcNow;$notice.frame=$last.frame;$notice.elapsed=$last.elapsed;$notice.stageStartedElapsed=$last.elapsed-.75
 $notice.createdAt=$now.ToString('o');$notice.stageDeadlineAt=$now.AddSeconds(44.25).ToString('o');$notice.totalDeadlineAt=$now.AddSeconds(120-$last.elapsed).ToString('o')
 $noticePath=Join-Path $f.context.probeRoot 'capture-pending.json';Save $noticePath $notice
 $ctx.notice=$notice;$ctx.noticeSha256=HashFile $noticePath
 $f.report.startedAt=$now.AddSeconds(-$last.elapsed).ToString('o')
 $f.report.captureWindowStartedAt=$notice.createdAt;$f.report.captureStageDeadlineAt=$notice.stageDeadlineAt;$f.report.captureTotalDeadlineAt=$notice.totalDeadlineAt;$f.report.capturePendingNotificationSha256=$ctx.noticeSha256
 $pending=$f.report|ConvertTo-Json -Depth 80|ConvertFrom-Json -DateKind String
 $pending.status='RUNNING';$pending.passed=$false;$pending.pendingCapture='sorting'
 Convert-StreetFixtureSdkTimes $pending
 $ctx.responses.pendingRead=$pending|ConvertTo-Json -Depth 80 -Compress
 $ack=$pending|ConvertTo-Json -Depth 80|ConvertFrom-Json -DateKind String;$ack.pendingCapture=$null
 $ctx.responses.captureAck=$ack
 # The completed numeric fixture initially emitted a final report; preserve it outside the live hint path.
 $terminal=LLWithin $f.context.reportPath $f.context.probeRoot
 $terminalBackup=LLWithin ($terminal+'.fixture-before-group') $f.context.probeRoot
 LLAssert (-not(Test-Path -LiteralPath $terminalBackup)-and(LLStreetSamePath (Split-Path $terminal -Parent) (Split-Path $terminalBackup -Parent))) 'Fixture terminal backup must be fresh in the same isolated root.'
 $terminalBytes=[IO.File]::ReadAllBytes($terminal);$terminalValue=LLJson $terminalBytes
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){LLAssert (LLStreetIdentityField $key $terminalValue.$key $f.context.$key) 'Only this exact isolated fixture terminal may move.'}
 [IO.File]::Move($terminal,$terminalBackup)
 Check ((LLHash ([IO.File]::ReadAllBytes($terminalBackup)))-ceq(LLHash $terminalBytes)) 'Synthetic terminal bytes preserved alongside the same fixture live root'
 $hint=LLReadStreetHint $ctx.identity $null
 Check ($hint.status-ceq'CAPTURE_HINT'-and-not$hint.nativeSuccessProven) 'Actual local settled hint precedes three real managed child calls'
 $json=Invoke-StreetCaptureWindow $ctx;$frame=LLParseStreetCaptureGroup $json $ctx.identity
 Check ($frame.members.Count-eq3) 'Actual producer returns one group with three independent original wrapper stdout byte streams'
 $proof=@($frame.members|ForEach-Object {ReadJson $_.receiptPath})
 Check (@($proof.processId|Sort-Object -Unique).Count-eq3) 'Three independent actual SDK child processes in one local capture group'
 Check ([DateTimeOffset]::Parse($proof[0].finishedAt)-le[DateTimeOffset]::Parse($proof[1].startedAt)-and[DateTimeOffset]::Parse($proof[1].finishedAt)-le[DateTimeOffset]::Parse($proof[2].startedAt)) 'Each original process finishes before the next group action'
 $actualAck=ReadJson (Join-Path $ctx.out 'sdk-ack.json')
 $f.report.captureReceiptSha256=$actualAck.captureReceiptSha256;$f.report.captureAcknowledgedAt=(LLStreetTimestamp $actualAck.captureAcknowledgedAt).ToUniversalTime().ToString('o')
 $f.report.finishedAt=[DateTimeOffset]::UtcNow.ToString('o')
 [IO.File]::WriteAllBytes($ledger,$full);$f.report.sampleLedgerSha256=HashFile $ledger
 $f.context.receipts=@();Save $f.context.reportPath $f.report
 $reportJson=[IO.File]::ReadAllText($f.context.reportPath)
 $registration=$reportJson|ConvertFrom-Json -DateKind String;$registration.status='RUNNING';$registration.passed=$false
 $null=Receipt $f 'register' $registration;$null=Receipt $f 'complete' $reportJson;$null=Receipt $f 'cleanup' $reportJson
 $null=Receipt $f 'play' 'Entered play mode' 'editor_play';$null=Receipt $f 'stop' 'Exited play mode' 'editor_stop'
 $null=Receipt $f 'postStop' @{isPlaying=$false;authorClean=$true;scenePath=$f.context.scenePath;editorPid=12345;editorStartedAt=$f.context.editorStartedAt;projectPath=$project;confinerGeometrySha256=$f.report.confinerGeometrySha256}
 $null=Receipt $f 'savesBefore' @{files=$f.authorization.saveFiles};$null=Receipt $f 'savesAfter' @{files=$f.authorization.saveFiles}
 $console=@{entries=@();cursor=0;session='fixture-session';returned=0;dropped=$false;reset=$false;fixtureOnly=$true};$null=Receipt $f 'consoleBefore0' $console 'console';$null=Receipt $f 'consoleAfter0' $console 'console'
 # One simulated app return from the real PS producer. Never manufacture three per-member app0 objects.
 $appPath=Join-Path $taskRoot 'captureGroup-app-response.json';Save $appPath @{exit_code=0;output=$json;fixtureOnly=$true;transport='SIMULATED_OUTER_APP_ONLY';wall_time_seconds=10.266}
 $f.context.captureGroup=@{id=$frame.groupId;kind=$frame.kind;wrapperResponsePath=$appPath;wrapperResponseSha256=(HashFile $appPath);outputSha256=(LLHash ($utf8.GetBytes($json)))}
 foreach($member in $frame.members){$f.context.receipts+=@{role=$member.role;path=$member.receiptPath;sha256=$member.receiptSha256;groupId=$frame.groupId;groupIndex=$member.index;wrapperResponsePath=$appPath;wrapperResponseSha256=(HashFile $appPath)}}
 Save $f.contextPath $f.context
 $r=Consume $f 'group-positive';Check ($r.exit-eq0-and$r.text.Contains('FIXTURE_CI_PASSED')) ('Full actual consumer accepts three real wrapper/accounting streams with one shared fixture app: '+$r.text)
 function GroupReject($name,[scriptblock]$mutation){
  $backup=@{};$paths=@($f.contextPath,$appPath)+@($frame.members|ForEach-Object {[IO.Path]::ChangeExtension($_.receiptPath,'accounting.json')})
  foreach($path in $paths){$backup[$path]=[IO.File]::ReadAllBytes($path)}
  try{
   $c=ReadJson $f.contextPath;$app=ReadJson $appPath;$value=LLJson ($utf8.GetBytes($app.output))
   & $mutation $c $app $value
   $app.output=$value|ConvertTo-Json -Depth 100 -Compress;Save $appPath $app
   $c.captureGroup.wrapperResponseSha256=HashFile $appPath;$c.captureGroup.outputSha256=LLHash ($utf8.GetBytes($app.output))
   foreach($call in $c.receipts|Where-Object groupId){if($call.wrapperResponsePath-ceq$appPath){$call.wrapperResponseSha256=HashFile $appPath}}
   Save $f.contextPath $c;$r=Consume $f $name
   Check ($r.exit-ne0-and-not$r.text.Contains('FIXTURE_CI_PASSED')) ('Actual grouped consumer rejects '+$name)
  }finally{foreach($path in $paths){[IO.File]::WriteAllBytes($path,$backup[$path])}}
 }
 GroupReject 'group-missing-member' {param($c,$a,$v)$v.members=@($v.members|Select-Object -First 2)}
 GroupReject 'group-wrong-order' {param($c,$a,$v)$v.members=@($v.members[1],$v.members[0],$v.members[2])}
 GroupReject 'group-duplicate-index' {param($c,$a,$v)$v.members[1].index=0}
 GroupReject 'group-duplicate-receipt' {param($c,$a,$v)$v.members[1].receiptPath=$v.members[0].receiptPath}
 GroupReject 'group-foreign-run' {param($c,$a,$v)$v.runId='foreign'}
 GroupReject 'group-foreign-claim' {param($c,$a,$v)$v.executionId='0'*32}
 GroupReject 'group-foreign-request' {param($c,$a,$v)$v.requestSha256='0'*64}
 GroupReject 'group-foreign-notification' {param($c,$a,$v)$v.notificationSha256='0'*64}
 GroupReject 'group-raw-sha' {param($c,$a,$v)$v.members[0].wrapperOutputSha256='0'*64}
 GroupReject 'group-raw-partial' {param($c,$a,$v)$b=$utf8.GetBytes('{"partial":true}');$v.members[0].wrapperOutputBase64=[Convert]::ToBase64String($b);$v.members[0].wrapperOutputSha256=LLHash $b}
 GroupReject 'group-nonzero-app' {param($c,$a,$v)$a.exit_code=1}
 GroupReject 'group-partial-app' {param($c,$a,$v)$a|Add-Member session_id 123}
 GroupReject 'group-fake-separate-app' {param($c,$a,$v)($c.receipts|Where-Object role -CEQ 'capture').wrapperResponsePath+='-fake'}
 GroupReject 'group-no-accounting' {param($c,$a,$v)Save ([IO.Path]::ChangeExtension($v.members[1].receiptPath,'accounting.json')) @{status='MISSING'}}
 $expired=$notice|ConvertTo-Json|ConvertFrom-Json -DateKind String;$expired.stageDeadlineAt=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o')
 Reject {Street-AssertGroupRoom $ctx $expired 1} 'Actual local admission refuses expired original stage clock'
 $huge=$ctx.localDurations;$ctx.localDurations=[Collections.Generic.List[double]]::new();$ctx.localDurations.Add(10.9892471);$fresh=$notice|ConvertTo-Json|ConvertFrom-Json -DateKind String;$fresh.stageDeadlineAt=[DateTimeOffset]::UtcNow.AddSeconds(44).ToString("o");$fresh.totalDeadlineAt=[DateTimeOffset]::UtcNow.AddSeconds(110).ToString("o");Street-AssertGroupRoom $ctx $fresh 3;Check $true "Actual admission accepts measured L10.9892471 within fresh45 gate";$fresh.stageDeadlineAt=[DateTimeOffset]::UtcNow.AddSeconds(22.166927).ToString("o")
 Reject {Street-AssertGroupRoom $ctx $fresh 3} 'Actual admission refuses three locally slow members within the unchanged measured admission rule'
 $ctx.localDurations=$huge
 # Failure tests still execute the actual host/original wrapper, substituting only the SDK process.
 foreach($case in @(@{name='pending-business';role='pendingRead';exit=0},@{name='capture-unknown';role='capture';exit=19},@{name='ack-business';role='captureAck';exit=0},@{name='missing-accounting';account=$true},@{name='capture-persist';persist=$true},@{name='late-native-ack';late=$true})){
  $bad=New-StreetFixtureNativeContext $task $project (Join-Path $taskRoot ('group-failure-'+$case.name)) $queue
  $bad.runId=$ctx.runId;$bad.target.osStartUtc=$ctx.target.osStartUtc;$bad.identity=$ctx.identity;$bad.request=$ctx.request;$bad.pngPath=$ctx.pngPath
  foreach($duration in $ctx.localDurations){$bad.localDurations.Add($duration)}
  $n=$notice|ConvertTo-Json|ConvertFrom-Json -DateKind String;$at=[DateTimeOffset]::UtcNow;$n.createdAt=$at.ToString('o');$n.stageDeadlineAt=$at.AddSeconds(44.25).ToString('o');$n.totalDeadlineAt=$at.AddSeconds(120-$n.elapsed).ToString('o');$bad.notice=$n
  $bad.noticeSha256=LLHash ($utf8.GetBytes(($n|ConvertTo-Json -Compress)))
  $p=$pending|ConvertTo-Json -Depth 80|ConvertFrom-Json -DateKind String;$p.captureWindowStartedAt=$n.createdAt;$p.captureStageDeadlineAt=$n.stageDeadlineAt;$p.captureTotalDeadlineAt=$n.totalDeadlineAt;$p.capturePendingNotificationSha256=$bad.noticeSha256
  $bad.responses.pendingRead=$p|ConvertTo-Json -Depth 80 -Compress;$p.pendingCapture=$null;$bad.responses.captureAck=$p
  $bad.faultRole=$case.role;$bad.faultExit=[int]$case.exit
  if($case.late){$bad.lateAck=(LLStreetTimestamp $bad.notice.stageDeadlineAt).AddTicks(1).ToString('o')}
  if($case.persist){[IO.File]::WriteAllText((Join-Path $bad.out 'capture-receipt.sha256'),'FOREIGN_FIXTURE_BYTES',$utf8)}
  if($case.account){$bad.AfterDecode={param($role,$value,$context)if($role-ceq'pendingRead'){$raw=LLJson ([IO.File]::ReadAllBytes((Join-Path $context.out 'pendingRead-host.stdout.bin')));$path=$raw.stdoutPath.Replace('.stdout.bin','.accounting.json');Save $path @{status='MISSING';fixtureOnly=$true}}}}
  Reject {Invoke-StreetCaptureWindow $bad} ('Actual PS group stops on '+$case.name)
  Check (-not(Test-Path (Join-Path $bad.out 'capture-group-descriptor.json'))) ('Failed actual group never declares a completed frame: '+$case.name)
  if($case.name-cin@('pending-business','missing-accounting')){Check (-not(Test-Path (Join-Path $bad.out 'capture-parameters.json'))) ('No capture process after failed pending/accounting: '+$case.name)}
  if($case.name-cin@('capture-unknown','capture-persist')){Check (-not(Test-Path (Join-Path $bad.out 'captureAck-parameters.json'))) ('No ACK after capture unknown/persistence failure: '+$case.name)}
 }
 Save (Join-Path $f.data 'group-summary.json') @{fixtureOnly=$true;nativeEvidenceAccepted=$false;actualWrapperProcesses=3;sharedOuterAppObjects=1;outerAppSimulated=$true;actualGroupSeconds=([DateTimeOffset]::Parse($frame.finishedAt)-[DateTimeOffset]::Parse($frame.startedAt)).TotalSeconds;ackWithinOriginalStage=([DateTimeOffset]::Parse($actualAck.captureAcknowledgedAt)-le[DateTimeOffset]::Parse($notice.stageDeadlineAt));nativeRenderingAccepted=$false;frame=$frame}
}
