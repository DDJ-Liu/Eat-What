"""Exercise the real font guard in a disposable repository; never touch project index."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
LEGACY = 'Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset'
STATIC = 'Assets/Font/Heavy/ShortCycle_Heavy_Static.asset'
DYNAMIC = 'Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset'
MANIFEST = 'DevTools/Fonts/ShortCycleFontBuildManifest.json'
SHELL = str(Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/bin/bash.exe') if os.name == 'nt' else 'sh'
ENV = os.environ.copy()
if os.name == 'nt':
    ENV['PATH'] = str(Path(SHELL).parents[1] / 'usr/bin') + os.pathsep + ENV['PATH']

results = []
with tempfile.TemporaryDirectory(prefix='mig63-font-') as tmp:
    repo = Path(tmp)

    def git(*args, data=None):
        return subprocess.run(
            ['git', '-C', str(repo), *args], input=data,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True
        ).stdout.decode().strip()

    def sha(content):
        return hashlib.sha256(content).hexdigest()

    def check(name, succeeds):
        run = subprocess.run(
            [SHELL, '.git/hooks/pre-commit'], cwd=repo, env=ENV,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE
        )
        assert (run.returncode == 0) == succeeds, (
            f'{name}: exit={run.returncode}\n{run.stderr.decode(errors="replace")}'
        )
        results.append({'case': name, 'passed': True, 'exit': run.returncode})

    def stage_blob(path, content):
        oid = git('hash-object', '-w', '--stdin', data=content)
        git('update-index', '--add', '--cacheinfo', f'100644,{oid},{path}')

    def pointer(content):
        return (
            'version https://git-lfs.github.com/spec/v1\n'
            f'oid sha256:{sha(content)}\nsize {len(content)}\n'
        ).encode()

    def write_manifest(status, static_content, dynamic_content):
        value = {
            'schemaVersion': 1,
            'approvalStatus': status,
            'approvedBy': 'font-guard-fixture',
            'approvedAtUtc': '2026-09-23T00:00:00Z',
            'legacyAssetPath': LEGACY,
            'legacyAssetSha256': sha(original).upper(),
            'staticAssetPath': STATIC,
            'staticAssetSha256': sha(static_content).upper(),
            'dynamicAssetPath': DYNAMIC,
            'dynamicEmptyAssetSha256': sha(dynamic_content).upper(),
        }
        (repo / MANIFEST).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

    git('init', '-b', 'migration/unity-6.3')
    git('config', 'core.autocrlf', 'false')
    git('config', 'core.hooksPath', '.git/hooks')
    (repo / LEGACY).parent.mkdir(parents=True)
    original = (ROOT / LEGACY).read_bytes()
    (repo / LEGACY).write_bytes(original)
    (repo / 'DevTools/GitHooks').mkdir(parents=True)
    shutil.copy2(
        ROOT / 'DevTools/GitHooks/check-mig63-font.sh',
        repo / 'DevTools/GitHooks/check-mig63-font.sh'
    )
    shutil.copy2(ROOT / '.ai-workspace/MIG63/pre-commit.sh', repo / '.git/hooks/pre-commit')
    git('add', LEGACY)

    # Legacy mode remains unchanged until the PRE-1 static asset exists.
    check('legacy raw baseline passes', True)
    stage_blob(DYNAMIC, b'partial dynamic only')
    check('legacy mode blocks staged dynamic without static', False)
    git('update-index', '--force-remove', DYNAMIC)
    stage_blob(MANIFEST, b'{}')
    check('legacy mode blocks staged manifest without static', False)
    git('update-index', '--force-remove', MANIFEST)
    changed = original + b'\nMIG63 fixture only\n'
    (repo / LEGACY).write_bytes(changed)
    check('legacy working drift blocks even with clean index', False)
    git('add', LEGACY)
    (repo / LEGACY).write_bytes(original)
    check('legacy staged drift blocks after working restoration', False)
    stage_blob(LEGACY, pointer(original))
    check('legacy LFS baseline content passes', True)
    stage_blob(LEGACY, pointer(changed))
    check('legacy LFS changed content blocks', False)
    git('config', 'mig63.fontApprovedSha256', sha(changed))
    check('legacy hash exception without reason blocks', False)
    git('config', 'mig63.fontApprovalReason', 'Fixture-only explicit exact-hash approval')
    (repo / LEGACY).write_bytes(changed)
    check('legacy exact-hash approval covers both representations', True)
    (repo / LEGACY).write_bytes(changed + b'other')
    check('legacy approval never covers a different hash', False)
    git('config', '--unset', 'mig63.fontApprovedSha256')
    git('config', '--unset', 'mig63.fontApprovalReason')
    (repo / LEGACY).write_bytes(original)
    git('update-index', '--force-remove', LEGACY)
    check('legacy missing index asset blocks', False)
    git('add', LEGACY)
    (repo / LEGACY).unlink()
    check('legacy missing working asset blocks', False)
    (repo / LEGACY).write_bytes(original)
    git('symbolic-ref', 'HEAD', 'refs/heads/main')
    check('main remains blocked', False)
    git('symbolic-ref', 'HEAD', 'refs/heads/backup/test')
    check('backup remains blocked', False)
    git('symbolic-ref', 'HEAD', 'refs/heads/content/p0p1-layout')
    check('content file-ownership freeze remains enforced', False)
    git('symbolic-ref', 'HEAD', 'refs/heads/migration/unity-6.3')
    (repo / 'probe.unity').write_text('fixture only')
    git('add', 'probe.unity')
    check('migration scene freeze remains enforced', False)
    git('config', 'mig63.phase', 'integrating')
    check('approved migration phase permits scene commit', True)

    # Manifest mode protects both the static atlas and explicitly empty dynamic fallback.
    static_content = b'fixture static TMP asset\n'
    dynamic_content = b'fixture empty dynamic TMP asset\n'
    (repo / STATIC).write_bytes(static_content)
    (repo / DYNAMIC).write_bytes(dynamic_content)
    (repo / MANIFEST).parent.mkdir(parents=True, exist_ok=True)
    write_manifest('APPROVED', static_content, dynamic_content)
    git('add', STATIC, DYNAMIC, MANIFEST)
    check('approved manifest raw assets pass', True)

    (repo / STATIC).write_bytes(static_content + b'drift')
    check('static working drift blocks', False)
    (repo / STATIC).write_bytes(static_content)
    stage_blob(STATIC, static_content + b'staged drift')
    check('static staged drift blocks', False)
    git('add', STATIC)

    (repo / DYNAMIC).write_bytes(dynamic_content + b'cached glyph')
    check('dynamic working population drift blocks', False)
    (repo / DYNAMIC).write_bytes(dynamic_content)
    stage_blob(DYNAMIC, dynamic_content + b'staged glyph')
    check('dynamic staged population drift blocks', False)
    git('add', DYNAMIC)

    write_manifest('PENDING_REVIEW', static_content, dynamic_content)
    git('add', MANIFEST)
    check('pending manifest blocks', False)
    write_manifest('APPROVED', static_content, dynamic_content)
    git('add', MANIFEST)
    check('restored approved manifest passes', True)

    staged_manifest = (repo / MANIFEST).read_bytes()
    write_manifest('PENDING_REVIEW', static_content, dynamic_content)
    check('working and staged manifests must match', False)
    (repo / MANIFEST).write_bytes(staged_manifest)

    stage_blob(STATIC, pointer(static_content))
    stage_blob(DYNAMIC, pointer(dynamic_content))
    check('manifest mode accepts exact LFS content oids', True)
    git('add', STATIC, DYNAMIC)
    (repo / DYNAMIC).unlink()
    check('missing dynamic fallback blocks', False)

print(json.dumps({'count': len(results), 'results': results}, indent=2))
