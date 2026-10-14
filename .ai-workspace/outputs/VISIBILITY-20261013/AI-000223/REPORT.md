# AI-000223 ENGINE 原生验证与场景修复

2026-10-13，Unity 6000.3.24f1，实例 `Eat-What@d5759c75ae7cb5de`，活动场景 `Assets/Scenes/Cooking/ShortCycle_P0P1.unity`。入场场景/meta 备份在同输出目录。本任务有意写入该场景、两份新专用材质与 ShortCycle 组件说明；无源码、坐标、ProjectSettings、提交或推送操作。用户保存的 ClueBoard 两锚点及本体 `(-16.39,-0.68,-0.3)` 保留。

## 原生状态与时序

既有 ManualVerificationDriver 业务入口实测 Cover→Catalog→Cover→Catalog→Browse→Catalog→Cover，Browse↔ClueBoardShell，Browse↔Kitchen/Fridge，StepDetailModal 开关；Generic/Hover Modal、Tray 三态、Traveler 则通过现有 Presentation API 直接补测，不冒称业务按钮测试。15 个 Set、4 个 Space、3 个 Modal、2 个 TransitionController 的场景引用已核；未建第二 Presenter/UGUI。Shared ClueBoard 是合并的唯一常驻视觉件；Book_Base/Book_Pages 在 Catalog/Browse 可见，在 Cover/P1 隐藏。

Catalog→Browse 相机 x `0→-5.89→-9.60`，P0→P1 `-9.60→-18.22→-25.60`，P1→P0 `-25.60→-16.06→-9.60`；各一 Start/Complete。目标视觉在 Pan 起点已准备，交互只在到位后获焦点。快速返程在旧 P1 Pan 中途触发，旧回调不完成，新 P0 Pan 完成：Start=2/Complete=1，最终 Browse/x=-9.60，无迟到复亮。PageGuard 在 Browse/ClueBoardShell/返回为 inactive-unfocused→active-focused→inactive-unfocused，同槽相机 x 保持 -9.60。Tray Highlight y=.080 scale1.025、Detail y=.180 scale1.05、Collapsed 命令正常；Generic→Hover→依次 Pop 恢复原叠层。Traveler 的 P0/P1 锚点与本体同位，两个 MoveTo 均 true、即时回调各一次，位置不漂移。细目见 `transition-matrix.json`。

## 最小修复

Play 截图品红方块定位到 48 个 `ShortCycleSubCard` 的 `unlockedMaterial` 和 `lockedSilhouetteMaterial` 均为 null；`Configure` 覆写 Icon.sharedMaterial。通过 Unity Editor API 新建 `Sprites/Default` 白色普通、黑色剪影两份专用材质，并给 48 个组件补 96 个引用。Refresh/Compile 无 Error，Play 中 8 个当前绑定子卡材质均非 null；抽样锁定→解锁→锁定，分别为类别图标/物品图标/类别图标及黑/白/黑材质，finally 恢复原绑定。最终截图无品红。

`StartCooking_Button/Label` 的 TMP 在字图下方另绘重复“开始做饭”，源码没有该 Label/LabelArt 路径动态依赖。只关闭 TMP 子对象，保留正式 LabelArt 字图、父按钮交互及原位置；复测截图重复消失。

## 核验与遗留

最终 Editor 已退出 Play，稳定 Edit/无编译/场景未脏；Console 0 Error，3 条已知 Warning（Shared ClueBoard 合并两条、OpenBookStructure 无 Renderer bounds 一条）。场景 focused diff 仅新增 48+48 材质引用与 1 个 Label `m_IsActive`；另有任务前用户三处 Transform 差异，未改动。没有临时探针资产或运行态遗留。详见 `patch-fields.json`/`evidence.json`。

最终观感仍需用户一次验收：左页标题与子卡、顶部夹板文字及右页标签等属于当前保存态内部排布贴叠，非跨视图重复 Set 激活；遵守本轮“不重排”和用户锚点优先，未擅改坐标。部分类别图标为 `CK01-MissingSprite` 占位，锁定态会显示占位轮廓，是素材/数据项；厨房似“3”来自两条既有装饰曲线。P0 边缘仍可能看见 P1 物理结构一角，因相机矩形交叉，交互焦点已关。根项目文档由控制台统一维护；已更新 ShortCycle 组件配置说明的验证状态、材质与按钮入口。

资产写入审计限制：创建专用材质的 Editor 调用曾使用全局 `AssetDatabase.SaveAssets()`，后续不再使用该全局保存方式。当前 `git status --short -- Assets` 除本任务场景/新材质外，还列出本批 CODE 源码差异及在本批开始已处于 modified 状态的 `ShortCycle_Heavy_DynamicFallback.asset`。该字体资产文件时间为 16:49:15，落在本任务运行时段，表明期间发生过磁盘保存/触碰；现有证据不能确定具体机制，也不能区分任务前已有差异与本次可能新增的字节漂移。不能据此声称所有其它资产逐字节不变，亦不把既有差异全部归因于本任务；未对字体作主动字段编辑或恢复。尝试用 Git LFS 读取 diff 遇到 `.git/lfs/tmp` 权限拒绝，未绕过或恢复用户资产。全批次字体提交保护仍交由提交边界 hook。
