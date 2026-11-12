# 任务简表

生成时间：2026-10-27T14:44:14.1223998+08:00。永久派生索引，不加入 worker 必读。来源：验证过身份/来源的报告机器块、历史回填与 queue/usage；token 口径逐任务见索引，unknown 不等于零。解析失败记录数：0。

字段：ID｜短标题｜实际工具｜车道｜复杂度｜预算/实际轮次｜输入/缓存/输出 token（口径见索引）｜结算视图｜闭合方式｜短总结｜attempts｜续行批准/消费｜关系索引｜输出。
追加字段：线标记｜用量分类（批准映射的语义归属，与实际工具及队列车道独立；不改历史账目）。
状态缩写：CV=CLOSED_VERIFIED；CW=CLOSED_WITHOUT_ACCEPTANCE；CS=CLOSED_SUPERSEDED；CF=CLOSED_FAILED；OPEN=未关闭。闭合方式由人工门/验收/行政/Fail证据区分，不能确定时 unknown。工具/数值过长时显示“详”，完整值见该行索引。无续行记录显示 0/0，仅表示当前可见批准记录为零。
所有短标题/短总结的全文、dependsOn、supersedes、父根、失败史、批准与消费证据、输出路径均在各行“关系”索引；缺失字段为 null/unknown，不补造。实际轮次为最新执行口径，非累计 attempt 轮次。

## unknown / 批次 unknown

[AI-000001][r1]｜生成冰箱猫 Idle 与双眼分层眨…｜unknown｜ART_AIGC｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Completed placeholder refrigera…｜1｜0/0｜[关系][r1]｜[输出][o1]｜unknown｜unknown

[AI-000253][r2]｜AI250 心跳参数一行修复｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜队列车道CODE；实际工具PowerShell7离线及只读no…｜1｜0/0｜[关系][r2]｜[输出][o2]｜unknown｜unknown

[AI-000252][r3]｜AI250 心跳参数缺陷只读诊断｜PowerShell-wo…｜CODE｜S｜40/unknown｜193440/191104/1871｜CV｜机器｜队列车道CODE；实际工具PowerShell7只读。AI25…｜1｜0/0｜[关系][r3]｜[输出][o3]｜unknown｜unknown

[AI-000251][r4]｜用量监管补充正式失败窗口归因｜PowerShell-wo…｜CODE｜M｜80/1｜1194795/1169920/15936｜CV｜机器｜队列车道CODE；实际工具PowerShell7离线工作流。正…｜1｜0/0｜[关系][r4]｜[输出][o4]｜unknown｜unknown

[AI-000250][r5]｜CLI-FIRST 固定官方CLI…｜unity-officia…｜ENGINE_CLI｜M｜80/2｜2045181/2010112/15388｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜3｜2/2｜[关系][r5]｜[输出][o5]｜unknown｜unknown

## MIG-F07 / 批次 unknown

[AI-000249][r6]｜MIG-F07 ENGINE_CL…｜powershell7｜CODE｜L｜120/unknown｜8350890/8139136/68483｜CV｜机器｜队列车道CODE；实际工具PowerShell7离线工作流。E…｜1｜0/0｜[关系][r6]｜[输出][o6]｜unknown｜unknown

[AI-000248][r7]｜MIG-F07 官方CLI与固定C…｜unity-cli+pip…｜ENGINE_MCP｜M｜80/unknown｜7947338/7827712/30297｜CV｜机器｜执行流水线：ENGINE_MCP（固定chat）；实际执行工具…｜1｜0/0｜[关系][r7]｜[输出][o7]｜unknown｜unknown

## GOV-04 / 批次 unknown

[AI-000247][r8]｜GOV-04 FLOOR-01历史…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜FLOOR-01完成历史首样本测量：六角色5条可核验，inpu…｜1｜0/0｜[关系][r8]｜[输出][o8]｜unknown｜unknown

[AI-000246][r9]｜GOV-04 回执上下文写入兼容对…｜powershell｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜Write-Result统一PSCustomObject回执类…｜1｜0/0｜[关系][r9]｜[输出][o9]｜unknown｜unknown

## GOV04-FIX / 批次 unknown

[AI-000245][r10]｜GOV04-FIX 修复上下文采样…｜powershell｜CODE｜L｜120/unknown｜unknown/unknown/unknown｜CV｜机器｜修复GOV04上下文新鲜采样、普通回执及控制台WATCH消费；…｜1｜0/0｜[关系][r10]｜[输出][o10]｜unknown｜unknown

## G74-20261014 / 批次 unknown

[AI-000244][r11]｜G74 CODE-R01 Edit…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜CODE-R01后继Unity原生自动导入与编译收口完成；两新…｜1｜0/0｜[关系][r11]｜[输出][o11]｜unknown｜unknown

[AI-000243][r12]｜G74 CODE-R01承载物解析…｜powershell｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜G74 CODE-R01：完成白名单顺序承载物解析、会话级歧义…｜1｜0/0｜[关系][r12]｜[输出][o12]｜unknown｜unknown

[AI-000242][r13]｜G74 DATA-V1 Schem…｜powershell｜DATA｜unknown｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜G74 DATA-V1 Schema v2.4.1 valid…｜1｜0/0｜[关系][r13]｜[输出][o13]｜unknown｜unknown

## OMISSIONS-20261014 / 批次 unknown

[AI-000241][r14]｜DATA-F1b 退出后字体发布合…｜powershell｜DATA｜unknown｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜DATA-F1b post-exit Static crede…｜1｜0/0｜[关系][r14]｜[输出][o14]｜unknown｜unknown

[AI-000240][r15]｜Spine两旧资产精确日志白名单修复｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜AI-000240精确Spine旧载荷日志白名单源码候选完成；…｜2｜1/1｜[关系][r15]｜[输出][o15]｜unknown｜unknown

[AI-000239][r16]｜Spine已知旧资产白名单只读定位｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜Spine Sample/output4 legacy loa…｜1｜0/0｜[关系][r16]｜[输出][o16]｜unknown｜unknown

## GOV-04-05-20261014 / 批次 unknown

[AI-000238][r17]｜GOV-05 复杂度分级与轮次预算｜powershell｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜GOV-05 S/M/L derived budgets, f…｜1｜0/0｜[关系][r17]｜[输出][o17]｜unknown｜unknown

[AI-000237][r18]｜GOV-04 上下文量监测与用户重…｜powershell7｜CODE｜unknown｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜GOV-04上下文来源分离、领取/结算计量、控制台心跳入口、W…｜1｜0/0｜[关系][r18]｜[输出][o18]｜unknown｜unknown

## OMISSIONS-20261014 / 批次 unknown

[AI-000236][r19]｜DATA-F1_ENGINE 选项…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜DATA-F1发布后Unity验证完成；七项机器检查通过，最终…｜1｜0/0｜[关系][r19]｜[输出][o19]｜unknown｜unknown

[AI-000235][r20]｜DATA-F1_DATA 选项A导…｜unity-batchmo…｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜DATA-F1 attempt3 single Current…｜3｜2/2｜[关系][r20]｜[输出][o20]｜unknown｜unknown

## DOC-SLIM / 批次 unknown

[AI-000254][r21]｜文档瘦身归档与永久任务简表工具适配｜PowerShell-wo…｜CODE｜L｜120/1｜3560775/3352448/65936｜CV｜机器｜队列CODE；实际工具PowerShell7离线与只读git …｜1｜0/0｜[关系][r21]｜[输出][o21]｜unknown｜unknown

## OMISSIONS-20261014 / 批次 unknown

[AI-000234][r22]｜DATA-F1_CODE 选项A导…｜powershell｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜DATA-F1 long-term font integrat…｜1｜0/0｜[关系][r22]｜[输出][o22]｜unknown｜unknown

## DOC-SLIM / 批次 unknown

[AI-000255][r23]｜追加授权的机器转储与备份处置｜PowerShell-wo…｜CODE｜M｜80/unknown｜2459980/2396288/42243｜CV｜机器｜队列CODE；实际PowerShell7离线/只读git di…｜1｜0/0｜[关系][r23]｜[输出][o23]｜unknown｜unknown

[AI-000257][r24]｜归档预审重复遍历消除｜PowerShell-wo…｜CODE｜S｜40/unknown｜477213/468352/5792｜CV｜机器｜队列CODE；实际PowerShell7静态解析/只读diff…｜1｜0/0｜[关系][r24]｜[输出][o24]｜unknown｜unknown

## WF / 批次 unknown

[AI-000276][r25]｜MIG-F08 文本资产 Git …｜PowerShell 7+…｜CODE｜S｜40/unknown｜11620429/11478016/90105｜CV｜机器｜固定CODE；实际PowerShell7/Python3/Gi…｜5｜2/2｜[关系][r25]｜[输出][o25]｜unknown｜unknown

## SC / 批次 unknown

[AI-000275][r26]｜SAVE-01 版本化存档与保存边…｜PowerShell 7+…｜CODE｜M｜80/unknown｜11517559/11348608/95022｜CV｜机器｜固定CODE/实际PowerShell7、Python、Git…｜1｜0/0｜[关系][r26]｜[输出][o26]｜unknown｜unknown

[AI-000274][r27]｜SEC-01 通用认证加密与共享存…｜PowerShell 7+…｜CODE｜M｜80/unknown｜4815815/4741504/41749｜CV｜机器｜固定CODE；实际工具PowerShell7/Python3/…｜1｜0/0｜[关系][r27]｜[输出][o27]｜unknown｜unknown

[AI-000273][r28]｜P2L-01 加工匹配、分区限制与…｜PowerShell 7+…｜CODE｜M｜80/unknown｜5578184/5474944/37610｜CV｜机器｜固定CODE；实际工具PowerShell7/Python3/…｜1｜0/0｜[关系][r28]｜[输出][o28]｜unknown｜unknown

[AI-000272][r29]｜D-T1b 步骤详情数据驱动与收藏…｜PowerShell 7+…｜CODE｜M｜80/unknown｜9422881/9262208/60594｜CV｜机器｜固定CODE；实际工具PowerShell7/Python3/…｜1｜0/0｜[关系][r29]｜[输出][o29]｜unknown｜unknown

[AI-000271][r30]｜AI266报告路径及分模式1-3-…｜PowerShell 7+…｜CODE｜L｜120/unknown｜8734238/8618368/50642｜CV｜机器｜CODE / PowerShell7、Python3、Git只…｜2｜1/1｜[关系][r30]｜[输出][o30]｜unknown｜unknown

[AI-000270][r31]｜AI266报告目录与分模式续行只读…｜PowerShell 7 …｜CODE｜S｜40/unknown｜4384834/4310272/33771｜CV｜机器｜AI266同根只读诊断完成；固定CODE，实际PS7/Pyth…｜1｜0/0｜[关系][r31]｜[输出][o31]｜unknown｜unknown

[AI-000269][r32]｜D-T1a 食谱本数据绑定与最小解…｜PowerShell 7+…｜CODE｜L｜120/unknown｜12878163/12599936/66994｜CV｜机器｜CODE / PowerShell、Python、Git只读、…｜1｜0/0｜[关系][r32]｜[输出][o32]｜unknown｜unknown

[AI-000268][r33]｜AI265探针时序与真实前置条件最…｜PowerShell 7+…｜CODE｜M｜80/unknown｜2037175/2006912/18285｜CV｜机器｜原AI265同根探针REPAIR完成：固定CODE，实际PS7…｜1｜0/0｜[关系][r33]｜[输出][o33]｜unknown｜unknown

[AI-000267][r34]｜AI265探针失败只读诊断｜PowerShell 7+…｜CODE｜S｜40/unknown｜1976249/1945472/11234｜CV｜机器｜AI265同根只读诊断完成：固定CODE，实际PS7/Pyth…｜1｜0/0｜[关系][r34]｜[输出][o34]｜unknown｜unknown

[AI-000266][r35]｜ASM-01 落地后一次导表幂等验证｜codex-local-d…｜DATA｜M｜80/unknown｜16420801/16331392/30931｜CV｜机器｜队列车道 DATA；实际工具 PowerShell 7 与 U…｜3｜2/2｜[关系][r35]｜[输出][o35]｜unknown｜unknown

[AI-000265][r36]｜ASM-01 引擎落地与MIG-F…｜unity-officia…｜ENGINE_CLI｜L｜120/unknown｜17834810/17677824/59824｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜3｜2/2｜[关系][r36]｜[输出][o36]｜unknown｜unknown

[AI-000264][r37]｜ASM-01 窗口①兼容实施与可安…｜PowerShell 7+…｜CODE｜L｜120/unknown｜unknown/unknown/unknown｜CV｜机器｜ASM-01 CODE兼容实施完成；固定CODE，实际PS7/…｜1｜0/0｜[关系][r37]｜[输出][o37]｜unknown｜unknown

[AI-000263][r38]｜ASM-01 CODE程序集落地包…｜PowerShell 7 …｜CODE｜L｜120/unknown｜unknown/unknown/unknown｜CV｜人工｜CODE准备阶段完成：实际工具PowerShell7/Rosl…｜1｜0/0｜[关系][r38]｜[输出][o38]｜unknown｜unknown

## LEDGER-SLIM / 批次 unknown

[AI-000262][r39]｜指令账本归档与追加保护的文档验证器…｜powershell-7+…｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜队列车道CODE；实际工具PowerShell 7、Git只读…｜1｜0/0｜[关系][r39]｜[输出][o39]｜unknown｜unknown

## DOC-SOLID / 批次 unknown

[AI-000261][r40]｜tar.xz完整任务归档的只读索引…｜powershell7+p…｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜队列车道CODE；实际工具PowerShell7、Python…｜1｜0/0｜[关系][r40]｜[输出][o40]｜unknown｜unknown

## DOC-PLACEMENT / 批次 unknown

[AI-000260][r41]｜任务简表默认落入归档追踪区｜powershell7+g…｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜队列车道CODE；实际工具PowerShell 7、Git只读…｜1｜0/0｜[关系][r41]｜[输出][o41]｜unknown｜unknown

## DOC-REVIEW / 批次 unknown

[AI-000259][r42]｜永久文档反馈：版本族与简表证据消费｜powershell7+g…｜CODE｜L｜120/unknown｜unknown/unknown/unknown｜CV｜机器｜队列车道 CODE；实际工具 PowerShell 7、Git…｜1｜0/0｜[关系][r42]｜[输出][o42]｜unknown｜unknown

## DOC-SLIM / 批次 unknown

[AI-000258][r43]｜真实超限包的转储与备份去重收口｜PowerShell-wo…｜CODE｜M｜80/1｜2313867/2214400/35828｜CV｜机器｜队列CODE；实际PowerShell7离线/Git只读dif…｜1｜0/0｜[关系][r43]｜[输出][o43]｜unknown｜unknown

[AI-000256][r44]｜实际转储兼容与归档检索收口｜PowerShell-wo…｜CODE｜M｜80/unknown｜2549620/2504320/27101｜CV｜机器｜队列CODE；实际PowerShell7离线/只读diff。真…｜1｜0/0｜[关系][r44]｜[输出][o44]｜unknown｜unknown

## OMISSIONS-20261014 / 批次 unknown

[AI-000233][r45]｜GOV-03 控制端遗漏增量处置｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜GOV-03 S4 read-only audit: 32 t…｜1｜0/0｜[关系][r45]｜[输出][o45]｜unknown｜unknown

[AI-000232][r46]｜GOV-02 控制端遗漏增量处置｜powershell｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜GOV-02: completed bounded 120-t…｜1｜0/0｜[关系][r46]｜[输出][o46]｜unknown｜unknown

[AI-000231][r47]｜GOV-01 控制端遗漏增量处置｜git-readonly｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜GOV-01增量完成：状态计数/注意项优先级/可复用全文快照、…｜1｜0/0｜[关系][r47]｜[输出][o47]｜unknown｜unknown

## PRE5C-FONTR / 批次 unknown

[AI-000206][r48]｜FONT-R2-COMPILE｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜同工程Unity6000.3.24f1真实Refresh/Co…｜1｜0/0｜[关系][r48]｜[输出][o48]｜unknown｜unknown

[AI-000205][r49]｜FONT-R2｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜FONT-R2：141行FontSwapOnce复用三材质GU…｜1｜0/0｜[关系][r49]｜[输出][o49]｜unknown｜unknown

[AI-000204][r50]｜FONT-R1｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r50]｜[输出][o50]｜unknown｜unknown

[AI-000203][r51]｜FONT-R0｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r51]｜[输出][o51]｜unknown｜unknown

[AI-000202][r52]｜FONT-R0-PRE｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜FONT-R0-PRE只读对账完成：核16场景/Prefab/…｜1｜0/0｜[关系][r52]｜[输出][o52]｜unknown｜unknown

[AI-000201][r53]｜PRE-5c｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜PRE-5c workflow implementation …｜2｜0/0｜[关系][r53]｜[输出][o53]｜unknown｜unknown

## PRE-5b / 批次 unknown

[AI-000200][r54]｜PRE-5b 监管瘦身与计量补全（…｜unknown｜CODE｜unknown｜unknown/unknown｜54523993/54052864/166276｜CV｜机器｜PRE-5b 无模型 WATCH、共享两轮根修复、机器验收、小…｜1｜0/0｜[关系][r54]｜[输出][o54]｜unknown｜unknown

## U1-DATA-SIGNED-ID-PROOF / 批次 unknown

[AI-000199][r55]｜修复原生根证明有符号localFi…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复生产C#与PowerShell本地文件ID有符号Int64…｜1｜0/0｜[关系][r55]｜[输出][o55]｜unknown｜unknown

## U1-DATA-NATIVE-ROOT-PROOF / 批次 unknown

[AI-000198][r56]｜修复生成共享Texture2D/S…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复AI194导表依赖证明对合法共享Texture2D+Spr…｜1｜0/0｜[关系][r56]｜[输出][o56]｜unknown｜unknown

