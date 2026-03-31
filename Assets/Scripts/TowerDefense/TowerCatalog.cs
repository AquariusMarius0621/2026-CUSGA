using UnityEngine;

/// <summary>
/// TowerDefinition 表示“一种塔的静态说明书”。
///
/// 这里刻意只放“相对稳定、不会随运行时瞬时变化的数据”，例如：
/// - 给玩家看的展示名
/// - 建造消耗
/// - 占地半径
/// - HUD / 部署卡使用的强调色与职能描述
///
/// 这样做的核心目的是把“塔是什么”与“当前这局游戏里发生了什么”拆开：
/// - `TowerDefenseGame` 继续处理资源、放置、波次、胜负状态这些运行时编排
/// - `TowerDefinition` 只回答某种塔的固定配置是什么
///
/// 对原型项目来说，这种拆分已经足够带来结构收益，
/// 但又不会把我们带进过早的 ScriptableObject 资产化或复杂配置系统。
/// </summary>
public sealed class TowerDefinition
{
    /// <summary>
    /// 用构造函数一次性写死静态信息，
    /// 是为了强调这份数据在运行期应当被当作只读配置看待。
    /// </summary>
    public TowerDefinition(
        TowerType towerType,
        string displayName,
        int buildCost,
        float placementRadius,
        float expansionSquareSize,
        string cardRoleSummary,
        Color accentColor)
    {
        TowerType = towerType;
        DisplayName = displayName;
        BuildCost = buildCost;
        PlacementRadius = placementRadius;
        ExpansionSquareSize = expansionSquareSize;
        CardRoleSummary = cardRoleSummary;
        AccentColor = accentColor;
    }

    /// <summary>
    /// 这份定义对应哪一种塔。
    /// </summary>
    public TowerType TowerType { get; }

    /// <summary>
    /// 给 HUD、提示文案、部署成功消息使用的玩家可读名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 建造这类塔需要消耗多少电量。
    /// </summary>
    public int BuildCost { get; }

    /// <summary>
    /// 这类塔用于占地判定的半径。
    ///
    /// 注意这里说的是“建造判定半径”，
    /// 不是攻击范围，也不是碰撞物理半径。
    /// </summary>
    public float PlacementRadius { get; }

    /// <summary>
    /// 这类塔在“部署网络扩张”里提供的方形范围边长。
    ///
    /// 这次玩法改动后，塔不只是一个占地物件，
    /// 它还会把后续允许放塔的范围继续向外扩张。
    ///
    /// 这里直接记录“方形边长”，是为了让规则读起来更贴近设计语言：
    /// 讨论时我们说的是“这座塔能往外扩多大一格”，
    /// 而不是再把方形规则绕回圆形半径。
    /// </summary>
    public float ExpansionSquareSize { get; }

    /// <summary>
    /// 部署卡第二行的功能摘要文案。
    /// </summary>
    public string CardRoleSummary { get; }

    /// <summary>
    /// 这类塔在 HUD / 部署卡里使用的强调色。
    /// </summary>
    public Color AccentColor { get; }

    /// <summary>
    /// 生成部署卡的多行富文本。
    ///
    /// 把这段格式化逻辑放在定义对象里，
    /// 是为了让“卡片长什么样”跟着“塔定义”一起走，
    /// 避免 HUD 层又重新复制一套关于塔文案的分支逻辑。
    /// </summary>
    public string BuildCardLabelMarkup()
    {
        string accentHex = ColorUtility.ToHtmlStringRGB(AccentColor);
        return
            $"{DisplayName.ToUpperInvariant()}\n" +
            $"<size=20><color=#9FB4C8>{CardRoleSummary} • GRID {ExpansionSquareSize:0.0}</color></size>\n" +
            $"<size=32><color=#{accentHex}>{BuildCost} EN</color></size>";
    }
}

/// <summary>
/// TowerCatalog 是当前原型里“塔静态定义的统一入口”。
///
/// 为什么这里不用更重的字典资产或配置表？
/// - 当前只有两种塔
/// - 原型还处在快速迭代期
/// - 我们需要的是“把分散的 switch 收口”，而不是引入更重的系统
///
/// 所以这里选择一个非常轻的目录对象：
/// - 让总控能统一查成本、展示名、占地半径
/// - 给 HUD 一份稳定的塔展示信息来源
/// - 为后面继续拆 `Placement / HUD / Economy` 打基础
/// </summary>
public sealed class TowerCatalog
{
    private readonly TowerDefinition _relayDefinition;
    private readonly TowerDefinition _defenseDefinition;

    public TowerCatalog(TowerDefinition relayDefinition, TowerDefinition defenseDefinition)
    {
        _relayDefinition = relayDefinition;
        _defenseDefinition = defenseDefinition;
    }

    /// <summary>
    /// 尝试获取指定塔类型的定义。
    ///
    /// 用 Try 风格而不是直接抛异常，
    /// 是因为当前原型里 `TowerType.None` 本来就是一个合法中间态。
    /// </summary>
    public bool TryGetDefinition(TowerType towerType, out TowerDefinition definition)
    {
        switch (towerType)
        {
            case TowerType.Relay:
                definition = _relayDefinition;
                return true;
            case TowerType.Defense:
                definition = _defenseDefinition;
                return true;
            default:
                definition = null;
                return false;
        }
    }

    /// <summary>
    /// 获取定义；若没有则返回 null。
    ///
    /// 保留这个便捷入口，是为了让调用侧在“预期应该存在定义”的场景里
    /// 写起来更直观一些。
    /// </summary>
    public TowerDefinition GetDefinition(TowerType towerType)
    {
        TryGetDefinition(towerType, out TowerDefinition definition);
        return definition;
    }
}