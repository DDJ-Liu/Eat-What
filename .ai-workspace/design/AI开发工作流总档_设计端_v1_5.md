# AI 开发工作流总档 · 设计端 · v1.5

> 用途：设计端跨会话上下文同步稿；按2026-09-22用户最新决定和工程事实更新。新会话读取本文及常驻工程规范，MIG63历史基线不再作为每个业务任务必读项；涉及E2时查当前迁移台账。本文已入库，设计端会话需接收本文件才算同步。
> 与工作区根文档的分工：KNOWN_PITFALLS / TOOLKIT_REGISTRY / ENGINEERING_CONVENTIONS / CONTROL_CHAT / WORKFLOW 由控制台维护，本文**只指向不镜像**；本文收设计端决策、阶段状态、架构与实现偏差、任务代号、待办。
> 基线日期：2026-09-22；E1/H完成，H后独立重序列化已通过；本版更新单工作区开发、分支交接及后续项，不定义F内容。

---

## 一、阶段状态

| 项 | 现状 |
| --- | --- |
| CK01阶段 | AI1～145已关闭（137 VERIFIED、8行政关闭），下一编号146；历史错误/验收不重置 |
| MIG63 | 迁移主体E1/H通过；b1e2ce4定向重序列化、3e91c8f接棒记录已纳入本次main集成；F待用户指定、E2待F验收后执行 |
| 引擎 | 固定Unity6000.3.24f1；URP17.3.0、Input System1.20.0、Cinemachine2.10.7；后续升级补丁另行安排 |
| 开发工作区 | D:/GameProject/Eat-What；codex/unity6-followup，跟踪同名origin；旧content/p0p1-layout已集成后删除；main只接收集成 |
| 模式/管理 | MANUAL，主控制台“AI工作流-控制台（接管）”负责F/E2；不启动F、不恢复日程；独立ART日审不变 |
| 基线/副本 | E1标签unity6.3-baseline固定37bb854；backup固定ec3aa7c；migration分支及U6副本保留到E2处理，不承担新业务 |
| 下次集成 | F验收后完成E2，再将新开发分支整体集成main；本轮不提前清理副本或改字体方案 |
| Build/入口 | ShortCycle_P0P1为index0，CookingProcess为index1；Prepare保留文件，两个Lab不入正式Build |
| 权威入口 | 根AGENTS、项目基线、CODEBASE_MAP及.ai-workspace/CONTROL_CHAT、KNOWN_PITFALLS、ENGINEERING_CONVENTIONS、TOOLKIT_REGISTRY；本轮细节见.ai-workspace/MIG63/H后分支切换与控制台移交.md |

## 二、工作区与流程规则（现行，指向根文档）

- 五条车道：CODE / ENGINE_MCP / ART_AIGC / **ART**（资产审查同步，日审 `ART-DAILY-YYYYMMDD` 不占编号）/ ART_ASSET；部署入引擎一律 ENGINE_MCP。
- 模式按工作流协议：MANUAL在用户明确触发范围内执行，不能因收到状态同步自动派发；SUPERVISED按依赖、资源锁和已批准人工节点推进。模式切换由用户显式声明。
- 人工审查按任务链已批准节点执行；既有设计/视觉/素材导入闸门不能被“统一后置”概括覆盖，详细规则以CONTROL_CHAT和工作流协议为准。
- 范围授权按用户直接指令及会话既有批准判断，不重复索取已批准的执行许可；监管模式下控制台在委托范围内审定执行清单，明确设计/视觉人工节点保留。未知用户变更或真实平台拒绝按实际情况处理，不把历史路由规则当作额外审批。
- `CloseWithoutAcceptance`：无独立人工观察路径的配表/源码交付可行政关闭（终态 CLOSED_WITHOUT_ACCEPTANCE，不等于人工通过）。
- 美术导入：先逐文件识别/比对/最终路径命名草案，用户审核后正式导入；不经 ArtImports 中转。视觉改动先进统一 Lab。
- Editor 脚本：CODE 任务产出 `Assets/Editor/**` 后链尾追加 ENGINE Compile-only 步（P-036/P-038）。
- 授权块格式：路径 / 动作（删除·新建·重设父级·修改字段·覆盖·改名·保存）/ 范围 / 备份 / 保持不变；SHA 未知写"以领取时为准并在报告记录"。
- 工具登记：先查 `TOOLKIT_REGISTRY`，新建/扩充/更名/弃用回写；完成门槛六要素含"工具登记影响"（手册 C7）。
- G26：验证辅助代码（探针、驱动器、LayoutAudit、runner、两 Lab）不删不降级；冲突时 VERIFY-TEMP 标注。
- 坑账本历史至P-045，MIG63补充由主控制台统一编号（以当前文件为准）；设计端自记：P-030 拆聚合类必附"职责→归属→触发点"表；P-035 探针一步激活；P-036 stub runner ≠ Editor 编译。

