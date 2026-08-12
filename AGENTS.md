# 项目开发规范

## 项目状态

这是一个正在积极开发的现有 Unity 项目。

除非明确要求变更，否则应保留现有架构。

## Unity

Unity 版本：2022.3.62f2

## Unity 工作规则

When working with Unity:

- Prefer Unity MCP for Unity Editor operations such as scenes, GameObjects,
  components, prefabs, materials, ScriptableObjects, editor state, Play Mode,
  Console inspection, and other serialized Unity assets.

- Do not manually edit Unity YAML scene or prefab files when the same operation
  can be performed safely through Unity MCP.

- Source code may be edited directly when appropriate.

- Before modifying existing production assets, inspect the relevant Unity state
  first.

- After meaningful Unity changes:
  1. Refresh/compile Unity if necessary.
  2. Check the Unity Console.
  3. Run relevant tests or Play Mode validation when appropriate.
  4. Review the Git diff.

- Do not commit, revert, discard, or overwrite unrelated user changes unless
  explicitly instructed.

## 通用规则

在修改现有系统之前：

1. 检查相关类及其使用位置。
2. 理解现有实现方式。
3. 优先采用合理范围内最小的改动。
4. 不进行与当前任务无关的重构。
5. 除非明确要求，否则不引入新的架构。

## Unity 资源安全

除非确有必要，否则不要直接编辑 Unity YAML 文件。

这包括：

- `.unity`
- `.prefab`
- `.asset`
- `.meta`

修改资源时，优先使用 Unity 编辑器 API 或支持 Unity 的工具。

## 序列化

修改以下内容时务必谨慎：

- 序列化字段名称
- 公开的序列化数据
- `ScriptableObject` 结构
- 存档数据结构

除非明确要求，否则应保持向后兼容。

## Git

确保改动聚焦且便于审查。

除非明确要求，否则不要提交或推送代码。

## 验证

完成代码修改后：

- 检查是否存在明显的 Unity 编译问题。
- 报告发生改动的文件。
- 说明仍需在 Unity 内完成的验证事项。
