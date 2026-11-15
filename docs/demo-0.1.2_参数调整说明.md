# demo-0.1.2 参数调整说明

最后实际核对：2026-11-13，当前源码、保存资产及用户整体通过决定。当前磁盘参数：Size 7.5～10；Expand Time 7秒；Shrink Delay 10秒、Shrink Time 14秒；Recenter Delay 5秒；Look Max Distance 2.5世界单位、Look Max Speed 4世界单位/秒；Edge Zone Ratio 0.2。这是用户最新保存值，源码默认值不变。 既有机器结果保持；新包已构建并普通启动/正常退出通过；用户已在2026-11-13明确包体全部验收通过。

## 动态视野

在 Project 选择 `Assets/Settings/Street/CameraFollowConfig.asset`，在其 **Camera Follow Config** Inspector 修改下列字段。街景镜头对象上的 **Camera Follow Config Binder** 是现有配置使用方；动态视野现由StreetFollowDriver写入本帧CameraState，不改序列化Lens。不要在运行时直接覆盖 Cinemachine 的 Lens Size 来代替正式配置。

Size 是正交画面垂直范围的一半，单位为世界单位。Size 越大，画面看到的范围越大，角色看起来越小。这里“放大”指扩大视野；“缩回”指收回视野。比例为本次 Size ÷ Ortho Size Max，已由驱动提供给偏移合成统一使用，不缩放 FOCUS。

| Inspector 字段 | 单位/范围 | 源码默认值 | 当前磁盘保存值 | 调整效果及关系 |
| --- | --- | --- | --- | --- |
| Dynamic Zoom Enabled | 开关 | 开 | 开 | 开启后由移动状态控制 Size；关闭时函数不给出 Size，驱动不写本帧Size，保留原镜头Lens，比例为 1。驱动已接入，既有机器基础检查及用户整体体验已通过。 |
| Ortho Size Min | 世界单位；有限且不大于 Max，实际镜头使用正值 | 7.2 | 7.5 | 起始及停止后的目标视野；调大后静止时看到更多，调小后更近。 |
| Ortho Size Max | 世界单位；正数且不小于 Min | 8.3 | 10 | 移动时的目标视野，也是比例分母；调大扩大运动中视野。与 Min 相等时固定 Size、比例 1。 |
| Move Speed Threshold | 世界单位/秒；有限非负 | 0.5 | 0.5 | 沿用脚底实际二维速度，严格大于此值才算移动；纯纵向移动也计入，按键但撞墙不动不算。调高更不容易触发。 |
| Expand Time | 秒；有限非负 | 1.2 | 7 | 从当前 Size 扩至 Max 的完整过渡时长；越大越慢，0 在下一次有效推进时到达目标。 |
| Expand Curve | Linear / Quadratic / SmoothStep / EaseInOut | EaseInOut | SmoothStep | 只控制扩张过程，不影响缩回。 |
| Shrink Delay | 秒；有限非负 | 0 | 10 | 停步后先保持当前Size，再开始缩回；越大等待越久，0保持旧行为。重新移动取消等待，再次停步完整重计。独立于目标回中的Recenter Delay，不改变Shrink Time。 |
| Shrink Time | 秒；有限非负 | 1.5 | 14 | 等待Shrink Delay后，从停步当时Size缩至Min的完整时长；越大越慢。0在等待结束后第一次有剩余推进时间时到目标。 |
| Shrink Curve | 同上四种枚举 | EaseInOut | Quadratic | 只控制缩回过程，不影响扩张。 |
| Freeze On Override | 开关 | 开 | 开 | 由现有驱动在外部接管时决定是否传入冻结。纯函数接收Frozen，驱动结合现有剧情/对话状态或NonLive接管处理；未新建对话系统。 |

Linear 匀速；Quadratic 前慢后快；SmoothStep 两端缓、中间快；EaseInOut 两端更平缓。它们是已有枚举预设，不是可绘制的 AnimationCurve。曲线按整段时间归一化求值，30/60/120 帧率的离线到达时长已核对。

中途停步先保持当前Size，等Shrink Delay后才开始对应Shrink Time，不先跳到上下限；重新移动取消等待并从当前Size扩张，再次停步重等完整延迟。跨越等待边界时仅用超出部分收缩，恰好到边界或零dt时保持值。第一次启用的首个样本固定为Min。冻结期间等待/过渡均不累计，关闭期间也保留状态但不给出Size；重新开启使用保留状态，组件重启或瞬移才按原规则重置。

