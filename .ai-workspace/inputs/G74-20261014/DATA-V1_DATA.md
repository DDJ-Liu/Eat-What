# 本轮有效执行边界（2026-10-14用户）
固定职能chat，同session63adb86921c541a19134e192e330dfb0；不新开chat、不用内部代理。complexity:M / turnBudget:80，按用户既有文件数>3规则修订原S40，不新TK；预算不冒充尚未实现GOV05机器上限。
先读CORE/SUPERVISED、对应worker、AGENTS四条硬上限、项目入口相关节和实际消费者。所有exec固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe、项目根queue正式API。只领指定TaskId/token，每批写前Checkpoint；正式失败即停，仅通用ApproveResume。未知tokens/成本null。无Git提交/推送。
权威只读Schema：inputs/CK01-B_配表Schema_v2_4_1.md，用户取消由变更稿定稿，正文不改；生产不改CSV/xlsx/Authoring，不导表/消费轮次/发布字体或Generated。禁止Play/Build、FontSwapOnce、内存快照/纹理JSON、全树哈希和独立Final。保留未知dirty。
输出outputs/G74-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，真实证据与预声明合同一致。允许清单diff+一份定向回归，新变化/失败才必要复验。大返回落盘只回路径/计数/最多20异常，实际/预算及未知量记录。控制台统一合并项目入口和追踪账本，worker给文档影响建议，勿并发改入口。
Complete/Fail/Yield后立即按HANDOFF.md Prepare→一次真实Send→Record，用户长期授权已存在；不自行领后继，不重试拒绝或UNKNOWN。
# DATA-V1：G74 Schema与校验器差异
生产允许清单：DataTables/Schemas/Cooking/KitchenAreas.schema.json、RecipeSteps.schema.json、DevTools/DataTables/Validate-DataTables.ps1；必要验证仅扩充既有Test-Validate-DataTables.ps1/Fixtures，不碰.cs。
1. preset_tools deprecated:true/允空；最终版明确已废弃不校验，取消有效外键检查，列与非空值保留。allowed_actions/carriers说明约束集合、承载物实例来源场景；保留合法性/core_facility要求。T3两列仅补说明任意kind Items和Tools引用，不改类型。
2. 复用AI188现有SceneInventoryPath解析、身份与过期检查；V04/V08不重新造清单实现、不读preset_tools。缺失/不可用清单info说明并跳过场景依赖检查，静态外键合法性照查，不伪报通过。
3. V09每recipe×area×T2标准/替代食材，筛T12 input/action，按T1白名单顺序取记录，carrier去重∩sceneCarriers∩allowed_carriers；>1warning给候选及首条裁决。不得按T12行序/字母序裁决；替换旧V09无支持分区语义。V04输入多候选语义保持，可共用算法减少重复计算；报告说明覆盖。
4. 三组必需回归：单候选、双候选/白名单顺序、缺清单info；必要补allowed_carriers和deprecated断言。现行CSV只读校验一次，列error/warning，真实error不改业务值迎合。历史清单若过期诚实未知/跳过，不启动Unity重导。
checks: schemaAligned,orderedCandidates,sceneInventoryReuse,targetedRegression,currentCsvReadOnly,scopePreserved。
