"""Strict reader for the Unity YAML subset emitted by the actual CK01 generated assets.
Read-only fixture acquisition, never an importer or replacement for AssetDatabase.
"""
from pathlib import Path
import argparse, json, re, struct

def load_unity(text):
    lines=[]
    for raw in text.splitlines():
        if not raw.strip() or raw.startswith(('%','---')):continue
        lines.append((len(raw)-len(raw.lstrip(' ')),raw.strip()))
    def scalar(value,i):
        if value.startswith('"'):
            while True:
                try:return json.loads(value),i
                except json.JSONDecodeError:
                    if i>=len(lines):raise ValueError('Unterminated quoted Unity scalar')
                    next_value=lines[i][1];i+=1
                    value=value[:-1]+next_value if value.endswith('\\') else value+' '+next_value
        if value.startswith("'"):
            if not value.endswith("'"):raise ValueError('Unsupported multiline single quote')
            return value[1:-1].replace("''","'"),i
        if value=='[]':return [],i
        if value.startswith('{') and value.endswith('}'):
            out={}
            for pair in value[1:-1].split(','):
                if not pair.strip():continue
                key,raw=pair.split(':',1);out[key.strip()]=raw.strip()
            return out,i
        return re.split(r'\s+#',value,1)[0],i
    def mapping(i,indent,first=None):
        out={}
        def item(key,value,i):
            if key in out:raise ValueError('Duplicate YAML field '+key)
            if value:
                out[key],i=scalar(value,i)
            elif i<len(lines) and (lines[i][0]>indent or lines[i][0]==indent and lines[i][1].startswith('-')):
                out[key],i=block(i,lines[i][0])
            else:out[key]=''
            return i
        if first is not None:
            key,value=first.split(':',1);i=item(key,value.strip(),i)
        while i<len(lines) and lines[i][0]==indent and not lines[i][1].startswith('-'):
            text=lines[i][1]
            if ':' not in text:raise ValueError('Unsupported YAML mapping '+text)
            key,value=text.split(':',1);i+=1;i=item(key,value.strip(),i)
        return out,i
    def sequence(i,indent):
        out=[]
        while i<len(lines) and lines[i][0]==indent and lines[i][1].startswith('-'):
            text=lines[i][1][1:].strip();i+=1
            if text and not text.startswith(('{','"',"'")) and re.match(r'^[A-Za-z_][A-Za-z0-9_]*:',text):
                value,i=mapping(i,indent+2,text)
            elif text:value,i=scalar(text,i)
            elif i<len(lines) and lines[i][0]>indent:value,i=block(i,lines[i][0])
            else:value=None
            out.append(value)
        return out,i
    def block(i,indent):
        return sequence(i,indent) if lines[i][1].startswith('-') else mapping(i,indent)
    result,end=block(0,lines[0][0])
    if end!=len(lines):raise ValueError('Unconsumed Unity YAML at '+str(lines[end]))
    return result

def snapshot(root):
    scripts={}
    for folder in ['Assets/Scripts/DataBase/CookingData','Assets/Scripts/DataBase/Localization']:
        for source in (root/folder).glob('*.cs'):
            meta=Path(str(source)+'.meta')
            if not meta.exists():continue
            match=re.search(r'^guid:\s*(\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
            types=re.findall(r'public\s+(?:sealed\s+)?class\s+(\w+)\s*:\s*ScriptableObject',source.read_text(encoding='utf-8-sig'))
            if match and len(types)==1:scripts[match[1]]=types[0]
    catalog=None;assets=[];native=[]
    for path in sorted((root/'Assets/Generated/DataTables').rglob('*.asset')):
        text=path.read_text(encoding='utf-8-sig')
        guid=re.search(r'^guid:\s*(\w+)',Path(str(path)+'.meta').read_text(encoding='utf-8-sig'),re.M).group(1)
        if not re.search(r'^--- !u!114 ',text,re.M):native.append(str(path.relative_to(root)).replace('\\','/'));continue
        doc=load_unity(text)['MonoBehaviour'];script=doc['m_Script']['guid'];kind=scripts[script]
        record={'key':guid+':11400000','guid':guid,'path':str(path.relative_to(root)).replace('\\','/'),'type':kind,'name':doc['m_Name'],'fields':{k:v for k,v in doc.items() if not k.startswith('m_')}}
        if kind=='CK01GeneratedDataCatalog':catalog=record
        else:assets.append(record)
    if catalog is None:raise ValueError('No actual generated catalog')
    excluded={catalog['guid']}|{a['guid']for a in assets};refs=[]
    for name in ['Assets/Scenes/Cooking/ShortCycle_P0P1.unity','Assets/Scenes/CookingProcess.unity','Assets/Scenes/ToolTests/FridgeScrollLab.unity']:
        path=root/name
        if not path.exists():raise ValueError('Required scene missing: '+name)
        for number,line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(),1):
            for guid in re.findall(r'guid: ([0-9a-f]{32})',line):
                if guid in excluded:refs.append({'scene':name,'line':number,'guid':guid})
    return {'sceneReferences':refs,'catalog':catalog,'assets':assets,'nativeAssets':native,'source':'Actual serialized Unity assets and .meta GUIDs read-only; not CSV reconstruction'}
if __name__=='__main__':
    args=argparse.ArgumentParser();args.add_argument('--project-root',required=True);args.add_argument('--output',required=True);v=args.parse_args()
    result=snapshot(Path(v.project_root));out=Path(v.output);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'dataSOs':len(result['assets']),'catalogSOs':1,'nativeAssets':len(result['nativeAssets']),'output':str(out)}))