最大值非正、Min 大于 Max、时长负数、非有限值或未知曲线会被拒绝；函数返回无效结果并保持前态，不改配置值。纯函数只实现原合同中的 Size 范围校验，当前驱动在动态启用时进一步要求Min>0，本轮用户已整体确认可见效果。不要通过更改停步速度倍率掩盖视野接线错误。

当前磁盘参数：Size 7.5～10；Expand Time 7秒；Shrink Delay 10秒、Shrink Time 14秒；Recenter Delay 5秒；Look Max Distance 2.5世界单位、Look Max Speed 4世界单位/秒；Edge Zone Ratio 0.2。这是用户最新保存值，源码默认值不变。Shrink Delay建议示例0.5秒+Shrink Time1.5秒：停步先保持0.5秒，再收缩1.5秒；默认0保留旧节奏。若想减少呼吸幅度，缩小上下限差；幅度合适但变化太急，调整过渡时长；只想晚一点开始缩回，则调Shrink Delay。建议示例不是已验收观感。

生效时机：配置资产已有Changed通知，Driver快照现包含Shrink Delay。静止期间修改Shrink Delay、Shrink Time、Shrink Curve或静止目标，从当前Size重开完整等待/收缩；移动期间仅修改Shrink Delay不重开扩张。冻结优先保留原Size与等待进度，恢复后再处理设置变化。非法延迟和等待状态与其它非法配置一样拒绝计算、不改用户值。ShrinkDelay已由用户验证通过；本次浮动联动的边界、可见范围与手感见新增人工检查。

本批保留Composer的一次位置阻尼、停步倍率、FOCUS、前视触边锁存、Confiner和人工保存值；回中新增浮动触发暂停/解除重启规则，见下节。当前新函数每次为固定量标量运算，不创建场景对象、无逐帧分配、无共享状态；它本身不会增加 GPU 绘制。

<a id="视角浮动v012-02"></a>

## 视角浮动：距离、速度和右摇杆

最后实际核对：2026-11-13，正式源码、现有Input System引用和离线消费链。配置入口为Project中的 `Assets/Settings/Street/CameraFollowConfig.asset` → **Camera Follow Config**。现有 **Street Follow Driver** 消费这些参数，无需新增组件；街景已保存的 **PlayerRoot_TUNE** 对象上，**Legacy Input Provider** 与正式 **Long Loop Player Controller** 同在一处，现提供键鼠和右摇杆视角。原0.1.2用户整体通过结论保留。2026-11-13 N0-OPT补丁仅离线核对同帧比例限幅，下面的距离含义已同步，新增比例表现仍待用户人工检查；这不增加逐设备或引擎验证结论。

| Inspector字段 | 单位/有效范围 | 源码默认值 | 当前磁盘保存值 | 调整效果及关系 |
| --- | --- | --- | --- | --- |
| Look Float Enabled | 开关 | 开 | 开 | 开启鼠标/右摇杆浮动；关闭停止触发暂停，已有浮动按Out Time和速度上限退出。 |
| Edge Zone Ratio | 单侧边缘占整幅宽/高比例，0到0.5之间且不含0 | 0.25 | 0.2 | 仅鼠标：越小越靠边才触发。右摇杆不套这层屏幕中央区。 |
| Look Response Curve | 四种既有曲线枚举 | Quadratic | Quadratic | 鼠标入边缘深度或摇杆死区后的向量幅度映射为目标强度，摇杆方向保持；不是时间曲线。 |
| Look In Time | 秒，有限非负 | 0.35 | 0.35 | 目标绝对幅度增大时的过渡时长；越大越慢，0仍受世界速度上限。 |
| Look Out Time | 秒，有限非负 | 0.5 | 0.8 | 目标幅度减小或相等（含等幅反向）的过渡时长；越大退出越慢。 |
| Look Max Distance | 最大视野下的世界单位，有限非负 | 2.5 | 2.5 | 浮动自身二维径向距离基准；实际硬上限为该值×同帧Size/Ortho Size Max，斜角共用半径。0清除浮动距离，越大允许看得越远。关闭动态视野时Ratio=1。 |
| Look Max Speed | 世界单位/秒，有限非负 | 8 | 4 | 浮动世界偏移变化速度上限，进入、退出、反向与视野比例变化共用；0保持当前合法浮动量，包含暂停正常退出。 |
| Gamepad Look Axis | RightStick | RightStick | RightStick | 已接真实右摇杆，不再只是预留；本次只接视角，不新增手柄移动或交互按键。 |

