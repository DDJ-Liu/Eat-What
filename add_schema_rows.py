import re, copy, shutil
from pathlib import Path
import openpyxl
from openpyxl.comments import Comment

src = Path(r'Assets/DataTables/DataTables.xlsx')
out = Path(r'Assets/DataTables/DataTables_with_schema.xlsx')
schema_path = Path(r'C:/Users/wangt/Desktop/EatWhat/CK01-B_配表Schema_v2_5_3.md')

# Parse the first 4-column schema table after each T-section.
text = schema_path.read_text(encoding='utf-8')
sections = {}
for m in re.finditer(r'^###\s+T(\d+)\s+([^\n]+)', text, re.M):
    tid = m.group(1)
    start = m.end()
    nxt = re.search(r'^###\s+', text[start:], re.M)
    block = text[start:start + (nxt.start() if nxt else len(text))]
    tm = re.search(r'^\|\s*列名\s*\|\s*类型\s*\|\s*必填\s*\|\s*说明\s*\|\s*$', block, re.M)
    if not tm:
        continue
    lines = block[tm.end():].splitlines()
    rows=[]
    for line in lines:
        if not line.startswith('|'):
            if rows:
                break
            continue
        if re.match(r'^\|\s*:?-+', line):
            continue
        parts=[p.strip() for p in line.strip().strip('|').split('|')]
        if len(parts)>=4:
            rows.append(parts[:4])
    if rows:
        sections[tid]=rows

sheet_tid = {
    'Recipes':'1','RecipeSlots':'2','RecipeSteps':'3','Items':'4','Tags':'6','Tools':'7',
    'ProcessActions':'11','ProcessingRecords':'12','RecipeVariants':'13','KitchenAreas':'14',
    'InitialInventory':'15','Characters':'16','Dialogues':'17','DialogueLines':'18','CGs':'19',
    'Scenes':'20','SceneStates':'21','SceneStatePlacements':'22','AmbientPools':'23',
    'Localization':'9','FridgeCapacityLevels':'10'
}

manual = {
 'CookingGameConfig': {
   'key': ('enum_key','required','配置键；只能使用 schema 规定的键名'),
   'value': ('string','required','配置值；按 key 对应的标量类型填写'),
   'comment': ('string','optional','人读备注；导表器忽略')},
 'KitchenAreas': {
   'id': ('string_id','required','厨房分区主键；沿用试点 kebab-case'),
   'assetName': ('string','required','对应厨房分区资源/逻辑名称'),
   'areaName': ('string','required','分区显示或说明名称'),
   'containerTags': ('string_list_pipe','required','场景承载物标签集合；使用 | 分隔'),
   'core_facility': ('ref:T7','required','核心固定设施；必须为 facility 类型工具'),
   'preset_tools': ('list<ref:T7>_pipe','optional','已废弃；留空，不参与授予与校验'),
   'allowed_actions': ('list<ref:T11>_pipe','required','本区允许动作；使用 | 分隔，不得重复'),
   'allowed_carriers': ('list<ref:T7>_pipe','required','本区承载物约束集合；使用 | 分隔，必须含 core_facility'),
   'is_unlocked_default': ('bool','required','demo 初始解锁状态'),
   'unlock_condition': ('string','optional','预留的解锁条件'),
   'comment': ('string','optional','人读备注')},
 'FridgeCapacityLevels': {
   'fcap_id': ('string_id','required','容量档位主键，格式如 fcap_lv1'),
   'level_index': ('int','required','档序；从 1 起连续且唯一'),
   'capacity': ('int','required','该档容量；随档严格递增'),
   'upgrade_condition': ('string','optional','预留的升级代价/条件'),
   'comment': ('string','optional','人读备注')},
}

def norm_type(t):
    t=t.strip()
    t=t.replace('string(ID)','string_id').replace('string(key)','string_key')
    t=t.replace('ref列表→','list<ref:').replace('ref→','ref:')
    t=t.replace('（8 列）','').replace('（6列）','').replace('（6 列）','')
    t=t.replace('enum(曲线)','enum_curve').replace('覆盖集','override_set').replace('命令','command')
    t=t.replace('sprite','sprite').replace('prefab','prefab')
    t=t.replace('float','float').replace('int','int').replace('bool','bool')
    t=t.replace('loc_key','loc_key').replace('string','string')
    # close list marker for common ref-list text
    if t.startswith('list<ref:') and not t.endswith('>'):
        t += '>'
    return t

def req_token(req):
    req=req.strip()
    if req in ('✓','必填','01必填') or '必填' in req and '条件' not in req:
        return 'required'
    if '条件' in req:
        return 'conditional'
    if '允空' in req or req=='—':
        return 'optional'
    return 'optional'

