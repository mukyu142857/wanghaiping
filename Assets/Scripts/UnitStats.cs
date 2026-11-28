using UnityEngine;

public sealed class UnitStats : MonoBehaviour
{
    public enum Faction { Player, Enemy }
    public enum Terrain { Any, Dock, Plain, Mountain, River }
    public enum AttackType { Melee, Ranged }

    public int id;
    public string displayName;
    public Faction faction;
    public Terrain terrain;
    public AttackType attackType;
    [Min(0)] public int cost;
    [Min(1)] public int maxHealth;
    [Min(0)] public int damage;
    [Min(0)] public float moveSpeed;
    [Min(0)] public float attackRange;
    [Min(0.01f)] public float attackIntervalSeconds;
    [TextArea(2, 4)] public string specialMechanic;

    // Values above use Unity units: 100 pixels = 1 unit.
}
