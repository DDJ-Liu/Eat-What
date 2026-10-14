# Spine 已知旧资产日志精确白名单修复（原 AI235 根）

用户直接决定见 `Spine旧资产日志白名单_用户决定_20261014.json`；不改变版本检查规则，不删除资产。依 AI239 紧凑定位修复；保持原 root235/问题key/失败史/修复预算。

## 范围与实现
- 固定 CODE。修改已有 Spine 包 `SkeletonDataAsset.cs` 和 `SkeletonBaker.cs` 所需最少日志路径，优先同文件小辅助策略，不新工具编号、不引入新架构或新长期入口。
- 白名单只覆盖 Sample 和 output4 的已知 3.8.75 不支持版本异常。至少绑定 SDA 与 binary 的精确路径/GUID和已知 binary SHA，未知/指纹漂移/同名副本/其它错误必须保持 Error。资产元数据不改，运行时身份无法可靠确认时默认不豁免。
- 必须确认实际异常来自不支持版本检查，不能仅凭资产身份把任意 null 加载失败放行。保留拒绝、返回 null、清理旧缓存语义；不伪造 SkeletonData 或可用动画。只有日志由 Error 降为可识别且去重的非 Error 提示。
- 覆盖 quiet=true 后 Mecanim 再报错的主路径，以及真实加载异常和 Editor compatibility 对同一已知失败的重复报告。其它资产/新的加载问题保持原失败诊断，不吞掉不相关异常。跨调用不得残留上次白名单结果导致随后新错误被误放。
- `SkeletonBinary.cs`、`SkeletonDataCompatibility` 和所有资产/scene/prefab保持；不改全局 DATA 日志门、旧 AI194 范围批准或旧指纹，不重启/恢复旧字体卡。

## 验证上限
只做相关源码离线编译与一个小型定向回归，复用既有测试族（已有 Test-SpineLegacyGate 可扩展），覆盖两例命中、路径/GUID/载荷/异常漂移拒绝、null不转成功、版本拒绝仍在、重复提示去重/状态复位。不新建独立 Unity/冷载进程，不进入本次全量 Final，不做全量资源/内存/纹理导出；不重放 AI235。验证实际执行生产策略方法，避免只有源码字符串断言。编译时区分真实UnityDLL编译与stubs，明确原生Editor尚待后续获准单次发布验证。

## 交付
输出 `outputs/OMISSIONS-20261014/<taskId>/REPORT.md`、`evidence.json`、`acceptance.json`，证据包含必要代码diff、实际测试输出与原始失败。如需新增长期TK编号或扩范围先说明，不自行扩范围。provider/model/计费tokens费用缺失null。
对应项目文档仅补 Spine 日志策略/限制和入口定位；控制台统一收尾页首状态，勿重复大段历史。

成功检查字段（evidence.checks）：exactIdentityAndFailure、loadRejectionPreserved、otherErrorsPreserved、targetedValidationPassed、assetsAndVersionRulesUnchanged、documentationAndUnknownPreserved。

Complete/Fail/Yield 后当前轮 Prepare→一次实际发送绑定控制台→Record；用户长期授权在 AGENTS/HANDOFF，无需再问。回传后结束，不自领后继。