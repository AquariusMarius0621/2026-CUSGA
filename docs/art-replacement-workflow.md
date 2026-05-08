# 美术替换工作流

Updated: 2026-04-20

## 这份文档是干什么的
这份文档专门告诉你：

- 以后如果你要替换塔、敌人、UI、地图的美术资源
- 应该优先去改哪些场景对象
- 应该优先去看哪些 Inspector 字段
- 哪些地方已经整理成“不需要改玩法代码”

目标只有一个：

让你以后换自己的美术资源时，尽量不需要重写玩法脚本。

## 最推荐的替换顺序
1. 先换运行时 Prefab 的外观，并把场景原型当结构对照
2. 再换 UI 卡片和 HUD 样式
3. 最后换地图场景装饰和路径表现

原因是：

- 原型外观会直接影响游戏里生成出来的塔和敌人
- UI 只是显示入口，晚一点改不会影响玩法
- 地图装饰最自由，放到最后最不容易返工

## 运行时 Prefab 入口
当前真正用于运行时生成的实体，已经整理到这些 Prefab 资产里：

- `Assets/Prefabs/TowerDefense/Runtime/RelayTowerPrototype.prefab`
- `Assets/Prefabs/TowerDefense/Runtime/SingleTargetTowerPrototype.prefab`
- `Assets/Prefabs/TowerDefense/Runtime/SlowFieldTowerPrototype.prefab`
- `Assets/Prefabs/TowerDefense/Runtime/BombardTowerPrototype.prefab`
- `Assets/Prefabs/TowerDefense/Runtime/EnemyPrototype.prefab`

战斗反馈也已经整理成单独的 Prefab 入口：

- `Assets/Prefabs/TowerDefense/Vfx/ShotTrace.prefab`
- `Assets/Prefabs/TowerDefense/Vfx/SlowPulse.prefab`
- `Assets/Prefabs/TowerDefense/Vfx/BombProjectile.prefab`
- `Assets/Prefabs/TowerDefense/Vfx/BombExplosion.prefab`

这意味着你后面如果想直接替换“真正运行时会生成出来的实体美术”，
优先改这些 Prefab 资产就行。

`SampleScene / RuntimePrototypes` 下面那些对象现在更适合当作作者参考原型和结构对照。
如果你刻意改了场景里的原型，并且想把它们重新同步回 Prefab，
就需要重新执行编辑器工具 `TowerDefensePrefabAuthoringTool.BatchCreateOrUpdateRuntimePrefabs`。

## 一、替换塔的美术

### 1. 战斗塔
先说明这一点：

- `SampleScene / RuntimePrototypes / DefenseTowerPrototype` 现在更适合拿来对照层级和挂点
- 真正运行时会生成的战斗塔，优先分别改：
  - `Assets/Prefabs/TowerDefense/Runtime/SingleTargetTowerPrototype.prefab`
  - `Assets/Prefabs/TowerDefense/Runtime/SlowFieldTowerPrototype.prefab`
  - `Assets/Prefabs/TowerDefense/Runtime/BombardTowerPrototype.prefab`

如果你只是想知道有哪些挂点，就先在 `SampleScene` 里展开：

- `RuntimePrototypes`
- `DefenseTowerPrototype`

你会看到这几个关键层级：

- `DefenseTowerPrototype`
- `FeedbackRoot`
- `TypeSignatureRoot`
- `LevelMarkerRoot`

重点怎么改：

- 想换塔本体外观：
  现在三种战斗塔已经支持各自独立主塔身 Sprite
  不再只是共用一个主体图片。
  重点改 `DefenseTower` 组件里三套 `CombatTuning` 各自的：
  - `singleTargetTuning.bodySprite`
  - `slowFieldTuning.bodySprite`
  - `bombardTuning.bodySprite`

  如果某一类没指定 `bodySprite`，
  会自动回退到原型体当前默认主 Sprite，
  所以老场景不会因为新增这个入口而突然丢图。
- 想换攻击反馈：
  现在建议优先改反馈 Prefab 资产本身：
  - `Assets/Prefabs/TowerDefense/Vfx/ShotTrace.prefab`
  - `Assets/Prefabs/TowerDefense/Vfx/SlowPulse.prefab`
  - `Assets/Prefabs/TowerDefense/Vfx/BombProjectile.prefab`
  - `Assets/Prefabs/TowerDefense/Vfx/BombExplosion.prefab`

  如果你只是想让单体塔 tracer、炸弹塔飞行物、爆炸更明显，
  这一步通常确实需要你自己补正式美术资源，或者至少调这些 Prefab 里的：
  - `SpriteRenderer.sprite`
  - `SpriteRenderer.color`
  - `Transform.localScale`
  - 你自己额外加的子物体、粒子或发光图层

  `DefenseTower` 组件里的三套 `CombatTuning` 已经接好了对应入口：
  - `singleTargetTuning.shotTracePrefab`
  - `slowFieldTuning.slowPulsePrefab`
  - `bombardTuning.bombProjectilePrefab`
  - `bombardTuning.bombExplosionPrefab`

  所以后面你主要是换 Prefab 资源，而不是改玩法逻辑。
