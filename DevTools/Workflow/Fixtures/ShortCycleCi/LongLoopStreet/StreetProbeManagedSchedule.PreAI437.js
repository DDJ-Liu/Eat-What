// @exec: {"yield_time_ms": 10000, "max_output_tokens": 1200}
// OUTPUT-ONLY candidate for the fixed ENGINE_CLI chat, never executed by CODE.
// Copy into functions.exec only after CONTROL review, actual claim, SDK compilation and context preparation.
// Keep this one exec cell alive; functions.wait returns progress without a model turn inside the capture gate.
const root = "D:/GameProject/Eat-What";
const out = root + "/.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000435/attempt2";
const boundExecution = "__CURRENT_EXECUTION_ID__";
if (!/^[0-9a-f]{32}$/.test(boundExecution)) throw Error("Bind actual new claim before any native command");
const ps = s => "'" + String(s).replaceAll("'", "''") + "'";
const need = (v,m) => { if (!v) throw Error(m); };
let appIndex=0,nativeCount=0,readCount=0,captureIssued=false,playIssued=false,registered=false,stopIssued=false,failing=false;
let maxAppSeconds=0,cursor=null,notice=null,deadline=0,terminalRead=null;
const appSamples=[];
async function persist(file,value) {
  // Preserve the actual object exactly as returned. JSON.stringify is passed as a single-quoted PS literal.
  const json=JSON.stringify(value);
  const command="$p="+ps(file)+";if(Test-Path -LiteralPath $p){throw 'No response overwrite'};[IO.File]::WriteAllText($p,"+ps(json)+",[Text.UTF8Encoding]::new($false))";
  const saved=await tools.exec_command({cmd:command,workdir:root,max_output_tokens:1000});
  need(saved.exit_code===0&&!saved.session_id,"Cannot persist actual app object; stop without replay");
}
async function execActual(command,label,native=false) {
  const before=Date.now();
  let value=await tools.exec_command({cmd:command,workdir:root,yield_time_ms:10000,max_output_tokens:5000});
  if(value.session_id) {
    await persist(out+"/"+label+"-app-partial-"+(++appIndex)+".json",value);
    while(value.session_id) {
      value=await tools.write_stdin({session_id:value.session_id,chars:"",yield_time_ms:1000,max_output_tokens:5000});
      if(value.session_id) await persist(out+"/"+label+"-app-partial-"+(++appIndex)+".json",value);
    }
  }
  // Do not combine chunks into a fabricated exec object. A truncated/incomplete final output fails.
  await persist(out+"/"+label+"-app-response.json",value);
  need(value.exit_code===0&&!value.session_id,label+" actual app process failed");
  if(native) {
    let summary;try{summary=JSON.parse(value.output);}catch{throw Error(label+" final app output is incomplete; preserve chunks and stop");}
    need(summary.action==="TOOL_CAPTURED"&&summary.exitCode===0&&summary.taskId==="AI-000435"&&summary.attempt===2&&summary.executionId===boundExecution,label+" genuine managed child proof required");
    const seconds=(Date.now()-before)/1000;
    maxAppSeconds=Math.max(maxAppSeconds,seconds);appSamples.push({role:label,appSecondsIncludingPersistence:seconds});
  }
  return value;
}
async function readSaved(label) {
  const command="$v=Get-Content -LiteralPath "+ps(out+"/"+label+"-result.json")+" -Raw|ConvertFrom-Json -DateKind String;if($v-is[string]){$v=$v|ConvertFrom-Json -DateKind String};$v|ConvertTo-Json -Depth 80 -Compress";
  const actual=await tools.exec_command({cmd:command,workdir:root,max_output_tokens:15000});
  need(actual.exit_code===0&&!actual.session_id,"Cannot read saved actual native result");
  return JSON.parse(actual.output);
}
async function native(label,payload="") {
  need(nativeCount<(failing?60:57),"Official call budget exhausted");
  nativeCount++; // Mark issued BEFORE invoking. Unknown calls are never replayed.
  const cmd="& "+ps(out+"/run-one.ps1")+" -Name "+ps(label)+(payload?" -Payload "+ps(payload):"");
  return execActual(cmd,label,true);
}
async function managedRead(label) {
  need(++readCount<=Number(storeReadLimit),"Read budget exhausted");
  await native(label,"read");
  return readSaved(label);
}
function admit(remainingRoles) {
  // Measured app latency from already-required calls, including saving original response objects.
  // This is an admission estimate, not a guarantee that 35s worst case fits24s.
  need(notice&&deadline>Date.now(),"Sorting deadline already expired");
  need((deadline-Date.now())/1000>remainingRoles*(maxAppSeconds+.5)+.5,"Insufficient measured window; controlled cancel");
}
async function hint() {
  // Local reads only; no Invoke-TaskTool and no synthetic native receipt.
  const cmd="& "+ps(out+"/read-hint.ps1")+(cursor?" -CursorJson "+ps(JSON.stringify(cursor)):"");
  const response=await tools.exec_command({cmd,workdir:root,max_output_tokens:4500});
  need(response.exit_code===0&&!response.session_id,"Local hint malformed/foreign/discontinuous");
  const h=JSON.parse(response.output);need(h.nativeSuccessProven===false,"Hint must never claim native proof");
  cursor=h.cursor;return h;
}
// These booleans come from the concrete CONTROL-reviewed attempt2 plan, not inferred from old logs.
const normalImportRequired=false; // If true, CONTROL must first formally expand435 Scope and save approval proof.
const storeReadLimit=normalImportRequired?37:38; // Optional recompile consumes one existing read slot.
try {
  await execActual("& "+ps(out+"/prepare-target.ps1"),"prepareTarget");
  await native("status");
  await native("editorStatus");
  await execActual("& "+ps(out+"/check-status.ps1"),"checkStatus");
  if(normalImportRequired) {
    await native("normalImport");
    // One non-resetting120s local readiness wait. It is never loaded-domain proof.
    await execActual("& "+ps(out+"/wait-normal-import.ps1"),"waitNormalImport");
  }
  // Compile after any import, against the current17 actual refs. Never execute these offline DLLs.
  await execActual("& "+ps(out+"/build-payloads.ps1")+" -Names @('preflight','savesBefore','savesAfter','ready','register','read','captureAck','postStop','cancel')","compileSdk");
  await native("preflight","preflight");
  await native("savesBefore","savesBefore");
  await native("consoleBefore0");
  await execActual("& "+ps(out+"/check-console.ps1")+" -When before","checkConsoleBefore");
  // prepare-context.ps1 then seals current raw Scope/review/source/config/assembly/save identities.
  await execActual("& "+ps(out+"/prepare-context.ps1")+" -PresetReviewPath "+ps(out+"/ci-contexts/a2_street435.preset-review.json"),"prepareContext");
  playIssued=true;await native("play");
  let ready=false;const readyStart=Date.now();
  for(let i=0;i<3&&!ready&&Date.now()-readyStart<30000;i++){await native("ready"+i,"ready");ready=(await readSaved("ready"+i)).ready===true;}
  need(ready&&Date.now()-readyStart<=30000,"Single30s natural-ready window exhausted");
  registered=true;await native("register","register");
  let r=await readSaved("register");need(r.status==="RUNNING"&&r.runId==="a2_street435","Actual Begin must register RUNNING");
  const totalDeadline=Date.parse(r.startedAt)+120000; // Original native start, including all app/capture time.
  need(Number.isFinite(totalDeadline),"Actual registration clock missing");
  while(!terminalRead&&Date.now()<totalDeadline){
    const h=await hint();
    if(h.status==="EXPIRED_HINT")throw Error("Actual capture clock expired");
    if(h.status==="CAPTURE_HINT"&&!captureIssued) {
      notice=h.notification;deadline=Math.min(Date.parse(notice.stageDeadlineAt),Date.parse(notice.totalDeadlineAt));
      admit(3);
      r=await managedRead("pendingRead");
      need(r.status==="RUNNING"&&r.pendingCapture==="sorting"&&r.runId==="a2_street435"&&r.executionId===boundExecution&&r.attempt===2&&r.capabilityVersion==="AI436-sorting24-clock-ack-v1","Actual managed sorting hold confirmation");
      need(r.capturePendingNotificationSha256===h.notificationSha256&&r.captureStageDeadlineAt===notice.stageDeadlineAt&&r.captureTotalDeadlineAt===notice.totalDeadlineAt,"Actual managed notification bytes/deadline binding");
      admit(2);captureIssued=true;await native("capture"); // Only official command capture_game_view, screen1024x576.
      admit(1);await native("captureAck","captureAck"); // Uses actual capture WRAPPER receipt SHA.
    }
    if(h.status==="TERMINAL_HINT"){
      terminalRead=await managedRead("complete"); // Actual ReadJson bytes, not local terminal.passed.
      break;
    }
    await new Promise(resolve=>setTimeout(resolve,250)); // No model-turn hop in the urgent capture chain.
  }
  need(terminalRead&&terminalRead.status==="SUCCEEDED"&&terminalRead.passed===true,"Actual terminal complete report required");
  await native("cleanup","read");
  stopIssued=true;await native("stop");
  await native("postStop","postStop");
  await native("savesAfter","savesAfter");
  await native("consoleAfter0");
  await execActual("& "+ps(out+"/check-console.ps1")+" -When after","checkConsoleAfter");
  await execActual("& "+ps(out+"/finalize-context.ps1"),"finalizeContext");
  // Final consumer independently validates all original bytes, accounting, current claim and continuous Console.
  await execActual("& "+ps(root+"/DevTools/Verify-All.ps1")+" -TaskId AI-000435 -Profile ShortCycleNativeCi -Mode Final -NativeCiContextPath "+ps(out+"/ci-contexts/a2_street435.json")+" -StateRoot "+ps(out+"/verify-state"),"nativeConsumer");
  await persist(out+"/orchestration-statistics.json",{nativeCount,readCount,captureIssued,playIssued,stopIssued,appSamples,nativeSuccessProvenByLocalHints:false});
  text("Candidate completed native evidence consumption; ENGINE must still review results and formally settle.");
} catch(error) {
  failing=true;notify("Controlled stop: "+String(error));
  // Failed/unknown native calls retain original objects and are never repeated.
  // If no registered run, skip Cancel. If a stop/unknown exit was already issued, never issue a second.
  if(registered&&!stopIssued&&nativeCount<60){
    try{await native("cancel","cancel");}catch(cancelError){notify("Cancel result uncertain; no resend: "+String(cancelError));}
    for(let i=0;i<2&&nativeCount<59;i++){try{await native("failureCleanup"+i,"read");const clean=await readSaved("failureCleanup"+i);if(clean.cleanup&&Object.values(clean.cleanup).every(x=>x===true))break;}catch(cleanError){notify(String(cleanError));break;}}
  }
  if(playIssued&&!stopIssued&&nativeCount<60){stopIssued=true;try{await native("stop");}catch(stopError){notify("Stop uncertain; no second Stop: "+String(stopError));}}
  await persist(out+"/orchestration-failure.json",{error:String(error),nativeCount,readCount,captureIssued,playIssued,registered,stopIssued,appSamples,passed:false});
  throw error; // ENGINE formally Fail + immediate handoff; no second capture/Play or tool switch.
}
