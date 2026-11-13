"""Read staged paths/blob metadata only; never change the index or run LFS filters."""
import argparse
import fnmatch
import json
import os
from pathlib import Path
import subprocess

MAX_BLOB_BYTES = 100_000_000

# Deliberately reviewed source exceptions, mirrored in the root .gitignore.
INPUT_PATTERNS = (
    'README.md', 'USER_WORK_STATUS.md', '*_用户长期授权_*.json',
    '排布指导书_*.md', '排布书v*_回填_*.csv', 'AI开发工作流总档_*.md',
    'CK01_P0P1_需求对照基准*.md', 'CK01-B_配表Schema*.md',
    'P0-P1_开发路径规划*.md', 'P0-P1_模块拆分追踪表*.md',
    'P2_模块拆分表_*.md', '平行推进框架_长循环L与小游戏MG_*.md',
    '派发包索引_*.md', '追踪表_v*_常驻摘录*.md',
    'ART车道_每日资产审查日报模板_*.md', '体验清单_demo-*.md',
    'E2_街道空间与镜头系统设计_*.docx',
)
INPUT_ART_PATTERNS = ('美术需求单_*.md', 'ART_DAILY_CONFIG.json',
                      'ART_EXPORT_RULES.md', 'ART_PENDING_ITEMS.json')
SHORT_CYCLE_SOURCES = {
    '派发/BATCH2-INDEPENDENT-20261023/编排登记.json',
    '派发/BATCH2-INDEPENDENT-20261023/执行登记.md',
    '派发/BATCH2-INDEPENDENT-20261023/用户暂停短循环并切回可玩包调整验收_20261102.json',
}
OUTPUT_SOURCES = {
    'README.md', 'CK01-C/C-T5b-X_BLOCKED_ON_ART.md',
    'control/Export-UnclosedTasks.ps1', 'control/人工处理索引.md',
    'control/未关闭任务汇总.md', 'control/下一轮开发承接.md',
    'control/REV-014_人工验收关闭记录.md',
}
ARCHIVE_TRACKING = {'TASK_DIGEST.md', 'TASK_DIGEST_INDEX.json',
                    'usage-backfill.json', 'INSTRUCTIONS_ARCHIVE_INDEX.json'}
LOCAL_ROOTS = {
    'library', 'temp', 'obj', 'build', 'builds', 'release', '交付文件',
    'logs', 'memorycaptures', 'recordings', 'profilercaptures', 'profiling',
    'usersettings', '.vs', '.idea', '.gradle', '.consulo', 'exportedobj',
    '.claude', '.art-asset-sync', '美术资产',
}
AI_LOCAL_ROOTS = {'runtime', 'logs', 'cache', 'revisions', 'exports'}
HANDOVER_SOURCES = {'README.md', 'COMMON.md', 'CODE.md', 'CONTROL.md', 'ENGINE_CLI.md'}
TOOL_BACKUPS = {'scenes', 'prefabs', 'generated', 'fonts', 'artassets'}


def generated_reason(path):
    parts = path.split('/')
    lower = [part.lower() for part in parts]
    if '__pycache__' in lower or lower[-1].endswith(('.pyc', '.pyo')):
        return 'local Python interpreter cache'
    if lower[:2] == ['assets', 'spine examples']:
        return 'local third-party Spine vendor examples'
    if lower[0] in LOCAL_ROOTS:
        return 'local generated/sync directory'
    if len(parts) == 1 and (lower[0] in {'.mcp.json', '.vsconfig', 'sysinfo.txt',
                                                      'crashlytics-build.properties'} or
                            lower[0].endswith(('.csproj', '.unityproj', '.sln', '.suo',
                                              '.user', '.userprefs', '.pidb', '.booproj',
                                              '.svd', '.pdb', '.mdb', '.opendb', '.vc.db'))):
        return 'local Unity/IDE file'
    if lower[0] == 'captures' and path != 'Captures/README.md':
        return 'local capture'
    if len(parts) >= 3 and lower[0] == 'devtools':
        if lower[1] in TOOL_BACKUPS and lower[2] == 'backup':
            return 'local tool backup'
        if lower[1:3] == ['datatables', '.batch-workspace']:
            return 'local table workspace'
    if lower[0] == 'packages' and path not in {'Packages/manifest.json', 'Packages/packages-lock.json'}:
        return 'package-manager generated content'
    if lower[0] != '.ai-workspace' or len(parts) < 2:
        return None
    section = lower[1]
    relative = '/'.join(parts[2:])
    if section in AI_LOCAL_ROOTS:
        return 'local AI runtime/evidence'
    if section == 'handover':
        return None if parts[-1] in HANDOVER_SOURCES else 'local machine handover state'
    if section == 'inputs':
        if relative in SHORT_CYCLE_SOURCES:
            return None
        if len(parts) == 3 and any(fnmatch.fnmatchcase(relative, p) for p in INPUT_PATTERNS):
            return None
        if len(parts) == 4 and parts[2] == 'art' and any(
                fnmatch.fnmatchcase(parts[3], p) for p in INPUT_ART_PATTERNS):
            return None
        if relative == 'DOC-REVIEW-20261017/旧链参考文档保留清单.md':
            return None
        if len(parts) >= 6 and parts[2] == '派发' and parts[-2] == 'cards' and parts[-1].endswith('.md'):
            return None
        if len(parts) >= 6 and parts[2] == '派发' and parts[-2] == 'original' and fnmatch.fnmatchcase(parts[-1], '派发包索引_*.md'):
            return None
        if len(parts) == 6 and parts[2:5] == ['派发', 'BATCH2-INDEPENDENT-20261023', '原始派发包'] and fnmatch.fnmatchcase(parts[-1], '派发包索引_*.md'):
            return None
        return 'temporary AI input (not a reviewed permanent source)'
    if section == 'outputs' and relative not in OUTPUT_SOURCES:
        return 'temporary AI output'
    if section == 'archive':
        if parts[-1] in {'README.md', 'INDEX.md', 'INDEX.json', 'index.md', 'index.json'}:
            return None
        if len(parts) == 4 and parts[2] == 'tracking' and parts[3] in ARCHIVE_TRACKING:
            return None
        return 'archived evidence (keep only lightweight source indexes/tracking)'
    return None


