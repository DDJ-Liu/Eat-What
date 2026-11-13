# CFG-01 · 配置组范式与参数调整说明模板 [CODE] `S · 40` `module: L`

> 来源：派发包_可玩包体0_1_1_v1（2026-11-03）§二；单卡文件；U 隔离清单有效；参数口径 G98（每卡附《参数调整说明》段）。

### CFG-01 · 配置组范式与参数调整说明模板 [CODE] `S · 40` `module: L`
**目标**：按 E2 附录 C.13 建配置组 ScriptableObject 骨架与《参数调整说明》模板，后续卡只填段。
**内容**：① `Assets/Settings/Street/` 下建 `CameraFollowConfig`（已有雏形，补齐 C.3~C.6 字段）、`CameraModeConfig`（C.2）、`DepthScaleConfig`（C.1 的 depthScaleFactor/DEPTH/FOCUS）、`SceneBoundsConfig`（C.8）；其余七组（Interest/Transition/Cinematic/Effects/Occlusion/SceneConfig/RoomOverride）只建空类不建资产；② 字段名 = E2 附录 C 参数名；预设值 = 附录 C 预设；③ 运行时可读：提供 `StreetConfigs.Current` 只读访问 + 运行时修改后 `Changed` 事件；④ `docs/参数调整说明_街道镜头.md` 模板：四列（E2 参数 / 插件或字段名 / 挂载点 / 调整方式）+ 每卡一节。
**要求**：不引入第三方；SO 资产 `.meta` 由本卡生成；不改 CM 组件。
**验收**：编译 0 Error；四个 SO 资产可加载；模板文件在位；单测：修改字段触发 `Changed`。
**参数说明段**：本卡写模板与 C.1~C.3 的对照表头。
