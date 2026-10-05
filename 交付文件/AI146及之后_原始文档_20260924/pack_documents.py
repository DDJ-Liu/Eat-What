import json,hashlib,zipfile,datetime,os
from pathlib import Path
ROOT=Path(r"D:/GameProject/Eat-What")
WORK=ROOT/"交付文件"/"AI146及之后_原始文档_20260924"
selection=json.loads((WORK/"selection.json").read_text(encoding="utf-8"))
stamp=datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
dest=ROOT/"交付文件"/("AI146-AI183_原始任务文档_"+stamp+".zip")
manifest={"packagedAt":datetime.datetime.now().astimezone().isoformat(),"taskIds":selection["taskIds"],
"preservation":selection["preservation"],"selection":selection["selection"],
"missingOriginalArtifactPaths":selection["missingOriginalArtifactPaths"],"files":[]}
with zipfile.ZipFile(dest,"x",compression=zipfile.ZIP_DEFLATED,compresslevel=6,allowZip64=True) as z:
    for r in selection["files"]:
        p=ROOT/r["path"]
        before=p.stat()
        data=p.read_bytes()
        after=p.stat()
        if before.st_size!=after.st_size or before.st_mtime_ns!=after.st_mtime_ns or len(data)!=after.st_size:
            raise RuntimeError("Source changed during read: "+r["path"])
        arc="Eat-What/"+r["path"]
        z.writestr(arc,data)
        manifest["files"].append({"path":r["path"],"zipEntry":arc,"bytes":len(data),
                                 "sha256":hashlib.sha256(data).hexdigest()})
    z.writestr("_打包清单.json",json.dumps(manifest,ensure_ascii=False,indent=2).encode("utf-8"))
with zipfile.ZipFile(dest,"r") as z:
    if len(z.namelist())!=len(set(z.namelist())): raise RuntimeError("Duplicate ZIP names")
    for item in manifest["files"]:
        data=z.read(item["zipEntry"])
        if hashlib.sha256(data).hexdigest()!=item["sha256"]: raise RuntimeError("Archive mismatch: "+item["path"])
        source_hash=hashlib.sha256((ROOT/item["path"]).read_bytes()).hexdigest()
        if source_hash!=item["sha256"]: raise RuntimeError("Source changed after packing: "+item["path"])
digest=hashlib.sha256(dest.read_bytes()).hexdigest()
dest.with_suffix(".zip.sha256").write_text(digest+"  "+dest.name+"\n",encoding="utf-8")
result={"archive":str(dest),"sha256":digest,"fileCount":len(manifest["files"]),
"archiveEntries":len(manifest["files"])+1,"originalBytes":sum(r["bytes"] for r in manifest["files"]),
"zipBytes":dest.stat().st_size,"verifiedArchiveAndSources":True,"taskCount":len(selection["taskIds"]),
"missingOriginalArtifactPaths":selection["missingOriginalArtifactPaths"]}
(WORK/"package-result.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps(result,ensure_ascii=False))