## U1-DATA-PUBLISH-PROOF / 批次 unknown

[AI-000197][r57]｜绑定已验依赖候选并修复目录发布漂移…｜unknown｜CODE｜unknown｜unknown/unknown｜13584308/13376000/41315｜CV｜unknown｜已把真实同轮依赖证明候选摘要绑定到生产目录发布，补齐零差异、i…｜1｜0/0｜[关系][r57]｜[输出][o57]｜unknown｜unknown

## U1-DATA-SPINE-EXCEPTION / 批次 unknown

[AI-000196][r58]｜获批Spine遗留加载错误有限例外…｜unknown｜CODE｜unknown｜unknown/unknown｜15787818/15639040/57740｜CV｜unknown｜已实现限定旧Spine加载错误身份门、AnimationRef…｜1｜0/0｜[关系][r58]｜[输出][o58]｜unknown｜unknown

## U1-DATA-BATCH-GUARD / 批次 unknown

[AI-000195][r59]｜隔离导表路径与资源导入错误发布门修复｜unknown｜CODE｜unknown｜unknown/unknown｜11509577/11388672/36595｜CV｜unknown｜Shortened isolated DATA batch d…｜1｜0/0｜[关系][r59]｜[输出][o59]｜unknown｜unknown

## DATA-R1 / 批次 unknown

[AI-000194][r60]｜DATA-R1 USER-U1 r…｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜USER-U1-R1正式DATA-R1已完成：两轮独立Unit…｜4｜0/0｜[关系][r60]｜[输出][o60]｜unknown｜unknown

## U1-HEADER-PRESERVE-R1 / 批次 unknown

[AI-000193][r61]｜表头注释未知备注保留与兼容条目归属…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复AI189后隔离复现的同作者未知增补与无关联legacy备…｜1｜0/0｜[关系][r61]｜[输出][o61]｜unknown｜unknown

## U1-CONTENT-GUARD-R1 / 批次 unknown

[AI-000192][r62]｜Authoring部分发布回退与并…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复Authoring部分发布与回退契约：导出前冻结目标/备份…｜1｜0/0｜[关系][r62]｜[输出][o62]｜unknown｜unknown

## U1-R1-EXPORT / 批次 unknown

[AI-000191][r63]｜首轮合表格式注释修复与忠实导出｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜当前人工xlsx整表格式/119字段中文注释受保护发布，14份…｜1｜0/0｜[关系][r63]｜[输出][o63]｜unknown｜unknown

## U1-SCENE-VERIFY / 批次 unknown

[AI-000190][r64]｜真实厨房区域厨具清单与运行验证｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜AI190真实Unity6核验完成：CookingProces…｜1｜0/0｜[关系][r64]｜[输出][o64]｜unknown｜unknown

## U1-HEADER-DOC / 批次 unknown

[AI-000189][r65]｜统一合表所有表头补齐字段解释｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成14张schema表119个字段的中文表头用途/填法说明、…｜1｜0/0｜[关系][r65]｜[输出][o65]｜unknown｜unknown

## U1-SCENE-TOOLS / 批次 unknown

[AI-000188][r66]｜区域厨具改由场景实际配置识别｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000188 CODE完成：区域厨具由真实SceneTo…｜1｜0/0｜[关系][r66]｜[输出][o66]｜unknown｜unknown

## PRE5C-FONTR / 批次 unknown

[AI-000207][r67]｜FONT-R3｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜FONT-R3原207 attempt4：唯一第二Run(fa…｜4｜unknown/unknown｜[关系][r67]｜[输出][o67]｜unknown｜unknown

## unknown / 批次 unknown

[AI-000208][r68]｜PRE-5c AI201受控修复：…｜unknown｜CODE｜unknown｜unknown/1｜unknown/unknown/unknown｜CV｜机器｜累计120软限交接、240/30分钟临时硬停、任务拒验审计与取…｜1｜0/0｜[关系][r68]｜[输出][o68]｜unknown｜unknown

## PRE5C-FONTR / 批次 unknown

[AI-000209][r69]｜FONT-R3 已批准Shader…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜只读诊断5/5完成：确认原207 Shader相等守卫根因及既…｜1｜0/0｜[关系][r69]｜[输出][o69]｜unknown｜unknown

[AI-000210][r70]｜FONT-R3 精确Shader合…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜精确Shader合同修复；Fonts Final首次2/2、1…｜1｜0/0｜[关系][r70]｜[输出][o70]｜unknown｜unknown

## USAGE-OPT / 批次 unknown

[AI-000230][r71]｜状态摘要、验证复用与停止轮询降耗，…｜powershell｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜完成 CODE 用量优化：Status 紧凑输出、逐 runn…｜1｜0/0｜[关系][r71]｜[输出][o71]｜unknown｜unknown

## COVER-BUBBLE / 批次 unknown

[AI-000229][r72]｜封面两个气泡本地化文字与场景显隐接线｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜已用Unity MCP为原Cover两气泡各接入TMP+Loc…｜1｜0/0｜[关系][r72]｜[输出][o72]｜unknown｜unknown

## DATA-R2 / 批次 unknown

[AI-000228][r73]｜DATA-R2 USER-U1 r…｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜USER-U1-R2 DONE源经项目解析器核验后，仅更新Lo…｜1｜0/0｜[关系][r73]｜[输出][o73]｜unknown｜unknown

## COVER-BUBBLE-SOURCE / 批次 unknown

[AI-000227][r74]｜两个封面气泡文案源与忠实导出｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜已在Localization工作簿追加两条获批气泡文案并忠实导…｜1｜0/0｜[关系][r74]｜[输出][o74]｜unknown｜unknown

## LAYOUT-V03 / 批次 unknown

[AI-000226][r75]｜LAYOUT-V03 三弹层视口锚…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜三处 Shared 弹层根已用 Unity Editor 接入…｜2｜1/1｜[关系][r75]｜[输出][o75]｜unknown｜unknown

## UI-VIS-TIMING / 批次 unknown

[AI-000225][r76]｜线索板限定状态、冰箱食谱本连续显示…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜已通过Unity MCP保存ShortCycle_P0P1显隐…｜1｜0/0｜[关系][r76]｜[输出][o76]｜unknown｜unknown

[AI-000224][r77]｜按用户调整显示规则并新增可配置显隐…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜每Set显隐进度、FSM批处理和Kitchen视觉-only连…｜1｜0/0｜[关系][r77]｜[输出][o77]｜unknown｜unknown

## UI-VIS / 批次 unknown

[AI-000223][r78]｜ShortCycle显隐动画全路径…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜完成ShortCycle全状态显隐/动画原生验证；修复48个S…｜1｜0/0｜[关系][r78]｜[输出][o78]｜unknown｜unknown

[AI-000222][r79]｜ShortCycle全状态显隐归属…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜统一 ShortCycle 全状态显隐归属与切换起点时机；视觉…｜1｜0/0｜[关系][r79]｜[输出][o79]｜unknown｜unknown

## SC / 批次 unknown

[AI-000277][r80]｜SAVE-01 数字规范化与认证往…｜PowerShell 7+…｜CODE｜M｜80/unknown｜2455086/2382080/29472｜CV｜机器｜固定CODE；实际PowerShell7/Python3/Gi…｜1｜0/0｜[关系][r80]｜[输出][o80]｜unknown｜unknown

## E2-20261013 / 批次 unknown

[AI-000221][r81]｜E2远端恢复、私有保全与迁移配置解除｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜E2远端LFS证据子集独立恢复22/22；完整5133文件私有…｜1｜0/0｜[关系][r81]｜[输出][o81]｜unknown｜unknown

## E2-20261012 / 批次 unknown

[AI-000219][r82]｜E2迁移证据归档与恢复验证｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜本地E2迁移归档与恢复5133/5133通过，迁移侧9独有内容…｜1｜0/0｜[关系][r82]｜[输出][o82]｜unknown｜unknown

## FONT-R / 批次 unknown

[AI-000218][r83]｜已批准Static恢复与主工程只读…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜Attempt2 completed: one approve…｜2｜unknown/unknown｜[关系][r83]｜[输出][o83]｜unknown｜unknown

[AI-000217][r84]｜第三修复完整原生隔离验证｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜AI217 attempt2一次原生隔离补验完整9项通过；唯一…｜2｜unknown/unknown｜[关系][r84]｜[输出][o84]｜unknown｜unknown

[AI-000216][r85]｜冷首载快照一致性最小修复｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜Memory快照单次hash读取前移，生产净增2行；冷缓存旧拒…｜1｜0/0｜[关系][r85]｜[输出][o85]｜unknown｜unknown

[AI-000215][r86]｜冷首载快照差异取证｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜冷首载差异取证完成，六项合同通过；唯一变化为texture J…｜1｜0/0｜[关系][r86]｜[输出][o86]｜unknown｜unknown

[AI-000214][r87]｜Static保护真实Unity隔离…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜4｜unknown/unknown｜[关系][r87]｜[输出][o87]｜unknown｜unknown

[AI-000213][r88]｜Static原生材质副作用保护修复｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜AI213完成Static副作用最小绑定顺序和固定SHA/入口…｜1｜0/0｜[关系][r88]｜[输出][o88]｜unknown｜unknown

[AI-000212][r89]｜FONT-R3 新生产ratio失…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜同根Static ratio失败只读诊断完成：3399资源零漂…｜1｜0/0｜[关系][r89]｜[输出][o89]｜unknown｜unknown

## PRE5C-FONTR / 批次 unknown

[AI-000211][r90]｜FONT-R3 Shader修复真…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜当前源码真实Unity编译/PDB SHA核对通过；一次Run…｜1｜0/0｜[关系][r90]｜[输出][o90]｜unknown｜unknown

## E2-20261012 / 批次 unknown

[AI-000220][r91]｜E2长期工具功能更名与引用收口｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜机器｜Unity Editor 保GUID完成 ProjectVal…｜1｜0/0｜[关系][r91]｜[输出][o91]｜unknown｜unknown

## U1-STEP-REF / 批次 unknown

[AI-000187][r92]｜standard_ingredie…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜U1-STEP-REF：standard_ingredient…｜1｜0/0｜[关系][r92]｜[输出][o92]｜unknown｜unknown

## SC / 批次 unknown

[AI-000278][r93]｜窗口② D-T2/SEC02/P2…｜codex-provide…｜ENGINE_CLI｜M｜80/unknown｜11740399/11635712/50718｜CV｜人工｜队列车道：ENGINE_CLI；实际工具：unity-cli-…｜1｜0/0｜[关系][r93]｜[输出][o93]｜unknown｜unknown

[AI-000280][r94]｜D-T2/SAVE 隔离存档与动态…｜PowerShell 7+…｜CODE｜M｜80/unknown｜8859240/8684416/79214｜CV｜机器｜窗口②阶段①源码/离线完成：pre-Awake精确目标/fre…｜1｜0/0｜[关系][r94]｜[输出][o94]｜unknown｜unknown

## MIGF07 / 批次 unknown

[AI-000346][r95]｜MIGF07-SCHEMA CLI…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜MIGF07 read-only source-bound C…｜1｜0/0｜[关系][r95]｜[输出][o95]｜CODE(只读工具计量)｜task

## MIGF10 / 批次 unknown

[AI-000345][r96]｜MIGF10-LEGACY-MAP…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜MIGF10 read-only three-family d…｜1｜0/0｜[关系][r96]｜[输出][o96]｜CODE(只读依赖图)｜task

## E06 / 批次 unknown

[AI-000344][r97]｜E06-SCAN Travelin…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜E06-SCAN found an existing prod…｜1｜0/0｜[关系][r97]｜[输出][o97]｜CODE(只读文档)｜task

## WF / 批次 unknown

[AI-000343][r98]｜进度持久化诊断与原子读写协作修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜G87独立pipeline-repair：队列原子写失败结构化…｜1｜0/0｜[关系][r98]｜[输出][o98]｜CLI(磨合)｜pipeline-repair

[AI-000342][r99]｜精确helper载荷与真实eval…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜G87独立pipeline-repair：完整helper候选…｜1｜0/0｜[关系][r99]｜[输出][o99]｜CLI(磨合)｜pipeline-repair

## SC / 批次 unknown

[AI-000341][r100]｜旧适配器独立脚本身份与原生迁移交接…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Adapter class moved verbatim in…｜1｜0/0｜[关系][r100]｜[输出][o100]｜CODE(产品修复)｜task

## WF / 批次 unknown

[AI-000340][r101]｜窗口③只读预检载荷与调用预算修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜两份不同目的完整只读候选移除全局MonoScript扫描，保留…｜1｜0/0｜[关系][r101]｜[输出][o101]｜CLI(磨合)｜pipeline-repair

## UI / 批次 unknown

[AI-000339][r102]｜窗口③首段：承接320单问号、步骤…｜unity-officia…｜ENGINE_CLI｜L｜120/unknown｜42545016/42106240/280143｜CS｜行政｜unknown｜13｜11/11｜[关系][r102]｜unknown｜unknown｜unknown

## WF / 批次 unknown

[AI-000338][r103]｜历史直接修复豁免边界修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜历史直连豁免现限真实直接父根；根目标间接叶缺nature拒绝且…｜1｜0/0｜[关系][r103]｜[输出][o103]｜QUEUE(监管)｜pipeline-repair

[AI-000337][r104]｜同根二级修复拒验解除校验（pipe…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜修复同根二级拒验解除的后裔关联校验；隔离正式API67断言通过…｜1｜0/0｜[关系][r104]｜[输出][o104]｜QUEUE(监管)｜pipeline-repair

## SC / 批次 unknown

[AI-000336][r105]｜P2矩阵探针末回调取消结算修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜修复P2探针最终/恢复回调取消误报成功；新增10用例与原21/…｜1｜0/0｜[关系][r105]｜[输出][o105]｜unknown｜unknown

[AI-000335][r106]｜P2-C9 独立矩阵探针交付补齐｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜补齐 AI334 P2-C9 独立可调用矩阵探针；21新用例与…｜1｜0/0｜[关系][r106]｜[输出][o106]｜unknown｜unknown

[AI-000334][r107]｜P2-C9 CODE：Space_…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜P2-C9四态显隐、正交弹层/过渡与单AreaDetail分区…｜1｜0/0｜[关系][r107]｜[输出][o107]｜unknown｜unknown

[AI-000333][r108]｜MG0 打开阶段取消与销毁生命周期…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Same-root MG0 Host opening life…｜1｜0/0｜[关系][r108]｜[输出][o108]｜unknown｜unknown

[AI-000332][r109]｜MG0-STUB 小游戏契约与倒计…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜MG0 core host, modal adapter, d…｜1｜0/0｜[关系][r109]｜[输出][o109]｜unknown｜unknown

[AI-000331][r110]｜P2-C11 产出提交重入修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Same-root P2-C11 repair: one op…｜1｜0/0｜[关系][r110]｜[输出][o110]｜unknown｜unknown

[AI-000330][r111]｜P2-C11 产出物生成与组合命名…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜P2-C11 actual matcher-to-single…｜1｜0/0｜[关系][r111]｜[输出][o111]｜unknown｜unknown

[AI-000329][r112]｜P2-C10 操作区会话与增量路径锁｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜新增纯C#操作区会话，按T12多重集子集增量收窄候选，首份按G…｜1｜0/0｜[关系][r112]｜[输出][o112]｜unknown｜unknown

[AI-000328][r113]｜CLUE-01 内容与显示门协作修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜修复CLUE线索板RendererHidden期间库存刷新和重…｜1｜0/0｜[关系][r113]｜[输出][o113]｜unknown｜unknown

## PROBE-CI / 批次 unknown

[AI-000347][r114]｜PROBE-CI-01 既有Ver…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜PROBE-CI-01 CODE offline entry …｜1｜0/0｜[关系][r114]｜[输出][o114]｜CODE(探针CI离线实现)｜task

## SC / 批次 unknown

[AI-000327][r115]｜CLUE-01 线索板内容绑定｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜CLUE-01 fixed CODE delivered or…｜1｜0/0｜[关系][r115]｜[输出][o115]｜unknown｜unknown

## PROBE-CI / 批次 unknown

[AI-000348][r116]｜CI结果写入归属与任务身份边界修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜AI348同根结果归属/身份修复候选完成；24项定向隔离回归及…｜1｜0/0｜[关系][r116]｜[输出][o116]｜CODE(CI结果归属修复)｜task

## WF / 批次 unknown

[AI-000350][r117]｜人批追加额度与已审指令原子绑定修复｜PowerShell 7 …｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Approved HUMAN instruction and …｜1｜0/0｜[关系][r117]｜[输出][o117]｜QUEUE(监管)｜pipeline-repair

## L / 批次 unknown

[AI-000369][r118]｜V01-02 Build配置语义读…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜AI369 narrow Build non-scene se…｜1｜0/0｜[关系][r118]｜[输出][o118]｜CLI(磨合)｜pipeline-repair

[AI-000368][r119]｜V01-02 单例定向序列化与保存…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜AI364 Build单例显式路径单对象序列化与写前非scen…｜1｜0/0｜[关系][r119]｜[输出][o119]｜CLI(磨合)｜pipeline-repair

[AI-000367][r120]｜V01-02 精确Build配置持…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜AI364精确Build配置单例定向持久化候选、完整原卡续行指…｜1｜0/0｜[关系][r120]｜[输出][o120]｜CLI(磨合)｜pipeline-repair

[AI-000366][r121]｜V01-02 Build列表持久化…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜AI364 Build全局列表持久化候选、原卡完整最小续行指令…｜1｜0/0｜[关系][r121]｜[输出][o121]｜CLI(磨合)｜pipeline-repair

[AI-000365][r122]｜V01-02 只读预检类型解析修复｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜G87 pipeline repair for AI364: …｜1｜0/0｜[关系][r122]｜[输出][o122]｜CLI(磨合)｜pipeline-repair

[AI-000364][r123]｜V01-02 街景骨架与用户资产目录｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜9572231/9531008/38573｜CS｜行政｜unknown｜6｜1/1｜[关系][r123]｜unknown｜ENGINE_CLI｜task

[AI-000363][r124]｜V01-02 街景骨架入口空壳及m…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜Created attachable empty EatWha…｜1｜0/0｜[关系][r124]｜[输出][o124]｜CODE｜task

## CLUE-02 / 批次 unknown

[AI-000362][r125]｜CLUE-02 线索板四块八替代位…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜13012894/12962176/46367｜OPEN｜unknown｜unknown｜1｜0/0｜[关系][r125]｜unknown｜ENGINE_CLI｜task

## FIX-A2 / 批次 unknown

[AI-000361][r126]｜FIX-A2 图标尺寸修复原生接入…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2424141/2421120/9556｜CV｜人工｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r126]｜[输出][o126]｜ENGINE_CLI｜task

