# 当前输入

2026-09-24 17:30 用户改为手动诊断：已正式 ExitSupervision，当前 MANUAL / PAUSED_MODE，stopRequested、exitRequested=true，原 ai 保持 PAUSED。仅诊断，不修复/回退/重试/派发。已用不保存的材质副本复现 FromHeavy 的 TMP ScaleRatioA/B/C 重算导致指纹失效；回退在复制备份前被同一材质守卫拦住。40 条 kern 非目标 override 来源已定位为旧字段加载转换及保存结构保护缺口，另有 ROLLBACK_FAILED 重入条件的源码推导，未执行重入。失败现场24文件及字体/CSV10文件本轮SHA不变，部分保存状态尚未恢复；原错误/恢复史和人工结论不改。详细报告：`.ai-workspace/outputs/control/U1-PRE-R1-20260923/manual-diagnosis-20260924/AI170失败原因定位报告.md`。下方较早接力均为历史，不构成新的执行授权；本条专项核验不提升完整复核日期。

2026-09-24 17:16 最新停止状态：AI170第7次执行真实失败，full事务ROLLBACK_FAILED、第一批APPLYING，原7错误/6issue/6恢复完整保留。单FromSourceHan样例1项已成功并回退，但全量第一批FromHeavy新材质出现OWNED_MATERIAL_DRIFT且公开回退同样失败；不能称零写入或已恢复。控制台只读核实两场景SHA已变、五Prefab与7个既有meta不变，三新材质保留，其中FromHeavy内容SHA与创建记录不同；主场景另有40条新增m_ActiveFontFeatures.Array.data[0] override待归属核验。正式备份和样例快照各14文件均匹配，失败现场另保全24文件。17:20主Editor单ShortCycle/Edit/clean/Console0仅表示无未保存脏场景，不代表磁盘已回基线。session CIRCUIT、stopRequested=true，原ai已PAUSED；CODE/ENGINE均idle，不Reconcile放行、不派发、不追加补丁/Reset/换key绕最后额度。原批准有效，需用户明确新的现场诊断与受控恢复范围，不能自动继续全量迁移。详细报告outputs/control/U1-PRE-R1-20260923/AI170第七轮失败现场与全队列未关闭清单.md；AI171/172等待，DATA已验保持，人审/Git/E2条件不变。

2026-09-23 本轮最新：[人工反馈与字体明确批准](U1-PRE_人工反馈与字体批准_20260923.md)，任务卡在 `U1-PRE-R1-20260923/`。AI154 Modal已人工通过；AI149保留旧拒绝并按后续修复覆盖关闭；AI156空间结果拒绝，由AI168/169返工。AI167/170承接已批准171项字体材质迁移；AI171/172承接迁移E2归档和长期工具更名。当前监管scope为AI167～172，旧U1前置已成功项不重跑；字体新画面和AI165构图未代签。下述初始包的scope/未注册DATA/等待批准表述为历史时点，以最新边界和队列为准。

2026-09-23 U1前置包当前入口：[执行边界与接力](U1-PRE_执行边界与接力_20260923.md)、[PRE-1～5派发原件](控制台输入_U1前置任务包PRE-1~5派发_20260923.md)、[设计总档v1.5.2](AI开发工作流总档_设计端_v1_5_2.md)、[路径规划v2.2](P0-P1_开发路径规划_v2_2_20260923.md)。三原件按SHA验证投放；指导书v0.3按用户明确决定等待AI156实际坐标报告，当前不提供。原F链结果按队列保留；监管仅推进AI156及AI157～164，PRE-2b首跑待DATA注册后由控制台重分类。旧设计原件保留供对照，当前执行冲突以执行边界和实际队列为准。

前 145 项的需求、批准、修订与旧配表包已收录到 [2026-09-17 阶段归档](../archive/2026-09-17/README.md)。2026-09-22 已按原路径恢复仍需跨阶段引用的 Schema、需求基准、CSV v1.2 对照样本和排布指导书 v0.1，并投放 v0.2、追踪表 v1.2.2、规划 v2.1.1、USER 状态及设计端只读副本。

常驻文件名模式为：`排布指导书_*`、`CK01-B_配表Schema_*`、`CK01_P0P1_需求对照基准_*`（含无后缀现行主本）、`P0-P1_模块拆分追踪表_*`、`P0-P1_开发路径规划_*`、`AI开发工作流总档_*`、`U1-PRE_*`、`控制台输入_U1前置任务包PRE-1~5派发_*`、`USER_WORK_STATUS.md`。机器事实以 `DevTools/Workflow/Archive-Stage.ps1 -PermanentInputPatterns` 为准；新版核对完成后旧版才归档。CSV v1.2 仅作历史对照，权威 CSV 仍在根目录 `DataTables/`。当前 v1.5.2、v2.2 与 PRE 执行覆盖必须保留；不存在的指导书 v0.3、追踪表 v1.2.3 不得伪造。

当前 F 批次采用 F-α；`_TUNE` 全工程审查和搁置素材保持原决定。

2026-09-23 新增独立结构修复输入：

- `控制台输入_FIX-T5空间结构修复派发_20260923.md`：G68/G69/D-H 与 FIX-T5a→FIX-T5b 的精确范围。用户最新指令将其定为 **F-α 之外的额外开发链**；不得加入 F-α 依赖或阻塞 F-α 验收。
- `排布指导书_ShortCycle_P0P1.md`：当前稳定排布入口，内容由已审 v0.2 投放；FIX-T5 以同日派发文档作为覆盖增量。T5b 通过后的 v0.3 仍由设计端按派发文档 §4 产出。
# 2026-09-23 外显 DATA 追加

用户要求建立可见独立配表线，入口见 [DATA-VISIBLE 授权与接力](DATA-VISIBLE_外显配表线授权与接力_20260923.md)。已创建常驻任务“AI流水线-配表DATA”（01a0ce9d-8e5a-7542-82b6-6d27609d99e0）；[AI174 路由改造](DATA-VISIBLE-20260923/DATA-VIS-1_CODE.md) → 控制台正式绑定 → [AI175 实际领取回报](DATA-VISIBLE-20260923/DATA-VIS-2_DATA.md)已加入监管。当前路由未绑定；不重跑AI164，不替USER-U1标DONE。