- 想调整反馈出现位置：
  改 `FeedbackRoot`
- 想调整塔型签名位置：
  改 `TypeSignatureRoot`
- 想调整等级标记位置：
  改 `LevelMarkerRoot`

### 2. 继电器
同样建议这样理解：

- `SampleScene / RuntimePrototypes / RelayTowerPrototype` 主要用于看结构和挂点
- 真正运行时会生成的继电器，优先改 `Assets/Prefabs/TowerDefense/Runtime/RelayTowerPrototype.prefab`

在 `SampleScene` 里展开：

- `RuntimePrototypes`
- `RelayTowerPrototype`

你会看到：

- `RelayTowerPrototype`
- `VisualRoot`

重点怎么改：

- 想换继电器本体外观：
  改 `VisualRoot` 上的 `SpriteRenderer`
- 想改继电器运行时颜色逻辑：
  改 `RelayTower` 组件里的：
  - `normalColor`
  - `saturatedColor`

### 3. 这些改动会不会影响逻辑
正常不会。

因为现在战斗塔、继电器的玩法脚本都已经尽量通过这些显式引用工作：

- `bodyRendererReference`
- `feedbackRootReference`
- `typeSignatureRootReference`
- `levelMarkerRootReference`
- `visualRootReference`

而且战斗塔主体外观本身也已经按塔类型分开到各自 tuning 里：

- `singleTargetTuning.bodySprite`
- `slowFieldTuning.bodySprite`
- `bombardTuning.bodySprite`

所以你改的是外观挂点，不是玩法算法。

## 二、替换敌人的美术

同样地：

- `SampleScene / RuntimePrototypes / EnemyPrototype` 主要用于看结构和挂点
- 真正运行时会生成的敌人，优先改 `Assets/Prefabs/TowerDefense/Runtime/EnemyPrototype.prefab`

在 `SampleScene` 里展开：

- `RuntimePrototypes`
- `EnemyPrototype`

你会看到：

- `EnemyPrototype`
- `VisualScaleRoot`
- `HealthBarRoot`

重点怎么改：

- 想换敌人本体外观：
  改 `VisualScaleRoot` 上的 `SpriteRenderer`
- 想换敌人受击时的颜色反馈：
  改 `Enemy` 组件里的：
  - `bodyColor`
  - `slowTintColor`
  - `standardHitFlashColor`
  - `bombardHitFlashColor`
- 想换血条样式：
  改 `Enemy` 组件里的：
  - `healthBarFillSpriteOverride`
  - `healthBarBackgroundSpriteOverride`
  - `healthBarFillColor`
  - `healthBarBackgroundColor`
- 想调身体缩放反馈和血条位置互不影响：
  现在身体缩放走 `VisualScaleRoot`
  血条走 `HealthBarRoot`
  这两层已经拆开了

## 三、替换部署卡和 HUD 样式

### 1. 四张部署卡
在 `SampleScene` 的右侧部署区里，关键对象是：

- `RelayTowerButton`
- `DefenseTowerButton`
- `SlowFieldTowerButton`
- `BombardTowerButton`

每张卡上都有 `TowerShopCard`。

现在卡片已经有这些显式视觉入口：

- `backgroundImageReference`
- `iconImageReference`
- `accentGraphicReferences`

你可以怎么改：

- 想换卡片底图：
  改按钮本体 `Image`
- 想换卡片主图标：
  改图标子物体 `Image`
- 想换卡片细节装饰：
  改 `accentGraphicReferences` 对应的那些小图形

### 2. 四张卡片的统一文案和配色
这些现在主要从 `SampleScene` 里的 `GameController` 读取。

看 `TowerDefenseGame` 组件里的：

- `Tower Presentation`

这里每种塔都有一组配置：

- `displayName`
- `cardRoleSummary`
- `selectionHint`
- `upgradeFocusSummary`
- `accentColor`
- `cardIconSprite`
- `cardIconTint`
- `cardBackgroundTint`
- `cardAccentTint`

以后如果你想统一改卡片图标和卡片配色：
优先改这里。

### 3. HUD 主题
同样在 `GameController` 的 `TowerDefenseGame` 组件里看：

- `HUD Theme`

这里现在已经承载了大部分 HUD 语义配色，比如：

- 顶部资源卡颜色
- 操作区说明颜色
- 状态消息颜色
- 正向/消耗/警告/危险提示颜色
- 拖拽提示颜色

以后如果你要统一改 HUD 风格：
优先改这里，不要先改 `TowerDefenseHudPresenter.cs`。

## 四、替换地图场景美术

### 1. 路径和战场装饰
地图里的路径、阴影、地板、场景建筑，现在大部分都已经是 Scene 里的对象。

优先改这些场景对象的：

- `SpriteRenderer`
- `Image`
- 排序层级
- 材质
- 缩放

而不是先去改脚本。

