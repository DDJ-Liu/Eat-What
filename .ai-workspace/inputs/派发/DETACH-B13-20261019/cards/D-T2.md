### D-T2 · 食谱本场景绑定 [ENGINE_MCP] `module: SC` `complexity: M` `预算 80`

**dependsOn**：D-T1a、D-T1b CLOSED；USER 授权窗口 ②。
**ModuleId**：同 D-T1a/b 的"场景"列。

**目标**：把 Binder/Piece 接到现有场景对象：CatalogCard ×8、Title、DishPhoto_TUNE、IngredientList 点位、StickerNotes、CurrentRecipeTab、StepDetail Modal 行容器、FlipAnchor_L/R（仅引用留位）。
**要求**：① 只改组件引用与序列化字段，**不改 Transform / `_TUNE` 值**（G75）；② 经 Editor 保存，不手改 YAML；③ 场景备份 `DevTools/Scenes/backup/D-T2_pre/`；④ 不新增 Sorting Layer/Tag/Layer；⑤ LayoutAudit 前后对比：位置零差异。
**可判定验收**：① LayoutAudit 位置 diff = 0；② 探针 13 步 + D-T1 新增断言在真实场景通过；③ 三份可见区审计 v2 无回归；④ 实机截图 P0_Catalog / P0_Browse / P0_StepDetail 三张（非验收依据，供人工段）；⑤ Console 0 Error；⑥ 字体：若新 loc 文案出现缺字，报 DATA 车道重建 Static 图集（G70），本卡不改字体资产（P-048/P-051）。
**授权块**（窗口 ②）：路径 `Assets/Scenes/ShortCycle_P0P1.unity`｜动作：修改字段（组件引用）｜范围：上列对象｜备份：如上｜保持不变：全部 Transform、`_TUNE`、Sorting、字体引用｜SHA：以领取时为准并在报告记录。
**人工段清单预告（节点验收 A）**：目录 8 卡（3 正式 + 问号占位）、点卡进 ①、①页四项内容、换菜同步、⑤页步骤、收藏星标、ESC 回退不变。

---

