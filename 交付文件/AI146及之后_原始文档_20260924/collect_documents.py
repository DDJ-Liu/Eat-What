import os,re,json,datetime
from pathlib import Path
ROOT=Path(r"D:/GameProject/Eat-What")
OUT=ROOT/"交付文件"/"AI146及之后_原始文档_20260924"
OUT.mkdir(parents=True,exist_ok=True)
DOC={".md",".txt",".log",".json",".jsonl",".csv",".tsv",".xml",".html",".htm",".pdf",".doc",".docx",".xls",".xlsx",".ods",".rtf",".yaml",".yml",".diff",".patch"}
PRUNE={".git","Library","Temp","obj","bin","__pycache__",".batch-workspace","node_modules"}
q=json.loads((ROOT/".ai-workspace/runtime/queue-state.json").read_text(encoding="utf-8-sig"))
tasks=[t for t in q["tasks"] if int(t.get("sequence",t["id"].split("-")[-1]))>=146]
ids={int(t.get("sequence",t["id"].split("-")[-1])) for t in tasks}
def has_id(s):
    return any(int(m.group(1)) in ids for m in re.finditer(r"(?<![A-Za-z0-9])AI[-_ ]*0*(\d{3,})(?!\d)",s,re.I))
selected={}
def add(p,reason):
    p=Path(p)
    if not p.is_absolute(): p=ROOT/p
    if not p.is_file() or p.is_symlink(): return
    rel=p.relative_to(ROOT).as_posix()
    if rel.startswith("交付文件/"): return
    selected.setdefault(rel,set()).add(reason)
def walk(p):
    p=ROOT/p if not Path(p).is_absolute() else Path(p)
    if not p.exists(): return
    for d,ds,fs in os.walk(p):
        ds[:]=[n for n in ds if n not in PRUNE and not (Path(d)/n).is_symlink()]
        for f in fs:
            x=Path(d)/f
            if not x.is_symlink(): yield x
def tree(p,reason,docs=False):
    for x in walk(p):
        if not docs or x.suffix.lower() in DOC: add(x,reason)
for sub in ["U1-PRE","U1-PRE-R1","DATA-VISIBLE"]:
    tree(".ai-workspace/outputs/"+sub,"完整任务输出目录")
for sub in ["F-alpha_启动_20260922","U1-PRE-20260923","U1-PRE-R1-20260923","DATA-VISIBLE-20260923"]:
    tree(".ai-workspace/outputs/control/"+sub,"完整控制台中间与接力记录")
for x in walk(".ai-workspace/outputs"):
    if has_id(x.relative_to(ROOT).as_posix()): add(x,"AI146及之后编号匹配")
for x in walk(".ai-workspace/inputs"):
    rel=x.relative_to(ROOT).as_posix()
    if "/art/" not in rel and not x.name.startswith("ART车道"): add(x,"任务输入与设计原件")
for x in (ROOT/".ai-workspace").glob("*.md"): add(x,"工作流及任务入口原件")
for sub in ["protocol","pitfalls","toolkit","workers","templates","MIG63","design","archive/2026-09-23"]:
    tree(".ai-workspace/"+sub,"关联规则/迁移/设计/本轮归档文档",docs=True)
for p in ["AGENTS.md","CODEBASE_MAP.md","项目整体阅读理解与推进基线_2026-08-14.md"]:
    add(p,"项目共同文档原件")
for p in ["queue-state.json","worker-state.json","mode-state.json","usage-202609.jsonl"]:
    add(".ai-workspace/runtime/"+p,"完整原始状态快照（不裁剪历史）")
for p in ["DevTools/Fonts","DevTools/Workflow","DevTools/ShortCycle","DevTools/DataTables",
          "DevTools/MIG63","DevTools/GitHooks","组件说明文档","DataTables"]:
    for x in walk(p):
        rel=x.relative_to(ROOT).as_posix()
        if x.suffix.lower() in {".md",".pdf",".docx",".xlsx",".csv",".jsonl"} and (
            x.suffix.lower() in {".md",".pdf",".docx"} or has_id(rel)):
            add(x,"关联工具/配置说明与验证记录")
