using UnityEngine;

public sealed class FortressData : MonoBehaviour
{
    public enum Side { Left, Right }

    public Side side;
    [Min(1)] public int maxHealth = 1200;
    [Min(0)] public float attackRange = 56f;
    [Min(0)] public int attackDamage = 18;
    [Min(0.01f)] public float attackIntervalSeconds = 1f;
    public bool playerControllable = false;
    public bool armorBreakable = false;
    public bool automaticallyCounterattacks = true;
}
