"""Explicit, read-only baseline/index/worktree content comparison; no hook installation."""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess

ATTRS = ('text', 'eol', 'working-tree-encoding', 'ident', 'filter')

class CompareError(Exception):
    def __init__(self, code, message):
        super().__init__(message)
        self.code = code

def validate_indexed_config(env):
    # Only complete safe.directory entries may pass through unchanged. Their
    # opaque values are neither read for decisions nor included in diagnostics.
    count = env.get('GIT_CONFIG_COUNT')
    supplied = {key for key in env if key.startswith(('GIT_CONFIG_KEY_', 'GIT_CONFIG_VALUE_'))}
    if count is None:
        if supplied:
            raise CompareError('GIT_INDEXED_CONFIG_INVALID', 'Indexed config entries lack a count.')
        return
    if not re.fullmatch(r'0|[1-9][0-9]*', count) or len(count) > 4 or int(count) > 1024:
        raise CompareError('GIT_INDEXED_CONFIG_INVALID', 'Invalid indexed config count.')
    expected = {prefix + str(i) for i in range(int(count)) for prefix in ('GIT_CONFIG_KEY_', 'GIT_CONFIG_VALUE_')}
    if supplied != expected:
        raise CompareError('GIT_INDEXED_CONFIG_INVALID', 'Indexed config keys/values are incomplete or out of range.')
    if any(env['GIT_CONFIG_KEY_' + str(i)].lower() != 'safe.directory' for i in range(int(count))):
        raise CompareError('GIT_INDEXED_CONFIG_UNSUPPORTED', 'Only complete safe.directory indexed configuration is supported.')

class Repository:
    def __init__(self, path):
        self.root = Path(path).resolve(strict=True)
        self.env = os.environ.copy()
        for name in ('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES'):
            if self.env.get(name):
                raise CompareError('GIT_TARGET_OVERRIDE', 'Remove ambient ' + name + ' before explicit comparison.')
        validate_indexed_config(self.env)
        self.env.update(GIT_OPTIONAL_LOCKS='0', GIT_TERMINAL_PROMPT='0', GIT_NO_LAZY_FETCH='1', GIT_NO_REPLACE_OBJECTS='1')
        actual = self.git('rev-parse', '--show-toplevel').decode('utf-8').strip()
        if Path(actual).resolve() != self.root:
            raise CompareError('REPOSITORY_ROOT_REQUIRED', 'Repository must name the exact worktree root.')

    def git(self, *args, data=None, allow_missing=False):
        cmd = ['git', '--no-optional-locks', '--literal-pathspecs', '-c', 'core.fsmonitor=false', '-c', 'gc.auto=0', '-c', 'maintenance.auto=false', '-C', str(self.root), *args]
        run = subprocess.run(cmd, input=data, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=self.env, timeout=30)
        if run.returncode and not (allow_missing and run.returncode == 1):
            raise CompareError('GIT_READ_FAILED', run.stderr.decode('utf-8', errors='replace').strip()[:600] or 'Git read failed: ' + args[0])
        return run.stdout

    def attributes(self, path):
        fields = self.git('check-attr', '-z', *ATTRS, '--', path).decode('utf-8').split('\0')
        if len(fields) != len(ATTRS) * 3 + 1 or fields[-1] != '':
            raise CompareError('ATTRIBUTES_UNAVAILABLE', 'Cannot establish all clean attributes.')
        result = {}
        for i in range(0, len(fields)-1, 3):
            if fields[i] != path or fields[i+1] not in ATTRS:
                raise CompareError('ATTRIBUTES_UNAVAILABLE', 'Unexpected attribute identity.')
            result[fields[i+1]] = fields[i+2]
        return result

    def index_entry(self, path):
        entries = [x for x in self.git('ls-files', '--stage', '-z', '--', path).split(b'\0') if x]
        if not entries:
            raise CompareError('INDEX_PATH_MISSING', 'Explicit path is absent from index.')
        if len(entries) != 1:
            raise CompareError('INDEX_UNMERGED', 'Path has multiple index stages.')
        identity, stored_path = entries[0].split(b'\t', 1)
        mode, oid, stage = identity.decode('ascii').split()
        if stored_path.decode('utf-8') != path or stage != '0':
            raise CompareError('INDEX_UNMERGED', 'Path has no single stage-zero identity.')
        if mode not in ('100644', '100755'):
            raise CompareError('NONREGULAR_INDEX_PATH', 'Only regular indexed files are supported.')
        return mode, oid

    def working_bytes(self, path):
        full = self.root
        for component in path.split('/'):
            full = full / component
            try:
                info = full.lstat()
            except OSError as error:
                raise CompareError('WORKTREE_UNREADABLE', str(error)) from error
            if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & getattr(stat, 'FILE_ATTRIBUTE_REPARSE_POINT', 1024):
                raise CompareError('WORKTREE_REDIRECTION', 'Symlink/reparse paths are not supported.')
        if not stat.S_ISREG(info.st_mode):
            raise CompareError('WORKTREE_NOT_FILE', 'Explicit path is not a regular readable file.')
        try:
            return full.read_bytes()
        except OSError as error:
            raise CompareError('WORKTREE_UNREADABLE', str(error)) from error

    def settings(self):
        return {key: self.git('config', '--get', key, allow_missing=True).decode('utf-8').strip()
                for key in ('core.autocrlf', 'core.eol', 'core.safecrlf', 'core.checkRoundtripEncoding')}

