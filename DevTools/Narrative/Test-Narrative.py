"""Narrative assembly compile/sample entry; uses installed real Mono/Unity references.
Bee dependency compilation follows DevTools/LongLoop/Test-LongLoopPlayer.py.
Never launches Unity. No substitutes are used for these assembly compilations.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--unity-editor", required=True)
    parser.add_argument("--compile-only", action="store_true", help="Compile current production dependencies without running Narrative fixtures.")
    parser.add_argument("--interaction-prompt", action="store_true", help="Compile production and run only the Toolkit prompt state/device/localization group.")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    # Reuse the existing source evidence/output boundary implementation.
    sys.path.insert(0, str(root / "DevTools/LongLoop"))
    from evidence_closure import ClosureEvidence, output_path
    out = output_path(root, args.output, fresh=True)
    out.mkdir(parents=True)
    closure = ClosureEvidence(root, out / "closure-evidence")
    editor = Path(args.unity_editor)
    fx = editor / "Data/MonoBleedingEdge/lib/mono/4.5"
    mono = editor / "Data/MonoBleedingEdge/bin/mono.exe"
    csc = fx / "csc.exe"
    bee = root / "Library/Bee/artifacts/1900b0aP.dag"
    bcl = [fx / "mscorlib.dll", fx / "System.dll", fx / "System.Core.dll"]
    json_refs = list((root / 'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll'))
    if len(json_refs) != 1: raise ValueError('Expected existing Newtonsoft package')
    json_reference = json_refs[0]
    records = []

    def write_json(path, value):
        path.write_text(json.dumps(value, indent=2), encoding="utf-8")

    def run_stage(name, command, inputs):
        preflight = out / (name + ".preflight.json")
        write_json(preflight, dict(command=command, capturedBeforeExecution=True,
            runnerSha256=digest(Path(__file__)), monoSha256=digest(mono), compilerSha256=digest(csc), **inputs))
        started = datetime.now(timezone.utc).isoformat()
        process = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = process.communicate()
        stdout_path, stderr_path = out / (name + ".stdout.bin"), out / (name + ".stderr.bin")
        stdout_path.write_bytes(stdout)
        stderr_path.write_bytes(stderr)
        record = dict(stage=name, command=command, preflight=str(preflight), preflightSha256=digest(preflight),
            startedAt=started, finishedAt=datetime.now(timezone.utc).isoformat(), pid=process.pid,
            exitCode=process.returncode, stdout=str(stdout_path), stderr=str(stderr_path))
        records.append(record)
        write_json(out / "stages.json", records)
        if process.returncode:
            print(stdout.decode("utf-8-sig", "replace"), file=sys.stderr)
            print(stderr.decode("utf-8-sig", "replace"), file=sys.stderr)
            raise SystemExit(process.returncode)
        return stdout.decode("utf-8-sig", "replace").strip()

    def compile_assembly(name, lines, original=None):
        sources = [root / line.strip('"') for line in lines if line.startswith('"') and line.endswith('.cs"')]
        refs = [Path(line[4:-1]) for line in lines if line.startswith('-r:"')]
        extras = [Path(line.split(':"', 1)[1][:-1]) for line in lines
                  if line.startswith(('-analyzer:', '-additionalfile:', '-analyzerconfig:'))]
        def actual(p):
            return p if p.is_absolute() else root / p
        source_records = closure.capture(name, [dict(path=str(p), repoPath=p.relative_to(root).as_posix(),
            restorePath=name + '/' + p.relative_to(root).as_posix()) for p in sources])
        rsp = out / (name + ".rsp")
        rsp.write_text("\n".join(lines) + "\n", encoding="utf-8")
        run_stage(name, [str(mono), str(csc), '@' + str(rsp)], dict(
            rsp=str(rsp), rspSha256=digest(rsp), originalRsp=original,
            sources=source_records, sourceClosureManifest=str(out / 'closure-evidence/manifest.json'),
            references=[dict(path=str(actual(p)), sha256=digest(actual(p))) for p in refs],
            analyzerAndAdditionalInputs=[dict(path=str(actual(p)), sha256=digest(actual(p))) for p in extras]))
        return out / (name + '.dll')

    def compile_bee(name, replacements, extra_sources=(), original=None):
        original = original or bee / (name + '.rsp')
        snapshot = out / (name + '.original.rsp')
        snapshot.write_bytes(original.read_bytes())
        lines = snapshot.read_text(encoding='utf-8-sig').splitlines()
        for i, line in enumerate(lines):
            if line.startswith('-out:'):
                lines[i] = '-out:"' + str(out / (name + '.dll')) + '"'
            elif line.startswith('-refout:'):
                lines[i] = '-refout:"' + str(out / (name + '.ref.dll')) + '"'
            elif line.startswith('-r:"'):
                path = Path(line[4:-1])
                for dependency, fresh in replacements.items():
                    if (root / path).resolve() == (original.parent / (dependency + '.ref.dll')).resolve():
                        lines[i] = '-r:"' + fresh.as_posix() + '"'
        for dependency, fresh in replacements.items():
            if name in ('Toolkit', 'Editor.DataTables') and dependency == 'Narrative.Core' and not any(
                    Path(line[4:-1]).stem in ('Narrative.Core', 'Narrative.Core.ref')
                    for line in lines if line.startswith('-r:"')):
                lines.append('-r:"' + fresh.as_posix() + '"')
            if lines.count('-r:"' + fresh.as_posix() + '"') != 1:
                raise ValueError('Expected exactly one fresh dependency reference')
        for source in extra_sources:
            line = '"' + source.relative_to(root).as_posix() + '"'
            if line not in lines:
                lines.append(line)
        return compile_assembly(name, lines, dict(path=str(original), sha256=digest(original), snapshot=str(snapshot)))

    assembly_definitions = {}
    outputs = {}
    def compile_narrative(layer):
        name = 'Narrative.' + layer
        directory = root / 'Assets/Scripts/Narrative' / layer
        definition = json.loads((directory / (name + '.asmdef')).read_bytes())
        assembly_definitions[name] = definition
        refs = bcl if layer == 'Core' else bcl + [fx / 'Facades/netstandard.dll',
            editor / 'Data/Managed/UnityEngine/UnityEngine.CoreModule.dll',
            editor / 'Data/Managed/UnityEngine/UnityEngine.JSONSerializeModule.dll',
            editor / 'Data/Managed/UnityEngine/UnityEngine.UIModule.dll',
            root / 'Library/ScriptAssemblies/Unity.TextMeshPro.dll', root / 'Library/ScriptAssemblies/UnityEngine.UI.dll',
            outputs['Narrative.Core'], toolkit, generated, json_reference]
        lines = ['-nologo', '-nostdlib+', '-langversion:9.0', '-target:library', '-out:"' + str(out / (name + '.dll')) + '"']
        lines += ['-r:"' + str(p) + '"' for p in refs]
        lines += ['"' + p.relative_to(root).as_posix() + '"' for p in sorted(directory.rglob('*.cs'))]
        outputs[name] = compile_assembly(name, lines)
    compile_narrative('Core')
    toolkit = compile_bee('Toolkit', {'Narrative.Core': outputs['Narrative.Core']},
        [root / 'Assets/Scripts/Tools/NarrativeCurveBridge.cs', root / 'Assets/Scripts/Tools/InteractionPrompt.cs',
         root / 'Assets/Scripts/Tools/InteractionPromptState.cs'])
    short_core = compile_bee('ShortCycle.Core', {})
    new_rows = [root / 'Assets/Scripts/DataBase/CookingData' / (name + '.cs') for name in (
        'CK01CharacterData', 'CK01DialogueData', 'CK01DialogueLineData', 'CK01CGData',
        'CK01SceneData', 'CK01SceneStateData', 'CK01SceneStatePlacementData', 'CK01AmbientPoolData', 'CK01OptionalValues')]
    generated = compile_bee('Generated.DataTables', {'Toolkit':toolkit, 'ShortCycle.Core':short_core}, new_rows)
    compile_bee('Editor.DataTables', {'Generated.DataTables':generated, 'Toolkit':toolkit, 'Narrative.Core':outputs['Narrative.Core']},
        [root / 'Assets/Editor/DataTables/NarrativeTableValidation.cs'],
        original=root / 'Library/Bee/artifacts/1900b0aE.dag/Editor.DataTables.rsp')
    compile_narrative('Unity')
    if args.interaction_prompt:
        sample = out / 'InteractionPromptLogicTests.exe'
        lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(sample) + '"']
        lines += ['-r:"' + str(p) + '"' for p in bcl]
        lines += ['"' + p + '"' for p in (
            'Assets/Scripts/Tools/InteractionPromptState.cs', 'Assets/Scripts/Tools/Input/IGameInput.cs',
            'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs', 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
            'DevTools/Narrative/InteractionPromptUnityBoundary.cs', 'DevTools/Narrative/InteractionPromptLogicTests.cs')]
        compile_assembly('InteractionPromptLogicTests', lines)
        result = run_stage('interaction-prompt', [str(mono), str(sample)], dict(exe=str(sample), exeSha256=digest(sample),
            boundary='external Vector2/ScriptableObject/log handles only; actual state/IGameInput enum/LocService/Catalog'))
        report = dict(passed=True, interactionPromptOnly=True, unityNativeLaunched=False, substitutesUsedForProductionCompile=False,
            stages=records, promptTestText=result, boundary='state/device/localization only; no rendered UI or hardware execution',
            assemblyDefinitions=assembly_definitions, outputs={name:dict(path=str(p), sha256=digest(p)) for name,p in outputs.items()})
        write_json(out / 'offline-validation.json', report)
        print(json.dumps(dict(passed=True, interactionPromptOnly=True, report=str(out / 'offline-validation.json'))))
        return
    if args.compile_only:
        report = dict(passed=True, compileOnly=True, unityNativeLaunched=False, substitutesUsed=False,
            assemblyDefinitions=assembly_definitions, outputs={name:dict(path=str(p), sha256=digest(p)) for name,p in outputs.items()}, stages=records)
        write_json(out / 'offline-validation.json', report)
        print(json.dumps(dict(passed=True, compileOnly=True, stages=len(records), report=str(out / 'offline-validation.json'))))
        return
    sample = out / 'NarrativeAssemblyTests.exe'
    lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(sample) + '"']
    lines += ['-r:"' + str(p) + '"' for p in bcl + list(outputs.values())]
    lines += ['"DevTools/Narrative/NarrativeAssemblyTests.cs"']
    compile_assembly('NarrativeAssemblyTests', lines)
    text = run_stage('example', [str(mono), str(sample)], dict(exe=str(sample), exeSha256=digest(sample)))
    command_sample = out / 'NarrativeCommandTests.exe'
    command_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(command_sample) + '"']
    command_lines += ['-r:"' + str(p) + '"' for p in bcl + [outputs['Narrative.Core']]]
    command_lines += ['"DevTools/Narrative/NarrativeCommandTests.cs"', '"DevTools/Narrative/NarrativeAdvanceInputTests.cs"',
        '"DevTools/Narrative/NarrativeCurveTests.cs"', '"DevTools/Narrative/Fixtures/StreetCurveOracle.cs"',
        '"DevTools/Narrative/NarrativeCommandSchedulerTests.cs"',
        '"DevTools/Narrative/NarrativePresentationConfigTests.cs"',
        '"DevTools/Narrative/NarrativeOverrideTests.cs"',
        '"DevTools/Narrative/NarrativeDataDirectoryTests.cs"',
        '"DevTools/Narrative/DialoguePresentationResolverTests.cs"',
        '"DevTools/Narrative/NarrativeDialogueStateMachineTests.cs"',
        '"DevTools/Narrative/NarrativeTypewriterTests.cs"',
        '"DevTools/Narrative/NarrativeStepSequenceTests.cs"',
        '"DevTools/Narrative/NarrativeInteractionRulesTests.cs"',
        '"DevTools/Narrative/NarrativeTalkedRecordTests.cs"',
        '"DevTools/Narrative/NarrativePortraitLayoutTests.cs"',
        '"DevTools/Narrative/NarrativeSpeakerHighlightTests.cs"',
        '"DevTools/Narrative/NarrativeStreetReactionTests.cs"',
        '"Assets/Scripts/Tools/NarrativeCurveBridge.cs"']
    compile_assembly('NarrativeCommandTests', command_lines)
    fixture = root / 'DevTools/Narrative/Fixtures/CommandSignatures.v2_5.tsv'
    command_text = run_stage('commands', [str(mono), str(command_sample), str(fixture)], dict(
        exe=str(command_sample), exeSha256=digest(command_sample),
        independentFixture=str(fixture), independentFixtureSha256=digest(fixture),
        presentationFixture=str(fixture.parent / 'PresentationDefaults.v2_5.tsv'),
        presentationFixtureSha256=digest(fixture.parent / 'PresentationDefaults.v2_5.tsv')))
    # V014-05 appended group: actual components with external engine handles only.
    layers_sample = out / 'NarrativeDialogueLayersTests.exe'
    layers_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(layers_sample) + '"']
    layers_lines += ['-r:"' + str(p) + '"' for p in bcl + [outputs['Narrative.Core']]]
    layers_lines += ['"Assets/Scripts/Narrative/Unity/NarrativeDialogueCanvas.cs"',
        '"Assets/Scripts/Narrative/Unity/NarrativeSceneDimmer.cs"',
        '"Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs"',
        '"DevTools/Narrative/NarrativeDialogueLayersUnityBoundary.cs"',
        '"DevTools/Narrative/NarrativeDialogueLayersTests.cs"']
    compile_assembly('NarrativeDialogueLayersTests', layers_lines)
    layers_text = run_stage('dialogue-layers', [str(mono), str(layers_sample)],
        dict(exe=str(layers_sample), exeSha256=digest(layers_sample), nativeUnity=False,
            fixtureBoundary='external engine hierarchy/UI/TMP handles only; actual components and resolver'))
    # V014-06: actual presenter/Core/FSM/LocService; recorder deliberately has no text layout implementation.
    box_sample = out / 'NarrativeDialogueBoxTests.exe'
    box_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(box_sample) + '"']
    box_lines += ['-r:"' + str(p) + '"' for p in bcl + [outputs['Narrative.Core']]]
    box_lines += ['"' + p + '"' for p in (
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxPresenter.cs',
        'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs', 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
        'DevTools/Narrative/InteractionPromptUnityBoundary.cs', 'DevTools/Narrative/NarrativeDialogueBoxTests.cs')]
    compile_assembly('NarrativeDialogueBoxTests', box_lines)
    box_text = run_stage('dialogue-box', [str(mono), str(box_sample)],
        dict(exe=str(box_sample), exeSha256=digest(box_sample), nativeUnity=False,
            fixtureBoundary='output recorder only; actual presenter/Core/FSM/localization; no glyph or line layout'))
    # V014-07: real FSM/Sequence/Typewriter; fake box geometry and dim request handles only.
    entrance_sample = out / 'NarrativeDialogueEntranceTests.exe'
    entrance_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(entrance_sample) + '"']
    entrance_lines += ['-r:"' + str(p) + '"' for p in bcl + [outputs['Narrative.Core']]]
    entrance_lines += ['"' + p + '"' for p in (
        'Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxPresenter.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueEntrance.cs',
        'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs', 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
        'DevTools/Narrative/InteractionPromptUnityBoundary.cs', 'DevTools/Narrative/NarrativeDialogueEntranceTests.cs')]
    compile_assembly('NarrativeDialogueEntranceTests', entrance_lines)
    entrance_text = run_stage('dialogue-entrance', [str(mono), str(entrance_sample)],
        dict(exe=str(entrance_sample), exeSha256=digest(entrance_sample), nativeUnity=False,
            fixtureBoundary='actual FSM/StepSequence/BoxPresenter/Typewriter; fake box geometry/dim requests and external view/clock/lock only'))
    # V014-08: real FSM/Sequence/Dimmer and 07/06 presentation; external engine/UI handles only.
    exit_sample = out / 'NarrativeDialogueExitTests.exe'
    exit_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(exit_sample) + '"']
    exit_lines += ['-r:"' + str(p) + '"' for p in bcl + [outputs['Narrative.Core']]]
    exit_lines += ['"' + p + '"' for p in (
        'Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueCanvas.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeSceneDimmer.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxPresenter.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueEntrance.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueExit.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueContinuationPolicy.cs',
        'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs', 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
        'DevTools/Narrative/NarrativeDialogueLayersUnityBoundary.cs', 'DevTools/Narrative/NarrativeDialogueExitTests.cs')]
    compile_assembly('NarrativeDialogueExitTests', exit_lines)
    exit_text = run_stage('dialogue-exit', [str(mono), str(exit_sample)],
        dict(exe=str(exit_sample), exeSha256=digest(exit_sample), nativeUnity=False,
            fixtureBoundary='actual FSM/Sequence/Dimmer/Entrance/Presenter; external Unity/UI handles, fake box geometry/view/clock/lock only'))
    # V014-01 persistence: compile actual production factory separately for each real build branch.
    persistence_results = []
    persistence_root = out.parent / 'p' / out.name
    if persistence_root.exists(): raise ValueError('Fresh short persistence data root required; retain previous evidence')
    persistence_sources = sorted((root / 'Assets/Scripts/Tools/Security').glob('*.cs')) + sorted((root / 'Assets/Scripts/Tools/Persistence').glob('*.cs'))
    persistence_sources += [root / name for name in ('Assets/Scripts/Narrative/Unity/NarrativeTalkedPersistence.cs',
        'DevTools/Narrative/InteractionPromptUnityBoundary.cs', 'DevTools/Narrative/NarrativeTalkedPersistenceTests.cs')]
    for mode in ('release', 'UNITY_EDITOR', 'DEVELOPMENT_BUILD'):
        sample = out / ('NarrativeTalkedPersistence-' + mode + '.exe')
        lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(sample) + '"']
        if mode != 'release': lines.append('-define:' + mode)
        lines += ['-r:"' + str(p) + '"' for p in bcl + [fx / 'Facades/netstandard.dll', outputs['Narrative.Core'], json_reference]]
        lines += ['"' + p.relative_to(root).as_posix() + '"' for p in persistence_sources]
        compile_assembly('NarrativeTalkedPersistence-' + mode, lines)
        environment = __import__('os').environ
        old_mono = environment.get('MONO_PATH')
        environment['MONO_PATH'] = str(json_reference.parent)
        try:
            result = run_stage('talked-persistence-' + mode, [str(mono), str(sample), str(persistence_root / mode)],
                dict(exe=str(sample), exeSha256=digest(sample), actualFileIO=True, actualFactory=True,
                    defines=[] if mode == 'release' else [mode], realUserSaveAccess=False))
        finally:
            if old_mono is None: environment.pop('MONO_PATH', None)
            else: environment['MONO_PATH'] = old_mono
        persistence_results.append(json.loads(next(line.split('TALKED_PERSISTENCE_RESULT ', 1)[1] for line in result.splitlines() if line.startswith('TALKED_PERSISTENCE_RESULT '))))
    # V014-09: actual service session, FSM/scheduler/input/07/08/01; fake geometry and injected lock only.
    service_sample = out / 'NarrativeDialogueServiceTests.exe'
    service_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(service_sample) + '"']
    service_lines += ['-r:"' + str(p) + '"' for p in bcl + [fx / 'Facades/netstandard.dll', outputs['Narrative.Core'], toolkit, json_reference]]
    service_lines += ['"' + name + '"' for name in (
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueSession.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeTalkedPersistence.cs',
        'Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueCanvas.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeSceneDimmer.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxPresenter.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueEntrance.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueExit.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueContinuationPolicy.cs',
        'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs', 'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
        'DevTools/Narrative/NarrativeDialogueLayersUnityBoundary.cs', 'DevTools/Narrative/NarrativeDialogueServiceTests.cs')]
    compile_assembly('NarrativeDialogueServiceTests', service_lines)
    environment = __import__('os').environ
    old_mono = environment.get('MONO_PATH')
    environment['MONO_PATH'] = str(json_reference.parent)
    try:
        service_text = run_stage('dialogue-service', [str(mono), str(service_sample), str(out / 'service-files')],
            dict(exe=str(service_sample), exeSha256=digest(service_sample), actualFileIO=True, realUserSaveAccess=False,
                fixtureBoundary='actual session/FSM/scheduler/input/07/08/01 and Toolkit DLL; fake box geometry/injected lock/runtime-source handles only'))
    finally:
        if old_mono is None: environment.pop('MONO_PATH', None)
        else: environment['MONO_PATH'] = old_mono
    marker_sample = out / 'NarrativeHeadMarkerViewTests.exe'
    marker_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(marker_sample) + '"']
    marker_lines += ['-r:"' + str(p) + '"' for p in bcl]
    marker_lines += ['"' + p + '"' for p in ('Assets/Scripts/Narrative/Unity/NarrativeHeadMarkerView.cs',
        'DevTools/Narrative/NarrativeHeadMarkerUnityBoundary.cs', 'DevTools/Narrative/NarrativeHeadMarkerViewTests.cs')]
    compile_assembly('NarrativeHeadMarkerViewTests', marker_lines)
    marker_text = run_stage('head-marker-view', [str(mono), str(marker_sample)],
        dict(exe=str(marker_sample), exeSha256=digest(marker_sample), nativeUnity=False,
            fixtureBoundary='actual drawing component; external hierarchy/UI/TMP/font/texture/sorting handles only'))
    prefab_sample = out / 'NarrativeDialoguePrefabTests.exe'
    prefab_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(prefab_sample) + '"']
    prefab_lines += ['-r:"' + str(p) + '"' for p in bcl + [fx / 'Facades/netstandard.dll', outputs['Narrative.Core'], toolkit, json_reference]]
    prefab_lines += ['"' + p + '"' for p in (
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueCanvas.cs', 'Assets/Scripts/Narrative/Unity/NarrativeSceneDimmer.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxView.cs', 'Assets/Scripts/Narrative/Unity/NarrativeDialogueBoxPresenter.cs',
        'Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs', 'Assets/Scripts/Narrative/Unity/NarrativeDialogueEntrance.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueExit.cs', 'Assets/Scripts/Narrative/Unity/NarrativeDialogueContinuationPolicy.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueComposition.cs', 'Assets/Scripts/Narrative/Unity/NarrativeDialogueSession.cs',
        'Assets/Scripts/Narrative/Unity/NarrativeDialogueService.cs', 'Assets/Scripts/Narrative/Unity/NarrativeTalkedPersistence.cs',
        'Assets/Scripts/Tools/Input/IGameInput.cs', 'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs',
        'Assets/Scripts/DataBase/Localization/LocTableAsset.cs', 'DevTools/Narrative/NarrativeDialogueLayersUnityBoundary.cs',
        'DevTools/Narrative/NarrativePrefabServiceBoundary.cs', 'DevTools/Narrative/NarrativeDialoguePrefabTests.cs',
        'DevTools/Narrative/NarrativeNameBoxSideTests.cs', 'DevTools/Narrative/NarrativeDialogueNotificationTests.cs')]
    compile_assembly('NarrativeDialoguePrefabTests', prefab_lines)
    old_mono = environment.get('MONO_PATH'); environment['MONO_PATH'] = str(json_reference.parent)
    try:
        prefab_text = run_stage('dialogue-prefab', [str(mono), str(prefab_sample), str(out.parent / 'p' / (out.name + '-prefab'))],
            dict(exe=str(prefab_sample), exeSha256=digest(prefab_sample), nativeUnity=False, realTmpLayoutVerified=False,
                fixtureBoundary='actual Canvas/Box/Service/Session/07/08/composition/01; external hierarchy/UI event transport/TMP handles and Runtime directory delivery only'))
    finally:
        if old_mono is None: environment.pop('MONO_PATH', None)
        else: environment['MONO_PATH'] = old_mono
    # V015-05 real FSM/session/sequences/layout/highlight with external stage/box/dim call transport.
    portrait_sequence = out / 'NarrativePortraitSequenceTests.exe'
    sequence_lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(portrait_sequence) + '"']
    sequence_lines += ['-r:"' + str(p) + '"' for p in bcl + [fx / 'Facades/netstandard.dll', outputs['Narrative.Core'], toolkit, json_reference, prefab_sample]]
    sequence_lines += ['"DevTools/Narrative/NarrativePortraitSequenceTests.cs"']
    compile_assembly('NarrativePortraitSequenceTests', sequence_lines)
    old_mono = environment.get('MONO_PATH'); environment['MONO_PATH'] = str(json_reference.parent)
    try:
        portrait_sequence_text = run_stage('portrait-sequence', [str(mono), str(portrait_sequence), str(out.parent / 'p' / (out.name + '-portrait-sequence'))],
            dict(exe=str(portrait_sequence), exeSha256=digest(portrait_sequence), nativeUnity=False,
                fixtureBoundary='actual FSM/session/entrance/exit/sequence/layout/highlight; external stage/box/dim calls and image handles only'))
    finally:
        if old_mono is None: environment.pop('MONO_PATH', None)
        else: environment['MONO_PATH'] = old_mono
    portrait_stage_results = []
    old_mono = environment.get('MONO_PATH'); environment['MONO_PATH'] = str(json_reference.parent)
    try:
        for mode in ('release', 'UNITY_EDITOR', 'DEVELOPMENT_BUILD'):
            sample = out / ('NarrativePortraitStage-' + mode + '.exe')
            lines = ['-nologo', '-nostdlib+', '-target:exe', '-out:"' + str(sample) + '"']
            if mode != 'release': lines += ['-define:' + mode]
            lines += ['-r:"' + str(p) + '"' for p in bcl + [fx / 'Facades/netstandard.dll', outputs['Narrative.Core'], json_reference]]
            lines += ['"' + p + '"' for p in (
                'Assets/Scripts/Narrative/Unity/NarrativePortraitStage.cs',
                'Assets/Scripts/Narrative/Unity/NarrativePresentationSurfaces.cs',
                'DevTools/Narrative/NarrativePortraitStageUnityBoundary.cs',
                'DevTools/Narrative/NarrativePortraitStageTests.cs')]
            compile_assembly('NarrativePortraitStage-' + mode, lines)
            result = run_stage('portrait-stage-' + mode, [str(mono), str(sample)],
                dict(exe=str(sample), exeSha256=digest(sample), nativeUnity=False, buildCondition=mode,
                    fixtureBoundary='actual stage plus original layout/highlight models; external hierarchy/clone/destroy and supplied Sprite rectangles only'))
            portrait_stage_results.append(dict(buildCondition=mode, result=json.loads(result)))
    finally:
        if old_mono is None: environment.pop('MONO_PATH', None)
        else: environment['MONO_PATH'] = old_mono
    report = dict(passed=True, unityNativeLaunched=False, substitutesUsed=False,
        portraitStageResults=portrait_stage_results, portraitSequenceTestText=portrait_sequence_text,
        assemblyDefinitions=assembly_definitions, outputs={name:dict(path=str(p), sha256=digest(p)) for name,p in outputs.items()},
        stages=records, sampleText=text, commandTestText=command_text, independentCommandFixture=str(fixture),
        independentCommandFixtureSha256=digest(fixture), existingGeneratedTransitiveDependency='ShortCycle.Core (unchanged)',
        dialogueServiceTestText=service_text,
        dialoguePrefabTestText=prefab_text,
        headMarkerViewTestText=marker_text,
        talkedPersistenceResults=persistence_results,
        dialogueExitTestText=exit_text,
        dialogueEntranceTestText=entrance_text,
        dialogueBoxTestText=box_text,
        dialogueBoxRealTmpLineInfoVerified=False,
        dialogueLayersTestText=layers_text,
        dialogueLayersFixtureBoundary='External engine hierarchy/UI/TMP handles only; no native rendering claim. Production assembly compilation above uses real Unity DLLs.')
    write_json(out / 'offline-validation.json', report)
    print(json.dumps(dict(passed=True, compiledAssemblies=list(outputs), example=text, report=str(out / 'offline-validation.json'))))


if __name__ == '__main__':
    main()
