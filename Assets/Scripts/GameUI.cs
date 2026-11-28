using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class GameUI : MonoBehaviour
{
    [SerializeField] bool battleMode;
    const string SceneRoot = "Assets/Battlefield/Scenes/";
    static readonly string[] StageKeys = { "Mountain", "Plain", "Dock", "River" };
    static readonly string[] StageNames = { "山地", "平原", "海边", "河流" };
    readonly Color ink = new Color(.11f,.17f,.20f);
    readonly Color paper = new Color(.91f,.92f,.90f);
    readonly Color card = new Color(.77f,.82f,.82f);
    readonly Color accent = new Color(.23f,.40f,.47f);
    Font font;
    Canvas canvas;
    GameObject menu, select, intro, battle, pause, result, roundComplete;
    Text introTitle, introBody, resultTitle, resultBody, costText;
    Text roundText;
    Button[] unitButtons = new Button[4];
    int stageIndex;
    BattleStage stage;
    BattleCost cost;
    SoldierPool pool;
    BattleRoundManager roundManager;
    string order = "防御";

    void Awake()
    {
        if (Application.isPlaying) RebuildUi();
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += EnsureEditorPreview;
    }

    void EnsureEditorPreview()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid()) return;
        if (transform.Find("UI Canvas") == null)
        {
            BuildEditorPreview();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }

    public void BuildEditorPreview()
    {
        if (!Application.isPlaying) RebuildUi();
    }
