"""Real small-repository/dual-clone regression for compare-git-content; no project Git writes."""
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
ENTRY=HERE/'compare-git-content.py'
results=[]
base=Path(os.environ.get('EATWHAT_GIT_CONTENT_REPORT_ROOT',tempfile.gettempdir()))
base.mkdir(parents=True,exist_ok=True)
run=Path(tempfile.mkdtemp(prefix='git-content-',dir=base))
# Ignore external machine hook/filter configuration only in fixture Git processes.
env=os.environ.copy()
env.update(GIT_CONFIG_NOSYSTEM='1',GIT_CONFIG_GLOBAL=str(run/'empty-global.gitconfig'),GIT_TERMINAL_PROMPT='0',GIT_LFS_SKIP_SMUDGE='1')
(run/'empty-global.gitconfig').write_text('',encoding='utf-8')

def git(repo,*args,data=None):
    call=subprocess.run(['git','-C',str(repo),*args],input=data,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env,check=True)
    return call.stdout.decode('utf-8').strip()

def check(value,name,details=None):
    if not value:raise AssertionError(name+': '+str(details))
    results.append(dict(case=name,passed=True,details=details))

def control_state(repo):
    # Tiny disposable repository only, including object inventory; never enumerate production .git.
    return {str(p.relative_to(repo)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (repo/'.git').rglob('*') if p.is_file() and p.name!='index.lock'}

def compare(repo,commit,paths,expected,name,reason=None,process_env=None):
    before=control_state(repo)
    call=subprocess.run([sys.executable,str(ENTRY),'--repo',str(repo),'--baseline',commit,*paths],stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env if process_env is None else process_env)
    value=json.loads(call.stdout.decode('utf-8'))
    check(all(byte<128 for byte in call.stdout),name+' emits locale-independent ASCII JSON')
    check((call.returncode==0)==expected and value['success']==expected,name,value)
    check(control_state(repo)==before,name+' leaves all tiny-repo Git files unchanged')
    if reason:check(any(x.get('reason')==reason for x in value['results']) or value.get('reason')==reason,name+' precise fail reason',reason)
    return value

def stage_blob(repo,path,content,mode='100644'):
    oid=git(repo,'hash-object','-w','--stdin',data=content)
    git(repo,'update-index','--add','--cacheinfo',mode+','+oid+','+path)
    return oid

def lfs(content):
    return ('version https://git-lfs.github.com/spec/v1\noid sha256:'+hashlib.sha256(content).hexdigest()+'\nsize '+str(len(content))+'\n').encode()

seed=run/'seed';seed.mkdir();git(seed,'init','-b','codex/fixture')
git(seed,'config','user.name','Isolated fixture');git(seed,'config','user.email','fixture@example.invalid')
git(seed,'config','core.autocrlf','false');git(seed,'config','core.safecrlf','false');git(seed,'config','core.eol','lf')
attrs=b'*.txt text\nident.txt ident\nencoded.txt text working-tree-encoding=UTF-16LE eol=lf\n*.raw -text\nfont.asset filter=lfs diff=lfs merge=lfs -text\n'
(seed/'.gitattributes').write_bytes(attrs)
(seed/'sample.txt').write_bytes(b'field: original\nsecond: same\n')
(seed/'ident.txt').write_bytes(b'$Id$\nfield: original\n')
(seed/'encoded.txt').write_bytes('field: original\n'.encode('utf-16le'))
(seed/'bytes.raw').write_bytes(b'exact\nbytes\n')
(seed/'unreadable.txt').write_bytes(b'readable initially\n')
cn_path='中文配置.txt'
(seed/cn_path).write_bytes('字段: 保持完整\n'.encode('utf-8'))
font=b'isolated real font bytes\r\nkeep exact\x00\x01'
(seed/'font.asset').write_bytes(font)
git(seed,'add','.gitattributes','sample.txt','ident.txt','encoded.txt','bytes.raw','unreadable.txt',cn_path)
stage_blob(seed,'font.asset',lfs(font))
git(seed,'-c','core.hooksPath=','commit','-m','synthetic baseline only')
commit=git(seed,'rev-parse','HEAD');clones=[]
checkout_evidence=[]
for name,autocrlf in [('lf','false'),('crlf','true')]:
    clone=run/name
    call=subprocess.run(['git','clone','--no-checkout','--no-hardlinks','--no-local',str(seed),str(clone)],stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env,check=True)
    # Set both rules before the first real checkout. autocrlf=false alone does
    # not request LF on a platform whose native EOL is CRLF.
    git(clone,'config','core.autocrlf',autocrlf);git(clone,'config','core.safecrlf','false');git(clone,'config','core.eol',name)
    git(clone,'checkout','--detach',commit)
    (clone/'font.asset').write_bytes(font)
    check(git(clone,'rev-parse','HEAD')==commit,'same existing commit in '+name)
    sample=(clone/'sample.txt').read_bytes()
    checkout_evidence.append(dict(clone=name,commit=commit,configBeforeFirstCheckout=dict(autocrlf=autocrlf,eol=name),observedConfig=dict(autocrlf=git(clone,'config','--get','core.autocrlf'),eol=git(clone,'config','--get','core.eol')),attributes=git(clone,'check-attr','text','eol','--','sample.txt'),rawSha256=hashlib.sha256(sample).hexdigest(),crlf=sample.count(b'\r\n'),lf=sample.count(b'\n')))
    clones.append(clone)
(run/'checkout-evidence.json').write_text(json.dumps(checkout_evidence,indent=2)+'\n',encoding='utf-8')
check(b'\r\n' not in (clones[0]/'sample.txt').read_bytes() and b'\r\n' in (clones[1]/'sample.txt').read_bytes(),'actual different LF/CRLF checkout bytes')
reports=[]
for repo in clones:
    label=repo.name
    original=(repo/'sample.txt').read_bytes()
    report=compare(repo,commit,['sample.txt','ident.txt','encoded.txt','bytes.raw','font.asset'],True,label+' explicit baseline/index/worktree and built-in clean rules')
    reports.append(report)
    check(report['results'][0]['baseline']['blobOid']==report['results'][0]['index']['blobOid']==report['results'][0]['worktree']['gitCleanOid'],label+' three Git-normalized identities same')
    check(report['results'][1]['worktree']['rawSha256']!=report['results'][1]['baseline']['rawSha256'],label+' actual ident expansion normalized by Git')
    check(report['results'][4]['worktree']['content']['sha256']==hashlib.sha256(font).hexdigest(),label+' LFS compares real bytes not pointer blob')
    # Actual non-ASCII path/diagnostic under localized and UTF8 child IO. This
    # never changes host locale/env and does not weaken strict UTF8 decoding.
    cn_file=repo/cn_path;cn_bytes=cn_file.read_bytes()
    for encoding in ('gbk','utf-8'):
        child_io=env.copy();child_io['PYTHONIOENCODING']=encoding
        value=compare(repo,commit,[cn_path],True,label+' Chinese success protocol '+encoding,process_env=child_io)
        check(value['results'][0]['path']==cn_path,label+' Chinese success path retained '+encoding)
        cn_file.unlink()
        try:cn_file.lstat()
        except OSError as error:expected_diagnostic=str(error)
        else:raise AssertionError('Missing Chinese fixture must raise an OS error')
        value=compare(repo,commit,[cn_path],False,label+' Chinese missing protocol '+encoding,'WORKTREE_UNREADABLE',process_env=child_io)
        check(value['results'][0]['path']==cn_path and value['results'][0]['diagnostic']==expected_diagnostic and cn_path in expected_diagnostic,label+' exact localized diagnostic and path retained '+encoding)
        cn_file.write_bytes(cn_bytes)
    (repo/'sample.txt').write_bytes(original.replace(b'original',b'changed'))
    changed=compare(repo,commit,['sample.txt'],False,label+' unstaged real field change fails','CONTENT_DIFFERENT')
    check(changed['results'][0]['matches']['baselineIndex'] and not changed['results'][0]['matches']['indexWorktree'],label+' unstaged change cannot hide behind unchanged index')
    git(repo,'add','sample.txt')
    compare(repo,commit,['sample.txt'],False,label+' staged real field change fails','CONTENT_DIFFERENT')
    (repo/'sample.txt').write_bytes(original)
    compare(repo,commit,['sample.txt'],False,label+' baseline worktree same but changed index fails','CONTENT_DIFFERENT')
    git(repo,'reset','--','sample.txt')
    (repo/'sample.txt').unlink()
    compare(repo,commit,['sample.txt'],False,label+' missing worktree fails','WORKTREE_UNREADABLE')
    (repo/'sample.txt').write_bytes(original)
    compare(repo,commit,['missing.txt'],False,label+' missing baseline fails','BASELINE_PATH_MISSING')
    git(repo,'update-index','--force-remove','sample.txt')
    compare(repo,commit,['sample.txt'],False,label+' missing index fails','INDEX_PATH_MISSING')
    git(repo,'reset','--','sample.txt')
    raw=(repo/'bytes.raw').read_bytes();(repo/'bytes.raw').write_bytes(raw.replace(b'\n',b'\r\n'))
    compare(repo,commit,['bytes.raw'],False,label+' -text raw bytes are never line-normalized','CONTENT_DIFFERENT')
    (repo/'bytes.raw').write_bytes(raw)
    (repo/'font.asset').write_bytes(lfs(font))
    compare(repo,commit,['font.asset'],False,label+' LFS pointer is not actual font','LFS_CONTENT_UNAVAILABLE')
    (repo/'font.asset').write_bytes(font+b'drift')
    compare(repo,commit,['font.asset'],False,label+' LFS real working bytes drift fails','CONTENT_DIFFERENT')
    (repo/'font.asset').write_bytes(font)
    stage_blob(repo,'font.asset',lfs(font+b'index drift'))
    compare(repo,commit,['font.asset'],False,label+' LFS staged OID drift fails','CONTENT_DIFFERENT')
    git(repo,'reset','--','font.asset')
check(reports[0]['results'][0]['worktree']['rawSha256']!=reports[1]['results'][0]['worktree']['rawSha256'],'raw backup SHA differs across clones')
check([x['matches'] for x in reports[0]['results']]==[x['matches'] for x in reports[1]['results']],'same commit different clones identical passing decisions')
repo=clones[0]
# Synthetic indexed config boundary cases only alter child-process dictionaries.
# Normal dual-clone cases above retained the real inherited configuration unchanged.
def child_config(entries, count):
    child={key:value for key,value in env.items() if key != 'GIT_CONFIG_COUNT' and not key.startswith(('GIT_CONFIG_KEY_', 'GIT_CONFIG_VALUE_'))}
    if count is not None:child['GIT_CONFIG_COUNT']=count
    child.update(entries)
    return child
safe=child_config({'GIT_CONFIG_KEY_0':'safe.directory','GIT_CONFIG_VALUE_0':str(repo)},'1')
compare(repo,commit,['sample.txt'],True,'synthetic complete safe.directory indexed config passes unchanged',process_env=safe)
compare(repo,commit,['sample.txt'],True,'explicit zero indexed config passes',process_env=child_config({},'0'))
for name,entries,count in [
 ('bad count',{},'invalid'),('negative count',{},'-1'),
 ('missing key',{'GIT_CONFIG_VALUE_0':'synthetic'},'1'),
 ('missing value',{'GIT_CONFIG_KEY_0':'safe.directory'},'1'),
 ('orphan entry',{'GIT_CONFIG_KEY_0':'safe.directory'},None),
 ('out of range',{'GIT_CONFIG_KEY_0':'safe.directory','GIT_CONFIG_VALUE_0':'synthetic'},'0')]:
    compare(repo,commit,['sample.txt'],False,'indexed config '+name+' rejected','GIT_INDEXED_CONFIG_INVALID',process_env=child_config(entries,count))
for key in ('core.autocrlf','core.worktree','filter.custom.clean'):
    unknown=child_config({'GIT_CONFIG_KEY_0':key,'GIT_CONFIG_VALUE_0':'synthetic'},'1')
    compare(repo,commit,['sample.txt'],False,'unknown indexed '+key+' rejected','GIT_INDEXED_CONFIG_UNSUPPORTED',process_env=unknown)
for key in ('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR','GIT_OBJECT_DIRECTORY','GIT_ALTERNATE_OBJECT_DIRECTORIES'):
    redirected=safe.copy();redirected[key]=str(repo)
    compare(repo,commit,['sample.txt'],False,'actual '+key+' redirect rejected','GIT_TARGET_OVERRIDE',process_env=redirected)
compare(repo,'HEAD',['sample.txt'],False,'implicit or moving HEAD baseline rejected','EXPLICIT_COMMIT_REQUIRED')
compare(repo,'f'*40,['sample.txt'],False,'nonexistent commit rejected','GIT_READ_FAILED')
for path in ['../outside','/absolute','sample*','folder/../sample.txt','.git/config']:
    compare(repo,commit,[path],False,'path rejected '+path,'INVALID_EXPLICIT_PATH')
# Exercise actual stage 1/2/3 conflict, not a mocked index.
oid=git(repo,'rev-parse',commit+':sample.txt')
git(repo,'update-index','--force-remove','sample.txt')
records=''.join('100644 '+oid+' '+str(stage)+'\tsample.txt\n' for stage in (1,2,3))
git(repo,'update-index','--index-info',data=records.encode())
compare(repo,commit,['sample.txt'],False,'unmerged index fails closed','INDEX_UNMERGED')
git(repo,'reset','--','sample.txt')
# Do not execute an arbitrary configured clean program; current attributes still reported.
(repo/'.gitattributes').write_bytes(attrs+b'sample.txt filter=unknown-clean\n')
git(repo,'config','filter.unknown-clean.clean','not-a-real-program')
compare(repo,commit,['sample.txt'],False,'external clean filter rejected without executing','EXTERNAL_CLEAN_FILTER_UNSUPPORTED')
(repo/'.gitattributes').write_bytes(attrs)
if os.name=='nt':
    from ctypes import wintypes
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,wintypes.LPVOID,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
    kernel.CreateFileW.restype=wintypes.HANDLE
    kernel.CloseHandle.argtypes=[wintypes.HANDLE]
    handle=kernel.CreateFileW(str(repo/'unreadable.txt'),0x80000000,0,None,3,0,None)
    if handle==ctypes.c_void_p(-1).value:raise ctypes.WinError(ctypes.get_last_error())
    try:compare(repo,commit,['unreadable.txt'],False,'actual Windows deny-share unreadable fails','WORKTREE_UNREADABLE')
    finally:kernel.CloseHandle(handle)
else:
    # POSIX portability: make opening fail even when tests run with root privileges.
    import importlib.util
    from unittest.mock import patch
    spec=importlib.util.spec_from_file_location('git_content',ENTRY);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    with patch.object(Path,'read_bytes',side_effect=PermissionError('synthetic unreadable')):
        value=module.compare(module.Repository(repo),commit,'unreadable.txt')
    check(value['status']=='FAIL' and value['reason']=='WORKTREE_UNREADABLE','POSIX injected unreadable fails closed')
# Existing font guard and branch protections remain the same real source fixture.
font_run=subprocess.run([sys.executable,str(HERE/'test-font-guard.py')],stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env)
(run/'font-guard.log').write_bytes(font_run.stdout+font_run.stderr)
check(font_run.returncode==0,'original permanent font/branch hook regression passes',font_run.stderr.decode(errors='replace')[-400:])
font_results=json.loads(font_run.stdout.decode('utf-8'))
report=dict(success=True,assertionCount=len(results),fontGuardCases=font_results['count'],baselineCommit=commit,checkoutEvidence=checkout_evidence,fixtureRunDirectory=str(run),cloneDirectories=[str(p) for p in clones],productionGitWrites=0,results=results)
(run/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=True,indent=2))
