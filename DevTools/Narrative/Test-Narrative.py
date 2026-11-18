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
            outputs['Narrative.Core'], toolkit, generated]
        lines = ['-nologo', '-nostdlib+', '-langversion:9.0', '-target:library', '-out:"' + str(out / (name + '.dll')) + '"']
        lines += ['-r:"' + str(p) + '"' for p in refs]
        lines += ['"' + p.relative_to(root).as_posix() + '"' for p in sorted(directory.rglob('*.cs'))]
        outputs[name] = compile_assembly(name, lines)
    compile_narrative('Core')
    toolkit = compile_bee('Toolkit', {'Narrative.Core': outputs['Narrative.Core']},
        [root / 'Assets/Scripts/Tools/NarrativeCurveBridge.cs'])
    short_core = compile_bee('ShortCycle.Core', {})
    new_rows = [root / 'Assets/Scripts/DataBase/CookingData' / (name + '.cs') for name in (
        'CK01CharacterData', 'CK01DialogueData', 'CK01DialogueLineData', 'CK01CGData',
        'CK01SceneData', 'CK01SceneStateData', 'CK01SceneStatePlacementData', 'CK01AmbientPoolData', 'CK01OptionalValues')]
    generated = compile_bee('Generated.DataTables', {'Toolkit':toolkit, 'ShortCycle.Core':short_core}, new_rows)
    compile_bee('Editor.DataTables', {'Generated.DataTables':generated, 'Toolkit':toolkit, 'Narrative.Core':outputs['Narrative.Core']},
        [root / 'Assets/Editor/DataTables/NarrativeTableValidation.cs'],
        original=root / 'Library/Bee/artifacts/1900b0aE.dag/Editor.DataTables.rsp')
    compile_narrative('Unity')
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
        '"Assets/Scripts/Tools/NarrativeCurveBridge.cs"']
    compile_assembly('NarrativeCommandTests', command_lines)
    fixture = root / 'DevTools/Narrative/Fixtures/CommandSignatures.v2_5.tsv'
    command_text = run_stage('commands', [str(mono), str(command_sample), str(fixture)], dict(
        exe=str(command_sample), exeSha256=digest(command_sample),
        independentFixture=str(fixture), independentFixtureSha256=digest(fixture),
        presentationFixture=str(fixture.parent / 'PresentationDefaults.v2_5.tsv'),
        presentationFixtureSha256=digest(fixture.parent / 'PresentationDefaults.v2_5.tsv')))
    report = dict(passed=True, unityNativeLaunched=False, substitutesUsed=False,
        assemblyDefinitions=assembly_definitions, outputs={name:dict(path=str(p), sha256=digest(p)) for name,p in outputs.items()},
        stages=records, sampleText=text, commandTestText=command_text, independentCommandFixture=str(fixture),
        independentCommandFixtureSha256=digest(fixture), existingGeneratedTransitiveDependency='ShortCycle.Core (unchanged)')
    write_json(out / 'offline-validation.json', report)
    print(json.dumps(dict(passed=True, compiledAssemblies=list(outputs), example=text, report=str(out / 'offline-validation.json'))))


if __name__ == '__main__':
    main()