def inspect(repo):
    env = dict(os.environ, GIT_OPTIONAL_LOCKS='0', GIT_TERMINAL_PROMPT='0',
               GIT_NO_LAZY_FETCH='1', GIT_NO_REPLACE_OBJECTS='1')
    command = ['git', '--no-optional-locks', '--literal-pathspecs', '-c', 'core.fsmonitor=false',
               '-c', 'gc.auto=0', '-c', 'maintenance.auto=false', '-C', str(repo)]

    def git(*args, data=None):
        result = subprocess.run(command + list(args), input=data, env=env,
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if result.returncode:
            raise RuntimeError(result.stderr.decode('utf-8', errors='replace').strip())
        return result.stdout

    actual = Path(os.fsdecode(git('rev-parse', '--show-toplevel').strip())).resolve()
    if actual != repo.resolve():
        raise RuntimeError('Git root differs from the requested repository')
    if git('ls-files', '--unmerged', '-z'):
        raise RuntimeError('Unmerged index entries')
    raw = git('diff', '--cached', '--raw', '--no-abbrev', '-z', '--no-renames',
              '--no-ext-diff', '--no-textconv', '--diff-filter=ACMRT')
    fields = raw.split(b'\0')
    if fields.pop() != b'' or len(fields) % 2:
        raise RuntimeError('Unexpected staged diff format')
    entries = []
    for header, name in zip(fields[::2], fields[1::2]):
        oldmode, mode, oldoid, oid, status = header.decode('ascii').split()
        if not oldmode.startswith(':') or status not in 'ACMRT':
            raise RuntimeError('Unexpected staged entry')
        entries.append((os.fsdecode(name), mode, oid))
    blobs = sorted({oid for _, mode, oid in entries if mode != '160000'})
    sizes = {}
    if blobs:
        lines = git('cat-file', '--batch-check=%(objectname) %(objecttype) %(objectsize)',
                    data=('\n'.join(blobs) + '\n').encode('ascii')).splitlines()
        if len(lines) != len(blobs):
            raise RuntimeError('Incomplete staged object metadata')
        for expected, line in zip(blobs, lines):
            oid, kind, size = line.decode('ascii').split()
            if oid != expected or kind != 'blob':
                raise RuntimeError('Missing or unexpected staged blob')
            sizes[oid] = int(size)
    rejected = []
    for path, mode, oid in entries:
        reason = generated_reason(path)
        size = sizes.get(oid)
        if reason:
            rejected.append({'path': path, 'reason': reason, 'blobBytes': size})
        if size is not None and size > MAX_BLOB_BYTES:
            rejected.append({'path': path, 'reason': 'Git blob exceeds 100,000,000 bytes; use source LFS policy',
                             'blobBytes': size})
    return {'success': not rejected, 'stagedEntries': len(entries),
            'maxBlobBytes': MAX_BLOB_BYTES, 'rejected': rejected}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path.cwd())
    args = parser.parse_args()
    try:
        result = inspect(args.repo)
    except Exception as error:
        result = {'success': False, 'error': str(error)}
    print(json.dumps(result, ensure_ascii=True))
    return 0 if result['success'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