## 三、设计端产出规范（不变项摘要，全文见 v1.2 §三）

- 任务四段结构（目标/前置/要求/可判定验收）+ ModuleId + dependsOn + 手册条款引用 + 至少一条架构断言；执行器读不到设计文档库，需求浓缩为 inputs 桥接件。
- **代码任务必附改动清单**（文件 / 处置五词 / 要点 / 归属层 / 工具三问结论），用户审后入队（框架 v0.3 §六）。
- **拆聚合类必附职责归属三列表**（G31 形态）。
- 排布类验收模板（G25）：启用集合断言 / Tight 可见区 / 度量与写入分离 / D 段逐条证据 / 数据驱动断言 / 措辞禁令 / 截图非验收依据。
- Traveler 可见性规则表（G34）；越屏隐藏是状态 Enter/Exit 的附带调用（框架 §四）。
- 任务写明D:/GameProject/Eat-What、新开发分支和6000.3.24f1；Editor API核对本机目标版本，不再要求WS双路由标记。
- 拆分方法论（v1.2 §3.3）沿用；本轮起改为**模块 → 功能块 → 需求条目**三层追踪表（见《P0-P1 模块拆分追踪表》）。

## 四、决策总表

### 4.1 批次决策 G
| 编号 | 决策（一句话） |
|---|---|
| G1~G17 | v1.2 §4.2：新场景冻结旧场景｜demo 仅 FreeTry｜存档占位｜厨具 fake｜鼠标+键盘预留｜换菜无弹窗｜滚动位置保留｜CSV 导表权威｜三分类裁决｜资产基线先行｜①/③双画面｜托盘化三态菜筐｜线索板壳｜便签 tag=T6｜schema 定稿引用｜工程惯例接入 |
| G18~G20 | 基准 v1.5：AIGC 占位契约 `_AIGC`｜图标 512 画布 MaxSize 2 次幂｜S1 五格/层 |
| G21 | C-T5 拆段（结构/数值/复原），人工可替代 |
| G22 | （撤回）独立门控组件 → 改为状态过渡伴随操作（框架 §四） |
| G23 | 资产同步入链，指导书资产可解析断言 |
| G24 | 人工验证驱动器（ManualVerificationDriver） |
| G25 | 排布类固定验收模板 |
| G26 | 验证辅助代码保护 |
| G27 | 冰箱猫版本冲突以 NAS 为真源覆盖 |
| G28 | 旋转素材 `_RotFallback` 回退，C-T5b-X 挂起替换 |
| G29 | 框架文档与当时链解耦（已定档 v0.3；CK01-K 已执行） |
| G30 | K-T2/T3 提前于 5b |
| G31 | Presenter 解散职责归属表（SessionManager 引导；Binder 填内容；Director 层栈） |
| G32 | 人工闸门② 后移合并 |
| G33 | UI 段 loc key `ui_<space>_<element>.text`；首批 8 条 |
| G34 | 线索板可见性规则：P0 封面/目录/浏览隐藏；④展开显示于 P0 锚点；P1 常显；平移随 Traveler |
| G35 | 封面翻开默认进 ③ 目录页；不分触发来源；指定菜谱时该卡高亮（后续） |
| G36 | Phase 0 ESC 逐层回退 ⑤/④→①→③→⓪，退出仅封面层；`close_cover` |
| G37~G43 | Schema v2.4：`T1.whitelist_proc_ids`（限制命中）｜承载物激活由白名单+carrier 推导｜容量 demo 30/正式 20｜`prd_unknown` + `T8.unknown_product_item`｜T14 `allowed_actions/allowed_carriers`｜T2↔T3 双向可达性 warning｜便签手配 |
| G44 | 描边 `overrideGroup`=跟组；默认值只在 Reset 读一次；合并/参数来源两套独立开关；描边/阴影三态各自独立 |
| G45 ★ | 合并渲染=逐组 alpha 遮罩 + 显式离屏 + 双宿主参与普通排序；每组 2 RT；不改 Renderer2D 全局 |
| G46 | 【长期】已固定Unity6000.3.24f1，不能自动改成另一个最新补丁；不上6.6 |
| G47 | 【已失效·H后】双工作区并行限制已结束；历史决策见v1.4。字体保护等长期规则继续按现行工程文件执行 |
| G48 | 【已失效·H后】双工作区并行限制已结束；历史决策见v1.4。字体保护等长期规则继续按现行工程文件执行 |
| G49 | 【已失效·H后】双工作区并行限制已结束；历史决策见v1.4。字体保护等长期规则继续按现行工程文件执行 |
| G50 | 【长期·至复议】Cinemachine 留 2.x，不迁 CM3 |
| G51 | 【长期·转待办】官方 AI 工具链（Unity CLI + Pipeline、官方 MCP）不进 MIG63，E2 后另开评估批次 |
| G52 | 【已失效·H后】双工作区并行限制已结束；历史决策见v1.4。字体保护等长期规则继续按现行工程文件执行 |
| G53 | 【已失效·H后】双工作区并行限制已结束；历史决策见v1.4。字体保护等长期规则继续按现行工程文件执行 |
| G54 | 【保留至E2】E1/H已完成；F由用户定义并验收，随后证据归档/恢复核对、临时配置与副本清理、关闭确认。E2后再集成main并出总档v1.6 |

