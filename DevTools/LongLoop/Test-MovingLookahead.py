"""Focused product regression. Never invokes Unity or the frozen FOLLOW-R probes."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import subprocess

from evidence_closure import ClosureEvidence


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', required=True)
    ap.add_argument('--counter', action='store_true')
    ap.add_argument('--deadzone-counter', action='store_true')
    ap.add_argument('--motion-only', action='store_true')
    args = ap.parse_args()
    root = Path(__file__).resolve().parents[2]
    out = Path(args.out).resolve()
    out.relative_to(root / '.ai-workspace/outputs')
    out.mkdir(parents=True, exist_ok=False)
    editor = Path('C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor')
    mono = editor / 'Data/MonoBleedingEdge/bin/mono.exe'
    csc = editor / 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
    fx = csc.parent
    cm = root / 'Library/PackageCache/com.unity.cinemachine@f3f96bcb59af'
    assert json.loads((cm / 'package.json').read_text())['version'] == '3.1.7'
    closure = ClosureEvidence(root, out / 'closure')
    records = []

    def sha(p):
        return hashlib.sha256(p.read_bytes()).hexdigest()

    def execute(name, command, inputs, refs=()):
        refs = list(refs)+[p for p in inputs if not p.is_relative_to(root)]
        inputs = [p for p in inputs if p.is_relative_to(root)]
        captured = closure.capture(name, [dict(path=str(p), repoPath=p.relative_to(root).as_posix(),
            restorePath=name+'/'+p.relative_to(root).as_posix()) for p in inputs])
        pre = dict(command=command, capturedBeforeExecution=True, sources=captured,
            monoSha256=sha(mono), compilerSha256=sha(csc),
            references=[dict(path=str(p), sha256=sha(p)) for p in refs])
        (out / (name+'.preflight.json')).write_text(json.dumps(pre, indent=2), encoding='utf-8')
        start = datetime.now(timezone.utc).isoformat()
        p = subprocess.run(command, cwd=root, capture_output=True)
        (out / (name+'.stdout.txt')).write_bytes(p.stdout)
        (out / (name+'.stderr.txt')).write_bytes(p.stderr)
        records.append(dict(stage=name, exitCode=p.returncode, started=start,
            finished=datetime.now(timezone.utc).isoformat(), command=command))
        (out / 'runs.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
        print(name, 'exit', p.returncode, flush=True)
        if p.returncode:
            print(p.stdout.decode('utf-8-sig', 'replace'), p.stderr.decode('utf-8-sig', 'replace'))
            raise SystemExit(p.returncode)

    runner = [Path(__file__).resolve(), Path(__file__).with_name('evidence_closure.py')]
    if not (args.counter or args.deadzone_counter):
        original = root / 'Library/Bee/artifacts/1900b0aE.dag/LongLoop.rsp'
        lines = original.read_text(encoding='utf-8-sig').splitlines()
        sources = [root / line[1:-1] for line in lines if line.startswith('"') and line.endswith('.cs"')]
        # Bee can lag behind newly added source files; compile the current assembly closure.
        current = sorted((root / 'Assets/Scripts/LongLoop').rglob('*.cs'))
        for p in current:
            if p not in sources:
                lines.append('"'+p.relative_to(root).as_posix()+'"')
                sources.append(p)
        assert all(p.is_file() for p in sources), 'stale removed Bee source'
        refs = [root / line[4:-1] for line in lines if line.startswith('-r:"')]
        lines = [('-out:"'+str(out/'LongLoop.dll')+'"') if x.startswith('-out:') else
                 ('-refout:"'+str(out/'LongLoop.ref.dll')+'"') if x.startswith('-refout:') else x for x in lines]
        rsp = out / 'LongLoop.rsp'
        rsp.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        extras = [root / x.split(':"', 1)[1][:-1] for x in lines
                  if x.startswith(('-analyzer:"', '-additionalfile:"', '-analyzerconfig:"'))]
        execute('production-compile', [str(mono), str(csc), '@'+str(rsp)],
                sources+runner+[original, rsp]+extras, refs)

    def method(text, marker):
        start = text.index(marker)
        end = text.index('{', start)+1
        depth = 1
        while depth:
            depth += (text[end] == '{')-(text[end] == '}')
            end += 1
        return text[start:end]

    vcam = cm / 'Runtime/Core/CinemachineVirtualCameraBase.cs'
    confiner = cm / 'Runtime/Behaviours/CinemachineConfiner2D.cs'
    text = vcam.read_text(encoding='utf-8-sig')
    fragments = [method(text, m) for m in ('internal void AddExtension(',
        'internal void RemoveExtension(', 'protected void InvokePostPipelineStageCallback(')]
    callbacks = out / 'CmInstalledCallbacks.cs'
    callbacks.write_text('using System.Collections.Generic;using UnityEngine;namespace Unity.Cinemachine {'
        'public partial class CinemachineVirtualCameraBase {'+'\n'.join(fragments)+
        'internal List<CinemachineExtension> Extensions { get; private set; }}'
        'public partial class CinemachineConfiner2D {'+
        method(confiner.read_text(encoding='utf-8-sig'), 'protected override void PostPipelineStageCallback(')+'}}', encoding='utf-8')
    names = ['StreetFollowDriver', 'CameraFollowConfigBinder', 'StreetCameraDebug',
             'StreetFollowPipeline', 'StreetFollowPoint', 'GroundSpace', 'ChannelContext']
    sources = sorted((root / 'Assets/Scripts/LongLoop/Configuration').glob('*.cs'))
    sources += [root / ('Assets/Scripts/LongLoop/'+n+'.cs') for n in names]
    sources += [root / p for p in ['Assets/Scripts/Tools/GroundSpaceMath.cs',
        'Assets/Scripts/Tools/Input/IGameInput.cs', 'DevTools/LongLoop/FollowUnityShim.cs',
        'DevTools/LongLoop/FollowCinemachineBoundary.cs', 'DevTools/LongLoop/ChannelUnityBoundary.cs',
        'DevTools/LongLoop/StreetConfigLogicTests.cs', 'DevTools/LongLoop/StreetFollowPipelineTests.cs',
        'DevTools/LongLoop/StreetFollowDriverTests.cs']]
    sources += [cm / ('Runtime/'+p+'.cs') for p in ['Components/CinemachinePositionComposer',
        'Core/ScreenComposerSettings', 'Core/LookaheadSettings', 'Core/Predictor']]+[callbacks]
    assert not any('Probe' in p.name or 'FollowRSmoke' in p.name for p in sources)
    refs = [fx / p for p in ['mscorlib.dll', 'System.dll', 'System.Core.dll', 'Facades/netstandard.dll']]
    rsp = out / 'product-tests.rsp'
    exe = out / 'product-tests.exe'
    rsp.write_text('\n'.join(['/nologo', '/noconfig', '/langversion:preview', '/target:exe',
        '/define:MOVING_LOOKAHEAD_PRODUCT'+(';OLD_WORLD_DAMPING' if args.counter else ''), '/main:StreetFollowDriverTests', '/out:"'+str(exe)+'"']+
        ['/r:"'+str(p)+'"' for p in refs]+['"'+str(p)+'"' for p in sources])+'\n', encoding='utf-8')
    inputs = sources+runner+[vcam, confiner, rsp, cm/'package.json']
    execute('product-compile', [str(mono), str(csc), '@'+str(rsp)], inputs, refs)
    test_args = (['--counter'] if args.counter else ['--deadzone-counter'] if args.deadzone_counter
                 else ['--motion-only'] if args.motion_only else [])
    execute('product-test', [str(mono), str(exe)]+test_args, inputs, refs)
    print('Actual installed CM Composer/Damper and callbacks; Unity/Brain/rectangle geometry remain explicit offline boundaries.')


if __name__ == '__main__':
    main()
