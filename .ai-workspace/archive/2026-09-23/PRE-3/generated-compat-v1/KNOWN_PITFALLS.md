# AI 工作流工程坑账本 · 兼容索引

> 详细规则已按车道拆分到 [pitfalls/INDEX.md](pitfalls/INDEX.md)。保留本路径与 P 标题，旧链接仍可定位编号；新任务卡只写 P 编号。

## P-043 | [ENGINE_MCP/控制端] 临时dirty的归属与验证收尾必须随任务建立

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-043-engine-mcp-控制端)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-042 | [CODE/ENGINE_MCP] 单向边界机制不能代替双向输入故障的因果证据

见 [CODE 正文](pitfalls/CODE.md#p-042-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-001 | [ENGINE_MCP] Sprite Mesh Type 通过 TextureImporterSettings 设置（2022根因，6000.3.24f1复核适用）

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-001-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-002 | [ENGINE_MCP] EditorSceneManager 使用 UnityEditor.SceneManagement 命名空间

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-002-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-003 | [ENGINE_MCP] 反射调用不得依赖 Color32 到 Color 的隐式转换

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-003-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-004 | [CODE] PowerShell 反射写入 C# float 必须显式转换为 Single

见 [CODE 正文](pitfalls/CODE.md#p-004-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-005 | [ART_AIGC] PowerShell 函数参数不得使用保留自动变量名

见 [ART 正文](pitfalls/ART.md#p-005-art-aigc)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-006 | [ART_AIGC] System.Drawing DrawImage 必须选择显式 Single 坐标重载

见 [ART 正文](pitfalls/ART.md#p-006-art-aigc)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-007 | [ART_AIGC] 透明背景生成图必须做 alpha 回退和双底边缘检查

见 [ART 正文](pitfalls/ART.md#p-007-art-aigc)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-008 | [CODE] Inspector 赋值的 SerializeField 应显式初始化为 null

见 [CODE 正文](pitfalls/CODE.md#p-008-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-009 | [ENGINE_MCP] TMP 字号测量不得让 Ellipsis 和宽松容差制造假通过

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-009-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-010 | [ENGINE_MCP] MCP telemetry 陈旧不等于 Editor 断线

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-010-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-011 | [CODE] PowerShell 空数组跨函数边界可能退化为 null 单元素计数

见 [CODE 正文](pitfalls/CODE.md#p-011-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-012 | [CODE] PowerShell Parser 的 ref 输出变量必须先初始化

见 [CODE 正文](pitfalls/CODE.md#p-012-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-013 | [ENGINE_MCP] 生成 Unity 资产前必须用 AssetDatabase 逐级创建目录

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-013-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-014 | [全流水线] `.ai-workspace` 内文件检索误报不存在

见 [CODE 正文](pitfalls/CODE.md#p-014-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-015 | [全流水线] 满足功能验收但绕开项目框架

见 [CODE 正文](pitfalls/CODE.md#p-015-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-016 | [ENGINE_MCP] 场景序列化字段逐行硬引用生成数据资产

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-016-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-017 | [ENGINE_MCP/CODE] 场景内容误走 UGUI/Canvas 层

见 [CODE 正文](pitfalls/CODE.md#p-017-engine-mcp-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-018 | [CODE] schema 允空数值列导入时被强制 Parse

见 [CODE 正文](pitfalls/CODE.md#p-018-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-019 | [ART_AIGC/全流水线] 无 BOM UTF-8 中文 PowerShell 脚本在 PS 5.1 解析失败

见 [CODE 正文](pitfalls/CODE.md#p-019-art-aigc-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-020 | [ART_AIGC/ENGINE_MCP] 图标按可见区缩放会破坏作者相对比例

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-020-art-aigc-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-021 | [CODE/ENGINE_MCP] SpriteOutline thickness 为源像素尺度，低显示缩放下易触 32 上限

见 [CODE 正文](pitfalls/CODE.md#p-021-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-022 | [ENGINE_MCP] 增补指令叠加时只执行增量、跳过主指令

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-022-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-023 | [ENGINE_MCP] 结构件分轴缩放导致非等比巨幅拉伸

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-023-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-024 | [ENGINE_MCP/CODE] 渲染文本未验证导致本地化回退串进入截图

见 [CODE 正文](pitfalls/CODE.md#p-024-engine-mcp-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-025 | [ENGINE_MCP] 另建平行视觉层绕过功能树重排

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-025-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-026 | [ENGINE_MCP/CODE] `sprite.bounds` 不是 Tight 可见区

见 [CODE 正文](pitfalls/CODE.md#p-026-engine-mcp-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-027 | [ENGINE_MCP] 审计 actual 由排布脚本写入值回读

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-027-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-028 | [ENGINE_MCP] 跨屏 Gate 登记全空间 Renderer，退化为整组隐藏

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-028-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-029 | [ENGINE_MCP] 序列化禁用 Renderer 架空启用集合断言

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-029-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-030 | [设计端] 解散聚合类时未分配引导职责

见 [设计端与控制端 正文](pitfalls/DESIGN.md#p-030-设计端)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-031 | [ENGINE_MCP] 序列化 TMP Renderer 被置 0 且报告声称“可见”

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-031-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-032 | [全流水线] AUTOMATIC 同流水线接力向活动线程自发消息后丢失

见 [CODE 正文](pitfalls/CODE.md#p-032-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-033 | [全流水线] 上下文压缩后旧指令覆盖 activeTask 并误判孤儿

见 [CODE 正文](pitfalls/CODE.md#p-033-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-034 | [控制端/ENGINE_MCP] 换图任务缺少视觉载体却只授权既有字段

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-034-控制端-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-035 | [设计端/ENGINE_MCP] 探针父子两级关闭导致人工段需两步激活

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-035-设计端-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-036 | [CODE/ENGINE_MCP] stub runner 通过不等于 Unity Editor API 编译通过

见 [CODE 正文](pitfalls/CODE.md#p-036-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-037 | [控制端/全流水线] 不能仅靠完成消息维持监管任务推进

见 [CODE 正文](pitfalls/CODE.md#p-037-控制端-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-038 | [CODE/ENGINE_MCP] 场景组件误放 Editor 程序集导致不可挂载

见 [CODE 正文](pitfalls/CODE.md#p-038-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-039 | [CODE/ENGINE_MCP] 独立 position/localPosition 桩会掩盖坐标空间混用

见 [CODE 正文](pitfalls/CODE.md#p-039-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-040 | [CODE] Group生命周期全量写子件导致手调值无法保持

见 [CODE 正文](pitfalls/CODE.md#p-040-code)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-042 | [控制端/ART/ENGINE_MCP] 先整体中转导入再识别规划不满足美术工程导入流程

见 [ENGINE_MCP 正文](pitfalls/ENGINE.md#p-042-控制端-art-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-041 | [CODE/ENGINE_MCP] Sprite多Pass声明和RT字段计数不能代替实际合并绘制及预算证明

见 [CODE 正文](pitfalls/CODE.md#p-041-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-044 | [CODE/ENGINE_MCP] 0-key曲线被误当有效运动映射

见 [CODE 正文](pitfalls/CODE.md#p-044-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-045 | [CODE/ENGINE_MCP] 完成机位缓存不能替代移动中的实际到位判断

见 [CODE 正文](pitfalls/CODE.md#p-045-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-046 | [设计端/CODE/ENGINE_MCP] 空间布局必须用世界线区间与机位表定义

见 [CODE 正文](pitfalls/CODE.md#p-046-设计端-code-engine-mcp)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

## P-047 | [控制端/全流水线] 每步一任务会放大上下文、回报与验证成本

见 [CODE 正文](pitfalls/CODE.md#p-047-控制端-全流水线)；全索引见 [pitfalls/INDEX.md](pitfalls/INDEX.md)。

