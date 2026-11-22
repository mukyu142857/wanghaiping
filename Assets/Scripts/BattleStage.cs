using UnityEngine;

public sealed class BattleStage : MonoBehaviour
{
    public enum Terrain { Dock, Plain, Mountain, River }

    public Terrain terrain;
    public string displayName;
    [Tooltip("UnitStats.id values available to the player in this stage.")]
    public int[] playerUnitIds;
    [Tooltip("UnitStats.id values available to the enemy in this stage.")]
    public int[] enemyUnitIds;
    [Tooltip("Assign future 2D background artwork here. Empty for the placeholder stage.")]
    public SpriteRenderer backgroundArtwork;
}
