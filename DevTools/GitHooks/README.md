# 字体提交检查

2026-09-21：用户采纳；覆盖 CONTENT 和 MIG 两个 clone。检查脚本为 `check-mig63-font.sh`，由各自 `.git/hooks/pre-commit` 调用。Git 不会自动安装 hook，新 clone 必须安装并运行一次检查。现有 LFS `pre-push` 不变。

保护 `Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset`，基线 SHA-256：
`1D379A7EB64DE1871194614F83BF61AC52E0D323677048FE237E3FBD3B399CC2`。

每次提交同时检查工作树整份资产和 Git index（兼容原始 blob 与 LFS 指针的内容 SHA）。即使这次没有暂存字体，也拒绝磁盘字体漂移；只恢复磁盘文件而遗漏暂存区副本同样会被拦截。检查只读，不恢复、不暂存、不保存 Unity 资源。

手工运行（Git Bash/macOS shell，仓库根目录）：

```sh
sh DevTools/GitHooks/check-mig63-font.sh
```

回归检查：`python DevTools/GitHooks/test-font-guard.py`，在临时Git仓库测试15项情况，不改工程index或字体。Windows使用Git for Windows随附的bash，不调用系统WSL bash。两份shell源码由局部 `.gitattributes` 规则固定LF，不做全仓行尾重写。

已有 hook 时应保留原逻辑并在其中调用上面脚本，非零状态立即退出。MIG63 当前完整 hook 模板在 `.ai-workspace/MIG63/pre-commit.sh`。先备份、对照现有 hook 再安装；不能盲目覆盖其他工具的 hook。此字体检查在 E2 后保留，届时只解除临时分支/归属冻结。

检查失败时，先备份完整 asset/meta、核对 Editor 是否还有未保存的人工修改及资产来源。确认仅为本次验证导致的动态图集变化后，按照 MIG63 的已验证办法通过 Editor 恢复整份资产并重导入，再分别核对磁盘与暂存区；不得拼接 YAML 或只清 dirty 标记。

**有意修改字体时**，必须先取得用户对具体内容的批准。临时放行只接受一个精确内容 SHA 和非空批准原因（两侧单独配置，不推送本地配置）：

```sh
git config --local mig63.fontApprovedSha256 <已批准的64位SHA256>
git config --local mig63.fontApprovalReason '<批准依据及用途>'
```

这不是通用跳过开关，也不会跳过 MIG63 文件归属检查。新基线获批后同步更新两个分支的脚本/文档，验证后删除这两个本地配置，避免旧批准被长期复用。当前未配置任何例外。

限制：本地 hook 可以被 `--no-verify` 等手段绕过，不能声称服务器强制保护；正常提交必须走检查。它保护落盘和 index 内容，不能检测尚未落盘的 TMP 内存变化，因此 Unity 验证结束仍须检查资源及 Git diff。
