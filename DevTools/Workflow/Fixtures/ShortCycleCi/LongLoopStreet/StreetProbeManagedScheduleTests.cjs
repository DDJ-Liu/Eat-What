// Existing StreetProbeCi invokes this explicit fixture. All tools/time/receipts below are SIMULATED.
// The schedule and admission/payload guards are the delivered sources, never a rewritten ideal scheduler.
const fs=require("fs"),path=require("path"),vm=require("vm"),cp=require("child_process"),crypto=require("crypto");
const [candidate,oldCandidate,out,pwsh,consumer]=process.argv.slice(2);
fs.mkdirSync(out,{recursive:true});
const hash=p=>crypto.createHash("sha256").update(fs.readFileSync(p)).digest("hex").toUpperCase();
const source=fs.readFileSync(candidate,"utf8"),legacy=fs.readFileSync(oldCandidate,"utf8");
const execution="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",epoch=Date.parse("2026-11-05T00:00:00Z");
const checks=[],results=[];let guardIndex=0;
function check(ok,name){checks.push({name,passed:!!ok,fixtureOnly:true});if(!ok)throw Error(name);}
function psLiteral(command,variable){
 const match=command.match(new RegExp("\\$"+variable+"='((?:[^']|'')*)'"));
 return match?match[1].replaceAll("''","'"):null;
}
function actualGuard(input,label){
 const id=++guardIndex,base=path.join(out,"guards",String(id));fs.mkdirSync(base,{recursive:true});
 const inputPath=path.join(base,"input.json");fs.writeFileSync(inputPath,JSON.stringify(input));
 const script=path.join(__dirname,"StreetProbeManagedGuardFixture.ps1");
 const sources=[script,consumer,path.join(__dirname,"StreetProbeManagedState.ps1"),candidate,__filename].map(p=>{const snapshot=path.join(base,"sources",path.basename(p));fs.mkdirSync(path.dirname(snapshot),{recursive:true});fs.copyFileSync(p,snapshot);return {path:p,sha256:hash(p),snapshot,snapshotSha256:hash(snapshot)};});
 fs.writeFileSync(path.join(base,"preflight.json"),JSON.stringify({beforeEntry:true,fixtureOnly:true,input:{path:inputPath,sha256:hash(inputPath)},sources,interpreter:{path:pwsh,sha256:hash(pwsh)}}));
 const r=cp.spawnSync(pwsh,["-NoProfile","-File",script,"-InputPath",inputPath,"-ConsumerPath",consumer],{encoding:null,windowsHide:true,timeout:15000});
 fs.writeFileSync(path.join(base,"stdout.bin"),r.stdout||Buffer.alloc(0));fs.writeFileSync(path.join(base,"stderr.bin"),r.stderr||Buffer.alloc(0));
 fs.writeFileSync(path.join(base,"process.json"),JSON.stringify({label,exitCode:r.status,signal:r.signal,error:r.error?.message||null,fixtureOnly:true}));
 if(r.status!==0)throw Error("Actual guard fixture interpreter failed: "+String(r.stderr));
 return JSON.parse(r.stdout.toString("utf8").trim().replace(/^\uFEFF/,""));
}
async function scenario(name,options={},old=false){
 let now=epoch,nativeStart=null,notice=null,readIndex=0,callNumber=0,partial=null;
 const disk=new Map(),journal=new Map(),native=[],returned=[],trace=[];let failedPersist=false,usedFault=false;
 const taskAttempt=old?2:3,runId=old?"a2_street435":"a3_street435";
 const root="D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000435/attempt"+taskAttempt;
 const iso=n=>new Date(n).toISOString(),sleep=n=>{now+=n;};
 const identity={taskId:"AI-000435",attempt:taskAttempt,executionId:execution,ownerThreadId:"11111111-1111-4111-8111-111111111111",runId,projectPath:"D:\\GameProject\\Eat-What",scenePath:"Assets/Scenes/LongLoop/LongLoop_Street.unity",editorPid:12345,editorStartedAt:"2026-11-01T00:00:00Z"};
 const request={...identity,maximumSeconds:120,stages:Array.from({length:22},(_,i)=>({timeoutSeconds:i===11?24:4}))};
 const schedule={forecastAppSeconds:options.latency||6.3,perRoleReserveSeconds:.5,totalReserveSeconds:.5};
 const clean={inputRestored:true,configsRestored:true,configOwnerRestored:true,callbacksRemoved:true,temporaryObjectsDestroyed:true,noForeignOverwrite:true,error:null};
 const reports=new Map();
 const report=(status="RUNNING")=>({...identity,startedAt:iso(nativeStart??now),capabilityVersion:"AI441-sorting45-clock-ack-v1",status,passed:status==="SUCCEEDED",cleanup:clean,checks:Array.from({length:32},(_,i)=>({id:"fixture"+i,passed:true})),requiredCount:32});
 function makeNotice(){
  if(notice)return;
  const at=Math.max(now,nativeStart+16600),elapsed=(at-nativeStart)/1000;
  notice={...identity,schemaVersion:1,kind:"STREET_PROBE_CAPTURE_PENDING",phaseIndex:11,phaseId:"depth-mid",captureId:"sorting",frame:150,elapsed,stageStartedElapsed:elapsed-.6,createdAt:iso(at),stageDeadlineAt:iso(at+23400),totalDeadlineAt:iso(nativeStart+120000)};
 }
 const pendingReport=()=>({...report(),pendingCapture:"sorting",capturePendingNotificationSha256:"F".repeat(64),captureWindowStartedAt:notice.createdAt,captureStageDeadlineAt:notice.stageDeadlineAt,captureTotalDeadlineAt:notice.totalDeadlineAt});
 let returnMarker="";
 function output(value,exit=0){const r={exit_code:exit,output:returnMarker+(typeof value==="string"?value:JSON.stringify(value)),wall_time_seconds:(options.latency||6.3),fixtureOnly:true};returned.push(r);return r;}
 function persist(command){
  const encoded=psLiteral(command,"streetCarry");if(!encoded)return null;
  const packet=JSON.parse(encoded);
  if(options.persistFail&&packet.label.startsWith("captureCall")&&!failedPersist){failedPersist=true;return output("STREET_PERSIST_FAILED: fixture disk error",91);}
  for(const item of packet.records){if(disk.has(item.path)&&disk.get(item.path)!==item.json)throw Error("Fixture detected response overwrite");disk.set(item.path,item.json);}
  sleep(options.diskMs??150);
  if(packet.reservation){disk.set(packet.reservation.path,packet.reservation.token+"\n");returnMarker="STREET_RETURN_SPACE_RESERVED:"+packet.reservation.token+"\n";}
  return packet;
 }
 function validate(role,m,at){
  if(!["pendingRead","capture","captureAck"].includes(role))return {accepted:true};
  const value={notice,schedule:{...schedule,...m},remaining:role==="pendingRead"?3:role==="capture"?2:1,now:iso(at)};
  const admission=actualGuard(value,name+":"+role+":admission");
  if(!admission.accepted)return admission;
  if(role==="pendingRead")return actualGuard({...value,remaining:0,role,report:pendingReport(),identity,request,noticeSha:"F".repeat(64)},name+":pending");
  if(role==="captureAck"){
   const acknowledged=options.lateAck?Date.parse(notice.stageDeadlineAt)+1:at+400;
   const r={...report(),pendingCapture:null,captureReceiptSha256:"C".repeat(64),captureAcknowledgedAt:iso(acknowledged)};
   return actualGuard({...value,remaining:0,role,report:r,identity,request,noticeSha:"F".repeat(64),receipt:{startedAt:iso(at),finishedAt:iso(Math.max(at+1000,acknowledged+1))},captureReceipt:{sha256:"C".repeat(64)}},name+":ACK");
  }
  return admission;
 }
 async function execCommand(arg){
  const command=arg.cmd;callNumber++;trace.push({call:callNumber,at:now-epoch,command});
  const packet=persist(command);if(packet&&packet.exit_code===91)return packet;
  if(command.includes("Write-Output '{\"flushed\":true}'")){sleep((options.latency||6.3)*1000);return output({flushed:true});}
  // Old actual candidate's independent disk persistence/readSaved calls are separately charged.
  const oldFile=psLiteral(command,"p");
  if(oldFile&&command.includes("WriteAllText($p,")){
   const match=command.match(/WriteAllText\(\$p,'((?:[^']|'')*)'/);if(match)disk.set(oldFile,match[1].replaceAll("''","'"));
   sleep((options.latency||6.3)*1000);return output("");
  }
  if(command.includes("Get-Content -LiteralPath")&&command.includes("-result.json")){
   const match=command.match(/\/([A-Za-z0-9]+)-result\.json/);sleep((options.latency||6.3)*1000);return output(reports.get(match?.[1])||report());
  }
  if(command.includes("/read-hint.ps1")){
   makeNotice();sleep((options.latency||6.3)*1000);
   return output({status:"CAPTURE_HINT",cursor:{offset:10,tail:"",lines:150},notification:notice,notificationSha256:"F".repeat(64),nativeSuccessProven:false});
  }
  const roleMatch=command.match(/\/run-one\.ps1'\s+-Name\s+'([^']+)'/);
  if(!roleMatch){
   sleep((options.latency||6.3)*1000);
   if(command.includes("/select-ready.ps1"))return output({next:"BEGIN",nativeSuccessProven:false});
   if(command.includes("-NativeCiContextPath")){
    for(const role of native.filter(x=>x.success).map(x=>x.role)){check(disk.has(root+"/"+role+"-app-response.json"),name+": before consumer actual "+role+" app response on disk");}
   }
   return output("FIXTURE_ONLY local helper complete");
  }
  const role=roleMatch[1],metricsText=psLiteral(command,"m"),metrics=metricsText?JSON.parse(metricsText):schedule;
  if(role==="pendingRead"&&!old){
   makeNotice();sleep(Math.max(0,Date.parse(notice.createdAt)-now));
   if(options.totalTimeout)now=nativeStart+120001;
   if(options.insufficient)now=Date.parse(notice.stageDeadlineAt)-10000;
  }
  if(options.unknownBegin&&role==="register"&&!usedFault){usedFault=true;sleep(6300);throw Error("FIXTURE_ONLY unknown Begin tool transport");}
  if(options.unknownExit&&role==="pendingRead"&&!usedFault){usedFault=true;sleep(6300);return output("STREET_NATIVE_UNKNOWN: fixture timeout",1);}
  const admission=old?{accepted:true}:validate(role,metrics,now);
  if(!admission.accepted){sleep((options.latency||6.3)*1000);return output("FIXTURE_ONLY actual production stage guard rejected: "+admission.error,1);}
  check(!native.some(x=>x.role===role),name+": no duplicate official role "+role);
  if(role==="register")nativeStart=now+500;
  if(role==="complete"){sleep(Math.max(0,nativeStart+55000-now));}
  if(role==="capture"&&options.foreignRun){sleep(6300);return output({action:"TOOL_CAPTURED",taskId:"AI-000435",attempt:taskAttempt,executionId:"b".repeat(32),exitCode:0},0);}
  let value=role.startsWith("ready")?{ready:true}:role==="register"?report():role==="pendingRead"?pendingReport():["complete","cleanup"].includes(role)?report("SUCCEEDED"):report("FAILED");
  reports.set(role,value);native.push({role,at:now-epoch,processRecorded:true,usageRecorded:true,finishRecorded:true,success:true});
  const summary={action:"TOOL_CAPTURED",taskId:"AI-000435",attempt:taskAttempt,executionId:execution,processId:native.length+100,exitCode:0,fixtureOnly:true};
  sleep((options.latency||6.3)*1000);
  const final=output(summary);
  if(options.partial&&role==="pendingRead"&&!partial){partial=final;const r={session_id:77,output:"",wall_time_seconds:1,fixtureOnly:true};returned.push(r);return r;}
  return final;
 }
 const tools={
  exec_command:execCommand,
  write_stdin:async()=>{sleep(500);check(!!partial,name+": genuine separate fixture continuation");const r=partial;partial=null;return r;},
  apply_patch:async patch=>{
   if(options.patchFail)return {isError:true,fixtureOnly:true};
   check(!/^\*\*\* Update File:/m.test(patch),name+": no reservation rewrite");
   for(const part of patch.split(/^\*\*\* Add File: /m).slice(1)){const at=part.indexOf("\n"),target=part.slice(0,at),json=part.slice(at+1).split("\n").filter(x=>x.startsWith("+")).map(x=>x.slice(1)).join("\n")+"\n";check(!disk.has(target),name+": CreateNew return sidecar");JSON.parse(json);disk.set(target,json);}
   return {fixtureOnly:true};
  }
 };
 class FixtureDate extends Date{constructor(...args){super(...(args.length?args:[now]));}static now(){return now;}}
 const context={tools,store:(k,v)=>journal.set(k,JSON.parse(JSON.stringify(v))),load:k=>journal.get(k),text:()=>{},notify:()=>{},Date:FixtureDate,setTimeout:(fn,ms)=>{sleep(ms);fn();},clearTimeout:()=>{}};
 let error=null;
 try{await new vm.Script("(async()=>{"+(old?legacy:source).replaceAll("__CURRENT_EXECUTION_ID__",execution)+"})()",{filename:candidate}).runInNewContext(context,{timeout:10000});}catch(e){error=String(e);}
 const final=journal.get("AI435-a3-returned-objects");
 const record={name,fixtureOnly:true,nativeEvidenceAccepted:false,error,native,returnedObjectCount:returned.length,nowSeconds:(now-epoch)/1000,completed:!!final?.completed,pendingRawCount:final?.pending?.length??null,finalResponseOnDisk:!!final?.finalStorageResponse&&disk.has(final.finalStorageResponse.path),trace};
 fs.writeFileSync(path.join(out,name+".json"),JSON.stringify(record,null,2));results.push(record);
 return {record,final,disk};
}
(async()=>{
 const ok=await scenario("same-path-6.3s-carry");check(ok.record.completed&&!ok.record.error&&ok.record.finalResponseOnDisk,"Actual candidate completes6.3s roundtrips plus150ms writes with every native accounting before return");
 check(ok.record.native.filter(x=>x.role==="capture").length===1&&ok.record.native.some(x=>x.role==="captureAck"),"Exactly one actual candidate capture/ACK in explicit virtual model");
 const slow=await scenario("same-path-6.9s-carry",{latency:6.9,diskMs:100});check(slow.record.completed&&!slow.record.error,"Actual candidate completes6.9s +100ms front writes under stated virtual model");
 const old=await scenario("old-actual-extra-hops",{},true);check(!!old.record.error&&old.record.error.includes("Insufficient")&&!old.record.native.some(x=>x.role==="capture"),"Actual readonly old scheduler independent persist/readSaved causes24s admission rejection");
 for(const [name,opt] of [["remaining-insufficient",{insufficient:true}],["late-ACK",{lateAck:true}],["total120-expired",{totalTimeout:true}],["foreign-run",{foreignRun:true}]]){
  const r=await scenario(name,opt);check(!!r.record.error&&!r.record.completed&&r.record.finalResponseOnDisk,name+": real delivered flow rejects and final raw response flushes");
 }
 const session=await scenario("partial-final-separate",{partial:true});check(session.record.completed&&!session.record.error&&session.final.objects.some(x=>(x.originPath||x.path).includes("partial")),"Session partial/final kept separately; no fabricated merged exec");
 const unknown=await scenario("unknown-Begin",{unknownBegin:true});check(!!unknown.record.error&&!unknown.record.native.some(x=>x.role==="cancel")&&unknown.record.native.filter(x=>x.role==="stop").length===1,"Unknown Begin never blind Cancel; one confirmed Play Stop lifecycle");
 const timed=await scenario("unknown-pending-result",{unknownExit:true});check(!!timed.record.error&&!timed.record.native.some(x=>x.role==="capture")&&timed.record.finalResponseOnDisk,"Unknown result retained without capture/replay");
 const disk=await scenario("disk-fail-before-capture",{persistFail:true});check(!!disk.record.error&&!disk.record.native.some(x=>x.role==="capture")&&disk.final.diskFailed&&disk.record.finalResponseOnDisk,"Disk failure blocks next native; finally stores exact original failed object");
 const patch=await scenario("final-write-rejected",{patchFail:true});check(!!patch.record.error&&!!patch.final.flushError,"Rejected final file write leaves explicit store/reservation gap and cannot pass");
 fs.writeFileSync(path.join(out,"result.json"),JSON.stringify({fixtureOnly:true,nativeEvidenceAccepted:false,checks,scenarioCount:results.length,actualCandidateSha256:hash(candidate),oldActualSchedulerSha256:hash(oldCandidate),guardsUseActualPowerShellSources:true,notRealUnityLatencyGuarantee:true},null,2));
 process.stdout.write(JSON.stringify({fixtureOnly:true,status:"PASS",checks:checks.length,scenarios:results.length,nativeEvidenceAccepted:false})+"\n");
})().catch(error=>{fs.writeFileSync(path.join(out,"failure.json"),JSON.stringify({fixtureOnly:true,error:String(error),checks,results},null,2));process.stderr.write(String(error.stack)+"\n");process.exitCode=1;});
