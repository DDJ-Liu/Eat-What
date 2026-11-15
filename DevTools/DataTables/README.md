# DataTables 开发工具入口

正式 Unity 隔离导表及发布契约见 DATA_PIPELINE.md；运行器为 Run-DataPipeline.ps1，共用进程/路径/日志/事务核心为 DataPipeline.Common.ps1，编辑器外回归为 Test-BatchImport.ps1。人工工作簿入口另见 Authoring/README.md，不由隔离导表器反向改写。

AI195 源码门禁仅经编辑器外夹具验证；真实 AI194 DATA 轮次恢复、Unity 首导日志和生成物发布均不在此 README 的已完成范围。测试用真实错误片段见 Fixtures/README.md。

## N0-04 八表代码接入（2026-11-15 局部核对）

现有注册表增加 Narrative 的 Characters、Dialogues、DialogueLines、CGs 与 Street 的 Scenes、SceneStates、SceneStatePlacements、AmbientPools：8表105列，加原14表119列为22表224列。原14份schema/CSV定义与T14.preset_tools保留。正式新增空CSV和用户原xlsx由后继DATA任务处理；本次CODE仅在独立夹具/临时目录测试，当前正式输入未齐时不能宣称完整DataTables回归通过。

8个 `CK01*Data` 行类型及 `CK01OptionalValues.cs` 位于 `Assets/Scripts/DataBase/CookingData/`，沿既有asmref归入Generated.DataTables，程序集引用不变。可空float/int/bool以hasValue/value保留未指定与显式0/false；本阶段不代演出配置填默认值。comment沿原通用导入约定留在作者数据，不写入生成行。新sprite/prefab字段存文件名string，只返回精确查找/缺失/重名诊断，不绑定素材、不创建占位资产。原Cooking六个sprite目录保持。

`CK01GeneratedDataCatalog` 增加TryGetCharacter/Dialogue/DialogueLine/CG/Scene/AmbientPool，以及完整组合键的TryGetSceneState(sceneId,stateId)和TryGetSceneStatePlacement(sceneId,stateId,kind,target)，均沿bool+out行+诊断契约，缺失/重复不挑首项。返回已有行对象，不能当作冻结快照。组合资产名/stableId用各段长度前缀，避免不同场景的同名状态冲突；原单键路径不变。

原通用校验器按新schema元数据检查有限数值/范围、父段等级决定speaker必填、场景内状态引用和气泡池只引用ambient段；预摆实例决定anchor_id必填需要后续场景上下文，本阶段只保留条件说明。命令/覆盖文本原样保存，链语义/运行效果仍由后续剧情层负责。

相称回归沿现有 `DevTools/Verify-All.ps1 -Profile Narrative`，包含真实DLL编译、原Narrative组、1141表契约/导入/查询检查及49工作簿检查；后两组分别复用现有StaticCompile宿主和真实Authoring入口，边界见 [Narrative README](../Narrative/README.md)。四旧PS已按用户批准仅扩16行总数，完整四入口待DATA正式输入就绪统一执行。配置人员见[八表说明](../../docs/剧情八表配表说明.md)，作者操作见[Authoring README](Authoring/README.md)。
