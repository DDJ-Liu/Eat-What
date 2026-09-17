# 美术流水线（ART）· 每日资产审查日报模板 v1.0 + 车道规则（2026-09-08）

> 投放位置：`.ai-workspace/workers/ART.md`（车道规则）+ `.ai-workspace/templates/ART_DAILY_TEMPLATE.md`（日报模板）；CONTROL_CHAT 拆解规则增补见 §3。
> 日报编号：`ART-DAILY-YYYYMMDD`，不占 AI-xxxxxx 序列；产物目录 `.ai-workspace/outputs/art/daily/ART-DAILY-YYYYMMDD/`。

---

## 1. 车道规则（workers/ART.md）

1. 职责：`E:\EatWhat\美术资产` 的审查、同步区维护、差异与规范报告。**不进 Unity、不写 Assets、不改场景**；部署入引擎仍由 ENGINE_MCP 承担（dependsOn 本车道的同步任务或日报）。
2. 常驻工作流：工作日 00:00 触发每日审查。触发时若本车道有任务运行，排队等待其完成后执行；不抢占、不并行。
3. 编号：日报用 `ART-DAILY-YYYYMMDD`；只有用户/控制台显式下达的同步任务（J-T3 型）才领 AI-xxxxxx。
4. 基线：每次日报以上一份日报的 `manifest.json` 为比较基线；首日以最近一次 J 链 sync manifest（AI-000057 对应的 `art-sync-*.json`）为基线。
5. 必读：KNOWN_PITFALLS（P-019 PowerShell 编码、P-026 可见区）、总清单「导出规范与PSD分层规范」页（E1~E6 / P1~P6）、TOOLKIT_REGISTRY 不适用。
6. 报告六要素照旧；"工具登记影响"固定写"无"。
7. 触发接力：日报 §6 命中任何"挂起任务触发条件"时，控制台据此入队对应任务（不由本车道自行入队）。

---

## 2. 日报模板（ART_DAILY_TEMPLATE.md）

