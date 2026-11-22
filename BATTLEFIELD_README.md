# 战场画面原型

Unity 2022.3 URP 项目。100 px = 1 Unity 单位。

- 战场：320 × 64 单位，对应 32000 × 6400 px；X 为左右推进方向，Z 为场地宽度。
- 堡垒中心：X = -140、+140，相距 280 单位。
- 堡垒占位画面：25.6 × 32 单位；根节点碰撞体：16 × 24 单位，厚度暂为 4 单位。
- 堡垒配置：1200 血、56 单位射程、18 伤害、1 秒攻击间隔。当前只保存数据，不执行攻击。

在 Unity 中打开项目后，可以通过 `Tools > Battlefield > Rebuild Prototype Scene` 重新生成 `Assets/Battlefield/Scenes/Battlefield.unity` 和堡垒预制体。场景会加入 Build Settings。之后将 `Artwork placeholder` 子物体替换为带立绘的 SpriteRenderer，即可沿用堡垒根节点的位置、碰撞体和配置。

## 兵种预制体

`Tools > Battlefield > Rebuild Unit Prefabs` 会生成 `Assets/Battlefield/Units/Prefabs` 中的 13 个兵种。首次导入脚本时也会自动生成。每个预制体都有 `UnitStats` 配置、4.8 × 4.8 单位的根碰撞体，以及 9.6 × 9.6 单位的临时立绘。蓝色代表我方，红色代表敌方；替换 `Artwork Placeholder - replace Sprite` 子物体的 Sprite 后即可使用正式立绘。

移速和射程已从 px 换算成 Unity 单位（除以 100）。特殊机制目前保存在 `specialMechanic` 文本字段中，战斗效果尚未实现。制作真实立绘时，建议导入设置为 Sprite、Pixels Per Unit = 100，保证 960 × 960 px 对应 9.6 × 9.6 单位。

## 士兵对象池

兵种预制体生成后会同时生成 `Assets/Battlefield/Units/SoldierPool.prefab`。把该预制体拖入战斗场景。默认每种兵预热 4 个实例；不足时自动增建。生成：`pool.Spawn(兵种ID, 世界坐标, 朝向)`；回收：`pool.Despawn(士兵对象)`。回收后实例保持在对象池下并停用，重复回收或把其他对象传入时会返回 `false`。

## 费用系统

战斗场景中的 `Battle Cost (finite 50 points)` 从 0 费开始，每秒生成 1 费，整局累计最多生成 50 费。花费不会使已用掉的生成额度恢复。通过 `BattleCost.TrySpend(amount)` 扣费，`CanAfford(amount)` 判断余额；`Ledger.Current` 是可用费用，`Ledger.Generated` 是累计已回复费用。组件会在画面左上角显示费用。若场景中没有该对象，使用 `Tools > Battlefield > Install Cost System` 安装。
