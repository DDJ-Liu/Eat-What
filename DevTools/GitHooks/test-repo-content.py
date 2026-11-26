"""Real hooks in disposable Git repositories; no project index/config/hook writes."""
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import runpy
import shutil
import subprocess
import sys
import tempfile
import argparse

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / 'DevTools/GitHooks/check-repo-content.py'
PORTABLE = ROOT / '.ai-workspace/MIG63/pre-commit.sh'
LEGACY = 'Assets/Fonts/Heavy/ShortCycle_Heavy_WithFallback.asset'
SHELL = str(Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/bin/bash.exe') if os.name == 'nt' else 'sh'
ENV = os.environ.copy()
ENV.update(GIT_CONFIG_NOSYSTEM='1', GIT_CONFIG_GLOBAL=os.devnull, GIT_OPTIONAL_LOCKS='0')
if os.name == 'nt':
    ENV['PATH'] = str(Path(SHELL).parents[1] / 'usr/bin') + os.pathsep + ENV['PATH']
results = []


def check(name, condition):
    assert condition, name
    results.append({'case': name, 'passed': True})


def repair_boundaries():
    """Only root513 rejection paths and the actual old standalone entry."""
    vendor = sorted(p.relative_to(ROOT).as_posix() for p in
                    (ROOT / 'Assets/Spine Examples').rglob('*') if p.is_file())
    # A new clone may not contain deliberately local vendor files.
    examples = vendor or ['Assets/Spine Examples/Getting Started/example.cs']
    caches = ['__pycache__/root.pyc', 'DevTools/GitHooks/__pycache__/checker.cpython-311.pyc',
              'DevTools/Fixtures/nested/__pycache__/anything.txt', 'nested/raw.pyc',
              'nested/old.pyo', 'nested/deeper/__pycache__/optimized.cpython-311.opt-1.pyc']
    allowed = ['Assets/Scenes/Spine Sample.unity', 'Assets/Scenes/Spine Sample.unity.meta',
               'Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity',
               'Assets/Editor/ProjectValidation/SpineSceneProbe.cs',
               'DevTools/Fixtures/source.py', 'DevTools/Fixtures/native_extension.pyd',
               'DevTools/Fixtures/__pycache__example.py', 'docs/配置说明.md',
               '.vscode/extensions.json', '.vscode/launch.json', '.vscode/settings.json',
               'Eat-What.code-workspace', '.kiro/steering/unity-workflow.md']
    blocked = examples + caches
    with tempfile.TemporaryDirectory(prefix='repo-policy-repair-') as tmp:
        repo = Path(tmp)

        def git(*args, data=None, expected=(0,)):
            value = subprocess.run(['git', '-C', str(repo), *args], input=data, env=ENV,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            assert value.returncode in expected, (args, value.stderr.decode(errors='replace'))
            return value.stdout

        git('init', '-b', 'codex/source-repair-fixture')
        git('config', 'core.autocrlf', 'false')
        shutil.copy2(ROOT / '.gitignore', repo / '.gitignore')
        paths = blocked + allowed
        ignored = set(os.fsdecode(p) for p in git('check-ignore', '--no-index', '-z', '--stdin',
                      data=b'\0'.join(os.fsencode(p) for p in paths)+b'\0', expected=(0, 1)).split(b'\0') if p)
        for path in blocked:
            check('actual generated path ignored: '+path, path in ignored)
        for path in allowed:
            check('formal source remains visible: '+path, path not in ignored)
        for path in paths:
            target = repo / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(b'isolated synthetic fixture bytes\n')
        git('add', '--force', '--pathspec-from-file=-', '--pathspec-file-nul',
            data=b'\0'.join(os.fsencode(p) for p in paths)+b'\0')

        def checker():
            before = [(repo / '.git' / p).read_bytes() for p in ['index', 'config', 'HEAD']]
            run = subprocess.run([sys.executable, str(CHECKER), '--repo', str(repo)], env=ENV,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            value = json.loads(run.stdout.decode('utf-8'))
            check('checker leaves tiny repository index/config/HEAD unchanged',
                  before == [(repo / '.git' / p).read_bytes() for p in ['index', 'config', 'HEAD']])
            return run, value

        run, value = checker()
        check('real force-add of generated paths is rejected', run.returncode != 0 and not value['success'])
        rejected = {x['path'] for x in value['rejected']}
        check('exactly generated paths rejected; no formal-source false positive', rejected == set(blocked))
        for path in blocked:
            check('actual staged generated path rejected: '+path, path in rejected)
        git('read-tree', '--empty')
        git('add', '--force', '--pathspec-from-file=-', '--pathspec-file-nul',
            data=b'\0'.join(os.fsencode(p) for p in allowed)+b'\0')
        run, value = checker()
        check('formal sources pass the actual helper with no blocked entries', run.returncode == 0 and value['success'])
    direct = subprocess.run([sys.executable, str(ROOT / 'DevTools/GitHooks/test-font-guard.py')],
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=ENV)
    check('old standalone font entry exits zero without injection', direct.returncode == 0)
    font = json.loads(direct.stdout.decode('utf-8'))
    check('original standalone37 assertions all pass', font['count'] == 37 and all(x['passed'] for x in font['results']))
    print(json.dumps({'success': True, 'count': len(results), 'results': results,
                      'vendorFilesOnDisk': len(vendor), 'vendorPathsTested': len(examples),
                      'cachePathsTested': len(caches), 'formalSourcesTested': len(allowed),
                      'fontCount': font['count'], 'fontResults': font['results'],
                      'productionGitWrites': 0, 'vendorFileContentsRead': 0}, ensure_ascii=True, indent=2))


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repair-boundaries', action='store_true', help='Only root513 vendor/cache/old-entry repair coverage')
args = parser.parse_args()
if args.repair_boundaries:
    repair_boundaries()
    raise SystemExit(0)


spec = importlib.util.spec_from_file_location('repo_content', CHECKER)
policy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy)
check('production threshold is 100,000,000 decimal bytes', policy.MAX_BLOB_BYTES == 100_000_000)

with tempfile.TemporaryDirectory(prefix='repo-content-') as tmp:
    repo = Path(tmp)

    def git(*args, data=None, allowed=(0,)):
        value = subprocess.run(['git', '-C', str(repo), *args], input=data, env=ENV,
                               stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        assert value.returncode in allowed, (args, value.stderr.decode(errors='replace'))
        return value.stdout

    git('init', '-b', 'codex/repo-content-fixture')
    git('config', 'core.autocrlf', 'false')
    git('config', 'core.hooksPath', '.git/hooks')
    git('config', 'user.name', 'Disposable fixture')
    git('config', 'user.email', 'fixture@example.invalid')
    original = b'isolated historical legacy font baseline\n'
    (repo / LEGACY).parent.mkdir(parents=True)
    (repo / LEGACY).write_bytes(original)
    (repo / 'release').mkdir()
    (repo / 'release/previous-build.bin').write_bytes(b'already tracked generated file')
    git('add', LEGACY, 'release/previous-build.bin')
    # Seed before installing a hook: later removal must remain possible.
    git('commit', '-m', 'Disposable baseline')
    shutil.copy2(ROOT / '.gitignore', repo / '.gitignore')
    tool = repo / 'DevTools/GitHooks'
    tool.mkdir(parents=True)
    font = (ROOT / 'DevTools/GitHooks/check-mig63-font.sh').read_text(encoding='utf-8')
    font = font.replace('1d379a7eb64de1871194614f83bf61ac52e0d323677048fe237e3fbd3b399cc2',
                        hashlib.sha256(original).hexdigest())
    (tool / 'check-mig63-font.sh').write_text(font, encoding='utf-8', newline='\n')
    shutil.copy2(CHECKER, tool / CHECKER.name)
    shutil.copy2(PORTABLE, repo / '.git/hooks/pre-commit')

    manifest = json.loads((ROOT / '.ai-workspace/PERMANENT_DOCUMENTS.json').read_text(encoding='utf-8-sig'))
    sources = list(manifest['exactPaths'])
    for family in manifest['families']:
        sources.extend(family.get('currentPaths', []))
    sources.extend('.ai-workspace/inputs/' + p for p in policy.SHORT_CYCLE_SOURCES)
    short_cycle = ROOT / '.ai-workspace/inputs/派发/BATCH2-INDEPENDENT-20261023/原始派发包'
    sources.extend(p.relative_to(ROOT).as_posix() for p in short_cycle.rglob('*.md'))
    sources += [
        'Assets/脚本/示例.cs', 'Assets/脚本/示例.cs.meta', 'Assets/Scenes/Spine Sample.unity',
        'Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectSettings.asset',
        'LocalPackages/com.coplaydev.unity-mcp/package.json', 'DataTables/Cooking/正式配表.xlsx',
        'DevTools/Fixtures/项目.csproj', 'docs/配置说明.md', '组件说明文档/视觉特效/说明.md',
        '.ai-workspace/inputs/监管结算即时回传_用户长期授权_20261014.json',
        '.ai-workspace/inputs/E2_街道空间与镜头系统设计_v1_0.docx',
        '.ai-workspace/inputs/派发/新版/phase/cards/规划.md',
        '.ai-workspace/inputs/派发/新版/original/派发包索引_短循环_v1.md',
        '.ai-workspace/handover/20261112/COMMON.md',
        '.ai-workspace/handover/20261112/CODE.md',
        '.ai-workspace/handover/20261112/CONTROL.md',
        '.ai-workspace/handover/20261112/ENGINE_CLI.md',
    ]
    sources = sorted(set(sources))
    blocked = [
        'Assets/Spine Examples/Getting Started/example.cs',
        '__pycache__/root.pyc', 'DevTools/GitHooks/__pycache__/checker.cpython-311.pyc',
        'DevTools/Fixtures/nested/cache.pyo', 'nested/old.pyc',
        'release/demo-0.1/Game.exe', 'Build/game.exe', 'Builds/game.exe', '交付文件/demo/Game.exe',
        'Library/cache', 'Temp/file', 'Obj/file', 'Logs/Editor.log', 'MemoryCaptures/capture.snap',
        'Recordings/video.mp4', 'ProfilerCaptures/frame.data', 'Profiling/frame.raw',
        'UserSettings/layout.wlt', '.vs/private.json', '.idea/workspace.xml', '.mcp.json',
        'Game.csproj', 'Captures/frame.png', 'DevTools/Fonts/backup/font.asset',
        'DevTools/DataTables/.batch-workspace/table.xlsx', '美术资产/sync.png',
        '.ai-workspace/runtime/queue.json', '.ai-workspace/outputs/AI-123/REPORT.md',
        '.ai-workspace/inputs/acceptance/AI-123.json', '.ai-workspace/inputs/临时需求.txt',
        '.ai-workspace/inputs/派发/batch/raw.json', '.ai-workspace/inputs/art/AI000057_manifest.json',
        '.ai-workspace/revisions/old.json', '.ai-workspace/handover/local.json',
        '.ai-workspace/handover/20261112/bind-new-code.json',
        '.ai-workspace/archive/old/records.zip', '.ai-workspace/archive/old/snapshot.png',
        'Packages/com.generated/file.cs',
    ]
    candidates = sources + blocked
    ignored = set(os.fsdecode(value) for value in git(
        'check-ignore', '--no-index', '-z', '--stdin',
        data=b'\0'.join(os.fsencode(p) for p in candidates) + b'\0', allowed=(0, 1)).split(b'\0') if value)
    for path in sources:
        check('source ignore boundary: ' + path, path not in ignored)
        check('source staged boundary: ' + path, policy.generated_reason(path) is None)
    for path in blocked:
        check('generated ignore boundary: ' + path, path in ignored)
        check('generated staged boundary: ' + path, policy.generated_reason(path) is not None)

    def reset():
        git('reset', '--mixed', 'HEAD')

    def stage(path, content):
        oid = git('hash-object', '-w', '--stdin', data=content).decode().strip()
        git('update-index', '--add', '--cacheinfo', '100644,' + oid + ',' + path)

    def hook(name, expected, contains=None, extra_env=None):
        index_before = (repo / '.git/index').read_bytes()
        config_before = (repo / '.git/config').read_bytes()
        value = subprocess.run([SHELL, '.git/hooks/pre-commit'], cwd=repo,
                               env=extra_env or ENV, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        output = (value.stdout + value.stderr).decode('utf-8', errors='replace')
        check(name, (value.returncode == 0) == expected)
        if contains:
            check(name + ' diagnostic', contains in output)
        check(name + ' index unchanged', (repo / '.git/index').read_bytes() == index_before)
        check(name + ' config unchanged', (repo / '.git/config').read_bytes() == config_before)
        return value

    reset()
    stage('Assets/脚本/示例.cs', b'// source\n')
    stage('docs/配置说明.md', '配置说明\n'.encode())
    stage('组件说明文档/视觉特效/说明.md', '组件说明\n'.encode())
    hook('real complete hook allows source and Chinese docs', True)
    for path in blocked:
        reset()
        stage(path, b'generated')
        hook('real complete hook blocks: ' + path, False, '"success": false')
    reset()
    stage('release/previous-build.bin', b'modified generated file')
    hook('tracked generated modification blocked', False, 'local generated')
    reset()
    git('update-index', '--force-remove', 'release/previous-build.bin')
    hook('tracked generated deletion allowed', True)
    reset()
    # Only this disposable checker copy gets a smaller limit; production has no bypass.
    fixture_checker = tool / CHECKER.name
    fixture_checker.write_text(CHECKER.read_text(encoding='utf-8').replace(
        'MAX_BLOB_BYTES = 100_000_000', 'MAX_BLOB_BYTES = 256'), encoding='utf-8', newline='\n')
    stage('Assets/ordinary.bin', b'x' * 256)
    hook('exact fixture byte limit allowed', True)
    stage('Assets/ordinary.bin', b'x' * 257)
    hook('ordinary Git blob above fixture limit blocked', False, 'Git blob exceeds')
    reset()
    pointer = (b'version https://git-lfs.github.com/spec/v1\n' +
               b'oid sha256:' + b'1' * 64 + b'\nsize 200000000\n')
    stage('Assets/large-source.png', pointer)
    hook('LFS source pointer allowed by actual index blob size', True)
    reset()
    stage('release/large-delivery.png', pointer)
    hook('LFS release pointer cannot bypass source boundary', False, 'local generated')
    reset()
    stage('Assets/small-index.bin', b'x')
    (repo / 'Assets/small-index.bin').write_bytes(b'y' * 1024)
    hook('large unstaged worktree file is not measured as index blob', True)
    reset()
    special_path = 'Assets/name, with space.txt' if os.name == 'nt' else 'Assets/name\nwith-newline.txt'
    stage(special_path, b'source')
    hook('NUL-safe platform-valid staged path parsing', True)
    reset()
    shutil.copy2(CHECKER, fixture_checker)
    fixture_checker.unlink()
    hook('missing checker fails closed', False, 'checker is missing')
    shutil.copy2(CHECKER, fixture_checker)
    # Alternate legitimate commit indexes remain honored, with the project untouched.
    alternate = repo / 'alternate-index'
    shutil.copy2(repo / '.git/index', alternate)
    env_index = dict(ENV, GIT_INDEX_FILE=str(alternate))
    hook('alternate commit index accepted', True, extra_env=env_index)

# The original fixture now owns its mandatory checker dependency. Run it directly.
captured = io.StringIO()
with contextlib.redirect_stdout(captured):
    runpy.run_path(str(ROOT / 'DevTools/GitHooks/test-font-guard.py'), run_name='__main__')
font_results = json.loads(captured.getvalue())
check('all existing font/branch assertions retained', font_results['count'] == 37 and
      all(item['passed'] for item in font_results['results']))
original_prefix = '''#!/bin/sh
root=$(git rev-parse --show-toplevel) || exit 1
sh "$root/DevTools/GitHooks/check-mig63-font.sh" || exit 1
# Permanent branch guard; migration-specific path freezes were retired at E2.
branch=$(git symbolic-ref --quiet --short HEAD) || exit 1
case "$branch" in
  main|backup/*) echo "MIG63: direct commits on main/backup are blocked." >&2; exit 1 ;;
esac
'''
check('original font then branch prefix preserved exactly',
      PORTABLE.read_bytes().startswith(original_prefix.encode('utf-8')))
print(json.dumps({'success': True, 'count': len(results), 'results': results,
                  'fontCount': font_results['count'], 'fontResults': font_results['results']}, ensure_ascii=True, indent=2))
