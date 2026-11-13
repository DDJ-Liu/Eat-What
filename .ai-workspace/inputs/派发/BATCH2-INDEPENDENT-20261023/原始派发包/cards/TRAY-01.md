# TRAY-01 · 托盘拖放逻辑 + 8 桩 [CODE] `L · 120`

> 来源：派发包_脱离期批次2_独立项_v0.4（2026-10-23）§二；单卡文件。**禁改范围见索引 §〇 U 隔离清单**，触及即 BLOCKED_ON_USER。

### TRAY-01 · 托盘拖放逻辑 + 8 桩 [CODE] `L · 120`
M7-B1-R3 容量（T8.prep_zone_capacity）、M7-B2-R1/R2/R4 拖入/退回/DataProcessor、M8-B1-R2 换菜清空、M1-B3-R3 `fridge.filter / fridge.tidy / inventory.×2 / tray.×3` 桩实装。TK-INT-04/05 改造；动作走 23 全集不新增；InventoryManager 唯一写面；失败回弹 TK-MOT-07。验收：runner 拖入/退回/满拒绝（文案 key 登记字形清单，中文不定稿）/换菜清空；探针 +3 断言；场景 diff 为空。
