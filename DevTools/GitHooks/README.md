# GitHooks 工具与提交内容检查

2026-11-13 Git 内容政策：便携 pre-commit 按 **原字体 SHA/manifest → 原 main/backup 分支保护 → 暂存内容守卫** 顺序运行。`check-repo-content.py` 只读暂存新增/修改/类型变化路径及实际 Git blob 大小，不扫描全仓、不运行 clean/smudge，不写 index、objects、config 或 hooks。Python 3 或守卫源码缺失、Git 读取失败、未解决冲突均阻止提交。生产阈值固定为 **100,000,000 十进制字节**，等于阈值允许，大于阈值拒绝；没有环境变量或命令参数放宽入口。

日常提交分类：

| 类别 | 处理 |
| --- | --- |
| Assets 与 meta、正式 ProjectSettings、Packages manifest/lock、审定 LocalPackages | 源文件；继续遵守已有字体与大资产 LFS 属性 |
| DataTables 正式工作簿/源、DevTools 工具/Fixtures、项目 docs 与组件说明 | 源文件；已取消全部 xlsx 与 docs 的误忽略，保留原项目固定说明位置 |
| 正式 AI 规范/脚本/设计、永久清单中的输入与版本规划、原长期授权、USER_WORK_STATUS | 源文件；inputs 按具名族/目录例外保留，未对白名单以外的临时输入整体放开 |
| release、Build/Builds、交付文件、缓存/日志/录屏/Profiler、IDE 私有状态、同步美术区及工具备份 | 本机生成物；忽略并拒绝强行暂存的已审目录内容 |
| AI runtime/cache/logs/revisions/exports、handover 机器 JSON、临时 inputs/outputs | 本机执行证据；只保留具名的永久输入、控制索引/工具、未完成 ART 阻塞指针及 README/COMMON/CODE/CONTROL/ENGINE_CLI 接棒 Markdown |
| 归档证据包、快照与原始证据 | 保留磁盘及历史证据，日常提交只允许 README/INDEX/index 和四个具名 tracking 永久简表 |

暂停的短循环规划另有明确例外：`inputs/派发/BATCH2-INDEPENDENT-20261023/编排登记.json`、`执行登记.md`、20261102 用户暂停原件，以及 `原始派发包/派发包索引_*.md` 和原始 cards；包含 MIGF08-HOOK、DOCS-ENG 等未开始规划。规则变更不恢复这些业务。handover 的机器绑定/claim/commit JSON 本地保留，四角色/共同入口 Markdown 继续可入库。

`.gitignore` **不会自动移出已追踪文件或清除历史对象**。暂存删除已追踪生成文件允许；新增或修改仍拒绝。旧的超限普通对象即使当前删除，推送仍可能失败，须由 CONTROL 备份并处理已审的未发布历史。本工具不改历史、不代做移出索引、提交、合并或安装本机 hook。遗留归档 LFS 属性仅用于保持历史指针语义，不代表允许新增归档包。

守卫按 **index blob** 判断：合法源资产的真实 LFS 指针很小，指针声明的大文件 size 不作为 Git blob 大小；release 等生成路径即便为 LFS 指针也拒绝。未暂存的工作树大小不作为暂存内容。路径规则与 `.gitignore` 的具名例外一起维护；原有 Tiles_noGit、`Assets/Spine Examples/` 第三方本地示例、生成 link.xml 与旧导入包等专用忽略继续保留。Spine Examples 也受暂存路径守卫限制；正式 `Assets/Scenes/Spine Sample.unity`、HorizontalPlayerControllerTest 与 ProjectValidation 保留为源，磁盘资源不删改。此守卫不承担所有扩展名/文件内容的语义审查，提交者仍须审阅 diff。

相称离线回归：`python DevTools/GitHooks/test-repo-content.py`。它只修改临时 Git 仓库，真实执行完整 hook，检查永久清单源边界、中文 docs、生成路径、普通 blob 阈值、LFS 指针、生成文件删除、缺失守卫及原 37 项字体/分支断言。大 blob 测试只把**临时复制的守卫**阈值改为 256 字节，不创建 100MB 文件。AI516 已在旧 `test-font-guard.py` 的临时夹具工厂中直接复制新增 checker 依赖，原 37 项断言和字体/分支语义保持。原 standalone `python DevTools/GitHooks/test-font-guard.py` 与 `Test-GitContent.ps1` 内嵌调用恢复依赖闭合；新入口不再注入复制补丁。`python DevTools/GitHooks/test-repo-content.py --repair-boundaries` 只验证原513的 vendor/cache/旧入口修复，真实 force-add 到临时 index，覆盖当前618个vendor文件路径而不读取其内容，不重跑完整486项矩阵。

2026-10-05 用户明确移除 ShortCycle_Heavy_WithFallback 的保护，当前 `protectionMode=STATIC_ONLY`：仅 `ShortCycle_Heavy_Static.asset` 的工作树及index/LFS内容SHA必须等于已批准manifest。WithFallback与DynamicFallback不再固定字节；DynamicFallback的R3结束清空仍由运行验收检查。三现有材质保留并在R3重调UpdateShaderRatios。

`DevTools/Fonts/ShortCycleFontBuildManifest.json` 必须APPROVED，带批准人/时间，工作树和index完全一致，Static路径固定；缺Static、未知模式、未批准/不一致manifest或Static任一表示漂移均拒绝。manifest旧生成时间/旧字体哈希/字数只是历史生成观测，不伪改为本轮重建结果。用户已确认当前布局与字体用于新基线checkpoint，不等于字体迁移或视觉签收。