鼠标归一化输入在窗口外仍按最近边缘处理，不会随移出距离增加继续加大力度。两轴空间映射仍独立；世界目标在乘既有Max Horizontal/Vertical Offset及同帧Size/Max比例后，再按Look Max Distance×同一比例做二维半径限制；Look Max Speed不随比例缩放，合成时不重复乘比例。保存的2.5在Size=7.5、Max=10时对应1.875世界单位，在Size=10时对应2.5；这是基准值与实际半径的区别，保存值不变。最终同轴共享预算、安全区及Confiner仍可能进一步缩小实际可见移动。

这两个新上限约束浮动通道，不是角色跟随、前视、瞬移或最终整台相机的总速度上限。运行中调小距离上限，或视野缩小使实际半径小于当前偏移，都会按原越界纠正规则立即压回新边界（即使Look Max Speed=0）；重启、瞬移及最终门控清零也不按普通速度限制拖延。Look Max Speed=0会保留合法残余，想彻底禁止浮动距离应设Look Max Distance=0或按正常退出方式关闭浮动并保留正速度。

右摇杆读取已有Unity Input System的Gamepad.current和rightStick.ReadValue，沿已有stickDeadzone处理；有效非零值直接表示方向/强度，经过响应曲线和同一浮动上限，不必先推到屏幕边缘对应阈值。有效摇杆输入优先；摇杆回中后保持手柄中立，不被停在边缘的鼠标立刻重新触发。实际移动鼠标才回到鼠标视角；同时有效输入以摇杆优先。无设备、断开/禁用、失焦/暂停及组件停用安全归中。这里不提供新的死区或绑定编辑器。

浮动的当前有效目标非零时，暂停Shrink及Recenter：保留当前Size、X/Y回中和水平前视退出的位置及进度；期间不消耗等待或返回曲线时间。鼠标在边缘停住仍持续触发。移回非响应区、右摇杆归中或有效浮动被关闭/门控后立即解除，不必等Look Out尾部归零；从当前值重新等待完整Shrink Delay/Recenter Delay，再按各自Shrink Time/Curve或Recenter Speed/Ramp Time/Curve返回。每次解除只重启一次。

暂停只作用于收缩和回中；角色实际移动时，正常扩张、死区跟随与前视建立继续。对话/外部接管已有冻结规则优先。距离/速度为0或对应方向预算为0时，不形成有效浮动触发。浮动时间参数和返回时间参数各管自己的通道，不能相互代替。

当前保存样例为边缘比例0.2、Quadratic、0.35秒进入/0.8秒退出，距离2.5、速度4。幅度太大先减小Look Max Distance，移动太急再减小Look Max Speed或增大进出时间；仅为后续调参方法，不自动修改已验收参数。

<!-- DEMO012-VERTICAL-OFFSET-499-20261112 -->
## 纵向输入偏移（V012-03）

最后实际核对：2026-11-12；核对生产纯函数、输入抽象、配置源码和磁盘保存值。驱动源码已接入此通道，原生运行尚未验收。配置入口仍是Project中CameraFollowConfig.asset的 **Camera Follow Config** Inspector；本纯函数无需挂到对象上。

| Inspector字段 | 单位/有效范围 | 源码默认值 | 当前磁盘保存值 | 调整效果及关系 |
| --- | --- | --- | --- | --- |
| Vertical Speed Threshold | 世界单位/秒，有限非负 | 0.5 | 1 | 实际脚底Y速度绝对值严格低于它且有上/下输入才建立偏移。越大越容易在慢走时触发，越小越接近真正顶边才触发；相等不触发，0不会触发。 |
| Vertical Offset In Time | 秒，有限非负 | 0.3 | 0.3 | 触发建立及直接反向使用，从当前量走完整新时长；越大越慢，0在下一次正dt步到目标。 |
| Vertical Offset Out Time | 秒，有限非负 | 0.4 | 0.7 | 松开或速度恢复到阈值及以上时，已有量回0的完整时长；越大尾部越长。 |
| Vertical Offset Curve | Linear / Quadratic / SmoothStep / EaseInOut | SmoothStep | SmoothStep | 同一时间曲线用于进入、退出和反向；Linear匀速，Quadratic前慢后快，SmoothStep两端缓，EaseInOut两端更平缓。不是空间响应曲线。 |
| Max Vertical Offset | 世界单位，既有配置有效范围为有限非负 | 1.5 | 1.5 | 后续V012-04的纵向距离及求和上限配置；本函数不读取。调大可提高后续允许位移，浮动Y同时占用纵向预算，最后仍受裁剪限制。 |

