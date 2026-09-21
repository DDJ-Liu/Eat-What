# MIG63 M3R 迁移分支增量

2026-09-21；并行期根入口、执行台账及配置中文说明由 CONTENT 维护，本文件记录 MIG 的最终增量，M4 汇合时一并读取。

- `d70d7a5`：6000.3.24f1 首次导入，无人工修改。按 M1 快照拆分，排除 M2 人工修复及性能测试 JSON；首次构建触发的 Graphics API 显式化来源尚待确认，不能把标题当作所有设置来源证明。
- `108b092`：M2 配置修复与工具适配。含 Compatibility=false、DefaultVolumeProfile 默认组件初始化及引用、Build index0=ShortCycle/index1=CookingProcess、三个配置补全和四个 DevTools 适配。
- `74a922c`：N1 将升级后 TMP 84文件及 GUID 纳入 Git/LFS；SHA 与 M2 快照一致。两 clone 既有行尾配置/attributes 语义一致，不做全仓行尾重写。
- 本次修复 `Assets/Scripts/MouseInteractive/MouseManager.cs`：Unity 6 Windows Uniform 滚轮恢复旧阈值单位；冷却期间累积且最多排队一步；反向、离开目标、换层、禁启与失焦清理；用 unscaled time。保留阈值/序列化字段、输入方向及场景配置。单格、小幅积累、冷却、反向和目标/层切换的真实 Input System 数据注入通过，修复后硬件手感仍待用户复验。
- ShortCycle 既有业务探针和相关离线回归（92源码、216绑定断言）通过；CookingProcess 在 Editor 可加载且无运行错误，不声称旧完整业务或 Player 跨场景加载已验。
- Tomato 3.8.99 为用户指定的测试/后续参考，复用现有 Avatars/Tomato。SkeletonAnimation、SkeletonMecanim 的实际GPU显示、idle/walk/kick、HorizontalPlayerController 移动/翻转/停止、CharacterEquipment 换装已验证。旧 Sample/output4、两场景与 runtime 保留，旧3.8.75在2022的读取失败登记历史缺陷，不再等待旧数据重导出；最终参考绑定、重复Play和Renderer延迟启用接线仍在M4。
- 同一 SpriteOutline2D 的两条D3D11警告在2022和6000.3完全相同；用户VFXLab已通过，保持Shader不变。最终同分辨率字体/Alpha/色调对照仍保留。
- MCP 正式方案按用户决定采用仓库内固定版本包与相对manifest；M4前同时核对服务端/锁文件/版本来源和6.3连接，完成前仍隔离。CompanyName/ProductName列E1后、存档系统前处理，不混入迁移修复。
- 新Player：`Builds/MIG63/Windows-x64-feedback/EatWhat.exe`。构建与自动启动检查不能代替真实鼠标复验。旧输出保留作比较。

证据位于原工作区 `.ai-workspace/outputs/control/MIG63_执行_20260921/M3R/`，完整状态报告在 CONTENT 的 `.ai-workspace/MIG63/M3R_人工反馈处理与补漏.md`。M0/M1/M2共2500文件已逐SHA备份至独立物理E盘 `E:/MIG63-Backups/20260921/`，TMP原快照继续保留至H/E2。本轮临时工具的源码和错误史留档，不作为产品代码。

main/backup维持启动S，未提前M4/E1集成或将Unity 6资源写回原2022工作区。后续顺序：硬件滚轮复验 → C冻结 → M4依赖/参考绑定/完整回归 → 用户批准最终SHA的E1 → H接棒 → F首批功能验收 → E2归档清理。
