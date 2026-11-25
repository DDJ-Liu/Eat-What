# 模块正文：5. 描边、阴影与泡泡

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 5. 描边、阴影与泡泡

| 功能 | 源码入口 | 程序集／宿主 |
| --- | --- | --- |
| 正式描边参数 | Assets/Scripts/Rendering/SpriteOutline2D.cs、SpriteOutlineGroup2D.cs、SpriteOutlineDefaults.cs | `Toolkit` |
| 合并后端 | Assets/Scripts/Tools/Rendering/SpriteOutlineMergeRenderer2D.cs、SpriteOutlineMergeMember2D.cs | `Toolkit` |
| Shader | Assets/Shaders/2D/ | 见下方已核源码归属 |
| 泡泡折射与池 | Assets/Scripts/Tools/Rendering/SceneColorCapture2D.cs、EtherBubbleDistortion2D.cs、EtherBubbleEmitter2D.cs | `Toolkit` |
| Lab 驱动/装配 | Assets/Scripts/Tools/Debug/VisualEffectsLabDriver.cs；Assets/Editor/VisualEffects/；Assets/Editor/Rendering/ | `Toolkit` |

逐组像素 alpha 遮罩与显式离屏调度，组内 _MergedShadow/_MergedOutline 宿主参与普通排序；每组两张 RT，尺寸按包围盒和像素边距取8倍数，无成员级 RT。组阴影并集后使用组参数，ForceOff 使用本地；描边参与合并与参数来源是两种不同开关。正式 Outline Color 支持 HDR RGB 和 Alpha，Hover 恢复完整 RGBA。

配置与限制以[视觉特效说明索引](../../组件说明文档/视觉特效/README.md)为准。所有视觉改动先进入统一 Lab；保留现有探针、夹具与回归场景。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [VisualEffectsLabDriver.cs](<../../Assets/Scripts/Tools/Debug/VisualEffectsLabDriver.cs>)；[EtherBubbleDistortion2D.cs](<../../Assets/Scripts/Tools/Rendering/EtherBubbleDistortion2D.cs>)；[EtherBubbleEmitter2D.cs](<../../Assets/Scripts/Tools/Rendering/EtherBubbleEmitter2D.cs>) |
| `Toolkit` | [Assets/Scripts/Rendering/Toolkit.asmref](<../../Assets/Scripts/Rendering/Toolkit.asmref>) | [SpriteOutline2D.cs](<../../Assets/Scripts/Rendering/SpriteOutline2D.cs>)；[SpriteOutlineDefaults.cs](<../../Assets/Scripts/Rendering/SpriteOutlineDefaults.cs>)；[SpriteOutlineGroup2D.cs](<../../Assets/Scripts/Rendering/SpriteOutlineGroup2D.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/visual-effects.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