输入只按上/下/无方向解释，非零模拟轴也取其符号，不按轴幅度降低满量目标。速度必须来自实际脚底纵向位移，不能拿按键意图或整个二维速度代替；仅横向移动不妨碍本函数按Y速度判断。松开后的目标为0但状态按退出时间回落，初始无输入且无旧状态才立即是0。正常纵向速度达到阈值及以上时，即使仍按住也不建立；负向速度取绝对值判断。

输入方向改变时从当时量开始新的完整进入时长；例如上偏移+0.4时改按下，先连续经过0再到−1，短时保留正偏移是过渡的一部分。曲线或本段时长变化也从当前值重新计时，阈值变化只有改变目标时才重开过渡。dt=0保持当前输出；负时间、负阈值/时长、非有限值、未知曲线和非法初始化状态返回无效，保留前态，后继不能把无效结果的默认0当正常输出。

建议起点可保留当前阈值1、0.3秒进入/0.4秒退出、SmoothStep，属于保存样例，未做新效果人工验收。若正常慢走也触发，先降低阈值；若顶边建立太急，增大In Time；若松开后拖尾太久，减小Out Time。实际位移幅度留后续合成参数控制，不用速度阈值冒充距离。

本函数仅出−1..1归一化量，状态由驱动持有；世界距离/动态视野比例/与浮动Y求和及裁剪归V012-04，镜头写入和实际脚底速度已由StreetFollowDriver源码接入。没有新增纵向对话门控规则，也不改变浮动最终硬门控。FOCUS不乘比例，Composer只做一次位置阻尼，通道进出过渡不另控相机位置。函数不持有对象、无逐帧分配或额外绘制；运行性能、边界裁剪和画面效果仍待接入检查，当前不承诺Inspector即时生效。
<!-- /DEMO012-VERTICAL-OFFSET-499-20261112 -->

<!-- DEMO012-COMPOSITION-500-20261112 -->
## 偏移合成与视野比例（V012-04）

最后实际核对：2026-11-12；核对合成及原前视/回中源码、三个前置接口和当前配置磁盘值。**偏移类参数的数值是最大视野时的距离**。比例为本帧Size÷Ortho Size Max；关闭动态视野时比例固定1。新增合成现已由StreetFollowDriver消费；既有Lookahead/上限等仍可能影响旧镜头，不要把“新合成未接入”理解为这些旧字段都没有作用。

