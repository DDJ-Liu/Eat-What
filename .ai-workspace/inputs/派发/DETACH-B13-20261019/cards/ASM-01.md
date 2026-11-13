### ASM-01 · E-1 asmdef 分层 [CODE → ENGINE Compile-only] `module: SC` `complexity: L` `预算 120`

**dependsOn**：CODE-R01 CLOSED（避免与在跑代码改动冲突）；**领取时持 CODE 车道独占锁**，完成前不并行领取其它 CODE 卡。
**ModuleId**：L/MG 框架 v0.1 §三 E-1（唯一必做前置）。

改动清单（结构性，不改任何 `.cs` 内容）：
| 文件 / 目录 | 处置 | 改动要点 | 归属层 | 工具三问 |
|---|---|---|---|---|
| `Assets/Scripts/Tools/Toolkit.asmdef` | 新建 | 含 Tools / MouseInteractive / Rendering / Localization / DataBase；无对外依赖（仅 Unity 包引用） | Toolkit | — |
| `Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef` | 新建 | 引用 Toolkit、Generated 数据程序集；Core/Runtime/Presentation/Content/Interaction/Pieces/Debug 同一程序集（本轮不拆 Core 单独 asmdef，见断言 3） | SC | — |
| `Assets/Generated/DataTables/…/Generated.DataTables.asmdef` | 新建 | 导表产物与 `CK01GeneratedDataCatalog` 归此；被 ShortCycle 引用 | Data | 导表 `DataTableImportService` 输出目录不变 |
| `Assets/Scripts/MiniGames/Core/MiniGames.Core.asmdef` | 新建 | 空壳 + `IMiniGame` 占位接口文件（仅接口，不实现）；引用 Toolkit | MG0 | — |
| `Assets/Scripts/LongLoop/LongLoop.asmdef` | 新建 | 空壳；引用 Toolkit；不引用 ShortCycle 内部 | L | — |
| `Assets/Scripts/Legacy/Legacy.asmdef` + 旧 Prepare/Process 链脚本 `[核]` | 更名（移动） | 旧 CookingPrepare/CookingProcess/CutDice/Stir/InteractionController 等整体移入 Legacy；GUID 不变（经 Editor 移动）；CookingProcess 场景仍可加载 | Legacy | — |
| `Assets/Editor/ShortCycle/Editor.ShortCycle.asmdef`、`Assets/Editor/MIG63Validation/…`、`Assets/Editor/**` 其它 `[核]` | 新建 | Editor-only 程序集，按被引用的运行时程序集一一对应 | Editor | — |
| `Assets/Scripts/Cooking/ShortCycle/Debug/**`（探针/驱动器/Lab 脚本） | **标注** | G26 保护对象：随 ShortCycle 程序集走，不删不降级 | SC | — |
| `.ai-workspace/` runner 与 `Verify-All` 配置 `[核]` | 修改 | 按程序集拆分编译验证步骤；新增"单程序集故障隔离"检查 | DevTools | — |
| `CODEBASE_MAP.md` / `TOOLKIT_REGISTRY.md` | 修改 | 登记程序集与目录映射；TK 条目补"所属程序集"列 | 文档 | 工具登记影响：是 |

**目标**：工程由默认程序集拆为 Toolkit / Generated.DataTables / ShortCycle / Legacy / MiniGames.Core / LongLoop / Editor.* 七类程序集，任一程序集编译失败不挡其它程序集。
**前置**：Unity 6000.3.24f1；分支 `codex/unity6-followup`；CODE-R01 已关闭。
**要求**：① 不改任何 `.cs` 内容（含 namespace）；② 文件移动一律经 Editor（GUID 不变，`.meta` 成对移动，D4）；③ 第三方（Spine、TMP、Coplay MCP 本地包）各自程序集不动；④ Lab 场景与 Spine 预研场景引用的脚本随归属程序集走；⑤ 根 `AGENTS.md` / KNOWN_PITFALLS 中与"默认程序集"相关的条目由控制台标注（S4 范围）。
**可判定验收**：① Editor 编译 0 Error；② `ShortCycle_P0P1` Play 探针 13 步回归通过（AutomatedProbe）；③ `CookingProcess` 可加载无运行错误（Build 列表 index 1 要求）；④ **故障隔离测试**：在 LongLoop 程序集人为引入一个编译错误，ShortCycle 与 Toolkit 仍可编译、ShortCycle Play 可进；撤销后恢复；⑤ 导表再跑一次幂等（Generated 程序集不破坏 `DataTableImportService` 输出）；⑥ Console 0 Error。
**架构断言**：1. ShortCycle 不被 Toolkit 反向引用；2. LongLoop 与 MiniGames.Core 互不引用、均不引用 ShortCycle；3. 本轮 Core 不单独成程序集——Core"不引用 UnityEngine"的事实尚未由开发端核实（总档 §五 补记），拆 Core 另立卡。
**授权需求**：窗口 ①——Legacy 目录移动（动作：重设父级/移动；范围：旧链脚本清单；备份：分支即备份）。
**手册条款**：工程惯例 asmdef 命名；G26；D4 成对移动 .meta。

---

