# 最终人工验证清单

Updated: 2026-04-19

## 使用说明
- 这份清单给你在 Unity 里逐项人工验证当前项目状态用。
- 每一项后面都留了“结果”和“备注”，你可以直接把检查结果写进来。
- 以后你告诉我“去读这个清单”，我就直接读取这份文件判断哪里通过、哪里还没通过。

建议填写格式：

- `结果：通过`
- `结果：不通过`
- `备注：……`

---

## 一、SampleScene 静态检查

### 1. GameController Inspector
- 检查对象：`SampleScene / GameController`
- 检查点：
  - `TowerDefenseGame` Inspector 里能看到 `Placement Preview`
  - 能看到 `Tower Presentation`
  - 能看到 `HUD Theme`
  - `placementRingSpriteReference` 不是空
  - 四张按钮引用都不是空
- 结果：
- 备注：

### 2. RuntimePrototypes 根节点
- 检查对象：`SampleScene / RuntimePrototypes`
- 检查点：
  - 能看到 `RelayTowerPrototype`
  - 能看到 `DefenseTowerPrototype`
  - 能看到 `EnemyPrototype`
- 结果：
- 备注：

### 3. RelayTowerPrototype 层级与引用
- 检查对象：`SampleScene / RuntimePrototypes / RelayTowerPrototype`
- 检查点：
  - 子物体里有 `VisualRoot`
  - `RelayTower` 组件里的 `visualRootReference` 指向 `VisualRoot`
  - `bodyRendererReference` 指向 `VisualRoot` 上的 `SpriteRenderer`
- 结果：
- 备注：

### 4. DefenseTowerPrototype 层级与引用
- 检查对象：`SampleScene / RuntimePrototypes / DefenseTowerPrototype`
- 检查点：
  - 子物体里有 `FeedbackRoot`
  - 子物体里有 `TypeSignatureRoot`
  - 子物体里有 `LevelMarkerRoot`
  - `feedbackRootReference` 指向 `FeedbackRoot`
  - `typeSignatureRootReference` 指向 `TypeSignatureRoot`
  - `levelMarkerRootReference` 指向 `LevelMarkerRoot`
- 结果：
- 备注：

### 5. EnemyPrototype 层级与引用
- 检查对象：`SampleScene / RuntimePrototypes / EnemyPrototype`
- 检查点：
  - 子物体里有 `VisualScaleRoot`
  - 子物体里有 `HealthBarRoot`
  - `bodyRendererReference` 指向 `VisualScaleRoot` 上的 `SpriteRenderer`
  - `visualScaleRootReference` 指向 `VisualScaleRoot`
  - `healthBarRootReference` 指向 `HealthBarRoot`
- 结果：
- 备注：

### 6. 四张部署卡 Inspector
- 检查对象：
  - `RelayTowerButton`
  - `DefenseTowerButton`
  - `SlowFieldTowerButton`
  - `BombardTowerButton`
- 检查点：
  - 每张卡的 `TowerShopCard` 都能看到 `backgroundImageReference`
  - 每张卡的 `TowerShopCard` 都能看到 `iconImageReference`
  - 前两张卡的 `accentGraphicReferences` 不是空
- 结果：
- 备注：

---

## 二、SampleScene Play 检查

### 1. 进入 Play 后基础界面
- 检查点：
  - 顶部资源栏正常显示
  - 右侧四张部署卡正常显示
  - 左侧操作区正常显示
  - 控制台没有红色报错
- 结果：
- 备注：

### 2. 拖动继电器卡
- 检查点：
  - 拖拽预览跟手
  - 放置圆环正常显示
  - 合法区提示正常显示
  - 松手后继电器成功落地
- 结果：
- 备注：

### 3. 检查继电器实例层级
- 检查点：
  - 运行时生成的继电器实例外观在 `VisualRoot` 这层
  - 调整 `VisualRoot` 只影响继电器外观，不影响整体逻辑根
- 结果：
- 备注：

### 4. 放置战斗塔
- 检查点：
  - 战斗塔能正常放在继电器覆盖范围内
  - 运行时战斗塔实例层级中能看到：
    - `FeedbackRoot`
    - `TypeSignatureRoot`
    - `LevelMarkerRoot`
- 结果：
- 备注：

### 5. 敌人表现
- 检查点：
  - 敌人血条正常显示在身体上方
  - 敌人受击时身体反馈正常
  - 血条不会因为身体缩放反馈乱跳
- 结果：
- 备注：

### 6. 三类战斗反馈
- 检查点：
  - 单体塔有 tracer
  - 减速塔有范围脉冲
  - 炸弹塔有飞行物和爆炸
  - 运行时反馈对象正常挂在 `FeedbackRoot`
- 结果：
- 备注：

---

## 三、MainMenu 静态检查

### 1. MainMenuController Inspector
- 检查对象：`MainMenu / MainMenuController`
- 检查点：
  - 能看到 `Visual Theme`
  - 能看到 `Text Copy`
  - 可以直接修改颜色、Sprite、字体、文案
- 结果：
- 备注：

### 2. 主菜单场景对象联动
- 检查点：
  - 修改一个主题颜色后，场景里的主菜单对象能跟着变
  - 修改一段文案后，场景里的对应文字能跟着变
- 结果：
- 备注：

---

## 四、MainMenu Play 检查

### 1. 进入 Play
- 检查点：
  - 主菜单界面正常显示
  - 相机背景、按钮、标题、正文都正常
- 结果：
- 备注：

### 2. 点击开始按钮
- 检查点：
  - 能正常切到 `SampleScene`
  - 没有按钮引用丢失报错
- 结果：
- 备注：

---

## 五、控制台检查

### 1. SampleScene 控制台
- 检查点：
  - 没有 Missing Reference
  - 没有 NullReference
  - 没有脚本编译错误
- 结果：
- 备注：

### 2. MainMenu 控制台
- 检查点：
  - 没有主菜单缺引用报错
  - 没有切场景时报错
- 结果：
- 备注：

---

## 六、最终结论

### 你自己的总体判断
- 当前版本是否可以进入“继续做地图 / 继续换美术资源”的状态：
- 你的结论：
- 备注：

### 需要我继续处理的问题
- 问题 1：
- 问题 2：
- 问题 3：