| 项目/Inspector字段 | 是否乘视野比例 | 单位/范围 | 源码默认值 / 当前保存值 | 调整影响 |
| --- | --- | --- | --- | --- |
| Lookahead | 是；先取不大于横向上限的距离 | 世界单位，有限非负 | 2.1 / 3.5 | 最大视野下的前视距离；调大更前置，仍受触边建立与上限影响。 |
| Max Horizontal Offset | 是 | 世界单位，有限非负 | 6 | 前视+浮动X的共享限幅；同时作为浮动X满量距离，调大会同时增大预算与浮动满量。 |
| Max Vertical Offset | 是 | 世界单位，有限非负 | 1.5 | 纵向输入+浮动Y的共享限幅，两者满量都取此距离，不是各自独立最终预算。 |
| 浮动X/Y | 先乘轴上限与比例，再受Look Max Distance/Speed限制；合成不再重复乘 | 世界偏移向量 | 不在Inspector手填 | 与同轴另一通道同向限幅、反向抵消；最终门控优先归零。 |
| 纵向输入归一化量 | 换成世界距离时乘纵向上限和比例 | −1..1；由纵向函数产生 | 不在Inspector手填 | 与浮动Y求和后限幅，不新增对话门控。 |
| FOCUS | 否 | 沿用现有跟随点配置 | 本卡保持 | 始终先加到脚底跟随点，不随镜头大小拉伸。 |
| Dead Zone Width / Height | 比例值不乘；世界范围按本帧Size计算 | 0..1 | 0.1/0.2 / 当前0.05/0.2 | 屏幕占比保持，Size变小时相应世界宽高自然变小。 |
| 安全区比例 | 比例值不乘；范围按本帧Size计算 | 沿用既有安全区入口，0..1 | 本卡保持 | 全部偏移求和后只裁剪一次，不能先各裁各的再相加。 |
| Recenter Speed | 否 | 世界单位/秒，有限非负 | 4 / 0.65 | 回中速度设置不随Size乘系数；连续缩放保留累计回中时间，按变化后的路径重算原速度profile。 |
| Lookahead Time / Recenter Delay / Recenter Ramp Time | 否 | 秒，有限非负 | 0.5/0.6/0.25 / 当前0.8/0.8/0.25 | 保持原建立/等待/缓起缓停语义，不用比例再加速度增益。 |
| 速度阈值、Damping、Stopped Speed Multiplier及其它时长/曲线 | 否 | 沿用各原说明 | 本卡保持 | 不是偏移距离；不因视野比例改用户值或额外控相机速度。 |

实际配置入口仍为Project中CameraFollowConfig.asset的 **Camera Follow Config** Inspector；FOCUS/安全区使用原有入口，本卡未迁移绑定。上表“当前保存值”是磁盘样例，不是新合成的人工认可建议值。

横向距离为前视加浮动X后裁到Max Horizontal Offset×比例；纵向为纵向输入加浮动Y后裁到Max Vertical Offset×比例。例：最大Size10、前视5且横向上限不低于5，Size10时稳定前视5，Size5时2.5；若横向上限10、Size5，浮动原始目标X为5；新Look Max Distance默认2.5时，其自身再被限至2.5，速度限制还会影响到达过程；与前视同向合成仍共同限到5，反向可抵消。这个示例用于解释规则，不覆盖当前保存值。

视野与几何的生效顺序：调用方先算当前Size/比例；合成用该Size计算可见宽高，再推进目标/前视、转换和求和，最后一次安全区裁剪。真实前视Carry/Exit在比例变化时同步重投影，回中累计秒数保留，不反复重启缓起；固定7.2/8.3这类非二进制比例也不会误判成每帧变化。合成只写整体Target，额外浮动/纵向/兴趣不回灌前视状态。

典型起点为保留当前保存距离3.5/8/1.5，接入后再按人工观感调。希望只减弱浮动时优先调Look Max Distance，并核同轴共享预算；希望改变建立/退出节奏则调对应时间。边界裁剪和Confiner可使实际可见位移小于配置距离，这属于正常限制。配置资产Changed机制保持；当前已有Driver源码调用，仍未原生验证新比例效果。
<!-- /DEMO012-COMPOSITION-500-20261112 -->

<!-- DEMO012-PARALLAX-502-20261112 -->
## 层级视差（V012-06）

最后实际核对：2026-11-12；公式和层组件已离线验证，六组件现已在街景挂载并保存读回。参数入口为各LL_*_TUNE层容器的 **Street Parallax Layer**，不在CameraFollowConfig上；源码默认系数为1/1、引用为空；当前六层已保存下表两轴系数，全部引用ParallaxOrigin_TUNE与Cam_Street的Brain。

| 对应排序层/已保存容器 | 横向保存值 | 纵向保存值 | 预期相对画面移动 |
| --- | --- | --- | --- |
| LL_Background / LL_Background_TUNE | 0.3 | 0.3 | 远景最慢 |
| LL_Buildings / LL_Buildings_TUNE | 0.6 | 0.6 | 建筑较慢 |
| LL_MidgroundBack / LL_MidgroundBack_TUNE | 0.85 | 0.85 | 中后略慢 |
| LL_Characters / LL_Characters_TUNE | 1 | 1 | 不补正，保持普通角色层 |
| LL_MidgroundFront / LL_MidgroundFront_TUNE | 1.15 | 1.15 | 中前略快 |
| LL_Foreground / LL_Foreground_TUNE | 1.35 | 1.35 | 前景最快 |

