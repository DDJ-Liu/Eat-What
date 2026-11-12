<!-- STREET-OUTLINE-USER-PAUSED-20261110 -->
2026-11-10 用户明确反馈：本轮街景主角/NPC角色描边验收不通过，具体配置暂停，等待用户后续继续触发。保留现有Lab四对照和22份材质供后续使用，本次不调整、不接入正式Street。这不撤销此前独立Sprite效果的历史验收；AI464机器3/3也不代表本次Spine视觉通过。
<!-- /STREET-OUTLINE-USER-PAUSED-20261110 -->

# 街景 idle Tomato NPC：Prefab 配置与使用

最后实际核对：2026-11-09，Unity 6000.3.24f1，AI464 attempt2。已核对源码、实际 SkeletonData/skin/idle、Prefab/Street 保存和公开 API 读回；本卡未 Play。前后遮挡、自然动画、缩放画面与最终观感待人工，正式街景描边也待 Lab 视觉通过。

## 入口与拖放

Prefab：`Assets/Prefabs/LongLoop/NPC/TomatoIdleNPC.prefab`，GUID `18482e8563931f945979fc4facbec5b8`。可直接拖到 Street；不要把现玩家对象连同控制器复制成 NPC。Prefab 根为 `TomatoIdleNPC_TUNE`，根默认启用、位置为零；`Foot_TUNE` 与 `Visual_TUNE` 是它的直接子节点，默认局部位置均为零。

当前 Street 实例脚底坐标为 **(3.4,-4.09,0)**，在本轮实际 WalkArea 内，比运行出生标记 `PlayerSpawn_TUNE=(0.4,-4.09,0)` 向右3单位。Edit 中玩家 Foot 在(0,0,0)，Start 后会按原 StreetSceneEntry 去出生点；不要为对齐 Edit 玩家而把 NPC 移到可走区外。本卡没有移动玩家、出生点、WalkArea 或 Confiner。

## 外观与 idle

选择实例的 **Visual_TUNE**：

- **Skeleton Animation** 使用 Tomato 的 `skeleton_SkeletonData.asset`，初始 skin 为 `Tomato_Non`，动画 `idle`、**Loop** 开、**Time Scale** 1。实际 idle 时长1秒；walk、kick存在，但本 NPC 不消费输入或切换行走。
- **Character Equipment** 的 **Base Skin** 为 `Tomato_Non`，**Apply Default Equipment On Start** 开，**Auto Optimize** 关。Hair/Eyes/Ear/Mouth/TopCloth/BottomCloth/Shoes 分别为真实存在的 `Tomato_Hair_A`、`Tomato_Eyes_A`、`Tomato_Ear_A`、`Tomato_Mouth_A`、`Tomato_TopCloth_A`、`Tomato_BottomCloth_A`、`Tomato_Shoes_A`。这些是本试样初值，不代表组件通用默认或推荐唯一外观。换装前用实际 skin 下拉列表确认名称；空部件不应猜填。
- **Mesh Renderer** 序列化为关闭，保留 **Spine Runtime Mesh Renderer Bootstrap**，运行 Start 后启用。这是避免旧编辑态派生网格恢复问题的既有模式，不能用替换共享材质或删除 Bootstrap 来掩盖。
- 本 Prefab 没有玩家控制器、输入、镜头跟随点，也没有正式描边 Custom Materials。字段中只引用自身子节点及持久配置/角色资源；公开 API 已核无外部场景引用。

## 前后关系与脚底

根上的 **Y Sorter** 接自身 Foot，目标为 Visual 的 **Sorting Group**，层为实际 **LL_Characters**。本试样 **Base Order=499**，当前玩家500；**Precision TUNE=100**、**Depth Offset=0**、**Bake On Start** 关。原组件默认 Base Order=0、Precision=100、Depth Offset=0、Bake=false，本 Prefab 的499是角色差分初值。

排序按 `BaseOrder - Round((Foot世界Y + DepthOffset) × Precision)` 计算并限于合法整数范围。同脚底Y时玩家比 NPC 高1；脚底 Y 变化后仍可交叉，499并非永远在玩家或全部物体后。Precision 越高，同样Y变化造成的 order 变化越大；不要用极大固定 order 代替脚底排序。若玩家 Base Order 后续改变，应按新值把 NPC 设为低1；不会自动追随玩家配置。

Foot 是独立稳定基准。移动角色请移动根；修改 Foot 的局部偏移时，同时核对 Visual 中 Depth Scale 的 **Local Foot Offset**，二者不是自动绑定。实际轮廓脚底是否贴地仍需人工看画面。

## 缩放来源与缺省行为

Visual 的 **Depth Scale**：**Local Config** 指向现 `Assets/Settings/Street/DepthScaleConfig.asset`，**Ground Line** 留空，使用 `StreetConfigs.Current.GroundLine`；**Local Foot Offset=(0,0,0)**、**Use Override Factor** 关。Prefab 因此不携带对 Street GroundLine 的非法场景引用。

配置公式为 `factor=1+(GroundLineY-FootY)×DepthScaleFactor`；场景系数约0.03是当前用户值，组件 Override Factor 默认0.03，二者不要混淆。较靠下的脚底通常更大；配置/地面线缺失或非法时组件退回 factor=1并记录状态，不自动寻找同名场景对象。当前实例在地面线Y约-4.14、脚底Y约-4.09时公式约0.9985；这只是公式说明，未作 Play 视觉保证。

拖到其它场景时，应由该场景现有 StreetConfigBinder 提供 GroundLine；或在场景实例显式指定 Ground Line。将场景引用 Apply 回 Prefab 会破坏复用边界。调整外观基础尺寸用 Visual 的基准 scale，并停下运行后修改，避免与运行期 Depth Scale 所有权冲突。

## Edit 预览、运行与交付状态

本卡未 Play。Edit 预览可临时打开 Visual 的 Mesh Renderer，在 Scene 视图选中样例按 F；预览后将 Renderer 关回，避免把临时打开保存进正式 Prefab/场景。实际 idle 连续播放、玩家前后绕行和自然缩放需要后续适用的 Play/人工验收授权；本卡没有替代签收。

Console 出现原图集 PMA 材质与 Linear 色彩空间警告。原图集材质、贴图、Straight Alpha 开关和全局设置均保持；透明边缘、混合和颜色需要实景人工核对，不在本卡改为另一种混合方式。

2026-11-10 AI464 attempt4 已依本 ENGINE 直接用户批准，通过公开 API 将保存 NPC 时附带写入的 Cam_Street.localPosition 精确恢复为(0,2.0000002,-10)，单次保存并读回成功；NPC、其它作者对象及配置保持，原三合同机器证据3/3具备，正式结算见 attempt4 报告。a1～a3失败和实际平台拒绝完整保留。Edit 中 Cinemachine 后续仍可能派生相机位置，应区分磁盘作者值和内存派生值，不能据此循环保存。NPC自然Play、Lab观感和正式描边接入仍未验收，本次位置许可不等于视觉签收。