### 2. 路径/出怪口/防御点的可读性标记
这三类对象现在都有程序化占位表现：

- `EnemyPath`
- `EnemySpawnGate`
- `DefensePointFlag`

你可以先这样理解：

- 如果你还没替换正式美术
  这些脚本生成的线框和圈环很有用
- 如果你后面已经有自己的正式标记
  可以直接在对应组件里把表现参数调弱，或者关掉

重点字段大致是：

- `showReadabilityOverlay`
- `showReadabilityMarker`
- `autoCreateReadabilityRoot`
- `readabilityRootReference`
- `readabilityMaterialOverride`
- 各种颜色、宽度、半径、排序层级

如果你后面想自己接管这一层的场景层级，最推荐的方式是：

1. 先在对应对象下面自己建一个可读性根节点
2. 再把这个根节点拖到：
   - `readabilityRootReference`
3. 如果你希望这些线框统一吃你自己的材质，再把材质拖到：
   - `readabilityMaterialOverride`

这样就不必完全依赖脚本自动创建隐藏根节点了。

### 3. 放置提示表现
跟放置有关的视觉入口主要在 `GameController` 的 `TowerDefenseGame` 组件里：

- `placementRingSpriteReference`
- `validPreviewColor`
- `invalidPreviewColor`
- `placementAreaOverlayFillColor`
- `placementAreaOverlayEdgeColor`
- `starterZoneMarkerFillColor`
- `starterZoneMarkerEdgeColor`

以后如果你要把当前占位放置提示换成自己的美术：

- 优先先换 `placementRingSpriteReference`
- 再调整颜色
- 如果还不够，再继续改对应渲染器脚本

## 五、主菜单怎么换美术

在 `MainMenu` 场景里，看 `MainMenuController` 组件。

现在主菜单剩余视觉入口也已经尽量往 Inspector 收了。

重点可改内容：

- `backgroundColor`
- `primaryAccent`
- `secondaryAccent`
- `frameCoreColor`
- `frameInsetColor`
- `titleColor`
- `subtitleColor`
- `descriptionColor`
- `hintColor`
- `startButtonPrimaryTextColor`
- `startButtonSecondaryTextColor`
- `footerLeftTextColor`
- `footerRightTextColor`
- `backgroundSprite`
- `frameCoreSprite`
- `frameInsetSprite`
- `startButtonSprite`
- `titleFontAsset`
- `bodyFontAsset`
- `accentFontAsset`

文案也已经收进 Inspector：

- `titleCopy`
- `subtitleCopy`
- `descriptionCopy`
- `hintCopy`
- `startPrimaryCopy`
- `startSecondaryCopy`
- `footerLeftCopy`
- `footerRightCopy`

所以以后你如果改主菜单样式，优先在 `MainMenuController` 的 Inspector 里改，不要先回脚本里改写死值。

## 六、什么时候需要改代码
只有下面这些情况，才建议你再回脚本层：

- 你要新增一种全新的反馈类型
- 你要改反馈生成规则，而不是只换资源
- 你要把程序化占位表现彻底换成另一种实现方式
- 你要改玩法逻辑本身

如果只是下面这些情况，通常不需要改代码：

- 换塔 Sprite
- 换敌人 Sprite
- 换卡片图标
- 换 HUD 配色
- 换主菜单配色和文案
- 调整原型子节点位置
- 调整场景对象排序和装饰
- 替换运行时实体 Prefab 里的 Sprite、材质、颜色或子层级

## 七、最稳妥的实际操作顺序
以后你自己替换资源时，建议按这个顺序来：

1. 先改 `Assets/Prefabs/TowerDefense/Runtime`
   `RuntimePrototypes` 主要拿来对照层级与挂点；三种战斗塔现在已经拆成了各自独立 prefab，优先分别改它们自己的 prefab 和各自 `DefenseTower` 组件里的参数
2. 再改 `GameController` 里的 `Tower Presentation` 和 `HUD Theme`
3. 再改四张部署卡的图标和装饰
4. 再改地图里的路径、出怪口、防御点装饰
5. 最后改 `MainMenu`

## 八、改完后怎么自检
每次你替换完一批资源，建议至少做这几个检查：

1. 进 `SampleScene` 看原型对象引用有没有丢
2. 进 Play 看：
   - 塔能不能正常放
   - 敌人血条还在不在
   - 三类塔反馈还在不在
   - 继电器断电/满载颜色还正常不正常
3. 看四张部署卡：
   - 图标有没有变形
   - 文案有没有挤压重叠
4. 看主菜单：
   - 相机背景色
   - 按钮文字
   - 底图和边框
   是否仍然正常显示

## 九、一个简单原则
以后只要你在犹豫“该改 Scene / Inspector，还是该改脚本”，优先这样判断：

- 能通过场景对象、显式引用、Inspector 参数解决的，优先不要改脚本
- 只有当你要改“生成规则”或“玩法规则”时，才回脚本

这就是当前这个项目现在最重要的美术替换原则。
