# CORE-ASM-02 · 拆 Core 独立程序集 [CODE] `M · 80`

> 来源：派发包_脱离期批次2_独立项_v0.4（2026-10-23）§二；单卡文件。**禁改范围见索引 §〇 U 隔离清单**，触及即 BLOCKED_ON_USER。

### CORE-ASM-02 · 拆 Core 独立程序集 [CODE] `M · 80`
ASM-01 断言 3 后置项：`ShortCycle.Core.asmdef`（零 UnityEngine，P2L-01 已证）从 ShortCycle 拆出；ShortCycle 引用 Core；Core 单测程序集只引用 Core。验收：编译 0 Error；Core 内无 `using UnityEngine`（脚本断言）；探针回归；PROBE-CI-01 通过。
