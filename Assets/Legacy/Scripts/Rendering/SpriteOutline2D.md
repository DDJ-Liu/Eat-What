# SpriteOutline2D

`SpriteOutline2D` 在同一 `SpriteRenderer`、同一共享材质与 `MaterialPropertyBlock` 路径中组合原图、描边和可选投影阴影。它不复制渲染器或 Sprite，不访问 `renderer.material`，也不生成材质实例。旧的 `Configure(Material, Color, float)` 接口保持可用；未配置阴影的既有组件仍走原描边结果。

## 阴影参数与公开面

- `ShadowEnabled`：独立启用阴影，默认 `false`。
- `ShadowColor`：单色阴影颜色；RGB 支持有限 HDR 范围，alpha 参与最终阴影强度。
- `ShadowOffset`：二维偏移，单位为源图片像素；正 X/Y 表示阴影向纹理右/上方移动。Shader 使用 `_MainTex_TexelSize` 换算，并以反向 UV 采样实现该移动方向。
- `ShadowOpacity`：独立透明度，范围 0～1，默认 0。
- `ShadowBlur`：3×3 高斯核采样半径，单位为源图片像素，范围 0～32；0 使用单次 alpha 采样的硬边阴影。

单件可调用 `ConfigureShadow(bool, Color, Vector2, float, float)`；组合根可用 `SpriteOutlineGroup2D.ConfigureShadow` 幂等下发。所有 float/Vector2/Color 输入都会限制范围，NaN/Infinity 会回落到安全值。组级旧三参数 `Configure` 仍保留，并会连同组中当前阴影配置一起下发。

## 采样与合成

阴影只读取原 Sprite 纹理 alpha，不读取原图 RGB，也不把描边结果再次送入模糊。模糊核权重为 `1,2,1 / 2,4,2 / 1,2,1`，总和 16，输出按 `1/16` 归一化。

合成顺序固定为：阴影（底）→旧描边结果（中）→原图（顶）。这覆盖仅原图、仅描边、仅阴影和描边+阴影四种组合。阴影关闭或透明度为零时，Shader 在阴影采样前直接返回旧描边结果；硬边阴影增加 1 次 alpha 采样，模糊阴影增加 9 次 alpha 采样。

## 动画换帧与生命周期

组件缓存上一次已同步的 `SpriteRenderer.sprite`。配置变更或 Sprite 引用变化时才读取并写回属性块；检测位于 `LateUpdate`，因此 Animator Sprite 曲线完成本帧求值后，阴影和描边会沿同一路径同步。稳定帧不重复写入属性块。

脚本在 `Update` 更换 Sprite 同样会在该帧末被检测。空 Sprite 安全；禁用时描边厚度、阴影启用和阴影透明度同时写零，重新启用会恢复已保存配置。写入前始终调用 `GetPropertyBlock`，只覆盖本组件拥有的七个属性，保留其它消费者的字段。

## 正式合并后端

### 保留组内某个成员的重叠描边

适用前提：以下为正式SpriteOutlineGroup2D/成员同步链，需有效独立描边材质、非透明颜色、非零宽度，以及带透明留白的Full Rect Sprite。AI-000110已确认早期Lab的SampleMatrix原本不是正式组：Forward_2的本地ForceOff无法同步旧Adapter，且全不透明紧贴夹具没有独立外轮廓的可绘制片元。AI-000111随后提供显式修复入口，把原Forward_Group正规化为唯一组权威源并换用带32像素透明边的专用夹具；AI-000112已在原点位完成幂等构建、Shader导入、GPU状态矩阵、两次Play重入和Stop清理，机器合同通过，线宽、颜色与整体观感仍由用户主观签收。

在该成员的 **Sprite Outline 2D** 组件上，把 **Merge Outline** 设为 **Force Off**。这只关闭该成员参与组描边合并，独立描边仍按自身Thickness绘制，不经过全组实体遮罩的内边排除；Thickness为0才是关闭描边本身。成员仍在原组，其他成员照常合并。

需要继续使用组的描边颜色/粗细时，保持 **Override Group** 勾选；它只选择参数来源，与ForceOff可以同时使用。**Merge Shadow** 独立设置，可继续Follow Group。不要只修改后端数据组件 **Sprite Outline Merge Member 2D** 上的同名字段，正式SpriteOutline2D会同步覆盖它。

Inspector下方“描边请求”应为ForceOff=>False、“实际合并”描边=False；这表示独立绘制，不是关闭描边。普通Sprite前后遮挡仍有效，且独立路径仍受下节网格/透明留白限制。面向配置人员的完整说明见`组件说明文档/视觉特效/描边与阴影_配置与使用说明.md`；本文件保留实现与验证入口，不重复维护完整参数表。

`SpriteOutlineGroup2D.CreateOrSynchronizeMergeAdapters()` 只对已存在且已显式配置 camera/shader/双宿主的 `SpriteOutlineMergeRenderer2D` 创建/同步轻量 adapter；它不自动创建后端、相机或资源。`overrideGroup` 只决定合并描边使用本地还是组色宽，双三态只决定参与。后端真实就绪时，单件 MPB 按描边/阴影分别关闭重复输出；ForceOff、组关闭或后端错误/禁用时继续走独立路径。合并阴影由组参数统一合成，本地阴影值仍保留供独立路径恢复。

## 几何与裁切边界

Shader 只能为 SpriteRenderer 已提交的几何片元着色，不能扩大网格。带足够透明留白并使用 Full Rect 网格的 Sprite 可显示画布内的外扩描边/阴影；Tight Mesh 会沿不透明轮廓收紧几何，偏移或模糊超出网格的部分可能被裁掉。同理，透明画布不足也会在纹理边界截断采样。本组件不能在单 Renderer 内可靠解决这些几何边界，不能把该限制视为已消除。

## 编辑器外验证与 Unity 验收

运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File DevTools/Rendering/Test-SpriteOutlineShadow.ps1
```

该测试会编译生产 C# 与最小 Unity 桩，并验证旧 API/默认值、有限值归一化、PropertyBlock 合并、禁用/重启用、Sprite 脏跟踪、组级幂等、四种合成组合；同时静态检查 Shader 属性、归一化高斯核、偏移公式、双 Pass 共用计算和禁止材质实例。Shader 文本检查不等于 GPU 编译或视觉验收。

后续 `ENGINE_MCP` 应在 Unity 2022.3.62f2 中完成：

1. 分别使用透明留白充足/不足、Full Rect/Tight Mesh 的 Sprite，观察描边、硬边阴影和最大预期模糊/偏移的裁切。
2. 在不同 Transform 缩放和 Pixels Per Unit 素材上确认参数保持“源图片像素”语义，并确认项目期望的屏幕观感。
3. 播放 Animator Sprite 曲线并用脚本在 `Update` 换帧，检查同帧同步、空 Sprite、禁用/重启用和运行时配置。
4. 检查仅原图、仅描边、仅阴影、二者同时开启；覆盖不同颜色 alpha、模糊半径和正负偏移。
5. 分别在 URP 2D Renderer 与 Forward fallback 检查 GPU 编译、透明混合和视觉一致性，并检查 Console。
6. 在目标设备用代表性 Sprite 数量测试关闭、硬边和 3×3 模糊三档性能与采样成本。

项目级文档影响：无。Shader 路径、材质路径、既有调用入口和序列化资源接线未改变；扩充能力登记在工具库表。
