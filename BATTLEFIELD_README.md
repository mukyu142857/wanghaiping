# 战场画面原型

Unity 2022.3 URP 项目。100 px = 1 Unity 单位。

- 战场：320 × 64 单位，对应 32000 × 6400 px；X 为左右推进方向，Z 为场地宽度。
- 堡垒中心：X = -140、+140，相距 280 单位。
- 堡垒占位画面：25.6 × 32 单位；根节点碰撞体：16 × 24 单位，厚度暂为 4 单位。
- 堡垒配置：1200 血、56 单位射程、18 伤害、1 秒攻击间隔。当前只保存数据，不执行攻击。

在 Unity 中打开项目后，可以通过 `Tools > Battlefield > Rebuild Prototype Scene` 重新生成 `Assets/Battlefield/Scenes/Battlefield.unity` 和堡垒预制体。场景会加入 Build Settings。之后将 `Artwork placeholder` 子物体替换为带立绘的 SpriteRenderer，即可沿用堡垒根节点的位置、碰撞体和配置。

## 兵种预制体

`Tools > Battlefield > Rebuild Unit Prefabs` 会生成 `Assets/Battlefield/Units/Prefabs` 中的 13 个兵种。首次导入脚本时也会自动生成。每个预制体都有 `UnitStats` 配置、4.8 × 4.8 单位的根碰撞体，以及 9.6 × 9.6 单位的临时立绘。蓝色代表我方，红色代表敌方；替换 `Artwork Placeholder - replace Sprite` 子物体的 Sprite 后即可使用正式立绘。

移速和射程已从 px 换算成 Unity 单位（除以 100）。特殊机制目前保存在 `specialMechanic` 文本字段中，兵种特殊效果由 `UnitCombat` 在战斗中结算。制作真实立绘时，建议导入设置为 Sprite、Pixels Per Unit = 100，保证 960 × 960 px 对应 9.6 × 9.6 单位。

## 士兵对象池

兵种预制体生成后会同时生成 `Assets/Battlefield/Units/SoldierPool.prefab`。把该预制体拖入战斗场景。默认每种兵预热 4 个实例；不足时自动增建。生成：`pool.Spawn(兵种ID, 世界坐标, 朝向)`；回收：`pool.Despawn(士兵对象)`。回收后实例保持在对象池下并停用，重复回收或把其他对象传入时会返回 `false`。

## 费用系统

战斗场景中的 `Battle Cost (finite 100 points)` 从 0 费开始，每 1.2 秒生成 1 费，每回合累计最多生成 100 费。花费不会使已用掉的生成额度恢复；`BattleCost.ResetRound()` 可在下一回合将费用和累计回复都重置为 0。通过 `BattleCost.TrySpend(amount)` 扣费，`CanAfford(amount)` 判断余额；`Ledger.Current` 是可用费用，`Ledger.Generated` 是累计已回复费用。组件会在画面左上角显示费用。若场景中没有该对象，使用 `Tools > Battlefield > Install Cost System` 安装。

## 四个关卡变体

`Tools > Battlefield > Create Four Stage Variants` 从现有 `Battlefield.unity` 复制出 `Dock`、`Plain`、`Mountain`、`River` 四个场景，并加入 Build Settings。已有的变体场景不会被覆盖。各关有独立地面材质、临时天空配色和空的 `Background Artwork Slot`，便于之后替换背景立绘。`BattleStage` 组件保存本关可用的兵种 ID：通用我方 1/2/3、通用敌方 8/9，加上对应地形专属兵种（码头 4/10，平原 5/11，山地 6/12，河流 7/13）。场景会放入对应兵种的静态预览实例；出兵限制与敌方生成由战斗脚本读取这些 ID。

## UI 界面

`MainMenu.unity` 和四个地形场景已直接挂载 `Game UI` 组件，主菜单已排在 Build Settings 第一位。打开场景后，`Game UI` 会自动生成可在编辑器中查看的 `UI Canvas`，并将场景标记为待保存；按 Play 后会重建带完整点击行为的 Canvas。流程为主菜单 → 选地形 → 关卡介绍 → 战斗；地形卡片直接打开该地形的介绍弹窗，关闭弹窗返回选地形界面。战斗界面有费用、四个兵种按钮、进攻／撤退／防御、暂停、结算。点击兵种按钮会检查并扣除费用，从对象池取兵。战斗指令作用于全体我方士兵：默认防御，进攻时推进并攻击，撤退时回到左侧基地。每个地形场景共有四回合：首回合防守并清敌，第二、三回合依次摧毁地图 1/3 与 2/3 位置的据点（800、1000 血），第四回合摧毁敌方堡垒（1200 血）。首回合敌军编成待确定，`MarkDefenseWaveFinished()` 留作后续波次结束通知；回合结束先停在占位界面，点击临时继续按钮才进入下一回合，场上我方士兵保留。可通过 `GameUI.ShowResult(bool victory, string message)` 显示结算界面。无正式 UI 图片，当前为可替换的占位界面。

## 战斗镜头

四个地形场景使用斜俯视正交摄像机。16:9 Game 视图下，初始横向视野约 106.7 Unity 单位，即 320 单位地图长度的三分之一。按 A 向左、D 向右平移镜头，移动速度 35 单位/秒；镜头中心被限制在地图边界内。镜头控制脚本挂在四个场景的 `Main Camera` 上。

战斗场景的摄像机画面从屏幕底部约 330/1080 处开始，底部操作条占用独立区域，不再盖住地图。四关的 `Unit Lineup Preview` 默认关闭，开局没有摆在场上的士兵。点击我方兵种按钮后，士兵在左侧堡垒附近 X=-118、Z=-24～24 随机出生；当前沿用原型敌军生成器：从右侧堡垒附近每 8 秒随机出兵，首次等待 5 秒；正式首回合波次数量和兵种配置待确定。士兵出生后等待 1.5 秒行动；我方默认防御，敌军自动推进。生命归零时士兵回到对象池。当前攻击是直接扣血，已生效的地形效果包括码头远程伤害 +15%、平原移速 +10%、山地远程射程 +15%、河流远程承伤 -15%；已加入破甲、眩晕、150 px 击退、300 px 溅射等效果；尚未制作投射物和动画。
