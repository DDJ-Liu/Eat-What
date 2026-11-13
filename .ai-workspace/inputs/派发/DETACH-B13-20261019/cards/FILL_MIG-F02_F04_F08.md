### 填充小卡（可任意穿插）

| 卡 | 车道 | complexity / 预算 | 内容 | 验收 |
|---|---|---|---|---|
| MIG-F04 | CODE | S / 30 | `Tools.getMousePos` 无鼠标空引用守卫；`Camera.main` 缺失守卫 | 无鼠标设备 Play 0 Error（手柄或拔插模拟） |
| MIG-F08 | CODE/DevTools | S / 40 | 文本资产保护按 Git 规范化内容比较（基线/index/工作树），原始 SHA 仅备份；字体 LFS 仍按真实内容 | 同一提交在两个 clone 下保护判定一致 |
| MIG-F02 | ENGINE（窗口 ①） | S / 20 | CompanyName / ProductName 设为正式值（用户在窗口给出两个字符串）；记录 persistentDataPath 变化 | ProjectSettings diff 仅两字段 |

---