两轴系数为独立无量纲有限值，不自动限到0..1。1无补正，小于1为较远层，大于1为较近层；调大让相对画面移动更快。补正世界位移=(最终相机XY−原点XY)×(1−系数)，Z不补正，Size不参与。例：背景0.3、相机右移10，容器右补正7；前景1.35则左补正3.5，画面中表现为前景更快。

ParallaxOrigin_TUNE当前已由用户保存为(-1.75,-2.21,0)，不恢复历史出生点初值；六层的Parallax Origin都绑定它，Camera Brain绑定现有Cam_Street的CinemachineBrain。用户整理层内物件后，可将原点XY移到希望各层恢复摆放对齐时的相机位置；出生点与带FOCUS的相机位置可能不同，初始不是必然零补正。

启用时记录摆放，每次绑定Brain输出事件后按“初始位置+本次补正”绝对定位，不累加。运行调系数/原点在下一输出事件生效，编辑模式不移动容器；当前已通过一次基础Play三项检查，仍不等于运行行为或观感验收。停用/销毁/退出恢复原摆放，原父级未变时精确恢复localPosition；子物件局部坐标不写回。当前预设是原卡值，不是人工签收建议。完整字段、默认/保存区别、父级和循环接线限制、示例及成本见[中文组件说明](../组件说明文档/视觉特效/街景层级视差_配置与使用说明.md)。
<!-- /DEMO012-PARALLAX-502-20261112 -->

<!-- DEMO012-DRIVER-501-20261112 -->
## 驱动接入、生效与边界（V012-05）

源码最后核对2026-11-12；驱动接入已离线完成，原生导入/运行和整体体验未验收。CameraFollowConfig仍为正式调参入口，源码默认/当前保存值沿各节表格保持，没有为回归改生产默认或资产值；旧测试仅AI504明确关两开关。

成功帧的Size来自StreetDynamicZoom并在目标计算前写本帧Lens；关闭Dynamic Zoom Enabled不给Size、不覆盖输入Lens，Ratio1。相机序列化Size、FOCUS、Binder停步倍率、Composer阻尼不改。普通参数Changed刷新快照并按纯函数规则从当前值重定目标。仅关闭/重开功能会保留Zoom进度；停用再启用驱动、显式瞬移或自动TeleportReset会重置三状态，动态启用时首样本Min、浮动/纵向值0。重复/同时间回调复用已成功Size，不推进目标或三状态，避免Cinemachine每次先重拉场景Lens而跳回原Size。

输入优先现有IStreetChannelState.Input，再查gameState/Foot的公开父级IGameInput。正式Foot_TUNE是PlayerRoot_TUNE子节点，其父级LegacyInputProvider给鼠标/纵向轴；速度来自脚底实际XY位移，不用输入意图或provider假速度代替。RightStick仍预留。Gate Offsets In Dialogue取CameraModeConfig开关，活动状态由既有ConfigureGameState/IStreetChannelState.DialogueOrCinematic提供，默认未置位；没有新对话系统或序列化引用。

门控时最终浮动贡献为0，内部状态继续按Look Out Time退出。Freeze On Override与门控开关独立：live对话/剧情状态冻结Zoom，NonLive时保留Zoom并在返回首样本复用；关闭冻结时NonLive重置。纵向输入不添加同一浮动门控。

安装Confiner2D3.1.7要求Size变化时公开InvalidateLensCache；驱动比较上一实际应用Size而非每次传入的场景初值，实际改变才刷新，稳定/重复回调不重复刷新；停用归还原Lens时失效缓存供原生按原Size重建。原OversizeWindow.Enabled0、MaxWindowSize0保持，0表示不限制潜在窗口大小，不必为本卡改该优化提示。边界多边形仍要容纳最大视野，真实边界与分配成本待原生检查。无反射、内部缓存写入、位置修正量写入或第二层相机阻尼。

可选StreetCameraDebug增加Size/Ratio及四贡献，默认日志关闭，需显式ConfigureLogging与RequestSample才输出最近成功样本；当前磁盘debugger为空，不擅自添加调试器或作为验收前置。所有实际原生表现依[人工定稿](demo-0.1.2_人工检查说明.md#驱动接入后的人工检查定稿179)，不以离线通过代签。
<!-- /DEMO012-DRIVER-501-20261112 -->