[AI-000360][r127]｜FIX-A2 StepDetail…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Runtime StepDetail icon sizing …｜1｜0/0｜[关系][r127]｜[输出][o127]｜CODE｜task

## SHORTCYCLE / 批次 unknown

[AI-000359][r128]｜PLAYERLOG-WARN-R …｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜Read-only three-warning review,…｜1｜0/0｜[关系][r128]｜[输出][o128]｜CODE｜verification

[AI-000358][r129]｜FIX-A2-R Edit配置与人…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜1620038/1617920/6491｜CV｜人工｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r129]｜[输出][o129]｜ENGINE_CLI｜task

[AI-000357][r130]｜FIX-A1-R Edit配置与人…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2769519/2758400/10481｜CV｜人工｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r130]｜[输出][o130]｜ENGINE_CLI｜task

## WF / 批次 unknown

[AI-000356][r131]｜CLI小型Edit交接简化与Ass…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Two bounded Edit-only candidate…｜1｜0/0｜[关系][r131]｜[输出][o131]｜CLI(磨合)｜pipeline-repair

[AI-000355][r132]｜AI339 helper原生引用子…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜完整helper候选精确绑定原生父引用与m_FileID子值；…｜1｜0/0｜[关系][r132]｜[输出][o132]｜CLI(磨合)｜pipeline-repair

[AI-000354][r133]｜helper真实序列化值与引用语义…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜Complete exact manager typed-pr…｜1｜0/0｜[关系][r133]｜[输出][o133]｜CLI(磨合)｜pipeline-repair

[AI-000353][r134]｜AI339 helper序列化比较…｜PowerShell 7 …｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜AI353 completed full guard-pres…｜1｜0/0｜[关系][r134]｜[输出][o134]｜CLI(磨合)｜pipeline-repair

[AI-000352][r135]｜AI339只读RPC超时诊断与完整…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜AI352 attempt2 completed bounde…｜2｜1/1｜[关系][r135]｜[输出][o135]｜CLI(磨合)｜pipeline-repair

[AI-000351][r136]｜剩余额度内已审纠错绑定与程序集修复｜PowerShell 7 …｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜同 HUMAN grant 已批余量内已审指令修订及 CI 后…｜1｜0/0｜[关系][r136]｜[输出][o136]｜QUEUE/CLI(机制)｜pipeline-repair

## PROBE-CI / 批次 unknown

[AI-000349][r137]｜CI Editor会话所有权与失败…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜AI349同根Editor会话归属修复候选完成；原24及新增1…｜1｜0/0｜[关系][r137]｜[输出][o137]｜CODE(CI会话所有权修复)｜task

## SC / 批次 unknown

[AI-000326][r138]｜TRAY-01 退回输入语义同根修复｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜AI326 同根产品修复：托盘外释放退回/内侧取消，冰箱预留占…｜1｜0/0｜[关系][r138]｜[输出][o138]｜unknown｜unknown

[AI-000325][r139]｜TRAY-01 托盘拖放逻辑与8桩｜unknown｜CODE｜L｜120/unknown｜unknown/unknown/unknown｜CV｜机器｜TRAY-01 CODE: T8占用槽/预留退回与单位身份、八…｜1｜0/0｜[关系][r139]｜[输出][o139]｜unknown｜unknown

## FIX / 批次 unknown

[AI-000324][r140]｜FIX-A2 步骤详情三态占位 C…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜FIX-A2 CODE: StepDetail 食材/工具三态…｜1｜0/0｜[关系][r140]｜[输出][o140]｜unknown｜unknown

## WF / 批次 unknown

[AI-000299][r141]｜REPAIR-CLI-04 截图保…｜PowerShell 7+…｜CODE｜S｜40/unknown｜2943721/2896384/29840｜CV｜机器｜CODE离线修复完成：原共享PNG保存器新增有界严格IDAT/…｜2｜1/1｜[关系][r141]｜[输出][o141]｜CLI(磨合)｜pipeline-repair

[AI-000298][r142]｜REPAIR-CLI-03 证据序…｜PowerShell7 e…｜CODE｜S｜40/unknown｜2976009/2940672/32759｜CV｜机器｜固定CODE；CLI(磨合)/pipeline-repair，…｜2｜1/1｜[关系][r142]｜[输出][o142]｜CLI(磨合)｜pipeline-repair

[AI-000297][r143]｜REPAIR-CLI-02 载荷字…｜PowerShell7/.…｜CODE｜S｜40/unknown｜2724272/2687360/27918｜CV｜机器｜固定CODE；CLI(磨合)/pipeline-repair，…｜1｜0/0｜[关系][r143]｜[输出][o143]｜CLI(磨合)｜pipeline-repair

[AI-000296][r144]｜REPAIR-CLI-01 队列落…｜PowerShell7/.…｜CODE｜S｜40/unknown｜3045506/3006080/21924｜CV｜机器｜固定CODE；CLI(磨合)，usageCategory=pi…｜1｜0/0｜[关系][r144]｜[输出][o144]｜CLI(磨合)｜pipeline-repair

## unknown / 批次 unknown

[AI-000295][r145]｜AI289失效组件槽身份与公开AP…｜unity-officia…｜ENGINE_CLI｜M｜80/unknown｜1673629/1629824/20846｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r145]｜[输出][o145]｜unknown｜unknown

## SC / 批次 unknown

[AI-000294][r146]｜AI289联合探针前置与转场组件同…｜PowerShell7/.…｜CODE｜M｜80/unknown｜4598784/4502144/32085｜CV｜机器｜固定CODE；实际PowerShell7/.NET Add-T…｜1｜0/0｜[关系][r146]｜[输出][o146]｜unknown｜unknown

[AI-000293][r147]｜AI288启动取消恢复的同根源码修复｜PowerShell7/.…｜CODE｜M｜80/unknown｜4367771/4277504/45435｜CV｜机器｜固定CODE；实际PowerShell7/.NET宿主与Lau…｜1｜0/0｜[关系][r147]｜[输出][o147]｜unknown｜unknown

[AI-000292][r148]｜SEC02 Windows Mon…｜unity-officia…｜ENGINE_CLI｜L｜120/unknown｜17691322/17581952/125679｜OPEN｜unknown｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜3｜2/2｜[关系][r148]｜[输出][o148]｜unknown｜unknown

[AI-000291][r149]｜SEC02 B阶段DATA显式隔离…｜unknown｜DATA｜S｜40/unknown｜1474522/1410304/14321｜CV｜机器｜AI290真实快照显式隔离blob导出四件完成，认证磁盘回读及…｜1｜0/0｜[关系][r149]｜[输出][o149]｜unknown｜unknown

[AI-000290][r150]｜SEC02 B阶段当前Catalo…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜13550692/13468032/99204｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜5｜2/2｜[关系][r150]｜[输出][o150]｜unknown｜unknown

[AI-000289][r151]｜P2S 空壳与A阶段原生联合验证｜codex-provide…｜ENGINE_CLI｜M｜80/unknown｜47840813/46944512/308125｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜10｜7/7｜[关系][r151]｜[输出][o151]｜unknown｜unknown

[AI-000288][r152]｜D-T2 原生接线与隔离前置（联合…｜unity-officia…｜ENGINE_CLI｜M｜80/unknown｜20844971/20301568/147840｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜4｜1/1｜[关系][r152]｜[输出][o152]｜unknown｜unknown

[AI-000287][r153]｜窗口②A 显式Editor/Pla…｜PowerShell7/.…｜CODE｜M｜80/unknown｜11225969/11090816/79035｜CV｜机器｜CODE，实际工具PowerShell7/.NET实际宿主夹具…｜1｜0/0｜[关系][r153]｜[输出][o153]｜unknown｜unknown

## D-T2 / 批次 unknown

[AI-000286][r154]｜D-T2 候选图标间隙修复｜PowerShell7 (…｜ENGINE_CLI｜M｜80/unknown｜2679757/2655360/18343｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：null (no o…｜1｜0/0｜[关系][r154]｜[输出][o154]｜unknown｜unknown

## SEC02 / 批次 unknown

[AI-000285][r155]｜SEC02 构建清理归属修复｜PowerShell7/.…｜CODE｜M｜80/unknown｜2553342/2506240/41989｜CV｜机器｜CODE同根REPAIR/POLICY AI282:CODE2…｜1｜0/0｜[关系][r155]｜[输出][o155]｜unknown｜unknown

## SC / 批次 unknown

[AI-000284][r156]｜P2S追加探针在旧18清理后的会话…｜PowerShell 7+…｜CODE｜M｜80/unknown｜5069427/4729344/50927｜CV｜机器｜CODE同根修复AI281:CODE284:retry1（PO…｜1｜0/0｜[关系][r156]｜[输出][o156]｜unknown｜unknown

[AI-000283][r157]｜D-T2 新节点精确布局候选（只读）｜PowerShell7 (…｜ENGINE_CLI｜M｜80/unknown｜6481614/6375936/55457｜CV｜人工｜队列车道：ENGINE_CLI；实际工具：null (no o…｜1｜0/0｜[关系][r157]｜[输出][o157]｜unknown｜unknown

[AI-000282][r158]｜SEC-02 发布数据源与构建管线…｜PowerShell 7+…｜CODE｜L｜120/unknown｜3769732/3705088/44298｜CV｜机器｜CODE，实际工具PowerShell7/Python只读夹具…｜3｜1/1｜[关系][r158]｜[输出][o158]｜unknown｜unknown

[AI-000281][r159]｜P2S-01 过渡交接与返回（源码…｜PowerShell 7+…｜CODE｜M｜80/unknown｜4234836/4140416/54241｜CV｜机器｜CODE阶段①源码/离线：沿原Adapter/Presente…｜1｜0/0｜[关系][r159]｜[输出][o159]｜unknown｜unknown

## WF / 批次 unknown

[AI-000300][r160]｜REPAIR-CLI-05 宿主R…｜PowerShell 7+…｜CODE｜M｜80/unknown｜8226085/8092416/58102｜CV｜机器｜CODE离线完成fresh task/run/target宿主…｜1｜0/0｜[关系][r160]｜[输出][o160]｜CLI(磨合)｜pipeline-repair

[AI-000301][r161]｜G87-METRICS CLI磨合…｜PowerShell 7+…｜CODE｜M｜80/unknown｜5324372/5271040/29848｜CV｜机器｜CODE离线完成G87类别/简表接入：wrapper仅扩pip…｜1｜0/0｜[关系][r161]｜[输出][o161]｜CLI(磨合)｜pipeline-repair

[AI-000302][r162]｜SMOKE-CLI-01 CLI截…｜unknown｜ENGINE_CLI｜S｜40/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r162]｜unknown｜CLI(磨合)｜pipeline-repair

[AI-000303][r163]｜SMOKE-CLI-01 CLI截…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜10868119/10761600/52855｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜3｜1/1｜[关系][r163]｜[输出][o163]｜CLI(磨合)｜pipeline-repair

## TK / 批次 unknown

[AI-000323][r164]｜TK-01 四工具类与Core单测｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜TK-DAT-05/06与TK-LAY-06/07四通用工具、…｜1｜0/0｜[关系][r164]｜[输出][o164]｜unknown｜unknown

## SC / 批次 unknown

[AI-000322][r165]｜PLAYERLOG-01 已有Pl…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜四组AI292 Player警告完成证据定位；缺脚本精确场景引…｜1｜0/0｜[关系][r165]｜[输出][o165]｜unknown｜unknown

## WF / 批次 unknown

[AI-000321][r166]｜GIT-CP-01 当前阶段本地检…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜本地检查点commit 1a754d7cc0bc3eb03c6…｜1｜0/0｜[关系][r166]｜[输出][o166]｜unknown｜unknown

## SC / 批次 unknown

[AI-000320][r167]｜人工缺陷Catalog场景落地与S…｜unknown｜ENGINE_CLI｜M｜80/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r167]｜unknown｜unknown｜unknown

[AI-000319][r168]｜人工缺陷Catalog单一锁定提示…｜unknown｜CODE｜M｜80/unknown｜unknown/unknown/unknown｜CV｜机器｜CODE完成Catalog锁定单问号互斥及空卡装饰显隐/重绑恢…｜1｜0/0｜[关系][r168]｜[输出][o168]｜unknown｜unknown

## WF / 批次 unknown

[AI-000318][r169]｜G87 Player编译空输出与预…｜PowerShell7+G…｜CODE｜M｜80/unknown｜5591021/5512192/34364｜CV｜机器｜G87独立pipeline-repair诊断关联AI292：原…｜1｜0/0｜[关系][r169]｜[输出][o169]｜CLI(磨合)｜pipeline-repair

[AI-000317][r170]｜G87显式Player宿主原生装载…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2899228/2841344/30978｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r170]｜[输出][o170]｜CLI(磨合)｜pipeline-repair

[AI-000316][r171]｜G87显式Player数据验收宿主…｜PowerShell7+P…｜CODE｜M｜80/unknown｜15683918/15513344/63436｜CV｜机器｜G87独立pipeline-repair：完成AI292 Pl…｜1｜0/0｜[关系][r171]｜[输出][o171]｜CLI(磨合)｜pipeline-repair

[AI-000315][r172]｜AI290编译断连后当前装载只读核对｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2415265/2410240/14309｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r172]｜[输出][o172]｜CLI(磨合)｜pipeline-repair

## SC / 批次 unknown

[AI-000279][r173]｜窗口②审批包场景新增节点映射修正（…｜PowerShell7 (…｜ENGINE_CLI｜M｜80/unknown｜2284225/2189568/30478｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：null (no o…｜1｜0/0｜[关系][r173]｜[输出][o173]｜unknown｜unknown

## WF / 批次 unknown

[AI-000314][r174]｜AI313修复已加载接口只读核对｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜1844700/1835392/11942｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r174]｜[输出][o174]｜ENGINE_CLI(只读装载核查)｜task

[AI-000312][r175]｜AI290 Sprite身份拒绝原…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2004434/1991552/17030｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r175]｜[输出][o175]｜CLI(磨合)｜pipeline-repair

[AI-000311][r176]｜AI290 Sprite身份拒绝对…｜PowerShell 7+…｜CODE｜S｜40/unknown｜4063803/4028288/20964｜CV｜机器｜原AI290 Sprite身份故障只读定位材料已完成：98字段…｜2｜1/1｜[关系][r176]｜[输出][o176]｜CLI(磨合)｜pipeline-repair

[AI-000310][r177]｜AI290快照反射失败离线只读诊断｜PowerShell 7+…｜CODE｜S｜40/unknown｜6620422/6500608/31587｜CV｜机器｜原AI290 attempt2只读诊断完成：真实内层异常与根因…｜1｜0/0｜[关系][r177]｜[输出][o177]｜CLI(磨合)｜pipeline-repair

[AI-000309][r178]｜AI290管线SceneHandl…｜unity-officia…｜ENGINE_CLI｜S｜40/unknown｜2344848/2296064/22919｜CV｜机器｜队列车道：ENGINE_CLI；实际工具：unity-offi…｜1｜0/0｜[关系][r178]｜[输出][o178]｜CLI(磨合)｜pipeline-repair

[AI-000308][r179]｜G87 CLI SceneHand…｜PowerShell 7+…｜CODE｜S｜40/unknown｜8952429/8819840/35167｜CV｜机器｜独立CLI caller修复：真实Unity6 SceneHa…｜1｜0/0｜[关系][r179]｜[输出][o179]｜CLI(磨合)｜pipeline-repair

[AI-000307][r180]｜AI306未消费许可空值与集合摘要…｜PowerShell 7+…｜CODE｜M｜80/unknown｜5821496/5399168/57203｜CV｜机器｜仅新显式重核结构对比改为InputObject保形摘要；历史G…｜1｜0/0｜[关系][r180]｜[输出][o180]｜CLI(磨合)｜pipeline-repair

[AI-000306][r181]｜AI289管线修复 未消费许可严格…｜PowerShell 7+…｜CODE｜M｜80/unknown｜9198077/9156224/79999｜CV｜机器｜既有ApproveResume新增显式同一未用grant严格重…｜1｜0/0｜[关系][r181]｜[输出][o181]｜CLI(磨合)｜pipeline-repair

[AI-000305][r182]｜AI303冒烟两处动作常量参数修正｜PowerShell 7+…｜CODE｜S｜40/unknown｜1831591/1790976/14477｜CV｜机器｜仅本卡输出两份候选各一处动作参数改为实际全限定OpenCove…｜1｜0/0｜[关系][r182]｜[输出][o182]｜CLI(磨合)｜pipeline-repair

[AI-000304][r183]｜AI303 Ready时限与真实C…｜PowerShell 7+…｜CODE｜M｜80/unknown｜7151382/7008896/63998｜CV｜机器｜Ready默认45/最大60/Arm90且入口30保持；严格默…｜1｜0/0｜[关系][r183]｜[输出][o183]｜CLI(磨合)｜pipeline-repair

## SEC-02 / 批次 unknown

