# Loaded only inside the existing StreetProbeCi fixture. Calls the actual delivered consumer functions.
function Test-StreetProbeJsonBoundary {
 $folder=Join-Path $root 'json-boundary';[void][IO.Directory]::CreateDirectory($folder)
 $json='{"z":[null,true,false,[],[1],{"date":"2026-11-05T00:00:00+08:00","n":1.25}],"a":0,"B":1,"Keys":"real-key","Count":2,"SyncRoot":"real-data","empty":{}}'
 $objects=@((LLJson ($utf8.GetBytes($json))),($json|ConvertFrom-Json -AsHashtable -DateKind String))
 $ordered=[Collections.Specialized.OrderedDictionary]::new([StringComparer]::Ordinal)
 $plain=[Collections.Hashtable]::new([StringComparer]::Ordinal)
 foreach($property in $objects[0].PSObject.Properties){$ordered.Add($property.Name,$property.Value);$plain.Add($property.Name,$property.Value)}
 $objects+=@($ordered,$plain);$expected=LLCanonical $objects[0]
 foreach($object in $objects){Check ((LLCanonical $object) -ceq $expected) ('Actual canonical equal JSON across '+$object.GetType().FullName)}
 Check ($expected.Contains('"z":[null,true,false,[],[1],') -and $expected.IndexOf('"B":') -lt $expected.IndexOf('"a":')) 'Empty/single arrays, null/bool/date strings and Ordinal case order preserved'
 $caseKeys=[Collections.Hashtable]::new([StringComparer]::Ordinal);$caseKeys.Add('a',2);$caseKeys.Add('A',1)
 Check ((LLCanonical $caseKeys)-ceq'{"A":1,"a":2}') 'Dictionary keys retain both cases and Ordinal order without folding'
 Reject {LLJson ($utf8.GetBytes('{"A":1,"a":2}'))} 'Existing PSCustomObject JSON boundary explicitly rejects unrepresentable case-only duplicate rather than folding'
 Check ((LLCanonical ([pscustomobject]@{})) -ceq '{}' -and (LLCanonical ([object[]]@())) -ceq '[]') 'Empty object/array remain distinct'
 foreach($number in @([byte]1,[sbyte]1,[short]1,[ushort]1,[int]1,[uint]1,[long]1,[ulong]1,[single]1,[double]1,[decimal]1)){Check ((LLCanonical $number) -ceq '1') ('Finite JSON numeric scalar '+$number.GetType().Name)}
 foreach($bad in @([DateTime]::UtcNow,[DateTimeOffset]::UtcNow,[guid]::Empty,[char]'a',[version]'1.2',[double]::NaN,[double]::PositiveInfinity,[single]::NegativeInfinity,[DayOfWeek]::Monday)){
  Reject {LLCanonical $bad} ('Non-JSON/nonfinite scalar rejected '+$bad.GetType().Name)
 }
 $nonstring=@{};$nonstring[1]='number-key';Reject {LLCanonical $nonstring} 'Actual dictionary non-string key rejected'
 $cycle=@{};$cycle.self=$cycle;Reject {LLCanonical $cycle} 'Dictionary reference cycle bounded rejection'
 $cycle=[pscustomobject]@{self=$null};$cycle.self=$cycle;Reject {LLCanonical $cycle} 'JSON object reference cycle bounded rejection'
 $cycle=[Collections.ArrayList]::new();[void]$cycle.Add($cycle);Reject {LLCanonical $cycle} 'Array/list reference cycle bounded rejection'
 $deep=[pscustomobject]@{leaf=1};for($i=0;$i-lt66;$i++){$deep=[pscustomobject]@{child=$deep}};Reject {LLCanonical $deep} 'Explicit64 depth bound rejects'
 $shared=[pscustomobject]@{n=1};Check ((LLCanonical ([pscustomobject]@{a=$shared;b=$shared})) -ceq '{"a":{"n":1},"b":{"n":1}}') 'Shared acyclic reference remains legal JSON'
 Reject {LLJson ($utf8.GetBytes('{"same":1,"same":2}'))} 'Original strict duplicate JSON field check remains'
 $missing=[pscustomobject]@{returned=0};Reject {LLValidateStreetConsolePrefix $missing $missing} 'Missing required Console fields rejected'
 function HistoricalJson($name){
  $path=Join-Path $PSScriptRoot $name;$f=[IO.File]::OpenRead($path)
  try{$z=[IO.Compression.GZipStream]::new($f,[IO.Compression.CompressionMode]::Decompress);$m=[IO.MemoryStream]::new();try{$z.CopyTo($m);return LLJson ($m.ToArray())}finally{$z.Dispose();$m.Dispose()}}finally{$f.Dispose()}
 }
 $old=HistoricalJson 'StreetConsole117.fixture.json.gz';$after=HistoricalJson 'StreetConsole121.fixture.json.gz'
 LLValidateStreetConsolePrefix $old $after
 Check ($old.entries.Count-eq117-and$after.entries.Count-eq121-and$after.session-ceq'af5de125af094350a8609c3fbce1849c') 'Actual historical117 prefix to121 with four new warnings accepted'
 $hashOld=($old|ConvertTo-Json -Depth 30)|ConvertFrom-Json -AsHashtable -DateKind String
 $hashAfter=($after|ConvertTo-Json -Depth 30)|ConvertFrom-Json -AsHashtable -DateKind String
 LLValidateStreetConsolePrefix $hashOld $hashAfter;Check $true 'Actual console checker succeeds with OrderedHashtable through production canonical'
 function BadConsole($label,[scriptblock]$mutation){$v=($after|ConvertTo-Json -Depth 30)|ConvertFrom-Json -DateKind String;& $mutation $v;Reject {LLValidateStreetConsolePrefix $old $v} $label}
 BadConsole 'Historical prefix field difference rejects' {param($v)$v.entries[0].message+='changed'}
 BadConsole 'Missing sequence rejects' {param($v)$v.entries[119].seq=121}
 BadConsole 'New Console Error rejects' {param($v)$v.entries[120].logType='Error';$v.entries[120].level='error'}
 BadConsole 'Repeated old Error at a new sequence still rejects' {param($v)$e=$old.entries|Where-Object logType -EQ 'Error'|Select-Object -First 1;$v.entries[120].message=$e.message;$v.entries[120].logType='Error';$v.entries[120].level='error'}
 BadConsole 'Console reset rejects' {param($v)$v.reset=$true}
 BadConsole 'Console session reset rejects' {param($v)$v.session='foreign'}
 $legacy=Join-Path $PSScriptRoot 'LongLoopConsumer.PreAI437.ps1.txt'
 $child=Join-Path $folder 'old-counterexample.ps1'
 $body=@'
param([string]$Source)
$ErrorActionPreference='Stop'
$tok=$null;$err=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Source,[ref]$tok,[ref]$err)
foreach($name in @('LLAssert','LLCanonical')){$fn=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($fn.Count-ne1){throw 'Exact old function missing'};$definition=$fn[0].Extent.Text;if($name-ceq'LLCanonical'){$definition=$definition.Replace('function LLCanonical(','function OldActualCanonical(')};Invoke-Expression $definition}
# Exact old body and recursive call sites are retained. A test-only wrapper bounds and records
# a repeated dictionary reference instead of waiting for the interpreter's unbounded stack overflow.
$script:traceAncestors=[Collections.Generic.List[object]]::new();$script:traceCalls=0
function LLCanonical($value){
 $script:traceCalls++;if($script:traceCalls-gt256){throw 'UNPROVEN_TRACE_BOUND'}
 if($value-is[Collections.IDictionary]){foreach($ancestor in $script:traceAncestors){if([object]::ReferenceEquals($ancestor,$value)){throw ('OLD_ACTUAL_DICTIONARY_CYCLE '+$value.GetType().FullName+' calls='+$script:traceCalls)}}}
 [void]$script:traceAncestors.Add($value)
 try{OldActualCanonical $value}finally{$script:traceAncestors.RemoveAt($script:traceAncestors.Count-1)}
}
try{$v='{"event":{"seq":1,"message":"fixture"}}'|ConvertFrom-Json -AsHashtable -DateKind String;$unexpected=LLCanonical $v;[Console]::Error.WriteLine('Old source unexpectedly accepted: '+$unexpected);exit 4}
catch{[Console]::WriteLine('OLD_ACTUAL_RECURSION '+$_.Exception.Message);if($_.Exception.Message-match'OLD_ACTUAL_DICTIONARY_CYCLE.*OrderedHashtable'){exit 0};exit 5}
'@
 [IO.File]::WriteAllText($child,$body,$utf8)
 $pwsh=(Get-Command pwsh).Source
 Save (Join-Path $folder 'old-counterexample.preflight.json') @{beforeEntry=$true;oldSource=@{path=$legacy;sha256=(HashFile $legacy)};script=@{path=$child;sha256=(HashFile $child)};interpreter=@{path=$pwsh;sha256=(HashFile $pwsh)};fixtureOnly=$true}
 $start=Get-Date;$p=Start-Process -FilePath $pwsh -ArgumentList @('-NoProfile','-File',('"'+$child+'"'),'-Source',('"'+$legacy+'"')) -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $folder 'old.stdout.bin') -RedirectStandardError (Join-Path $folder 'old.stderr.bin')
 $timeout=-not$p.WaitForExit(15000);if($timeout){$p.Kill();$p.WaitForExit()}
 Save (Join-Path $folder 'old-counterexample.process.json') @{startedAt=$start.ToString('o');finishedAt=(Get-Date).ToString('o');exitCode=$p.ExitCode;timedOut=$timeout;stdout=(Join-Path $folder 'old.stdout.bin');stderr=(Join-Path $folder 'old.stderr.bin');fixtureOnly=$true}
 $raw=[IO.File]::ReadAllText((Join-Path $folder 'old.stdout.bin'))+[IO.File]::ReadAllText((Join-Path $folder 'old.stderr.bin'))
 Check (-not$timeout-and$p.ExitCode-eq0-and$raw-match'OLD_ACTUAL_RECURSION.*OLD_ACTUAL_DICTIONARY_CYCLE.*OrderedHashtable') 'Readonly actual preAI437 recursion reaches the same dictionary; explicit test wrapper bounds execution'
}
