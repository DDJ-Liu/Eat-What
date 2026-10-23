# 工具库 · SEC

2026-10-23 AI292 attempt3原生机器结果：人批四件范围+一轮续行后，Release/Dev各一次Windows x64 Mono Build成功（0 Error）；Release实际包体排除170源SO GUID、保留13 Sprite与MissingSprite，291认证blob未变。三个fresh隔离Player均受控exit0：Release169全量/Dev155可达对169基线PASS，坏副本单密文字节改动仅认证拒绝、decoded0/无fallback；数据宿主在Start前结束，存储factory实际0，不冒称业务探针或真实存档验证。四性能文件经精确初清及正常Postprocess清理，Resources目录/meta保留；5组自有pair/journal清理、10个审定源/设置文件SHA不变，删除后GUID缓存仍非空如实保留。七合同机器材料齐备，待CONTROL独立验收；A+C/USER-U5仍待人工，不提高完整核验日期。原两Fail/本根used2of5及318根因unknown保留。报告.ai-workspace/outputs/DETACH-B13-20261019/AI-000292/attempt3/REPORT.md。

<a id="tk-sec-01"></a>

| ID | 名称/位置 | 能力与公开面 | 状态 | 消费方/边界 |
| --- | --- | --- | --- | --- |
| TK-SEC-01 | Tools/Security/{IKeyProvider,ObfuscatedStaticKeyProvider,IAuthenticatedCipher,AesCbcHmacCipher,EncryptedBlob}；Tools/Persistence/{ISaveCodec,EncryptedJsonCodec} | `Encrypt(byte[]) / Decrypt(EncryptedBlob)`；`ISaveCodec.Encode(string) / TryDecode(byte[],out string,out string)` | AI274源码/离线通过，原生/IL2CPP待验 | SAVE-01后继及SEC-02候选；不接旧DataEncryptor/生产读取、不写存档 |

2026-10-19核对范围：Unity6000.3.24f1真实.NET Standard2.1引用编译通过；同安装捆绑Mono6.13实际运行GCM抛PlatformNotSupportedException，按原卡许可采用明确V2 AES-256-CBC+HMAC-SHA256。主机.NET10的GCM支持不能代替Unity运行支持。独立加密库仅引用BCL，无Toolkit上游、Unity类型、反射/动态加载或第三方密码库。原生Editor/Player及Windows x64 IL2CPP Development Build未验，见AI274 ENGINE-HANDOFF。

V2二进制：`EWSB(4) | version=2(1) | ciphertext_length(uint32 BE,4) | IV(16) | MAC(32) | ciphertext(N)`。PKCS7；N为16倍数、16～16777216；明文上限16777200 UTF8字节。完整头部、IV和密文参与HMAC，先认证再解密，MAC比较固定32次XOR且无内容早退。V1保留给未来明确的GCM格式，当前拒绝，不把IV16/MAC32冒充GCM12/16。严格长度/magic/version与UTF8，失败无部分明文/明文兜底。HKDF-SHA256零盐extract，再以不同标签各expand一块得到AES/HMAC独立32字节子密钥；每次IV来自系统安全随机源。

密钥provider返回新32字节缓冲，调用后清理；默认provider由拆分掩码常量运行时组合，不能当DRM。已生成常量须保持，不重跑生成器或擅改provider导致旧数据失效；本次生成辅助脚本已拒绝重复执行。密钥/固定IV不得作为生产CSV/JSON/场景配置；随机IV属于非秘密的认证二进制容器字段。禁止日志输出密钥。序列化、Envelope、版本/Schema校验、原子写/恢复、路径与真实SaveBoundary归SAVE-01；发布选择/Catalog加密归SEC-02。ISaveCodec只做现有JSON文本与字节边界，不新增JSON依赖，PlainJsonCodec未在本卡实现。

长期验证入口`DevTools/Security/Test-ToolkitSecurity.ps1`，由既有Verify-All Custom调用；夹具`DevTools/Security/Fixtures/`。305项主机、306项捆绑Mono和双向密文互读通过；独立程序集不引用Unity/业务。仅隔离合成数据，不读写真实存档。首败为csc默认System.Core与显式NetStandard冲突，/noconfig后通过，历史留存。工具三问：旧TK-DAT-03无此认证/版本边界，保持原链；能力可供存档与发布共用，按原卡新建TK-SEC-01；无新包或程序集方向改变。

参考：[Unity API兼容级别](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html)说明编译API面；[Microsoft AesGcm构造器](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.-ctor?view=netstandard-2.1)列平台不支持异常；[RFC5869](https://www.rfc-editor.org/rfc/rfc5869)定义HKDF。当前算法选择依据本机真实探针，不由文档存在性推断运行通过。

2026-10-19 AI275最小复用扩展：原HKDF提取到KeyDerivation，原两CBC标签逐字不变；新增DerivedKeyProvider供09用独立Envelope标签，无第二份主密钥或片段重生。原305/306安全回归及AI274合成包兼容通过。共享ISaveCodec/原V2容器/Provider不改；Persistence的文件IO归09和应用宿主，不放入Security。

2026-10-20 AI282（未验收候选）：TK-SEC-01扩充CatalogBlobCipher，复用既有V2 CBC+HMAC与同一主provider，以EatWhat/catalog/blob/v2分域，不重生密钥或改存档格式。Catalog全图认证与指纹属于Generated数据层，IO/构建属于Editor或DATA工具；SaveEnvelope仅公开既有规范化对象读写桥。首组安全runner通过；Catalog整组第三次在Mono结果标记JSON解析处失败，已停止，不把组件通过当整组通过。API引用编译与Mono234原始日志、三次失败见outputs/DETACH-B13-20261019/AI-000282/REPORT.md；真实Player/Build未验。既有TK扩充，无新TK或一次性生产工具。
2026-10-20 AI282 POLICY续行1/attempt2：仅修既有CatalogBlob.Common原生stdout协议（单个行首BOM+Ordinal匹配及严格拒绝），22项定向/原字节回放通过；原Catalog Final4在编译元数据JToken.FromObject处遇PS包装对象自引用失败，已停止，不再修复/复验。原三次失败保持，新证据outputs/DETACH-B13-20261019/AI-000282/attempt2/REPORT.md；原生/发布仍未验。

2026-10-20 AI282 POLICY续行2/attempt3：既有runner以有限原生JSON边界替代三处FromObject包装值转换，拒绝深度截断/缺失路径/错误数组和布尔；完整结果63项严格写入回读、原22协议项及Catalog Final5通过，主机/Mono234、指纹一致/双向认证已持久化。原attempt1/2失败与正式停止保留；待CONTROL独立验收，真实Build/Player/发布仍未验。证据outputs/DETACH-B13-20261019/AI-000282/attempt3/REPORT.md。

2026-10-20 AI285同根修复：原282数据/加密/三菜结果保留但清理义务被CONTROL拒验。既有CatalogBuildPlan承载实际Cleanup/Recovery共用的schema2资产+meta精确归属/全量预检/真空目录删除；未知orphan.meta、folder.meta漂移、未知子目录、旧/未完成journal与重定向均拒绝且保留。42定向/3受影响API编译通过，未重跑234/305/306，无新TK或生产导入。CONTROL已独立6合同/15复核、实际guard42项通过并以精确证据正式解除原282拒验，原失败/拒验历史保留；原生Build/场景/170SO包排除仍未验。证据outputs/DETACH-B13-20261019/AI-000285/REPORT.md。
