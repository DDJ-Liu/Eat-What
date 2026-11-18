"""Narrow V01-03 offline compiler and logic test. Never launches Unity or game DLLs."""
import argparse
import os
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import re
import zipfile
from evidence_closure import ClosureEvidence, output_path


def closure_roundtrip(root, out, historical, powershell):
    """Replay one preserved source closure; do not invoke any C# compiler or Unity."""
    from evidence_closure import encode, decode, lfs_pointer, relative_name, output_path, restore, write_json
    historical = Path(historical).resolve()
    snapshots = historical / "snapshots"
    entries, metadata = [], []
    for preflight in sorted(historical.glob("*.preflight.json")):
        data = json.loads(preflight.read_bytes())
        if "sources" not in data:
            continue
        stage = preflight.name[:-len(".preflight.json")]
        for item in data["sources"]:
            original = Path(item["path"])
            repo_path = original.relative_to(root).as_posix() if original.is_absolute() else original.as_posix()
            snapshot = Path(item["snapshot"])
            expected = item["snapshotSha256"]
            if digest(snapshot) != expected or expected != item["sha256"]:
                raise AssertionError("Original source snapshot identity differs")
            entries.append({"path": str(snapshot), "repoPath": repo_path,
                            "restorePath": snapshot.relative_to(snapshots).as_posix(), "expectedSha256": expected})
        runner = snapshots / stage / "Test-LongLoopPlayer.py"
        if digest(runner) != data["runnerSha256"]:
            raise AssertionError("Original runner snapshot identity differs")
        entries.append({"path": str(runner), "repoPath": "DevTools/LongLoop/Test-LongLoopPlayer.py",
                        "restorePath": runner.relative_to(snapshots).as_posix(), "expectedSha256": digest(runner)})
    actual = {p.relative_to(snapshots).as_posix() for p in snapshots.rglob("*") if p.is_file()}
    if actual != {e["restorePath"] for e in entries}:
        raise AssertionError("Historical snapshot file set is not completely covered")
    source_map = out / "selected-source-map.json"
    write_json(source_map, entries)
    # Keep original preflight/RSP/streams/exit records, losslessly, without old DLLs or source copies.
    with zipfile.ZipFile(out / "historical-provenance.zip", "w", zipfile.ZIP_DEFLATED) as z:
        for source in sorted(historical.iterdir()):
            if source.is_file() and (source.suffix in (".json", ".rsp") or source.name.endswith((".stdout.bin", ".stderr.bin"))):
                z.write(source, source.name)
                metadata.append({"name": source.name, "bytes": source.stat().st_size, "sha256": digest(source)})
    write_json(out / "historical-provenance-index.json", metadata)
    child_records = []

    def child(name, script, options):
        command = [powershell, "-NoProfile", "-File", str(root / "DevTools/Workflow" / script),
                   "-RepositoryRoot", str(root), "-PythonPath", sys.executable, *options]
        started = datetime.now(timezone.utc).isoformat()
        process = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = process.communicate()
        (out / (name + ".stdout.bin")).write_bytes(stdout)
        (out / (name + ".stderr.bin")).write_bytes(stderr)
        record = {"command": command, "pid": process.pid, "exitCode": process.returncode,
                  "startedAt": started, "finishedAt": datetime.now(timezone.utc).isoformat(),
                  "stdoutSha256": hashlib.sha256(stdout).hexdigest().upper(),
                  "stderrSha256": hashlib.sha256(stderr).hexdigest().upper()}
        child_records.append(record)
        write_json(out / "children.json", child_records)
        if process.returncode:
            raise RuntimeError(stderr.decode("utf-8", "replace"))

    pack = out / "compact"
    child("pack", "Evidence-Closure.ps1", ["-SourceManifestPath", str(source_map), "-OutputDirectory", str(pack)])
    restored = out / "restored"
    child("restore", "Restore-Closure.ps1", ["-ManifestPath", str(pack / "manifest.json"), "-DestinationDirectory", str(restored)])
    restored_files = {p.relative_to(restored).as_posix(): digest(p) for p in restored.rglob("*") if p.is_file()}
    if restored_files != {e["restorePath"]: e["expectedSha256"] for e in entries}:
        raise AssertionError("Restored source file set/SHA mismatch")
    child("whatif", "Restore-Closure.ps1", ["-ManifestPath", str(out / "missing.json"),
          "-DestinationDirectory", str(out / "whatif-must-not-exist"), "-WhatIf"])
    if (out / "whatif-must-not-exist").exists():
        raise AssertionError("WhatIf wrote output")
    # Small codec boundary checks supplement the real historical replay; no fixture Git repository/commits.
    cases = [(b"same\r\n", b"same\r\n", False), (b"a\nb\n", b"a\r\nb\r\n", False),
             (b"\xef\xbb\xbfa\r\nb\nc\r\nd\n", b"\xef\xbb\xbfA\r\nb\nc\r\nD\n", False),
             (b"a\0b\r\nc", b"a\0B\r\nc", False), (None, b"untracked\0\xff", False),
             (b"version https://git-lfs.github.com/spec/v1\noid sha256:" + b"0" * 64 + b"\nsize 8\n", b"actual\0\xff", True)]
    for base, target, is_lfs in cases:
        record, tape = encode(base, target, is_lfs)
        if decode(record, base, tape) != target:
            raise AssertionError("Byte codec boundary failed")
    pointer = out / "unhydrated-lfs.txt"
    pointer.write_bytes(cases[-1][0])
    rejected = ClosureEvidence(root, out / "pointer-rejection")
    try:
        rejected.capture("pointer", [{"path": str(pointer), "repoPath": "untracked-pointer.txt", "restorePath": "pointer.txt"}])
    except ValueError:
        pointer_rejected = True
    else:
        raise AssertionError("Unhydrated LFS pointer was accepted")
    for bad in ("../Assets/test.cs", "C:/Assets/test.cs", "a\\b", ".git/index", "CON", "a/../b"):
        try:
            relative_name(bad)
        except ValueError:
            pass
        else:
            raise AssertionError("Unsafe relative path was accepted")
    try:
        output_path(root, root / "Assets" / "never-written", fresh=True)
    except ValueError:
        pass
    else:
        raise AssertionError("Active source destination was accepted")
    malformed = json.loads((pack / "manifest.json").read_bytes())
    first = next(iter(malformed["objects"].values()))
    first["bytes"] += 1
    write_json(pack / "corrupt-manifest.json", malformed)
    try:
        restore(root, pack / "corrupt-manifest.json", out / "corrupt-must-not-exist")
    except ValueError:
        pass
    else:
        raise AssertionError("Corrupt identity was accepted")
    if (out / "corrupt-must-not-exist").exists():
        raise AssertionError("Rejected restore wrote files")
    manifest = json.loads((pack / "manifest.json").read_bytes())
    kinds = {}
    for record in manifest["objects"].values():
        kinds[record["kind"]] = kinds.get(record["kind"], 0) + 1
    result = {"historicalRun": str(historical), "files": len(entries), "sourceBytes": sum(e["bytes"] for e in manifest["files"]),
              "allFileSetsAndHashesMatch": True, "codecCases": len(cases), "unsafePathsRejected": 7,
              "unhydratedLfsRejected": pointer_rejected, "corruptRestoreRejectedBeforeWrite": True,
              "actualHistoricalStorageKinds": kinds, "syntheticLfsBoundaryOnly": True,
              "unityCalls": 0, "csharpCompilerCalls": 0, "gitWrites": 0, "children": child_records,
              "historicalOriginalRspBytes": "Not saved by AI422; original SHA identities and saved derived RSP retained. Not recompiled."}
    write_json(out / "roundtrip-result.json", result)
    print(json.dumps(result))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--closure-roundtrip", help="Verify one preserved source closure with the compact pack/restore scripts; no compiler or Unity")
    parser.add_argument("--powershell", help="PowerShell 7 executable for the isolated closure roundtrip")
    parser.add_argument("--avatar-init", action="store_true",
                        help="Compile current LongLoop assembly and run the real avatar source with control-flow substitutes")
    parser.add_argument("--legacy-rsp", help="Replay an existing Unity Bee Legacy.rsp with only the declared LongLoop reference and output relocation")
    parser.add_argument("--preflight-candidate", help="Compile a preflight payload with the installed Unity Pipeline SDK wrapper; never execute it")
    parser.add_argument("--street-config", action="store_true",
                        help="Compile current Toolkit/LongLoop with the new config sources and run actual-source config tests")
    parser.add_argument("--ground-space", action="store_true",
                        help="Compile current Toolkit/LongLoop with ground math and run actual-source ground tests")
    parser.add_argument("--walk-area", action="store_true",
                        help="Compile current Toolkit/LongLoop/Legacy and run actual-source area and controller tests")
    parser.add_argument("--depth-scale", action="store_true",
                        help="Compile current closures and test actual depth, both facing writers and walk regression")
    parser.add_argument("--depth-facing-old-source", action="store_true",
                        help="Execute preserved AI426 source counterexamples; expected child exit1 is recorded explicitly")
    parser.add_argument("--y-sort", action="store_true",
                        help="Compile current closures and execute only actual-source sorting and independent-foot cases")
    parser.add_argument("--follow-pipeline", action="store_true", help="Compile current closures and test pure FR-01 target rules")
    parser.add_argument("--dynamic-zoom", action="store_true", help="Compile current LongLoop and test the pure V012-01 zoom plus unchanged configuration suite")
    parser.add_argument("--look-float", action="store_true", help="Compile current LongLoop and test the pure two-axis V012-02 look float")
    parser.add_argument("--vertical-offset", action="store_true", help="Compile current LongLoop and test pure V012-03 blocked vertical input")
    parser.add_argument("--follow-composition", action="store_true", help="Compile current LongLoop and test V012-04 scaled channel composition")
    parser.add_argument("--parallax", action="store_true", help="Compile current LongLoop and test V012-06 math and component lifecycle")
    parser.add_argument("--follow-pipeline-logic-only", action="store_true", help="Focused pure target compilation")
    parser.add_argument("--follow-r-timestep-counterexample", action="store_true", help="Expected old a5 tuple and independent-clock coefficient rejections before production repair")
    parser.add_argument("--follow-r-snapshot-counterexample", action="store_true", help="Expected old Watch permanent stop under actual queue-mutex contention")
    parser.add_argument("--follow-r-input-counterexample", action="store_true", help="Expected old input monitor focus/size context false positives using actual LegacyInputProvider")
    parser.add_argument("--follow-r-startup-counterexample", action="store_true", help="Execute two expected rejection cases against the captured pre-fix current production startup")
    parser.add_argument("--follow-r-smoke", action="store_true", help="Current FOLLOW-R five-check sequence, exact MANUAL route and extracted owned lifecycle")
    parser.add_argument("--follow-r-smoke-logic-only", action="store_true", help="Focused FOLLOW-R logic/lifecycle; final uses four current closures")
    parser.add_argument("--follow-driver-v012", action="store_true", help="Run independent V012-05 actual-driver integration cases")
    parser.add_argument("--follow-driver", action="store_true", help="Compile current runtime/Editor closures and actual CM3.1.7 Driver boundary")
    parser.add_argument("--follow-driver-logic-only", action="store_true", help="Focused Driver boundary compilation; no production closure replay")
    parser.add_argument("--street-probe", action="store_true", help="Compile current Editor probe closure and test its actual bounded sequence/check source")
    parser.add_argument("--street-observation-compile", action="store_true", help="Compile only the current actual Editor probe Bee closure; never execute Unity")
    parser.add_argument("--street-smoke", action="store_true", help="Compile the actual Editor probe closure and run only the eight named SMK scenarios")
    parser.add_argument("--street-ready-json", action="store_true", help="Compile current Editor sources and run only the three Ready position JSON scenarios")
    parser.add_argument("--street-report-json", action="store_true", help="Compile current Editor and three actual menu-expression/DTO routing scenarios; native JsonUtility is explicitly substituted offline")
    parser.add_argument("--street-state-read", action="store_true", help="Compile current Editor and run three real Windows file/mutex and guard scenarios in isolated owned files")
    parser.add_argument("--street-frame-read", action="store_true", help="Compile current Editor and extracted actual SmokeRun with transparent Unity boundaries; three real-mutex frame scenarios")
    parser.add_argument("--street-payload-directory", help="Compile all nine delivered short Street payloads with the installed SDK wrapper")
    parser.add_argument("--street-payload-roles", help="Explicit comma-separated subset for the reviewed bootstrap/business compilation phases")
    parser.add_argument("--street-editor-reference", help="New current-source Editor assembly for short payload metadata; never executed")
    parser.add_argument("--probe-reference-manifest", help="Explicit source/hash-bound unchanged Toolkit/LongLoop/Legacy references for Editor probe compilation")
    args = parser.parse_args()
    if args.follow_pipeline_logic_only and not args.follow_pipeline:
        parser.error("--follow-pipeline-logic-only requires --follow-pipeline")
    root = Path(__file__).resolve().parents[2]
    out = output_path(root, args.output)
    out.mkdir(parents=True, exist_ok=True)
    if args.closure_roundtrip:
        closure_roundtrip(root, out, args.closure_roundtrip, args.powershell or shutil.which("pwsh"))
        return
    closure_store = None

    def capture_sources(name, sources):
        nonlocal closure_store
        if closure_store is None:
            closure_store = ClosureEvidence(root, out / "closure-evidence")
        inputs = list(sources) + [Path(__file__).resolve(), Path(__file__).resolve().with_name("evidence_closure.py")]
        identities = closure_store.capture(name, [{"path": str(p), "repoPath": p.relative_to(root).as_posix(),
                      "restorePath": name + "/" + p.relative_to(root).as_posix()} for p in inputs])
        return identities[:len(sources)]
    editor = Path("C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor")
    mono = editor / "Data/MonoBleedingEdge/bin/mono.exe"
    csc = editor / "Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
    fx = editor / "Data/MonoBleedingEdge/lib/mono/4.5"
    ue = editor / "Data/Managed/UnityEngine"
    sa = root / "Library/ScriptAssemblies"
    common = [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll",
              fx / "Facades/netstandard.dll", ue / "UnityEngine.CoreModule.dll"]
    records = []
    narrative_dependencies = []
    narrative_core = None
    narrative_toolkit = None
    narrative_unity = None

    def build(name, sources, refs, target="library", main_type=None, language="latest", defines=(), record_list=None, assembly_path=None):
        # Existing old-suite fixtures compile the same expanded check source without stale runtime metadata.
        if any(p.name=="LongLoopProbeChecks.cs" for p in sources) and not any(p.name=="StreetFollowPipeline.cs" for p in sources):
            sources=list(sources)+[root/"Assets/Scripts/LongLoop/StreetFollowPipeline.cs",root/"Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs",root/"Assets/Scripts/LongLoop/Configuration/StreetConfigAsset.cs"]
        if any(p.name == "StreetFollowPipeline.cs" for p in sources):
            bridge = root / "Assets/Scripts/Tools/NarrativeCurveBridge.cs"
            sources = list(sources) + ([] if bridge in sources else [bridge])
            refs = list(refs) + [ensure_narrative_core()]
        exe = Path(assembly_path) if assembly_path is not None else out / (name + (".exe" if target == "exe" else ".dll"))
        exe.parent.mkdir(parents=True, exist_ok=True)
        rsp = out / (name + ".rsp")
        lines = ["/nologo", "/noconfig", "/langversion:" + language, "/target:" + target,
                 '/out:"' + str(exe) + '"']
        if main_type:
            lines.append('/main:' + main_type)
        if defines:
            lines.append('/define:' + ';'.join(defines))
        lines += ['/r:"' + str(p) + '"' for p in refs]
        lines += ['"' + str(p) + '"' for p in sources]
        rsp.write_text("\n".join(lines) + "\n", encoding="utf-8")
        command = [str(mono), str(csc), "@" + str(rsp)]
        source_identity = capture_sources(name, sources)
        preflight = out / (name + ".preflight.json")
        preflight.write_text(json.dumps({"capturedBeforeCompile": True,
                                         "runner": str(Path(__file__)), "runnerSha256": digest(Path(__file__)),
                                         "sourceEvidenceSchema": "code-source-closure/1",
                                         "sourceClosureManifest": str(out / "closure-evidence/manifest.json"),
                                         "mono": str(mono), "monoSha256": digest(mono),
                                         "compiler": str(csc), "compilerSha256": digest(csc),
                                         "rsp": str(rsp), "rspSha256": digest(rsp),
                                         "command": command, "sources": source_identity,
                                         "references": [{"path": str(p), "sha256": digest(p)} for p in refs]},
                                        indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        process = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        captured_out, captured_err = process.communicate()
        result = subprocess.CompletedProcess(command, process.returncode, captured_out, captured_err)
        finished_at = datetime.now(timezone.utc).isoformat()
        stdout, stderr = out / (name + ".stdout.bin"), out / (name + ".stderr.bin")
        stdout.write_bytes(result.stdout)
        stderr.write_bytes(result.stderr)
        record = {
            "stage": name, "command": command, "exitCode": result.returncode, "pid": process.pid,
            "startedAt": started_at, "finishedAt": finished_at,
            "preflight": str(preflight), "preflightSha256": digest(preflight),
            "responseFile": str(rsp), "assembly": str(exe),
            "stdout": str(stdout), "stderr": str(stderr),
            "sources": [{"path": str(p), "sha256": digest(p)} for p in sources],
            "references": [{"path": str(p), "sha256": digest(p)} for p in refs]
        }
        (records if record_list is None else record_list).append(record)
        if result.returncode:
            print(result.stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
            print(result.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            (out / "offline-validation.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
            raise SystemExit("Compile failed: " + name)
        record["assemblySha256"] = digest(exe)
        return exe

    def src(*paths):
        return [root / path for path in paths]

    def write_narrative_dependencies():
        (out / "narrative-dependencies.json").write_text(json.dumps(narrative_dependencies, indent=2), encoding="utf-8")

    def ensure_narrative_core():
        nonlocal narrative_core
        if narrative_core is None:
            narrative_core = build("Narrative.Core", sorted((root / "Assets/Scripts/Narrative/Core").rglob("*.cs")),
                [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], defines=("UNITY_EDITOR",), record_list=narrative_dependencies)
            write_narrative_dependencies()
        return narrative_core

    # V013-04 fresh dependency DAG for LongLoop's new direct Narrative.Unity reference.
    def ensure_narrative_unity():
        nonlocal narrative_unity
        if narrative_unity is None:
            toolkit = build_bee("Toolkit", {}, record_list=narrative_dependencies)
            short_core = build_bee("ShortCycle.Core", {}, record_list=narrative_dependencies)
            rows = src(*("Assets/Scripts/DataBase/CookingData/" + n + ".cs" for n in (
                "CK01CharacterData", "CK01DialogueData", "CK01DialogueLineData", "CK01CGData",
                "CK01SceneData", "CK01SceneStateData", "CK01SceneStatePlacementData", "CK01AmbientPoolData", "CK01OptionalValues")))
            generated = build_bee("Generated.DataTables", {"Toolkit": toolkit, "ShortCycle.Core": short_core},
                rows, record_list=narrative_dependencies)
            narrative_unity = build("Narrative.Unity", sorted((root / "Assets/Scripts/Narrative/Unity").rglob("*.cs")),
                common + [ue / "UnityEngine.JSONSerializeModule.dll", ue / "UnityEngine.UIModule.dll",
                    sa / "Unity.TextMeshPro.dll", sa / "UnityEngine.UI.dll", ensure_narrative_core(), toolkit, generated],
                record_list=narrative_dependencies)
            write_narrative_dependencies()
        return narrative_unity

    if args.street_payload_directory:
        directory = Path(args.street_payload_directory).resolve()
        editor_reference = Path(args.street_editor_reference).resolve()
        roles = ("preflight", "savesBefore", "savesAfter", "ready", "register", "read", "captureAck", "postStop", "cancel", "shortCapabilities", "normalImport", "importReadback")
        if args.street_payload_roles:
            selected = tuple(args.street_payload_roles.split(","))
            if not selected or len(set(selected)) != len(selected) or any(name not in roles for name in selected):
                raise SystemExit("Unknown or duplicate Street SDK compilation role")
            roles = selected
        sdk_path = root / "Library/PackageCache/com.unity.pipeline@62c08c808737/Runtime/Compilation/EvalCodeCompiler.cs"
        sdk_text = sdk_path.read_text(encoding="utf-8-sig")
        start = sdk_text.index('return $@"', sdk_text.index('private static string BuildSourceCode')) + len('return $@"')
        template = sdk_text[start:sdk_text.index('";', start)].replace("{{", "{").replace("}}", "}")
        business_refs = common + [ue / "UnityEditor.CoreModule.dll", editor_reference]
        json_reference = root / "Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        manifest = []
        for name in roles:
            payload = directory / (name + ".eval.cs")
            body = payload.read_text(encoding="utf-8-sig")
            bootstrap = name in ("shortCapabilities", "normalImport", "importReadback")
            if not bootstrap and (len(payload.read_bytes()) >= 1024 or "LongLoopAutomatedProbe.ExecutePayload" not in body or "void Gate(" in body):
                raise SystemExit("Actual short role malformed: " + name)
            refs = (common + [ue / "UnityEditor.CoreModule.dll", json_reference] + ([] if name == "normalImport" else [editor_reference])) if bootstrap else business_refs
            wrapper = out / (name + ".wrapper.cs")
            wrapper.write_text(template.replace("{editorUsing}", "using UnityEditor;").replace("{id}", "AI444_" + name).replace("{userCode}", body), encoding="utf-8")
            before = out / (name + ".sdk-inputs.json")
            before.write_text(json.dumps({"beforeCompiler": True, "payload": str(payload), "payloadSha256": digest(payload), "payloadText": body,
                "sdk": str(sdk_path), "sdkSha256": digest(sdk_path), "sdkSource": sdk_text,
                "editorReference": str(editor_reference), "editorReferenceSha256": digest(editor_reference), "specializedReferences": len(refs),
                "python": sys.executable, "pythonSha256": digest(Path(sys.executable))}, indent=2), encoding="utf-8")
            build("SDK_" + name, [wrapper], refs)
            manifest.append({"role": name, "payload": str(payload), "payloadBytes": payload.stat().st_size, "payloadSha256": digest(payload), "compile": records[-1]})
        (out / "sdk-matrix.json").write_text(json.dumps({"actualInstalledSdkWrapper": True, "currentEditorMetadata": True,
            "unityExecuted": False, "gameDllExecuted": False, "roleCount": len(manifest), "roles": manifest}, indent=2), encoding="utf-8")
        print(json.dumps({"roles": len(manifest), "businessRoles": sum(m["role"] not in ("shortCapabilities", "normalImport", "importReadback") for m in manifest), "bootstrapRoles": sum(m["role"] in ("shortCapabilities", "normalImport", "importReadback") for m in manifest), "allCompilerExitsZero": True, "specializedReferencesByRole": {m["role"]:len(m["compile"]["references"]) for m in manifest}, "dllExecuted": False}))
        return

    if args.preflight_candidate:
        candidate = Path(args.preflight_candidate).resolve()
        previous = root / ".ai-workspace/outputs/PLAYABLE-DEMO01-20261026/AI-000377/attempt1"
        old_payload = (previous / "preflight.eval.cs").read_text(encoding="utf-8-sig")
        old_wrapper = (previous / "offline/preflight/preflight.wrapper.cs").read_text(encoding="utf-8-sig")
        sdk = root / "Library/PackageCache/com.unity.pipeline@62c08c808737/Runtime/Compilation/EvalCodeCompiler.cs"
        source = sdk.read_text(encoding="utf-8-sig")
        method = source.index("private static string BuildSourceCode")
        start = source.index('return $@"', method) + len('return $@"')
        end = source.index('";', start)
        template = source[start:end].replace("{{", "{").replace("}}", "}")
        def wrap(payload, name):
            return (template.replace("{id}", name)
                    .replace("{editorUsing}", "using UnityEditor;")
                    .replace("{userCode}", payload))
        if wrap(old_payload, "a3420000") != old_wrapper:
            raise SystemExit("Installed SDK wrapper does not reproduce the previously verified complete wrapper")
        context = json.loads((previous / "offline/preflight/preflight.context.json").read_text(encoding="utf-8"))
        refs = [Path(path) for path in context["references"]]
        if len(refs) != 7 or not all(path.is_file() for path in refs):
            raise SystemExit("Installed complete seven-reference SDK context is missing")
        wrapper = out / "preflight.candidate.wrapper.cs"
        wrapper.write_text(wrap(candidate.read_text(encoding="utf-8-sig"), "378c0de0"), encoding="utf-8")
        assembly = out / "PipelineEval_378c0de0.dll"
        rsp = out / "preflight.candidate.rsp"
        rsp.write_text("\n".join(["/nologo", "/nostdlib+", "/target:library", "/langversion:latest",
                                  '/out:"' + str(assembly) + '"'] +
                                 ['/reference:"' + str(path) + '"' for path in refs] +
                                 ['"' + str(wrapper) + '"']) + "\n", encoding="utf-8")
        command = [str(mono), str(csc), "@" + str(rsp)]
        result = subprocess.run(command, cwd=root, capture_output=True)
        (out / "preflight.stdout.bin").write_bytes(result.stdout)
        (out / "preflight.stderr.bin").write_bytes(result.stderr)
        report = {"unityNativeLaunched": False, "dllExecuted": False,
                  "installedSdk": str(sdk), "installedSdkSha256": digest(sdk),
                  "originalCompleteWrapperReproduced": True,
                  "candidate": str(candidate), "candidateSha256": digest(candidate),
                  "wrapper": str(wrapper), "wrapperSha256": digest(wrapper),
                  "references": [{"path": str(path), "sha256": digest(path)} for path in refs],
                  "rsp": str(rsp), "rspSha256": digest(rsp),
                  "command": command, "exitCode": result.returncode,
                  "stdout": str(out / "preflight.stdout.bin"),
                  "stderr": str(out / "preflight.stderr.bin"),
                  "assembly": str(assembly)}
        if result.returncode == 0:
            report["assemblySha256"] = digest(assembly)
        (out / "preflight-compile.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"references": len(refs), "sdkWrapperReproduced": True,
                          "compileExitCode": result.returncode, "dllExecuted": False}))
        if result.returncode:
            print(result.stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
            print(result.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(result.returncode)
        return

    if args.legacy_rsp:
        original = Path(args.legacy_rsp).resolve()
        if not original.is_file() or original.name != "Legacy.rsp":
            raise SystemExit("A real Unity Bee Legacy.rsp is required")
        asmref = json.loads((root / "Assets/Scripts/Controller/Legacy.asmref").read_text(encoding="utf-8"))
        if asmref.get("reference") != "Legacy":
            raise SystemExit("Controller source is not assigned to Legacy")
        definitions = {}
        for path in (root / "Assets").rglob("*.asmdef"):
            definition = json.loads(path.read_text(encoding="utf-8-sig"))
            definitions[definition["name"]] = definition
        if "LongLoop" not in definitions["Legacy"]["references"]:
            raise SystemExit("Legacy.asmdef does not declare LongLoop")
        visiting, visited = set(), set()
        def check_acyclic(name):
            if name in visiting:
                raise SystemExit("Assembly reference cycle at " + name)
            if name in visited or name not in definitions:
                return
            visiting.add(name)
            for dependency in definitions[name].get("references", []):
                check_acyclic(dependency)
            visiting.remove(name)
            visited.add(name)
        check_acyclic("Legacy")
        lines = original.read_text(encoding="utf-8-sig").splitlines()
        source_lines = [line for line in lines if line.startswith('"Assets/') and line.endswith('.cs"')]
        if '"Assets/Scripts/Controller/HorizontalPlayerController.cs"' not in source_lines:
            raise SystemExit("Bee Legacy source set lacks the prototype controller")
        if any(line.startswith('-r:') and 'LongLoop' in line for line in lines):
            raise SystemExit("Original Bee RSP already has a LongLoop reference")
        for line in source_lines:
            if not (root / line.strip('"')).is_file():
                raise SystemExit("Missing Bee source: " + line)
        toolkit = sa / "Toolkit.dll"
        longloop = build("LongLoop",
                         src("Assets/Scripts/LongLoop/HorizontalMotion.cs",
                             "Assets/Scripts/LongLoop/IPlayerAvatar.cs",
                             "Assets/Scripts/LongLoop/LongLoopPlayerController.cs",
                             "Assets/Scripts/LongLoop/TomatoSpineAvatar.cs",
                             "Assets/Scripts/LongLoop/StreetSceneEntry.cs"),
                         common + [ue / "UnityEngine.Physics2DModule.dll",
                                   sa / "spine-unity.dll", toolkit])
        rewritten = []
        changed_options = []
        for line in lines:
            if line.startswith("-out:"):
                replacement = '-out:"' + str(out / "Legacy.dll") + '"'
            elif line.startswith("-refout:"):
                replacement = '-refout:"' + str(out / "Legacy.ref.dll") + '"'
            else:
                replacement = line
            if replacement != line:
                changed_options.append({"from": line, "to": replacement})
            rewritten.append(replacement)
        reference = '-r:"' + str(longloop) + '"'
        insertion = max(i for i, line in enumerate(rewritten) if line.startswith("-r:")) + 1
        rewritten.insert(insertion, reference)
        if len(changed_options) != 2:
            raise SystemExit("Bee RSP did not have exactly -out and -refout to relocate")
        derived = out / "Legacy-derived.rsp"
        derived.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        command = [str(mono), str(csc), "@" + str(derived)]
        result = subprocess.run(command, cwd=root, capture_output=True)
        (out / "Legacy.stdout.bin").write_bytes(result.stdout)
        (out / "Legacy.stderr.bin").write_bytes(result.stderr)
        source_identity = [{"path": line.strip('"'), "sha256": digest(root / line.strip('"'))}
                           for line in source_lines]
        reference_lines = [line for line in lines if line.startswith("-r:")]
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "asmref": "Assets/Scripts/Controller/Legacy.asmref",
                  "declaredLegacyReferences": definitions["Legacy"]["references"],
                  "acyclicLocalAssemblyGraph": True,
                  "originalRsp": str(original), "originalRspSha256": digest(original), "declaredRetiredSourcesRemoved": removed,
                  "derivedRsp": str(derived), "derivedRspSha256": digest(derived),
                  "changedOriginalOptions": changed_options,
                  "addedReference": reference,
                  "sourceCount": len(source_identity), "sources": source_identity,
                  "originalReferenceCount": len(reference_lines),
                  "originalReferences": reference_lines,
                  "longLoopCompile": records[0],
                  "legacyCompile": {"command": command, "exitCode": result.returncode,
                                    "stdout": str(out / "Legacy.stdout.bin"),
                                    "stderr": str(out / "Legacy.stderr.bin"),
                                    "assembly": str(out / "Legacy.dll"),
                                    "referenceAssembly": str(out / "Legacy.ref.dll")}}
        if result.returncode == 0:
            report["legacyCompile"]["assemblySha256"] = digest(out / "Legacy.dll")
            report["legacyCompile"]["referenceAssemblySha256"] = digest(out / "Legacy.ref.dll")
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"sources": len(source_identity), "originalReferences": len(reference_lines),
                          "longLoopExitCode": records[0]["exitCode"], "legacyExitCode": result.returncode}))
        if result.returncode:
            print(result.stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
            print(result.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(result.returncode)
        return

    bee = root / "Library/Bee/artifacts/1900b0aP.dag"

    def build_bee(name, replacements, extra_sources=(), extra_references=(), original_rsp=None, record_list=None):
        nonlocal narrative_toolkit
        if name == "Toolkit" and narrative_toolkit is not None:
            return narrative_toolkit
        if name in ("LongLoop", "Legacy") and "Toolkit" not in replacements:
            replacements = dict(replacements)
            replacements["Toolkit"] = build_bee("Toolkit", {}, record_list=narrative_dependencies)
            write_narrative_dependencies()
        original = original_rsp if original_rsp is not None else bee / (name + ".rsp")
        original_snapshot = out / (name + ".original.rsp")
        shutil.copyfile(original, original_snapshot)
        lines = original_snapshot.read_text(encoding="utf-8-sig").splitlines()
        retired_follow={"Assets/Scripts/LongLoop/"+n+".cs" for n in ("FollowRecenterExtension","FollowRecenterMath","StreetFollowChannels","ChannelPipeline")}
        removed=[line for line in lines if line.strip('"') in retired_follow]
        lines=[line for line in lines if line not in removed]

        if name == "Toolkit":
            current_core = ensure_narrative_core()
            bridge = root / "Assets/Scripts/Tools/NarrativeCurveBridge.cs"
            if '"' + bridge.relative_to(root).as_posix() + '"' not in lines and bridge not in extra_sources:
                extra_sources = tuple(extra_sources) + (bridge,)
            if any(line.startswith('-r:"') and Path(line[4:-1]).stem in ("Narrative.Core", "Narrative.Core.ref") for line in lines):
                replacements = dict(replacements, **{"Narrative.Core": current_core})
            else:
                extra_references = tuple(extra_references) + (current_core,)
        # Older business modes pass candidate additions that current Bee may already contain.
        # V013-02: compile the actual scene target components in every current LongLoop closure.
        if name == "LongLoop":
            current_core = ensure_narrative_core()
            marker_sources = src("Assets/Scripts/LongLoop/NarrativeActorMarker.cs",
                                 "Assets/Scripts/LongLoop/NarrativeAnchorMarker.cs",
                                 "Assets/Scripts/LongLoop/NarrativeTargetRegistry.cs")
            # V013-03 production addition; all existing marker sources remain unchanged.
            marker_sources += src("Assets/Scripts/LongLoop/NpcShell.cs")
            # V013-07 current trigger and real coordinator, never an old cached LongLoop DLL.
            marker_sources += src("Assets/Scripts/LongLoop/NarrativeAmbientBubbleTrigger.cs",
                                  "Assets/Scripts/LongLoop/NarrativeAmbientBubbleDriver.cs")
            marker_sources += src("Assets/Scripts/LongLoop/NarrativeSceneStateApplication.cs",
                                  "Assets/Scripts/LongLoop/NarrativeSceneStateApplier.cs")
            marker_sources += src("Assets/Scripts/LongLoop/NarrativeSceneDebugPanel.cs")
            extra_references = tuple(extra_references) + (sa / "Unity.InputSystem.dll",)
            extra_sources = tuple(extra_sources) + tuple(p for p in marker_sources if p not in extra_sources)
            if any(line.startswith('-r:"') and Path(line[4:-1]).stem in ("Narrative.Core", "Narrative.Core.ref") for line in lines):
                replacements = dict(replacements, **{"Narrative.Core": current_core})
            else:
                extra_references = tuple(extra_references) + (current_core,)
        extra_sources = tuple(p for p in extra_sources if '"' + p.relative_to(root).as_posix() + '"' not in lines)
        # V013-04 bind the actual current Unity layer, never an empty marker or stale Bee dependency.
        if name == "LongLoop":
            current_unity = ensure_narrative_unity()
            if any(line.startswith('-r:"') and Path(line[4:-1]).stem in ("Narrative.Unity", "Narrative.Unity.ref") for line in lines):
                replacements = dict(replacements, **{"Narrative.Unity": current_unity})
            else:
                extra_references = tuple(extra_references) + (current_unity,)
        source_lines = [line for line in lines if line.startswith('"Assets/') and line.endswith('.cs"')]
        sources = [root / line.strip('"') for line in source_lines] + list(extra_sources)
        if not sources or not all(path.is_file() for path in sources):
            raise SystemExit("Current Bee source closure missing: " + name)
        if name == "Toolkit" and not all(path in sources for path in src(
                "Assets/Scripts/Tools/Input/IGameInput.cs", "Assets/Scripts/Tools/Input/LegacyInputProvider.cs")):
            raise SystemExit("Toolkit closure omits changed input source")
        if name == "Legacy" and root / "Assets/Scripts/Controller/HorizontalPlayerController.cs" not in sources:
            raise SystemExit("Legacy closure omits prototype consumer")
        rewritten = []
        for line in lines:
            if line.startswith("-out:"):
                line = '-out:"' + str(out / (name + ".dll")) + '"'
            elif line.startswith("-refout:"):
                line = '-refout:"' + str(out / (name + ".ref.dll")) + '"'
            else:
                for old, new in replacements.items():
                    if line.startswith('-r:"') and (root / Path(line[4:-1])).resolve() in {
                            (bee / (old + ".ref.dll")).resolve(), (original.parent / (old + ".ref.dll")).resolve()}:
                        line = '-r:"' + str(new).replace('\\', '/') + '"'
            rewritten.append(line)
        for old in replacements:
            expected = '-r:"' + str(replacements[old]).replace('\\', '/') + '"'
            if rewritten.count(expected) != 1:
                raise SystemExit("Derived Bee closure must use exactly one current " + old + " reference")
        for source in extra_sources:
            relative = source.relative_to(root).as_posix()
            if '"' + relative + '"' in rewritten:
                raise SystemExit("Added source already in Bee response: " + relative)
            rewritten.append('"' + relative + '"')
        existing_refs = {(root / Path(line[4:-1])).resolve() if not Path(line[4:-1]).is_absolute()
                         else Path(line[4:-1]).resolve() for line in rewritten if line.startswith('-r:"')}
        for reference in extra_references:
            if reference.resolve() not in existing_refs:
                rewritten.append('-r:"' + reference.as_posix() + '"')
                existing_refs.add(reference.resolve())
        derived = out / (name + ".derived.rsp")
        derived.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        references = [Path(line[4:-1]) for line in rewritten if line.startswith('-r:"')]
        if not all((root / path).is_file() if not path.is_absolute() else path.is_file() for path in references):
            raise SystemExit("Derived Bee reference missing: " + name)
        source_identity = capture_sources(name, sources)
        command = [str(mono), str(csc), "@" + str(derived)]
        preflight = out / (name + ".preflight.json")
        other_options = [line for line in rewritten if line.startswith(('-analyzer:', '-additionalfile:', '-analyzerconfig:'))]
        other_inputs = []
        for option in other_options:
            if ':"' not in option or not option.endswith('"'):
                raise SystemExit("Cannot identify analyzer/additional file option: " + option)
            path = Path(option.split(':"', 1)[1][:-1])
            actual = path if path.is_absolute() else root / path
            if not actual.is_file():
                raise SystemExit("Missing analyzer/additional file: " + option)
            other_inputs.append({"option": option, "path": str(actual), "length": actual.stat().st_size,
                                 "sha256": digest(actual)})
        preflight.write_text(json.dumps({"capturedBeforeCompile": True,
                                         "originalRsp": str(original), "originalRspSha256": digest(original), "declaredRetiredSourcesRemoved": removed,
                                         "originalRspSnapshot": str(original_snapshot),
                                         "originalRspSnapshotSha256": digest(original_snapshot),
                                         "derivedRsp": str(derived), "derivedRspSha256": digest(derived),
                                         "runnerSha256": digest(Path(__file__)), "python": sys.executable, "pythonSha256": digest(Path(sys.executable)), "monoSha256": digest(mono),
                                         "sourceEvidenceSchema": "code-source-closure/1",
                                         "sourceClosureManifest": str(out / "closure-evidence/manifest.json"),
                                         "compilerSha256": digest(csc), "command": command,
                                         "sources": source_identity,
                                         "analyzerAndAdditionalFileInputs": other_inputs,
                                         "references": [{"path": str(path),
                                                         "length": (root / path if not path.is_absolute() else path).stat().st_size,
                                                         "sha256": digest(root / path if not path.is_absolute() else path)}
                                                        for path in references]}, indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        process = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        captured_out, captured_err = process.communicate()
        result = subprocess.CompletedProcess(command, process.returncode, captured_out, captured_err)
        finished_at = datetime.now(timezone.utc).isoformat()
        stdout, stderr = out / (name + ".stdout.bin"), out / (name + ".stderr.bin")
        stdout.write_bytes(result.stdout)
        stderr.write_bytes(result.stderr)
        record = {"stage": name, "command": command, "preflight": str(preflight),
                  "startedAt": started_at, "finishedAt": finished_at, "pid": process.pid,
                  "preflightSha256": digest(preflight), "exitCode": result.returncode,
                  "stdout": str(stdout), "stderr": str(stderr), "sourceCount": len(sources),
                  "referenceCount": len(references), "assembly": str(out / (name + ".dll")),
                  "referenceAssembly": str(out / (name + ".ref.dll"))}
        (records if record_list is None else record_list).append(record)
        if result.returncode:
            (out / "offline-validation.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
            print(result.stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
            print(result.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit("Compile failed: " + name)
        record["assemblySha256"] = digest(out / (name + ".dll"))
        record["referenceAssemblySha256"] = digest(out / (name + ".ref.dll"))
        if name == "Toolkit":
            narrative_toolkit = out / (name + ".ref.dll")
        return out / (name + ".ref.dll")

    if args.avatar_init:
        current_toolkit = build_bee("Toolkit", {})
        current_lines = set((bee / "LongLoop.rsp").read_text(encoding="utf-8-sig").splitlines())
        current_missing = [p for p in sorted((root / "Assets/Scripts/LongLoop").rglob("*.cs"))
                           if '"' + p.relative_to(root).as_posix() + '"' not in current_lines]
        build_bee("LongLoop", {"Toolkit": current_toolkit}, current_missing)
        production = list(records)
        flow = build("AvatarControlFlowTests",
                     src("Assets/Scripts/LongLoop/IPlayerAvatar.cs",
                         "Assets/Scripts/LongLoop/TomatoSpineAvatar.cs",
                         "DevTools/LongLoop/TomatoAvatarInitializationTests.cs"),
                     [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        run = subprocess.run([str(mono), str(flow)], cwd=root, capture_output=True)
        (out / "avatar-flow.stdout.bin").write_bytes(run.stdout)
        (out / "avatar-flow.stderr.bin").write_bytes(run.stderr)
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "productionCompiles": production, "pureDependencyCompiles": list(narrative_dependencies),
                  "realCurrentToolkitAndSpineCompile": records[1],
                  "controlFlowCompile": records[2],
                  "controlFlowRunExitCode": run.returncode,
                  "controlFlowStdout": str(out / "avatar-flow.stdout.bin"),
                  "controlFlowStderr": str(out / "avatar-flow.stderr.bin"),
                  "controlFlowText": run.stdout.decode("utf-8-sig", "replace").strip(),
                  "controlFlowOnly": True}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"realCompile": records[1]["exitCode"], "toolkitCompile": records[0]["exitCode"],
                          "flowCompile": records[2]["exitCode"],
                          "flowRun": run.returncode, "text": report["controlFlowText"]}))
        if run.returncode: raise SystemExit(run.returncode)
        return

    if args.street_state_read or args.street_frame_read:
        import time
        original = root / "Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added = src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs", "Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed = set(original.read_text(encoding="utf-8-sig").splitlines())
        added = [p for p in added if '"'+p.relative_to(root).as_posix()+'"' not in listed]
        build_bee("Editor.ProjectValidation", {}, added, [root/"Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"], original_rsp=original)
        newtonsoft = root/"Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        for library in (newtonsoft,ue/"UnityEngine.CoreModule.dll",ue/"UnityEngine.SharedInternalsModule.dll"):
            shutil.copyfile(library,out/library.name)
        sources=src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs","Assets/Scripts/LongLoop/DepthScaleMath.cs","Assets/Scripts/LongLoop/YSortMath.cs","DevTools/LongLoop/StreetProbeChecksTests.cs")
        if args.street_frame_read:
            probe=root/"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs";text=probe.read_text(encoding="utf-8-sig")
            validation=text[text.index("        private static bool ValidateSmokeRequest("):text.index("        private static object SmokeReady(")]
            smoke=text[text.index("        private sealed class SmokeRun"):text.index("        // Read only these necessary collider/Confiner")]
            generated=out/"frame.cs"
            generated.write_text("using System;using System.IO;using System.Linq;using System.Text;using System.Diagnostics;using UnityEngine;\nnamespace EatWhat.EditorValidation {public static partial class LongLoopAutomatedProbe {\n"+validation+smoke.replace("private sealed class SmokeRun","public sealed class SmokeRun",1)+"}}\n",encoding="utf-8")
            (out/"frame-source.json").write_text(json.dumps({"actualSource":str(probe),"actualSourceSha256":digest(probe),"actualValidation":validation,"actualSmokeRun":smoke,"onlyExtractionChange":"SmokeRun private -> public for fixture access; method bodies unchanged","generatedSha256":digest(generated),"boundary":"Unity state/controller/JSON/evidence boundaries in existing test branch are transparent substitutes. Real production reader, guards, SmokeRun methods and Sequence execute; no Unity process/product movement claim."},indent=2),encoding="utf-8")
            sources.append(generated)
        exe=build("FrameReadTests" if args.street_frame_read else "StateReadTests",sources,common+[newtonsoft],"exe",main_type="StreetProbeChecksTests",defines=("STREET_FRAME_READ_TESTS" if args.street_frame_read else "STREET_STATE_READ_TESTS",))
        pwsh=Path(shutil.which("pwsh") or Path.home()/".cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe")
        publisher=out/"publish.ps1"
        publisher.write_text(r'''param([string]$Path,[string]$Ready,[string]$Release,[string]$Operation='publish')
$ErrorActionPreference='Stop'
$mutex=[Threading.Mutex]::new($false,'Local\EatWhat_AI_Workspace_Queue_v1');$taken=$false
try {
    try {$taken=$mutex.WaitOne(2000)}catch [Threading.AbandonedMutexException]{$taken=$true}
    if(-not $taken){throw 'Fixture publisher mutex timeout'}
    [IO.File]::WriteAllText($Ready,'publisher-holds-existing-queue-mutex')
    $clock=[Diagnostics.Stopwatch]::StartNew()
    while($Release -and -not [IO.File]::Exists($Release)){if($clock.ElapsedMilliseconds -gt 5000){throw 'Fixture release timeout'};Start-Sleep -Milliseconds 10}
    if($Operation -eq 'abandon'){@{pid=$PID;operation='abandon';published=$false}|ConvertTo-Json -Compress;[Environment]::Exit(0)}
    if($Operation -eq 'release'){@{pid=$PID;operation='release';published=$false}|ConvertTo-Json -Compress;return}
    $temp="$Path.fixture-$PID";[IO.File]::WriteAllText($temp,'{"generation":2}')
    [IO.File]::Move($temp,$Path,$true)
    @{pid=$PID;operation='publish';published=$true;value=[IO.File]::ReadAllText($Path)}|ConvertTo-Json -Compress
} finally {if($taken){$mutex.ReleaseMutex()};$mutex.Dispose()}
''',encoding="utf-8")
        fixture=out/"f";state=fixture/".ai-workspace/runtime/queue-state.json"
        state.parent.mkdir(parents=True,exist_ok=False);state.write_text('{"generation":1}',encoding="utf-8")
        children=[];releases=[];scenarios=[]
        def start(name,command):
            preflight=out/(name+".preflight.json")
            preflight.write_text(json.dumps({"beforeRun":True,"command":command,"executableSha256":digest(Path(command[0])),"testAssemblySha256":digest(exe),"publisherSha256":digest(publisher),"fixtureRoot":str(fixture),"productionRuntimeTouched":False},indent=2),encoding="utf-8")
            child=subprocess.Popen(command,cwd=out,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
            record={"name":name,"pid":child.pid,"startedAt":datetime.now(timezone.utc).isoformat(),"preflight":str(preflight)}
            handle=(child,record);children.append(handle);return handle
        def finish(handle):
            child,record=handle
            if "exitCode" in record:return record
            try:stdout,stderr=child.communicate(timeout=12)
            except subprocess.TimeoutExpired:
                child.kill();stdout,stderr=child.communicate();record["fixtureProcessTimeout"]=True
            stdout_path,stderr_path=out/(record["name"]+".stdout.bin"),out/(record["name"]+".stderr.bin")
            stdout_path.write_bytes(stdout);stderr_path.write_bytes(stderr)
            record.update({"exitCode":child.returncode,"finishedAt":datetime.now(timezone.utc).isoformat(),"stdout":str(stdout_path),"stderr":str(stderr_path),"stdoutSha256":digest(stdout_path),"stderrSha256":digest(stderr_path)})
            return record
        def wait_marker(path,handle):
            deadline=time.monotonic()+5
            while not path.exists():
                if handle[0].poll() is not None or time.monotonic()>deadline:raise RuntimeError("Fixture marker missing: "+str(path))
                time.sleep(.01)
        def publish(name,operation="publish",hold=True):
            ready,release=out/(name+".ready"),out/(name+".release")
            if hold:releases.append(release)
            command=[str(pwsh),"-NoProfile","-File",str(publisher),"-Path",str(state),"-Ready",str(ready),"-Operation",operation]
            if hold:command += ["-Release",str(release)]
            handle=start(name,command);wait_marker(ready,handle);return handle,release
        def reader(name,role,path,release=None):
            marker=out/(name+".ready");command=[str(mono),str(exe),role,str(path),str(marker)]
            if release is not None:command.append(str(release))
            return start(name,command),marker
        def passed(*handles):
            if any(finish(h)["exitCode"]!=0 for h in handles):raise RuntimeError("Isolated state regression failed; raw streams retained")
        success=False
        try:
            if args.street_frame_read:
                for case,name in (("A","TransientBusyThenFresh"),("B","BusyUntilOriginalDeadline"),("C","FreshInvalidStateStillFails")):
                    test,prepared=reader("frame-"+case,case,fixture);wait_marker(prepared,test)
                    writer,release=publish(case+"1",operation="release");(out/(case+"1.go")).touch()
                    if case!="B":wait_marker(release,test)
                    else:passed(test);release.touch()
                    passed(writer);(out/(case+"1.ack")).touch()
                    if case=="A":
                        wait_marker(out/"A2.want",test);writer,release=publish("A2",operation="release");(out/"A2.go").touch();wait_marker(release,test);passed(writer);(out/"A2.ack").touch()
                    passed(test);scenarios.append({"name":name,"passed":True})
            else:
                writer,release=publish("serial-writer");read,marker=reader("serial-reader","read",state);wait_marker(marker,read)
                time.sleep(.1);release.touch();passed(writer,read)
                scenarios.append({"name":"real-windows-serialized-publish-read","passed":True})
                busy,release=publish("busy-writer");timeout,_=reader("busy-reader","timeout",state);passed(timeout)
                guards,_=reader("json-live-guards","guards",fixture/"context.json");passed(guards)
                scenarios.append({"name":"json-live-guards-and-uncoordinated-context","passed":True})
                release.touch();passed(busy)
                error_release=out/"errors.release";releases.append(error_release)
                errors,marker=reader("missing-and-parse","errors",state.parent/"worker-state.json",error_release);wait_marker(marker,errors)
                probe,_=publish("after-errors-writer",hold=False);passed(probe);error_release.touch();passed(errors)
                abandoned,release=publish("abandoned-writer",operation="abandon");read,marker=reader("abandoned-reader","abandoned",state);wait_marker(marker,read);release.touch();passed(abandoned,read)
                scenarios.append({"name":"bounded-busy-error-release-and-abandoned-mutex","passed":True})
            success=True
        finally:
            for release in releases:release.touch(exist_ok=True)
            for handle in children:finish(handle)
            report={"passed":success,"namedScenarios":3,"scenarios":scenarios,"editorCompile":records[0],"logicCompile":records[-1],"children":[r for _,r in children],"unityNativeLaunched":False,"realWindowsFilesAndNamedMutex":True,"productionRuntimeTouched":False,"historicalHandleOwner":"UNKNOWN","nativeLatencyGuarantee":False,"actualFrameSource":str(out/"frame-source.json") if args.street_frame_read else None}
            (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"editorExit":records[0]["exitCode"],"passed":success,"namedScenarios":len(scenarios),"realWindowsFilesAndNamedMutex":True,"productionRuntimeTouched":False}))
        return

    if args.street_report_json:
        original = root / "Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added = src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs", "Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed = set(original.read_text(encoding="utf-8-sig").splitlines())
        added = [p for p in added if '"'+p.relative_to(root).as_posix()+'"' not in listed]
        build_bee("Editor.ProjectValidation", {}, added, [root/"Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"], original_rsp=original)
        probe = root/"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs"
        text = probe.read_text(encoding="utf-8-sig")
        menu = text[text.index("private static void SmokeMenu("):text.index("private static void ValidateSmokeRequest(")]
        ready = text[text.index("private static object SmokeReady("):text.index("public static StreetProbeReport BeginSmoke(")]
        patterns = {"serialization":(r"var text=(.*?);",menu),"inspect":(r'if\(action=="inspect"\)result=(.*?);',menu),
                    "ready":(r'return (new \{ready=player\.isActiveAndEnabled.*?\});',ready),"notReady":(r'return (new \{ready=false,reason="WAIT_FOR_NORMAL_PLAY"\});',ready)}
        expressions = {}
        for name,(pattern,body) in patterns.items():
            matches = re.findall(pattern,body,re.S)
            if len(matches)!=1: raise SystemExit("Actual menu expression must be unique: "+name)
            expressions[name]=matches[0]
        substitutions = {"inspect":{"smoke==null||smoke.Terminal&&smoke.Report().cleanup.Passed":"cleanupIdle","Capabilities()":"capabilities","EditorApplication.isPlaying":"playing"},
                         "ready":{"player.isActiveAndEnabled":"active","area!=null&&area.IsUsable":"usable","player.transform.position":"playerPosition","spawn.position":"spawnPosition","EditorApplication.isPlaying":"playing"}}
        bound = dict(expressions)
        for name,replacements in substitutions.items():
            for actual,parameter in replacements.items():
                if bound[name].count(actual)!=1: raise SystemExit("Actual offline input binding changed: "+actual)
                bound[name]=bound[name].replace(actual,parameter)
        generated = out/"m.cs"
        generated.write_text("using EatWhat.EditorValidation;using UnityEngine;\ninternal static class StreetMenuSerializationExpression {\n"
                             +"public static string Write(object result)=>"+expressions["serialization"]+";\n"
                             +"public static object Inspect(bool cleanupIdle,object capabilities,bool playing)=>"+bound["inspect"]+";\n"
                             +"public static object Ready(bool active,bool initialized,bool neutral,bool usable,bool playing,Vector3 playerPosition,Vector3 spawnPosition)=>"+bound["ready"]+";\n"
                             +"public static object NotReady()=>"+expressions["notReady"]+";}\n",encoding="utf-8")
        expression_path=out/"menu-expressions.json"
        expression_path.write_text(json.dumps({"source":str(probe),"sourceSha256":digest(probe),"actualExpressions":expressions,"offlineInputBindings":substitutions,"generatedSha256":digest(generated),"boundary":"Actual serialization expression and actual DTO/Vector3. JsonUtility is a transparent public-field stand-in; Inspect/Ready inputs are supplied values, not Unity observations or native menu execution."},indent=2),encoding="utf-8")
        newtonsoft = root/"Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        for library in (newtonsoft,ue/"UnityEngine.CoreModule.dll",ue/"UnityEngine.SharedInternalsModule.dll"):
            shutil.copyfile(library,out/library.name)
        exe = build("ReportJsonTests",src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs","Assets/Scripts/LongLoop/DepthScaleMath.cs","Assets/Scripts/LongLoop/YSortMath.cs","DevTools/LongLoop/StreetProbeChecksTests.cs")+[generated],common+[newtonsoft],"exe",main_type="StreetProbeChecksTests",defines=("STREET_REPORT_JSON_TESTS",))
        command=[str(mono),str(exe)]
        preflight=out/"report-cases.preflight.json"
        preflight.write_text(json.dumps({"beforeRun":True,"command":command,"monoSha256":digest(mono),"exeSha256":digest(exe),"newtonsoftSha256":digest(out/"Newtonsoft.Json.dll"),"actualUnityCoreSha256":digest(out/"UnityEngine.CoreModule.dll"),"menuExpressionsSha256":digest(expression_path),"jsonUtility":"TRANSPARENT_PUBLIC_FIELD_STAND_IN"},indent=2),encoding="utf-8")
        started=datetime.now(timezone.utc).isoformat();child=subprocess.Popen(command,cwd=out,stdout=subprocess.PIPE,stderr=subprocess.PIPE);stdout,stderr=child.communicate()
        stdout_path,stderr_path=out/"report-cases.stdout.bin",out/"report-cases.stderr.bin"
        stdout_path.write_bytes(stdout);stderr_path.write_bytes(stderr)
        case_lines=[json.loads(line) for line in stdout.decode("utf-8-sig").splitlines() if line.strip().startswith("{")]
        scenarios=[line for line in case_lines if "scenario" in line]
        success=child.returncode==0 and len(scenarios)==3 and all(line.get("passed") is True for line in scenarios)
        report={"unityNativeLaunched":False,"nativeJsonUtilityExecuted":False,"productionEditorDllExecuted":False,"jsonUtility":"TRANSPARENT_PUBLIC_FIELD_STAND_IN","namedScenarios":3,"editorCompile":records[0],"logicCompile":records[-1],"actualMenuExpressions":str(expression_path),"currentScenarios":{"pid":child.pid,"exitCode":child.returncode,"startedAt":started,"finishedAt":datetime.now(timezone.utc).isoformat(),"preflight":str(preflight),"stdout":str(stdout_path),"stderr":str(stderr_path),"stdoutSha256":digest(stdout_path),"stderrSha256":digest(stderr_path),"scenarios":[{"name":line["scenario"],"passed":line["passed"]} for line in scenarios]},"passed":success}
        (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"editorExit":records[0]["exitCode"],"casesExit":child.returncode,"passed":success,"namedScenarios":3,"nativeJsonUtilityExecuted":False}))
        if not success: raise SystemExit("Menu JSON routing regression failed; original streams retained")
        return

    if args.street_ready_json:
        original = root / "Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added = src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs", "Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed = set(original.read_text(encoding="utf-8-sig").splitlines())
        added = [p for p in added if '"'+p.relative_to(root).as_posix()+'"' not in listed]
        build_bee("Editor.ProjectValidation", {}, added, [root/"Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"], original_rsp=original)
        probe = root/"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs"
        text = probe.read_text(encoding="utf-8-sig")
        ready = text[text.index("private static object SmokeReady("):text.index("public static StreetProbeReport BeginSmoke(")]
        expression = re.search(r'return (new \{ready=false,reason="WAIT_FOR_NORMAL_PLAY"\});', ready)
        if not expression: raise SystemExit("Actual nonReady expression missing; do not substitute a test algorithm")
        generated = out/"n.cs"
        generated.write_text("internal static class StreetReadyNotReadyExpression {public static object Read()=>"+expression.group(1)+";}\n",encoding="utf-8")
        (out/"nonready-expression.json").write_text(json.dumps({"source":str(probe),"sourceSha256":digest(probe),"actualExpression":expression.group(1),"generatedSha256":digest(generated),"boundary":"Execute the actual compiled nonReady return expression only; Unity Editor flags are preserved by source diff, not simulated as a native result."},indent=2),encoding="utf-8")
        newtonsoft = root/"Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        for library in (newtonsoft,ue/"UnityEngine.CoreModule.dll",ue/"UnityEngine.SharedInternalsModule.dll"):
            shutil.copyfile(library,out/library.name)
        exe = build("ReadyJsonTests",src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs","Assets/Scripts/LongLoop/DepthScaleMath.cs","Assets/Scripts/LongLoop/YSortMath.cs","DevTools/LongLoop/StreetProbeChecksTests.cs")+[generated],common+[newtonsoft],"exe",main_type="StreetProbeChecksTests",defines=("STREET_READY_JSON_TESTS",))
        def ready_case(name,arguments):
            command=[str(mono),str(exe)]+arguments
            (out/(name+".preflight.json")).write_text(json.dumps({"beforeRun":True,"command":command,"monoSha256":digest(mono),"exeSha256":digest(exe),"newtonsoftSha256":digest(out/"Newtonsoft.Json.dll"),"actualUnityCoreSha256":digest(out/"UnityEngine.CoreModule.dll")},indent=2),encoding="utf-8")
            started=datetime.now(timezone.utc).isoformat();child=subprocess.Popen(command,cwd=out,stdout=subprocess.PIPE,stderr=subprocess.PIPE);stdout,stderr=child.communicate()
            (out/(name+".stdout.bin")).write_bytes(stdout);(out/(name+".stderr.bin")).write_bytes(stderr)
            return {"pid":child.pid,"exitCode":child.returncode,"startedAt":started,"finishedAt":datetime.now(timezone.utc).isoformat(),"stdout":str(out/(name+".stdout.bin")),"stderr":str(out/(name+".stderr.bin")),"stdoutText":stdout.decode("utf-8-sig","replace"),"stderrText":stderr.decode("utf-8-sig","replace")}
        old=ready_case("old-vector",["--old-ready-vector"])
        expected=old["exitCode"]!=0 and "JsonSerializationException" in old["stderrText"] and "Self referencing loop" in old["stderrText"]
        current=ready_case("scalar-and-notready",[])
        report={"unityNativeLaunched":False,"productionEditorDllExecuted":False,"namedScenarios":3,"editorCompile":records[0],"logicCompile":records[-1],"oldCounterexample":dict(old,expectedFailure=expected,exceptionCategory="JsonSerializationException" if expected else "UNEXPECTED_BOUNDARY"),"currentScenarios":current,"actualProductionProjection":"LongLoopProbeChecks.ReadyPosition","nonReadyExpressionEvidence":str(out/"nonready-expression.json")}
        (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"editorExit":records[0]["exitCode"],"oldCounterexampleExit":old["exitCode"],"expectedOldLoop":expected,"newScenariosExit":current["exitCode"],"namedScenarios":3}))
        if not expected or current["exitCode"]: raise SystemExit("Ready JSON boundary regression failed; original streams retained")
        return

    if args.street_smoke:
        original = root / "Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added = src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs", "Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed = set(original.read_text(encoding="utf-8-sig").splitlines())
        added = [p for p in added if '"' + p.relative_to(root).as_posix() + '"' not in listed]
        build_bee("Editor.ProjectValidation", {}, added, [root / "Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"], original_rsp=original)
        newtonsoft = root / "Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        shutil.copyfile(newtonsoft, out / "Newtonsoft.Json.dll")
        boundary = "DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/"
        exe = build("StreetSmokeChecksTests", src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs", "DevTools/LongLoop/FollowUnityShim.cs", "DevTools/LongLoop/StreetProbeChecksTests.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs", "Assets/Scripts/LongLoop/YSortMath.cs", boundary+"StreetProbeFixtureData.cs", "DevTools/LongLoop/StreetConfigurationTests.cs",
            boundary+"LegacyStreetProbeDriverGuard.cs", boundary+"StreetProbeBudgetTests.cs", boundary+"PreAI441ProbeTimingChecks.cs", boundary+"LegacyProbeTimingChecks.cs"),
            [fx/"mscorlib.dll", fx/"System.dll", fx/"System.Core.dll", fx/"System.Web.Extensions.dll", fx/"Facades/netstandard.dll", newtonsoft], "exe", main_type="StreetProbeChecksTests")
        command = [str(mono), str(exe), "--smoke"]
        (out/"smoke-run.preflight.json").write_text(json.dumps({"beforeRun":True, "command":command, "exeSha256":digest(exe), "monoSha256":digest(mono), "runnerSha256":digest(Path(__file__)), "scenarios":8, "newtonsoftSha256":digest(out/"Newtonsoft.Json.dll")}, indent=2), encoding="utf-8")
        started = datetime.now(timezone.utc).isoformat()
        child = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = child.communicate()
        (out/"smoke-run.stdout.bin").write_bytes(stdout); (out/"smoke-run.stderr.bin").write_bytes(stderr)
        report = {"unityNativeLaunched":False, "productionDllExecuted":False, "editorCompile":records[0], "logicCompile":records[-1], "scenarioLimit":8,
                  "logicRun":{"pid":child.pid, "exitCode":child.returncode, "startedAt":started, "finishedAt":datetime.now(timezone.utc).isoformat(), "stdout":str(out/"smoke-run.stdout.bin"), "stderr":str(out/"smoke-run.stderr.bin"), "text":stdout.decode("utf-8-sig", "replace").strip()},
                  "boundary":"Actual SMK sequence/cleanup/live-claim code with an explicit pure-logic Unity boundary; real Editor assembly compiled separately, native SMOKE/Play not performed."}
        (out/"offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"editorCompile":records[0]["exitCode"], "logicExit":child.returncode, "text":report["logicRun"]["text"]}))
        if child.returncode: raise SystemExit(child.returncode)
        return

    if args.street_observation_compile:
        original = root / "Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added = src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs", "Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed = set(original.read_text(encoding="utf-8-sig").splitlines())
        added = [p for p in added if '"' + p.relative_to(root).as_posix() + '"' not in listed]
        build_bee("Editor.ProjectValidation", {}, added,
                  [root / "Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"], original_rsp=original)
        report = {"unityNativeLaunched": False, "dllExecuted": False, "currentActualReferences": True,
                  "compilerRuns": records}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"currentEditorCompileExit": records[0]["exitCode"], "sources": records[0]["sourceCount"], "references": records[0]["referenceCount"]}))
        return

    if args.street_probe:
        prior441=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LongLoopProbeChecks.PreAI441.cs.txt"
        isolated441=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/PreAI441ProbeTimingChecks.cs"
        if isolated441.read_text(encoding="utf-8-sig") != prior441.read_text(encoding="utf-8-sig").replace("namespace EatWhat.EditorValidation","namespace EatWhat.PreAI441TimingValidation",1):
            raise SystemExit("Immediate predecessor differs beyond isolation namespace")
        timing_snapshot=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LongLoopProbeChecks.PreAI436.cs.txt"
        timing_source=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LegacyProbeTimingChecks.cs"
        if timing_source.read_text(encoding="utf-8-sig")!=timing_snapshot.read_text(encoding="utf-8-sig").replace("namespace EatWhat.EditorValidation","namespace EatWhat.LegacyTimingValidation",1):
            raise SystemExit("Old timing source differs beyond its isolation namespace")
        legacy_root=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet"
        legacy_snapshot=legacy_root/"LongLoopAutomatedProbe.PreAI432.cs.txt"
        legacy_source=legacy_root/"LegacyStreetProbeDriverGuard.cs"
        expression=re.search(r'if\((camera\.GetComponents<CinemachineExtension>\(\)\.Any\(.*?binding\.Composer\))\)\s*throw new InvalidOperationException\("Unknown camera pipeline driver\."\);',legacy_snapshot.read_text(encoding="utf-8-sig"),re.S)
        if not expression or "return "+expression.group(1)+";" not in legacy_source.read_text(encoding="utf-8-sig"):
            raise SystemExit("Readonly old guard and compiled counterexample differ")
        if not args.probe_reference_manifest: raise SystemExit("Explicit unchanged dependency reference manifest required")
        manifest_path=Path(args.probe_reference_manifest).resolve()
        reuse=json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        replacements={}
        if set(reuse)!=set(("Toolkit","LongLoop","Legacy")): raise SystemExit("Three explicit unchanged dependency closures required")
        for name,record in reuse.items():
            reference=Path(record["referenceAssembly"])
            if digest(reference)!=record["referenceAssemblySha256"]: raise SystemExit("Reused reference bytes changed: "+name)
            prior=Path(record["preflight"])
            if digest(prior)!=record["preflightSha256"]: raise SystemExit("Reused source proof changed: "+name)
            proof=json.loads(prior.read_text(encoding="utf-8-sig"))
            for source in proof["sources"]:
                if digest(root/source["path"])!=source["sha256"]: raise SystemExit("Reused production source changed: "+source["path"])
            replacements[name]=reference
        original=root/"Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
        added=src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs","Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        listed=set(original.read_text(encoding="utf-8-sig").splitlines())
        added=[p for p in added if '"'+p.relative_to(root).as_posix()+'"' not in listed]
        build_bee("Editor.ProjectValidation",replacements,added,
            [root/"Library/Bee/artifacts/1900b0aE.dag/Unity.Cinemachine.ref.dll"],original_rsp=original)
        newtonsoft=root/"Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        shutil.copyfile(newtonsoft,out/"Newtonsoft.Json.dll")
        sdk_path=root/"Library/PackageCache/com.unity.pipeline@62c08c808737/Runtime/Compilation/EvalCodeCompiler.cs"
        sdk=sdk_path.read_text(encoding="utf-8-sig")
        start=sdk.index("private static object SerializeResult(object value)")
        brace=sdk.index("{",start);depth=1;end=brace+1
        while depth:
            depth += (sdk[end]=="{")-(sdk[end]=="}");end+=1
        boundary=root/"DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet"
        cancel_source=(boundary/"StreetProbeCancelSdkFixture.cs.txt").read_text(encoding="utf-8-sig")
        cancel_source=cancel_source.replace("__SDK_SERIALIZER__",sdk[start:end]).replace("__OLD_CANCEL_BODY__",(boundary/"StreetProbeCancel.PreAI441.body.cs.txt").read_text(encoding="utf-8-sig")).replace("__NEW_CANCEL_BODY__",(boundary/"StreetProbeCancel.body.cs").read_text(encoding="utf-8-sig"))
        generated=out/"CancelSdkBoundary.cs";generated.write_text(cancel_source,encoding="utf-8")
        sdk_inputs=[sdk_path,boundary/"StreetProbeCancelSdkFixture.cs.txt",boundary/"StreetProbeCancel.PreAI441.body.cs.txt",boundary/"StreetProbeCancel.body.cs"]
        (out/"cancel-sdk-inputs.json").write_text(json.dumps({"beforeCompiler":True,"sources":[{"path":str(p),"sha256":digest(p),"text":p.read_text(encoding="utf-8-sig")} for p in sdk_inputs]},indent=2),encoding="utf-8")
        cancel_exe=build("CancelSdkBoundary",[generated]+src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs","Assets/Scripts/LongLoop/DepthScaleMath.cs","Assets/Scripts/LongLoop/YSortMath.cs"),common+[newtonsoft,ue/"UnityEngine.JSONSerializeModule.dll",ue/"UnityEngine.SharedInternalsModule.dll"],"exe",main_type="CancelSdkBoundary")
        shutil.copyfile(ue/"UnityEngine.CoreModule.dll",out/"UnityEngine.CoreModule.dll")
        shutil.copyfile(ue/"UnityEngine.JSONSerializeModule.dll",out/"UnityEngine.JSONSerializeModule.dll")
        shutil.copyfile(ue/"UnityEngine.SharedInternalsModule.dll",out/"UnityEngine.SharedInternalsModule.dll")
        cancel_json=out/"cancel-read.fixture.json"
        cancel_json.write_text(json.dumps({"runId":"a6_street435","error":"EXPLICIT_CANCEL","cleanup":{k:True for k in ["inputRestored","configsRestored","configOwnerRestored","callbacksRemoved","temporaryObjectsDestroyed","noForeignOverwrite"]}}),encoding="utf-8")
        cancel_command=[str(mono),str(cancel_exe),str(cancel_json)]
        (out/"cancel-run.preflight.json").write_text(json.dumps({"beforeRun":True,"command":cancel_command,"exeSha256":digest(cancel_exe),"jsonSha256":digest(cancel_json)},indent=2),encoding="utf-8")
        cancel_start=datetime.now(timezone.utc).isoformat();cancel_run=subprocess.run(cancel_command,cwd=root,capture_output=True);cancel_end=datetime.now(timezone.utc).isoformat()
        (out/"cancel-run.stdout.bin").write_bytes(cancel_run.stdout);(out/"cancel-run.stderr.bin").write_bytes(cancel_run.stderr)
        (out/"cancel-run.json").write_text(json.dumps({"command":cancel_command,"exitCode":cancel_run.returncode,"startedAt":cancel_start,"finishedAt":cancel_end,"fixtureOnly":True,"installedSerializerSource":str(sdk_path)},indent=2),encoding="utf-8")
        if cancel_run.returncode:
            print(cancel_run.stderr.decode("utf-8-sig","replace"),file=sys.stderr);raise SystemExit(cancel_run.returncode)
        exe=build("StreetProbeChecksTests",src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs",
            "DevTools/LongLoop/FollowUnityShim.cs","DevTools/LongLoop/StreetProbeChecksTests.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs","Assets/Scripts/LongLoop/YSortMath.cs",
            "DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeFixtureData.cs",
            "DevTools/LongLoop/StreetConfigurationTests.cs","DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LegacyStreetProbeDriverGuard.cs",
            "DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/StreetProbeBudgetTests.cs","DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/PreAI441ProbeTimingChecks.cs","DevTools/Workflow/Fixtures/ShortCycleCi/LongLoopStreet/LegacyProbeTimingChecks.cs"),
            [fx/"mscorlib.dll",fx/"System.dll",fx/"System.Core.dll",fx/"System.Web.Extensions.dll",fx/"Facades/netstandard.dll",newtonsoft],"exe",main_type="StreetProbeChecksTests")
        command=[str(mono),str(exe)]
        preflight=out/"probe-run.preflight.json"
        preflight.write_text(json.dumps({"capturedBeforeRun":True,"command":command,"exeSha256":digest(exe),
            "monoSha256":digest(mono),"runnerSha256":digest(Path(__file__)),"runtimeJsonLibrary":{"path":str(out/"Newtonsoft.Json.dll"),"sha256":digest(out/"Newtonsoft.Json.dll")},"oldSourceSnapshotSha256":digest(legacy_snapshot),"oldExtractedCounterexampleSha256":digest(legacy_source)},indent=2),encoding="utf-8")
        start=datetime.now(timezone.utc).isoformat();result=subprocess.run(command,cwd=root,capture_output=True);end=datetime.now(timezone.utc).isoformat()
        stdout,stderr=out/"probe-run.stdout.bin",out/"probe-run.stderr.bin";stdout.write_bytes(result.stdout);stderr.write_bytes(result.stderr)
        report={"unityNativeLaunched":False,"unityProductionDllExecuted":False,"editorCompile":records[0],"pureCompile":records[-1],
            "reusedProductionReferences":reuse,"referenceManifest":str(manifest_path),"referenceManifestSha256":digest(manifest_path),"cancelSdkCompile":records[1],"cancelSdkRunExit":cancel_run.returncode,
            "logicRun":{"exitCode":result.returncode,"startedAt":start,"finishedAt":end,"stdout":str(stdout),"stderr":str(stderr),
                "text":result.stdout.decode("utf-8-sig","replace").strip()},"boundary":"Actual production bounded sequence/check DTO logic; Unity/Brain/physics/native renderer remain future ENGINE."}
        (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"editorCompile":records[0]["exitCode"],"logicExit":result.returncode,"logic":report["logicRun"]["text"]}))
        if result.returncode:print(result.stderr.decode("utf-8-sig","replace"),file=sys.stderr);raise SystemExit(result.returncode)
        return

    if args.dynamic_zoom or args.look_float or args.vertical_offset or args.follow_composition or args.parallax:
        configs = sorted((root / "Assets/Scripts/LongLoop/Configuration").glob("*.cs"))
        module_name = "StreetParallax" if args.parallax else ("StreetFollowComposition" if args.follow_composition else ("StreetVerticalOffset" if args.vertical_offset else ("StreetLookFloat" if args.look_float else "StreetDynamicZoom")))
        stage = "parallax" if args.parallax else ("composition" if args.follow_composition else ("vertical-offset" if args.vertical_offset else ("look-float" if args.look_float else "zoom")))
        zoom = root / ("Assets/Scripts/LongLoop/" + module_name + ".cs")
        pipeline = root / "Assets/Scripts/LongLoop/StreetFollowPipeline.cs"
        lines = set((bee / "LongLoop.rsp").read_text(encoding="utf-8-sig").splitlines())
        missing = [p for p in sorted((root / "Assets/Scripts/LongLoop").rglob("*.cs"))
                   if '"' + p.relative_to(root).as_posix() + '"' not in lines]
        build_bee("LongLoop", {}, missing)
        production = list(records)
        boundary = out / "ZoomConfigBoundary.cs"
        boundary.write_text("namespace UnityEngine { public static class Debug { public static void LogWarning(string message, object context) {} } }", encoding="utf-8")
        sources = configs + [zoom, pipeline, boundary] + src("DevTools/LongLoop/" + module_name + "Tests.cs",
            "DevTools/LongLoop/StreetConfigLogicTests.cs", "DevTools/LongLoop/FollowUnityShim.cs",
            "Assets/Scripts/LongLoop/ChannelContext.cs", "Assets/Scripts/Tools/Input/IGameInput.cs")
        if args.follow_composition:
            sources += src("Assets/Scripts/LongLoop/StreetDynamicZoom.cs")
        if args.parallax:
            sources += src("Assets/Scripts/LongLoop/StreetParallaxLayer.cs", "DevTools/LongLoop/StreetParallaxLayerTests.cs")
        exe = build(module_name + "Tests", sources, [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"],
                    "exe", main_type=module_name + "Tests")
        command = [str(mono), str(exe)]
        started = datetime.now(timezone.utc).isoformat()
        process = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = process.communicate()
        (out / (stage + "-run.stdout.bin")).write_bytes(stdout)
        (out / (stage + "-run.stderr.bin")).write_bytes(stderr)
        report = {"unityNativeLaunched": False, "productionCompiles": production, "logicCompile": records[-1],
                  "logicTest": {"command": command, "pid": process.pid, "exitCode": process.returncode,
                                "startedAt": started, "finishedAt": datetime.now(timezone.utc).isoformat(),
                                "text": stdout.decode("utf-8-sig", "replace")}}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"productionExitCodes": [r["exitCode"] for r in production],
                          "logicRunExitCode": process.returncode, "logic": report["logicTest"]["text"]}))
        if process.returncode:
            print(stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(process.returncode)
        return

    if args.follow_pipeline:
        pipeline=root/"Assets/Scripts/LongLoop/StreetFollowPipeline.cs"
        text=pipeline.read_text(encoding="utf-8-sig")
        forbidden=r"\b(?:Cinemachine\w*|Camera\w*|Transform\w*|MonoBehaviour|ScriptableObject|StreetConfigs|RawPosition|PositionCorrection|ForceCameraPosition)\b"
        if re.search(forbidden,text): raise SystemExit("Pure target pipeline contains an engine/object/global-state dependency")
        (out/"pure-source-check.json").write_text(json.dumps({"source":str(pipeline),"sha256":digest(pipeline),"forbiddenPattern":forbidden,"passed":True},indent=2),encoding="utf-8")
        configs=sorted((root/"Assets/Scripts/LongLoop/Configuration").glob("*.cs"))
        def missing_pipeline(name,candidates):
            lines=set((bee/(name+".rsp")).read_text(encoding="utf-8-sig").splitlines())
            return [p for p in candidates if '"'+p.relative_to(root).as_posix()+'"' not in lines]
        if not args.follow_pipeline_logic_only:
            toolkit=build_bee("Toolkit",{})
            longloop=build_bee("LongLoop",{"Toolkit":toolkit},missing_pipeline("LongLoop",configs+[pipeline]))
            build_bee("Legacy",{"Toolkit":toolkit,"LongLoop":longloop})
        production=list(records)
        boundary=out/"PipelineConfigBoundary.cs"
        boundary.write_text("namespace UnityEngine { public static class Debug { public static void LogWarning(string message, object context) {} } }",encoding="utf-8")
        sources=configs+[pipeline]+src("DevTools/LongLoop/StreetFollowPipelineTests.cs",
            "DevTools/LongLoop/StreetConfigLogicTests.cs","DevTools/LongLoop/FollowUnityShim.cs",
            "Assets/Scripts/LongLoop/ChannelContext.cs",
            "Assets/Scripts/Tools/Input/IGameInput.cs")+[boundary]
        exe=build("StreetFollowPipelineTests",sources,[fx/"mscorlib.dll",fx/"System.dll",fx/"System.Core.dll"],"exe",main_type="StreetFollowPipelineTests")
        command=[str(mono),str(exe)]
        (out/"pipeline-run.preflight.json").write_text(json.dumps({"capturedBeforeRun":True,"command":command,
            "monoSha256":digest(mono),"exeSha256":digest(exe),"runnerSha256":digest(Path(__file__))},indent=2),encoding="utf-8")
        started=datetime.now(timezone.utc).isoformat()
        process=subprocess.Popen(command,cwd=root,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        stdout,stderr=process.communicate()
        (out/"pipeline-run.stdout.bin").write_bytes(stdout);(out/"pipeline-run.stderr.bin").write_bytes(stderr)
        report={"unityVersion":"6000.3.24f1","unityNativeLaunched":False,"productionCompiles":production,
                "logicCompile":records[-1],"logicTest":{"command":command,"pid":process.pid,"exitCode":process.returncode,
                "startedAt":started,"finishedAt":datetime.now(timezone.utc).isoformat(),"text":stdout.decode("utf-8-sig","replace")}}
        (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"productionExitCodes":[r["exitCode"] for r in production],"logicRunExitCode":process.returncode,"logic":report["logicTest"]["text"]}))
        if process.returncode:
            print(stderr.decode("utf-8-sig","replace"),file=sys.stderr);raise SystemExit(process.returncode)
        return

    if args.follow_driver or args.follow_driver_v012 or args.follow_r_smoke:
        configs=sorted((root/"Assets/Scripts/LongLoop/Configuration").glob("*.cs"))
        own=src("Assets/Scripts/LongLoop/StreetFollowDriver.cs","Assets/Scripts/LongLoop/CameraFollowConfigBinder.cs","Assets/Scripts/LongLoop/StreetCameraDebug.cs","Assets/Scripts/LongLoop/StreetFollowPipeline.cs","Assets/Scripts/LongLoop/StreetDynamicZoom.cs","Assets/Scripts/LongLoop/StreetLookFloat.cs","Assets/Scripts/LongLoop/StreetVerticalOffset.cs","Assets/Scripts/LongLoop/StreetFollowComposition.cs")
        forbidden=r"\bForceCameraPosition\s*\(|\bBindingFlags\.NonPublic|\b(?:RawPosition|PositionCorrection)\s*(?:[+\-*/]?=)"
        for path in own:
            if re.search(forbidden,path.read_text(encoding="utf-8-sig")):raise SystemExit("Illegal target ownership operation in "+str(path))
        (out/"driver-source-check.json").write_text(json.dumps({"paths":[str(p) for p in own],"forbiddenPattern":forbidden,"passed":True},indent=2),encoding="utf-8")
        cm=root/"Library/PackageCache/com.unity.cinemachine@f3f96bcb59af"
        if json.loads((cm/"package.json").read_text())["version"]!="3.1.7":raise SystemExit("Recheck CM version")
        if args.follow_driver_v012:
            lines=set((bee/"LongLoop.rsp").read_text(encoding="utf-8-sig").splitlines())
            missing=[p for p in sorted((root/"Assets/Scripts/LongLoop").rglob("*.cs"))
                     if '"' + p.relative_to(root).as_posix() + '"' not in lines]
            build_bee("LongLoop",{},missing)
        if not (args.follow_driver_logic_only or args.follow_r_smoke_logic_only or args.follow_driver_v012):
            def missing_driver(name,candidates):
                lines=set((bee/(name+".rsp")).read_text(encoding="utf-8-sig").splitlines())
                return [p for p in candidates if '"'+p.relative_to(root).as_posix()+'"' not in lines]
            toolkit=build_bee("Toolkit",{})
            longloop=build_bee("LongLoop",{"Toolkit":toolkit},missing_driver("LongLoop",sorted((root/"Assets/Scripts/LongLoop").rglob("*.cs"))))
            legacy=build_bee("Legacy",{"Toolkit":toolkit,"LongLoop":longloop})
            editor_rsp=root/"Library/Bee/artifacts/1900b0aE.dag/Editor.ProjectValidation.rsp"
            lines=editor_rsp.read_text(encoding="utf-8-sig").splitlines()
            replacements={name:path for name,path in {"Toolkit":toolkit,"LongLoop":longloop,"Legacy":legacy}.items() if any(line.startswith('-r:') and line.endswith('/'+name+'.ref.dll"') for line in lines)}
            if "LongLoop" not in replacements:raise SystemExit("Current Editor must reference the new LongLoop closure")
            added=src("Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs","Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
            added=[p for p in added if '"'+p.relative_to(root).as_posix()+'"' not in lines]
            build_bee("Editor.ProjectValidation",replacements,added,original_rsp=editor_rsp)
        production=list(records)
        def method(text,marker):
            start=text.index(marker);begin=text.index("{",start);end=begin+1;depth=1
            while depth:
                depth+=(text[end]=="{")-(text[end]=="}");end+=1
            return text[start:end]
        vcam=cm/"Runtime/Core/CinemachineVirtualCameraBase.cs";confiner=cm/"Runtime/Behaviours/CinemachineConfiner2D.cs"
        text=vcam.read_text(encoding="utf-8-sig")
        callbacks=out/"CmInstalledCallbacks.cs"
        fragments=[method(text,m) for m in ("internal void AddExtension(","internal void RemoveExtension(","protected void InvokePostPipelineStageCallback(")]
        fragments.append("internal List<CinemachineExtension> Extensions { get; private set; }")
        callbacks.write_text("using System.Collections.Generic;using UnityEngine;namespace Unity.Cinemachine { public partial class CinemachineVirtualCameraBase {"+"\n".join(fragments)+"} public partial class CinemachineConfiner2D {"+method(confiner.read_text(encoding="utf-8-sig"),"protected override void PostPipelineStageCallback(")+"\n"+method(confiner.read_text(encoding="utf-8-sig"),"public void InvalidateLensCache(")+"}}",encoding="utf-8")
        cm_sources=[cm/"Runtime/Components/CinemachinePositionComposer.cs",cm/"Runtime/Core/ScreenComposerSettings.cs",cm/"Runtime/Core/LookaheadSettings.cs",cm/"Runtime/Core/Predictor.cs",callbacks]
        (out/"cm-callback-provenance.json").write_text(json.dumps({"originals":[{"path":str(p),"sha256":digest(p)} for p in [vcam,confiner]],"boundary":"Actual installed Composer/Damper and callback bodies; Unity/Brain/Transform, player public-state shell and declared rectangle cache substituted."},indent=2),encoding="utf-8")
        sources=configs+own+src("Assets/Scripts/LongLoop/StreetFollowPoint.cs","Assets/Scripts/LongLoop/GroundSpace.cs","Assets/Scripts/Tools/GroundSpaceMath.cs","Assets/Scripts/LongLoop/ChannelContext.cs","Assets/Scripts/Tools/Input/IGameInput.cs","DevTools/LongLoop/FollowUnityShim.cs","DevTools/LongLoop/FollowCinemachineBoundary.cs","DevTools/LongLoop/ChannelUnityBoundary.cs","DevTools/LongLoop/StreetConfigLogicTests.cs","DevTools/LongLoop/StreetFollowPipelineTests.cs","DevTools/LongLoop/StreetFollowDriverTests.cs")+cm_sources
        sources+=src("Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")
        newtonsoft=root/"Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"
        shutil.copyfile(newtonsoft,out/"Newtonsoft.Json.dll")
        main_type="StreetFollowDriverV012Tests" if args.follow_driver_v012 else "StreetFollowDriverTests"
        if args.follow_r_smoke:
            probe=root/"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs"
            actual=probe.read_text(encoding="utf-8-sig")
            names=["private static object FollowRReady(","private static bool FollowRBindings(","private static StreetProbeReport BeginFollowR(","private static void ApplyFollowRConsole(","private sealed class FollowRRun","private static StreetProbeReport ReadSmoke(","private static bool ValidateSmokeRequest(","private static bool ValidateFollowRClaim("]
            fragments=[method(actual,n) for n in names]
            generated=out/"FollowRLifecycle.cs"
            generated.write_text("using System;using System.IO;using System.Linq;using System.Text;using System.Diagnostics;using EatWhat.LongLoop;using EatWhat.LongLoop.Configuration;using EatWhat.Toolkit.Input;using Spine.Unity;using Unity.Cinemachine;using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;namespace EatWhat.EditorValidation {public static partial class LongLoopAutomatedProbe {"+"\n".join(f.replace("private static ","public static ",1).replace("private sealed class FollowRRun","public sealed class FollowRRun",1).replace("bool ValidateSmokeRequest(","bool ActualValidateSmokeRequest(",1).replace("bool ValidateFollowRClaim(","bool ActualValidateFollowRClaim(",1) for f in fragments)+"}}",encoding="utf-8")
            (out/"follow-r-lifecycle-source.json").write_text(json.dumps({"source":str(probe),"sha256":digest(probe),"fragments":dict(zip(names,fragments)),"generatedSha256":digest(generated),"onlyExtractionChange":"Public visibility and Actual method-name aliases for fixture seam dispatch; production method bodies unchanged","boundary":"Application/Editor/Brain/scene/controller/JsonUtility/filesystem publication are transparent offline shells. Production Begin/Read/Ready/FollowRRun/Console and actual live snapshot/claim methods execute against isolated files when UseRealClaimValidation is set. No real production claim or Unity execution."},indent=2),encoding="utf-8")
            sources+=src("DevTools/LongLoop/FollowRSmokeTests.cs","DevTools/LongLoop/FollowRSmokeUnityBoundary.cs","Assets/Scripts/Tools/Input/LegacyInputProvider.cs")+[generated]
            main_type="FollowRSmokeTests"
        exe=build(main_type,sources,[fx/"mscorlib.dll",fx/"System.dll",fx/"System.Core.dll",fx/"Facades/netstandard.dll",newtonsoft],"exe",main_type=main_type,language="preview")
        command=[str(mono),str(exe)]+(["--old-input-counterexample"] if args.follow_r_input_counterexample else ["--old-snapshot-counterexample"] if args.follow_r_snapshot_counterexample else ["--old-timestep-counterexample"] if args.follow_r_timestep_counterexample else ["--old-startup-counterexample"] if args.follow_r_startup_counterexample else [])
        (out/"driver-run.preflight.json").write_text(json.dumps({"capturedBeforeRun":True,"command":command,"monoSha256":digest(mono),"exeSha256":digest(exe)},indent=2),encoding="utf-8")
        start=datetime.now(timezone.utc).isoformat();process=subprocess.Popen(command,cwd=root,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=dict(os.environ,FOLLOW_R_FIXTURE_ROOT=str(out/"lifecycle-fixtures"),FOLLOW_R_LIVE_MODE_PATH=str(out.parent/"live-mode-before.json")));stdout,stderr=process.communicate()
        (out/"driver-run.stdout.bin").write_bytes(stdout);(out/"driver-run.stderr.bin").write_bytes(stderr)
        report={"unityNativeLaunched":False,"productionCompiles":production,"logicCompile":records[-1],"logicTest":{"command":command,"pid":process.pid,"exitCode":process.returncode,"startedAt":start,"finishedAt":datetime.now(timezone.utc).isoformat(),"text":stdout.decode("utf-8-sig","replace")}}
        (out/"offline-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
        print(json.dumps({"productionExitCodes":[r["exitCode"] for r in production],"logicRunExitCode":process.returncode,"logic":report["logicTest"]["text"]}))
        if process.returncode:print(stderr.decode("utf-8-sig","replace"),file=sys.stderr);raise SystemExit(process.returncode)
        return


    # These older headless shims omit Inspector-only Tooltip metadata.
    if args.y_sort or args.walk_area or args.depth_scale or args.ground_space or args.street_config:
        config_metadata = out / "ConfigMetadataBoundary.cs"
        config_metadata.write_text("namespace UnityEngine { [System.AttributeUsage(System.AttributeTargets.Field)] public sealed class TooltipAttribute : System.Attribute { public TooltipAttribute(string text) {} } }", encoding="utf-8")

    if args.y_sort:
        new_longloop = sorted((root / "Assets/Scripts/LongLoop/Configuration").glob("*.cs")) + src(
            "Assets/Scripts/LongLoop/GroundSpace.cs", "Assets/Scripts/LongLoop/WalkArea.cs",
            "Assets/Scripts/LongLoop/PlanarMotion.cs", "Assets/Scripts/LongLoop/DepthScale.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs", "Assets/Scripts/LongLoop/IDepthOverride.cs",
            "Assets/Scripts/LongLoop/YSorter.cs", "Assets/Scripts/LongLoop/YSortMath.cs")
        def missing_from_bee(name, candidates):
            listed = set((bee / (name + ".rsp")).read_text(encoding="utf-8-sig").splitlines())
            return [p for p in candidates if '"' + p.relative_to(root).as_posix() + '"' not in listed]
        toolkit = build_bee("Toolkit", {}, missing_from_bee("Toolkit", src("Assets/Scripts/Tools/GroundSpaceMath.cs")))
        longloop = build_bee("LongLoop", {"Toolkit": toolkit}, missing_from_bee("LongLoop", new_longloop))
        build_bee("Legacy", {"Toolkit": toolkit, "LongLoop": longloop})
        logic = build("YSorterLogicTests", src(
            "Assets/Scripts/LongLoop/YSorter.cs", "Assets/Scripts/LongLoop/YSortMath.cs",
            "Assets/Scripts/LongLoop/DepthScale.cs", "Assets/Scripts/LongLoop/DepthScaleMath.cs",
            "Assets/Scripts/LongLoop/IDepthOverride.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigAsset.cs",
            "Assets/Scripts/LongLoop/Configuration/DepthScaleConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraModeConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/SceneBoundsConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigs.cs",
            "DevTools/LongLoop/WalkAreaUnityShim.cs", "DevTools/LongLoop/YSortRenderingShim.cs",
            "DevTools/LongLoop/YSorterLogicTests.cs") + [config_metadata],
            [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        command = [str(mono), str(logic)]
        before = {"capturedBeforeRun": True, "command": command,
                  "exeSha256": digest(logic), "monoSha256": digest(mono),
                  "runnerSha256": digest(Path(__file__))}
        (out / "sorting-run.preflight.json").write_text(json.dumps(before, indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        run = subprocess.run(command, cwd=root, capture_output=True)
        finished_at = datetime.now(timezone.utc).isoformat()
        stdout, stderr = out / "sorting-run.stdout.bin", out / "sorting-run.stderr.bin"
        stdout.write_bytes(run.stdout)
        stderr.write_bytes(run.stderr)
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "productionCompiles": records[:3],
                  "logicTest": {"compileExitCode": records[3]["exitCode"], "runExitCode": run.returncode,
                                "startedAt": started_at, "finishedAt": finished_at,
                                "stdout": str(stdout), "stderr": str(stderr),
                                "text": run.stdout.decode("utf-8-sig", "replace").strip()}}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"productionExitCodes": [r["exitCode"] for r in records[:3]],
                          "logicRunExitCode": run.returncode, "logic": report["logicTest"]["text"]}))
        if run.returncode: raise SystemExit(run.returncode)
        return

    if args.depth_facing_old_source:
        fixture = root / "DevTools/LongLoop/Fixtures/DepthScaleFacingBaselineAI426"
        manifest = json.loads((fixture / "SOURCE.json").read_text(encoding="utf-8-sig"))
        for source in manifest["files"]:
            if digest(root / source["fixturePath"]) != source["sourceSha256"]:
                raise SystemExit("Preserved rejected source identity differs")
        sources = [fixture / name for name in (
            "DepthScale.cs", "LongLoopPlayerController.cs", "TomatoSpineAvatar.cs")]
        sources += src(
            "Assets/Scripts/Tools/Input/IGameInput.cs",
            "Assets/Scripts/LongLoop/IPlayerAvatar.cs",
            "Assets/Scripts/LongLoop/HorizontalMotion.cs",
            "Assets/Scripts/LongLoop/WalkArea.cs",
            "Assets/Scripts/LongLoop/PlanarMotion.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs",
            "Assets/Scripts/LongLoop/IDepthOverride.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigAsset.cs",
            "Assets/Scripts/LongLoop/Configuration/DepthScaleConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraModeConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/SceneBoundsConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigs.cs",
            "DevTools/LongLoop/WalkAreaLogicTests.cs",
            "DevTools/LongLoop/WalkAreaUnityShim.cs",
            "DevTools/LongLoop/DepthScaleLogicTests.cs",
            "DevTools/LongLoop/DepthScaleSpineShim.cs",
            "DevTools/LongLoop/DepthScaleFacingLifecycleTests.cs")
        exe = build("FacingLifecycleOldSources", sources,
                    [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"],
                    "exe", "DepthScaleFacingLifecycleTests")
        command = [str(mono), str(exe)]
        before = {"capturedBeforeRun": True, "command": command,
                  "exeSha256": digest(exe), "monoSha256": digest(mono),
                  "runnerSha256": digest(Path(__file__))}
        (out / "old-run.preflight.json").write_text(json.dumps(before, indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        run = subprocess.run(command, cwd=root, capture_output=True)
        finished_at = datetime.now(timezone.utc).isoformat()
        stdout, stderr = out / "old-run.stdout.bin", out / "old-run.stderr.bin"
        stdout.write_bytes(run.stdout)
        stderr.write_bytes(run.stderr)
        text = run.stdout.decode("utf-8-sig", "replace")
        expected = run.returncode == 1 and all(marker in text for marker in (
            "CASE:controller-depth-before-awake-disabled-left: expected -2, actual -1.88",
            "CASE:avatar-depth-before-awake-disabled-left: expected -2, actual -1.88",
            "LIFECYCLE_REGRESSION_FAILURES=2"))
        report = {"kind": "PRESERVED_REAL_SOURCE_EXPECTED_NEGATIVE", "compile": records[0],
                  "run": {"command": command, "startedAt": started_at, "finishedAt": finished_at,
                          "exitCode": run.returncode, "stdout": str(stdout), "stderr": str(stderr), "text": text},
                  "expectedFailureConfirmed": expected, "unityNativeLaunched": False,
                  "fixtureManifest": str(fixture / "SOURCE.json"),
                  "fixtureManifestSha256": digest(fixture / "SOURCE.json")}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"compileExitCode": records[0]["exitCode"], "childRunExitCode": run.returncode,
                          "expectedFailureConfirmed": expected, "text": text}))
        if not expected: raise SystemExit(2)
        return

    if args.walk_area or args.depth_scale:
        config_sources = sorted((root / "Assets/Scripts/LongLoop/Configuration").glob("*.cs"))
        if len(config_sources) != 14:
            raise SystemExit("Current config source closure differs")
        math = root / "Assets/Scripts/Tools/GroundSpaceMath.cs"
        new_longloop = config_sources + src(
            "Assets/Scripts/LongLoop/GroundSpace.cs",
            "Assets/Scripts/LongLoop/WalkArea.cs",
            "Assets/Scripts/LongLoop/PlanarMotion.cs",
            "Assets/Scripts/LongLoop/DepthScale.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs",
            "Assets/Scripts/LongLoop/IDepthOverride.cs")
        current_toolkit = build_bee("Toolkit", {}, [math])
        current_longloop = build_bee("LongLoop", {"Toolkit": current_toolkit}, new_longloop)
        build_bee("Legacy", {"Toolkit": current_toolkit, "LongLoop": current_longloop})
        shim = out / "WalkAreaUnityShim.cs"
        shim.write_text((root / "DevTools/LongLoop/WalkAreaUnityShim.cs").read_text(encoding="utf-8"), encoding="utf-8")
        logic_sources = src(
            "Assets/Scripts/Tools/Input/IGameInput.cs",
            "Assets/Scripts/LongLoop/IPlayerAvatar.cs",
            "Assets/Scripts/LongLoop/HorizontalMotion.cs",
            "Assets/Scripts/LongLoop/WalkArea.cs",
            "Assets/Scripts/LongLoop/PlanarMotion.cs",
            "Assets/Scripts/LongLoop/LongLoopPlayerController.cs",
            "Assets/Scripts/LongLoop/TomatoSpineAvatar.cs",
            "Assets/Scripts/LongLoop/DepthScale.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs",
            "Assets/Scripts/LongLoop/IDepthOverride.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigAsset.cs",
            "Assets/Scripts/LongLoop/Configuration/DepthScaleConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraModeConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/SceneBoundsConfig.cs",
            "Assets/Scripts/LongLoop/Configuration/StreetConfigs.cs",
            "DevTools/LongLoop/WalkAreaLogicTests.cs",
            "DevTools/LongLoop/WalkAreaUnityShim.cs",
            "DevTools/LongLoop/DepthScaleSpineShim.cs") + [config_metadata]
        if args.depth_scale:
            logic_sources += src("DevTools/LongLoop/DepthScaleLogicTests.cs",
                                 "DevTools/LongLoop/DepthScaleFacingLifecycleTests.cs")
        logic = build("DepthScaleLogicTests" if args.depth_scale else "WalkAreaLogicTests", logic_sources,
            [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe",
            "DepthScaleLogicTests" if args.depth_scale else "WalkAreaLogicTests")
        run_preflight = out / "walk-run.preflight.json"
        run_preflight.write_text(json.dumps({"capturedBeforeRun": True,
                                             "exe": str(logic), "exeSha256": digest(logic),
                                             "mono": str(mono), "monoSha256": digest(mono),
                                             "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
        run_started_at = datetime.now(timezone.utc).isoformat()
        run = subprocess.run([str(mono), str(logic)], cwd=root, capture_output=True)
        run_finished_at = datetime.now(timezone.utc).isoformat()
        stdout, stderr = out / "walk-run.stdout.bin", out / "walk-run.stderr.bin"
        stdout.write_bytes(run.stdout)
        stderr.write_bytes(run.stderr)
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "productionCompiles": records[:3],
                  "logicTest": {"compileExitCode": records[3]["exitCode"],
                                "startedAt": run_started_at, "finishedAt": run_finished_at,
                                "runExitCode": run.returncode, "stdout": str(stdout),
                                "stderr": str(stderr), "preflight": str(run_preflight),
                                "text": run.stdout.decode("utf-8-sig", "replace").strip(),
                                "unityBoundaryShim": str(shim)}}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"productionExitCodes": [r["exitCode"] for r in records[:3]],
                          "logicRunExitCode": run.returncode, "logic": report["logicTest"]["text"]}))
        if run.returncode: raise SystemExit(run.returncode)
        return

    if args.ground_space:
        config_sources = src("Assets/Scripts/LongLoop/Configuration/StreetConfigAsset.cs",
                             "Assets/Scripts/LongLoop/Configuration/DepthScaleConfig.cs",
                             "Assets/Scripts/LongLoop/Configuration/CameraModeConfig.cs",
                             "Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs",
                             "Assets/Scripts/LongLoop/Configuration/SceneBoundsConfig.cs",
                             "Assets/Scripts/LongLoop/Configuration/StreetConfigs.cs")
        math = root / "Assets/Scripts/Tools/GroundSpaceMath.cs"
        adapter = root / "Assets/Scripts/LongLoop/GroundSpace.cs"
        current_toolkit = build_bee("Toolkit", {}, [math])
        build_bee("LongLoop", {"Toolkit": current_toolkit},
                  sorted((root / "Assets/Scripts/LongLoop/Configuration").glob("*.cs")) + [adapter])
        shim = out / "GroundUnityShim.cs"
        shim.write_text("""using System;
namespace UnityEngine {
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute {
        public string menuName;
    }
    public class ScriptableObject {}
    public class Transform {}
    public class Collider2D {}
    public struct Vector3 {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
    }
}
""", encoding="utf-8")
        logic = build("GroundSpaceLogicTests", [math, adapter] + config_sources +
                      src("DevTools/LongLoop/GroundSpaceLogicTests.cs") + [shim, config_metadata],
                      [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        run_preflight = out / "ground-run.preflight.json"
        run_preflight.write_text(json.dumps({"capturedBeforeRun": True,
                                             "exe": str(logic), "exeSha256": digest(logic),
                                             "mono": str(mono), "monoSha256": digest(mono),
                                             "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
        run = subprocess.run([str(mono), str(logic)], cwd=root, capture_output=True)
        stdout, stderr = out / "ground-run.stdout.bin", out / "ground-run.stderr.bin"
        stdout.write_bytes(run.stdout)
        stderr.write_bytes(run.stderr)
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "productionCompiles": records[:2],
                  "logicTest": {"compileExitCode": records[2]["exitCode"],
                                "runExitCode": run.returncode, "stdout": str(stdout),
                                "stderr": str(stderr), "preflight": str(run_preflight),
                                "text": run.stdout.decode("utf-8-sig", "replace").strip(),
                                "shimOnlyForSourceLogic": str(shim)}}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"productionExitCodes": [r["exitCode"] for r in records[:2]],
                          "logicRunExitCode": run.returncode, "logic": report["logicTest"]["text"]}))
        if run.returncode: raise SystemExit(run.returncode)
        return

    if args.street_config:
        config_sources = sorted((root / "Assets/Scripts/LongLoop/Configuration").glob("*.cs"))
        if len(config_sources) != 14:
            raise SystemExit("Expected four groups, seven placeholders, base, StreetConfigs and binder")
        current_toolkit = build_bee("Toolkit", {})
        build_bee("LongLoop", {"Toolkit": current_toolkit}, config_sources)
        shim = out / "ConfigUnityShim.cs"
        shim.write_text("""using System;
namespace UnityEngine {
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute {
        public string menuName;
    }
    public class ScriptableObject {}
    public class MonoBehaviour { public bool isActiveAndEnabled = true; }
    public class Transform {}
    public class Collider2D {}
    public static class Application { public static bool isPlaying; }
    public static class Debug { public static void LogWarning(string message, object context) {} }
}
""", encoding="utf-8")
        logic = build("StreetConfigLogicTests", config_sources +
                      src("DevTools/LongLoop/StreetConfigLogicTests.cs") + [shim, config_metadata],
                      [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        run_preflight = out / "config-run.preflight.json"
        run_preflight.write_text(json.dumps({"capturedBeforeRun": True,
                                             "exe": str(logic), "exeSha256": digest(logic),
                                             "mono": str(mono), "monoSha256": digest(mono),
                                             "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
        run = subprocess.run([str(mono), str(logic)], cwd=root, capture_output=True)
        stdout, stderr = out / "config-run.stdout.bin", out / "config-run.stderr.bin"
        stdout.write_bytes(run.stdout)
        stderr.write_bytes(run.stderr)
        report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
                  "productionCompiles": records[:2],
                  "logicTest": {"compileExitCode": records[2]["exitCode"],
                                "runExitCode": run.returncode, "stdout": str(stdout),
                                "stderr": str(stderr), "preflight": str(run_preflight),
                                "text": run.stdout.decode("utf-8-sig", "replace").strip(),
                                "shimOnlyForSourceLogic": str(shim)}}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"productionExitCodes": [r["exitCode"] for r in records[:2]],
                          "logicRunExitCode": run.returncode, "logic": report["logicTest"]["text"]}))
        if run.returncode: raise SystemExit(run.returncode)
        return

    current_toolkit = build_bee("Toolkit", {})
    # Match the existing channel/driver branches when Bee predates current source additions.
    current_lines = set((bee / "LongLoop.rsp").read_text(encoding="utf-8-sig").splitlines())
    current_missing = [p for p in sorted((root / "Assets/Scripts/LongLoop").rglob("*.cs"))
                       if '"' + p.relative_to(root).as_posix() + '"' not in current_lines]
    current_longloop = build_bee("LongLoop", {"Toolkit": current_toolkit}, current_missing)
    build_bee("Legacy", {"Toolkit": current_toolkit, "LongLoop": current_longloop})
    shim = out / "LogicVector2Shim.cs"
    shim.write_text("""using System;
using System.Collections.Generic;
namespace UnityEngine {
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero => new Vector2(0f,0f); }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static explicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y); }
    public enum KeyCode { A,D,LeftArrow,RightArrow,E,Escape,W,S,UpArrow,DownArrow }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    public class MonoBehaviour { public bool isActiveAndEnabled = true; }
    public static class Input { public static HashSet<KeyCode> Held = new HashSet<KeyCode>();
        public static HashSet<KeyCode> Down = new HashSet<KeyCode>(); public static Vector3 mousePosition;
        public static bool GetKey(KeyCode key) => Held.Contains(key);
        public static bool GetKeyDown(KeyCode key) => Down.Contains(key); }
    public static class Screen { public static int width,height; }
    public static class Application { public static bool isFocused=true; }
}
// Transparent processed-control sample only; no real Input System device is created.
namespace UnityEngine.InputSystem.Controls {
    public sealed class StickControl { public UnityEngine.Vector2 Value; public UnityEngine.Vector2 ReadValue()=>Value; }
}
namespace UnityEngine.InputSystem {
    public sealed class Gamepad {
        public static Gamepad current;
        public bool added=true,enabled=true;
        public Controls.StickControl rightStick=new Controls.StickControl();
    }
}
""", encoding="utf-8")
    logic = build("LongLoopLogicTests",
                  src("Assets/Scripts/Tools/Input/IGameInput.cs",
                      "Assets/Scripts/Tools/Input/LegacyInputProvider.cs",
                      "Assets/Scripts/LongLoop/HorizontalMotion.cs",
                      "DevTools/LongLoop/LongLoopLogicTests.cs") + [shim],
                  [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
    (out / "logic-run.preflight.json").write_text(json.dumps({
        "capturedBeforeRun": True, "exe": str(logic), "exeSha256": digest(logic),
        "mono": str(mono), "monoSha256": digest(mono),
        "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
    run = subprocess.run([str(mono), str(logic)], cwd=root, capture_output=True)
    (out / "logic-run.stdout.bin").write_bytes(run.stdout)
    (out / "logic-run.stderr.bin").write_bytes(run.stderr)
    report = {"unityVersion": "6000.3.24f1", "unityNativeLaunched": False,
              "productionCompiles": records[:3],
              "logicTest": {"compileExitCode": records[3]["exitCode"], "runExitCode": run.returncode,
                            "stdout": str(out / "logic-run.stdout.bin"),
                            "stderr": str(out / "logic-run.stderr.bin"),
                            "text": run.stdout.decode("utf-8-sig", "replace").strip(),
                            "vector2ShimOnlyForPureLogic": str(shim)}}
    # V013-02 actual components, with only Unity scene/object operations substituted.
    if run.returncode == 0:
        target_exe = build("NarrativeTargetTests", src(
            "Assets/Scripts/LongLoop/NarrativeActorMarker.cs",
            "Assets/Scripts/LongLoop/NarrativeAnchorMarker.cs",
            "Assets/Scripts/LongLoop/NarrativeTargetRegistry.cs",
            "DevTools/LongLoop/NarrativeTargetUnityBoundary.cs",
            "DevTools/LongLoop/NarrativeTargetTests.cs"),
            [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll", ensure_narrative_core()], "exe")
        command = [str(mono), str(target_exe)]
        (out / "targets-run.preflight.json").write_text(json.dumps({
            "capturedBeforeRun": True, "command": command, "exeSha256": digest(target_exe),
            "monoSha256": digest(mono), "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        target_run = subprocess.run(command, cwd=root, capture_output=True)
        (out / "targets-run.stdout.bin").write_bytes(target_run.stdout)
        (out / "targets-run.stderr.bin").write_bytes(target_run.stderr)
        report["targetRegistryTest"] = {"compileExitCode": records[-1]["exitCode"],
            "runExitCode": target_run.returncode, "command": command,
            "startedAt": started_at, "finishedAt": datetime.now(timezone.utc).isoformat(),
            "text": target_run.stdout.decode("utf-8-sig", "replace").strip(),
            "unityBoundaryOnly": "DevTools/LongLoop/NarrativeTargetUnityBoundary.cs"}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"targetRegistryTest": report["targetRegistryTest"]}))
        if target_run.returncode:
            print(target_run.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(target_run.returncode)
    # V013-03 consumes the real shell and depth implementation, only at the default entry.
    if run.returncode == 0:
        npc_sources = src("Assets/Scripts/LongLoop/NpcShell.cs", "Assets/Scripts/LongLoop/DepthScale.cs",
            "Assets/Scripts/LongLoop/DepthScaleMath.cs", "Assets/Scripts/LongLoop/IDepthOverride.cs",
            "DevTools/LongLoop/NpcShellTests.cs", "DevTools/LongLoop/NpcShellUnityBoundary.cs")
        npc_sources += src(*("Assets/Scripts/LongLoop/Configuration/" + n + ".cs" for n in
            ("StreetConfigAsset", "StreetConfigs", "DepthScaleConfig", "CameraModeConfig", "CameraFollowConfig", "SceneBoundsConfig")))
        npc_exe = build("NpcShellTests", npc_sources,
            [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        command = [str(mono), str(npc_exe)]
        (out / "npc-run.preflight.json").write_text(json.dumps({"capturedBeforeRun": True,
            "command": command, "exeSha256": digest(npc_exe), "monoSha256": digest(mono),
            "runnerSha256": digest(Path(__file__))}, indent=2), encoding="utf-8")
        started_at = datetime.now(timezone.utc).isoformat()
        npc_run = subprocess.run(command, cwd=root, capture_output=True)
        (out / "npc-run.stdout.bin").write_bytes(npc_run.stdout)
        (out / "npc-run.stderr.bin").write_bytes(npc_run.stderr)
        report["npcShellTest"] = {"compileExitCode": records[-1]["exitCode"], "runExitCode": npc_run.returncode,
            "command": command, "startedAt": started_at, "finishedAt": datetime.now(timezone.utc).isoformat(),
            "text": npc_run.stdout.decode("utf-8-sig", "replace").strip(),
            "unityBoundaryOnly": "DevTools/LongLoop/NpcShellUnityBoundary.cs"}
        (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps({"npcShellTest": report["npcShellTest"]}))
        if npc_run.returncode:
            print(npc_run.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(npc_run.returncode)
    # V013-05 actual state application with permitted object/index fakes; original tests above unchanged.
    if run.returncode == 0:
        for stage, sources, refs in [
            ("NarrativeSceneStateTests", src("Assets/Scripts/LongLoop/NarrativeSceneStateApplication.cs", "DevTools/LongLoop/NarrativeSceneStateTests.cs"), [ensure_narrative_core()]),
            ("NarrativeSceneInactiveTargetsTests", src("DevTools/LongLoop/NarrativeSceneInactiveTargetsTests.cs"), [target_exe]),
            ("NpcShellStateRestoreTests", src("DevTools/LongLoop/NpcShellStateRestoreTests.cs"), [npc_exe])]:
            exe = build(stage, sources, [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"] + refs, "exe")
            command = [str(mono), str(exe)]
            (out / (stage + ".run.preflight.json")).write_text(json.dumps({"capturedBeforeRun": True, "command": command, "exeSha256": digest(exe), "monoSha256": digest(mono)}, indent=2), encoding="utf-8")
            started_at = datetime.now(timezone.utc).isoformat()
            child = subprocess.run(command, cwd=root, capture_output=True)
            (out / (stage + ".run.stdout.bin")).write_bytes(child.stdout)
            (out / (stage + ".run.stderr.bin")).write_bytes(child.stderr)
            report[stage] = {"compileExitCode": records[-1]["exitCode"], "runExitCode": child.returncode, "command": command, "startedAt": started_at, "finishedAt": datetime.now(timezone.utc).isoformat(), "text": child.stdout.decode("utf-8-sig", "replace").strip()}
            (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            print(json.dumps({stage: report[stage]}))
            if child.returncode:
                print(child.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
                raise SystemExit(child.returncode)
    # V013-08 actual panel/route/model behavior; only external Unity, row/text delivery, input and GUI are substituted.
    if run.returncode == 0:
        debug_sources = src(*("Assets/Scripts/LongLoop/" + n + ".cs" for n in ("NarrativeSceneDebugPanel", "NarrativeSceneStateApplier", "NarrativeSceneStateApplication", "NarrativeTargetRegistry", "NarrativeActorMarker", "NarrativeAnchorMarker", "NpcShell", "NarrativeAmbientBubbleTrigger", "NarrativeAmbientBubbleDriver")))
        debug_sources += src(*("Assets/Scripts/Narrative/Unity/" + n + ".cs" for n in ("NarrativeRuntimeEntry", "NarrativeDataAdapter", "DialoguePresentationConfig", "NarrativeBubbleView")))
        debug_sources += src(*("Assets/Scripts/DataBase/CookingData/" + n + ".cs" for n in ("CK01CharacterData", "CK01DialogueData", "CK01DialogueLineData", "CK01CGData", "CK01SceneData", "CK01SceneStateData", "CK01SceneStatePlacementData", "CK01AmbientPoolData", "CK01OptionalValues")))
        debug_sources += src("DevTools/LongLoop/NarrativeDebugUnityBoundary.cs", "DevTools/LongLoop/NarrativeSceneDebugTests.cs", "DevTools/Narrative/NarrativeAmbientBubbleTests.cs")
        debug_exe = build("NarrativeSceneDebugTests", debug_sources, [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll", ensure_narrative_core()], "exe", defines=("UNITY_EDITOR",))
        def debug_run(stage, command):
            (out / (stage + ".run.preflight.json")).write_text(json.dumps({"capturedBeforeRun": True, "command": command, "monoSha256": digest(mono)}, indent=2), encoding="utf-8")
            start = datetime.now(timezone.utc).isoformat()
            child = subprocess.run(command, cwd=root, capture_output=True)
            (out / (stage + ".run.stdout.bin")).write_bytes(child.stdout)
            (out / (stage + ".run.stderr.bin")).write_bytes(child.stderr)
            item = {"command": command, "exitCode": child.returncode, "startedAt": start, "finishedAt": datetime.now(timezone.utc).isoformat(), "text": child.stdout.decode("utf-8-sig", "replace").strip()}
            report[stage] = item
            (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            if child.returncode:
                print(child.stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
                print(child.stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
                raise SystemExit(child.returncode)
            return item
        print(json.dumps({"debugRoutes": debug_run("debug-routes", [str(mono), str(debug_exe)])}))
        # Compile the current full LongLoop sources under actual Release/Development symbols.
        # Existing adjacent dependency APIs are unchanged; Core's conditional API is rebuilt with matching identity/symbols.
        metadata_exe = build("NarrativeDebugReleaseTests", src("DevTools/LongLoop/NarrativeDebugReleaseTests.cs"), [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], "exe")
        editor_record = next(r for r in records if r["stage"] == "LongLoop")
        editor_input = Path(editor_record["command"][-1][1:])
        editor_lines = editor_input.read_text(encoding="utf-8-sig").splitlines()
        current_core = ensure_narrative_core()
        configurations = []
        for mode, symbols in [("Release", ()), ("Development", ("DEVELOPMENT_BUILD",)), ("Editor", ("UNITY_EDITOR",))]:
            folder = out / ("debug-native-" + mode.lower()); folder.mkdir(exist_ok=False)
            core = build("DebugCore" + mode, sorted((root / "Assets/Scripts/Narrative/Core").rglob("*.cs")), [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"], defines=symbols, assembly_path=folder / "Narrative.Core.dll")
            output = folder / "LongLoop.dll"; lines = []
            for line in editor_lines:
                if line.startswith(("-refout:", "/refout:")): continue
                if line.startswith(("-out:", "/out:")): line = '-out:"' + str(output) + '"'
                if line.startswith(("-define:", "/define:")):
                    values = [v for v in line.split(":", 1)[1].split(";") if not v.startswith("UNITY_EDITOR") and v != "DEVELOPMENT_BUILD"]
                    if not values: continue
                    line = "-define:" + ";".join(values)
                line = line.replace(str(current_core), str(core)).replace(current_core.as_posix(), core.as_posix())
                lines.append(line)
            if symbols: lines.append("-define:" + ";".join(symbols))
            rsp = folder / "LongLoop.rsp"; rsp.write_text("\n".join(lines) + "\n", encoding="utf-8")
            command = [str(mono), str(csc), "@" + str(rsp)]
            (folder / "preflight.json").write_text(json.dumps({"capturedBeforeCompile": True, "mode": mode, "sourceRsp": str(editor_input), "sourceRspSha256": digest(editor_input), "derivedRspSha256": digest(rsp), "matchingCore": str(core), "matchingCoreSha256": digest(core), "command": command}, indent=2), encoding="utf-8")
            start = datetime.now(timezone.utc).isoformat(); child = subprocess.run(command, cwd=root, capture_output=True)
            (folder / "compile.stdout.bin").write_bytes(child.stdout); (folder / "compile.stderr.bin").write_bytes(child.stderr)
            (folder / "compile-result.json").write_text(json.dumps({"command": command, "exitCode": child.returncode, "startedAt": start, "finishedAt": datetime.now(timezone.utc).isoformat()}, indent=2), encoding="utf-8")
            if child.returncode:
                print(child.stdout.decode("utf-8-sig", "replace"), file=sys.stderr); print(child.stderr.decode("utf-8-sig", "replace"), file=sys.stderr); raise SystemExit(child.returncode)
            configurations.append((mode, output, core))
        for mode, loop, core in configurations:
            print(json.dumps({"debug" + mode: debug_run("debug-compile-" + mode.lower(), [str(mono), str(metadata_exe), mode, str(loop), str(core), str(out), str(sa), str(ue)])}))
    (out / "offline-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"productionExitCodes": [r["exitCode"] for r in records[:3]],
                      "logicRunExitCode": run.returncode, "logic": report["logicTest"]["text"]}))
    if run.returncode: raise SystemExit(run.returncode)


if __name__ == "__main__":
    main()