正常pre-commit仍调用 `check-mig63-font.sh`，不使用no-verify；日常字体任务不改本地hook/config。2026-10-13 E2仅解除迁移phase和旧分支冻结；安装与便携hook一致，字体、main/backup保护及LFS pre-push保持。历史无Static clone保留旧精确legacy检查；没有protectionMode的旧manifest保留旧双字体合同。已经提交Static的仓库不允许删除Static降级到旧模式。

字体回归通过上述 `test-repo-content.py` 入口，在临时Git仓库调用真实hook，覆盖旧合同及STATIC_ONLY的工作树/index/LFS/审批/缺失/漂移；历史SHA常量仅在隔离夹具中绑定合成历史字节，不改生产字体/index。Windows使用Git for Windows bash。shell源码固定LF。

新clone仍需按 `.ai-workspace/MIG63/pre-commit.sh` 安装hook，保留其他原有hook逻辑。任何未来更改Static基线须用户明确批准精确SHA与manifest；不盲恢复未知资源变化。hook保护已落盘及index，不能替代Editor内存dirty/Console/视觉验收，也不声称服务器强制保护。


## 显式 Git 内容比较（TK-WF-05）

MIG-F08/AI276交付 `compare-git-content.py`。此比较器仍须显式调用，没有接入 pre-commit，也不默认检查全仓；新暂存内容守卫只做路径/大小检查，不以比较器恢复通用文本身份冻结。不恢复E2已删除的content文件归属、migration场景或phase冻结。本机 hook 安装状态由 CONTROL 另核，便携源码修改不等于已安装。

```powershell
python DevTools/GitHooks/compare-git-content.py --repo "<审定仓库根目录>" --baseline "<完整既存commit OID>" "<明确相对文件路径>"
```

运行前替换尖括号占位符。baseline必须是明确既存commit本身的完整OID，不接受隐式HEAD、移动ref或当前工作树作为新基线；不会自动更新基线迎合通过。可追加多个明确文件路径，只接受仓库内forward-slash相对常规文件，不扫目录、通配符、越界、symlink/reparse或.git。比较内容并保守拒绝baseline/index模式变化。

逐文件JSON给出baseline commit/blob OID/raw SHA256、index blob OID/raw SHA256、工作树raw SHA256及Git clean OID、实际属性/换行配置、三方相等结果或原因。raw SHA只用于同机字节完整性；文本身份由真实 `git hash-object --stdin --path=...`（不带-w）按当前.gitattributes和Git内建text/eol/ident/working-tree-encoding规则计算，不是简单删除CR，也不是忽略真实文本变化。未暂存变化仍检查；-text保持原始字节，LF/CRLF是否等价服从实际Git规则，不为所有文件强制归一化。

LFS另走真实内容身份：基线/index需标准oid+size指针，工作树必须真实内容字节；比较内容SHA256/OID和长度，不把指针文本算作字体。缺真实内容、非法/扩展指针、未知状态均拒绝，不迁移文件或改属性。字体/manifest的-text、STATIC_ONLY与批准SHA仍由原字体hook保护，新比较器不替代批准流程。

入口只读，不写index/object/config/hooks，不重写行尾；Git可选锁、自动维护和fsmonitor关闭于本次只读命令参数。未知外部clean filter不执行并明确失败，LFS不调用clean/smudge进程。真实Git目标重定向环境继续拒绝；indexed配置仅接受结构完整、仅含safe.directory的继承项原样透传（含0项），不增加或打印其值，不删除宿主配置来强行通过。未知indexed键、缺KEY/VALUE、坏count均失败。

参数完整时stdout为locale-independent ASCII JSON转义，按严格UTF8解析后Unicode路径/诊断完整；成功exit0，内容不等、缺失/不可读/未合并、环境/规则不支持或未知错误均非0并给出原因。不要把参数错误、未执行或未知结果当通过。使用者应检查success及每个结果，不只看HEAD/index。

定向回归使用 `DevTools/GitHooks/Test-GitContent.ps1`，通过既有Verify-All的Custom显式RunnerPath调用，不并入默认全域Final。只建小型合成seed与同一commit的两个clone；首次checkout前明确autocrlf/eol，验证实际LF/CRLF字节、Git内建规则、真实/未暂存/仅index变化、缺失/不可读/未合并、LFS真实内容、配置边界及中文路径/诊断在GBK/UTF8子进程IO中的无损往返，再运行原字体/分支夹具。本次真实242项检查与37项完整字体回归通过，证据 `.ai-workspace/outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md`。前四次失败、许可与最小修正保留，原生Unity/Build不在本工具范围。

已读实际客户端配置：`.vscode/extensions.json` 是 Unity 扩展建议，`launch.json` 是 Attach to Unity，`settings.json` 是文件排除与相对 solution 名；`Eat-What.code-workspace` 仅相对根目录。均未含机器绝对路径，本轮作为共享项目配置保留，不笼统忽略 `.vscode` 或 `*.code-workspace`。`.kiro/steering/unity-workflow.md` 是既有客户端规范入口，继续可入库，其旧指针/流程由 CONTROL 按 AGENTS/CORE 入口约束另行核对，本卡未修改。

Python 解释缓存规则覆盖任意目录层的 `__pycache__/`、`*.pyc`、`*.pyo`，强行加入暂存区仍拒绝；`.py`、Fixtures 源与原生模块 `.pyd` 不因缓存规则排除。AI516 修复继承原513拒验与额度历史，未改生产 hook、字体、manifest 或大对象阈值。
