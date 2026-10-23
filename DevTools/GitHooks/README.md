# GitHooks 工具与字体提交检查

2026-10-05 用户明确移除 ShortCycle_Heavy_WithFallback 的保护，当前 `protectionMode=STATIC_ONLY`：仅 `ShortCycle_Heavy_Static.asset` 的工作树及index/LFS内容SHA必须等于已批准manifest。WithFallback与DynamicFallback不再固定字节；DynamicFallback的R3结束清空仍由运行验收检查。三现有材质保留并在R3重调UpdateShaderRatios。

`DevTools/Fonts/ShortCycleFontBuildManifest.json` 必须APPROVED，带批准人/时间，工作树和index完全一致，Static路径固定；缺Static、未知模式、未批准/不一致manifest或Static任一表示漂移均拒绝。manifest旧生成时间/旧字体哈希/字数只是历史生成观测，不伪改为本轮重建结果。用户已确认当前布局与字体用于新基线checkpoint，不等于字体迁移或视觉签收。

正常pre-commit仍调用 `check-mig63-font.sh`，不使用no-verify；日常字体任务不改本地hook/config。2026-10-13 E2仅解除迁移phase和旧分支冻结；安装与便携hook一致，字体、main/backup保护及LFS pre-push保持。历史无Static clone保留旧精确legacy检查；没有protectionMode的旧manifest保留旧双字体合同。已经提交Static的仓库不允许删除Static降级到旧模式。

回归：`python DevTools/GitHooks/test-font-guard.py` 在临时Git仓库调用真实hook，覆盖旧合同及STATIC_ONLY的工作树/index/LFS/审批/缺失/漂移；历史SHA常量仅在隔离夹具中绑定合成历史字节，不改生产字体/index。Windows使用Git for Windows bash。shell源码固定LF。

新clone仍需按 `.ai-workspace/MIG63/pre-commit.sh` 安装hook，保留其他原有hook逻辑。任何未来更改Static基线须用户明确批准精确SHA与manifest；不盲恢复未知资源变化。hook保护已落盘及index，不能替代Editor内存dirty/Console/视觉验收，也不声称服务器强制保护。


## 显式 Git 内容比较（TK-WF-05）

MIG-F08/AI276交付 `compare-git-content.py`。当前生产pre-commit只有上述字体与main/backup永久保护，没有通用文本保护消费者；新工具必须显式调用，没有安装或接入生产hook，也不默认检查全仓。不恢复E2已删除的content文件归属、migration场景或phase冻结。

```powershell
python DevTools/GitHooks/compare-git-content.py --repo "<审定仓库根目录>" --baseline "<完整既存commit OID>" "<明确相对文件路径>"
```

运行前替换尖括号占位符。baseline必须是明确既存commit本身的完整OID，不接受隐式HEAD、移动ref或当前工作树作为新基线；不会自动更新基线迎合通过。可追加多个明确文件路径，只接受仓库内forward-slash相对常规文件，不扫目录、通配符、越界、symlink/reparse或.git。比较内容并保守拒绝baseline/index模式变化。

逐文件JSON给出baseline commit/blob OID/raw SHA256、index blob OID/raw SHA256、工作树raw SHA256及Git clean OID、实际属性/换行配置、三方相等结果或原因。raw SHA只用于同机字节完整性；文本身份由真实 `git hash-object --stdin --path=...`（不带-w）按当前.gitattributes和Git内建text/eol/ident/working-tree-encoding规则计算，不是简单删除CR，也不是忽略真实文本变化。未暂存变化仍检查；-text保持原始字节，LF/CRLF是否等价服从实际Git规则，不为所有文件强制归一化。

LFS另走真实内容身份：基线/index需标准oid+size指针，工作树必须真实内容字节；比较内容SHA256/OID和长度，不把指针文本算作字体。缺真实内容、非法/扩展指针、未知状态均拒绝，不迁移文件或改属性。字体/manifest的-text、STATIC_ONLY与批准SHA仍由原字体hook保护，新比较器不替代批准流程。

入口只读，不写index/object/config/hooks，不重写行尾；Git可选锁、自动维护和fsmonitor关闭于本次只读命令参数。未知外部clean filter不执行并明确失败，LFS不调用clean/smudge进程。真实Git目标重定向环境继续拒绝；indexed配置仅接受结构完整、仅含safe.directory的继承项原样透传（含0项），不增加或打印其值，不删除宿主配置来强行通过。未知indexed键、缺KEY/VALUE、坏count均失败。

参数完整时stdout为locale-independent ASCII JSON转义，按严格UTF8解析后Unicode路径/诊断完整；成功exit0，内容不等、缺失/不可读/未合并、环境/规则不支持或未知错误均非0并给出原因。不要把参数错误、未执行或未知结果当通过。使用者应检查success及每个结果，不只看HEAD/index。

定向回归使用 `DevTools/GitHooks/Test-GitContent.ps1`，通过既有Verify-All的Custom显式RunnerPath调用，不并入默认全域Final。只建小型合成seed与同一commit的两个clone；首次checkout前明确autocrlf/eol，验证实际LF/CRLF字节、Git内建规则、真实/未暂存/仅index变化、缺失/不可读/未合并、LFS真实内容、配置边界及中文路径/诊断在GBK/UTF8子进程IO中的无损往返，再运行原字体/分支夹具。本次真实242项检查与37项完整字体回归通过，证据 `.ai-workspace/outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md`。前四次失败、许可与最小修正保留，原生Unity/Build不在本工具范围。
