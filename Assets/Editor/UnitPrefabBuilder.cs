using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UnitPrefabBuilder
{
    const string Root = "Assets/Battlefield/Units";

    struct Row
    {
        public int id, cost, hp, damage;
        public string name, key, special;
        public UnitStats.Faction faction;
        public UnitStats.Terrain terrain;
        public UnitStats.AttackType attack;
        public float speed, range, interval;

        public Row(int id, string key, string name, UnitStats.Faction faction, UnitStats.Terrain terrain,
            UnitStats.AttackType attack, int cost, int hp, int damage, float speed, float range, float interval, string special = "")
        {
            this.id = id; this.key = key; this.name = name; this.faction = faction; this.terrain = terrain;
            this.attack = attack; this.cost = cost; this.hp = hp; this.damage = damage;
            this.speed = speed; this.range = range; this.interval = interval; this.special = special;
        }
    }

    static readonly Row[] Rows =
    {
        new Row(1,"Militia","民兵",UnitStats.Faction.Player,UnitStats.Terrain.Any,UnitStats.AttackType.Melee,8,110,9,440,1000,1.4f),
        new Row(2,"Spearman","长枪兵",UnitStats.Faction.Player,UnitStats.Terrain.Any,UnitStats.AttackType.Melee,12,95,12,380,2600,1.7f,"长柄拒马：对近战单位伤害 +25%；被 500px 内贴脸攻击时承伤 +10%。"),
        new Row(3,"Archer","弓箭兵",UnitStats.Faction.Player,UnitStats.Terrain.Any,UnitStats.AttackType.Ranged,11,65,14,400,6400,1.9f,"远程脆皮：受到近战攻击时承伤 +30%。"),
        new Row(4,"Crossbowman","弩手",UnitStats.Faction.Player,UnitStats.Terrain.Dock,UnitStats.AttackType.Ranged,15,80,22,360,5200,3,"命中附加 2 秒破甲，目标承伤 +25%。"),
        new Row(5,"LightCavalry","轻骑兵",UnitStats.Faction.Player,UnitStats.Terrain.Plain,UnitStats.AttackType.Melee,18,100,18,640,1200,2,"移动超过 1500px 后首次攻击伤害 ×2，并造成击退。"),
        new Row(6,"Musketeer","火铳手",UnitStats.Faction.Player,UnitStats.Terrain.Mountain,UnitStats.AttackType.Ranged,16,55,28,340,5800,3,"山地射程额外 +800px；命中附带 8 点范围伤害。"),
        new Row(7,"ShieldAxeman","盾斧兵",UnitStats.Faction.Player,UnitStats.Terrain.River,UnitStats.AttackType.Melee,14,150,8,320,1000,2,"水域固守：承伤 -15%；20% 概率免除一次远程伤害；攻击附带破甲。"),
        new Row(8,"Bandit","流寇",UnitStats.Faction.Enemy,UnitStats.Terrain.Any,UnitStats.AttackType.Melee,8,85,11,460,1000,1),
        new Row(9,"RaiderArcher","倭寇弓手",UnitStats.Faction.Enemy,UnitStats.Terrain.Any,UnitStats.AttackType.Ranged,11,60,13,420,6200,2),
        new Row(10,"AxeRaider","刀斧手",UnitStats.Faction.Enemy,UnitStats.Terrain.Dock,UnitStats.AttackType.Melee,15,88,20,520,1100,1,"破盾突击：移速 +20%。"),
        new Row(11,"StaffFighter","长棍手",UnitStats.Faction.Enemy,UnitStats.Terrain.Plain,UnitStats.AttackType.Melee,14,90,9,500,1600,2,"阵型击退：命中有 25% 概率击退并眩晕 3 秒，附带破甲。"),
        new Row(12,"NinjaAssassin","忍者刺客",UnitStats.Faction.Enemy,UnitStats.Terrain.Mountain,UnitStats.AttackType.Melee,17,50,24,600,1000,1,"山地隐匿：隐身，首次攻击后解除。"),
        new Row(13,"HeavyArmoredSoldier","重甲甲兵",UnitStats.Faction.Enemy,UnitStats.Terrain.River,UnitStats.AttackType.Melee,16,140,15,300,1000,2,"水域重甲：远程伤害减免 35%，破甲后效果解除。")
    };

    static UnitPrefabBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode &&
                AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/01_Militia.prefab") == null)
                Build();
        };
    }

    [MenuItem("Tools/Battlefield/Rebuild Unit Prefabs")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Placeholders");
        AssetDatabase.Refresh();
        var playerSprite = Placeholder("Player", new Color32(56, 125, 210, 255));
        var enemySprite = Placeholder("Enemy", new Color32(206, 75, 63, 255));
        foreach (var row in Rows)
        {
            var root = new GameObject(row.id.ToString("00") + "_" + row.key);
            var stats = root.AddComponent<UnitStats>();
            stats.id = row.id; stats.displayName = row.name; stats.faction = row.faction;
            stats.terrain = row.terrain; stats.attackType = row.attack;
            stats.cost = row.cost; stats.maxHealth = row.hp; stats.damage = row.damage;
            stats.moveSpeed = row.speed / 100f; stats.attackRange = row.range / 100f;
            stats.attackIntervalSeconds = row.interval; stats.specialMechanic = row.special;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 4.8f, 0);
            collider.size = new Vector3(4.8f, 4.8f, 1f);
            var visual = new GameObject("Artwork Placeholder - replace Sprite");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0, 4.8f, 0);
            visual.AddComponent<SpriteRenderer>().sprite = row.faction == UnitStats.Faction.Player ? playerSprite : enemySprite;
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + root.name + ".prefab");
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SoldierPoolPrefabBuilder.Build();
        Debug.Log("Built " + Rows.Length + " unit prefabs.");
    }

    static Sprite Placeholder(string name, Color32 color)
    {
        var path = Root + "/Placeholders/" + name + ".png";
        var texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < 96; y++)
        for (int x = 0; x < 96; x++)
        {
            var dx = x - 48; var dy = y - 69;
            bool head = dx * dx + dy * dy < 12 * 12;
            bool body = x >= 31 && x <= 65 && y >= 20 && y <= 56;
            bool legs = (x >= 31 && x <= 43 || x >= 53 && x <= 65) && y >= 5 && y < 20;
            texture.SetPixel(x, y, head || body || legs ? color : clear);
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 10f;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