wb = openpyxl.load_workbook(src)
# Preserve workbook as much as openpyxl supports; add schema rows directly before current data.
for ws in wb.worksheets:
    headers=[ws.cell(1,c).value for c in range(1, ws.max_column+1)]
    ws.insert_rows(2, amount=3)
    info={}
    if ws.title in manual:
        info=manual[ws.title]
    else:
        rows=sections.get(sheet_tid.get(ws.title,''), [])
        for col, typ, req, desc in rows:
            # Expand grouped schema names into matching workbook columns.
            for h in headers:
                if h is None: continue
                if h==col or (('~' in col or ' ~ ' in col) and h in [x.strip() for x in col.replace(' ','').split('~')]):
                    info[h]=(norm_type(typ), req_token(req), desc)
        # Special grouped columns and workbook naming differences
        if ws.title=='Dialogues':
            for h in headers:
                if h and h not in info:
                    if h.startswith('cast_') and h.endswith('_station'): info[h]=('string','optional','引用演出配置里的站位名；留空按出场人数取默认站位')
                    elif h.startswith('cast_') and h.endswith(('_pos_x','_pos_y')): info[h]=('float','optional','本段角色显示锚点位置；相对屏幕底边中点的像素坐标')
                    elif h.startswith('cast_') and h.endswith('_scale'): info[h]=('float','optional','本段角色额外缩放；与角色表缩放相乘')
                    elif h.startswith('cast_') and h.endswith('_order'): info[h]=('int','optional','立绘前后叠放次序；数字大者在前')
        if ws.title=='RecipeSlots':
            for h in headers:
                if h and re.match(r'sub_\d+_count$',str(h)): info[h]=('int','conditional','对应替代品需求数量；替代品非空时必填且 >=1')
                elif h and re.match(r'sub_\d+$',str(h)): info[h]=('ref:T4','optional','替代品池；从 sub_01 起连续填写')
        if ws.title=='ProcessingRecords':
            for h in headers:
                if h and re.match(r'input_\d+$',str(h)): info[h]=('ref:T4','conditional' if h=='input_01' else 'optional','输入物多重集；从 input_01 连续填写，列序无逻辑意义')
        if ws.title=='CGs':
            for h in headers:
                if h in ('keep_box','keep_portraits','keep_name'): info[h]=('bool','optional','CG 显示期间是否保留对应界面元素')
        if ws.title=='SceneStatePlacements':
            for h in headers:
                if h=='anchor_id': info[h]=('string_id','conditional','角色站位锚点；角色无预摆实例时必填')
                elif h=='facing': info[h]=('enum','optional','角色朝向：left 或 right')
                elif h=='marker': info[h]=('enum','optional','头顶标记：none / talk / story')
        if ws.title=='AmbientPools':
            info['dialogue_ids']=('list<ref:T17>','required','气泡类对话 ID 列表；使用 ; 分隔，至少一条')
    # Fallback for every unmatched column, keeping code-readable metadata.
    for h in headers:
        if h is None: continue
        if h not in info:
            if h=='comment': info[h]=('string','optional','人读备注；导表器忽略')
            elif h.endswith('_key') or h in ('name_key','text_key'): info[h]=('loc_key','required','本地化词条 key；必须存在于 Localization')
            elif h.endswith('_sprite'): info[h]=('sprite','optional','资源文件名；无扩展名、无路径')
            elif h.endswith('_prefab'): info[h]=('prefab','optional','Prefab 文件名；无扩展名、无路径')
            else: info[h]=('string','optional','按字段名约定填写；详见 CK01-B Schema v2.5.3')
    # Fill the three metadata rows, copying header style and dimensions only.
    for c,h in enumerate(headers, start=1):
        cell=ws.cell(1,c)
        for r in (2,3,4):
            target=ws.cell(r,c)
            target._style=copy.copy(cell._style)
            target.number_format='General'
            target.alignment=copy.copy(cell.alignment)
            target.font=copy.copy(cell.font)
            target.fill=copy.copy(cell.fill)
            target.border=copy.copy(cell.border)
            target.protection=copy.copy(cell.protection)
        if h is None:
            continue
        typ, req, desc = info[h]
        ws.cell(2,c).value=typ
        ws.cell(3,c).value=req
        ws.cell(4,c).value=desc
        # Keep metadata rows visually distinguishable and machine-readable.
        ws.cell(2,c).font=copy.copy(cell.font); ws.cell(2,c).font=copy.copy(ws.cell(2,c).font); ws.cell(2,c).font=ws.cell(2,c).font.copy(bold=True)
        ws.cell(3,c).font=ws.cell(3,c).font.copy(bold=True)
    ws.freeze_panes='A5'

wb.save(out)
print(out)
