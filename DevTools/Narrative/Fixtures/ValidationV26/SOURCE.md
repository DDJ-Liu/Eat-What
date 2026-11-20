# V-26 固定回归输入

historical.json自包含9张CSV的冻结文本和一份修正前SceneStatePlacements文本；测试只打开本目录文件与工程现行schema，不打开活动任务outputs。它经原DataTableCsvParser和原NarrativeValidationTests.Input/Context送入真实NarrativeTableValidation，不是新CSV加载器、探针或比较器。

修正前条目来自实际0.1.3记录 `.ai-workspace/outputs/PLAYABLE-DEMO013-20261115/AI-000566/attempt1/SceneStatePlacements-before.csv`，原文件775B，SHA256 **0B344BCBF7FBDFBFC5828686CA49520F9EEE806CE6DD9BCFE137A67AAEB46113**。保留两场景状态的8条原业务值。test_default中番茄与基础路人各有自己的池，同时是chr_test_passer_a#2所挂amb_test_c的指定speaker，应各一条V-26；test_second中该发起者active=false，不应串入。池与台词按实际引用链，不把任意同标签行当已挂内容。

修复后条目与其它配套CSV冻结自2026-11-19本卡开工时的正式输入，包含已交573的0.1.4内容，**不是整套0.1.3历史快照**。与修前条目做隔离实例对照，当前修后为0条V-26。每个来源路径、原字节SHA和长度在historical.json.provenance列出；CSV文本以UTF-8解码并将行尾归一为LF，字段/行顺序不改，SHA标的是原件字节而非JSON字符串编码。

其余用例仅在内存隔离副本上改一个因素：active、scene/state、是否有表内池、精确speaker/带后缀条目、自己/另一发起者、池内后续对话/可达行、重复speaker和非actor。正式CSV、xlsx与资产不写；其它旧规则的提示仍按原实现返回，V-26只增加warning，不改变valid/错误级别或修值。

表里为空而只在场景实例自身填写的池不在本静态检查可见范围，不读取场景、Prefab或运行时对象来猜值；V-26=0不代表所有实例已无叠泡。Schema的文档修订与实际两个序列化版本常量分开，本夹具不修改或签收格式版本。
