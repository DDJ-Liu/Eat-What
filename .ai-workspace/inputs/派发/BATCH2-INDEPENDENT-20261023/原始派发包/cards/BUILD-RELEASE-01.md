# BUILD-RELEASE-01 · 发布脚本 [CODE·DevTools] `M · 60`

> 来源：派发包_脱离期批次2_独立项_v0.4（2026-10-23）§二；单卡文件。**禁改范围见索引 §〇 U 隔离清单**，触及即 BLOCKED_ON_USER。

### BUILD-RELEASE-01 · 发布脚本 [CODE·DevTools] `M · 60`
`DevTools/Build-Release.ps1`：版本号/构建号写入、SEC-02 blob 导出 + 回读校验、Release/Dev Mono 两种输出目录、BuildReport 摘要；不做 IL2CPP、不发布、不 Publish。验收：一次 Release 构建 0 Error（复用 AI292 口径）；输出目录结构固定；脚本幂等。