### 4.2 拍板 D、挂起 H、问题 Q
- D1~D18（K-T1 待拍板项，全部按建议：接口注入、正交模态、Teardown、成对移动 .meta、TrayStateController 不缩职责、runner 硬验收等）。
- D-A~D-G ★（实现偏差，按事实归档）：**D-A 整猫随动**（VisualContent 整体位移，非头脚固定）｜**D-B 封面态由封面美术图承载，书底/双页在 Cover 隐藏，目录签仅 Browse**｜**D-C 镜头 0.45s 线性 2-key；滚轮物理方向反转配置**｜D-D 线索板 P0 锚点 (5.5,−2.1168) + `SnapTo`｜D-E 合并后端 RT 上限 8192、CatRig 唯一组、Prepare 旧场景 Group 保留｜D-F 23 动作 + CurrentRecipeTab 独立 Set｜D-G 字体图集动态 216 字形为新基线。
- H-01/H-02 已由 G35/G36 关闭；H-03 Phase1→Phase2 过渡残留（挂起）；H-04 ④ 展开态构图待 F2-a 补图。
- Q-01 层数按容量（6）；Q-01b 筛选中按可见数收缩（`ApplyFilterView` 留位）；Q-02 `prd_unknown.flavor` 文案通过。

## 五、架构现状（CK01-K 接管后）

