public enum BattleTerrain { Dock, Plain, Mountain, River }

public static class BattleModifiers
{
    public static float MoveSpeed(float value, BattleTerrain terrain)
        => terrain == BattleTerrain.Plain ? value * 1.1f : value;

    public static float AttackRange(float value, bool ranged, BattleTerrain terrain)
        => ranged && terrain == BattleTerrain.Mountain ? value * 1.15f : value;

    public static float OutgoingDamage(float value, bool ranged, BattleTerrain terrain)
        => ranged && terrain == BattleTerrain.Dock ? value * 1.15f : value;

    public static float IncomingDamage(float value, bool ranged, BattleTerrain terrain)
        => ranged && terrain == BattleTerrain.River ? value * 0.85f : value;
}