def explicit_path(value):
    parts = PurePosixPath(value).parts
    if not value or not parts or value != '/'.join(parts) or any(x.lower() in ('.', '..', '.git') for x in parts) or any(c in value for c in '\\:*?[]\x00\r\n') or value.startswith('/'):
        raise CompareError('INVALID_EXPLICIT_PATH', 'Use an exact repository-relative forward-slash file path, without traversal or patterns.')
    return value

def sha(data):
    return hashlib.sha256(data).hexdigest()

def pointer(data):
    marker = b'version https://git-lfs.github.com/spec/v1'
    if not data.startswith(marker):
        return None
    match = re.fullmatch(rb'version https://git-lfs.github.com/spec/v1\noid sha256:([0-9a-f]{64})\nsize (0|[1-9][0-9]*)\n', data)
    if not match:
        raise CompareError('INVALID_LFS_POINTER', 'LFS identity must be a standard oid/size pointer; unknown extensions fail closed.')
    return dict(sha256=match[1].decode(), bytes=int(match[2]))

def compare(repo, commit, value):
    result = dict(path=value, status='FAIL')
    try:
        path = explicit_path(value)
        entries = [x for x in repo.git('ls-tree', '-z', commit, '--', path).split(b'\0') if x]
        if len(entries) != 1:
            raise CompareError('BASELINE_PATH_MISSING', 'Explicit path has no unique baseline blob.')
        identity, stored_path = entries[0].split(b'\t', 1)
        base_mode, kind, base_oid = identity.decode('ascii').split()
        if stored_path.decode('utf-8') != path or kind != 'blob' or base_mode not in ('100644', '100755'):
            raise CompareError('NONREGULAR_BASELINE_PATH', 'Baseline must name one regular blob.')
        index_mode, index_oid = repo.index_entry(path)
        baseline = repo.git('cat-file', 'blob', base_oid)
        indexed = repo.git('cat-file', 'blob', index_oid)
        attrs = repo.attributes(path)
        settings = repo.settings()
        working = repo.working_bytes(path)
        result.update(baseline=dict(commit=commit, blobOid=base_oid, mode=base_mode, rawSha256=sha(baseline)), index=dict(blobOid=index_oid, mode=index_mode, rawSha256=sha(indexed)), worktree=dict(rawSha256=sha(working), bytes=len(working)), attributes=attrs, gitSettings=settings)
        base_pointer, index_pointer = pointer(baseline), pointer(indexed)
        if attrs['filter'] == 'lfs' or base_pointer or index_pointer:
            if attrs['filter'] != 'lfs' or attrs['text'] != 'unset' or attrs['working-tree-encoding'] != 'unspecified':
                raise CompareError('LFS_ATTRIBUTES_UNSAFE', 'LFS requires current filter=lfs -text without working-tree encoding.')
            if not base_pointer or not index_pointer:
                raise CompareError('LFS_POINTER_REQUIRED', 'Both committed and indexed LFS identities must be valid pointers.')
            if pointer(working):
                raise CompareError('LFS_CONTENT_UNAVAILABLE', 'Working file is a pointer; real LFS bytes are required.')
            current = dict(sha256=sha(working), bytes=len(working))
            result['mode'] = 'lfs-real-content'
            result['baseline']['content'] = base_pointer
            result['index']['content'] = index_pointer
            result['worktree']['content'] = current
            matches = dict(baselineIndex=base_pointer==index_pointer, baselineWorktree=base_pointer==current, indexWorktree=index_pointer==current)
        else:
            if attrs['filter'] not in ('unspecified', 'unset'):
                raise CompareError('EXTERNAL_CLEAN_FILTER_UNSUPPORTED', 'External clean filters are not executed by this read-only entry: ' + attrs['filter'])
            clean_oid = repo.git('hash-object', '--stdin', '--path=' + path, data=working).decode('ascii').strip()
            result['mode'] = 'git-clean'
            result['worktree']['gitCleanOid'] = clean_oid
            matches = dict(baselineIndex=base_oid==index_oid, baselineWorktree=base_oid==clean_oid, indexWorktree=index_oid==clean_oid)
        matches['baselineIndexMode'] = base_mode == index_mode
        result['matches'] = matches
        if repo.index_entry(path)!=(index_mode,index_oid) or repo.attributes(path)!=attrs or repo.settings()!=settings or repo.working_bytes(path)!=working:
            raise CompareError('CONCURRENT_CHANGE', 'Index/attributes/settings/worktree changed during comparison.')
        if not all(matches.values()):
            raise CompareError('CONTENT_DIFFERENT', 'Baseline, index or current worktree content/mode differs.')
        result.update(status='PASS', reason='EQUIVALENT')
    except (CompareError, OSError, ValueError, subprocess.SubprocessError) as error:
        result.update(reason=getattr(error,'code','READ_UNAVAILABLE'), diagnostic=str(error))
    return result

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', required=True)
    parser.add_argument('--baseline', required=True, help='Full existing commit OID; no implicit HEAD baseline')
    parser.add_argument('paths', nargs='+', help='Explicit repository-relative paths, no glob or directory scan')
    args=parser.parse_args(argv)
    report=dict(schemaVersion=1, baseline=args.baseline, readOnly=True, results=[], success=False)
    try:
        if not re.fullmatch(r'(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})', args.baseline):
            raise CompareError('EXPLICIT_COMMIT_REQUIRED', 'Baseline must be a full existing commit OID.')
        repo=Repository(args.repo)
        commit=repo.git('rev-parse','--verify','--end-of-options',args.baseline+'^{commit}').decode('ascii').strip()
        if commit.lower()!=args.baseline.lower():
            raise CompareError('COMMIT_IDENTITY_MISMATCH','Baseline must be the commit itself, not a tag object.')
        report.update(repository=str(repo.root),baseline=commit)
        report['results']=[compare(repo,commit,path) for path in args.paths]
        report['success']=all(x['status']=='PASS' for x in report['results'])
    except (CompareError, OSError, ValueError, subprocess.SubprocessError) as error:
        report.update(reason=getattr(error,'code','READ_UNAVAILABLE'),diagnostic=str(error))
    print(json.dumps(report,ensure_ascii=True,indent=2))
    return 0 if report['success'] else 1

if __name__=='__main__':
    raise SystemExit(main())
