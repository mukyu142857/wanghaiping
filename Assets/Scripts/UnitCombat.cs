using UnityEngine;

[RequireComponent(typeof(UnitStats))]
public sealed class UnitCombat : MonoBehaviour
{
    UnitStats stats;
    PooledSoldier pooled;
    int health;
    float nextAttack;
    float nextSearch;
    UnitCombat target;

    void Awake()
    {
        stats = GetComponent<UnitStats>();
        pooled = GetComponent<PooledSoldier>();
    }

    void OnEnable()
    {
        if (stats == null) stats = GetComponent<UnitStats>();
        health = stats.maxHealth;
        nextAttack = 0f;
        nextSearch = 0f;
        target = null;
    }

    void Update()
    {
        if (Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.2f;
            target = FindTarget();
        }

        if (target != null && target.isActiveAndEnabled)
        {
            var delta = target.transform.position - transform.position;
            delta.y = 0;
            if (delta.magnitude > stats.attackRange)
                Move(delta.normalized);
            else if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + stats.attackIntervalSeconds;
                target.TakeDamage(stats.damage);
            }
        }
        else
        {
            float direction = stats.faction == UnitStats.Faction.Player ? 1f : -1f;
            if (Mathf.Abs(transform.position.x) < 130f || Mathf.Sign(transform.position.x) != direction)
                Move(new Vector3(direction, 0, 0));
        }
    }

    UnitCombat FindTarget()
    {
        UnitCombat closest = null;
        float best = Mathf.Max(stats.attackRange + 8f, 28f);
        foreach (var enemy in FindObjectsOfType<UnitCombat>())
        {
            if (enemy == this || !enemy.isActiveAndEnabled || enemy.stats.faction == stats.faction) continue;
            var offset = enemy.transform.position - transform.position;
            offset.y = 0;
            float distance = offset.magnitude;
            if (distance < best) { best = distance; closest = enemy; }
        }
        return closest;
    }

    void Move(Vector3 direction)
    {
        var position = transform.position + direction * stats.moveSpeed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, -132f, 132f);
        position.z = Mathf.Clamp(position.z, -29f, 29f);
        transform.position = position;
    }

    public void TakeDamage(int damage)
    {
        if (health <= 0) return;
        health -= Mathf.Max(0, damage);
        if (health > 0) return;
        if (pooled == null) pooled = GetComponent<PooledSoldier>();
        if (pooled != null && pooled.Owner != null) pooled.Owner.Despawn(gameObject);
        else Destroy(gameObject);
    }
}
