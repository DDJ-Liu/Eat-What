# V-25 长期离线夹具

2026-11-16 AI561/V013-09。这里是专用最小文本夹具，不可导入工程或当作生产场景。Actor/Anchor 的脚本 GUID 来自实际两个 `.cs.meta`，源 Prefab GUID 来自已验 GenericNPC；合成 localID 9007199254740993123 及其相邻值用于检查整数精度，未从实例名称推断角色身份。文本省略与这三项审计无关的 Unity 字段，不声称可直接加载。

`dirty.scene.txt` 与 `dirty.csv` 恰好涵盖：actor 锚点不存在、空锚点且没有完整 ID 预摆、预摆无条目。`clean.scene.txt` 与 `clean.csv` 对应完整 chr_vendor#2，V-25 零告警。`generic.prefab.txt` 的空标记由 `override.scene.txt` 精确 GUID/localID 覆盖；这是当前 Street 中 GenericNPC 实例及未来角色 ID override 的同种表示。

`NarrativeSceneContentTests.cs` 调用真实审计，不复制算法；另只读实际 Street/GenericNPC，当前继承空 ID 必须未核，在内存中构造实际源 localID 的覆盖例。其它变体在内存内产生，包括相邻 localID、错误 GUID、双实例、stripped 继承、空/坏文本、嵌套/增删超支持范围、完整后缀和禁用组件。原冻结夹具不修改。

运行 `DevTools/DataTables/Test-NarrativeSceneContent.ps1 -OutputRoot <新目录>`。入口复制真实 DataTables 到输出目录，使用原 CSV/schema 解析和校验报告；只替换复制品的四张相关 CSV，名字键使用已有 act_boil.name。scope 变体在复制品中补同场景另一状态及另一场景同名状态，防止状态/场景混用；unknown 变体写空的合成场景。固定三类 warning、valid 保持和旧 sceneInventory=UNVERIFIED 分别检查。输出保留实际报告及 summary.json，不运行 Unity、导表或 Build。