missing=[]
for t in tasks:
    result=t.get("result") or {}
    artifact_paths=[]
    for raw in result.get("artifacts",[]):
        if not isinstance(raw,str): continue
        if (ROOT/raw).exists(): artifact_paths.append(raw)
        else:
            parts=re.findall(r'"([^"\n]+)"|\x27([^\x27\n]+)\x27',raw)
            artifact_paths.extend([a or b for a,b in parts] if parts else [raw])
    for p in artifact_paths:
        if not isinstance(p,str): continue
        pp=ROOT/p
        if not pp.exists():
            missing.append({"taskId":t["id"],"artifact":p})
            continue
        if pp.is_dir():
            if p.replace("\\","/").startswith(".ai-workspace/outputs/"):
                tree(pp,"任务结算直接列出的输出目录")
            else: tree(pp,"任务结算列出目录内的原文档",docs=True)
        elif pp.suffix.lower() in DOC or p.replace("\\","/").startswith(".ai-workspace/outputs/"):
            add(pp,"任务结算直接列出的文档/证据")
for _ in range(3):
    before=len(selected)
    for rel in list(selected):
        if Path(rel).suffix.lower()!=".md": continue
        p=ROOT/rel
        text=p.read_text(encoding="utf-8-sig",errors="replace")
        for raw in re.findall(r"\]\(([^)\n]+)\)",text):
            raw=raw.strip().strip("<>").split("#")[0]
            if not raw or re.match(r"[a-z]+://",raw,re.I): continue
            raw=re.sub(r":\d+$","",raw).replace("%20"," ")
            candidates=[p.parent/raw,ROOT/raw]
            for cand in candidates:
                try: cand=cand.resolve(); cand.relative_to(ROOT.resolve())
                except (ValueError,OSError): continue
                if cand.is_file() and cand.suffix.lower() in DOC|{".png",".jpg",".jpeg",".webp",".svg"}:
                    add(cand,"原文档本地链接附件"); break
    if len(selected)==before: break
for rel in list(selected):
    if Path(rel).suffix.lower()!=".md": continue
    txt=(ROOT/rel).read_text(encoding="utf-8-sig",errors="replace")
    for raw in re.findall(chr(96)+r"([^"+chr(96)+r"\n]+)"+chr(96),txt):
        raw=raw.strip().replace("\\","/")
        if len(raw)>400: continue
        if not raw.startswith((".ai-workspace/","DevTools/","组件说明文档/","DataTables/")): continue
        pp=ROOT/raw
        if pp.is_file() and (pp.suffix.lower() in DOC or raw.startswith(".ai-workspace/outputs/")):
            add(pp,"原文档精确点名附件")
        elif pp.is_dir() and raw.startswith(".ai-workspace/outputs/") and has_id(rel):
            tree(pp,"具名任务报告精确点名的证据目录")
rows=[{"path":p,"size":(ROOT/p).stat().st_size,"reasons":sorted(r)} for p,r in sorted(selected.items())]
inventory={"createdAt":datetime.datetime.now().astimezone().isoformat(),"taskIds":[t["id"] for t in tasks],
 "files":rows,"missingOriginalArtifactPaths":missing,
 "preservation":"原文件逐字节打包，保持项目内相对路径；共同文档和状态文件保留完整原件，未裁剪旧历史。",
 "selection":"AI146及之后任务输出、控制中间证据、任务卡/输入/审批/设计、关联工具说明、原始日志/截图/清单和状态。非任务Unity缓存与隔离工程副本不打包。"}
(OUT/"selection.json").write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding="utf-8")
summary={"tasks":len(tasks),"first":tasks[0]["id"],"last":tasks[-1]["id"],"files":len(rows),"sourceBytes":sum(x["size"] for x in rows),
 "missingOriginalArtifactPaths":missing,"largest":sorted(rows,key=lambda r:r["size"],reverse=True)[:8],
 "namedMarkdownCounts":{t["id"]:sum(1 for r in rows if t["id"] in r["path"] and Path(r["path"]).suffix.lower()==".md") for t in tasks}}
(OUT/"inventory-check.json").write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps({k:v for k,v in summary.items() if k!='largest'},ensure_ascii=True))
