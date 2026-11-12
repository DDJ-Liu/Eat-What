// @exec: {"yield_time_ms": 10000, "max_output_tokens": 1200}
// AI438 output-only435attempt7 candidate. This same source is exercised by the explicit offline tool fixture.
// Actual ENGINE must bind the execution placeholder and obtain new CONTROL proof before using functions.exec.
function streetReturnedSha256(text){
 const b=Array.from(encodeURIComponent(text).replace(/%([0-9A-F]{2})/g,(_,h)=>String.fromCharCode(parseInt(h,16))),c=>c.charCodeAt(0)),bits=b.length*8;
 if(bits>=0x100000000)throw Error("Returned object exceeds bounded SHA input");
 b.push(128);while(b.length%64!==56)b.push(0);for(let i=7;i>=0;i--)b.push(i>=4?0:(bits>>>(i*8))&255);
 const k=[0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2];
 const h=[0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19],r=(x,n)=>(x>>>n)|(x<<(32-n)),w=new Int32Array(64);
 for(let p=0;p<b.length;p+=64){
  for(let i=0;i<16;i++)w[i]=(b[p+i*4]<<24)|(b[p+i*4+1]<<16)|(b[p+i*4+2]<<8)|b[p+i*4+3];
  for(let i=16;i<64;i++){const x=w[i-15],y=w[i-2];w[i]=((r(x,7)^r(x,18)^(x>>>3))+w[i-16]+(r(y,17)^r(y,19)^(y>>>10))+w[i-7])|0;}
  let [a,c,d,e,f,g,j,l]=h;
  for(let i=0;i<64;i++){const t1=(l+(r(f,6)^r(f,11)^r(f,25))+((f&g)^(~f&j))+k[i]+w[i])|0,t2=((r(a,2)^r(a,13)^r(a,22))+((a&c)^(a&d)^(c&d)))|0;l=j;j=g;g=f;f=(e+t1)|0;e=d;d=c;c=a;a=(t1+t2)|0;}
  [a,c,d,e,f,g,j,l].forEach((x,i)=>h[i]=(h[i]+x)|0);
 }
 return h.map(x=>(x>>>0).toString(16).padStart(8,"0")).join("").toUpperCase();
}
async function streetFinishEvidencePackage(host,root,out,execution,consumer,attempt=7){
 const quote=s=>"'"+String(s).replaceAll("'","''")+"'",need=(v,m)=>{if(!v)throw Error(m);};
 const utf8Bytes=s=>encodeURIComponent(s).replace(/%[0-9A-F]{2}/g,"x").length;
 const actual=[];let response;
 const remember=r=>{actual.push(r);host.store('AI435-a'+attempt+'-final-package-returns',JSON.parse(JSON.stringify(actual)));};
 response=await host.tools.exec_command({cmd:"& "+quote(out+"/close-evidence.ps1")+" -ConsumerResponsePath "+quote(consumer.path)+" -ConsumerResponseSha256 "+quote(streetReturnedSha256(consumer.json)),workdir:root,yield_time_ms:10000,max_output_tokens:2000});remember(response);
 while(response.session_id){response=await host.tools.write_stdin({session_id:response.session_id,chars:"",yield_time_ms:1000,max_output_tokens:2000});remember(response);}
 need(response.exit_code===0&&!response.session_id,"Post-consume storage failed; preserve original returns, never replay native");
 const measured=JSON.parse(response.output);
 need(measured.action==='STREET_FINAL_EVIDENCE_READY'&&measured.taskId==='AI-000435'&&measured.attempt===attempt&&measured.executionId===execution&&measured.returnNamespaceReserved===true&&/^[a-f0-9]{32}$/.test(measured.reservationToken),"Exact measured current storage/return namespace proof required");
 need(Number.isSafeInteger(measured.finalWholeTaskPackageBytes)&&measured.finalWholeTaskPackageBytes>=0&&measured.reservedFinalReturnBytes===65536,"Actual final whole-task measurement and explicit return reservation required");
 const dir=out+'/final-package-returns',files=actual.map((r,i)=>({path:dir+'/'+String(i).padStart(3,'0')+'.json',json:JSON.stringify(r)+'\n'}));
 const map={taskId:'AI-000435',attempt,executionId:execution,baseMeasuredWholeTaskBytes:measured.finalWholeTaskPackageBytes,reservationToken:measured.reservationToken,entries:files.map(f=>({path:f.path,bytes:utf8Bytes(f.json),sha256:streetReturnedSha256(f.json)})),finalWholeTaskPackageBytes:0,maximumBytes:10000000,passed:false,measurement:'actual post-consume whole task including reservation plus exact final CreateNew UTF8 files; CONTROL independently remeasures before acceptance'};
 let mapJson='';for(let i=0;i<8;i++){mapJson=JSON.stringify(map)+'\n';const size=measured.finalWholeTaskPackageBytes+files.reduce((n,f)=>n+utf8Bytes(f.json),0)+utf8Bytes(mapJson);if(size===map.finalWholeTaskPackageBytes&&map.passed===(size<=10000000))break;map.finalWholeTaskPackageBytes=size;map.passed=size<=10000000;}
 mapJson=JSON.stringify(map)+'\n';
 const patch='*** Begin Patch\n'+files.concat([{path:dir+'/final-package-map.json',json:mapJson}]).map(f=>'*** Add File: '+f.path+'\n'+f.json.slice(0,-1).split('\n').map(s=>'+'+s).join('\n')).join('\n')+'\n*** End Patch';
 const saved=await host.tools.apply_patch(patch);need(!saved?.isError,'Final add-only evidence write rejected; retain actual objects in store');
 host.store('AI435-a'+attempt+'-final-package-map',map);
 need(map.finalWholeTaskPackageBytes-measured.finalWholeTaskPackageBytes<=measured.reservedFinalReturnBytes&&map.passed,'Final whole task including actual last return files exceeds10MB; all original returns preserved, do not Complete');
 return map;
}
async function runStreetProbeManagedSchedule(host,execution,options={}){
 const attempt=options.attempt??7,runId=options.runId??"a7_street435";
 const root="D:/GameProject/Eat-What",out=root+"/.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000435/attempt"+attempt;
 const need=(ok,m)=>{if(!ok)throw Error(m);},quote=s=>"'"+String(s).replaceAll("'","''")+"'";
 need(/^[0-9a-f]{32}$/.test(execution),"Current actual claim required");
 const recoveryKey="AI435-a"+attempt+"-returned-objects";
 const state={schemaVersion:1,taskId:"AI-000435",attempt,executionId:execution,runId,pending:[],objects:[],calls:[],nativeCount:0,playIssued:false,playConfirmed:false,registerIssued:false,registered:false,captureIssued:false,stopIssued:false,diskFailed:false,unknownNative:false,completed:false};
 let sequence=0,forecastSeconds=0;
 const journal=()=>host.store(recoveryKey,JSON.parse(JSON.stringify(state)));
 function remember(response,label){
  // Capture before validation, including failures and every partial. Store is NOT a disk commit.
  const item={path:out+"/"+label+".json",json:JSON.stringify(response)};
  state.pending.push(item);state.objects.push({path:item.path,json:item.json});journal();return item;
 }
 function carryPrefix(batch,label,reservation){
  const packet=JSON.stringify({root:out,records:batch,label,reservation});
  return "$ErrorActionPreference='Stop';$streetCarry="+quote(packet)+"|ConvertFrom-Json -Depth 100 -DateKind String;"+
   "$streetCarryStarted=[Diagnostics.Stopwatch]::StartNew();try{"+
   "if($streetCarry.reservation){$returnRoot=[IO.Path]::GetFullPath($streetCarry.reservation.returnRoot);$base=[IO.Path]::GetFullPath($streetCarry.root).TrimEnd('\\','/');"+
   "if(-not$returnRoot.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $returnRoot)){throw 'Return namespace already exists or is foreign'};"+
   "for($ancestor=[IO.Path]::GetDirectoryName($returnRoot);$ancestor;$ancestor=[IO.Path]::GetDirectoryName($ancestor)){if((Test-Path -LiteralPath $ancestor)-and([IO.File]::GetAttributes($ancestor)-band[IO.FileAttributes]::ReparsePoint)){throw 'Redirected return namespace'}};"+
   "[void][IO.Directory]::CreateDirectory($returnRoot);for($slot=0;$slot-lt$streetCarry.reservation.slots;$slot++){if(Test-Path -LiteralPath (Join-Path $returnRoot ($slot.ToString('D3')+'.json'))){throw 'Return sidecar already exists'}};"+
   "if(Test-Path -LiteralPath (Join-Path $returnRoot 'return-map.json')){throw 'Return mapping exists'};"+
   "$f=[IO.File]::Open($streetCarry.reservation.path,[IO.FileMode]::CreateNew);try{$b=[Text.UTF8Encoding]::new($false).GetBytes($streetCarry.reservation.token+[Environment]::NewLine);$f.Write($b)}finally{$f.Dispose()};"+
   "[Console]::Out.WriteLine('STREET_RETURN_SPACE_RESERVED:'+$streetCarry.reservation.token)};"+
   "foreach($record in $streetCarry.records){$path=[IO.Path]::GetFullPath($record.path);$base=[IO.Path]::GetFullPath($streetCarry.root).TrimEnd('\\','/');"+
   "if(-not$path.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Response outside exact task output'};"+
   "for($ancestor=[IO.Path]::GetDirectoryName($path);$ancestor;$ancestor=[IO.Path]::GetDirectoryName($ancestor)){if((Test-Path -LiteralPath $ancestor)-and([IO.File]::GetAttributes($ancestor)-band[IO.FileAttributes]::ReparsePoint)){throw 'Redirected response ancestor'}};"+
   "$bytes=[Text.UTF8Encoding]::new($false).GetBytes($record.json);$sha=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes));"+
   "if(Test-Path -LiteralPath $path){if(([IO.File]::GetAttributes($path)-band[IO.FileAttributes]::ReparsePoint)-or(Get-FileHash -LiteralPath $path).Hash-cne$sha){throw 'Existing response bytes differ'}}"+
   "else{$f=[IO.File]::Open($path,[IO.FileMode]::CreateNew);try{$f.Write($bytes)}finally{$f.Dispose()}};"+
   "if((Get-FileHash -LiteralPath $path).Hash-cne$sha){throw 'Original returned object disk SHA mismatch'}};"+
   "$meta=[ordered]@{label=$streetCarry.label;elapsedSeconds=$streetCarryStarted.Elapsed.TotalSeconds;objects=@($streetCarry.records|ForEach-Object {@{path=$_.path;sha256=(Get-FileHash -LiteralPath $_.path).Hash}});at=[DateTimeOffset]::UtcNow.ToString('o')};"+
   "$metaPath=Join-Path $streetCarry.root ($streetCarry.label+'-carry-write.json');"+
   "$f=[IO.File]::Open($metaPath,[IO.FileMode]::CreateNew);try{$b=[Text.UTF8Encoding]::new($false).GetBytes(($meta|ConvertTo-Json -Depth 20));$f.Write($b)}finally{$f.Dispose()};"+
   "}catch{[Console]::Error.WriteLine('STREET_PERSIST_FAILED: '+$_.Exception.Message);exit 91};";
 }
 async function call(command,label,nativeRole=null,measure=false){
  const batch=state.pending.slice(),id=++sequence,callLabel=label+"Call"+id;
  const started=Date.now();let actual;
  const metrics={taskId:"AI-000435",attempt,executionId:execution,runId,forecastAppSeconds:forecastSeconds,perRoleReserveSeconds:.5,totalReserveSeconds:.5};
  const saveMetrics="$m="+quote(JSON.stringify(metrics))+";$p="+quote(out+"/"+callLabel+"-schedule.json")+";$f=[IO.File]::Open($p,[IO.FileMode]::CreateNew);try{$b=[Text.UTF8Encoding]::new($false).GetBytes($m);$f.Write($b)}finally{$f.Dispose()};";
  const full=carryPrefix(batch,callLabel,null)+saveMetrics+command.replaceAll("__SCHEDULE_PATH__",out+"/"+callLabel+"-schedule.json");
  journal();
  try{actual=await host.tools.exec_command({cmd:full,workdir:root,yield_time_ms:10000,max_output_tokens:6000});}
  catch(error){if(nativeRole)state.unknownNative=true;journal();throw error;}
  let item=remember(actual,callLabel+"-app-response");
  if(actual.exit_code!==91)state.pending=state.pending.filter(x=>!batch.includes(x));
  if(actual.exit_code===91){state.diskFailed=true;journal();throw Error("Prior response persistence failed; no next native command was issued");}
  while(actual.session_id){
   const oldPath=item.path;item.path=out+"/"+callLabel+"-app-partial"+(++sequence)+".json";state.objects.find(x=>x.path===oldPath).path=item.path;
   journal();
   try{actual=await host.tools.write_stdin({session_id:actual.session_id,chars:"",yield_time_ms:1000,max_output_tokens:6000});}
   catch(error){if(nativeRole)state.unknownNative=true;journal();throw error;}
   item=remember(actual,callLabel+"-app-final"+(++sequence));
  }
  const elapsed=(Date.now()-started)/1000;
  state.calls.push({label,nativeRole,appRoundtripSeconds:elapsed,returnedAppWallSeconds:actual.wall_time_seconds??null,prefixWriteIncluded:true});
  // No concatenation of partial output into a fabricated response. Final actual output must be complete.
  if(nativeRole&&(!Number.isInteger(actual.exit_code)||String(actual.output).includes("STREET_NATIVE_UNKNOWN")))state.unknownNative=true;
  need(actual.exit_code===0&&!actual.session_id,label+" actual app failed");
  let result;
  if(!nativeRole&&label!=="select-ready"){journal();return {action:"LOCAL_COMPLETED",nativeSuccessProven:false};}
  try{result=JSON.parse(actual.output);}catch{if(nativeRole)state.unknownNative=true;journal();throw Error(label+" incomplete/truncated actual output");}
  if(nativeRole&&result.action==="TOOL_CAPTURED"){
   need(result.taskId==="AI-000435"&&result.attempt===attempt&&result.executionId===execution&&result.exitCode===0,"Exact current managed receipt required");
   const prior=item.path;item.path=out+"/"+nativeRole+"-app-response.json";state.objects.find(x=>x.path===prior).path=item.path;
   if(measure)forecastSeconds=Math.max(forecastSeconds,elapsed);
  }
  if(nativeRole==="captureGroup"&&result.kind==="STREET_CAPTURE_GROUP_RAW_V1"){
   need(result.taskId==="AI-000435"&&result.attempt===attempt&&result.executionId===execution&&result.runId===runId&&result.memberCount===3&&result.members.length===3,"Exact one-app group identity/count");
   ["pendingRead","capture","captureAck"].forEach((role,index)=>need(result.members[index].role===role&&result.members[index].index===index,"Fixed grouped role order/index"));
   const prior=item.path;item.path=out+"/captureGroup-app-response.json";state.objects.find(x=>x.path===prior).path=item.path;
   state.groupId=result.groupId;state.groupReturned=true;
  }
  journal();return result;
 }
 async function local(role,args=""){
  return call("& "+quote(out+"/"+role+".ps1")+args,role);
 }
 async function native(role,payload="",waitFor=""){
  need(state.nativeCount<(state.failing?60:57),"Official call budget exhausted");
  if(role==="capture"){need(!state.captureIssued,"No second capture");state.captureIssued=true;}
  if(role==="play"){need(!state.playIssued,"No second Play");state.playIssued=true;}
  if(role==="register"){need(!state.registerIssued,"No second Begin");state.registerIssued=true;}
  if(role==="stop"){need(!state.stopIssued,"No second Stop");state.stopIssued=true;}
  state.nativeCount++;journal();
  const command="& "+quote(out+"/run-one.ps1")+" -Name "+quote(role)+(payload?" -Payload "+quote(payload):"")+
   (waitFor?" -WaitFor "+quote(waitFor):"")+" -ScheduleInputPath "+quote("__SCHEDULE_PATH__");
  const r=await call(command,role,role,!state.playConfirmed);
  if(r.action==="LOCAL_WAIT"){state.nativeCount--;journal();return r;}
  need(r.action==="TOOL_CAPTURED","A local hint cannot prove a managed role");
  if(role==="play")state.playConfirmed=true;
  if(role==="register")state.registered=true; // run-one verified actual RUNNING payload, not merely issued.
  journal();return r;
 }
 async function captureGroup(){
  need(!state.captureIssued&&state.nativeCount+3<=57,"One bounded fixed capture group only");
  state.captureIssued=true;state.nativeCount+=3;journal();
  const r=await call("& "+quote(out+"/capture-window.ps1")+" -ScheduleInputPath "+quote("__SCHEDULE_PATH__"),"captureGroup","captureGroup");
  if(r.action==="LOCAL_WAIT"){state.nativeCount-=3;state.captureIssued=false;journal();return false;}
  need(r.kind==="STREET_CAPTURE_GROUP_RAW_V1"&&state.groupReturned,"Actual group frame required, never three synthesized app0 objects");
  journal();return true;
 }
 async function waitAndNative(role,waitFor){
  for(let slice=0;slice<3;slice++){const r=await native(role,"read",waitFor);if(r.action!=="LOCAL_WAIT")return r;}
  throw Error("Original native120s clock/local bounded waits exhausted");
 }
 async function flush(){
  if(!state.pending.length)return;
  const batch=state.pending.slice(),label="finalFlush"+(++sequence);
  const reservation={path:out+"/"+label+"-app-response.json",token:"STREET_RESERVED_"+execution+"_"+sequence,returnRoot:out+"/"+label+"-returned",slots:128};
  const returned=[];let response,transportError=null;
  try{
   response=await host.tools.exec_command({cmd:carryPrefix(batch,label,reservation)+"Write-Output '{\"flushed\":true}'",workdir:root,yield_time_ms:10000,max_output_tokens:2000});
   returned.push(remember(response,label+"-actual-return"));
   while(response.session_id){
    need(returned.length<reservation.slots,"Bounded storage partials exhausted; keep raw store, never replay");
    response=await host.tools.write_stdin({session_id:response.session_id,chars:"",yield_time_ms:1000,max_output_tokens:2000});
    returned.push(remember(response,label+"-partial"+(++sequence)));
   }
  }catch(error){transportError=error;state.storageTransportError=String(error);journal();}
  // The storage exec reserves a fresh empty return namespace before writing the batch.
  // A failed/unknown reservation never licenses writes into a pre-existing directory.
  need(returned.some(x=>JSON.parse(x.json).output?.includes("STREET_RETURN_SPACE_RESERVED:"+reservation.token)),"Return namespace not confirmed; originals remain in store");
  const entries=returned.map((item,index)=>({originPath:item.path,path:reservation.returnRoot+"/"+String(index).padStart(3,"0")+".json",objectJsonSha256:streetReturnedSha256(item.json),fileSha256:streetReturnedSha256(item.json+"\n"),item}));
  const mapping={schemaVersion:1,kind:"STREET_ADD_ONLY_RETURN_MAP",originalReservation:reservation.path,reservationToken:reservation.token,reservationPreserved:true,serialization:"JSON.stringify UTF-8 plus AddFile LF",entries:entries.map(({item,...v})=>v)};
  const mapPath=reservation.returnRoot+"/return-map.json";
  const patch="*** Begin Patch\n"+entries.map(e=>"*** Add File: "+e.path+"\n+"+e.item.json+"\n").join("")+"*** Add File: "+mapPath+"\n+"+JSON.stringify(mapping)+"\n*** End Patch";
  const saved=await host.tools.apply_patch(patch);
  need(saved&&!saved.isError,"Add-only returned-object storage rejected; originals remain in store");
  for(const entry of entries){const stored=state.objects.find(x=>x.path===entry.item.path);need(stored,"Raw return disappeared");stored.originPath=stored.path;stored.path=entry.path;stored.objectJsonSha256=entry.objectJsonSha256;stored.fileSha256=entry.fileSha256;}
  state.pending=state.pending.filter(x=>!returned.includes(x));
  state.lastStorageResponse={path:entries.at(-1).path,json:returned.at(-1).json,mapPath,reservationPath:reservation.path};
  if(response&&!response.session_id)state.finalStorageResponse=state.lastStorageResponse;
  if(!transportError&&response?.exit_code===0&&!response.session_id)state.pending=state.pending.filter(x=>!batch.includes(x));
  journal();
  if(transportError)throw transportError;
  need(response?.exit_code===0&&!response.session_id,"Storage exec failed; original batch remains pending, no business replay");
  need(state.pending.length===0,"Unwritten originals retained in store; never replay business");
 }
 let failure=null;
 try{
  await local("prepare-target");await native("status");await native("editorStatus");await local("check-status");
  if(options.compiledPayloads){
   need(attempt===8&&runId==="a8_street435","Only the reviewed attempt8 compiled handoff is enabled");
   await local("build-payloads"," -Bootstrap");await native("shortCapabilities","shortCapabilities");
   let capability=await local("select-ready"," -CompiledInterface");
   if(capability.next==="IMPORT_ONCE"){
    await native("normalImport","normalImport");await native("importReadback","importReadback");
    capability=await local("select-ready"," -CompiledInterface");
   }
   need(capability.next==="CURRENT","Current loaded compiled Street interface not proven");
  }
  await local("build-payloads"," -Names @('preflight','savesBefore','savesAfter','ready','register','read','captureAck','postStop','cancel')");
  await native("preflight","preflight");await native("savesBefore","savesBefore");await native("consoleBefore0");await local("check-console"," -When before");
  await native("play");
  const readyStarted=Date.now();let ready=false;
  for(let i=0;i<3&&!ready&&Date.now()-readyStarted<30000;i++){await native("ready"+i,"ready");const selected=await local("select-ready"," -Role "+quote("ready"+i));ready=selected.next==="BEGIN";}
  need(ready&&Date.now()-readyStarted<=30000,"Single30s natural readiness window exhausted");
  await local("prepare-context"," -PresetReviewPath "+quote(out+"/ci-contexts/"+runId+".preset-review.json"));
  await native("register","register");
  let grouped=false;for(let i=0;i<3&&!grouped;i++)grouped=await captureGroup();
  need(grouped,"Original120s capture wait exhausted");
  await waitAndNative("complete","Terminal");await native("cleanup","read");
  await native("stop");await native("postStop","postStop");await native("savesAfter","savesAfter");await native("consoleAfter0");await local("check-console"," -When after");
  await local("finalize-context");
  // Every native app object is carried to disk before this actual Final consumer command starts.
  await call("& "+quote(root+"/DevTools/Verify-All.ps1")+" -TaskId AI-000435 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath "+quote(out+"/ci-contexts/"+runId+".json")+" -StateRoot "+quote(out+"/verify-state"),"nativeConsumer");
  state.consumerPassed=true;journal();
 }catch(error){
  failure=error;state.failing=true;state.failure=String(error);journal();
  // Unknown/persistence failures stop new commands. Unknown Begin does not authorize blind Cancel.
  if(!state.diskFailed&&!state.unknownNative){
   if(state.registered&&!state.stopIssued){try{await native("cancel","cancel");}catch(e){state.cleanupError=String(e);journal();}}
   if(state.registered&&!state.stopIssued&&!state.unknownNative){for(let i=0;i<2&&state.nativeCount<59;i++){try{await native("failureCleanup"+i,"read");}catch(e){state.cleanupError=String(e);break;}}}
  }
  // A confirmed Play can still use the one ordinary Stop lifecycle when Begin was unknown.
  if(state.playConfirmed&&!state.stopIssued&&!state.diskFailed&&state.nativeCount<60){try{await native("stop");}catch(e){state.stopError=String(e);journal();}}
  if(state.playIssued&&!state.playConfirmed&&!state.stopIssued&&!state.diskFailed&&state.nativeCount<60){
   // A failed host after a proven plaintext Play is different from an unknown native Play.
   // The safety helper revalidates the saved actual managed receipt/SDK/target before the one Stop.
   state.stopIssued=true;state.nativeCount++;journal();
   try{const r=await call("& "+quote(out+"/safety-stop.ps1"),"safetyStop","stop");if(r.action==="TOOL_CAPTURED")state.playConfirmed=true;else need(r.action==="NO_PLAY_PROVEN","Unknown safety transition");}
   catch(e){state.stopError=String(e);journal();}
  }
  if(state.playConfirmed&&state.stopIssued&&!state.registered&&!state.diskFailed&&!state.stopError&&state.nativeCount<58){
   try{await native("safetySavesAfter","savesAfter");await native("consoleAfter0");await local("check-console"," -When after");}catch(e){state.safetyReadbackError=String(e);journal();}
  }
 }finally{
  try{await flush();}catch(error){state.flushError=String(error);journal();failure??=error;}
 }
 if(!failure&&state.consumerPassed===true){
  try{const consumer=state.objects.find(x=>/\/nativeConsumerCall\d+-app-(response|final\d+)\.json$/.test(x.path));need(consumer,'Original successful consumer app object required');state.finalPackage=await streetFinishEvidencePackage(host,root,out,execution,consumer,attempt);}
  catch(error){state.finalPackageError=String(error);failure=error;}
 }
 state.completed=!failure&&state.consumerPassed===true&&state.finalPackage?.passed===true;
 host.store(recoveryKey,JSON.parse(JSON.stringify(state)));
 if(failure)throw failure;
 host.text("435a"+attempt+" evidence consumed and final package gated; independent review and formal settlement remain.");
 return state;
}
await runStreetProbeManagedSchedule({tools,store,text}, "__CURRENT_EXECUTION_ID__");