[AI-000313][r184]｜SEC-02 AI290已空Spr…｜PowerShell7+P…｜CODE｜M｜80/unknown｜7994481/7861888/36140｜CV｜机器｜固定CODE使用PowerShell7/Python3/Mon…｜1｜0/0｜[关系][r184]｜[输出][o184]｜CODE(产品修复)｜task

## L / 批次 unknown

[AI-000370][r185]｜V01-02 Build设置对象与…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜CV｜机器｜Guarded EditorBuildSettings sin…｜1｜0/0｜[关系][r185]｜[输出][o185]｜CLI(磨合)｜pipeline-repair

## U1-FORMAT / 批次 unknown

[AI-000186][r186]｜导表时合表整体格式规范化｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成隔离工作簿格式规范化与共享发布漂移门；CODE仅交工具和副…｜2｜0/0｜[关系][r186]｜[输出][o186]｜unknown｜unknown

## U1-R1-CHECK / 批次 unknown

[AI-000184][r187]｜首轮人工工作簿隔离导出与问题清单｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜首轮人工xlsx隔离导出与检查完成，DATA_INVALID：…｜1｜0/0｜[关系][r187]｜[输出][o187]｜unknown｜unknown

## CK01-K / 批次 unknown

[AI-000067][r188]｜FIX-T2 · 线索板可见性规则…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成共享Set可见性OR合并、RendererHidden常驻…｜1｜0/0｜[关系][r188]｜[输出][o188]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000066][r189]｜FIX-T1 TMP Render…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已依据用户直接授权完成 ShortCycle_P0P1 场景修…｜1｜0/0｜[关系][r189]｜[输出][o189]｜unknown｜unknown

[AI-000065][r190]｜C-T5b P0P1 资产配置与数…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 C-T5b 授权场景实例换图与数值排布；依用户决策使用真…｜1｜0/0｜[关系][r190]｜[输出][o190]｜unknown｜unknown

[AI-000064][r191]｜CAT-T2 冰箱猫骨架 Pref…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜CAT-T2 approved migration saved…｜1｜0/0｜[关系][r191]｜[输出][o191]｜unknown｜unknown

[AI-000063][r192]｜CAT-T1 冰箱猫骨架组件与绑定…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CAT-T1 冰箱猫骨架纯代码实现：新增可复用层架池与五…｜1｜0/0｜[关系][r192]｜[输出][o192]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000062][r193]｜TEMP-T1 线索板视觉样例与空…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 VerifyTemp 线索板样例、正式 SlotBloc…｜1｜0/0｜[关系][r193]｜[输出][o193]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000061][r194]｜B-T8V 本地化导表与 Unit…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜通过既有 DataTableImportService 将 L…｜1｜0/0｜[关系][r194]｜[输出][o194]｜unknown｜unknown

[AI-000060][r195]｜B-T8 本地化权威源补键｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜修复 Localization 聚合资产加载时未初始化 Loc…｜1｜0/0｜[关系][r195]｜[输出][o195]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000059][r196]｜DIAG-T2 TMP 中文文本不…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Diagnosed the ESC-only visibili…｜1｜0/0｜[关系][r196]｜[输出][o196]｜unknown｜unknown

[AI-000058][r197]｜DIAG-T1V 键盘输入运行时验证｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 DIAG-T1V 运行时接收验证：两次稳定 Play 中…｜1｜0/0｜[关系][r197]｜[输出][o197]｜unknown｜unknown

[AI-000057][r198]｜J-T6prime 第三批资产增量…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成第三批精确增量导入：经用户授权覆盖 ui_caipu_ku…｜1｜0/0｜[关系][r198]｜[输出][o198]｜unknown｜unknown

[AI-000056][r199]｜DIAG-T1 键盘输入诊断与最小…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已定位键盘无响应的单一主因：DebugAndReference…｜1｜0/0｜[关系][r199]｜[输出][o199]｜unknown｜unknown

[AI-000055][r200]｜J-T6 本批资产导入替换与尺寸回归｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 AI-000054 同批只读收尾审计：独立复核 51 项…｜1｜0/0｜[关系][r200]｜[输出][o200]｜unknown｜unknown

[AI-000054][r201]｜J-T6 本FIN批资产导入替换与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-J/J-T6 本批资产导入、替换与尺寸回归：当…｜1｜0/0｜[关系][r201]｜[输出][o201]｜unknown｜unknown

[AI-000053][r202]｜NAS 美术资产安全增量同步｜unknown｜ART_AIGC｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 NAS→项目根美术资产同步区的安全增量同步：使用 Pow…｜1｜0/0｜[关系][r202]｜[输出][o202]｜unknown｜unknown

## RENDERING / 批次 unknown

[AI-000052][r203]｜Sprite 阴影与描边兼容 Sh…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜六要素：1) 已完成：在原 EatWhat/2D/Sprite…｜1｜0/0｜[关系][r203]｜[输出][o203]｜unknown｜unknown

## CK01-K / 批次 unknown

[AI-000051][r204]｜K-T2b · 引导职责归属与 D…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 K-T2b：SessionManager 接管固定五段引…｜1｜0/0｜[关系][r204]｜[输出][o204]｜unknown｜unknown

[AI-000050][r205]｜K-T3 · 场景装配与接管｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜CK01-K K-T3 已完成 ShortCycle_P0P1…｜1｜0/0｜[关系][r205]｜[输出][o205]｜unknown｜unknown

[AI-000049][r206]｜K-T2 · 重构实施｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-K K-T2 纯代码重构：Host/FSM/D…｜1｜0/0｜[关系][r206]｜[输出][o206]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000068][r207]｜F-T1 · 冰箱格架滚轮最小实装｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 Phase1 冰箱格架滚轮最小实装：MouseScrol…｜1｜0/0｜[关系][r207]｜[输出][o207]｜unknown｜unknown

## CK01-K / 批次 unknown

[AI-000048][r208]｜K-T1 · P0-P1只读走查与…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-K K-T1 只读走查：37/37 项逐行核对…｜1｜0/0｜[关系][r208]｜[输出][o208]｜unknown｜unknown

[AI-000069][r209]｜FIX-T3 · 锚点、Set、探…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜FIX-T3 已在保留现有用户现场的 ShortCycle_P…｜3｜0/0｜[关系][r209]｜[输出][o209]｜unknown｜unknown

[AI-000071][r210]｜FIX-T4 · 封面翻开默认进入…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜FIX-T4 纯代码完成：新会话无选菜；Cover→Catal…｜1｜0/0｜[关系][r210]｜[输出][o210]｜unknown｜unknown

## SCROLL-LAB-T3 / 批次 unknown

[AI-000090][r211]｜FridgeScrollLab独立…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜FridgeScrollLab 完成两轮独立 Play 机器回…｜1｜0/0｜[关系][r211]｜[输出][o211]｜unknown｜unknown

## SCROLL-LAB-T2 / 批次 unknown

[AI-000089][r212]｜新建FridgeScrollLab…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已在 Unity 2022.3.62f2 稳定 Edit Mo…｜1｜0/0｜[关系][r212]｜[输出][o212]｜unknown｜unknown

## SCROLL-LAB-T1 / 批次 unknown

[AI-000088][r213]｜冰箱猫滚轮独立Lab源码、诊断与搭…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜1) 做了什么：交付合法Phase1薄宿主、REAL_INPU…｜1｜0/0｜[关系][r213]｜[输出][o213]｜unknown｜unknown

## OUT-T0 / 批次 unknown

[AI-000087][r214]｜OUT-T0 描边阴影只读盘点、G…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成描边/阴影只读盘点：确认现存21个Group均不含CatR…｜1｜0/0｜[关系][r214]｜[输出][o214]｜unknown｜unknown

## B-T10-L4 / 批次 unknown

[AI-000086][r215]｜B-T10-L4 修复DragLi…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000086 已修复 DragContainer 内部工…｜1｜0/0｜[关系][r215]｜[输出][o215]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000085][r216]｜B-T10-L3 · 隐藏Bind…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复隐藏 Bind 在 ScrollArea 初始化前按旧边界…｜1｜0/0｜[关系][r216]｜[输出][o216]｜unknown｜unknown

[AI-000084][r217]｜B-T10-L2 · 生命周期修复…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000084 第2次控制台恢复完成：复用 AI-0000…｜3｜0/0｜[关系][r217]｜[输出][o217]｜unknown｜unknown

[AI-000083][r218]｜B-T10-L1 · Set与滚动…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 ShortCyclePresentationSet 生命…｜1｜0/0｜[关系][r218]｜[输出][o218]｜unknown｜unknown

[AI-000082][r219]｜B-T9-API · 定稿只读属性…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 B-T9-API 契约收口：新增 UnknownProd…｜1｜0/0｜[关系][r219]｜[输出][o219]｜unknown｜unknown

## VFX / 批次 unknown

[AI-000081][r220]｜VFX-T3 · 泡泡实机验证与人…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜VFX-T3 运行机器门完成。修复 GlobalLight2D…｜1｜0/0｜[关系][r220]｜[输出][o220]｜unknown｜unknown

[AI-000080][r221]｜VFX-T2 · VisualEf…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 VisualEffectsLab Edit Mode 静…｜1｜0/0｜[关系][r221]｜[输出][o221]｜unknown｜unknown

[AI-000079][r222]｜VFX-T1 · 醚质泡泡工具 S…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜VFX-T1源码完成：显式双相机后景捕获、醚质泡泡局部折射Sh…｜1｜0/0｜[关系][r222]｜[输出][o222]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000078][r223]｜B-T10 · v2.4 重导、六…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 B-T10：Unity 通用导入器将 Schema v2…｜1｜0/0｜[关系][r223]｜[输出][o223]｜unknown｜unknown

[AI-000077][r224]｜B-T9 · Schema v2.…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Schema v2.4 与 Q-01b CODE 段完成：14…｜1｜0/0｜[关系][r224]｜[输出][o224]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000076][r225]｜F-T1d · 滚轮诊断探针挂载与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000076 已在 ShortCycle_P0P1 的 …｜1｜0/0｜[关系][r225]｜[输出][o225]｜unknown｜unknown

[AI-000075][r226]｜F-T1c · 冰箱真实滚轮输入链…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成默认关闭的冰箱真实滚轮只读诊断探针、逐层分类器、最小只读观…｜1｜0/0｜[关系][r226]｜[输出][o226]｜unknown｜unknown

## CK01-K / 批次 unknown

[AI-000074][r227]｜FIX-T2d · CameraF…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已将 CameraFrameGizmo 唯一 MonoBeha…｜1｜0/0｜[关系][r227]｜[输出][o227]｜unknown｜unknown

[AI-000073][r228]｜FIX-T2c · CameraF…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 CameraFrameGizmoMenu 的 Unity…｜1｜0/0｜[关系][r228]｜[输出][o228]｜unknown｜unknown

[AI-000072][r229]｜FIX-T4b · 封面到目录入口…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜已在 ShortCycle_P0P1 以 Unity 序列化 …｜1｜0/0｜[关系][r229]｜[输出][o229]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000070][r230]｜F-T1b · 冰箱滚轮场景接线与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜已在 ShortCycle_P0P1 的 ScrollRegi…｜1｜0/0｜[关系][r230]｜[输出][o230]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000047][r231]｜C-T5a-R · 编辑态可视复原…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 C-T5a-R：在授权范围内把 Spaces 的 Spr…｜1｜0/0｜[关系][r231]｜[输出][o231]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000046][r232]｜J-T5 · RecipeBook…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-J-T5：新增唯一 RecipeBook 路由…｜1｜0/0｜[关系][r232]｜[输出][o232]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000045][r233]｜C-T5a · 功能树启用、结构补…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜完成 CK01-C-T5a 结构落地：清除 AI41 平行层与…｜1｜0/0｜[关系][r233]｜[输出][o233]｜unknown｜unknown

## CK01-A / 批次 unknown

[AI-000020][r234]｜CK01-A-T1 · P0-P1…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-A-T1 只读裁决草案：44 个 Cookin…｜1｜0/0｜[关系][r234]｜[输出][o234]｜unknown｜unknown

## unknown / 批次 unknown

[AI-000019][r235]｜执行KitchenArea示例导入…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已通过 Unity MCP/Editor API 完成统一 C…｜1｜0/0｜[关系][r235]｜[输出][o235]｜unknown｜unknown

[AI-000018][r236]｜建立统一CSV导表框架与Kitch…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Completed the generic UTF-8 CSV…｜1｜0/0｜[关系][r236]｜[输出][o236]｜unknown｜unknown

[AI-000017][r237]｜修复ModuleId零匹配聚合与空…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Fixed zero-match ModuleId Statu…｜1｜0/0｜[关系][r237]｜[输出][o237]｜unknown｜unknown

[AI-000016][r238]｜改造队列人工验收清单与模块ID机制｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Implemented module-aware queue …｜1｜0/0｜[关系][r238]｜[输出][o238]｜unknown｜unknown

[AI-000015][r239]｜接入标签自适应并将文字限制在黄色区域｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已通过 Unity MCP 为 CookingPrepare …｜1｜0/0｜[关系][r239]｜[输出][o239]｜unknown｜unknown

[AI-000014][r240]｜实现冰箱食材标签中文动态字号与双行…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜新增冰箱食材标签 TMP 双行/动态字号组件与纯文本格式器；提…｜1｜0/0｜[关系][r240]｜[输出][o240]｜unknown｜unknown

[AI-000013][r241]｜按参考图调整 CookingPre…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜仅通过 Unity MCP/Editor API 在 Asse…｜1｜0/0｜[关系][r241]｜[输出][o241]｜unknown｜unknown

[AI-000012][r242]｜静态梳理 Spine Sample…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 Spine Sample 场景的离线 YAML、Pref…｜1｜0/0｜[关系][r242]｜[输出][o242]｜unknown｜unknown

[AI-000011][r243]｜静态梳理 CookingProce…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CookingProcess 场景的离线 YAML、GU…｜3｜0/0｜[关系][r243]｜[输出][o243]｜unknown｜unknown

[AI-000010][r244]｜修复动画换帧时 SpriteOut…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已复用并完成 SpriteOutline2D 的最小换帧同步修…｜2｜0/0｜[关系][r244]｜[输出][o244]｜unknown｜unknown

[AI-000009][r245]｜搭建并验证横版 PlayerCon…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成既有 HorizontalPlayerController…｜1｜0/0｜[关系][r245]｜[输出][o245]｜unknown｜unknown

[AI-000008][r246]｜实现横版街景左右移动 Player…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜新增仅水平移动的 HorizontalPlayerContro…｜3｜0/0｜[关系][r246]｜[输出][o246]｜unknown｜unknown

[AI-000007][r247]｜用 Unity MCP 核实并补全…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Completed the full static Unity…｜2｜0/0｜[关系][r247]｜[输出][o247]｜unknown｜unknown

[AI-000006][r248]｜梳理 CookingPrepare…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CookingPrepare 场景脚本、调用链、资源契约…｜3｜0/0｜[关系][r248]｜[输出][o248]｜unknown｜unknown

[AI-000005][r249]｜按效果图重构 CookingPre…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜依据参考图完成 CookingPrepare 美术层级重构：形…｜1｜0/0｜[关系][r249]｜[输出][o249]｜unknown｜unknown

[AI-000004][r250]｜修复 SpriteOutline2…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Refined SpriteOutline2D neighbo…｜1｜0/0｜[关系][r250]｜[输出][o250]｜unknown｜unknown

[AI-000003][r251]｜导入并接线冰箱猫 Idle/同步眨…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜复用并完成 CookingPrepare 冰箱猫 Idle/同…｜2｜0/0｜[关系][r251]｜[输出][o251]｜unknown｜unknown

[AI-000002][r252]｜实现冰箱猫 3-5 秒随机同步眨眼…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Added FridgeCatBlinkScheduler w…｜1｜0/0｜[关系][r252]｜[输出][o252]｜unknown｜unknown

## CK01-A / 批次 unknown

[AI-000021][r253]｜CK01-A-T2 · Edito…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-A-T2 Unity Editor/MCP 只…｜1｜0/0｜[关系][r253]｜[输出][o253]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000022][r254]｜CK01-J-T1 · NAS→工…｜unknown｜ART_AIGC｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-J NAS→工程美术同步：使用 DevTool…｜1｜0/0｜[关系][r254]｜[输出][o254]｜unknown｜unknown

[AI-000023][r255]｜CK01-J-T2 · 新美术资产…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-J P0-P1 新资产基线：安全部署 5 个已…｜1｜0/0｜[关系][r255]｜[输出][o255]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000024][r256]｜CK01-B-T1 · schem…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Completed CK01-B static Schema …｜1｜0/0｜[关系][r256]｜[输出][o256]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000044][r257]｜J-T4 · 同步资产增量部署、基…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成CK01-J J-T4：核定AI-000042新增7张参考…｜1｜0/0｜[关系][r257]｜[输出][o257]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000043][r258]｜C-T8 · 动作集、镜头事件、溢…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T8 纯代码实现：13 个具名动作与 re…｜1｜0/0｜[关系][r258]｜[输出][o258]｜unknown｜unknown

## CK01-J / 批次 unknown

[AI-000042][r259]｜J-T3 · NAS 增量同步与指…｜unknown｜ART_AIGC｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已完成 J-T3 NAS 美术资产正式同步与清单复核：使用 P…｜1｜0/0｜[关系][r259]｜[输出][o259]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000041][r260]｜C-T5 · AI-000040 …｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜完成 CK01-C-T5 的 AI40 全量返工：按指导书重排…｜1｜0/0｜[关系][r260]｜[输出][o260]｜unknown｜unknown

[AI-000040][r261]｜C-T5-v2.2 · v0.1 …｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜在既有 ShortCycle S1 上增量完成 CK01-C-…｜1｜0/0｜[关系][r261]｜[输出][o261]｜unknown｜unknown

[AI-000039][r262]｜C-T6-v2.2 · 锚点、替代…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T6 v2.2 纯代码增补：20/8 固定…｜1｜0/0｜[关系][r262]｜[输出][o262]｜unknown｜unknown