```markdown
# ART-DAILY-YYYYMMDD · 美术资产每日审查

- 审查时间：YYYY-MM-DD 00:00 +08:00（实际执行 HH:MM，若延后写延后原因：前序任务 AI-xxxxxx）
- 审查根：E:\EatWhat\美术资产
- 基线：ART-DAILY-YYYYMMDD（或 art-sync-*.json）
- 文件总数：N（PNG N1 / PSD N2 / 其它 N3 / 忽略 N4：Thumbs.db 等）
- 变更总数：新增 A / 修改 M / 删除 D / 改名 R（同 SHA 异名）/ 目录移动 V

## 1. 变更清单（文件级，按目录）
| 类型 | 相对路径 | 旧 SHA-8 | 新 SHA-8 | 旧尺寸 | 新尺寸 | 备注 |
|---|---|---|---|---|---|---|
| 新增 | UIUX/菜谱/xxx.png | — | 5f93e66b | — | 550×208 | |
| 修改 | 角色/厨具/冰箱猫/xxx.png | 07432879 | 29431a17 | 889×830 | 1478×1380 | |
| 删除 | … | … | — | … | — | |
| 改名 | 旧名 → 新名 | 同 | 同 | | | 视为改名不视为增删 |
（无变更时写"无变更"，仍出报告）

## 2. 尺寸与可见区回归（仅对本次新增/修改的 PNG）
| 相对路径 | 画布 | alpha 可见区 | 可见区占比 | 宽高比（可见区） | 上一版宽高比 | 变化% | 判定 |
|---|---|---|---|---|---|---|---|
| … | 1170×1638 | 1040×1458 | 89%/89% | 0.713 | 1.400 | 96.3% | 🔴 方向/比例变化 |
判定规则：宽高比变化 >5% 🔴；可见区占比与批次主流（≈83%）偏差 >10% 🟡（紧裁/留边不一致，E2）；画布最大边 >4096 🟡（E6 封顶）；纯黑/纯透明/尺寸 <8px 🔴（疑似坏图）。
可见区算法：alpha>0 像素的最小外接矩形（P-026），不用 sprite rect。

## 3. 命名与目录规范
| 相对路径 | 问题类型 | 说明 | 建议 |
|---|---|---|---|
| … | 含空格 / 含 `+` / 非 ASCII 文件名 / 前缀不符（应 ui_shicai_/ui_caipin_/ui_tag_/ui_gongju_/ui_caipu_/chuju_）/ 目录错位（如菜品图在菜谱目录）/ 重复 SHA（列全部同 SHA 文件）/ 画布不符（图标非 512） | | 请美术改名 / 移动 / 删除重复 |
（规则来源：总清单「导出规范」E1~E6、P1~P6）

## 4. 待产清单状态（固定表，每日刷新；由控制台维护条目，本车道只填状态）
| 待产项 | 期望文件名 | 今日状态 | 首次出现日期 |
|---|---|---|---|
| 转正素材 | ui_caipu_zhaopian_straight / tag_straight / toumingjiao_straight / kuang2_straight / kuang3_straight | 缺 5/5 | — |
| 冰箱猫拆分重导（E6 尺度） | Split 下 13 件 + zhutu | 缺 | — |
| ui_tag_* 族 | ui_tag_<tag_id>.png | 缺 | — |
| ui_gongju_* 族 | ui_gongju_<tool_id>.png（caidao 现为 chuju_ 前缀 217×257，不符） | 缺 | — |
| 封面图 CoverArt | ui_caipu_fengmian.png | 缺 | — |
| 第二只对白气泡 | chuju_bingxiangmao_qipao_<x>.png | 缺 | — |
| 四按钮 | OpenClueBoard / Escape / Tidy / EquipmentEntry | 缺 | — |
| UnknownCard | ui_caipu_pailide_unknown.png | 缺 | — |
| 缺图食材 | ui_shicai_fanqie / xiaocong / judan.png | 缺 | — |
| 命名待改源 | ui_caipu_kaishizuofan(+1) 正名恢复；qipao_gan  _ganma 去空格；tuya1~3 与 446 版菜品图移出 UIUX/菜谱 | 未处理 | — |

## 5. 同步区动作
- 本车道对同步区（.art-asset-sync/）执行：增量镜像 / 仅报告（默认仅报告；镜像需显式同步任务）
- 本次未镜像 / 已镜像 N 文件；manifest 路径：…

## 6. 挂起任务触发命中（控制台据此入队，本车道不入队）
| 触发条件 | 命中 | 对应挂起任务 |
|---|---|---|
| `_straight` 5 件全部出现 | 否/是 | C-T5b-X 转正素材替换（BLOCKED_ON_ART → 可入队；前置 ART 同步 + ENGINE 部署） |
| Split 13 件 + zhutu 重导且宽高与 shenti2 同尺度（±5%） | 否/是 | `_ScaleComp` 归 1（5 分钟 ENGINE 任务） |
| ui_tag_* / ui_gongju_* 出现 ≥1 | 否/是 | 段 4 F/E 相关任务的资产前置解除 |
| 缺图食材 3 件出现 | 否/是 | 线索板正式内容验收解除 |
| 任何 🔴 判定 | 否/是 | PENDING_USER 提醒（不入队） |

## 7. 六要素
- 做了什么 / 产物路径（本报告、manifest.json、diff.csv、dims.csv）/ 实际验证（文件数与 SHA 抽样复核）/ 待人工项（🔴 与 §3）/ 文档影响（无）/ 工具登记影响（无）
```

---

## 3. CONTROL_CHAT 拆解规则增补
1. 新增车道 `[ART]`：资产审查/同步/规范报告；`[ART_AIGC]` 保留生成类任务；部署入引擎一律 `[ENGINE_MCP]` 并 dependsOn 对应 `[ART]` 任务或指定日报编号。
2. 日报不占编号；控制台每个工作日读取 `ART-DAILY-*` §6，命中即按挂起卡入队，并在 INSTRUCTIONS 记录"触发来源：ART-DAILY-YYYYMMDD"。
3. 待产清单（§4）由控制台维护条目、ART 车道刷新状态；设计端新增待产项时通过控制台加行。
4. 日报 🔴 项出现时，当日不得入队消费该文件的 ENGINE 部署任务，等用户处理。
5. 日报若因前序任务延后超过 12 小时未执行，控制台 PENDING_USER 提醒。

## 4. 首份日报基线
- 首次执行以 AI-000057 的 `art-sync-20260904-011938881.json` 为基线；预期变更：0（除非美术已上传）。
- 待产清单 §4 首日状态由控制台按本文填入。
