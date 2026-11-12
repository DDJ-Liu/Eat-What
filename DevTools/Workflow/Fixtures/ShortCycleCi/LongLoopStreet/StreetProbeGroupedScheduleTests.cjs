// Explicit virtual transport/time model. The delivered scheduler executes byte-for-byte.
// Actual PowerShell wrapper/group evidence is exercised separately by StreetProbeCaptureGroupFixture.
const fs=require('fs'),vm=require('vm'),path=require('path'),crypto=require('crypto'),cp=require('child_process');
const [candidate,out,pwsh]=process.argv.slice(2);fs.mkdirSync(out,{recursive:true});
const source=fs.readFileSync(candidate,'utf8'),ex='a'.repeat(32),checks=[],results=[];
const check=(ok,name)=>{checks.push({name,passed:!!ok,fixtureOnly:true});if(!ok)throw Error(name)};
function literal(cmd,key){return cmd.match(new RegExp('\\$'+key+"='((?:[^']|'')*)'"))?.[1].replaceAll("''","'")}
async function run(name,opt={}){
 const attempt=opt.compiled?8:7,runId='a'+attempt+'_street435';let selectedInterfaces=0;
 let returnMarker='';let now=Date.parse('2026-11-05T00:00:00Z'),begin=0,partial=null,faulted=false;const disk=new Map(),store=new Map(),native=[],returned=[],trace=[];
 const root='D:/GameProject/Eat-What/.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000435/attempt'+attempt;
 function response(value,code=0){const r={exit_code:code,output:returnMarker+(typeof value==='string'?value:JSON.stringify(value)),wall_time_seconds:10.266,fixtureOnly:true};returned.push(r);return r}
 function receipt(role){native.push({role,at:now,processRecorded:true,usageRecorded:true,finishRecorded:true,simulated:true});return {action:'TOOL_CAPTURED',taskId:'AI-000435',attempt,executionId:ex,exitCode:0,fixtureOnly:true}}
 async function exec(arg){
  const cmd=arg.cmd;trace.push({at:now,cmd});const packet=literal(cmd,'streetCarry');
  if(cmd.includes('/close-evidence.ps1')){returnMarker='';return response({action:'STREET_FINAL_EVIDENCE_READY',taskId:'AI-000435',attempt,executionId:ex,returnNamespaceReserved:true,reservationToken:'c'.repeat(32),finalWholeTaskPackageBytes:opt.finalTooLarge?9999900:7000000,reservedFinalReturnBytes:65536,fixtureOnly:true});}
  if(packet){const p=JSON.parse(packet);if(opt.persistFail&&p.label.startsWith('captureGroup')&&!faulted){faulted=true;return response('STREET_PERSIST_FAILED',91)}
   for(const r of p.records){check(!disk.has(r.path)||disk.get(r.path)===r.json,name+': no response overwrite');disk.set(r.path,r.json)}
   if(p.reservation){disk.set(p.reservation.path,p.reservation.token+'\n');returnMarker='STREET_RETURN_SPACE_RESERVED:'+p.reservation.token+'\n';}now+=150;
  }
  if(cmd.includes('/capture-window.ps1')){
   const start=now,deadline=now+23250;
   for(const role of ['pendingRead','capture','captureAck']){
    if(opt.memberFail===role||opt.unknown===role){now+=10266;return response(opt.unknown?'STREET_NATIVE_UNKNOWN: simulated process unknown':'FIXTURE_ONLY member failure',1)}
    if(opt.accounting&&role==='capture'){now+=10266;return response('FIXTURE_ONLY missing accounting; next native blocked',1)}
    if(opt.diskMember&&role==='captureAck'){now+=10266;return response('FIXTURE_ONLY prior capture receipt persistence failed',1)}
    if(opt.lateAck&&role==='captureAck'){now=deadline+1;return response('FIXTURE_ONLY actual ACK late',1)}
    if(opt.totalTimeout){now=begin+120001;return response('FIXTURE_ONLY total120 expired',1)}
    now+=2200;receipt(role);
   }
   const frame={schemaVersion:1,kind:'STREET_CAPTURE_GROUP_RAW_V1',groupId:'c'.repeat(32),taskId:'AI-000435',attempt,executionId:opt.foreign?'b'.repeat(32):ex,runId,memberCount:3,members:['pendingRead','capture','captureAck'].map((role,index)=>({role,index,fixtureOnly:true})),fixtureOnly:true};
   if(opt.order)frame.members.reverse();if(opt.missing)frame.members.pop();
   now+=10266;const result=response(frame);
   if(opt.partial){partial=result;const p={session_id:77,output:'',wall_time_seconds:1,fixtureOnly:true};returned.push(p);return p}
   check(now-start>10266&&now-start<24000,name+': one10.266s outer return plus three2.2s local members');return result;
  }
  const m=cmd.match(/\/run-one\.ps1'\s+-Name\s+'([^']+)'/),role=m?.[1];now+=10266;
  if(role){if((opt.queryFail&&role==='shortCapabilities')||(opt.importFail&&role==='normalImport'))return response('FIXTURE_ONLY bootstrap process failure',1);if(opt.unknownBegin&&role==='register')throw Error('FIXTURE_ONLY unknown Begin transport');if(role==='register')begin=now;return response(receipt(role))}
  if(cmd.includes('/select-ready.ps1'))return response({next:cmd.includes('-CompiledInterface')?(opt.unknownCapability?'UNKNOWN':(opt.oldDomain&&selectedInterfaces++===0?'IMPORT_ONCE':(opt.readbackOld?'OLD_IMPLEMENTATION':'CURRENT'))):'BEGIN'});
  if(cmd.includes('/safety-stop.ps1'))return response(receipt('stop'));
  if(cmd.includes('-NativeCiContextPath')){
   check(disk.has(root+'/captureGroup-app-response.json'),name+': actual shared group object persisted before Final consumer');
   check(!disk.has(root+'/capture-app-response.json')&&!disk.has(root+'/captureAck-app-response.json'),name+': no fake per-member app0 objects');
  }
  return response('FIXTURE_ONLY local helper');
 }
 class D extends Date{constructor(...a){super(...(a.length?a:[now]))}static now(){return now}}
 const tools={exec_command:exec,write_stdin:async()=>{check(!!partial,name+': separate continuation');const r=partial;partial=null;now+=500;return r},apply_patch:async p=>{
  if(opt.patchFail)return {isError:true};
  check(!/^\*\*\* Update File:/m.test(p),name+": no existing reservation update");
  for(const part of p.split(/^\*\*\* Add File: /m).slice(1)){const at=part.indexOf("\n"),target=part.slice(0,at),json=part.slice(at+1).split("\n").filter(x=>x.startsWith("+")).map(x=>x.slice(1)).join("\n")+"\n";check(!disk.has(target),name+": new sidecar path only");JSON.parse(json);disk.set(target,json);}
  return {fixtureOnly:true};
 }};
 const invocation=opt.compiled?source.replace(', "__CURRENT_EXECUTION_ID__");',', "__CURRENT_EXECUTION_ID__", {attempt:8,runId:"a8_street435",compiledPayloads:true});'):source;
 let error=null;try{await new vm.Script('(async()=>{'+invocation.replaceAll('__CURRENT_EXECUTION_ID__',ex)+'})()', {filename:candidate}).runInNewContext({tools,store:(k,v)=>store.set(k,JSON.parse(JSON.stringify(v))),text:()=>{},Date:D},{timeout:10000})}catch(e){error=String(e)}
 const final=store.get('AI435-a'+attempt+'-returned-objects');const r={name,error,completed:!!final?.completed,native,rawReturned:returned.length,pendingRaw:final?.pending?.length,finalResponseOnDisk:!!final?.finalStorageResponse&&disk.has(final.finalStorageResponse.path),fixtureOnly:true,nativeEvidenceAccepted:false,trace};results.push(r);fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify(r,null,2));return {r,final,disk};
}
// Execute the delivered storage functions with real PowerShell/filesystem APIs.
// Only transport return shapes are simulated; there is no native/SDK success shortcut here.
async function actualStorageBoundary(name,options={}){
 const base=path.join(out,'actual-storage',name);fs.mkdirSync(base,{recursive:true});
 const output=path.join(base,'attempt');fs.mkdirSync(output);const raw=[],persisted=[],trace=[];
 const inputShapes=[{exit_code:0,output:'成功🍅\n` $()'}, {exit_code:1,output:'failed'}, {session_id:71,output:'partial'}, {exit_code:0,output:'terminal'}, {exit_code:0,output:'{"typed":true}\n*** Update File: data only\n*** Add File: still raw data'}, {exit_code:91,output:'storage failed'}, {session_id:72,output:''}, {exit_code:6,output:'unknown native'}];
 const state={pending:[],objects:[]};let pendingTerminal=null,patchCalls=0,childCalls=0;
 for(let i=0;i<inputShapes.length;i++){const item={path:path.join(output,'original-'+i+'.json'),json:JSON.stringify(inputShapes[i])};state.pending.push(item);state.objects.push({...item});}
 if(options.batchConflict)fs.writeFileSync(state.pending[0].path,'FOREIGN_ORIGINAL');
 if(options.namespaceConflict){fs.mkdirSync(path.join(output,'finalFlush1-returned'));fs.writeFileSync(path.join(output,'finalFlush1-returned','foreign.txt'),'FOREIGN_NAMESPACE');}
 const shaText=source.slice(source.indexOf('function streetReturnedSha256('),source.indexOf('async function runStreetProbeManagedSchedule('));
 const functions=source.slice(source.indexOf(' function remember('),source.indexOf(' async function call('));
 const flush=source.slice(source.indexOf(' async function flush(){'),source.indexOf(' let failure=null;'));
 const hashContext={};vm.runInNewContext(shaText,hashContext);
 for(const text of ['', 'abc','中文🍅\n',JSON.stringify({value:'\ud800'})])check(hashContext.streetReturnedSha256(text)===crypto.createHash('sha256').update(text).digest('hex').toUpperCase(),name+': actual returned-object SHA matches standard UTF8');
 const tools={
  exec_command:async arg=>{
   childCalls++;check(arg.cmd.includes('Write-Output')&&!arg.cmd.includes('Invoke-TaskTool'),name+': storage-only actual child, no native role');
   const pre={beforeEntry:true,source:candidate,sourceSha256:crypto.createHash('sha256').update(source).digest('hex').toUpperCase(),test:__filename,testSha256:crypto.createHash('sha256').update(fs.readFileSync(__filename)).digest('hex').toUpperCase(),interpreter:pwsh,interpreterSha256:crypto.createHash('sha256').update(fs.readFileSync(pwsh)).digest('hex').toUpperCase(),command:[pwsh,'-NoProfile','-Command',arg.cmd],fixtureOnly:true};
   fs.writeFileSync(path.join(base,'preflight.json'),JSON.stringify(pre));
   const start=Date.now(),p=cp.spawnSync(pwsh,['-NoProfile','-Command',arg.cmd],{encoding:null,windowsHide:true,timeout:30000});
   fs.writeFileSync(path.join(base,'stdout.bin'),p.stdout||Buffer.alloc(0));fs.writeFileSync(path.join(base,'stderr.bin'),p.stderr||Buffer.alloc(0));
   fs.writeFileSync(path.join(base,'process.json'),JSON.stringify({pid:p.pid,exitCode:p.status,signal:p.signal,error:p.error?.message||null,startedAt:new Date(start).toISOString(),finishedAt:new Date().toISOString()}));
   const terminal={exit_code:p.status,output:(p.stdout||Buffer.alloc(0)).toString('utf8')+(p.stderr||Buffer.alloc(0)).toString('utf8'),wall_time_seconds:(Date.now()-start)/1000,fixtureOnly:true};
   if(options.partial){pendingTerminal={...terminal,output:''};const partial={session_id:77,output:terminal.output,wall_time_seconds:terminal.wall_time_seconds,fixtureOnly:true};raw.push(partial);return partial;}
   raw.push(terminal);return terminal;
  },
  write_stdin:async()=>{if(options.transportFailure)throw Error('EXPLICIT_FIXTURE_TRANSPORT_UNKNOWN');check(!!pendingTerminal,name+': known separate continuation');const r=pendingTerminal;pendingTerminal=null;raw.push(r);return r;},
  apply_patch:async patch=>{
   patchCalls++;check(!patch.includes('*** Update File:'),name+': actual file boundary rejects Update');
   const records=patch.split(/^\*\*\* Add File: /m).slice(1).map(part=>{const n=part.indexOf('\n');return {file:part.slice(0,n),content:part.slice(n+1).split('\n').filter(s=>s.startsWith('+')).map(s=>s.slice(1)).join('\n')+'\n'};});
   for(const [index,r] of records.entries()){
    check(path.resolve(r.file).startsWith(path.resolve(output)+path.sep),name+': add-only target stays in actual fixture attempt');
    if(options.addCollision&&index===0)fs.writeFileSync(r.file,'FOREIGN_SIDECAR');
    try{fs.writeFileSync(r.file,r.content,{flag:'wx'});persisted.push(r.file);}catch(error){return {isError:true,error:String(error)};}
   }
   return {isError:false,fixtureOnly:true};
  }
 };
 const context={host:{tools,store:()=>{},text:()=>{}},state,out:output,root:path.resolve(base),execution:ex,need:(v,m)=>{if(!v)throw Error(m)},quote:s=>"'"+String(s).replaceAll("'","''")+"'",journal:()=>{},sequence:0};
 let error=null;try{await vm.runInNewContext('(async()=>{'+shaText+functions+flush+'await flush();})()',context);}catch(e){error=String(e);}
 fs.writeFileSync(path.join(base,'caught-result.json'),JSON.stringify({error,childCalls,patchCalls,rawReturns:raw.length},null,2));
 const mapFile=path.join(output,'finalFlush1-returned','return-map.json');
 if(options.namespaceConflict){check(!!error&&patchCalls===0&&state.pending.length>inputShapes.length,name+': unconfirmed namespace does not write sidecars or replay');check(fs.readFileSync(path.join(output,'finalFlush1-returned','foreign.txt'),'utf8')==='FOREIGN_NAMESPACE',name+': namespace original preserved');}
 else if(options.addCollision){check(!!error&&state.pending.length>0&&fs.readFileSync(path.join(output,'finalFlush1-returned','000.json'),'utf8')==='FOREIGN_SIDECAR',name+': CreateNew conflict never overwrites or claims persisted');}
 else{
  check(fs.existsSync(mapFile),name+': real returned-object map saved');const map=JSON.parse(fs.readFileSync(mapFile));
  check(fs.readFileSync(map.originalReservation,'utf8')===map.reservationToken+'\r\n'||fs.readFileSync(map.originalReservation,'utf8')===map.reservationToken+'\n',name+': exact original reservation remains unchanged');
  check(map.entries.length===raw.length,name+': every actual storage child/partial return has a sidecar');
  for(let i=0;i<raw.length;i++){const e=map.entries[i],b=fs.readFileSync(e.path);check(b.toString('utf8')===JSON.stringify(raw[i])+'\n',name+': actual return bytes saved with declared LF');check(crypto.createHash('sha256').update(b).digest('hex').toUpperCase()===e.fileSha256&&crypto.createHash('sha256').update(JSON.stringify(raw[i])).digest('hex').toUpperCase()===e.objectJsonSha256,name+': exact sidecar and object hashes verified');}
  if(options.batchConflict){check(!!error&&state.pending.length===inputShapes.length,name+': failed batch remains pending after final failure return was preserved');check(fs.readFileSync(state.pending[0].path,'utf8')==='FOREIGN_ORIGINAL',name+': earlier original not overwritten');}
  else if(options.transportFailure){check(!!error&&!state.finalStorageResponse&&state.lastStorageResponse,name+': known partial saved, terminal unknown remains honest');}
  else{check(!error&&state.pending.length===0,name+': actual storage completes without memory-only originals');for(let i=0;i<inputShapes.length;i++)check(fs.readFileSync(path.join(output,'original-'+i+'.json'),'utf8')===JSON.stringify(inputShapes[i]),name+': eight original return shapes preserved');}
 }
 check(childCalls===1,name+': no extra storage exec recursion or business replay');
 const result={name,error,childCalls,patchCalls,rawReturns:raw.length,persisted,fixtureOnly:true,nativeUnityInvoked:false};fs.writeFileSync(path.join(base,'result.json'),JSON.stringify(result,null,2));return result;
}
async function finalPackageBoundary(name,large=false){
 const base=path.join(out,'actual-package',name),target=path.join(base,'attempt');fs.mkdirSync(target,{recursive:true});
 const returns=[],stores=new Map(),q=s=>"'"+String(s).replaceAll("'","''")+"'";
 const command="$ErrorActionPreference='Stop';$o="+q(target)+";[void][IO.Directory]::CreateDirectory((Join-Path $o 'final-package-returns'));[IO.File]::WriteAllText((Join-Path $o 'final-package-returns/reservation.json'),'FIXTURE_ONLY');"+(large?"[IO.File]::WriteAllBytes((Join-Path $o 'large-original.bin'),[byte[]]::new(10000000));":"")+"$size=[long](Get-ChildItem -LiteralPath $o -File -Recurse|Measure-Object Length -Sum).Sum;@{action='STREET_FINAL_EVIDENCE_READY';taskId='AI-000435';attempt=7;executionId='"+ex+"';returnNamespaceReserved=$true;reservationToken='"+'c'.repeat(32)+"';finalWholeTaskPackageBytes=$size;reservedFinalReturnBytes=65536;fixtureOnly=$true}|ConvertTo-Json -Compress";
 const tools={exec_command:async()=>{fs.writeFileSync(path.join(base,'preflight.json'),JSON.stringify({beforeEntry:true,command:[pwsh,'-NoProfile','-Command',command],sourceSha256:crypto.createHash('sha256').update(source).digest('hex'),fixtureOnly:true}));const p=cp.spawnSync(pwsh,['-NoProfile','-Command',command],{encoding:null,windowsHide:true});fs.writeFileSync(path.join(base,'stdout.bin'),p.stdout);fs.writeFileSync(path.join(base,'stderr.bin'),p.stderr);fs.writeFileSync(path.join(base,'process.json'),JSON.stringify({pid:p.pid,exitCode:p.status,fixtureOnly:true}));const r={exit_code:p.status,output:p.stdout.toString('utf8'),wall_time_seconds:1,fixtureOnly:true};returns.push(r);return r;},apply_patch:async patch=>{check(!/^\*\*\* Update File:/m.test(patch),name+': actual final map is Add only');for(const part of patch.split(/^\*\*\* Add File: /m).slice(1)){const n=part.indexOf('\n'),file=part.slice(0,n),content=part.slice(n+1).split('\n').filter(s=>s.startsWith('+')).map(s=>s.slice(1)).join('\n')+'\n';fs.writeFileSync(file,content,{flag:'wx'});}return {isError:false,fixtureOnly:true};}};
 const declarations=source.slice(source.indexOf('function streetReturnedSha256('),source.indexOf('async function runStreetProbeManagedSchedule('));
 const context={host:{tools,store:(k,v)=>stores.set(k,v)},root:base,target,ex};let error=null;
 try{await vm.runInNewContext(declarations+'streetFinishEvidencePackage(host,root,target,ex,{path:target+"/consumer.json",json:"{}"})',context);}catch(e){error=String(e)}
 const map=JSON.parse(fs.readFileSync(path.join(target,'final-package-returns/final-package-map.json'))),raw=fs.readFileSync(path.join(target,'final-package-returns/000.json'),'utf8');
 const total=dir=>fs.readdirSync(dir,{withFileTypes:true}).reduce((n,e)=>n+(e.isDirectory()?total(path.join(dir,e.name)):fs.statSync(path.join(dir,e.name)).size),0);
 check(map.finalWholeTaskPackageBytes===total(target),name+': actual filesystem final total equals measured base plus all exact final UTF8 files');
 check(raw===JSON.stringify(returns[0])+'\n',name+': actual closing exec object preserved byte for byte');
 check(large?!!error&&!map.passed:!error&&map.passed,name+': actual oversize cannot report completion');
 fs.writeFileSync(path.join(base,'result.json'),JSON.stringify({error,actualBytes:total(target),map,fixtureOnly:true,nativeUnityInvoked:false},null,2));
}
(async()=>{
 await finalPackageBoundary('normal-final');await finalPackageBoundary('over-limit-final',true);
 for(const [name,opt] of [['normal',{}],['partial',{partial:true}],['batch-failure',{batchConflict:true}],['namespace-conflict',{namespaceConflict:true}],['add-conflict',{addCollision:true}],['transport-unknown',{partial:true,transportFailure:true}]])await actualStorageBoundary(name,opt);
 check(3*(10.266+.5)+.5===32.798&&32.798>24,'Original actual a3 three-app forecast32.798 cannot enter24; not a second actual ENGINE failure');
 for(const oldDomain of [false,true]){
  const v=await run('compiled8-'+(oldDomain?'import':'current'),{compiled:true,oldDomain});
  check(v.r.completed&&!v.r.error,'Reviewed attempt8 '+(oldDomain?'single import and readback':'current Domain without import')+' completes original full schedule');
  check(v.r.native.filter(x=>x.role==='shortCapabilities').length===1&&v.r.native.filter(x=>x.role==='normalImport').length===(oldDomain?1:0)&&v.r.native.filter(x=>x.role==='importReadback').length===(oldDomain?1:0),'Attempt8 exact bootstrap count with no preflight replay');
  check(v.r.native.length<=57&&v.r.native.filter(x=>x.role==='preflight').length===1,'Attempt8 success includes bootstrap inside original57 ceiling');
 }
 for(const [name,fault] of [['unknown-capability',{unknownCapability:true}],['query-fail',{queryFail:true}],['import-fail',{oldDomain:true,importFail:true}],['old-readback',{oldDomain:true,readbackOld:true}]]){
  const v=await run('compiled8-'+name,{compiled:true,...fault});check(!!v.r.error&&!v.r.completed&&!v.r.native.some(x=>['play','register','capture'].includes(x.role)),'Attempt8 '+name+' stops before Play/Begin/capture');
  check(v.r.native.filter(x=>x.role==='normalImport').length<=1&&v.r.native.filter(x=>x.role==='shortCapabilities').length<=1,'Attempt8 '+name+' never warms/replays/imports twice');
 }
 const ok=await run('outer10.266-group');check(ok.r.completed&&!ok.r.error&&ok.r.finalResponseOnDisk,'Actual delivered grouped scheduler completes explicit outer10.266s model');
 check(ok.r.native.filter(x=>['pendingRead','capture','captureAck'].includes(x.role)).length===3,'Model records all three ordered accounted members');
 for(const [n,o] of [['final-package-oversize',{finalTooLarge:true}],['pending-fail',{memberFail:'pendingRead'}],['capture-fail',{memberFail:'capture'}],['ack-fail',{memberFail:'captureAck'}],['unknown-pending',{unknown:'pendingRead'}],['unknown-capture',{unknown:'capture'}],['unknown-ack',{unknown:'captureAck'}],['no-accounting',{accounting:true}],['member-persist',{diskMember:true}],['late-ack',{lateAck:true}],['total120',{totalTimeout:true}],['foreign',{foreign:true}],['order',{order:true}],['missing',{missing:true}]]){
  const v=await run(n,o);check(!!v.r.error&&!v.r.completed&&v.r.finalResponseOnDisk,n+': fails with original returned objects flushed');
  check(v.r.native.filter(x=>x.role==='stop').length===1,n+': one ordinary Stop for confirmed Play');
  check(v.r.native.filter(x=>x.role==='capture').length<=1,n+': no capture replay');
 }
 const partial=await run('partial',{partial:true});check(partial.r.completed&&partial.final.objects.some(x=>(x.originPath||x.path).includes('partial')),'Partial/final actual app objects remain separate');
 const unknown=await run('unknown-begin',{unknownBegin:true});check(!!unknown.r.error&&!unknown.r.native.some(x=>x.role==='cancel')&&unknown.r.native.filter(x=>x.role==='stop').length===1,'Unknown Begin no blind Cancel, one Stop');
 const disk=await run('persist-before-group',{persistFail:true});check(!!disk.r.error&&disk.final.diskFailed&&!disk.r.native.some(x=>x.role==='capture'),'Front persistence failure blocks all next group actions');
 const patch=await run('final-flush-fail',{patchFail:true});check(!!patch.r.error&&!!patch.final.flushError,'Rejected final own-return storage cannot pass');
 fs.writeFileSync(path.join(out,'result.json'),JSON.stringify({fixtureOnly:true,nativeEvidenceAccepted:false,notRealUnityLatencyGuarantee:true,actualCandidateSha256:crypto.createHash('sha256').update(source).digest('hex').toUpperCase(),checks,scenarioCount:results.length},null,2));process.stdout.write(JSON.stringify({checks:checks.length,scenarios:results.length,fixtureOnly:true})+'\n');
})().catch(e=>{fs.writeFileSync(path.join(out,'failure.json'),JSON.stringify({error:String(e),checks,results},null,2));process.stderr.write(String(e.stack));process.exitCode=1});
