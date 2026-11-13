# E2 本地迁移证据归档

AI-000219，2026-10-12。此处是已实际恢复校验的本地成果；E2 尚未关闭，未提交、推送或删除源证据。

- records.zip：5,133 文件，原始 4,532,060,557 字节，包体 1,043,228,379 字节。
- SHA-256：5F73219CDF2F1D024941B7CC19EA14BE1930D274321EFBE20F76D9E2DC781AB4。
- manifest.json：逐文件 sourceId、原根目录、原路径、归档路径、字节数、采样时间与 SHA-256；main/ 和 migration/ 前缀区分两侧同名文件。
- 来源：两侧 MIG63 台账和输出，加主工程 control/MIG63_执行_20260921、control/MIG63_迁移规划_20260921。包含 M0 恢复快照、M1–M4/E1/H/F 的已有证据及失败记录。迁移副本 9 个内容未出现在主侧归档中的文件也已纳入。
- 恢复实证：D:/GameProject/Eat-What/.ai-workspace/outputs/E2-20261012/AI-000219/restore-check/。逐项解压并核字节数/SHA，共 5,133/5,133，无差异；只做一次批末完整性校验。两侧源目录未删除。

需要查找旧文件时，在 manifest 的 originalRoot/originalPath 定位 archivePath，再从 records.zip 解压到独立恢复目录。不要把恢复目录整体覆盖生产工程或当前工作流。包内 recovery/workflow 和 original-before/.git/hooks 是历史证据，不能覆盖当前 runtime 或活动 .git。

E 盘 14 个既有备份文件仍在 E:/MIG63-Backups/20260921、20260922；本轮核存在、大小及 ZIP 中央目录可读性，未再次逐内容哈希。Builds、Library、Temp、live .git、MCP venv、当前 runtime/非本批输入输出、用户本地设置不纳入；具体保留原因和路径见 AI-000219/inventory.json。

此包含原始机器日志和历史本地客户端/配置快照，按旧证据边界保持 LOCAL_ONLY，不可未经整理直接推送。未来持久化阶段应保留本地原件，并按已登记隐私边界生成可发布证据范围。records.zip 已匹配现有 LFS 属性，但未暂存且不存在本次远端持久化证明。

远端 ls-remote 因本宿主 SEC_E_NO_CREDENTIALS 未成功；本地 origin 引用不等于实时远端状态。迁移分支相对本地 remote 无独有提交，主分支比本地 origin 记录领先 3 提交；这不判定真实远端是否同步。后续须核远端提交/LFS/归档可恢复后，才讨论副本和迁移专属配置清理。
