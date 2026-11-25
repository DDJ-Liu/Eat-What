# 工具库 · SEC

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

<a id="tk-sec-01"></a>

| ID | 名称/位置 | 能力与公开面 | 状态 | 消费方/边界 | 程序集／宿主 |
| --- | --- | --- | --- | --- | --- |
| TK-SEC-01 | Tools/Security/{IKeyProvider,ObfuscatedStaticKeyProvider,IAuthenticatedCipher,AesCbcHmacCipher,EncryptedBlob}；Tools/Persistence/{ISaveCodec,EncryptedJsonCodec} | `Encrypt(byte[]) / Decrypt(EncryptedBlob)`；`ISaveCodec.Encode(string) / TryDecode(byte[],out string,out string)` | AI274源码/离线通过，原生/IL2CPP待验 | SAVE-01后继及SEC-02候选；不接旧DataEncryptor/生产读取、不写存档 | `Toolkit` |

V2二进制：`EWSB(4) | version=2(1) | ciphertext_length(uint32 BE,4) | IV(16) | MAC(32) | ciphertext(N)`。PKCS7；N为16倍数、16～16777216；明文上限16777200 UTF8字节。完整头部、IV和密文参与HMAC，先认证再解密，MAC比较固定32次XOR且无内容早退。V1保留给未来明确的GCM格式，当前拒绝，不把IV16/MAC32冒充GCM12/16。严格长度/magic/version与UTF8，失败无部分明文/明文兜底。HKDF-SHA256零盐extract，再以不同标签各expand一块得到AES/HMAC独立32字节子密钥；每次IV来自系统安全随机源。

密钥provider返回新32字节缓冲，调用后清理；默认provider由拆分掩码常量运行时组合，不能当DRM。已生成常量须保持，不重跑生成器或擅改provider导致旧数据失效；本次生成辅助脚本已拒绝重复执行。密钥/固定IV不得作为生产CSV/JSON/场景配置；随机IV属于非秘密的认证二进制容器字段。禁止日志输出密钥。序列化、Envelope、版本/Schema校验、原子写/恢复、路径与真实SaveBoundary归SAVE-01；发布选择/Catalog加密归SEC-02。ISaveCodec只做现有JSON文本与字节边界，不新增JSON依赖，PlainJsonCodec未在本卡实现。

长期验证入口`DevTools/Security/Test-ToolkitSecurity.ps1`，由既有Verify-All Custom调用；夹具`DevTools/Security/Fixtures/`。305项主机、306项捆绑Mono和双向密文互读通过；独立程序集不引用Unity/业务。仅隔离合成数据，不读写真实存档。首败为csc默认System.Core与显式NetStandard冲突，/noconfig后通过，历史留存。工具三问：旧TK-DAT-03无此认证/版本边界，保持原链；能力可供存档与发布共用，按原卡新建TK-SEC-01；无新包或程序集方向改变。

参考：[Unity API兼容级别](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html)说明编译API面；[Microsoft AesGcm构造器](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.-ctor?view=netstandard-2.1)列平台不支持异常；[RFC5869](https://www.rfc-editor.org/rfc/rfc5869)定义HKDF。当前算法选择依据本机真实探针，不由文档存在性推断运行通过。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [BuildReleaseEntry.cs](<../../Assets/Editor/Build/BuildReleaseEntry.cs>)；[CatalogBlobBuildProcessor.cs](<../../Assets/Editor/Build/CatalogBlobBuildProcessor.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CatalogBlobSerializer.cs](<../../Assets/Scripts/DataBase/CookingData/CatalogBlobSerializer.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [EncryptedJsonCodec.cs](<../../Assets/Scripts/Tools/Persistence/EncryptedJsonCodec.cs>)；[ISaveCodec.cs](<../../Assets/Scripts/Tools/Persistence/ISaveCodec.cs>)；[AesCbcHmacCipher.cs](<../../Assets/Scripts/Tools/Security/AesCbcHmacCipher.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/SEC.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
