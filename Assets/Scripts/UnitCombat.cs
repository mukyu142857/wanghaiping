using UnityEngine;

[RequireComponent(typeof(UnitStats))]
public sealed class UnitCombat : MonoBehaviour
{
    public enum Order { Defense, Attack, Retreat }
    UnitStats stats;
    PooledSoldier pooled;
    int health;
    float nextAttack;
    float nextSearch;
    UnitCombat target;
    BattleTarget structureTarget;
    BattleTerrain? terrain;
    Order order;
    float readyAt, stunnedUntil, armorBrokenUntil, distanceMoved;
    bool hidden, chargeUsed;

    public bool IsAlive => isActiveAndEnabled && health > 0;
    public bool IsPlayer => stats != null && stats.faction == UnitStats.Faction.Player;
    public bool IsHidden => hidden && IsAlive;

    public void SetOrder(Order next)
    {
        if (!IsPlayer) return;
        order = next;
        target = null;
        structureTarget = null;
    }

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
        structureTarget = null;
        order = IsPlayer ? Order.Defense : Order.Attack;
        readyAt = Time.time + 1.5f;
        stunnedUntil = armorBrokenUntil = distanceMoved = 0f;
        hidden = stats.id == 12;
        chargeUsed = false;
        var stage = FindObjectOfType<BattleStage>();
        terrain = stage != null ? (BattleTerrain)stage.terrain : (BattleTerrain?)null;
    }

    void Update()
    {
        if (health <= 0 || Time.time < readyAt || Time.time < stunnedUntil) return;
        if (IsPlayer && order == Order.Retreat)
        {
            if (transform.position.x > -118f) Move(Vector3.left);
            return;
        }
        if (Time.time >= nextSearch)
        {
            nextSearch = Time.time + 0.2f;
            target = FindTarget();
        }

        if (target != null && target.isActiveAndEnabled)
        {
            var delta = target.transform.position - transform.position;
            delta.y = 0;
            float range = AttackRange();
            if (delta.magnitude > range)
            {
                if (order == Order.Attack) Move(delta.normalized);
            }
            else if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + stats.attackIntervalSeconds;
                Attack(target);
            }
        }
        else if (order == Order.Attack && structureTarget != null && structureTarget.Health > 0)
        {
            var delta = structureTarget.transform.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude > AttackRange()) Move(delta.normalized);
            else if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + stats.attackIntervalSeconds;
                float damage = stats.damage;
                if (terrain.HasValue) damage = BattleModifiers.OutgoingDamage(damage,
                    stats.attackType == UnitStats.AttackType.Ranged, terrain.Value);
                bool charged = stats.id == 5 && !chargeUsed && distanceMoved >= 15f;
                if (charged) damage *= 2f;
                structureTarget.TakeDamage(Mathf.RoundToInt(damage));
                if (charged) chargeUsed = true;
                hidden = false;
            }
        }
        else if (order == Order.Attack)
        {
            float direction = stats.faction == UnitStats.Faction.Player ? 1f : -1f;
            if (Mathf.Abs(transform.position.x) < 130f || Mathf.Sign(transform.position.x) != direction)
                Move(new Vector3(direction, 0, 0));
        }
    }

    UnitCombat FindTarget()
    {
        UnitCombat closest = null;
        structureTarget = null;
        float range = AttackRange();
        float best = order == Order.Defense ? range : Mathf.Max(range + 8f, 28f);
        foreach (var enemy in FindObjectsOfType<UnitCombat>())
        {
            if (enemy == this || !enemy.IsAlive || enemy.IsHidden || enemy.stats.faction == stats.faction) continue;
            var offset = enemy.transform.position - transform.position;
            offset.y = 0;
            float distance = offset.magnitude;
            if (distance < best) { best = distance; closest = enemy; }
        }
        if (order == Order.Attack)
            foreach (var structure in FindObjectsOfType<BattleTarget>())
            {
                if (structure.Health <= 0 || structure.playerSide == IsPlayer || IsPlayer && !structure.isObjective) continue;
                var offset = structure.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance < best) { best = distance; closest = null; structureTarget = structure; }
            }
        return closest;
    }

    float AttackRange()
    {
        float range = stats.attackRange + (stats.id == 6 && terrain == BattleTerrain.Mountain ? 8f : 0f);
        return terrain.HasValue
            ? BattleModifiers.AttackRange(range, stats.attackType == UnitStats.AttackType.Ranged, terrain.Value)
            : range;
    }

    void Attack(UnitCombat victim)
    {
        float damage = stats.damage;
        if (stats.id == 2 && victim.stats.attackType == UnitStats.AttackType.Melee) damage *= 1.25f;
        if (terrain.HasValue) damage = BattleModifiers.OutgoingDamage(damage, stats.attackType == UnitStats.AttackType.Ranged, terrain.Value);
        bool charged = stats.id == 5 && !chargeUsed && distanceMoved >= 15f;
        if (charged) damage *= 2f;
        victim.TakeDamage(Mathf.RoundToInt(damage), stats.attackType, transform.position);
        if (victim.IsAlive)
        {
            if (stats.id == 4 || stats.id == 7 || stats.id == 11) victim.ApplyArmorBreak();
            if (charged || stats.id == 11 && Random.value < .25f)
            {
                victim.KnockBack(transform.position, 1.5f);
                victim.Stun(3f);
            }
        }
        if (stats.id == 6)
            foreach (var nearby in FindObjectsOfType<UnitCombat>())
                if (nearby != victim && nearby.IsAlive && nearby.IsPlayer != IsPlayer &&
                    Vector3.Distance(nearby.transform.position, victim.transform.position) <= 3f)
                    nearby.TakeDamage(8, UnitStats.AttackType.Ranged, transform.position);
        if (charged) chargeUsed = true;
        hidden = false;
    }

    void Move(Vector3 direction)
    {
        float speed = stats.moveSpeed * (stats.id == 10 ? 1.2f : 1f);
        if (terrain.HasValue) speed = BattleModifiers.MoveSpeed(speed, terrain.Value);
        var old = transform.position;
        var position = transform.position + direction * speed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, -132f, 132f);
        position.z = Mathf.Clamp(position.z, -29f, 29f);
        transform.position = position;
        distanceMoved += Vector3.Distance(old, position);
    }

    public void ApplyArmorBreak() => armorBrokenUntil = Mathf.Max(armorBrokenUntil, Time.time + 2f);
    public void Stun(float seconds) => stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);

    public void KnockBack(Vector3 attacker, float distance)
    {
        float direction = Mathf.Sign(transform.position.x - attacker.x);
        if (direction == 0f) direction = IsPlayer ? -1f : 1f;
        var position = transform.position;
        position.x = Mathf.Clamp(position.x + direction * distance, -132f, 132f);
        transform.position = position;
    }

    public void TakeDamage(int damage, UnitStats.AttackType attackType = UnitStats.AttackType.Melee,
        Vector3 attacker = default(Vector3))
    {
        if (health <= 0) return;
        bool ranged = attackType == UnitStats.AttackType.Ranged;
        if (stats.id == 7 && ranged && Random.value < .2f) return;
        float actual = terrain.HasValue
            ? BattleModifiers.IncomingDamage(damage, ranged, terrain.Value)
            : damage;
        if (stats.id == 2 && Vector3.Distance(attacker, transform.position) <= 5f) actual *= 1.1f;
        if (stats.id == 3 && !ranged) actual *= 1.3f;
        if (stats.id == 7) actual *= .85f;
        if (stats.id == 13 && ranged && Time.time >= armorBrokenUntil) actual *= .65f;
        if (Time.time < armorBrokenUntil) actual *= 1.25f;
        health -= Mathf.Max(0, Mathf.RoundToInt(actual));
        if (health > 0) return;
        if (pooled == null) pooled = GetComponent<PooledSoldier>();
        if (pooled != null && pooled.Owner != null) pooled.Owner.Despawn(gameObject);
        else Destroy(gameObject);
    }
}