```
InputManager → MouseManager → MouseInteractionLayer 栈顶 → MouseActionBinding / ActionRouter(23)
→ SessionManager(BeginSession 五步) → ShortCycleStateMachine(Phase0: Cover→Catalog→Browse | ④ | 正交 Modal；Phase1；Tray 三态)
→ IShortCyclePresentationHost → PresentationDirector(唯一 Host；世代号)
→ Space×4(Phase0/Phase1/RightReserved/Shared) / PresentationSet(ContainerInactive | RendererHidden) / ModalSet×3 / TravelingElement(线索板) / TrayPresentation(Preset×3) / CameraPan(0.45s 线性)
Content: FridgeBinder(容量 30→6 层，紧密填充) / ClueBoardBinder(无菜清空)；Data: CK01GeneratedDataCatalog(v2.4 只读面)
Fridge: MouseScrollableObject → ScrollAdapter(invert) → Router → Binder → FridgeCatRig → ScrollArea_Controller(Discrete 整层)
Render: SpriteOutline2D/Group2D + Defaults 资产 + MergeRenderer2D(双宿主 RT)；泡泡 SceneColorCapture2D + EtherBubble
Debug(G26): ManualVerificationDriver(13 键+T/H，状态板跟随相机) / AutomatedProbe / FridgeScrollTraceProbe / LayoutAudit / CameraFrameGizmo / 两 Lab
```
不得再引入平行静态层（P-025）；Presenter/Builder 兼容实现不是入口。

程序集事实：业务主要使用默认Assembly-CSharp与Assembly-CSharp-Editor；现有Assets内asmdef属于Spine/Text Animator等第三方，未实现独立业务asmdef分层。不得把规划写成已实现。读取到的v1.4本节没有asmdef分层原文，本版补充事实说明。

## 六、数据与本地化（v2.4）
14 表；新增 `T1.whitelist_proc_ids`、`T8.unknown_product_item=prd_unknown`、`T8.demo_fridge_capacity_level=fcap_lv2(30)`、`T14.allowed_actions/allowed_carriers`；校验 V-01~V-12 + 四类报告；三菜谱白名单初填 3/6/3 条（待策划复核）；导表入口 `DataTableImportService`；本地化 UTF-8 无 BOM，UI 段 key 8 条；P2 运行时规格（S-01/02/04/05）归档待 P2 批次。

## 七、美术
- 基线：CK01-J v2.2 + 批次 4（09-15：封面 A/B/C/D、气泡 A/B、dakaibingxiang、favorite_star、清汤面图、皮蛋面/未解锁归档、Q 角色撤回）。
- 待产（ART 日报 §4 权威）：`_straight`×5、拆分 13 件 E6 重导、`ui_tag_*`、`ui_gongju_*`、Tidy/EquipmentEntry 按钮、UnknownCard 确认、三食材图、`prd_unknown` 图标；命名待改源 4 项；`yanguang` 重复件清理。
- 挂起任务触发：`_straight` 齐 → C-T5b-X；拆分件重导 → `_ScaleComp` 归 1。

## 八、工具
以 `TOOLKIT_REGISTRY.md`（TK-INT/MOT/LAY/RND/DAT/LOC/DBG/CMD/WF）为准；规划中未建：TK-LAY-06 TidyLayout、TK-LAY-07 StackedIcon、TK-DAT-05 TagFilter、TK-DAT-06 PlaceholderRule；HoverCard/DragDrop 扩展待段 4。

## 九、任务代号索引（CK01 全批次，含 09-08~17 新增）

