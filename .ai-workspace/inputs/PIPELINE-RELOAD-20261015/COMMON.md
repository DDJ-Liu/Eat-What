# 2026-10-15 固定流水线重载交接（当前事实入口）

用户明确授权新开 CODE、ENGINE_MCP、CONTROL，并将其它过大常用线一并继承；DATA最后输入206609、ART_ASSET190910亦重载，ART_AIGC未知而保留。执行线先完成，CONTROL最后。授权原件：用户授权.json。旧chat保全归档，旧任务、错误、许可、回执、产物不删除；本轮只做组织接管。

## 当前边界
- 项目 D:/GameProject/Eat-What，Unity 6000.3.24f1，开发分支 codex/unity6-followup。HEAD/main/本地远端跟踪 refs于本轮核实均5035103a2fb51a07f80250cb05c8bc5ea215fdf2；没有本轮联网核远端，不据此声称远端服务器实查。上轮Git提交与合并已完成。本轮无提交/推送许可。
- 正式MANUAL，旧监管session63adb86921c541a19134e192e330dfb0 COMPLETE；245卡无RUNNING/QUEUED/NEEDS_USER/PENDING_USER，所有worker IDLE。239 SUCCEEDED、6 CLOSED_SUPERSEDED；226 VERIFIED、13 CLOSED_WITHOUT_ACCEPTANCE，不能将这些改成人工通过。AI245已SUCCEEDED/VERIFIED，49定向检查、控制6/6；已提交5035103。
- WATCH/旧ai和四个10分钟领取日程保持PAUSED；独立美术工作日午夜ACTIVE，独立周报宿主继续。组织重载不启动监管/业务或补跑验证。
- USER_WORK_STATUS为人工权威：U1/Excel/导表/字体/E2完成；U2 TODO、U3 TODO、U4 DOING（等v0.4终调/第四SlotBlock）、U5 TODO（44警告与场景UNVERIFIED）、U6可选TODO。P2场景接入、美术正式导入、未入队规划不启动。AI190 CLOSED_WITHOUT_ACCEPTANCE/SUPERSEDED；仅下一次归档一次性保全，见ARCHIVE_PRESERVATION，不上升永久规则。

## 执行约束
先读CORE，再读本角色worker和两项目文档页首/相关节，不扫全历史。固定PS7 C:/Users/DDJ/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe；runtime业务状态只走项目根queue正式API。后续实际任务需控制台绑定票据，当前bootstrap只读确认，不Poll、不Enqueue、不切模式、不Unity、不提交。新chat不清次数/预算/失败史。

四条硬上限依AGENTS优先：验证成本随改动规模；小字段diff+最多一图，全路径hash批末一次；无常规内存/纹理JSON。SHA保护在提交hook，TMP先material后font。一次性工具不冷进程/不Final。唯一ApproveResume(task,runs,scope)，幂等在同次授权内。P-051：Unity SHA退出稳定后取，授权发布更新保护基线；已完成字体不重跑。

固定职能：CODE工具/代码，DATA配表/导表，ENGINE真实Unity/场景，ART日常审查/同步；不每卡新chat。监管Complete/Fail/Yield同轮Prepare→一次Send→Record，控制Receive后独立审查；用户长期发送授权在inputs/监管结算即时回传_用户长期授权_20261014.json。不是本次bootstrap的重复回传要求，bootstrap由旧控制台wait读取。

上下文采样：新鲜精确单次input代理值，150K ADVISE/200K STRONG，未知null，非完整窗口或任务计费。控制台最终回复前在已有queue带ControllerThreadId；没有queue时至多一次Status。执行线普通命令带OwnerThreadId+当前TaskId。提醒不自动重载，新线程由本次明确用户授权开启。无工具消息无法脚本拦截，真实监管首次心跳验证留待未来真实批次，不能为测试恢复旧session。

## 长期资料入口
CORE.md、PIPELINES.md、CONTROL_CHAT.md、workers/<角色>.md、toolkit/WF.md、inputs/USER_WORK_STATUS.md、templates/人工段流程_v2_20261014.md、inputs/CK01-B_配表Schema_v2_4_1.md、inputs/排布指导书_ShortCycle_P0P1.md、ARCHIVE_PRESERVATION.md。
AI231～244验收在outputs/control/OMISSIONS-20261014、GOV-04-05-20261014、G74-20261014；AI245在outputs/control/CONTEXT-REPAIR-20261015；Git记录outputs/control/CONTEXT-COMMIT-20261015。旧完整证据留原处，按任务ID查询，不全读。

## 交接流程
新执行线只读本文件、对应ROLE文件及所需入口，确认职责、当前空闲、下一步等待正式任务，返回 READY（文件路径和理解摘要即可）。暂不修改共享文件，不向其它chat自动发消息。旧控制台统一完成绑定与日程迁移后通知接管完成；CONTROL创建放在执行线完成之后。最新映射会写回PIPELINES.md和outputs/control/PIPELINE-RELOAD-20261015/registry.json。