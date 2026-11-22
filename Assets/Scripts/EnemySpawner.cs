using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    BattleStage stage;
    SoldierPool pool;
    float nextSpawn;

    public void Initialize(BattleStage battleStage, SoldierPool soldierPool)
    {
        stage = battleStage;
        pool = soldierPool;
        nextSpawn = Time.time + 5f;
    }

    void Update()
    {
        if (stage == null || pool == null || stage.enemyUnitIds == null || stage.enemyUnitIds.Length == 0) return;
        if (Time.time < nextSpawn) return;
        nextSpawn = Time.time + 8f;
        int activeEnemies = 0;
        foreach (var soldier in FindObjectsOfType<UnitCombat>())
        {
            var stats = soldier.GetComponent<UnitStats>();
            if (stats != null && stats.faction == UnitStats.Faction.Enemy) activeEnemies++;
        }
        if (activeEnemies >= 20) return;
        int id = stage.enemyUnitIds[Random.Range(0, stage.enemyUnitIds.Length)];
        pool.Spawn(id, new Vector3(118f, 0f, Random.Range(-24f, 24f)), Quaternion.identity);
    }
}
