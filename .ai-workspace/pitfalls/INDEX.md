2026-11-11新规则复盘：[P-071 AI473八轮逐轮坑账](ENGINE.md#p-071)；[P-072 FOLLOW-R十四张管线卡复盘与冻结](DEVTOOLS.md#p-072)。现行格式：现象、层级、处置、预防、实际轮次及来源；不再以修复链为熔断原卡续验。

# 工程坑账本索引

> 任务卡只点 P 编号，再读对应车道正文；兼容入口 KNOWN_PITFALLS.md 保持。

2026-10-15 A4复核：P-001～030仅保留索引，历史全文仍可追溯；近20个实有编号的全文保留在车道文件，按当前任务读取，不增加全量预读。原编号缺口和重复编号的不同问题保持，不补造或合并条目。

## 近期条目（正文驻留车道文件）

| 编号 | 一句话 | 车道 | 正文 |
| --- | --- | --- | --- |
| P-070 | CLI黄金参数按实际子命令；角色不等于子命令，公开ArgumentList不加错误别名 | [CODE/ENGINE_CLI] | [DEVTOOLS](DEVTOOLS.md#p-070-cli-argv) |
| P-051 | Unity 资产 SHA 凭据取 Editor 退出后稳定磁盘；字体 Dynamic 按空 glyph 验收 | [DATA/CODE] | [CODE](CODE.md#p-051-unity-font-post-exit) |
| P-049 | PowerShell JSON 日期自动转换丢 UTC，须保留显式时区 | [CODE] | [CODE](CODE.md#p-049-code-utc-json) |
| P-048 | Prefab源批准不自动覆盖映射外继承传播，字体与atlas材质须配对核对 | [CODE/ENGINE_MCP/控制端] | [CODE](CODE.md#p-048-prefab-scope) |
| P-043 | 临时dirty的归属与验证收尾必须随任务建立 | [ENGINE_MCP/控制端] | [ENGINE_MCP](ENGINE.md#p-043-engine-mcp-控制端) / [设计端与控制端](DESIGN.md#p-043-engine-mcp-控制端) |
| P-042 | 单向边界机制不能代替双向输入故障的因果证据 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-042-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-042-code-engine-mcp) |
| P-031 | 序列化 TMP Renderer 被置 0 且报告声称“可见” | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-031-engine-mcp) |
| P-032 | AUTOMATIC 同流水线接力向活动线程自发消息后丢失 | [全流水线] | [CODE](CODE.md#p-032-全流水线) / [ENGINE_MCP](ENGINE.md#p-032-全流水线) / [ART](ART.md#p-032-全流水线) / [设计端与控制端](DESIGN.md#p-032-全流水线) |
| P-033 | 上下文压缩后旧指令覆盖 activeTask 并误判孤儿 | [全流水线] | [CODE](CODE.md#p-033-全流水线) / [ENGINE_MCP](ENGINE.md#p-033-全流水线) / [ART](ART.md#p-033-全流水线) / [设计端与控制端](DESIGN.md#p-033-全流水线) |
| P-034 | 换图任务缺少视觉载体却只授权既有字段 | [控制端/ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-034-控制端-engine-mcp) / [设计端与控制端](DESIGN.md#p-034-控制端-engine-mcp) |
| P-035 | 探针父子两级关闭导致人工段需两步激活 | [设计端/ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-035-设计端-engine-mcp) / [设计端与控制端](DESIGN.md#p-035-设计端-engine-mcp) |
| P-036 | stub runner 通过不等于 Unity Editor API 编译通过 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-036-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-036-code-engine-mcp) |
| P-037 | 不能仅靠完成消息维持监管任务推进 | [控制端/全流水线] | [CODE](CODE.md#p-037-控制端-全流水线) / [ENGINE_MCP](ENGINE.md#p-037-控制端-全流水线) / [ART](ART.md#p-037-控制端-全流水线) / [设计端与控制端](DESIGN.md#p-037-控制端-全流水线) |
| P-038 | 场景组件误放 Editor 程序集导致不可挂载 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-038-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-038-code-engine-mcp) |
| P-039 | 独立 position/localPosition 桩会掩盖坐标空间混用 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-039-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-039-code-engine-mcp) |
| P-040 | Group生命周期全量写子件导致手调值无法保持 | [CODE] | [CODE](CODE.md#p-040-code) |
| P-042 | 先整体中转导入再识别规划不满足美术工程导入流程 | [控制端/ART/ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-042-控制端-art-engine-mcp) / [ART](ART.md#p-042-控制端-art-engine-mcp) / [设计端与控制端](DESIGN.md#p-042-控制端-art-engine-mcp) |
| P-041 | Sprite多Pass声明和RT字段计数不能代替实际合并绘制及预算证明 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-041-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-041-code-engine-mcp) |
| P-044 | 0-key曲线被误当有效运动映射 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-044-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-044-code-engine-mcp) |
| P-045 | 完成机位缓存不能替代移动中的实际到位判断 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-045-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-045-code-engine-mcp) |
| P-046 | 空间布局必须用世界线区间与机位表定义 | [设计端/CODE/ENGINE_MCP] | [CODE](CODE.md#p-046-设计端-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-046-设计端-code-engine-mcp) / [设计端与控制端](DESIGN.md#p-046-设计端-code-engine-mcp) |
| P-047 | 每步一任务会放大上下文、回报与验证成本 | [控制端/全流水线] | [CODE](CODE.md#p-047-控制端-全流水线) / [ENGINE_MCP](ENGINE.md#p-047-控制端-全流水线) / [ART](ART.md#p-047-控制端-全流水线) / [设计端与控制端](DESIGN.md#p-047-控制端-全流水线) |

## P-001～030 历史索引

| 编号 | 一句话 | 车道 | 正文 |
| --- | --- | --- | --- |
| P-001 | Sprite Mesh Type 通过 TextureImporterSettings 设置（2022根因，6000.3.24f1复核适用） | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-001-engine-mcp) |
| P-002 | EditorSceneManager 使用 UnityEditor.SceneManagement 命名空间 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-002-engine-mcp) |
| P-003 | 反射调用不得依赖 Color32 到 Color 的隐式转换 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-003-engine-mcp) |
| P-004 | PowerShell 反射写入 C# float 必须显式转换为 Single | [CODE] | [CODE](CODE.md#p-004-code) |
| P-005 | PowerShell 函数参数不得使用保留自动变量名 | [ART_AIGC] | [ART](ART.md#p-005-art-aigc) |
| P-006 | System.Drawing DrawImage 必须选择显式 Single 坐标重载 | [ART_AIGC] | [ART](ART.md#p-006-art-aigc) |
| P-007 | 透明背景生成图必须做 alpha 回退和双底边缘检查 | [ART_AIGC] | [ART](ART.md#p-007-art-aigc) |
| P-008 | Inspector 赋值的 SerializeField 应显式初始化为 null | [CODE] | [CODE](CODE.md#p-008-code) |
| P-009 | TMP 字号测量不得让 Ellipsis 和宽松容差制造假通过 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-009-engine-mcp) |
| P-010 | MCP telemetry 陈旧不等于 Editor 断线 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-010-engine-mcp) |
| P-011 | PowerShell 空数组跨函数边界可能退化为 null 单元素计数 | [CODE] | [CODE](CODE.md#p-011-code) |
| P-012 | PowerShell Parser 的 ref 输出变量必须先初始化 | [CODE] | [CODE](CODE.md#p-012-code) |
| P-013 | 生成 Unity 资产前必须用 AssetDatabase 逐级创建目录 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-013-engine-mcp) |
| P-014 | `.ai-workspace` 内文件检索误报不存在 | [全流水线] | [CODE](CODE.md#p-014-全流水线) / [ENGINE_MCP](ENGINE.md#p-014-全流水线) / [ART](ART.md#p-014-全流水线) / [设计端与控制端](DESIGN.md#p-014-全流水线) |
| P-015 | 满足功能验收但绕开项目框架 | [全流水线] | [CODE](CODE.md#p-015-全流水线) / [ENGINE_MCP](ENGINE.md#p-015-全流水线) / [ART](ART.md#p-015-全流水线) / [设计端与控制端](DESIGN.md#p-015-全流水线) |
| P-016 | 场景序列化字段逐行硬引用生成数据资产 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-016-engine-mcp) |
| P-017 | 场景内容误走 UGUI/Canvas 层 | [ENGINE_MCP/CODE] | [CODE](CODE.md#p-017-engine-mcp-code) / [ENGINE_MCP](ENGINE.md#p-017-engine-mcp-code) |
| P-018 | schema 允空数值列导入时被强制 Parse | [CODE] | [CODE](CODE.md#p-018-code) |
| P-019 | 无 BOM UTF-8 中文 PowerShell 脚本在 PS 5.1 解析失败 | [ART_AIGC/全流水线] | [CODE](CODE.md#p-019-art-aigc-全流水线) / [ENGINE_MCP](ENGINE.md#p-019-art-aigc-全流水线) / [ART](ART.md#p-019-art-aigc-全流水线) / [设计端与控制端](DESIGN.md#p-019-art-aigc-全流水线) |
| P-020 | 图标按可见区缩放会破坏作者相对比例 | [ART_AIGC/ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-020-art-aigc-engine-mcp) / [ART](ART.md#p-020-art-aigc-engine-mcp) |
| P-021 | SpriteOutline thickness 为源像素尺度，低显示缩放下易触 32 上限 | [CODE/ENGINE_MCP] | [CODE](CODE.md#p-021-code-engine-mcp) / [ENGINE_MCP](ENGINE.md#p-021-code-engine-mcp) |
| P-022 | 增补指令叠加时只执行增量、跳过主指令 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-022-engine-mcp) |
| P-023 | 结构件分轴缩放导致非等比巨幅拉伸 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-023-engine-mcp) |
| P-024 | 渲染文本未验证导致本地化回退串进入截图 | [ENGINE_MCP/CODE] | [CODE](CODE.md#p-024-engine-mcp-code) / [ENGINE_MCP](ENGINE.md#p-024-engine-mcp-code) |
| P-025 | 另建平行视觉层绕过功能树重排 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-025-engine-mcp) |
| P-026 | `sprite.bounds` 不是 Tight 可见区 | [ENGINE_MCP/CODE] | [CODE](CODE.md#p-026-engine-mcp-code) / [ENGINE_MCP](ENGINE.md#p-026-engine-mcp-code) |
| P-027 | 审计 actual 由排布脚本写入值回读 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-027-engine-mcp) |
| P-028 | 跨屏 Gate 登记全空间 Renderer，退化为整组隐藏 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-028-engine-mcp) |
| P-029 | 序列化禁用 Renderer 架空启用集合断言 | [ENGINE_MCP] | [ENGINE_MCP](ENGINE.md#p-029-engine-mcp) |
| P-030 | 解散聚合类时未分配引导职责 | [设计端] | [设计端与控制端](DESIGN.md#p-030-设计端) |

## 2026-10-21 CLI磨合期新增

- [P-052 持久对象不适用于SaveToSerializedFileAndForget](ENGINE.md#p-052)
- [P-053 m_Component不可按普通SerializedProperty数组删除](ENGINE.md#p-053)
- [P-054 SceneAsset属于UnityEditor](ENGINE.md#p-054)
- [P-055 正则词边界被传成U+0008](ENGINE.md#p-055)
- [P-056 Unity结构体JSON自引用](ENGINE.md#p-056)
- [P-057 capture save_path进入Assets解析并Refresh](ENGINE.md#p-057)
- [P-058 启动后首Ready RPC返回400，根因未证](ENGINE.md#p-058)
- [P-059 队列Save-TextFile替换失败](ENGINE.md#p-059)
- [P-060 长上下文与连续失败并存，不据此证明因果](ENGINE.md#p-060)

## 2026-10-24 回查补记

- [P-061 动作wire ID](ENGINE.md#p-061)
- [P-062 Ready时限与默认封包](ENGINE.md#p-062)
- [P-063 SceneHandle显式标量与Editor导出](ENGINE.md#p-063)
- [P-064 Unity空语义与原空PPtr证明](ENGINE.md#p-064)
- [P-065 DATA许可连接超时](DATA.md#p-065)
- [P-066 DATA输出根与Scope冲突](DATA.md#p-066)
- [P-067 换行夹具与Git配置继承](DEVTOOLS.md#p-067)

规划按P-047及[CONTROL当前规则](../CONTROL_CHAT.md#dispatch-planning)；本轮按已有证据补坑账，不触发业务回归。

- [P-068 组件同名/meta交付、Assets互斥与小改动验收](CODE.md#p-068)
- [P-069 ProjectSettings置值与SaveAssets持久化协议](ENGINE.md#p-069)