[AI-000038][r263]｜S1-v2.2 · _straig…｜unknown｜ART_AIGC｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成增量美术资产同步：新增 7 张视觉参考 PNG，更新 0、…｜1｜0/0｜[关系][r263]｜[输出][o263]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000037][r264]｜CK01-B-T7 · v1.2 …｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜CK01-B v1.2/Schema v2.2 已完成 Uni…｜1｜0/0｜[关系][r264]｜[输出][o264]｜unknown｜unknown

[AI-000036][r265]｜CK01-B-T6 · 内容 CS…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜CK01-B content CSV v1.2 and Sch…｜1｜0/0｜[关系][r265]｜[输出][o265]｜unknown｜unknown

## OUT-T1 / 批次 unknown

[AI-000091][r266]｜OUT-T1 描边阴影分层控制、默…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成 OUT-T1 正式 v2 分层控制前半段：保留旧序列化/…｜1｜0/0｜[关系][r266]｜[输出][o266]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000035][r267]｜CK01-C-T6 · 显性 FS…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T6：三相与子态显性状态类 FSM、独立 …｜1｜0/0｜[关系][r267]｜[输出][o267]｜unknown｜unknown

[AI-000033][r268]｜CK01-C-T5 · 场景架构重…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T5：ShortCycle_P0P1 重构…｜1｜0/0｜[关系][r268]｜[输出][o268]｜unknown｜unknown

[AI-000032][r269]｜CK01-C-T4 · 交互框架接…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T4 代码侧架构修正：五个 builder…｜1｜0/0｜[关系][r269]｜[输出][o269]｜unknown｜unknown

## SCENE-GUIDE / 批次 unknown

[AI-000031][r270]｜CookingPrepare 层级…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已完成 CookingPrepare 场景组织只读审计：564…｜1｜0/0｜[关系][r270]｜[输出][o270]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000030][r271]｜CK01-B-T5 · 正式内容导…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜CK01-B-T5 已按修订边界完成：16 个无反向依赖旧生成…｜1｜0/0｜[关系][r271]｜[输出][o271]｜unknown｜unknown

[AI-000029][r272]｜CK01-B-T4 · 正式内容落…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜CK01-B-T4 已完成：13 份正式输入与权威源 SHA-…｜1｜0/0｜[关系][r272]｜[输出][o272]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000028][r273]｜CK01-C-T2 · 新场景搭建…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T2 独立 ShortCycle_P0P1…｜1｜0/0｜[关系][r273]｜[输出][o273]｜unknown｜unknown

[AI-000027][r274]｜CK01-C-T1 · 短循环骨架…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CK01-C-T1 纯代码短循环骨架：新增 Contex…｜1｜0/0｜[关系][r274]｜[输出][o274]｜unknown｜unknown

## CK01-B / 批次 unknown

[AI-000026][r275]｜CK01-B-T3 · 导表执行与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜通过 Unity MCP 完成 CK01-B-T3：修复导表器…｜1｜0/0｜[关系][r275]｜[输出][o275]｜unknown｜unknown

[AI-000025][r276]｜CK01-B-T2 · 导表器扩展…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜Completed CK01-B-T2 pure-code i…｜1｜0/0｜[关系][r276]｜[输出][o276]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000034][r277]｜CK01-C-T4B · 数据聚合…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜新增 CK01GeneratedDataCatalog 作为 …｜1｜0/0｜[关系][r277]｜[输出][o277]｜unknown｜unknown

## U1-CONTENT-GUARD / 批次 unknown

[AI-000185][r278]｜非空配表内容保护与写入边界｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成Authoring共享非空内容保护：逐格内容/结构快照与d…｜1｜0/0｜[关系][r278]｜[输出][o278]｜unknown｜unknown

## OUT-T1c / 批次 unknown

[AI-000092][r279]｜OUT-T1c 描边阴影改造后真实…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜OUT-T1c 真实 Unity 编译门通过：Unity 20…｜1｜0/0｜[关系][r279]｜[输出][o279]｜unknown｜unknown

## OUT-LAB-T3 / 批次 unknown

[AI-000094][r280]｜VisualEffectsLab描…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 VisualEffectsLab 描边阴影 v2 真实 …｜1｜0/0｜[关系][r280]｜[输出][o280]｜unknown｜unknown

## PRE-2a / 批次 unknown

[AI-000160][r281]｜PRE-2a DATA车道、隔离导…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 PRE-2a：在现有队列/监管生命周期内交付 DATA …｜1｜0/0｜[关系][r281]｜[输出][o281]｜unknown｜unknown

## PRE-3 / 批次 unknown

[AI-000159][r282]｜PRE-3 核心入口与按需索引、文…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜PRE-3完成：建立CORE与按需索引，长协议无损迁移，P/T…｜1｜0/0｜[关系][r282]｜[输出][o282]｜unknown｜unknown

## PRE-4 / 批次 unknown

[AI-000158][r283]｜PRE-4 DIAG-T3 托盘三…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜PRE-4 只读取证完成：托盘判定为缺陷（inactive 生…｜1｜0/0｜[关系][r283]｜[输出][o283]｜unknown｜unknown

## PRE-5 / 批次 unknown

[AI-000157][r284]｜PRE-5 真实用量记录、返回体规…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 PRE-5：真实/缺失用量追加记录、日报周报聚合、ENG…｜1｜0/0｜[关系][r284]｜[输出][o284]｜unknown｜unknown

## FIX-T5 / 批次 unknown

[AI-000156][r285]｜FIX-T5 正式场景四区空间重排…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜完成 FIX-T5 四区空间重排、P0Left/P1 镜头路由…｜1｜0/0｜[关系][r285]｜[输出][o285]｜unknown｜unknown

[AI-000155][r286]｜FIX-T5 子视图机位与视野判定…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜FIX-T5a 已完成：四槽与 Phase0 子视图机位路由、…｜1｜0/0｜[关系][r286]｜[输出][o286]｜unknown｜unknown

## MIG-F01R / 批次 unknown

[AI-000154][r287]｜MIG-F01R Lab修复与正式…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 MIG-F01R：Lab 与正式 ShortCycle …｜1｜0/0｜[关系][r287]｜[输出][o287]｜unknown｜unknown

[AI-000153][r288]｜MIG-F01R Modal滚轮阻…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 MIG-F01R CODE：新增 Phase1 专用 V…｜1｜0/0｜[关系][r288]｜[输出][o288]｜unknown｜unknown

## MIG-F04 / 批次 unknown

[AI-000152][r289]｜MIG-F04 无指针消费门控补修｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜新增 Tools.HasPointer、逐帧主相机缓存失效和缺…｜1｜0/0｜[关系][r289]｜[输出][o289]｜unknown｜unknown

## B-T12 / 批次 unknown

[AI-000151][r290]｜B-T12 Unity 6 首轮导…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 F4-ENGINE Unity 6 首轮导表：共享校验 …｜1｜0/0｜[关系][r290]｜[输出][o290]｜unknown｜unknown

## B-T11 / 批次 unknown

[AI-000150][r291]｜B-T11 配表 Excel 往返…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 F3-CODE 配表双入口：14-sheet Excel…｜1｜0/0｜[关系][r291]｜[输出][o291]｜unknown｜unknown

## MIG-F01 / 批次 unknown

[AI-000149][r292]｜MIG-F01 FridgeScr…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜完成 MIG-F01 F2-ENGINE：修复并扩充长期滚轮探…｜1｜0/0｜[关系][r292]｜[输出][o292]｜unknown｜unknown

[AI-000148][r293]｜MIG-F01 滚轮统一单位与 T…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 MIG-F01 F2-CODE：MouseManager…｜1｜0/0｜[关系][r293]｜[输出][o293]｜unknown｜unknown

## MIG-F04 / 批次 unknown

[AI-000147][r294]｜MIG-F04 Unity 6 无…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 F1 ENGINE Unity 6 Play 容错验证：…｜1｜0/0｜[关系][r294]｜[输出][o294]｜unknown｜unknown

[AI-000146][r295]｜MIG-F04 无鼠标与无主相机容…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 Tools.getMousePos 的无鼠标/无主相机空…｜1｜0/0｜[关系][r295]｜[输出][o295]｜unknown｜unknown

## REVIEW-T6d / 批次 unknown

[AI-000145][r296]｜REVIEW-T6d 满层数量与标…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜原任务获直接 /approve 后完成批准范围恢复：丢弃并保全…｜1｜0/0｜[关系][r296]｜[输出][o296]｜unknown｜unknown

## REVIEW-T6c / 批次 unknown

[AI-000144][r297]｜REVIEW-T6c 六层有效内容…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜REVIEW-T6c 六层有效内容机器补证完成：仅在Play内…｜1｜0/0｜[关系][r297]｜[输出][o297]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000143][r298]｜CAM-T2b · 中途返回原机位…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 ShortCycleCameraPanControlle…｜1｜0/0｜[关系][r298]｜[输出][o298]｜unknown｜unknown

## ART-DEPLOY / 批次 unknown

[AI-000142][r299]｜COVER-T1 · 已批准封面及…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜COVER-T1 已以既有 PresentationSet/S…｜1｜0/0｜[关系][r299]｜[输出][o299]｜unknown｜unknown

## PRE-1 / 批次 unknown

[AI-000161][r300]｜PRE-1 静态主字体与动态回退生…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成PRE-1纯代码交付：确定性216∪Localizatio…｜1｜0/0｜[关系][r300]｜[输出][o300]｜unknown｜unknown

## OUT-ALPHA / 批次 unknown

[AI-000141][r301]｜OUT-ALPHA-T2 · La…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜AI-000141 completed real Unity …｜1｜0/0｜[关系][r301]｜[输出][o301]｜unknown｜unknown

## PRE-1 / 批次 unknown

[AI-000162][r302]｜PRE-1 双字体资产生成、正式场…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜已通过 Unity 6000.3.24f1 生成并幂等验证 S…｜1｜0/0｜[关系][r302]｜[输出][o302]｜unknown｜unknown

## PRE-2b / 批次 unknown

[AI-000164][r303]｜PRE-2b DATA首跑：隔离b…｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI164正常宿主两真实CurrentCsvValidatio…｜2｜0/0｜[关系][r303]｜[输出][o303]｜unknown｜unknown

## unknown / 批次 unknown

[AI-000183][r304]｜PRE-1c-META-GUARD…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜升级字体迁移至schema10，共用生产阶段策略，既有meta…｜1｜0/0｜[关系][r304]｜[输出][o304]｜unknown｜unknown

## PRE-1c-TRANSACTION-CONTRACT-R1 / 批次 unknown

[AI-000182][r305]｜PRE-1c-TRANSACTIO…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复诊断与计划一致性及分阶段事务快照/回滚契约；schema …｜1｜0/0｜[关系][r305]｜[输出][o305]｜unknown｜unknown

## PRE-1c-SAVED-STATE-R1 / 批次 unknown

[AI-000181][r306]｜PRE-1c-SAVED-STAT…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成PRE-1c保存态统一修复：ShortCycleFontS…｜1｜0/0｜[关系][r306]｜[输出][o306]｜unknown｜unknown

## PRE-1c-MATERIAL-RESOLUTION-R1 / 批次 unknown

[AI-000180][r307]｜PRE-1c-MATERIAL-R…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 TMP 原始序列化引用与加载后有效材质双视图修复；sch…｜1｜0/0｜[关系][r307]｜[输出][o307]｜unknown｜unknown

## PRE-1c-PREFAB-REFERENCE-R1 / 批次 unknown

[AI-000179][r308]｜PRE-1c-PREFAB-REF…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜为真实m_PrefabInstance增加严格保存Global…｜1｜0/0｜[关系][r308]｜[输出][o308]｜unknown｜unknown

## PRE-1c-FINGERPRINT-R1 / 批次 unknown

[AI-000178][r309]｜PRE-1c-FINGERPRIN…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成非目标指纹v2：对象引用原子化，持久GlobalObjec…｜1｜0/0｜[关系][r309]｜[输出][o309]｜unknown｜unknown

## PRE-1c-LAB60 / 批次 unknown

[AI-000177][r310]｜PRE-1c-LAB60 Lab6…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜扩充TK-LOC-03支持独立Lab60批准输入；保留原171…｜1｜0/0｜[关系][r310]｜[输出][o310]｜unknown｜unknown

## PRE-1c-PREFAB-R1 / 批次 unknown

[AI-000176][r311]｜PRE-1c-PREFAB-R1 …｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复批准字体迁移中Prefab源/实例错误折叠：分离稳定写入身…｜1｜0/0｜[关系][r311]｜[输出][o311]｜unknown｜unknown

## DATA-VIS-2 / 批次 unknown

[AI-000175][r312]｜DATA-VIS-2 可见配表线首…｜unknown｜DATA｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜可见DATA首次真实票据只领取一次，THREAD/精确任务ID…｜1｜0/0｜[关系][r312]｜[输出][o312]｜unknown｜unknown

## DATA-VIS-1 / 批次 unknown

[AI-000174][r313]｜DATA-VIS-1 建立可见 D…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已建立可见 DATA 正式路由 API、原子审计与 route…｜1｜0/0｜[关系][r313]｜[输出][o313]｜unknown｜unknown

## PRE-1b-R1 / 批次 unknown

[AI-000173][r314]｜PRE-1b-R1 补齐批准迁移的…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜补齐批准字体迁移的固定计划、单样例、显式分批、跨批全量复核、恢…｜1｜0/0｜[关系][r314]｜[输出][o314]｜unknown｜unknown

## MIG-E2b / 批次 unknown

[AI-000172][r315]｜MIG-E2b 长期验证资产保GU…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r315]｜unknown｜unknown｜unknown

## MIG-E2a / 批次 unknown

[AI-000171][r316]｜MIG-E2a 迁移证据归档恢复核…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜0｜0/0｜[关系][r316]｜[输出][o316]｜unknown｜unknown

## PRE-1c / 批次 unknown

[AI-000170][r317]｜PRE-1c 批准171项字体材质…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CS｜行政｜unknown｜7｜0/0｜[关系][r317]｜[输出][o317]｜unknown｜unknown

## FIX-T5-R1b / 批次 unknown

[AI-000169][r318]｜FIX-T5-R1b 连续空间构图…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜完成 FIX-T5-R1b：连续空间构图、双返回键视口锚定、S…｜1｜0/0｜[关系][r318]｜[输出][o318]｜unknown｜unknown

## FIX-T5-R1a / 批次 unknown

[AI-000168][r319]｜FIX-T5-R1a 镜头视口锚定…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已交付通用CameraViewportAnchor、持续物理视…｜1｜0/0｜[关系][r319]｜[输出][o319]｜unknown｜unknown

## PRE-1b / 批次 unknown

[AI-000167][r320]｜PRE-1b 已批准字体材质配对迁…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜扩充既有 ShortCycleFontSwapTool：绑定批…｜1｜0/0｜[关系][r320]｜[输出][o320]｜unknown｜unknown

## PRE-2b-PROCESS-R1 / 批次 unknown

[AI-000166][r321]｜PRE-2b-PROCESS-R1…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复DATA批处理Windows GUI进程未等待与假0误报：…｜1｜0/0｜[关系][r321]｜[输出][o321]｜unknown｜unknown

## FIX-T5-G69-R1 / 批次 unknown

[AI-000165][r322]｜FIX-T5-G69-R1 ④左页…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CW｜行政｜修复 ShortCycle_P0P1 的④线索板态：保留 Br…｜1｜0/0｜[关系][r322]｜[输出][o322]｜unknown｜unknown

## PRE-2b / 批次 unknown

[AI-000163][r323]｜PRE-2b 无UI批导表Edit…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成无UI ImportAllBatch 与 PRE-1 字体…｜1｜0/0｜[关系][r323]｜[输出][o323]｜unknown｜unknown

## OUT-ALPHA / 批次 unknown

[AI-000140][r324]｜OUT-ALPHA-T1 · HD…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已最小修复 HDR 描边 Alpha 正式配置入口：单件、组、…｜1｜0/0｜[关系][r324]｜[输出][o324]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000139][r325]｜F-T1p · 物理滚轮反向配置与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜正式 P0 唯一 ScrollRegion/ShortCycl…｜1｜0/0｜[关系][r325]｜[输出][o325]｜unknown｜unknown

[AI-000138][r326]｜F-T1o · 区域滚动工具复用与…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 F-T1o 区域滚动工具复用与方向边界核查：现有 TK-…｜1｜0/0｜[关系][r326]｜[输出][o326]｜unknown｜unknown

[AI-000113][r327]｜F-T1f · 正式P0P1滚轮根…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成正式P0P1滚轮纯代码修复：空层栈先保护再Peek；物理方…｜1｜0/0｜[关系][r327]｜[输出][o327]｜unknown｜unknown

## OUT-MEMBER / 批次 unknown

[AI-000112][r328]｜OUT-MEMBER-T3 · F…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜OUT-MEMBER-T3 已在 UserRetry1 保存态…｜1｜0/0｜[关系][r328]｜[输出][o328]｜unknown｜unknown

[AI-000111][r329]｜OUT-MEMBER-T2 · F…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成Forward_2原点位纯代码修复：新增显式幂等且拒绝未知…｜1｜0/0｜[关系][r329]｜[输出][o329]｜unknown｜unknown

[AI-000110][r330]｜OUT-MEMBER-T1 · F…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 Forward_2 原点位只定位取证：确认早期 Samp…｜1｜0/0｜[关系][r330]｜[输出][o330]｜unknown｜unknown

## VFX-DOC / 批次 unknown

[AI-000109][r331]｜VFX-DOC-T1 · 泡泡与描…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜补齐首批视觉特效配置说明：新增泡泡说明，扩展描边/阴影的Mem…｜1｜0/0｜[关系][r331]｜[输出][o331]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000108][r332]｜F-T1e · P0P1真实滚轮断…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成P0P1真实滚轮断点的监管取证，未修改生产源码或Unity…｜1｜0/0｜[关系][r332]｜[输出][o332]｜unknown｜unknown

