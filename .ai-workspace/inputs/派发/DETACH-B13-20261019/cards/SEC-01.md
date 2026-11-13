### SEC-01 · 加密/解密工具（G77，TK-SEC-01）[CODE] `module: SC` `complexity: M` `预算 80`

**dependsOn**：ASM-01 CLOSED；SAVE-01 的 `ISaveCodec` 接口已定（可并行，接口先行）。
**ModuleId**：新增追踪条目 M1-B7-R1（安全与发布，见 §四 追踪表增项）。

改动清单：
| 文件 | 处置 | 改动要点 | 归属层 | 工具三问 |
|---|---|---|---|---|
| `Tools/Security/AesGcmCipher.cs` | 新建 | AES-256-GCM（.NET `System.Security.Cryptography.AesGcm`，Unity 6 .NET Standard 2.1 可用 `[核]`：若不可用降级 AES-CBC + HMAC-SHA256）；`Encrypt(ReadOnlySpan<byte> plain, out nonce, out tag)` / `Decrypt(...)`；每次随机 12B nonce | Toolkit | 三问②：全游戏可用 → TK-SEC-01 |
| `Tools/Security/IKeyProvider.cs` + `ObfuscatedStaticKeyProvider.cs` | 新建 | 密钥不以明文常量存在：拆分常量 + 运行时拼合（防"过分简单"的字符串搜索）；接口允许未来换 provider | Toolkit | — |
| `Tools/Security/EncryptedBlob.cs` | 新建 | 容器格式 `magic(4) | version(1) | nonce(12) | tag(16) | ciphertext`；版本号供日后换算法 | Toolkit | — |
| `Tools/Persistence/EncryptedJsonCodec.cs` | 新建 | 实现 `ISaveCodec`：JSON→UTF-8→AesGcm→Blob；读时校验 magic/version | Toolkit | — |
| `Tests/Toolkit.Security.Tests/*` | 新建 | 往返；篡改 1 字节→解密失败（tag 校验）；nonce 不重复；跨平台字节序 | Tests | — |
| `TOOLKIT_REGISTRY.md` / `ENGINEERING_CONVENTIONS` | 修改 | 登记 TK-SEC-01；惯例：**密钥与 IV 不进 CSV/JSON/场景** | 文档 | 工具登记影响：是 |

**目标**：一套可被存档与配表共用的对称加密工具，带认证标签与容器版本。
**要求**：① 不引入第三方加密库；② 不依赖 ShortCycle；③ 密钥派生逻辑写在代码里而非资源；④ IL2CPP 下可用（避免反射）。
**可判定验收**：单测全绿；IL2CPP Windows x64 Development Build 下单测场景通过（控制台若无 IL2CPP 环境，记"待 Build 验证"而非通过）。
**架构断言**：Toolkit.Security 零上游依赖。
**授权需求**：无。
**诚实说明**：密钥在客户端二进制内，有决心的逆向者可提取；本线目标是抬高门槛（符合 G77 口径），不是 DRM。

---

