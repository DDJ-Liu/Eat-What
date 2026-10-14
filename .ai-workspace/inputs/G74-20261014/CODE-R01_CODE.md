# 本轮有效执行边界（2026-10-14用户）
固定职能chat，同session63adb86921c541a19134e192e330dfb0；不新开chat、不用内部代理。complexity:M / turnBudget:80，按用户既有文件数>3规则修订原S40，不新TK；预算不冒充尚未实现GOV05机器上限。
先读CORE/SUPERVISED、对应worker、AGENTS四条硬上限、项目入口相关节和实际消费者。所有exec固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe、项目根queue正式API。只领指定TaskId/token，每批写前Checkpoint；正式失败即停，仅通用ApproveResume。未知tokens/成本null。无Git提交/推送。
权威只读Schema：inputs/CK01-B_配表Schema_v2_4_1.md，用户取消由变更稿定稿，正文不改；生产不改CSV/xlsx/Authoring，不导表/消费轮次/发布字体或Generated。禁止Play/Build、FontSwapOnce、内存快照/纹理JSON、全树哈希和独立Final。保留未知dirty。
输出outputs/G74-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，真实证据与预声明合同一致。允许清单diff+一份定向回归，新变化/失败才必要复验。大返回落盘只回路径/计数/最多20异常，实际/预算及未知量记录。控制台统一合并项目入口和追踪账本，worker给文档影响建议，勿并发改入口。
Complete/Fail/Yield后立即按HANDOFF.md Prepare→一次真实Send→Record，用户长期授权已存在；不自行领后继，不重试拒绝或UNKNOWN。
# CODE-R01：G74多候选承载物解析与warning
以最终版§八为准，派发稿§二指向已纠正，补其省略的allowed_carriers交集。
ShortCycleCarrierResolver.cs≤120行、纯逻辑：Resolve(recipeId,areaId,itemId,sceneCarriers)→carrierId/candidates/isAmbiguous；input/action筛选、白名单顺序、carrier去重∩场景∩allowed_carriers，0为空，>1首条。不读preset_tools/场景对象顺序，不加配表字段。
ShortCycleCarrierWarning.cs注入日志接口输出原件中文warning，Core不依赖UnityEngine；(recipe,area,item)每session一次，真实Session Teardown清空。Unity边界Debug.LogWarning；不弹UI/改变结果。复用Catalog只读能力；现有IShortCycleInventoryQuery签名兼容，新增相邻TryResolveCarrier只读面供未来P2调用，本卡不接UI/场景。
生产允许：Content/ShortCycleCarrierResolver.cs、ShortCycleCarrierWarning.cs，ShortCycleContracts.cs及必要既有Catalog/RuntimeManagers/Session直接边界。CODE不手写meta/不操作Unity，新增源码meta交ENGINE正常导入。需新架构/扩数据字段/场景接线则停说明。
验证扩充DevTools/ShortCycle/ShortCycleCoreTests.cs及必要既有runner源清单；原5例（单候选无warning、双候选首条且一次、重复去抖、零候选、白名单序异于T12）与必要约束/session重置，一次定向runner，不独立Final。Editor尾步已安排ENGINE后继，本卡交离线成果，不伪称Editor成功。
checks: orderedResolver,warningAndSessionReset,compatibleReadSurface,targetedRegression,scopePreserved。