#endif

    void RebuildUi()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name == "UI Canvas") DestroyImmediate(transform.GetChild(i).gameObject);
        menu = select = intro = battle = pause = result = roundComplete = null;
        font = Application.isPlaying
            ? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 26)
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (Application.isPlaying && FindObjectOfType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(events);
        }
        BuildCanvas();
        if (battleMode)
        {
            stage = FindInOwnScene<BattleStage>();
            cost = FindInOwnScene<BattleCost>();
            if (cost != null && Application.isPlaying) cost.SetHudVisible(false);
            pool = FindInOwnScene<SoldierPool>();
            if (pool == null && Application.isPlaying)
            {
                var prefab = Resources.Load<GameObject>("SoldierPool");
                if (prefab != null) pool = Instantiate(prefab).GetComponent<SoldierPool>();
            }
            if (Application.isPlaying && stage != null && pool != null && cost != null)
            {
                var previewUnits = stage.transform.Find("Unit Lineup Preview");
                if (previewUnits != null) previewUnits.gameObject.SetActive(false);
                var spawner = GetComponent<EnemySpawner>();
                if (spawner == null) spawner = gameObject.AddComponent<EnemySpawner>();
                roundManager = GetComponent<BattleRoundManager>();
                if (roundManager == null) roundManager = gameObject.AddComponent<BattleRoundManager>();
                roundManager.Initialize(stage, cost, spawner, this);
            }
            BuildBattle();
            Show(battle);
        }
        else
        {
            BuildMenu(); BuildSelect(); BuildIntro();
            Show(menu);
        }
    }

    T FindInOwnScene<T>() where T : Component
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    void Update()
    {
        if (!battleMode || battle == null || !battle.activeSelf) return;
        if (costText != null && cost != null && cost.Ledger != null)
            costText.text = $"费用 {cost.Ledger.Current}  /  总回复 {cost.Ledger.Generated}/{cost.Ledger.TotalSupply}";
        if (roundText != null && roundManager != null)
            roundText.text = $"第 {roundManager.Round}/4 回合  {roundManager.Goal}{roundManager.ObjectiveStatus}  堡垒 {roundManager.PlayerFortressHealth}";
        if (stage == null || cost == null || cost.Ledger == null) return;
        for (int i = 0; i < unitButtons.Length; i++)
        {
            int id = stage.playerUnitIds != null && i < stage.playerUnitIds.Length ? stage.playerUnitIds[i] : 0;
            var stats = FindUnit(id);
            unitButtons[i].interactable = stats != null && cost.CanAfford(stats.cost);
        }
    }

    void BuildCanvas()
    {
        var root = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
    }

    GameObject Screen(string name, bool dimWorld = true)
    {
        var screen = Panel(name, canvas.transform, new Vector2(1920,1080), Vector2.zero,
            dimWorld ? paper : new Color(0,0,0,0));
        screen.SetActive(false);
        return screen;
    }

    void BuildMenu()
    {
        menu = Screen("主菜单");
        Label(menu.transform, "守望", new Vector2(850,310), new Vector2(-430,260), 100);
        Button(menu.transform, "开始战役", new Vector2(720,200), new Vector2(430,110), () => Show(select), 58);
        Button(menu.transform, "选关", new Vector2(350,200), new Vector2(240,-135), () => Show(select), 48);
        Button(menu.transform, "退出", new Vector2(350,200), new Vector2(620,-135), Application.Quit, 48);
    }

    void BuildSelect()
    {
        select = Screen("选关界面");
        Button(select.transform, "↶", new Vector2(100,100), new Vector2(-855,460), () => Show(menu), 52);
        Label(select.transform, "选择地形", new Vector2(750,130), new Vector2(0,420), 66);
        var positions = new[] { new Vector2(-450,170), new Vector2(450,170), new Vector2(-450,-160), new Vector2(450,-160) };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Button(select.transform, StageNames[i], new Vector2(830,290), positions[i], () => OpenIntro(index), 70);
        }
    }

    void BuildIntro()
    {
        intro = Screen("关卡介绍弹窗");
        Panel("介绍图片区", intro.transform, new Vector2(820,975), new Vector2(-510,0), card);
        Label(intro.transform, "关卡插画待添加", new Vector2(760,100), new Vector2(-510,0), 38);
        Panel("介绍文字区", intro.transform, new Vector2(775,600), new Vector2(465,65), card);
        introTitle = Label(intro.transform, "", new Vector2(700,120), new Vector2(465,230), 58);
        introBody = Label(intro.transform, "", new Vector2(700,320), new Vector2(465,35), 34);
        Button(intro.transform, "开始", new Vector2(540,100), new Vector2(465,-350), StartBattle, 42);
        Button(intro.transform, "×", new Vector2(100,100), new Vector2(850,450), () => Show(select), 60);
    }

    void OpenIntro(int selectedStageIndex)
    {
        stageIndex = selectedStageIndex;
        introTitle.text = StageNames[stageIndex];
        introBody.text = "战场：" + StageNames[stageIndex] + "\n\n我方可使用通用兵种和该地形专属兵种。\n\n关卡内容与背景插画待完善。";
        Show(intro);
    }

    void StartBattle() => SceneManager.LoadScene(StageKeys[stageIndex]);

    void BuildBattle()
    {
        battle = Screen("战斗界面", false);
        Button(battle.transform, "Ⅱ", new Vector2(100,100), new Vector2(-855,460), OpenPause, 52);
        costText = Label(battle.transform, "费用 0 / 总回复 0/100", new Vector2(600,90), new Vector2(-340,460), 36);
        roundText = Label(battle.transform, "第 1/4 回合  防守", new Vector2(860,90), new Vector2(370,460), 30);
        Panel("底部操作区", battle.transform, new Vector2(1920,330), new Vector2(0,-375), new Color(.12f,.19f,.23f,.88f));
        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            int id = stage != null && stage.playerUnitIds != null && i < stage.playerUnitIds.Length ? stage.playerUnitIds[i] : 0;
            var stats = FindUnit(id);
            string label = stats != null ? stats.displayName + "\n" + stats.cost + "费" : "兵种 " + (i+1);
            unitButtons[i] = Button(battle.transform, label, new Vector2(300,235), new Vector2(-680 + i*315,-375), () => SpawnUnit(slot), 32, true);
        }
        Button(battle.transform, "进攻", new Vector2(400,110), new Vector2(690,-260), () => SetOrder("进攻"), 36);
        Button(battle.transform, "撤退", new Vector2(400,110), new Vector2(690,-380), () => SetOrder("撤退"), 36);
        Button(battle.transform, "防御", new Vector2(400,110), new Vector2(690,-500), () => SetOrder("防御"), 36);
        BuildPause(); BuildResult(); BuildRoundComplete();
    }

    void SpawnUnit(int slot)
    {
        if (stage == null || pool == null || cost == null || stage.playerUnitIds == null || slot >= stage.playerUnitIds.Length) return;
        int id = stage.playerUnitIds[slot];
        var stats = FindUnit(id);
        if (stats == null || !cost.CanAfford(stats.cost)) return;
        var position = new Vector3(-118f, 0f, Random.Range(-24f, 24f));
        var soldier = pool.Spawn(id, position, Quaternion.identity);
        if (soldier != null)
        {
            soldier.GetComponent<UnitCombat>().SetOrder(order == "进攻" ? UnitCombat.Order.Attack :
                order == "撤退" ? UnitCombat.Order.Retreat : UnitCombat.Order.Defense);
            cost.TrySpend(stats.cost);
        }
    }

    UnitStats FindUnit(int id)
    {
        return id > 0 && pool != null ? pool.GetUnitStats(id) : null;
    }

    void SetOrder(string next)
    {
        order = next;
        var command = next == "进攻" ? UnitCombat.Order.Attack :
            next == "撤退" ? UnitCombat.Order.Retreat : UnitCombat.Order.Defense;
        foreach (var soldier in FindObjectsOfType<UnitCombat>())
            if (soldier.IsPlayer && soldier.IsAlive) soldier.SetOrder(command);
    }

    public void ResetOrder() => order = "防御";

    void BuildPause()
    {
        pause = Screen("暂停菜单", false);
        Panel("遮罩", pause.transform, new Vector2(1920,1080), Vector2.zero, new Color(0,0,0,.68f));
        Button(pause.transform, "继续", new Vector2(800,170), new Vector2(0,220), Resume, 54);
        Button(pause.transform, "重新开始", new Vector2(800,170), Vector2.zero, Restart, 54);
        Button(pause.transform, "退出", new Vector2(800,170), new Vector2(0,-220), ExitBattle, 54);
        pause.SetActive(false);
    }

    void BuildResult()
    {
        result = Screen("结算界面");
        resultTitle = Label(result.transform, "胜利", new Vector2(1050,320), new Vector2(-260,190), 100);
        resultBody = Label(result.transform, "", new Vector2(1200,200), new Vector2(-180,-130), 42);
        Button(result.transform, "退出", new Vector2(300,110), new Vector2(360,-350), ExitBattle, 40);
        Button(result.transform, "重开", new Vector2(300,110), new Vector2(700,-350), Restart, 40);
        result.SetActive(false);
    }

    void BuildRoundComplete()
    {
        roundComplete = Screen("回合结束占位界面");
        Label(roundComplete.transform, "本回合目标完成", new Vector2(1200,180), new Vector2(0,200), 70);
        Label(roundComplete.transform, "回合之间的内容待添加；场上我方士兵会保留。",
            new Vector2(1400,150), new Vector2(0,10), 38);
        Button(roundComplete.transform, "临时继续下一回合", new Vector2(800,140),
            new Vector2(0,-240), ContinueRound, 44);
        roundComplete.SetActive(false);
    }

    public void ShowRoundComplete(int completedRound)
    {
        Time.timeScale = 0f;
        Show(roundComplete);
    }

    void ContinueRound()
    {
        Time.timeScale = 1f;
        roundManager.AdvanceRound();
        Show(battle);
    }

    public void ShowResult(bool victory, string message)
    {
        Time.timeScale = 0;
        resultTitle.text = victory ? "胜利" : "失败";
        resultBody.text = message;
        Show(result);
    }

    void OpenPause() { Time.timeScale = 0; Show(pause); }
    void Resume() { Time.timeScale = 1; Show(battle); }
    void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void ExitBattle() { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }

    void Show(GameObject target)
    {
        foreach (var screen in new[] { menu, select, intro, battle, pause, result, roundComplete })
            if (screen != null) screen.SetActive(screen == target);
    }

    GameObject Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return go;
    }

    Text Label(Transform parent, string value, Vector2 size, Vector2 position, int fontSize)
    {
        var go = new GameObject("Text " + value, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = go.GetComponent<Text>();
        text.font = font; text.text = value; text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter; text.color = ink;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    Button Button(Transform parent, string value, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action, int fontSize, bool hex = false)
    {
        var go = Panel("Button " + value, parent, size, position, hex ? Color.white : card);
        var image = go.GetComponent<Image>();
        if (hex) { image.sprite = HexSprite(); image.type = Image.Type.Simple; image.color = accent; }
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(action);
        Label(go.transform, value, size, Vector2.zero, fontSize);
        return button;
    }

    static Sprite hexSprite;
    static Sprite HexSprite()
    {
        if (hexSprite != null) return hexSprite;
        var texture = new Texture2D(128,100,TextureFormat.RGBA32,false);
        for (int y=0;y<100;y++) for(int x=0;x<128;x++)
        {
            float edge = y < 50 ? 32f - y*.64f : 32f - (99-y)*.64f;
            texture.SetPixel(x,y, x >= edge && x < 128-edge ? Color.white : Color.clear);
        }
        texture.Apply();
        hexSprite = Sprite.Create(texture,new Rect(0,0,128,100),new Vector2(.5f,.5f));
        return hexSprite;
    }
}
