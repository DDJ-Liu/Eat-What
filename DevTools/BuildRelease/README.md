# 发布数据构建接入

2026-11-19 demo-0.1.3 的实际 Windows64 构建已完成。修复角色形象的合法空引用处理后，现行快照覆盖 200 个数据资产、13 个 Sprite 键和 1 个 Prefab 键，认证读回与编译后指纹一致。Boot 和 Street 两场景构建成功，普通 Player 正常退出，真实存档字节一致。下方 11 月 16 日与 AI571 的离线记录描述各自执行时的范围。

## 合法空形象引用（2026-11-19）

当前发布捕获对GameObject形象复用已有Sprite的空引用来源证明。Editor反射可能取到CLR非null、但Unity判空的包装对象；只有生成资产在批准目录、未dirty、身份一致，SerializedObject对应顶层字段是ObjectReference且instanceID=0，并且同一磁盘资产/精确localID文档内该字段唯一保存为`{fileID: 0}`时，才能序列化为null。原路径边界、链接拒绝和精确字段检查保持；不是把所有Unity假空对象静默吞掉。

`CatalogBlobSerializer.Capture`原三参数入口和原Sprite证明四参数入口保留，追加五参数入口供Editor并列提供Sprite/Prefab证明。CLR null本来即可忠实保存；Prefab假空没有证明、证明被拒、非零PPtr、磁盘非空或损坏引用仍失败；正常Prefab不调用空值证明，继续登记稳定GUID:localID，已登记引用恢复失败仍拒绝。包格式/Schema/密钥/算法和指纹生成规则不变，不手填指纹或补虚构形象来迎合发布。

原角色表形象留空、生成引用为空时，读回仍是空名/null，沿既有场景规则保实例自身形象；有名但导入引用为空的语义也不被序列化器改写。修复不改CSV/xlsx、生成资产、Prefab、用户保存配置或当前版本号。

离线复用原Legacy14 Blob入口及原ImportContracts：保原Sprite空引用/认证/往返断言，追加两份正常共享形象与两份合法空引用共存的捕获/认证解码，以及证明缺失/非零PPtr/断引用拒绝。Editor真实源与Unity引用编译、外部PPtr输入夹具分别报告；真实Editor CaptureReferences与构建后续仍由原发布任务验证，本修复不启动引擎或创建新的原生验收轮。

最后实际核对：2026-11-16 AI555/V013-11源码及离线入口。此目录此前没有README，本页登记现有构建路径；实际包体操作留获批的发布卡。

AI555跨轮离线结果现已齐备：八表/Prefab真实往返210项、四个数据入口及旧Blob完整回归通过；原BuildRelease经独立AI558修复第395行宿主互操作后，原完整96检查通过，558已CONTROL独审两合同/九事实。根因是JRead管道把JObject展开为Object[]；该行保留本地CLR JObject并调用setter，原数组、全部Check/Reject及嵌入冻结源码保持。旧49份夹具中48未改、FullProof为独立获准的单点变更。原555/557/558逐轮失败和许可全部保留，本次555 attempt3只核对并引用实际通过证据，不重跑未受影响入口；整卡正式收口及CONTROL独审另记。原生导入/包体验收仍留14/17。

演示包由 `DevTools/Build-Demo.ps1` 校验目标、场景与本轮引擎上下文，再通过 Unity 官方 CLI 执行审定载体，调用 `CatalogBlobBuildProcessor.BuildPrepared`。另一入口 `DevTools/Build-Release.ps1` 使用下述 Stage、Prepare 和 Build 流程。两条入口共用快照与构建处理器。Stage通过BuildReleaseEntry进入CatalogBlobBuildProcessor.PrepareSnapshot：从正式Catalog/Generated资产捕获全图，校验对象可恢复，再交原CatalogBlobStaging事务输出catalog.bin、catalog.graph.json、export.json及指纹源码候选。指纹须在构建前按原步骤导入/编译，Build回调不能临时补生成。

新图Schema为2.5.1，包含原14种与八表行类型、三种Optional值类型；原2.4.1读兼容保留。图的sprites/prefabs分别保存稳定GUID:localID键，export.json在新图中额外记录prefabKeys。BuildReleaseEntry按真实数组长度核对应元数据；旧快照没有prefabs时继续原校验。密码、密钥、当前CatalogBuildFingerprint文件与算法/域不改，新内容的总指纹须自然重新准备，不能手填旧值。

CatalogBlobBuildProcessor的CaptureReferences并列收集Sprite和GameObject；原private Capture(out sprites)保留供现有BuildReleaseEntry反射调用，方法名不产生重载歧义。BuildPrepared再核实际图、已编译指纹及包认证，将两个引用列表写入任务自有的build-only Catalog，清空原表引用，沿原构建场景副本替换与排除SO路径。linker同时保留AssetTypes与ValueTypes，包含八表与CK01Optional类型。Prefab由这个对象引用列表进入包，未增加Resources目录或名称加载器。

非Dev且定义CK01_ENCRYPTED_DATA时，Catalog只建立EncryptedBlobSource，分别按稳定键查Sprite/Prefab，认证/引用失败明确拒绝。空的导入引用仍可保存null及原源名；已登记引用恢复失败不可悄悄变null。解码仅销毁其生成的SO，不销毁两个列表指向的共享持久资产。

离线验证使用原Test-BuildRelease.ps1和原DataTables Blob完整Legacy14显式输入；当前22表/八非空表、真实加密源及Runtime角色id查询由原ImportContracts追加用例单列。旧14表内容逐项与生产改动前冻结导出比对，不比较新旧总包头/指纹来代替。四阶段脚本本身无需改动，生产改动在其原调用实现内。

本卡没有运行Unity Editor/CLI/MCP、Build、实际Stage或写入StreamingAssets/Generated。当前原生22表仍八表空行，真实Importer保存、Prefab携带、linker裁剪与Player加载结果仍需14/17按授权验证；离线DLL编译和测试宿主结果不能代签原生包体验收。
