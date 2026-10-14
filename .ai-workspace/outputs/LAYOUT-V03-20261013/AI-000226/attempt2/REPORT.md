# AI-000226 attempt 2 · LAY-01 三弹层视口锚定

结果：三处 Shared 弹层根已通过 Unity Editor 接入现有 `CameraViewportAnchor` 并保存场景。任务合同六项检查已由 `Compare-Acceptance.ps1` 返回 `ACCEPTANCE_MATCHED`；视觉终验为 `PENDING_USER`。

## 实施与边界

- `Shared/StepDetailModal`、`Shared/GenericModal`、`Shared/HoverCard` 各新增一个锚点组件，显式引用正式 MainCamera 和自身根；viewport=(0.5,0.5)、inset=(0,0)、保持 z=0。未修改内部子项的局部排版、ModalSet、PresentationSet、Layer_Modal、原 inactive 默认值。
- 编辑器 SaveScene 后 scene clean。任务前原场景和 meta 备份留在本目录；与备份比较仅 57 行新增（3 个组件列表引用、3 个组件记录），0 行删除；meta 无变化。无手工 YAML 修改。
- 更新 `组件说明文档/ShortCycle状态显隐与镜头切换_配置说明.md` 的锚定配置与限制。根项目文档由控制台按批次统一维护；本任务无源码/Prefab/字体/配表写入。

## 验证

- Unity 6000.3.24f1；稳定 Play 样本 frameCount 1802→3956、unscaledTime 20.207→37.922、timeScale=1。
- 三弹层分别在 P0Center、P0Left、P1、Right 的根视口坐标均为 (0.5,0.5)，world z=0；每次通过 Director 公共接口开闭，Modal 层栈 1→2→1，闭合后根 inactive。Right 是现有机位检查，不新增 P2。
- P1→Right 转场的渲染帧末样本：progress=0.6096，camera/root world x=5.609，三个根视口均 (0.5,0.5)。早先 PanProgressed 的前 LateUpdate 样本仅说明采样时序，不作为渲染帧验收证据。
- StepDetailModal 的 Collider2D 与视觉同属锚定根；GenericModal/HoverCard 没有 Collider2D，不声称其硬件点击测试。P1 代表截图 `AI-000226_modal_anchor_P1.png` 已读、签名正确、2560×1440；人工审看最终构图仍待用户。
- Play 后退出并核 Edit Mode、scene clean、三根 inactive、引用与 ModalSet 保存态；Console 新 Error=0，既有 Shared Set 警告 6 条；scene diff 仍只有上述新增项。

证据见 `evidence.json`、`anchor-observations.json`、`patch-fields.json`、`acceptance.json` 和 `用户验收清单.md`。未进行全量哈希、内存快照、Build 或重复旧字体/Excel 验证。
