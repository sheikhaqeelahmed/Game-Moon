using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.Player;
using PlanetScrappers.Vehicles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PlanetScrappers.UI
{
    public enum GameScreen { None, MainMenu, Inventory, Depot, Shop, Missions, Pause, Settings, Credits, Achievements, Ending }

    /// <summary>
    /// Builds and runs all UI: the gameplay HUD plus every menu screen. Opening a screen pauses
    /// the game and frees the mouse; closing it resumes play.
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] PlayerController player;
        [SerializeField] ToolUser toolUser;
        [SerializeField] PlayerInteractor interactor;

        RectTransform hudRoot, screenRoot;
        CanvasScaler scaler;
        GameScreen achievementsReturn = GameScreen.Pause;
        InputAction escape, inventoryKey, missionsKey;
        GameScreen current = GameScreen.None;
        GameScreen settingsReturn = GameScreen.Pause;

        // HUD widgets
        Image healthFill, oxygenFill, backpackFill, targetFill, crosshair;
        Text healthValue, oxygenValue, creditsText, gainText, missionHead, missionObjective, missionProgress;
        Text targetName, targetInfo, promptText, botText, backpackText, backpackCounts, toastText;
        GameObject targetPanel, promptPanel, botPanel, toastPanel, gainPanel, missionPanel, truckPanel;
        Text truckText;
        const int SlotCount = 5;
        readonly Image[] slotBg = new Image[SlotCount];
        readonly Text[] slotText = new Text[SlotCount];
        float botTimer, toastTimer, gainTimer;

        public GameScreen Current => current;

        void Awake()
        {
            Instance = this;
            UIKit.Init();
            BuildCanvas();
            BuildHud();

            escape = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");
            escape.AddBinding("<Gamepad>/start");
            inventoryKey = new InputAction("Inventory", InputActionType.Button, "<Keyboard>/tab");
            inventoryKey.AddBinding("<Keyboard>/i");
            inventoryKey.AddBinding("<Gamepad>/select");
            missionsKey = new InputAction("Missions", InputActionType.Button, "<Keyboard>/j");
            missionsKey.AddBinding("<Gamepad>/dpad/up");
        }

        void OnEnable()
        {
            escape.Enable(); inventoryKey.Enable(); missionsKey.Enable();
            GameEvents.BotSays += OnBotSays;
            GameEvents.CreditsGained += OnCreditsGained;
            GameEvents.MissionCompleted += OnMissionCompleted;
        }

        void OnDisable()
        {
            escape.Disable(); inventoryKey.Disable(); missionsKey.Disable();
            GameEvents.BotSays -= OnBotSays;
            GameEvents.CreditsGained -= OnCreditsGained;
            GameEvents.MissionCompleted -= OnMissionCompleted;
            if (AchievementManager.Instance != null) AchievementManager.Instance.Unlocked -= OnAchievement;
        }

        void OnDestroy()
        {
            escape.Dispose(); inventoryKey.Dispose(); missionsKey.Dispose();
        }

        void Start()
        {
            if (AchievementManager.Instance != null) AchievementManager.Instance.Unlocked += OnAchievement;
        }

        void OnAchievement(AchievementManager.Definition d) => Toast($"Achievement unlocked: {d.title}", 4.5f);

        void OnMissionCompleted(PlanetScrappers.Missions.MissionDefinition m)
        {
            var list = GameDatabase.Instance.missions;
            if (list.Length > 0 && m == list[list.Length - 1]) StartCoroutine(ShowEndingLater());
        }

        System.Collections.IEnumerator ShowEndingLater()
        {
            yield return new WaitForSecondsRealtime(6f);
            Open(GameScreen.Ending);
        }

        /// <summary>Text size option: 1 = default. Larger values make every panel bigger.</summary>
        public void SetUiScale(float scale)
        {
            if (scaler == null) return;
            scale = Mathf.Clamp(scale, 0.8f, 1.5f);
            scaler.referenceResolution = new Vector2(1920f, 1080f) / scale;
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("UI Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            hudRoot = UIKit.Node("HUD", canvasGo.transform).Stretch();
            screenRoot = UIKit.Node("Screens", canvasGo.transform).Stretch();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        // ───────────────────────── State ─────────────────────────

        public void ShowMainMenu()
        {
            hudRoot.gameObject.SetActive(false);
            Open(GameScreen.MainMenu);
        }

        public void ShowHud()
        {
            hudRoot.gameObject.SetActive(true);
            CloseAll();
        }

        public void OpenStation(string id)
        {
            switch (id)
            {
                case "depot": Open(GameScreen.Depot); break;
                case "shop": Open(GameScreen.Shop); break;
                case "terminal": Open(GameScreen.Missions); break;
            }
        }

        public void Open(GameScreen screen)
        {
            if (screen == GameScreen.Settings && current != GameScreen.Settings) settingsReturn = current == GameScreen.MainMenu ? GameScreen.MainMenu : GameScreen.Pause;
            if (screen == GameScreen.Achievements && current != GameScreen.Achievements) achievementsReturn = current == GameScreen.MainMenu ? GameScreen.MainMenu : GameScreen.Pause;
            current = screen;
            Clear(screenRoot);
            switch (screen)
            {
                case GameScreen.MainMenu: BuildMainMenu(); break;
                case GameScreen.Inventory: BuildInventory(); break;
                case GameScreen.Depot: BuildDepot(); break;
                case GameScreen.Shop: BuildShop(); break;
                case GameScreen.Missions: BuildMissions(); break;
                case GameScreen.Pause: BuildPause(); break;
                case GameScreen.Settings: BuildSettings(); break;
                case GameScreen.Credits: BuildCredits(); break;
                case GameScreen.Achievements: BuildAchievements(); break;
                case GameScreen.Ending: BuildEnding(); break;
            }
            SetPaused(screen != GameScreen.None);
            SelectFirstButtonForGamepad();
        }

        /// <summary>Rebuilds the open screen, e.g. after buying something.</summary>
        void Refresh() => Open(current);

        public void Back()
        {
            if (current == GameScreen.Settings) { SettingsManager.Save(); Open(settingsReturn); return; }
            if (current == GameScreen.Credits) { Open(GameScreen.MainMenu); return; }
            if (current == GameScreen.Achievements) { Open(achievementsReturn); return; }
            if (current == GameScreen.MainMenu) return;
            CloseAll();
        }

        void CloseAll()
        {
            current = GameScreen.None;
            Clear(screenRoot);
            SetPaused(false);
        }

        void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (player != null) player.InputEnabled = !paused;
            if (paused) CursorLock.Unlock();
            else CursorLock.Lock();
        }

        /// <summary>Gamepad players need a selected button to navigate menus with the D-pad or stick.</summary>
        void SelectFirstButtonForGamepad()
        {
            if (Gamepad.current == null || EventSystem.current == null) return;
            var first = screenRoot.GetComponentInChildren<Button>();
            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        void Update()
        {
            bool inGame = GameManager.Instance != null && GameManager.Instance.InGame;

            if (AutoPlaytest.Running) { if (hudRoot.gameObject.activeSelf) UpdateHud(); return; } // automated test drives the UI itself

            if (escape.WasPressedThisFrame())
            {
                if (current == GameScreen.None && inGame) Open(GameScreen.Pause);
                else Back();
            }
            else if (inGame && inventoryKey.WasPressedThisFrame())
            {
                if (current == GameScreen.None) Open(GameScreen.Inventory);
                else if (current == GameScreen.Inventory) CloseAll();
            }
            else if (inGame && missionsKey.WasPressedThisFrame())
            {
                if (current == GameScreen.None) Open(GameScreen.Missions);
                else if (current == GameScreen.Missions) CloseAll();
            }

            // Recover the mouse lock after alt-tab while playing.
            if (current == GameScreen.None && inGame && !CursorLock.IsLocked && Application.isFocused &&
                Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CursorLock.Lock();

            if (hudRoot.gameObject.activeSelf) UpdateHud();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && !AutoPlaytest.Running && current == GameScreen.None && GameManager.Instance != null && GameManager.Instance.InGame)
                Open(GameScreen.Pause);
        }

        // ───────────────────────── HUD ─────────────────────────

        Image HudPanel(string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float width)
        {
            var p = UIKit.Panel(hudRoot, name, UIKit.PanelColor);
            UIKit.Outline(p, UIKit.Line);
            p.rectTransform.Anchor(anchor, anchor, pivot, pos, new Vector2(width, 0f));
            UIKit.VLayout(p.gameObject, 6f, 14);
            var fit = p.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return p;
        }

        RectTransform BarRow(Transform parent, string label, Color colour, out Image fill, out Text value)
        {
            var row = UIKit.Row(parent, 10f, 22f);
            UIKit.Label(row, label, 15, UIKit.Muted, FontStyle.Bold).Width(84);
            var bar = UIKit.Bar(row, colour, 12f, out fill);
            UIKit.Size(bar.gameObject, 80, 12, 200, 12, 1f);
            value = UIKit.Label(row, "100", 16, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleRight).Width(40);
            return row;
        }

        void BuildHud()
        {
            // Vitals
            var vitals = HudPanel("Vitals", new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), 380);
            BarRow(vitals.transform, "HEALTH", UIKit.Hex("#2FA36B"), out healthFill, out healthValue);
            BarRow(vitals.transform, "OXYGEN", UIKit.Hex("#1A86C7"), out oxygenFill, out oxygenValue);

            // Mission tracker
            var mission = HudPanel("Mission", new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -128), 380);
            missionPanel = mission.gameObject;
            missionHead = UIKit.Label(mission.transform, "MISSION 1", 14, UIKit.AccentText, FontStyle.Bold);
            missionObjective = UIKit.Label(mission.transform, "", 19, UIKit.Ink, FontStyle.Bold);
            missionProgress = UIKit.Label(mission.transform, "", 15, UIKit.Muted);

            // Credits
            var credits = HudPanel("Credits", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), 220);
            UIKit.Label(credits.transform, "CREDITS", 14, UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleRight);
            creditsText = UIKit.Label(credits.transform, "0", 40, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            var gain = HudPanel("Credits Gained", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -130), 220);
            gainPanel = gain.gameObject;
            gainText = UIKit.Label(gain.transform, "+0 cr", 22, UIKit.Green, FontStyle.Bold, TextAnchor.MiddleRight);
            gainPanel.SetActive(false);

            // Crosshair
            crosshair = UIKit.Panel(hudRoot, "Crosshair", Color.white);
            crosshair.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            UIKit.Outline(crosshair, new Color(0, 0, 0, 0.5f));

            // Target
            var target = HudPanel("Target", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0, 120), 420);
            targetPanel = target.gameObject;
            targetName = UIKit.Label(target.transform, "", 18, UIKit.Ink, FontStyle.Bold);
            var tb = UIKit.Bar(target.transform, UIKit.Accent, 12f, out targetFill);
            targetInfo = UIKit.Label(target.transform, "", 15, UIKit.Muted);
            targetPanel.SetActive(false);

            // Interaction prompt
            var prompt = HudPanel("Prompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -70), 360);
            promptPanel = prompt.gameObject;
            promptText = UIKit.Label(prompt.transform, "", 19, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            promptPanel.SetActive(false);

            // Tool slots
            var slots = UIKit.Node("Tool Slots", hudRoot);
            slots.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(700, 84));
            UIKit.HLayout(slots.gameObject, 10, 0, TextAnchor.MiddleCenter);
            for (int i = 0; i < SlotCount; i++)
            {
                slotBg[i] = UIKit.Panel(slots, "Slot " + (i + 1), UIKit.PanelColor);
                UIKit.Outline(slotBg[i], UIKit.Line);
                UIKit.Size(slotBg[i].gameObject, 130, 76, 130, 76);
                slotText[i] = UIKit.Label(slotBg[i].transform, "", 15, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                slotText[i].rectTransform.Stretch(6);
            }

            // Backpack
            var pack = HudPanel("Backpack", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), 340);
            backpackText = UIKit.Label(pack.transform, "BACKPACK", 15, UIKit.Ink, FontStyle.Bold);
            var pb = UIKit.Bar(pack.transform, UIKit.Hex("#F4C430"), 12f, out backpackFill);
            backpackCounts = UIKit.Label(pack.transform, "", 15, UIKit.Muted);
            UIKit.Label(pack.transform, "Tab inventory · J missions · F scan · V view", 13, UIKit.Muted);

            // SCRAP-BOT
            var bot = HudPanel("SCRAP-BOT", new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 60), 520);
            UIKit.Outline(bot, UIKit.Cyan);
            botPanel = bot.gameObject;
            UIKit.Label(bot.transform, "SCRAP-BOT", 14, UIKit.Cyan, FontStyle.Bold);
            botText = UIKit.Label(bot.transform, "", 19, UIKit.Ink);
            botPanel.SetActive(false);

            // Truck
            var truck = HudPanel("Truck", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), 620);
            truckPanel = truck.gameObject;
            truckText = UIKit.Label(truck.transform, "", 19, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            truckPanel.SetActive(false);

            // Toast
            var toast = HudPanel("Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), 360);
            toastPanel = toast.gameObject;
            toastText = UIKit.Label(toast.transform, "", 19, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            toastPanel.SetActive(false);
        }

        void UpdateHud()
        {
            float dt = Time.unscaledDeltaTime;

            var suit = SuitSystems.Instance;
            if (suit != null)
            {
                UIKit.SetFill(healthFill, suit.Health / 100f);
                UIKit.SetFill(oxygenFill, suit.Oxygen / 100f);
                healthValue.text = Mathf.CeilToInt(suit.Health).ToString();
                oxygenValue.text = Mathf.CeilToInt(suit.Oxygen).ToString();
                oxygenValue.color = suit.Oxygen < 25f ? UIKit.AccentText : UIKit.Ink;
            }

            if (Wallet.Instance != null) creditsText.text = Wallet.Instance.Credits.ToString("N0");

            var mm = MissionManager.Instance;
            if (mm != null)
            {
                var m = mm.Current;
                if (m != null)
                {
                    missionHead.text = $"MISSION {m.number} OF {GameDatabase.Instance.missions.Length}" + (m.number > 8 ? "   ·   CONTRACTS" : "");
                    missionObjective.text = m.objectiveText;
                    missionProgress.text = (m.count > 1 ? $"{Mathf.Min(mm.Progress, m.count)} / {m.count}   ·   reward {m.rewardCredits:N0} cr" : $"Reward {m.rewardCredits:N0} cr   ·   J for details") + TargetHint(mm);
                }
                else
                {
                    missionHead.text = "ALL CONTRACTS COMPLETE";
                    missionObjective.text = "Free salvage";
                    missionProgress.text = "Keep dismantling and selling. Fresh junk arrives every few minutes.";
                }
            }

            // Target and crosshair
            var t = toolUser != null ? toolUser.Target : null;
            targetPanel.SetActive(t != null);
            if (t != null)
            {
                var tool = PlayerEquipment.Instance.Equipped;
                targetName.text = $"{t.Definition.displayName}   ·   tier {t.Definition.tier}";
                UIKit.SetFill(targetFill, t.Fraction);
                string reach = toolUser.TargetInRange ? "" : "   ·   move closer";
                string weak = tool != null && tool.tier < t.Definition.tier ? $"   ·   needs tier {t.Definition.tier} tool" : "";
                targetInfo.text = $"{Mathf.CeilToInt(t.Durability)} / {t.MaxDurability:0}   ·   {t.StageName}{reach}{weak}";
            }
            crosshair.color = t != null && toolUser.TargetInRange ? UIKit.Accent : Color.white;

            // Prompt
            var near = interactor != null ? interactor.Current : null;
            promptPanel.SetActive(near != null);
            if (near != null) promptText.text = "[E]  " + near.Prompt;

            // Tool slots
            var eq = PlayerEquipment.Instance;
            for (int i = 0; i < SlotCount; i++)
            {
                var def = ToolAtLevel(i + 1);
                bool owned = eq != null && def != null && eq.Owns(def);
                bool equipped = owned && eq.Equipped == def;
                slotText[i].text = def == null ? "" : owned ? $"{i + 1}  {def.displayName}\nT{def.tier}"
                    : def.price <= 0 ? $"{i + 1}  {def.displayName}\non the crate" : $"{i + 1}  Locked\n{def.price:N0} cr at Nova's";
                slotText[i].color = owned ? UIKit.Ink : UIKit.Muted;
                slotBg[i].color = equipped ? new Color(1f, 0.85f, 0.72f, 0.95f) : UIKit.PanelColor;
            }

            // Backpack
            var inv = PlayerInventory.Instance;
            if (inv != null)
            {
                backpackText.text = $"BACKPACK   {inv.TotalUnits} / {inv.Capacity}";
                backpackText.color = inv.IsFull ? UIKit.AccentText : UIKit.Ink;
                UIKit.SetFill(backpackFill, inv.Capacity > 0 ? (float)inv.TotalUnits / inv.Capacity : 0f);
                var sb = new System.Text.StringBuilder();
                foreach (var r in GameDatabase.Instance.resources)
                {
                    if (sb.Length > 0) sb.Append("   ");
                    sb.Append($"{r.displayName} {inv.Count(r)}");
                }
                backpackCounts.text = sb.ToString();
            }

            var tr = SalvageTruck.Instance;
            bool driving = tr != null && tr.Driving;
            truckPanel.SetActive(driving);
            if (driving)
                truckText.text = $"SALVAGE TRUCK   ·   {Mathf.Abs(tr.Speed) * 3.6f:0} km/h   ·   bin {tr.Bin.TotalUnits}/{tr.Bin.Capacity}\n<size=15>W/S speed · A/D steer · Space brake · E get out · drive over scrap to collect it</size>";
            crosshair.enabled = !driving;

            // Timed panels
            botTimer -= dt;
            botPanel.SetActive(botTimer > 0f);
            toastTimer -= dt;
            toastPanel.SetActive(toastTimer > 0f);
            gainTimer -= dt;
            gainPanel.SetActive(gainTimer > 0f);
        }

        /// <summary>"Target 85 m, ahead-left" for the current objective, relative to where the camera looks.</summary>
        string TargetHint(MissionManager mm)
        {
            var truck = SalvageTruck.Instance;
            Transform from = truck != null && truck.Driving ? truck.transform : player != null ? player.transform : null;
            if (from == null || !mm.TryGetTarget(from.position, out var target)) return "";
            Vector3 d = target - from.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 4f) return "\nTarget: right here";
            float camYaw = player != null && player.Camera != null ? player.Camera.Yaw : from.eulerAngles.y;
            float angle = Mathf.DeltaAngle(camYaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
            string dir = Mathf.Abs(angle) < 25f ? "ahead" : Mathf.Abs(angle) > 155f ? "behind you" : angle > 0f ? (angle < 90f ? "ahead-right" : "to your right") : (angle > -90f ? "ahead-left" : "to your left");
            return $"\nTarget: {dist:0} m {dir}";
        }

        static ToolDefinition ToolAtLevel(int level)
        {
            foreach (var t in GameDatabase.Instance.tools) if (t != null && t.level == level) return t;
            return null;
        }

        void OnBotSays(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line)) return;
            botText.text = line;
            botTimer = seconds * Mathf.Max(0.5f, SettingsManager.Current.messageDuration);
        }

        void OnCreditsGained(int amount, string label)
        {
            gainText.text = $"+{amount:N0} cr";
            gainTimer = 2.5f;
        }

        public void Toast(string message, float seconds = 2.5f)
        {
            toastText.text = message;
            toastTimer = seconds;
        }
    }
}
