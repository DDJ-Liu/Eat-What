# Spine两个旧资产报错：只读定位

固定CODE。读CORE、CONTROL/SUPERVISED/worker CODE及用户决定Spine旧资产日志白名单_用户决定_20261014.json。父/root AI235，key data-f1-unapproved-spine-load-errors；这是正式失败后的只读诊断，复用控制既有8指纹/原生日志证据，不新Unity、不跑测试、不再哈希全树/大对象、不改生产代码/资源。只输出一次简短源码定位与可执行修复边界。

已有事实：AI235唯一batch成功后被SkeletonBaker.GenerateMecanimAnimationClips中GetSkeletonData(true)==null的LogError拦住；两条日志含DATA_SPINE_LOAD_FAILURE，源路径为Assets/Spine/Editor/spine-unity/Editor/Windows/SkeletonBaker.cs:84-95。Assets/Spine/Runtime/spine-csharp/SkeletonBinary.cs:131-132保留对3.8.75的Unsupported异常。SkeletonDataAsset.cs catch及UNITY_EDITOR兼容检查还有各自报告路径。主/隔离Sample、output4共8文件与旧指纹相同，证据outputs/control/OMISSIONS-20261014/ai235-independent-legacy.json；旧AI194例外仅用于旧轮，不能套用新235。无需重复发现这些事实。

审查上述真正调用链及AnimationReferenceAsset，给出最小包内白名单方案：准确资产身份（至少SDA和skeleton的GUID/路径绑定及已知源载荷身份）+明确的既有3.8.75不支持错误，才降为可识别、去重的非Error提示；真实读取/版本拒绝/null保持。不得只按name或目录通配，不得屏蔽该资产的新异常或非白名单资产错误。覆盖batch自动动画导入的报错源，必要时复用在Editor读取错误分支，不改版本检查器/骨骼资源、不删除或排除导入、不改变数据日志门全局规则。未知身份/漂移仍Error。

本诊断不执行修复、不恢复235。输出目录outputs/OMISSIONS-20261014/<TaskId>，REPORT/evidence/acceptance。checks rootCauseLocated,precisePolicyBounded,repairPathDefined,readOnlyAndUnknownPreserved。量级S/40仅计划，功能未上线不新填运行字段。每exec指定PS7、runtime仅queue正式API。Complete同轮Prepare→真实一次Send控制台→Record，已有长期授权，不自派后继。