| 代号 | 内容 | 状态 |
|---|---|---|
| A / J-T1~T6′ / B-T1~T10 / C-T1~T8 / C-T5a·R / C-T5b / K-T1~T3 / K-T2b | 见 v1.2 §九 + 本轮 | 全部关闭 |
| FIX-T1 / T2·T2c·T2d / T3 / T4·T4b | TMP Renderer 与驱动器 / 线索板可见性·参照框·日志·状态板 / 锚点·Set·探针 / 封面→目录·逐层 ESC | 关闭 |
| F-T1a~p | 滚轮：接线→诊断→整猫层级迁移→六层可达→五列命中→标签遮挡→方向反转与通用复用 | 关闭 |
| OUT-T0/T1/T1c/T2、OUT-L1、OUT-LAB、OUT-MEMBER、OUT-ALPHA | 描边分层：盘点→组件重做→Lab 合并→迁移→成员独立开关→HDR Alpha | 关闭 |
| CAM-T1~T3 | 镜头瞬移定位→线性修复→正式曲线 | 关闭 |
| REVIEW-T1~T6d | 统一复验：字体图集、缺字、线索板首行遮挡、六层内容、数量标签 | 关闭 |
| ART-SYNC/ART-DEPLOY/COVER-T1（AI122~126、142） | 批次 4 同步→正式归档→配置→复验；Cover 显隐 | 关闭 |
| 8 项行政关闭 | AI25/36/40/41/45/60/61/70 | 非人工通过 |
| 未开：D-T1/T2（段 3）、E/F/G（段 4）、C-T5c、C-T5b-X、`_ScaleComp` 归 1、H-03、P2 批次 | | 见《模块拆分追踪表》；其中改代码者已解除迁移冻结，但仍须用户指定范围后派发 |
| MIG63 | C/M4/E1/H及H后定向重序列化完成；证据和首次失败保留 | F未指定，E2未执行 |

## 十、待办

1. F范围由用户稍后指定；布局调整+无鼠标修复仅为管理端建议，未入队。主控制台接手分解、编排、派发和验收。
2. F验收后E2：证据归档及恢复验证、长期工具按功能改名/保GUID、解除临时配置、决定Spine Examples去留并清理迁移副本；E2完整关闭后合入main，出v1.6。
3. MIG-F01原生统一滚轮单位及手感（Mac用前适配）；F02公司/产品名须在存档落地前决定；F03Claude客户端固定版本或去留仍待用户，本轮保持现状；F04无鼠标/Camera.main容错独立任务；F05长期工具去批次名；F06定向重序列化已完成；F07官方Unity CLI/MCP及额外AI模型另批评估。
4. 新增F08：文本资产保护按Git规范化内容比较基线、index、工作树，原始SHA用于备份；不全仓重写行尾。字体LFS继续按真实内容SHA保护。
5. 新增F09：主静态字体+动态回退方案待用户拍板，批量文案前处理。当前216字形基线不变；构建清空开关不是日常不漂移的保证，需定义动态缓存、源字体与回退配置的版本管理。
6. 新增F10：612个本地忽略Spine Examples在E2由用户决定是否删除；保留快照、先查引用，不删除Tomato和两个业务预研。
7. 团队环境说明：安装6000.3.24f1、准备Git LFS、从新main取得codex/unity6-followup；TMP本地忽略目录先备份，checkout覆盖为预期；按DevTools/MCP恢复固定依赖。主控制台已接收，其他成员不冒称已通知。
8. 设计端既有事项：模块拆分追踪、人工流程、美术需求与B1/F2-a回写仍待各自原授权；_TUNE全工程审查、未定未来素材及H-03等仍不自动安排。以ART实时报告为准复核§七历史待产项。

### 变更记录
- v1.2 2026-09-01 | v1.3 2026-09-18：阶段闭合状态；规则改为指向根文档；决策 G21~G45、D-A~G、H/Q；架构现状；代号索引扩至 145 项；待办重排。
- v1.4 2026-09-20：新增 MIG63 批次与双工作区管理（§一 四行、§二·B 整节、§三 一条临时规范、G46~G54、§九 MIG 链、§十 重排）；全部临时内容标注生效区间 S→E2，并预告 v1.5 的移除动作。§五~§八 未改。

- v1.5 2026-09-22：依用户H后决定撤销双工作区临时派发规则，回填6000.3.24f1、当前Build及新分支模型；G47/48/49/52/53失效，G54保留至E2；F尚未指定，补F08～10与程序集事实；E2后出v1.6。