## OUT-T2-FINAL / 批次 unknown

[AI-000107][r333]｜OUT-T2-FINAL 无遮挡H…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000107 Recovery1 completed: …｜2｜0/0｜[关系][r333]｜[输出][o333]｜unknown｜unknown

## OUT-T2-PROOF / 批次 unknown

[AI-000106][r334]｜OUT-T2 正常主画面与真实Ca…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI106首次恢复完成：设备支持16384纹理且两张2008x…｜2｜0/0｜[关系][r334]｜[输出][o334]｜unknown｜unknown

## OUT-LAB-T3b / 批次 unknown

[AI-000105][r335]｜正式Cat隔离图证、半透明并集与M…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成正式 Cat 唯一夹具隔离补证：补齐同一实体五组合与 Ti…｜1｜0/0｜[关系][r335]｜[输出][o335]｜unknown｜unknown

## OUT-L1 / 批次 unknown

[AI-000104][r336]｜OUT-L1 Tight裁边Spr…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 Tight/裁边 Sprite 成员遮罩映射：以 tex…｜1｜0/0｜[关系][r336]｜[输出][o336]｜unknown｜unknown

## OUT-L1c / 批次 unknown

[AI-000103][r337]｜OUT-L1c 真实Cat像素轮廓…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000104 Tight裁边映射补丁后的原AI-0001…｜2｜0/0｜[关系][r337]｜[输出][o337]｜unknown｜unknown

## OUT-L1 / 批次 unknown

[AI-000102][r338]｜OUT-L1 真实Cat合并轮廓坐…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复真实 Cat 合并轮廓的显示宿主坐标空间重复缩放：保持宿主…｜1｜0/0｜[关系][r338]｜[输出][o338]｜unknown｜unknown

[AI-000101][r339]｜OUT-L1 修复正常2D主画面的…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 OUT-L1 正常2D主画面的离屏Pass泄漏：两个离屏…｜1｜0/0｜[关系][r339]｜[输出][o339]｜unknown｜unknown

## OUT-L1c / 批次 unknown

[AI-000100][r340]｜OUT-L1c GPU补证：最终合…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜AI-000100 attempt 2 恢复复验完成：AI-0…｜2｜0/0｜[关系][r340]｜[输出][o340]｜unknown｜unknown

## OUT-L1c-GPU / 批次 unknown

[AI-000099][r341]｜OUT-L1c-GPU Lab真实…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜OUT-L1c 正常 Play/GPU 技术门通过：四组两 R…｜1｜0/0｜[关系][r341]｜[输出][o341]｜unknown｜unknown

## OUT-L1c-STATIC / 批次 unknown

[AI-000098][r342]｜OUT-L1c-STATIC Vi…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜Unity 2022.3 静态机器门完成：增量装配 Visua…｜1｜0/0｜[关系][r342]｜[输出][o342]｜unknown｜unknown

## OUT-L1 / 批次 unknown

[AI-000097][r343]｜OUT-L1 Lab逐组alpha…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 OUT-L1 Lab-first 逐组 alpha 合并…｜1｜0/0｜[关系][r343]｜[输出][o343]｜unknown｜unknown

## OUT-T1b / 批次 unknown

[AI-000096][r344]｜OUT-T1b 描边阴影合并渲染与…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 OUT-T1b：正式 SpriteOutline2D/S…｜1｜0/0｜[关系][r344]｜[输出][o344]｜unknown｜unknown

## OUT-T2 / 批次 unknown

[AI-000095][r345]｜OUT-T2 P0P1与短循环Pr…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成用户精确批准的 OUT-T2 迁移：五个 GridPiec…｜2｜0/0｜[关系][r345]｜[输出][o345]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000114][r346]｜F-T1g · 正式滚轮落地与独立…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜AI-000114 已在 ShortCycle_P0P1 完成…｜1｜0/0｜[关系][r346]｜[输出][o346]｜unknown｜unknown

## REVIEW / 批次 unknown

[AI-000115][r347]｜REVIEW-T1 · 剩余集成人…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成17条剩余集成人审记录的当前版本集中查验：正常None/P…｜1｜0/0｜[关系][r347]｜[输出][o347]｜unknown｜unknown

[AI-000116][r348]｜REVIEW-T2 · 现行集成客…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已接收并复核 AI-000115 验收证据，确认本轮无需生产源…｜1｜0/0｜[关系][r348]｜[输出][o348]｜unknown｜unknown

[AI-000117][r349]｜REVIEW-T3 · 集成复验与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成REVIEW-T3恢复与统一复验：已从用户批准的保存态重载…｜1｜0/0｜[关系][r349]｜[输出][o349]｜unknown｜unknown

## CK01-C / 批次 unknown

[AI-000137][r350]｜CAM-T3 · 正式机位线性曲线…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜AI143 后真实 Unity 恢复复验闭环：公共 PanTo…｜2｜0/0｜[关系][r350]｜[输出][o350]｜unknown｜unknown

[AI-000136][r351]｜CAM-T2 · 镜头线性移动与空…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜修复 ShortCycleCameraPanControlle…｜1｜0/0｜[关系][r351]｜[输出][o351]｜unknown｜unknown

[AI-000135][r352]｜CAM-T1 · 镜头瞬移的场景接…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 CAM-T1 只读诊断：正式场景唯一相机与唯一 Came…｜1｜0/0｜[关系][r352]｜[输出][o352]｜unknown｜unknown

## F-T1n / 批次 unknown

[AI-000134][r353]｜F-T1n 标签遮挡与整猫装饰随动…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜F-T1n机器收尾完成：Deco_Lines保世界变换迁入Ca…｜1｜0/0｜[关系][r353]｜[输出][o353]｜unknown｜unknown

## REVIEW-T6b / 批次 unknown

[AI-000133][r354]｜REVIEW-T6b 布局与命中最…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成 REVIEW-T6b 最终复验与证据收口：仅修正 P0 …｜2｜0/0｜[关系][r354]｜[输出][o354]｜unknown｜unknown

## REVIEW-T6a / 批次 unknown

[AI-000132][r355]｜REVIEW-T6a 线索板首行遮…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成线索板首行遮挡补修并从授权断点精确恢复短循环专用字体：首行…｜1｜0/0｜[关系][r355]｜[输出][o355]｜unknown｜unknown

## F-T1m / 批次 unknown

[AI-000131][r356]｜F-T1m 五列命中与槽位布局补修｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜通过 Unity MCP 完成正式 P0、FridgeSlot…｜1｜0/0｜[关系][r356]｜[输出][o356]｜unknown｜unknown

## REVIEW / 批次 unknown

[AI-000130][r357]｜REVIEW-T5 · 缺字与全层…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成单一正式P0独立复验：AI127字体缺字修复经退出、卸载重…｜1｜0/0｜[关系][r357]｜[输出][o357]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000129][r358]｜F-T1l · 六层可达配置落地与…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜F-T1l 六层可达配置与回归完成：源 FridgeCat_R…｜1｜0/0｜[关系][r358]｜[输出][o358]｜unknown｜unknown

## OUT-LAB-T2 / 批次 unknown

[AI-000093][r359]｜VisualEffectsLab增…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 OUT-LAB-T2：在保留既有泡泡与描边原型的统一 V…｜1｜0/0｜[关系][r359]｜[输出][o359]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000128][r360]｜F-T1k · 六层实际可达映射查…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成六层实际可达性根因复核与默认兼容的 Anchor 对齐/自…｜1｜0/0｜[关系][r360]｜[输出][o360]｜unknown｜unknown

## ART-DEPLOY / 批次 unknown

[AI-000126][r361]｜T3 独立复验与本批人工验收包｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜AI-000126 已完成 T3 独立只读复验：16 项资产处…｜1｜0/0｜[关系][r361]｜[输出][o361]｜unknown｜unknown

[AI-000125][r362]｜T2 主封皮与C描边标签、批准菜谱…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已按批准边界完成菜谱主封皮、A/B气泡、B装饰、C独立外描边标…｜1｜0/0｜[关系][r362]｜[输出][o362]｜unknown｜unknown

[AI-000124][r363]｜T1 正式资产归档命名、ArtIm…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 AI-000124：14 张 Sprite 通过 Uni…｜1｜0/0｜[关系][r363]｜[输出][o363]｜unknown｜unknown

## ART-SYNC / 批次 unknown

[AI-000123][r364]｜T2 项目资产区导入与引擎配置审批…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜AI-000123 已完成资产区导入：16张候选逐路径/SHA…｜1｜0/0｜[关系][r364]｜[输出][o364]｜unknown｜unknown

[AI-000122][r365]｜T1 NAS同步与逐项变化确认（本…｜unknown｜ART_ASSET｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜已完成 NAS→本地美术资产同步根目录：新增20、更新0、源删…｜1｜0/0｜[关系][r365]｜[输出][o365]｜unknown｜unknown

## CK01-F / 批次 unknown

[AI-000121][r366]｜F-T1j 整猫滚动独立回归与验收…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜完成 F-T1j 整猫滚动独立机器回归：P0 两次正常业务 P…｜1｜0/0｜[关系][r366]｜[输出][o366]｜unknown｜unknown

[AI-000120][r367]｜F-T1i 整猫视觉层级与滚动挂载…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 F-T1i 整猫滚动源层级落地：FridgeCat_Ri…｜1｜0/0｜[关系][r367]｜[输出][o367]｜unknown｜unknown

[AI-000119][r368]｜F-T1h 整猫滚动影响查验与精确…｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成整猫滚动静态影响查验与唯一迁移方案：确认现有ScrollA…｜1｜0/0｜[关系][r368]｜[输出][o368]｜unknown｜unknown

## OUT-MEMBER / 批次 unknown

[AI-000118][r369]｜OUT-MEMBER-T2b · …｜unknown｜CODE｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜unknown｜完成 OUT-MEMBER-T2b 最小补修：EnsurePa…｜1｜0/0｜[关系][r369]｜[输出][o369]｜unknown｜unknown

## REVIEW / 批次 unknown

[AI-000127][r370]｜REVIEW-T4 · 线索标题字…｜unknown｜ENGINE_MCP｜unknown｜unknown/unknown｜unknown/unknown/unknown｜CV｜人工｜ShortCycle Heavy 字体专用 Atlas/Mat…｜1｜0/0｜[关系][r370]｜[输出][o370]｜unknown｜unknown

## L / 批次 unknown

[AI-000371][r371]｜V01-02 Build设置App…｜unknown｜CODE｜S｜40/unknown｜unknown/unknown/unknown｜OPEN｜unknown｜Applied exact four-item singlet…｜1｜0/0｜[关系][r371]｜[输出][o371]｜CLI(磨合)｜pipeline-repair

## 引用

