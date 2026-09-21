"""Exercise the real hook in a disposable repository; never touch project index."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
ASSET = 'Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset'
SHELL = str(Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/bin/bash.exe') if os.name == 'nt' else 'sh'
ENV = os.environ.copy()
if os.name == 'nt':
    ENV['PATH'] = str(Path(SHELL).parents[1] / 'usr/bin') + os.pathsep + ENV['PATH']
results = []
with tempfile.TemporaryDirectory(prefix='mig63-font-') as tmp:
    repo = Path(tmp)
    def git(*args, data=None):
        return subprocess.run(['git', '-C', str(repo), *args], input=data, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True).stdout.decode().strip()
    git('init', '-b', 'migration/unity-6.3')
    git('config', 'core.autocrlf', 'false')
    git('config', 'core.hooksPath', '.git/hooks')
    (repo / ASSET).parent.mkdir(parents=True)
    original = (ROOT / ASSET).read_bytes()
    (repo / ASSET).write_bytes(original)
    (repo / 'DevTools/GitHooks').mkdir(parents=True)
    shutil.copy2(ROOT / 'DevTools/GitHooks/check-mig63-font.sh', repo / 'DevTools/GitHooks/check-mig63-font.sh')
    shutil.copy2(ROOT / '.ai-workspace/MIG63/pre-commit.sh', repo / '.git/hooks/pre-commit')
    git('add', ASSET)
    def check(name, succeeds):
        run = subprocess.run([SHELL, '.git/hooks/pre-commit'], cwd=repo, env=ENV, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        assert (run.returncode == 0) == succeeds, f'{name}: {run.stderr.decode(errors="replace")}'
        results.append({'case': name, 'passed': True, 'exit': run.returncode})
    def stage_blob(content):
        oid = git('hash-object', '-w', '--stdin', data=content)
        git('update-index', '--add', '--cacheinfo', f'100644,{oid},{ASSET}')
    def pointer(sha):
        return f'version https://git-lfs.github.com/spec/v1\noid sha256:{sha}\nsize {len(original)}\n'.encode()
    check('raw baseline passes', True)
    changed = original + b'\nMIG63 fixture only\n'
    (repo / ASSET).write_bytes(changed)
    check('working drift blocks even with clean index', False)
    git('add', ASSET)
    (repo / ASSET).write_bytes(original)
    check('staged drift blocks after working restoration', False)
    stage_blob(pointer(hashlib.sha256(original).hexdigest()))
    check('LFS baseline content passes', True)
    stage_blob(pointer(hashlib.sha256(changed).hexdigest()))
    check('LFS changed content blocks', False)
    git('config', 'mig63.fontApprovedSha256', hashlib.sha256(changed).hexdigest())
    check('hash exception without reason blocks', False)
    git('config', 'mig63.fontApprovalReason', 'Fixture-only explicit exact-hash approval')
    (repo / ASSET).write_bytes(changed)
    check('exact-hash approval covers both representations', True)
    (repo / ASSET).write_bytes(changed + b'other')
    check('approval never covers a different hash', False)
    git('config', '--unset', 'mig63.fontApprovedSha256')
    git('config', '--unset', 'mig63.fontApprovalReason')
    (repo / ASSET).write_bytes(original)
    git('update-index', '--force-remove', ASSET)
    check('missing index asset blocks', False)
    git('add', ASSET)
    (repo / ASSET).unlink()
    check('missing working asset blocks', False)
    (repo / ASSET).write_bytes(original)
    git('symbolic-ref', 'HEAD', 'refs/heads/main')
    check('main remains blocked', False)
    git('symbolic-ref', 'HEAD', 'refs/heads/backup/test')
    check('backup remains blocked', False)
    # The baseline font itself is an added Assets/Font path in this empty fixture.
    git('symbolic-ref', 'HEAD', 'refs/heads/content/p0p1-layout')
    check('content file-ownership freeze remains enforced', False)
    git('symbolic-ref', 'HEAD', 'refs/heads/migration/unity-6.3')
    (repo / 'probe.unity').write_text('fixture only')
    git('add', 'probe.unity')
    check('migration scene freeze remains enforced', False)
    git('config', 'mig63.phase', 'integrating')
    check('approved migration phase permits scene commit', True)
print(json.dumps(results, indent=2))
