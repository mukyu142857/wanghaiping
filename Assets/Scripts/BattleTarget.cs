using UnityEngine;

public sealed class BattleTarget : MonoBehaviour
{
    public bool playerSide;
    public bool isObjective;
    public int maxHealth = 1200;
    public int Health { get; private set; }
    FortressData fortress;
    float nextAttack;

    void Awake()
    {
        fortress = GetComponent<FortressData>();
        if (fortress != null)
        {
            playerSide = fortress.side == FortressData.Side.Left;
            maxHealth = fortress.maxHealth;
        }
        Health = maxHealth;
    }

    public void ConfigureObjective(int health)
    {
        playerSide = false;
        isObjective = true;
        maxHealth = health;
        Health = health;
    }

    void Update()
    {
        if (fortress == null || Health <= 0 || !fortress.automaticallyCounterattacks || Time.time < nextAttack) return;
        UnitCombat nearest = null;
        float distance = fortress.attackRange;
        foreach (var unit in FindObjectsOfType<UnitCombat>())
        {
            if (!unit.IsAlive || unit.IsPlayer == playerSide || unit.IsHidden) continue;
            float current = Vector3.Distance(transform.position, unit.transform.position);
            if (current >= distance) continue;
            distance = current;
            nearest = unit;
        }
        if (nearest == null) return;
        nextAttack = Time.time + fortress.attackIntervalSeconds;
        nearest.TakeDamage(fortress.attackDamage, UnitStats.AttackType.Ranged, transform.position);
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0) return;
        Health = Mathf.Max(0, Health - Mathf.Max(0, damage));
        if (Health > 0) return;
        if (isObjective) Destroy(gameObject);
    }
}