[r1]: TASK_DIGEST_INDEX.json
[o1]: ../2026-10-16/AI-000001.zip
[r2]: TASK_DIGEST_INDEX.json
[o2]: ../2026-10-16/AI-000253.zip
[r3]: TASK_DIGEST_INDEX.json
[o3]: ../2026-10-16/AI-000252.zip
[r4]: TASK_DIGEST_INDEX.json
[o4]: ../2026-10-16/AI-000251.zip
[r5]: TASK_DIGEST_INDEX.json
[o5]: ../2026-10-16/AI-000250.zip
[r6]: TASK_DIGEST_INDEX.json
[o6]: ../2026-10-16/AI-000249.zip
[r7]: TASK_DIGEST_INDEX.json
[o7]: ../2026-10-16/AI-000248.zip
[r8]: TASK_DIGEST_INDEX.json
[o8]: ../2026-10-16/AI-000247.zip
[r9]: TASK_DIGEST_INDEX.json
[o9]: ../2026-10-16/AI-000246.zip
[r10]: TASK_DIGEST_INDEX.json
[o10]: ../2026-10-16/AI-000245.zip
[r11]: TASK_DIGEST_INDEX.json
[o11]: ../2026-10-16/AI-000244.zip
[r12]: TASK_DIGEST_INDEX.json
[o12]: ../../../Assets/Scripts/Cooking/ShortCycle/Content/ShortCycleCarrierResolver.cs
[r13]: TASK_DIGEST_INDEX.json
[o13]: ../../outputs/G74-20261014/AI-000242/current-validation-final.json
[r14]: TASK_DIGEST_INDEX.json
[o14]: ../2026-10-16/AI-000241.zip
[r15]: TASK_DIGEST_INDEX.json
[o15]: ../2026-10-16/AI-000240.zip
[r16]: TASK_DIGEST_INDEX.json
[o16]: ../2026-10-16/AI-000239.zip
[r17]: TASK_DIGEST_INDEX.json
[o17]: ../../../DevTools/Workflow/Test-TaskTier.ps1
[r18]: TASK_DIGEST_INDEX.json
[o18]: ../../../DevTools/Workflow/Get-ThreadContext.ps1
[r19]: TASK_DIGEST_INDEX.json
[o19]: ../2026-10-16/AI-000236.zip
[r20]: TASK_DIGEST_INDEX.json
[o20]: ../../runtime/data-reports/AI-000235-CURRENT-CSV-VALIDATION-20261014212949/data-pipeline-report.json
[r21]: TASK_DIGEST_INDEX.json
[o21]: ../../../DevTools/Workflow/ARCHIVING.md
[r22]: TASK_DIGEST_INDEX.json
[o22]: ../2026-10-16/AI-000234.zip
[r23]: TASK_DIGEST_INDEX.json
[o23]: ../2026-10-17/AI-000255.zip
[r24]: TASK_DIGEST_INDEX.json
[o24]: ../2026-10-17/AI-000257.zip
[r25]: TASK_DIGEST_INDEX.json
[o25]: ../../outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md
[r26]: TASK_DIGEST_INDEX.json
[o26]: ../../outputs/DETACH-B13-20261019/AI-000275/REPORT.md
[r27]: TASK_DIGEST_INDEX.json
[o27]: ../../outputs/DETACH-B13-20261019/AI-000274/REPORT.md
[r28]: TASK_DIGEST_INDEX.json
[o28]: ../../outputs/DETACH-B13-20261019/AI-000273/REPORT.md
[r29]: TASK_DIGEST_INDEX.json
[o29]: ../../outputs/DETACH-B13-20261019/AI-000272/REPORT.md
[r30]: TASK_DIGEST_INDEX.json
[o30]: ../../outputs/DETACH-B13-20261019/AI-000271/attempt2/REPORT.md
[r31]: TASK_DIGEST_INDEX.json
[o31]: ../../outputs/DETACH-B13-20261019/AI-000270/REPORT.md
[r32]: TASK_DIGEST_INDEX.json
[o32]: ../../outputs/DETACH-B13-20261019/AI-000269/REPORT.md
[r33]: TASK_DIGEST_INDEX.json
[o33]: ../../outputs/DETACH-B13-20261019/AI-000268/REPORT.md
[r34]: TASK_DIGEST_INDEX.json
[o34]: ../../outputs/DETACH-B13-20261019/AI-000267/REPORT.md
[r35]: TASK_DIGEST_INDEX.json
[o35]: ../../outputs/DETACH-B13-20261019/AI-000266/attempt3/REPORT.md
[r36]: TASK_DIGEST_INDEX.json
[o36]: ../../outputs/DETACH-B13-20261019/AI-000265/attempt3/REPORT.md
[r37]: TASK_DIGEST_INDEX.json
[o37]: ../../outputs/DETACH-B13-20261019/AI-000264/REPORT.md
[r38]: TASK_DIGEST_INDEX.json
[o38]: ../../outputs/DETACH-B13-20261019/AI-000263/REPORT.md
[r39]: TASK_DIGEST_INDEX.json
[o39]: ../2026-10-18-ledger-code/AI-000262.zip
[r40]: TASK_DIGEST_INDEX.json
[o40]: ../2026-10-17-doc-solid-code/AI-000261.zip
[r41]: TASK_DIGEST_INDEX.json
[o41]: TASK_DIGEST.md
[r42]: TASK_DIGEST_INDEX.json
[o42]: ../../TASK_DIGEST.md
[r43]: TASK_DIGEST_INDEX.json
[o43]: ../2026-10-17/AI-000258.zip
[r44]: TASK_DIGEST_INDEX.json
[o44]: ../2026-10-17/AI-000256.zip
[r45]: TASK_DIGEST_INDEX.json
[o45]: ../2026-10-16/AI-000233.zip
[r46]: TASK_DIGEST_INDEX.json
[o46]: ../2026-10-16/AI-000232.zip
[r47]: TASK_DIGEST_INDEX.json
[o47]: ../../../DevTools/Workflow/Test-UsageOptimization.ps1
[r48]: TASK_DIGEST_INDEX.json
[o48]: ../../../Assets/Editor/Fonts/FontSwapOnce.cs.meta
[r49]: TASK_DIGEST_INDEX.json
[o49]: ../../../Assets/Editor/Fonts/FontSwapOnce.cs
[r50]: TASK_DIGEST_INDEX.json
[o50]: ../2026-10-16/AI-000204.zip
[r51]: TASK_DIGEST_INDEX.json
[o51]: ../2026-10-16/AI-000203.zip
[r52]: TASK_DIGEST_INDEX.json
[o52]: ../2026-10-16/AI-000202.zip
[r53]: TASK_DIGEST_INDEX.json
[o53]: ../2026-10-16/AI-000201.zip
[r54]: TASK_DIGEST_INDEX.json
[o54]: ../2026-10-16/AI-000200.zip
[r55]: TASK_DIGEST_INDEX.json
[o55]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r56]: TASK_DIGEST_INDEX.json
[o56]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r57]: TASK_DIGEST_INDEX.json
[o57]: ../../../DevTools/DataTables/DataPipeline.Common.ps1
[r58]: TASK_DIGEST_INDEX.json
[o58]: ../../../Assets/Spine/Runtime/spine-unity/Asset%20Types/AnimationReferenceAsset.cs
[r59]: TASK_DIGEST_INDEX.json
[o59]: ../../../DevTools/DataTables/Run-DataPipeline.ps1
[r60]: TASK_DIGEST_INDEX.json
[o60]: ../../runtime/data-reports/AI-000194-USER-U1-R1-20261005014936/data-pipeline-report.json
[r61]: TASK_DIGEST_INDEX.json
[o61]: ../../../DevTools/DataTables/Authoring/authoring.mjs
[r62]: TASK_DIGEST_INDEX.json
[o62]: ../../../DevTools/DataTables/Authoring/Authoring.Common.ps1
[r63]: TASK_DIGEST_INDEX.json
[o63]: ../../runtime/data-rounds/USER-U1-R1.json
[r64]: TASK_DIGEST_INDEX.json
[o64]: ../../../CODEBASE_MAP.md
[r65]: TASK_DIGEST_INDEX.json
[o65]: ../../../DevTools/DataTables/Authoring/header-descriptions.mjs
[r66]: TASK_DIGEST_INDEX.json
[o66]: ../../../Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs
[r67]: TASK_DIGEST_INDEX.json
[o67]: ../2026-10-17-oversize-slim/AI-000207.tar.xz
[r68]: TASK_DIGEST_INDEX.json
[o68]: ../2026-10-16/AI-000208.zip
[r69]: TASK_DIGEST_INDEX.json
[o69]: ../2026-10-16/AI-000209.zip
[r70]: TASK_DIGEST_INDEX.json
[o70]: ../2026-10-16/AI-000210.zip
[r71]: TASK_DIGEST_INDEX.json
[o71]: ../../../DevTools/Workflow/Test-UsageOptimization.ps1
[r72]: TASK_DIGEST_INDEX.json
[o72]: ../2026-10-16/AI-000229.zip
[r73]: TASK_DIGEST_INDEX.json
[o73]: ../../../Assets/Generated/DataTables/Localization/Localization/Localization.asset
[r74]: TASK_DIGEST_INDEX.json
[o74]: ../../../DevTools/DataTables/Authoring/DataTables_Authoring.xlsx
[r75]: TASK_DIGEST_INDEX.json
[o75]: ../2026-10-16/AI-000226.zip
[r76]: TASK_DIGEST_INDEX.json
[o76]: ../2026-10-16/AI-000225.zip
[r77]: TASK_DIGEST_INDEX.json
[o77]: ../../../Assets/Scripts/Cooking/ShortCycle/Runtime/ShortCyclePresentationDirector.cs
[r78]: TASK_DIGEST_INDEX.json
[o78]: ../2026-10-16/AI-000223.zip
[r79]: TASK_DIGEST_INDEX.json
[o79]: ../../../组件说明文档/ShortCycle状态显隐与镜头切换_配置说明.md
[r80]: TASK_DIGEST_INDEX.json
[o80]: ../../outputs/DETACH-B13-20261019/AI-000277/REPORT.md
[r81]: TASK_DIGEST_INDEX.json
[o81]: ../../MIG63/pre-commit.sh
[r82]: TASK_DIGEST_INDEX.json
[o82]: ../E2-20261012/README.md
[r83]: TASK_DIGEST_INDEX.json
[o83]: ../2026-10-16/AI-000218.zip
[r84]: TASK_DIGEST_INDEX.json
[o84]: ../2026-10-17-oversize-slim/AI-000217.tar.xz
[r85]: TASK_DIGEST_INDEX.json
[o85]: ../2026-10-16/AI-000216.zip
[r86]: TASK_DIGEST_INDEX.json
[o86]: ../2026-10-16/AI-000215.zip
[r87]: TASK_DIGEST_INDEX.json
[o87]: ../2026-10-17-oversize-slim/AI-000214.tar.xz
[r88]: TASK_DIGEST_INDEX.json
[o88]: ../../../Assets/Editor/Fonts/FontSwapOnce.cs
[r89]: TASK_DIGEST_INDEX.json
[o89]: ../2026-10-16/AI-000212.zip
[r90]: TASK_DIGEST_INDEX.json
[o90]: ../2026-10-16/AI-000211.zip
[r91]: TASK_DIGEST_INDEX.json
[o91]: ../2026-10-16/AI-000220.zip
[r92]: TASK_DIGEST_INDEX.json
[o92]: ../../../DevTools/DataTables/Test-CK01BSchemas.ps1
[r93]: TASK_DIGEST_INDEX.json
[o93]: ../../outputs/DETACH-B13-20261019/AI-000278/REPORT.md
[r94]: TASK_DIGEST_INDEX.json
[o94]: ../../outputs/DETACH-B13-20261019/AI-000280/REPORT.md
[r95]: TASK_DIGEST_INDEX.json
[o95]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000346/REPORT.md
[r96]: TASK_DIGEST_INDEX.json
[o96]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000345/REPORT.md
[r97]: TASK_DIGEST_INDEX.json
[o97]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000344/REPORT.md
[r98]: TASK_DIGEST_INDEX.json
[o98]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000343/REPORT.md
[r99]: TASK_DIGEST_INDEX.json
[o99]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000342/REPORT.md
[r100]: TASK_DIGEST_INDEX.json
[o100]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000341/REPORT.md
[r101]: TASK_DIGEST_INDEX.json
[o101]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000340/REPORT.md
[r102]: TASK_DIGEST_INDEX.json
[r103]: TASK_DIGEST_INDEX.json
[o103]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000338/REPORT.md
[r104]: TASK_DIGEST_INDEX.json
[o104]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000337/REPORT.md
[r105]: TASK_DIGEST_INDEX.json
[o105]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000336/REPORT.md
[r106]: TASK_DIGEST_INDEX.json
[o106]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000335/REPORT.md
[r107]: TASK_DIGEST_INDEX.json
[o107]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000334/REPORT.md
[r108]: TASK_DIGEST_INDEX.json
[o108]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000333/REPORT.md
[r109]: TASK_DIGEST_INDEX.json
[o109]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000332/REPORT.md
[r110]: TASK_DIGEST_INDEX.json
[o110]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000331/REPORT.md
[r111]: TASK_DIGEST_INDEX.json
[o111]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000330/REPORT.md
[r112]: TASK_DIGEST_INDEX.json
[o112]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000329/REPORT.md
[r113]: TASK_DIGEST_INDEX.json
[o113]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000328/REPORT.md
[r114]: TASK_DIGEST_INDEX.json
[o114]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000347/REPORT.md
[r115]: TASK_DIGEST_INDEX.json
[o115]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000327/REPORT.md
[r116]: TASK_DIGEST_INDEX.json
[o116]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000348/REPORT.md
[r117]: TASK_DIGEST_INDEX.json
[o117]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000350/REPORT.md
[r118]: TASK_DIGEST_INDEX.json
[o118]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000369/REPORT.md
[r119]: TASK_DIGEST_INDEX.json
[o119]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000368/REPORT.md
[r120]: TASK_DIGEST_INDEX.json
[o120]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000367/REPORT.md
[r121]: TASK_DIGEST_INDEX.json
[o121]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000366/REPORT.md
[r122]: TASK_DIGEST_INDEX.json
[o122]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000365/REPORT.md
[r123]: TASK_DIGEST_INDEX.json
[r124]: TASK_DIGEST_INDEX.json
[o124]: ../../../Assets/Scripts/LongLoop/StreetSceneEntry.cs
[r125]: TASK_DIGEST_INDEX.json
[r126]: TASK_DIGEST_INDEX.json
[o126]: ../../outputs/AI339-SIMPLIFY-20261026/AI-000361/attempt1/REPORT.md
[r127]: TASK_DIGEST_INDEX.json
[o127]: ../../../Assets/Scripts/Cooking/ShortCycle/Content/StepDetailRowPiece.cs
[r128]: TASK_DIGEST_INDEX.json
[o128]: ../../outputs/AI339-SIMPLIFY-20261026/AI-000359/REPORT.md
[r129]: TASK_DIGEST_INDEX.json
[o129]: ../../outputs/AI339-SIMPLIFY-20261026/AI-000358/attempt1/REPORT.md
[r130]: TASK_DIGEST_INDEX.json
[o130]: ../../outputs/AI339-SIMPLIFY-20261026/AI-000357/attempt1/REPORT.md
[r131]: TASK_DIGEST_INDEX.json
[o131]: ../../outputs/AI339-SIMPLIFY-20261026/AI-000356/REPORT.md
[r132]: TASK_DIGEST_INDEX.json
[o132]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000355/REPORT.md
[r133]: TASK_DIGEST_INDEX.json
[o133]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000354/REPORT.md
[r134]: TASK_DIGEST_INDEX.json
[o134]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000353/REPORT.md
[r135]: TASK_DIGEST_INDEX.json
[o135]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000352/attempt2/REPORT.md
[r136]: TASK_DIGEST_INDEX.json
[o136]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000351/REPORT.md
[r137]: TASK_DIGEST_INDEX.json
[o137]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000349/REPORT.md
[r138]: TASK_DIGEST_INDEX.json
[o138]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000326/REPORT.md
[r139]: TASK_DIGEST_INDEX.json
[o139]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000325/REPORT.md
[r140]: TASK_DIGEST_INDEX.json
[o140]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000324/REPORT.md
[r141]: TASK_DIGEST_INDEX.json
[o141]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000299/attempt2/REPORT.md
[r142]: TASK_DIGEST_INDEX.json
[o142]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000298/attempt2/REPORT.md
[r143]: TASK_DIGEST_INDEX.json
[o143]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000297/REPORT.md
[r144]: TASK_DIGEST_INDEX.json
[o144]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000296/REPORT.md
[r145]: TASK_DIGEST_INDEX.json
[o145]: ../../outputs/DETACH-B13-20261019/AI-000295/REPORT.md
[r146]: TASK_DIGEST_INDEX.json
[o146]: ../../outputs/DETACH-B13-20261019/AI-000294/REPORT.md
[r147]: TASK_DIGEST_INDEX.json
[o147]: ../../outputs/DETACH-B13-20261019/AI-000293/REPORT.md
[r148]: TASK_DIGEST_INDEX.json
[o148]: ../../outputs/DETACH-B13-20261019/AI-000292/attempt3/REPORT.md
[r149]: TASK_DIGEST_INDEX.json
[o149]: ../../outputs/DETACH-B13-20261019/AI-000291/BUILD-HANDOFF.json
[r150]: TASK_DIGEST_INDEX.json
[o150]: ../../outputs/DETACH-B13-20261019/AI-000290/attempt5/REPORT.md
[r151]: TASK_DIGEST_INDEX.json
[o151]: ../../outputs/DETACH-B13-20261019/AI-000289/attempt10/REPORT.md
[r152]: TASK_DIGEST_INDEX.json
[o152]: ../../outputs/DETACH-B13-20261019/AI-000288/attempt4/REPORT.md
[r153]: TASK_DIGEST_INDEX.json
[o153]: ../../outputs/DETACH-B13-20261019/AI-000287/REPORT.md
[r154]: TASK_DIGEST_INDEX.json
[o154]: ../../outputs/DETACH-B13-20261019/AI-000286/REPORT.md
[r155]: TASK_DIGEST_INDEX.json
[o155]: ../../outputs/DETACH-B13-20261019/AI-000285/REPORT.md
[r156]: TASK_DIGEST_INDEX.json
[o156]: ../../outputs/DETACH-B13-20261019/AI-000284/REPORT.md
[r157]: TASK_DIGEST_INDEX.json
[o157]: ../../outputs/DETACH-B13-20261019/AI-000283/REPORT.md
[r158]: TASK_DIGEST_INDEX.json
[o158]: ../../outputs/DETACH-B13-20261019/AI-000282/attempt3/REPORT.md
[r159]: TASK_DIGEST_INDEX.json
[o159]: ../../outputs/DETACH-B13-20261019/AI-000281/REPORT.md
[r160]: TASK_DIGEST_INDEX.json
[o160]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000300/REPORT.md
[r161]: TASK_DIGEST_INDEX.json
[o161]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000301/REPORT.md
[r162]: TASK_DIGEST_INDEX.json
[r163]: TASK_DIGEST_INDEX.json
[o163]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000303/attempt3/REPORT.md
[r164]: TASK_DIGEST_INDEX.json
[o164]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000323/REPORT.md
[r165]: TASK_DIGEST_INDEX.json
[o165]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000322/REPORT.md
[r166]: TASK_DIGEST_INDEX.json
[o166]: ../../outputs/BATCH2-INDEPENDENT-20261023/AI-000321/REPORT.md
[r167]: TASK_DIGEST_INDEX.json
[r168]: TASK_DIGEST_INDEX.json
[o168]: ../../outputs/AC-HUMAN-FEEDBACK-20261023/AI-000319/REPORT.md
[r169]: TASK_DIGEST_INDEX.json
[o169]: ../../outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000318/REPORT.md
[r170]: TASK_DIGEST_INDEX.json
[o170]: ../../outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000317/attempt1/REPORT.md
[r171]: TASK_DIGEST_INDEX.json
[o171]: ../../outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000316/REPORT.md
[r172]: TASK_DIGEST_INDEX.json
[o172]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000315/REPORT.md
[r173]: TASK_DIGEST_INDEX.json
[o173]: ../../outputs/DETACH-B13-20261019/AI-000279/REPORT.md
[r174]: TASK_DIGEST_INDEX.json
[o174]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000314/REPORT.md
[r175]: TASK_DIGEST_INDEX.json
[o175]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000312/REPORT.md
[r176]: TASK_DIGEST_INDEX.json
[o176]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311/attempt2/REPORT.md
[r177]: TASK_DIGEST_INDEX.json
[o177]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000310/REPORT.md
[r178]: TASK_DIGEST_INDEX.json
[o178]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000309/attempt1/REPORT.md
[r179]: TASK_DIGEST_INDEX.json
[o179]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000308/REPORT.md
[r180]: TASK_DIGEST_INDEX.json
[o180]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000307/REPORT.md
[r181]: TASK_DIGEST_INDEX.json
[o181]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000306/REPORT.md
[r182]: TASK_DIGEST_INDEX.json
[o182]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000305/acceptance-compare.json
[r183]: TASK_DIGEST_INDEX.json
[o183]: ../../outputs/AI289-CLI-REPAIR-20261021/AI-000304/REPORT.md
[r184]: TASK_DIGEST_INDEX.json
[o184]: ../../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000313/REPORT.md
[r185]: TASK_DIGEST_INDEX.json
[o185]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000370/REPORT.md
[r186]: TASK_DIGEST_INDEX.json
[o186]: ../../../DevTools/DataTables/Authoring/Format-AuthoringWorkbook.ps1
[r187]: TASK_DIGEST_INDEX.json
[o187]: ../../../CODEBASE_MAP.md
[r188]: TASK_DIGEST_INDEX.json
[o188]: ../../../Assets/Scripts/Cooking/ShortCycle/Runtime/ShortCyclePresentationDirector.cs
[r189]: TASK_DIGEST_INDEX.json
[o189]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r190]: TASK_DIGEST_INDEX.json
[o190]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r191]: TASK_DIGEST_INDEX.json
[o191]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r192]: TASK_DIGEST_INDEX.json
[o192]: TASK_DIGEST_INDEX.json
[r193]: TASK_DIGEST_INDEX.json
[o193]: ../../../Assets/Prefabs/Cooking/ShortCycle/VerifyTemp/ClueBoard_Sample_VerifyTemp.prefab
[r194]: TASK_DIGEST_INDEX.json
[o194]: ../../../Assets/Generated/DataTables/Localization/Localization/Localization.asset
[r195]: TASK_DIGEST_INDEX.json
[o195]: ../../../Assets/Scripts/DataBase/Localization/LocTableAsset.cs
[r196]: TASK_DIGEST_INDEX.json
[o196]: TASK_DIGEST_INDEX.json
[r197]: TASK_DIGEST_INDEX.json
[o197]: TASK_DIGEST_INDEX.json
[r198]: TASK_DIGEST_INDEX.json
[o198]: TASK_DIGEST_INDEX.json
[r199]: TASK_DIGEST_INDEX.json
[o199]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleManualVerificationDriver.cs
[r200]: TASK_DIGEST_INDEX.json
[o200]: TASK_DIGEST_INDEX.json
[r201]: TASK_DIGEST_INDEX.json
[o201]: TASK_DIGEST_INDEX.json
[r202]: TASK_DIGEST_INDEX.json
[o202]: ../../../.art-asset-sync/reports/art-sync-20260903-185217632.json
[r203]: TASK_DIGEST_INDEX.json
[o203]: ../../../Assets/Shaders/2D/SpriteOutline2D.shader
[r204]: TASK_DIGEST_INDEX.json
[o204]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs
[r205]: TASK_DIGEST_INDEX.json
[o205]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r206]: TASK_DIGEST_INDEX.json
[o206]: TASK_DIGEST_INDEX.json
[r207]: TASK_DIGEST_INDEX.json
[o207]: ../../../Assets/Scripts/Cooking/ShortCycle/Interaction/ShortCycleScrollAdapter.cs
[r208]: TASK_DIGEST_INDEX.json
[o208]: TASK_DIGEST_INDEX.json
[r209]: TASK_DIGEST_INDEX.json
[o209]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r210]: TASK_DIGEST_INDEX.json
[o210]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleContext.cs
[r211]: TASK_DIGEST_INDEX.json
[o211]: TASK_DIGEST_INDEX.json
[r212]: TASK_DIGEST_INDEX.json
[o212]: ../../../Assets/Scenes/ToolTests/FridgeScrollLab.unity
[r213]: TASK_DIGEST_INDEX.json
[o213]: ../../../Assets/Scripts/Cooking/ShortCycle/Debug/FridgeScrollLabDriver.cs
[r214]: TASK_DIGEST_INDEX.json
[o214]: TASK_DIGEST_INDEX.json
[r215]: TASK_DIGEST_INDEX.json
[o215]: ../../../Assets/Scripts/MouseInteractive/Drag/DragContainer.cs
[r216]: TASK_DIGEST_INDEX.json
[o216]: ../../../Assets/Scripts/MouseInteractive/ScrollArea/ScrollArea_Controller.cs
[r217]: TASK_DIGEST_INDEX.json
[o217]: ../../../CODEBASE_MAP.md
[r218]: TASK_DIGEST_INDEX.json
[o218]: ../../../Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCyclePresentationSet.cs
[r219]: TASK_DIGEST_INDEX.json
[o219]: ../../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs
[r220]: TASK_DIGEST_INDEX.json
[o220]: ../../../Assets/Scenes/ToolTests/VisualEffectsLab.unity
[r221]: TASK_DIGEST_INDEX.json
[o221]: ../../../Assets/Scenes/ToolTests/VisualEffectsLab.unity
[r222]: TASK_DIGEST_INDEX.json
[o222]: ../../../Assets/Scripts/Tools/Rendering/SceneColorCapture2D.cs
[r223]: TASK_DIGEST_INDEX.json
[o223]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r224]: TASK_DIGEST_INDEX.json
[o224]: ../../../CODEBASE_MAP.md
[r225]: TASK_DIGEST_INDEX.json
[o225]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r226]: TASK_DIGEST_INDEX.json
[o226]: ../../../Assets/Scripts/Cooking/ShortCycle/Debug/ShortCycleFridgeScrollTraceProbe.cs
[r227]: TASK_DIGEST_INDEX.json
[o227]: ../../../Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs
[r228]: TASK_DIGEST_INDEX.json
[o228]: ../../../Assets/Editor/ShortCycle/CameraFrameGizmoMenu.cs
[r229]: TASK_DIGEST_INDEX.json
[o229]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r230]: TASK_DIGEST_INDEX.json
[o230]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r231]: TASK_DIGEST_INDEX.json
[o231]: TASK_DIGEST_INDEX.json
[r232]: TASK_DIGEST_INDEX.json
[o232]: TASK_DIGEST_INDEX.json
[r233]: TASK_DIGEST_INDEX.json
[o233]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r234]: TASK_DIGEST_INDEX.json
[o234]: TASK_DIGEST_INDEX.json
[r235]: TASK_DIGEST_INDEX.json
[o235]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r236]: TASK_DIGEST_INDEX.json
[o236]: ../../../DataTables/Cooking/KitchenAreas.csv
[r237]: TASK_DIGEST_INDEX.json
[o237]: ../../queue.ps1
[r238]: TASK_DIGEST_INDEX.json
[o238]: ../../queue.ps1
[r239]: TASK_DIGEST_INDEX.json
[o239]: ../../../Assets/Scenes/CookingPrepare.unity
[r240]: TASK_DIGEST_INDEX.json
[o240]: TASK_DIGEST_INDEX.json
[r241]: TASK_DIGEST_INDEX.json
[o241]: TASK_DIGEST_INDEX.json
[r242]: TASK_DIGEST_INDEX.json
[o242]: ../../../Spine%20Sample场景脚本与功能整理.md
[r243]: TASK_DIGEST_INDEX.json
[o243]: ../../../CookingProcess场景脚本与功能整理.md
[r244]: TASK_DIGEST_INDEX.json
[o244]: ../../../Assets/Scripts/Rendering/SpriteOutline2D.cs
[r245]: TASK_DIGEST_INDEX.json
[o245]: ../../../Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity
[r246]: TASK_DIGEST_INDEX.json
[o246]: ../../../Assets/Scripts/Controller/HorizontalPlayerController.cs
[r247]: TASK_DIGEST_INDEX.json
[o247]: ../../../CookingPrepare场景脚本与功能整理.md
[r248]: TASK_DIGEST_INDEX.json
[o248]: ../../../CookingPrepare场景脚本与功能整理.md
[r249]: TASK_DIGEST_INDEX.json
[o249]: ../../../Assets/Scenes/CookingPrepare.unity
[r250]: TASK_DIGEST_INDEX.json
[o250]: ../../../Assets/Shaders/2D/SpriteOutline2D.shader
[r251]: TASK_DIGEST_INDEX.json
[o251]: ../../../Assets/Sprites/Cooking/Fridge/FridgeCat/AIGC_Temp/IdleBlink_v1
[r252]: TASK_DIGEST_INDEX.json
[o252]: ../2026-10-16/AI-000002.zip
[r253]: TASK_DIGEST_INDEX.json
[o253]: TASK_DIGEST_INDEX.json
[r254]: TASK_DIGEST_INDEX.json
[o254]: ../../../.art-asset-sync/reports/art-sync-20260821-133839460.json
[r255]: TASK_DIGEST_INDEX.json
[o255]: TASK_DIGEST_INDEX.json
[r256]: TASK_DIGEST_INDEX.json
[o256]: TASK_DIGEST_INDEX.json
[r257]: TASK_DIGEST_INDEX.json
[o257]: TASK_DIGEST_INDEX.json
[r258]: TASK_DIGEST_INDEX.json
[o258]: ../../../Assets/Scripts/Cooking/ShortCycle
[r259]: TASK_DIGEST_INDEX.json
[o259]: ../../../.art-asset-sync/reports/art-sync-20260902-054315870.json
[r260]: TASK_DIGEST_INDEX.json
[o260]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r261]: TASK_DIGEST_INDEX.json
[o261]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r262]: TASK_DIGEST_INDEX.json
[o262]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleV22LayoutContracts.cs
[r263]: TASK_DIGEST_INDEX.json
[o263]: ../../../.art-asset-sync/reports/art-sync-20260901-173856226.json
[r264]: TASK_DIGEST_INDEX.json
[o264]: TASK_DIGEST_INDEX.json
[r265]: TASK_DIGEST_INDEX.json
[o265]: ../2026-10-16/AI-000036.zip
[r266]: TASK_DIGEST_INDEX.json
[o266]: TASK_DIGEST_INDEX.json
[r267]: TASK_DIGEST_INDEX.json
[o267]: ../../../Assets/Scripts/Cooking/ShortCycle
[r268]: TASK_DIGEST_INDEX.json
[o268]: TASK_DIGEST_INDEX.json
[r269]: TASK_DIGEST_INDEX.json
[o269]: ../../../Assets/Editor/ShortCycle/ShortCycleSceneBuilder.cs
[r270]: TASK_DIGEST_INDEX.json
[o270]: TASK_DIGEST_INDEX.json
[r271]: TASK_DIGEST_INDEX.json
[o271]: ../../../CODEBASE_MAP.md
[r272]: TASK_DIGEST_INDEX.json
[o272]: ../../../DataTables/Cooking
[r273]: TASK_DIGEST_INDEX.json
[o273]: TASK_DIGEST_INDEX.json
[r274]: TASK_DIGEST_INDEX.json
[o274]: ../../../Assets/Scripts/Cooking/ShortCycle
[r275]: TASK_DIGEST_INDEX.json
[o275]: ../../../Assets/Generated/DataTables
[r276]: TASK_DIGEST_INDEX.json
[o276]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r277]: TASK_DIGEST_INDEX.json
[o277]: ../../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs
[r278]: TASK_DIGEST_INDEX.json
[o278]: ../../../DevTools/DataTables/Authoring/Authoring.Common.ps1
[r279]: TASK_DIGEST_INDEX.json
[o279]: TASK_DIGEST_INDEX.json
[r280]: TASK_DIGEST_INDEX.json
[o280]: ../../../CODEBASE_MAP.md
[r281]: TASK_DIGEST_INDEX.json
[o281]: ../../workers/DATA.md
[r282]: TASK_DIGEST_INDEX.json
[o282]: ../../CORE.md
[r283]: TASK_DIGEST_INDEX.json
[o283]: ../2026-10-16/AI-000158.zip
[r284]: TASK_DIGEST_INDEX.json
[o284]: ../../../DevTools/Workflow/UsageMetrics.ps1
[r285]: TASK_DIGEST_INDEX.json
[o285]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r286]: TASK_DIGEST_INDEX.json
[o286]: ../../../Assets/Scripts/Cooking/ShortCycle/Runtime/ShortCyclePresentationDirector.cs
[r287]: TASK_DIGEST_INDEX.json
[o287]: ../2026-10-16/AI-000154.zip
[r288]: TASK_DIGEST_INDEX.json
[o288]: ../../../Assets/Scripts/Cooking/ShortCycle/Debug/ShortCycleModalScrollVerificationControl.cs
[r289]: TASK_DIGEST_INDEX.json
[o289]: ../../../Assets/Scripts/Tools/Tools.cs
[r290]: TASK_DIGEST_INDEX.json
[o290]: ../2026-10-16/AI-000151.zip
[r291]: TASK_DIGEST_INDEX.json
[o291]: TASK_DIGEST_INDEX.json
[r292]: TASK_DIGEST_INDEX.json
[o292]: ../2026-10-16/AI-000149.zip
[r293]: TASK_DIGEST_INDEX.json
[o293]: TASK_DIGEST_INDEX.json
[r294]: TASK_DIGEST_INDEX.json
[o294]: ../2026-10-16/AI-000147.zip
[r295]: TASK_DIGEST_INDEX.json
[o295]: TASK_DIGEST_INDEX.json
[r296]: TASK_DIGEST_INDEX.json
[o296]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r297]: TASK_DIGEST_INDEX.json
[o297]: ../../../CODEBASE_MAP.md
[r298]: TASK_DIGEST_INDEX.json
[o298]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleCameraPanController.cs
[r299]: TASK_DIGEST_INDEX.json
[o299]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r300]: TASK_DIGEST_INDEX.json
[o300]: ../../../Assets/Editor/Fonts/ShortCycleFontAtlasBuilder.cs
[r301]: TASK_DIGEST_INDEX.json
[o301]: ../../../组件说明文档/视觉特效/描边与阴影_配置与使用说明.md
[r302]: TASK_DIGEST_INDEX.json
[o302]: ../../../Assets/Font/Heavy/ShortCycle_Heavy_Static.asset
[r303]: TASK_DIGEST_INDEX.json
[o303]: ../../../DevTools/DataTables/DATA_PIPELINE.md
[r304]: TASK_DIGEST_INDEX.json
[o304]: ../../../DevTools/Fonts/README.md
[r305]: TASK_DIGEST_INDEX.json
[o305]: ../../../DevTools/Fonts/README.md
[r306]: TASK_DIGEST_INDEX.json
[o306]: ../../../DevTools/Fonts/README.md
[r307]: TASK_DIGEST_INDEX.json
[o307]: ../../../DevTools/Fonts/README.md
[r308]: TASK_DIGEST_INDEX.json
[o308]: ../../../DevTools/Fonts/README.md
[r309]: TASK_DIGEST_INDEX.json
[o309]: ../2026-10-16/AI-000178.zip
[r310]: TASK_DIGEST_INDEX.json
[o310]: ../../../DevTools/Fonts/README.md
[r311]: TASK_DIGEST_INDEX.json
[o311]: ../../../DevTools/Fonts/README.md
[r312]: TASK_DIGEST_INDEX.json
[o312]: ../2026-10-16/AI-000175.zip
[r313]: TASK_DIGEST_INDEX.json
[o313]: ../../queue.ps1
[r314]: TASK_DIGEST_INDEX.json
[o314]: ../2026-10-16/AI-000173.zip
[r315]: TASK_DIGEST_INDEX.json
[r316]: TASK_DIGEST_INDEX.json
[o316]: ../2026-10-16/AI-000171.zip
[r317]: TASK_DIGEST_INDEX.json
[o317]: ../2026-10-16/AI-000170.zip
[r318]: TASK_DIGEST_INDEX.json
[o318]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r319]: TASK_DIGEST_INDEX.json
[o319]: ../../../Assets/Scripts/Tools/Layout/CameraViewportAnchor.cs
[r320]: TASK_DIGEST_INDEX.json
[o320]: ../../../DevTools/Fonts/Test-ShortCycleFonts.ps1
[r321]: TASK_DIGEST_INDEX.json
[o321]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r322]: TASK_DIGEST_INDEX.json
[o322]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r323]: TASK_DIGEST_INDEX.json
[o323]: ../../../Assets/Editor/DataTables/DataTableImportService.cs
[r324]: TASK_DIGEST_INDEX.json
[o324]: TASK_DIGEST_INDEX.json
[r325]: TASK_DIGEST_INDEX.json
[o325]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r326]: TASK_DIGEST_INDEX.json
[o326]: ../../../组件说明文档/区域滚动_配置与复用说明.md
[r327]: TASK_DIGEST_INDEX.json
[o327]: ../../../Assets/Scripts/MouseInteractive/MouseManager.cs
[r328]: TASK_DIGEST_INDEX.json
[o328]: ../../../组件说明文档/视觉特效/描边与阴影_配置与使用说明.md
[r329]: TASK_DIGEST_INDEX.json
[o329]: ../../../Assets/Editor/Rendering/OutlineShadowLabBuilder.cs
[r330]: TASK_DIGEST_INDEX.json
[o330]: ../../../CODEBASE_MAP.md
[r331]: TASK_DIGEST_INDEX.json
[o331]: ../../../组件说明文档/视觉特效/README.md
[r332]: TASK_DIGEST_INDEX.json
[o332]: ../../../CODEBASE_MAP.md
[r333]: TASK_DIGEST_INDEX.json
[o333]: ../../../Assets/Scenes/ToolTests/VisualEffectsLab.unity
[r334]: TASK_DIGEST_INDEX.json
[o334]: ../../../Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab
[r335]: TASK_DIGEST_INDEX.json
[o335]: ../../../CODEBASE_MAP.md
[r336]: TASK_DIGEST_INDEX.json
[o336]: ../../../Assets/Scripts/Tools/Rendering/SpriteOutlineMergeRenderer2D.cs
[r337]: TASK_DIGEST_INDEX.json
[o337]: TASK_DIGEST_INDEX.json
[r338]: TASK_DIGEST_INDEX.json
[o338]: ../../../Assets/Scripts/Tools/Rendering/SpriteOutlineMergeRenderer2D.cs
[r339]: TASK_DIGEST_INDEX.json
[o339]: ../../../Assets/Shaders/2D/SpriteOutlineMerge2D.shader
[r340]: TASK_DIGEST_INDEX.json
[o340]: TASK_DIGEST_INDEX.json
[r341]: TASK_DIGEST_INDEX.json
[o341]: ../../../DevTools/Rendering/VisualEffectsLab.md
[r342]: TASK_DIGEST_INDEX.json
[o342]: ../../../Assets/Scenes/ToolTests/VisualEffectsLab.unity
[r343]: TASK_DIGEST_INDEX.json
[o343]: ../../../Assets/Scripts/Tools/Rendering/SpriteOutlineMergeRenderer2D.cs
[r344]: TASK_DIGEST_INDEX.json
[o344]: ../../../Assets/Scripts/Rendering/SpriteOutline2D.cs
[r345]: TASK_DIGEST_INDEX.json
[o345]: TASK_DIGEST_INDEX.json
[r346]: TASK_DIGEST_INDEX.json
[o346]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r347]: TASK_DIGEST_INDEX.json
[o347]: ../../../CODEBASE_MAP.md
[r348]: TASK_DIGEST_INDEX.json
[o348]: TASK_DIGEST_INDEX.json
[r349]: TASK_DIGEST_INDEX.json
[o349]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r350]: TASK_DIGEST_INDEX.json
[o350]: ../../../CODEBASE_MAP.md
[r351]: TASK_DIGEST_INDEX.json
[o351]: ../../../Assets/Scripts/Cooking/ShortCycle/ShortCycleCameraPanController.cs
[r352]: TASK_DIGEST_INDEX.json
[o352]: TASK_DIGEST_INDEX.json
[r353]: TASK_DIGEST_INDEX.json
[o353]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r354]: TASK_DIGEST_INDEX.json
[o354]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r355]: TASK_DIGEST_INDEX.json
[o355]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r356]: TASK_DIGEST_INDEX.json
[o356]: ../../../Assets/Scenes/Cooking/ShortCycle_P0P1.unity
[r357]: TASK_DIGEST_INDEX.json
[o357]: ../../../CODEBASE_MAP.md
[r358]: TASK_DIGEST_INDEX.json
[o358]: ../../../Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab
[r359]: TASK_DIGEST_INDEX.json
[o359]: ../../../Assets/Scenes/ToolTests/VisualEffectsLab.unity
[r360]: TASK_DIGEST_INDEX.json
[o360]: ../../../Assets/Scripts/Cooking/ShortCycle/Presentation/FridgeCatRig.cs
[r361]: TASK_DIGEST_INDEX.json
[o361]: TASK_DIGEST_INDEX.json
[r362]: TASK_DIGEST_INDEX.json
[o362]: TASK_DIGEST_INDEX.json
[r363]: TASK_DIGEST_INDEX.json
[o363]: ../../../CODEBASE_MAP.md
[r364]: TASK_DIGEST_INDEX.json
[o364]: ../../../CODEBASE_MAP.md
[r365]: TASK_DIGEST_INDEX.json
[o365]: ../../../.art-asset-sync/reports/art-sync-20260914-184248141.json
[r366]: TASK_DIGEST_INDEX.json
[o366]: ../../../CODEBASE_MAP.md
[r367]: TASK_DIGEST_INDEX.json
[o367]: ../../../Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab
[r368]: TASK_DIGEST_INDEX.json
[o368]: TASK_DIGEST_INDEX.json
[r369]: TASK_DIGEST_INDEX.json
[o369]: ../../../Assets/Editor/Rendering/OutlineShadowLabBuilder.cs
[r370]: TASK_DIGEST_INDEX.json
[o370]: TASK_DIGEST_INDEX.json
[r371]: TASK_DIGEST_INDEX.json
[o371]: ../../outputs/PLAYABLE-DEMO01-20261026/AI-000371/REPORT.md
