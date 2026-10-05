# 字体提交检查

2026-10-05 用户明确移除 ShortCycle_Heavy_WithFallback 的保护，当前 `protectionMode=STATIC_ONLY`：仅 `ShortCycle_Heavy_Static.asset` 的工作树及index/LFS内容SHA必须等于已批准manifest。WithFallback与DynamicFallback不再固定字节；DynamicFallback的R3结束清空仍由运行验收检查。三现有材质保留并在R3重调UpdateShaderRatios。

`DevTools/Fonts/ShortCycleFontBuildManifest.json` 必须APPROVED，带批准人/时间，工作树和index完全一致，Static路径固定；缺Static、未知模式、未批准/不一致manifest或Static任一表示漂移均拒绝。manifest旧生成时间/旧字体哈希/字数只是历史生成观测，不伪改为本轮重建结果。用户已确认当前布局与字体用于新基线checkpoint，不等于字体迁移或视觉签收。

正常pre-commit仍调用 `check-mig63-font.sh`，不修改本地hook/config、不使用no-verify。历史无Static clone保留旧精确legacy检查；没有protectionMode的旧manifest保留旧双字体合同。已经提交Static的仓库不允许删除Static降级到旧模式。

回归：`python DevTools/GitHooks/test-font-guard.py` 在临时Git仓库调用真实hook，覆盖旧合同及STATIC_ONLY的工作树/index/LFS/审批/缺失/漂移；历史SHA常量仅在隔离夹具中绑定合成历史字节，不改生产字体/index。Windows使用Git for Windows bash。shell源码固定LF。

新clone仍需按 `.ai-workspace/MIG63/pre-commit.sh` 安装hook，保留其他原有hook逻辑。任何未来更改Static基线须用户明确批准精确SHA与manifest；不盲恢复未知资源变化。hook保护已落盘及index，不能替代Editor内存dirty/Console/视觉验收，也不声称服务器强制保护。
