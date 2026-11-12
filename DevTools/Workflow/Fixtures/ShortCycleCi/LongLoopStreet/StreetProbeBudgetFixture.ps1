# Part of the existing StreetProbeCi fixture. No native Unity or synthetic success bypass.
function Test-StreetBudget441 {
 $old=ReadJson (Join-Path $PSScriptRoot 'StreetProbeGeometry.PreAI441.fixture.json')
 $path=Join-Path $PSScriptRoot 'StreetProbeGeometry.AI441.fixture.json'
 $g=ReadJson $path
 if($env:EATWHAT_STREET_HANDOFF_GEOMETRY){
  $delivered=ReadJson $env:EATWHAT_STREET_HANDOFF_GEOMETRY
  if($delivered.runId-ceq$g.runId){Check ((HashFile $path)-ceq(HashFile $env:EATWHAT_STREET_HANDOFF_GEOMETRY)) 'Current delivered geometry is exactly the tested stable plan'}
  else{$bound=$delivered|ConvertTo-Json -Depth 50|ConvertFrom-Json -DateKind String;$bound.runId=$g.runId;Check ($delivered.runId-ceq'a7_street435'-and(LLCanonical $bound)-ceq(LLCanonical $g)) 'Only reviewed future a7 run identity changes; exact geometry and all motion retained';$g=$delivered;$path=$env:EATWHAT_STREET_HANDOFF_GEOMETRY}
 }
 LLValidateStreetPlan $g
 Check ($g.stageDeadlineSum-eq117-and$g.cleanupSeconds-eq2-and$g.maximumSeconds-eq120) 'Actual geometry117 plus cleanup2 below immutable120'
 for($i=0;$i-lt22;$i++){
  $n=$g.stages[$i];$o=$old.stages[$i]
  $nc=$n|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String;$oc=$o|ConvertTo-Json -Depth 20|ConvertFrom-Json -DateKind String
  $nc.PSObject.Properties.Remove('timeoutSeconds');$oc.PSObject.Properties.Remove('timeoutSeconds')
  Check ((LLCanonical $nc)-ceq(LLCanonical $oc)) ('Exact unmodified motion/input/navigation/destination '+$n.id)
  $expected=if($n.id-ceq'depth-mid'){45}elseif($n.id-cin@('edge-approach','camera-settle','stopped-recenter')){6}else{3}
  Check ($n.timeoutSeconds-eq$expected) ('Exact allowed deadline '+$n.id)
 }
 foreach($name in @('maximumSeconds','expectedMoveSpeed','expectedVerticalFactor','edgeTangent','focusDelta','actualGeometry','paths','confinerGeometrySha256','humanTuningStatus','humanTuningCompleted','finalVisualAccepted')){Check ((LLCanonical $g.$name)-ceq(LLCanonical $old.$name)) ('Unchanged actual route/config '+$name)}
 $dir=Join-Path $root 'budget441';[void][IO.Directory]::CreateDirectory($dir)
 function RunPlan($name,$plan){
  $p=Join-Path $dir ($name+'.json');Save $p $plan
  $pi=[Diagnostics.ProcessStartInfo]::new();$pi.FileName=$mono;$pi.UseShellExecute=$false;$pi.CreateNoWindow=$true;$pi.RedirectStandardOutput=$true;$pi.RedirectStandardError=$true
  foreach($v in @($exe,'--validate-plan',$p)){$pi.ArgumentList.Add($v)}
  Save (Join-Path $dir ($name+'-preflight.json')) @{beforeEntry=$true;fixtureOnly=$true;command=@($mono,$exe,'--validate-plan',$p);exeSha256=(HashFile $exe);planSha256=(HashFile $p);monoSha256=(HashFile $mono)}
  $start=[DateTimeOffset]::UtcNow;$child=[Diagnostics.Process]::Start($pi);$stdout=$child.StandardOutput.ReadToEndAsync();$stderr=$child.StandardError.ReadToEndAsync();$child.WaitForExit()
  [IO.File]::WriteAllText((Join-Path $dir ($name+'.stdout.bin')),$stdout.GetAwaiter().GetResult(),$utf8);[IO.File]::WriteAllText((Join-Path $dir ($name+'.stderr.bin')),$stderr.GetAwaiter().GetResult(),$utf8)
  Save (Join-Path $dir ($name+'-process.json')) @{pid=$child.Id;exitCode=$child.ExitCode;start=$start.ToString('o');finish=[DateTimeOffset]::UtcNow.ToString('o')}
  return $child.ExitCode
 }
 Check ((RunPlan 'actual-delivered-positive' $g)-eq0) 'Actual producer validates exact delivered geometry45/117'
 foreach($case in @('sorting-over','ordinary-over','wrong-capture','over-total')){
  $p=$g|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String
  switch($case){'sorting-over'{$p.stages[11].timeoutSeconds=45.01}'ordinary-over'{$p.stages[11].timeoutSeconds=24;$p.stages[0].timeoutSeconds=13}'wrong-capture'{$p.stages[11].captureId='wrong'}'over-total'{$p.maximumSeconds=121}}
  $p.stageDeadlineSum=($p.stages|Measure-Object timeoutSeconds -Sum).Sum
  Reject {LLValidateStreetPlan $p} ('Actual consumer rejects '+$case)
  Check ((RunPlan $case $p)-ne0) ('Actual producer rejects '+$case)
 }
 $p=$g|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String;$p.stageDeadlineSum=102
 Reject {LLValidateStreetPlan $p} 'Fake declared sum102 cannot hide actual117'
 $p=$g|ConvertTo-Json -Depth 40|ConvertFrom-Json -DateKind String;$p.cleanupSeconds=0
 Reject {LLValidateStreetPlan $p} 'Cleanup reserve cannot be removed'
}
