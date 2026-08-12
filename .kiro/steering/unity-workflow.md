# Unity-Architect 工作流程

## 工作流程

在开始任何 Unity 编码任务前，遵循两阶段流程：

### 阶段 1：架构分析（Unity-Architect）

当收到编码任务时，首先调用 `@unity-Architect` 分析：

1. **理解需求**：明确任务目标和范围
2. **提取架构信息**：从记忆文件中查找相关内容
   - [architecture.md](../../.claude/projects/d--Git-ButtonProject/memory/architecture.md) — 整体架构
   - [scene-configuration.md](../../.claude/projects/d--Git-ButtonProject/memory/scene-configuration.md) — 场景配置
   - [prefab-guide.md](../../.claude/projects/d--Git-ButtonProject/memory/prefab-guide.md) — Prefab 模式
   - [file-index.md](../../.claude/projects/d--Git-ButtonProject/memory/file-index.md) — 文件索引
3. **输出上下文摘要**：提取与任务相关的架构片段

### 阶段 2：方案实施

基于 Architect 提供的上下文：
- 设计符合现有架构的解决方案
- 复用已有模式和组件
- 保持代码风格一致性

## Architect 输出格式

```
## 任务分析
[任务理解]

## 相关架构
[从记忆文件提取的相关内容]

## 关键文件
[需要修改/参考的文件路径]

## 建议方案
[简要方案描述]
```

## 何时使用

- 添加新功能
- 修改现有组件
- 重构代码
- 创建新的交互对象
