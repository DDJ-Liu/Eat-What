# 派发包索引 · INFRA-011 · v1-index（2026-11-09，A0 落盘版）

> 依据：《流程优化落档_0.1.1复盘》§一 G101~G103、§二；总档 v1.5.6。全部为基础设施卡，不改产品代码、不改场景资产；引擎线占用见 §三。
> 规则：A0 落盘；G87 修复轨（REPAIR→SMOKE）；G100 五句禁令对 SMOKE 卡生效；G101~G103 自本包起生效；R10 不合卡（本包每张卡一个目标，CONTROL 不得合并）。
> 用户介入：**零**。唯一协调点 = 标 "引擎线" 的四项须等用户完成 USER-V0.1.1b 签收与终调并保存后再领取（USER_WORK_STATUS 出现 `V0.1.1b DONE` 为信号）。

---

## 一、顺序

```
随时可派（纯 CODE，无引擎）：
  INFRA-CLIARG-01 → INFRA-EVID-01 → INFRA-BUILD-01（三者互不依赖，可并行）
用户 V0.1.1b DONE 之后（引擎线，串行）：
  INFRA-GRANT-01（CODE → SMOKE）→ INFRA-GUARD-01（CODE → SMOKE）→ PRB-FULL-RUN → PRB-CI-NIGHTLY
```

---


## 二、任务卡索引（正文见 cards/）

| 卡 | 文件 | 字节 |
|---|---|---:|
| INFRA-CLIARG-01 | cards/INFRA-CLIARG-01.md | 1,226 |
| INFRA-EVID-01 | cards/INFRA-EVID-01.md | 956 |
| INFRA-BUILD-01 | cards/INFRA-BUILD-01.md | 944 |
| INFRA-GRANT-01 | cards/INFRA-GRANT-01.md | 1,387 |
| INFRA-GUARD-01 | cards/INFRA-GUARD-01.md | 1,320 |
| PRB-FULL-RUN | cards/PRB-FULL-RUN.md | 866 |
| PRB-CI-NIGHTLY | cards/PRB-CI-NIGHTLY.md | 684 |

## 三、引擎线协调

| 卡 | 引擎占用 | 前置信号 |
|---|---|---|
| INFRA-GRANT-01 SMOKE | 空场景保存 1 次（≈10 分钟） | `V0.1.1b DONE` |
| INFRA-GUARD-01 SMOKE | 只读加载 Street（≈10 分钟） | GRANT-01 CLOSED |
| PRB-FULL-RUN | Street 1 Play（≈15 分钟） | GUARD-01 CLOSED |
| PRB-CI-NIGHTLY | 后台，Editor 空闲时 | PRB-FULL-RUN CLOSED |

用户终调期间（USER_WORK_STATUS 为 `V0.1.1b DOING`）引擎线不领任何卡。

*--- v1 结束 ---*
