/// <summary>
/// Small helper methods for buildable type classification.
/// Keeping these checks in one place avoids spreading fragile repeated switch logic.
/// </summary>
public static class TowerTypeUtility
{
    public static bool IsRelay(TowerType towerType)
    {
        return towerType == TowerType.Relay;
    }

    public static bool IsCombatTower(TowerType towerType)
    {
        return towerType == TowerType.SingleTarget
            || towerType == TowerType.SlowField
            || towerType == TowerType.Bombard;
    }
}
