### SEC-02 · 发布包配表加密（G77）[CODE → ENGINE] `module: SC` `complexity: L` `预算 120`

**dependsOn**：SEC-01 CLOSED；D-T1a CLOSED（Catalog 只读面已扩）；USER 授权窗口 ②。
**ModuleId**：M1-B7-R2。

设计（**用户 2026-10-15 已确认方案 A**；B 仅留作否决记录）：
| 项 | 推荐（A） | 备选（B） |
|---|---|---|
| 发布包数据形态 | 构建时把 Catalog 全量序列化为一个二进制 → AesGcm → `StreamingAssets/data/catalog.bin`；运行时 Catalog 从 blob 反序列化到内存，**不再加载 Generated SO**（SO 从 Build 排除） | 保留 SO 在包内，只对"敏感表"加密——但 SO 仍可被 AssetStudio 直接读，与 G77 目标不符 |
| 开关 | 编译符号 `CK01_ENCRYPTED_DATA`，由 BuildProcessor 在非 Development 构建自动置位；Editor/Dev 构建永远读 SO | 手动开关（易漏） |
| 一致性 | 构建时 blob 内写入 Generated SO 的内容哈希；运行时与代码内基线比对，不一致 → 拒绝启动并报错（防旧 blob 配新代码） | 不校验 |

改动清单（按 A）：
| 文件 | 处置 | 改动要点 | 归属层 |
|---|---|---|---|
| `Generated…/CK01GeneratedDataCatalog.cs` | 修改 | 加载策略抽成 `ICatalogSource`：`ScriptableObjectSource`（Editor/Dev）/ `EncryptedBlobSource`（发布）；只读面不变 | Data |
| `Tools/Security/…` | 扩充 | 复用 SEC-01 Blob | Toolkit |
| `Editor/Build/CatalogBlobBuildProcessor.cs` | 新建 | `IPreprocessBuildWithReport`：导出 blob 到 StreamingAssets；`IPostprocessBuildWithReport` 清理；Dev 构建跳过 | Editor |
| `DataTables/…/Run-DataPipeline.ps1` | 扩充 | 管线末尾加"blob 导出 + 回读校验"步（DATA 车道，G67） | DevTools |
| `ProjectSettings`（Scripting Define） | 修改 | 由 BuildProcessor 动态置位，**不**持久写入 ProjectSettings | — |

**可判定验收**：① Windows x64 Release Build：包内无 `Generated/DataTables` SO、有 `catalog.bin`；Play 走 13 步 + 三菜数据正确；② 篡改 `catalog.bin` 1 字节 → 启动报错不崩溃；③ Editor 与 Dev Build 行为不变；④ 导表幂等不受影响；⑤ 构建用时增量记录到报告。
**授权块**（窗口 ②）：新建 `Assets/StreamingAssets/data/`；新建 `Assets/Editor/Build/`；Build 列表不变。
**架构断言**：运行时任一时刻只有一个 `ICatalogSource`；只读面签名不变（D-T1a 等调用方无感）。

---

