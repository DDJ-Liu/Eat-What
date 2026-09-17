# PrepareArtPrototype 拼接迭代记录

## 2026-08-17 V4：可调 2D 外框描边

- 新增 Shader：`Assets/Shaders/2D/SpriteOutline2D.shader`
- 新增共享材质：`Assets/Materials/2D/SpriteOutline2D.mat`
- 单体控制：`SpriteOutline2D`；整组控制：原型根节点上的 `SpriteOutlineGroup2D`
- 默认颜色：`#30231F`；默认粗细：6 源图片像素；允许范围：0～32

### 实现与应用范围

- Shader 根据 Sprite Alpha 邻域生成外框，颜色与粗细由 `MaterialPropertyBlock` 按 Renderer 设置，不为每个实例复制材质。
- 27 个新导入 Sprite 全部改为 Full Rect 网格，以免 Tight Mesh 裁掉外框；PPU 与 Transform Scale 不变。
- `PrepareArtPrototype` 下 45/45 个 SpriteRenderer 均已挂载 `SpriteOutline2D` 并使用同一共享材质。
- 根节点组控制器可一次修改全部子素材；单个 Renderer 组件仍保留独立配置接口。

### 验证

- Shader 支持当前 URP，Shader 编译消息 0
- 脚本编译错误 0
- 27/27 源纹理为 Full Rect；45/45 场景实例使用描边组件和共享材质
- 结果截图：`Assets/Screenshots/Prepare_2560x1440_Outline_V1.png`

## 2026-08-17 V3：2560×1440 基准视口与原生缩放重排

- 设计基准：`2560×1440`，统一 `Pixels Per Unit = 100`
- 主相机：正交投影，`Orthographic Size = 7.2`
- 视口保护：URP `PixelPerfectCamera`，`Reference Resolution = 2560×1440`，`Crop Frame = Windowbox`
- 运行时默认窗口：`2560×1440`；实际输出分辨率可以变化，但非 16:9 时使用边框维持设计范围，不额外暴露或裁切画面
- 结果截图：`Assets/Screenshots/Prepare_2560x1440_Scale1_V3_refined.png`

### 本轮调整

- 以 `25.6 × 14.4` 世界单位作为完整设计画布重新排布猫主体、货架、菜谱板、夹板和前景资产。
- 所有宽或高不是 2 的幂次的交付素材都保持 `Transform Scale = (1,1,1)`；只允许 `512×512` 食材/菜品图标按可见内容缩放。
- 相机使用米白色纯色清屏，不再依赖场景 `Square` Sprite 覆盖整个视口。
- 继续使用 45 个 `SpriteRenderer` 和全部 15 个冰箱猫拆分件，没有删除既有交互对象。

### 验证

- 编辑态与 Play Mode 的相机可见范围均为 `25.600 × 14.400`
- `PixelPerfectCamera`：PPU 100、Reference 2560×1440、Windowbox、Grid Snapping None
- 非 2 次幂交付素材检查 30 个引用实例，Scale 违规 0
- Play Mode 原型正常启用；旧冰箱视觉未重新出现
- Console 编译/运行错误 0；仍有既存 Missing Script 警告 1 条，本轮未处理

## 2026-08-17 V2：按可见像素边界重排

- 场景：`Assets/Scenes/CookingPrepare.unity`
- 原型根节点：`PrepareManager/PrepareParent/FridgeManager/PrepareArtPrototype`
- 结果截图：`Captures/prepare-art-prototype-v2-playmode.png`
- 规模：45 个 `SpriteRenderer`，21 个唯一 Sprite 资产
- 冰箱猫拆分件：15/15 已使用，无遗漏

### 本轮调整

- 不再按 512×512 画布统一缩放食材，改为根据 Alpha 可见区域分别补偿；鸡蛋、皮蛋类小可见区资产不再显得过小。
- 货架内容收敛到参考图中的挂面、章鱼脚、火腿肠、鸡蛋和泡面，并在两层货架重复排布。
- 增加两排黄色/粉色标签底板。
- 增加眼光、四条线缆、右上便签板、菜品照片框、右侧夹板、铅笔、前景托盘、切板和菜刀。
- 复用场景内 `Square` Sprite 作为备菜原型的米白色全屏背景。
- 旧冰箱头尾和旧托盘仅关闭视觉渲染，不删除对象、Collider、布局或交互组件。

### 运行时兼容

`FridgeManager.GenerateFridgeVisual` 检测启用中的 `PrepareArtPrototype`：

- 继续生成旧中段，用于保持原有内容区高度和拖拽区域计算；
- 关闭旧头部、旧底部和运行时中段的 `SpriteRenderer`；
- 原型根节点被禁用或删除后，旧视觉自动恢复。

### 验证

- Unity 编译错误：0
- Play Mode：原型启用；旧中段生成 2 个；启用的旧 SpriteRenderer 为 0
- 资产盘点：15 个冰箱猫拆分件全部被原型引用
- 场景校验仍报告既存问题：`CookingManger/TableManager/TableParent/StepParent` 有 1 个 Missing Script，本轮未处理
