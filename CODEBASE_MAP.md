# 《是啊，吃什么》代码结构与修改导航

最后完整核验仍为2026-09-17；2026-11-13仅核对本轮入口、正式Build场景、现有镜头源码/资源路径及文档结构，未冒充全仓运行复核。当前Unity6000.3.24f1、开发分支codex/unity6-followup，main用于已完成内容集成。

0.1.2长循环/可玩包已由用户整体签收；角色描边暂停。短循环的未开始规划和中断工作保留，见[下一轮承接](.ai-workspace/outputs/control/下一轮开发承接.md)。历史长页首/逐轮过程已无损留在[入口归档](.ai-workspace/archive/2026-11-13/README.md)。

## 工程与场景

正式共享Build表保持五场景：Boot、ShortCycle_P0P1、CookingProcess、ShortCycle_P2、LongLoop_Street。0.1.2的Windows64 Mono非Development构建只在本次BuildPlayerOptions选Boot+Street，不改共享场景表。源码/资源定位细节见[工程入口](.ai-workspace/codebase/entry.md)。

## 长循环与镜头

`Assets/Scripts/LongLoop/StreetSceneEntry.cs`是街景入口；正式输入`Assets/Scripts/Tools/Input/LegacyInputProvider.cs`实现既有`IGameInput`，读取旧键鼠和现Unity Input System右摇杆。当前Provider与LongLoop玩家控制器同在PlayerRoot_TUNE，Driver经现公开父级路径取得输入。

镜头配置在`Assets/Settings/Street/CameraFollowConfig.asset`及同目录现有配置。`StreetFollowDriver`持有纯状态，`StreetDynamicZoom`推进本帧Size与Ratio，`StreetLookFloat`处理鼠标/直接右摇杆目标和世界距离/速度上限，`StreetVerticalOffset`处理纵向输入，`StreetFollowComposition`按本帧几何合成通道；原`StreetFollowPipeline`持有目标/前视/回中状态。浮动有效意图暂停Shrink/Recenter及水平前视退出，解除后从当前值重新等待各自完整Delay/曲线。FOCUS不随视野比例缩放。

Driver写独立目标节点及本帧Lens状态，Composer执行唯一位置阻尼，Confiner约束边界；不新增整机相机阻尼。`StreetParallaxLayer`在Cinemachine最终输出后读相机位置，只写所属层容器并在退出恢复，数学在`StreetParallax.cs`。街景已有六层接线及用户归层/原点配置。

正式使用/调参文档维护在[docs参数说明](docs/demo-0.1.2_参数调整说明.md)、[人工检查说明](docs/demo-0.1.2_人工检查说明.md)及[组件说明目录](组件说明文档/视觉特效/README.md)。这些是项目长期源文档，不以任务attempt中的摘录为配置权威。实际数值以用户保存资产/Inspector为准。

## 短循环

Session/FSM入口为`Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs`、`ShortCycleStateMachine.cs`；表现由`Runtime/ShortCyclePresentationDirector.cs`等现有组件处理，P2入口`Phase2/Phase2SceneEntry.cs`。详[当前主链](.ai-workspace/codebase/shortcycle.md)、[冰箱与滚动](.ai-workspace/codebase/scroll.md)。

`Core/ShortCycle.Core.asmdef`只引用BCL；纯合同与Phase2 Matcher/Carrier等在Core下。Generated.DataTables与ShortCycle单向引用Core，Editor.ShortCycle有必要直接引用。既有类型与GUID保持；本轮不重构或推进短循环。

## 数据、资源与视效

源表/CSV、生成物、导表/字体契约见[数据与本地化](.ai-workspace/codebase/data.md)，Resources字符串路径见[资源契约](.ai-workspace/codebase/resources.md)。正式工作簿/非空内容与用户配置不得因Git或清理改值；合法大源资产沿现LFS。

描边、阴影、泡泡和统一VisualEffectsLab见[视效定位](.ai-workspace/codebase/visual-effects.md)及中文组件说明。角色描边后续业务仍暂停，AI464现有样例/未完人工事项材料不清理。旧CookingPrepare/Process边界见[旧链说明](.ai-workspace/codebase/legacy-build.md)，不因清理删除Spine Sample、长期探针或Tomato预研。

## 构建、验证与Git

构建复用`DevTools/Build-Demo.ps1`、`Assets/Editor/Build/CatalogBlobBuildProcessor.cs`的BuildPrepared和当前正式ENGINE_CLI受管流程；0.1.2只更改了获批的版本标记。输入认证/owned临时事务、当前用户成果保护保留。CLI与标准预检/项目grant边界见[WF](.ai-workspace/toolkit/WF.md)和[ENGINE_CLI](.ai-workspace/workers/ENGINE_CLI.md)。

镜头离线测试入口`DevTools/LongLoop/Test-LongLoopPlayer.py`；引擎验收固定三类和累计三轮，整体体验由用户决定。FOLLOW-R旧门、全量后台探针和定时回归保持暂停。Git日常只追踪开发源、正式配置和长期文档；release/交付包/机器运行与临时证据留本地。GitHooks保持Static字体及main/backup保护，新增源边界和普通blob体积检查；详GitHooks README。
