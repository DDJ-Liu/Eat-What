# 固定版 Coplay MCP 依赖

2026-09-21，MIG63 最小恢复范围。Editor 包随仓库恢复；Python 服务端及其依赖仍须单独安装，未提供完整离线镜像。

## 版本与入口

- 上游：`https://github.com/CoplayDev/unity-mcp.git`，固定提交 `c21bf496bca87d54e75bad048563c3adb1782081`。
- Editor `10.1.3-beta.4`，`LocalPackages/com.coplaydev.unity-mcp`，manifest 用相对路径 `file:../LocalPackages/com.coplaydev.unity-mcp`。
- 同一提交的 `Server` 自报 `10.1.2`。两处版本标签不同，不要据此自动安装“最新”服务端。
- `ServerBaseline/pyproject.toml`、`uv.lock`、LICENSE 保存该服务端基线；`provenance.json` 记录来源与本地补丁，`editor-package-manifest.json` 保存上游 716 个文件的原始 SHA。
- 包采用 MIT 许可，保留源码及 `Coplay.LICENSE`。没有改动现有外部 Coplay 源码副本。

## 恢复与连接

1. 恢复工程 Git/LFS 内容，在锁定 Unity 6000.3.24f1 中解析本地包。检查 packages-lock 的 source 为 local，路径不指向迁移 clone。
2. 将上游源码恢复到独立工具目录并 checkout 上述完整 SHA，确认 `Server/uv.lock` 与本目录一致。不要使用滚动 beta 分支、无上限 PyPI prerelease 或默认 GUI 下载来替代这个基线。
3. 用本机 uv 对源码的 `Server` 执行 `uv sync --project <Server目录> --frozen --no-dev --python <Python3.11路径>`。可设置 `UV_PROJECT_ENVIRONMENT` 将虚拟环境放入工程忽略目录 `.ai-workspace/tooling/mcp-server-venv`。本轮实测 Python 3.11.9；从网络恢复依赖仍依赖包源可达性。
4. 在该环境运行 `mcp-for-unity --transport http --http-url http://127.0.0.1:8080`。先确认端口没有其他服务占用，不创建平行自动队列。
5. Unity 的 MCP 窗口使用已有 Local HTTP 地址并连接。Windows 同一用户的 EditorPrefs 共享；不要为了双 clone 试验盲目重写全局地址。需要其它客户端接入时单独审查其配置差异。
6. 每个新客户端会话先读取 `mcpforunity://instances`，调用 `set_active_instance` 选择目标，再核对 `Application.dataPath` 与 `Application.unityVersion`。本轮实测实例 `Eat-What-U6@6eb3fd4c46b5b4ee` 只对当前迁移目录有效；H 接棒后重新发现，不复制此 ID。

本轮使用本地 HTTP 服务实测 discovery、路由、只读查询、Editor API、脚本刷新及 Play 验证。没有修改 Codex 或 Claude 的配置来接入本轮服务。

## 本地补丁和配置事件

原包导入时会自动改写已存在的客户端配置。仓库版已改为：状态检查默认只读；启动检查禁止改写；禁用 stdio 版本和旧 ServerSrc 的自动迁移调度。用户主动 Configure 的功能保留。具体补丁文件和前后 SHA 见 `provenance.json`；未来更新包时必须复核，不能覆盖后就当隔离仍有效。

首次导入发生在补丁之前：Claude Desktop 的 `unityMCP` 条目被写入，原备份由上游写入函数删除，无法确知原内容或声称已还原。用户选择保全后保持现状；当前条目仍为宽范围预发行服务端命令，不作为本工程的固定版恢复入口。Codex 配置修改时间未变。当前客户端快照仅放本地迁移证据，不将个人客户端配置入库。后续重载需核对该快照哈希未变。

M4 测试服务的 PID/端口、启停和临时脚本另记迁移台账。完成本轮后停止本轮服务；H 按新工程路径重新连接。官方 Unity CLI/MCP 与额外模型评估是迁移后的独立批次，不在此处扩大投入。
