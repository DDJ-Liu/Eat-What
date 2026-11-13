### SAVE-01 · 存档规范与落盘（G76）[CODE] `module: SC` `complexity: M` `预算 80`

**dependsOn**：ASM-01 CLOSED；D-T1b CLOSED（PlayerProfile 快照接口）；**MIG-F02 CLOSED**（CompanyName/ProductName 决定 persistentDataPath；窗口 ① 处理）。
**ModuleId**：M1-B1-R2（存档点 1/2 由占位转实装）、M4-B4 落盘。

改动清单：
| 文件 | 处置 | 改动要点 | 归属层 | 工具三问 |
|---|---|---|---|---|
| `Tools/Persistence/SaveEnvelope.cs` | 新建 | `{format_version:int, schema_version:string(=Schema 版本), created_utc:string ISO-8601, payload:Dictionary<string, JsonElement>, hmac:string}`；分节 payload；不依赖 ShortCycle | Toolkit | 三问②：满足"其它模块可预见会用"（长循环存档）→ 提升为 TK-DAT-07 |
| `Tools/Persistence/JsonSaveStore.cs` | 新建 | `Write(slot, envelope)` / `TryRead(slot, out envelope, out diagnostic)`；原子写（临时文件 + 替换）；路径 `persistentDataPath/saves/<slot>.json`；读失败返回 diagnostic 不抛 | Toolkit | 同上 |
| `Tools/Persistence/ISaveCodec.cs` + `PlainJsonCodec.cs` | 新建 | 编解码接口；明文实现；加密实现由 SEC-01 提供并在发布构建注入（`#if !UNITY_EDITOR && !DEVELOPMENT_BUILD`） | Toolkit | — |
| `ShortCycle/Runtime/SaveBoundary.cs` | 修改 | 存档点 1（进 P0 成功后）/ 存档点 2（P1→P2 数据处理后）：收集各 Manager 快照 → envelope → store；`ApplySnapshot` 在 BeginSession 前 | Runtime | — |
| `Runtime/PlayerProfileManager.cs` | 修改 | 实装 `GetSnapshot()/ApplySnapshot()` 为 `player_profile` 节 | Runtime | — |
| `Tests/Toolkit.Persistence.Tests/*` | 新建 | 往返幂等；损坏文件→diagnostic；schema_version 不符→拒绝并保留原文件 | Tests | — |
| `TOOLKIT_REGISTRY.md` | 修改 | 登记 TK-DAT-07 Persistence（Envelope/Store/Codec） | 文档 | 工具登记影响：是（新建） |

**目标**：收藏跨会话持久；存档格式有版本与校验，可被 SEC-01 无改动地替换编解码器。
**要求**：① JSON 序列化用工程既有库（`[核]`：Newtonsoft 或 System.Text.Json，以 TOOLKIT_REGISTRY 为准，不新引入）；② 存档不含场景/Transform 信息；③ 不写 PlayerPrefs；④ 文件损坏时游戏可进（回退默认 profile 并 Console warning 一次）。
**可判定验收**：① 单测全绿；② Play：收藏 → 退出会话 → 重进，星标保持；③ 手工损坏文件后可进且 warning 一次；④ 路径在 MIG-F02 设定的 Company/Product 下。
**架构断言**：Toolkit.Persistence 不引用 ShortCycle；SaveBoundary 是唯一写盘入口。
**授权需求**：无（MIG-F02 另在窗口 ①）。

---

