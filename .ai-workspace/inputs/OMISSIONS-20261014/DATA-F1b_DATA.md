# DATA-F1b：退出后字体发布合同（DATA·DevTools，S/40）
用户明确指定一张新发布合同变更卡，固定DATA执行；关联AI235 attempt2，保留原错误/许可/预算，不重新领取或替代原卡。本卡只改DevTools和对应规则文档，不运行Unity/生产发布，不碰.cs/Unity资产/配表内容。任务卡40轮为明确上限，到限既有交接；queue自动复杂度字段尚待GOV05，不冒称已经硬拦截。
读本目录DATA-F1b_合同修订与单次发布授权_20261014.md、CORE、DATA worker、AGENTS四上限、两项目文档页首与字体/配表相关节、对应toolkit、原AI235 attempt2原始报告和控制ai235-attempt2-independent-review.json。指定PS7、项目根正式queue API，runtime只正式API；生产旧成果不动。
优先修改已有DevTools/DataTables/{Run-DataPipeline.ps1,DataPipeline.Common.ps1,Test-BatchImport.ps1}。P-051放pitfalls/CODE.md、INDEX.md并进CORE，相关现有项目模块/工具说明必要小改。不新增TK/专用续行入口/queue改动，不扩Final或冷测试。
要求：
- 现有本次Process真实退出后，以有界目标文件长度/mtime稳定确认后从磁盘计算新Static凭据；保留batch成功、nonce/run身份。批内旧资产SHA仅历史业务结果，不能继续拿来作为发布凭据。不做整树反复哈希或内存JSON。
- Dynamic移出所有发布SHA合同，包括临时/发布后/回滚SHA门、manifest Dynamic SHA比较；直接覆盖本次已清空候选，只记录前后glyph数，最终0。路径/身份/备份/事务/未知用户dirty保护仍保留，不手改YAML或修改.cs。
- Static候选与本次退出后磁盘凭据比较；旧基线用于现有live原值保护，不与新候选比较。授权发布更新manifest.staticAssetSha256，记录run/roundID、字符集差异、旧新SHA；已存在更新语句应修输入合同，不重复开发；no-op不重复更新。
- 支持原AI235 CurrentCsvValidation+精确FontPublishApprovalPath+-Publish字体专用路径，不落入Generated发布；无授权CurrentCsv仍禁Publish，正常DONE轮次原语义保持。审计原始文件不改。
验证只扩充并跑既有Test-BatchImport.ps1 -FontTransactionOnly：基线更新一次；Dynamic不进SHA且glyph0；模拟退出回调修改后凭据与最终磁盘一致、Static凭据后漂移拒绝；验证CurrentCsv字体Publish不动Generated。只跑受影响定向runner，成功不重复，不另起Unity/全套Final。
交付outputs/OMISSIONS-20261014/<TaskId>/{REPORT.md,evidence.json,acceptance.json}，五checks postExitCredential/staticBaselineAudit/dynamicGlyphContract/targetedFixturesAndScope/p051Documented；实际用量缺失null。正式失败一次即停。完成/失败同轮按长期授权Prepare→一次真实Send→Record回传绑定控制台；不自行续235。控制独立验收后才落实本次已批准原235一次发布及后继236原生Play/人工门。
