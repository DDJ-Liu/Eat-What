# Domain-specific435a3 output helper; no CLI runner. Its functions are exercised by StreetProbeCi.
function Street-AssertIdentity($report,$identity){
 foreach($key in @('taskId','attempt','executionId','ownerThreadId','runId','projectPath','scenePath','editorPid','editorStartedAt')){
  # Explicit human approval 2026-11-06: only these two managed-report diagnostics are non-blocking.
  # Generic comparators and the separate actual native target confirmation remain strict.
  if($key-cin@('projectPath','editorStartedAt')){
   $same=$false;$queryError=$null
   try{$same=LLStreetIdentityField $key $report.$key $identity.$key}catch{$queryError=$_.Exception.Message}
   if(-not$same){Write-Warning ("Street managed {0} diagnostic only: actual={1}; expected={2}; queryError={3}" -f $key,$report.$key,$identity.$key,$queryError)}
   continue
  }
  LLAssert (LLStreetIdentityField $key $report.$key $identity.$key) "Managed report identity differs: $key"
 }
 LLAssert ($report.capabilityVersion -ceq 'AI441-sorting45-clock-ack-v1') 'Loaded capture capability differs.'
}
function Street-AssertAdmission($notice,$schedule,[int]$remaining,[DateTimeOffset]$now=[DateTimeOffset]::UtcNow){
 LLAssert ($remaining -ge 1 -and $remaining -le 3 -and $schedule.forecastAppSeconds -is [ValueType] -and $schedule.forecastAppSeconds -isnot [bool]) 'Typed measured app admission required.'
 $forecast=[double]$schedule.forecastAppSeconds
 LLAssert (-not[double]::IsNaN($forecast)-and-not[double]::IsInfinity($forecast)-and$forecast-gt0-and$forecast-le35-and$schedule.perRoleReserveSeconds-eq.5-and$schedule.totalReserveSeconds-eq.5) 'Admission requires actual same-path app measurements and unchanged safety reserve.'
 $available=((LLStreetTimestamp $notice.stageDeadlineAt)-$now).TotalSeconds
 $total=((LLStreetTimestamp $notice.totalDeadlineAt)-$now).TotalSeconds
 LLAssert ($available -gt $remaining*($forecast+.5)+.5 -and $total -gt $remaining*($forecast+.5)+.5) 'Insufficient actual remaining sorting/total window; cancel without capture replay.'
}
function Street-AssertPending($report,$notice,[string]$noticeSha,$identity,$request){
 Street-AssertIdentity $report $identity;LLValidateCapturePending $notice $identity $request
 LLAssert ($report.status -ceq 'RUNNING'-and$report.pendingCapture-ceq'sorting'-and$report.capturePendingNotificationSha256-ceq$noticeSha-and(LLStreetSameInstant $report.captureStageDeadlineAt $notice.stageDeadlineAt)-and(LLStreetSameInstant $report.captureTotalDeadlineAt $notice.totalDeadlineAt)-and(LLStreetSameInstant $report.captureWindowStartedAt $notice.createdAt)) 'Actual managed pendingRead must bind original sorting notification/deadlines.'
}
function Street-AssertCleanup($report){
 foreach($key in @('inputRestored','configsRestored','configOwnerRestored','callbacksRemoved','temporaryObjectsDestroyed','noForeignOverwrite')){LLAssert (LLTrue $report.cleanup.$key) "Actual cleanup flag false: $key"}
 LLAssert ([string]::IsNullOrEmpty($report.cleanup.error)) 'Actual cleanup error.'
}
function Street-AssertRolePayload([string]$role,$report,$identity,$request,$notice,[string]$noticeSha,$receipt,$captureReceipt){
 if($role -cin @('register','pendingRead','captureAck','complete','cleanup','cancel','failureCleanup0','failureCleanup1')){Street-AssertIdentity $report $identity}
 switch -CaseSensitive ($role){
  'register' {LLAssert ($report.status-ceq'RUNNING'-and(LLFalse $report.passed)) 'Begin must return actual RUNNING, not completion'}
  'pendingRead' {Street-AssertPending $report $notice $noticeSha $identity $request}
  'captureAck' {
   LLAssert ([string]::IsNullOrEmpty($report.pendingCapture)-and$report.captureReceiptSha256-ceq$captureReceipt.sha256) 'ACK must bind the unique actual capture receipt'
   $at=(LLStreetTimestamp $report.captureAcknowledgedAt)
   LLAssert ($at-ge(LLStreetTimestamp $receipt.startedAt)-and$at-le(LLStreetTimestamp $receipt.finishedAt)-and$at-le(LLStreetTimestamp $notice.stageDeadlineAt)-and$at-le(LLStreetTimestamp $notice.totalDeadlineAt)) 'Actual ACK is late or outside its managed call'
  }
  'complete' {LLAssert ($report.status-ceq'SUCCEEDED'-and(LLTrue $report.passed)-and$report.requiredCount-eq32-and@($report.checks).Count-eq32) 'All32 actual complete assertions required';foreach($c in $report.checks){LLAssert (LLTrue $c.passed) 'Actual assertion failed'};Street-AssertCleanup $report}
  'cleanup' {LLAssert ($report.status-ceq'SUCCEEDED'-and(LLTrue $report.passed)) 'Actual successful cleanup report required';Street-AssertCleanup $report}
 }
}
