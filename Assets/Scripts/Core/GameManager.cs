using PlanetScrappers.Automation;
using PlanetScrappers.Destruction;
using PlanetScrappers.Vehicles;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.Player;
using PlanetScrappers.Save;
using PlanetScrappers.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlanetScrappers.Core
{
    public enum StartMode { Menu, NewGame, Continue }

    /// <summary>
    /// Starts a new game or continues a save, autosaves, and moves between the main menu and play.
    /// New Game and Main Menu reload the scene so every system starts from a clean state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static StartMode PendingStart = StartMode.Menu;

        [SerializeField] PlayerController player;
        [SerializeField] float autosaveInterval = 120f;

        public bool InGame { get; private set; }
        public float PlaySeconds { get; private set; }
        public float SessionSeconds { get; private set; }
        public int SessionDismantled { get; private set; }
        public int SessionScrap { get; private set; }
        public int SessionEarned { get; private set; }
        public string LastSavedAt { get; private set; }

        float autosaveTimer;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            GameEvents.ScrapSold += OnSold;
            GameEvents.ToolPurchased += OnAutosaveEvent;
            GameEvents.BackpackUpgraded += OnAutosaveEvent;
            GameEvents.MissionCompleted += OnAutosaveEvent;
            GameEvents.ObjectDismantled += OnDismantled;
            GameEvents.ScrapCollected += OnCollected;
        }

        void OnDisable()
        {
            GameEvents.ScrapSold -= OnSold;
            GameEvents.ToolPurchased -= OnAutosaveEvent;
            GameEvents.BackpackUpgraded -= OnAutosaveEvent;
            GameEvents.MissionCompleted -= OnAutosaveEvent;
            GameEvents.ObjectDismantled -= OnDismantled;
            GameEvents.ScrapCollected -= OnCollected;
        }

        void Start()
        {
            SettingsManager.Load();
            SettingsManager.Apply(player.Camera);

            var mode = PendingStart;
            PendingStart = StartMode.Menu;
            switch (mode)
            {
                case StartMode.NewGame: StartNew(); break;
                case StartMode.Continue: if (!LoadGame()) StartNew(); break;
                default: UIManager.Instance.ShowMainMenu(); break;
            }
        }

        void Update()
        {
            if (!InGame || Time.timeScale == 0f) return;
            PlaySeconds += Time.deltaTime;
            SessionSeconds += Time.deltaTime;
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveInterval) SaveGame(false);
        }

        void OnSold(int credits)
        {
            SessionEarned += credits;
            OnAutosaveEvent(0);
        }

        void OnDismantled(DestructibleObject o) => SessionDismantled++;
        void OnCollected(ResourceDefinition r, int amount) => SessionScrap += amount;
        void OnAutosaveEvent<T>(T _) { if (InGame) SaveGame(false); }

        void StartNew()
        {
            var db = GameDatabase.Instance;
            Wallet.Instance.SetState(db.startingCredits, null);
            PlayerInventory.Instance.Clear();
            PlayerInventory.Instance.SetCapacityLevel(0);
            PlayerEquipment.Instance.ClearAll();
            MissionManager.Instance.SetState(0, 0);
            InGame = true;
            UIManager.Instance.ShowHud();
            MissionManager.Instance.Brief(1.5f);
            SaveGame(false);
        }

        bool LoadGame()
        {
            var data = SaveManager.Read();
            if (data == null) return false;
            var db = GameDatabase.Instance;

            player.Teleport(new Vector3(data.position[0], data.position[1], data.position[2]), Quaternion.Euler(0f, data.yaw, 0f));
            SuitSystems.Instance?.SetState(data.health, data.oxygen);
            Wallet.Instance.SetState(data.credits, data.transactions);

            var inv = PlayerInventory.Instance;
            inv.Clear();
            inv.SetCapacityLevel(data.backpackLevel);
            foreach (var a in data.inventory) inv.SetCount(db.Resource(a.id), a.amount);

            var eq = PlayerEquipment.Instance;
            eq.ClearAll();
            foreach (var id in data.ownedTools) eq.Grant(db.Tool(id), false);
            if (!string.IsNullOrEmpty(data.equippedTool)) eq.Equip(db.Tool(data.equippedTool));

            MissionManager.Instance.SetState(data.missionIndex, data.missionProgress);

            foreach (var s in data.objects)
            {
                var o = DestructibleObject.All.Find(x => x.UniqueId == s.id);
                if (o != null) o.LoadState(s.dismantled, s.durability, s.respawn);
            }

            var truck = SalvageTruck.Instance;
            if (truck != null && data.truckPosition != null && data.truckPosition.Length == 3)
                truck.Place(new Vector3(data.truckPosition[0], data.truckPosition[1], data.truckPosition[2]), data.truckYaw);
            foreach (var st in data.storages)
            {
                var store = ScrapStorage.Find(st.id);
                if (store == null) continue;
                store.Clear();
                foreach (var a in st.items) store.SetCount(db.Resource(a.id), a.amount);
            }
            foreach (var pad in CollectorPad.Pads) pad.SetBuilt(data.builtCollectors.Contains(pad.PadId));

            PlaySeconds = data.playSeconds;
            LastSavedAt = data.savedAt;
            InGame = true;
            UIManager.Instance.ShowHud();
            GameEvents.Say("Welcome back, Scrapper. The junk waited for you. Mostly.", 5f);
            return true;
        }

        public void SaveGame(bool announce)
        {
            if (!InGame) return;
            autosaveTimer = 0f;
            var data = new SaveData
            {
                gameVersion = GameVersion.Version,
                playSeconds = PlaySeconds,
                yaw = player.transform.eulerAngles.y,
                health = SuitSystems.Instance != null ? SuitSystems.Instance.Health : 100f,
                oxygen = SuitSystems.Instance != null ? SuitSystems.Instance.Oxygen : 100f,
                credits = Wallet.Instance.Credits,
                backpackLevel = PlayerInventory.Instance.CapacityLevel,
                equippedTool = PlayerEquipment.Instance.Equipped != null ? PlayerEquipment.Instance.Equipped.id : null,
                missionIndex = MissionManager.Instance.Index,
                missionProgress = MissionManager.Instance.Progress
            };
            Vector3 p = player.transform.position;
            data.position = new[] { p.x, p.y + 0.1f, p.z };
            data.transactions.AddRange(Wallet.Instance.Log);
            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = PlayerInventory.Instance.Count(r);
                if (n > 0) data.inventory.Add(new ResourceAmount { id = r.id, amount = n });
            }
            data.ownedTools.AddRange(PlayerEquipment.Instance.OwnedIds);
            foreach (var o in DestructibleObject.All)
            {
                if (o.IsDismantled || o.Fraction < 1f)
                    data.objects.Add(new ObjectState { id = o.UniqueId, dismantled = o.IsDismantled, durability = o.Durability, respawn = o.RespawnTimer });
            }

            var truck = SalvageTruck.Instance;
            if (truck != null)
            {
                Vector3 tp = truck.transform.position;
                data.truckPosition = new[] { tp.x, tp.y, tp.z };
                data.truckYaw = truck.transform.eulerAngles.y;
                if (truck.Driving) data.position = new[] { tp.x - 3f, 0.2f, tp.z };
            }
            foreach (var store in ScrapStorage.All)
            {
                var st = new StorageState { id = store.StorageId };
                foreach (var r in GameDatabase.Instance.resources)
                {
                    int n = store.Count(r);
                    if (n > 0) st.items.Add(new ResourceAmount { id = r.id, amount = n });
                }
                data.storages.Add(st);
            }
            foreach (var pad in CollectorPad.Pads) if (pad.Built) data.builtCollectors.Add(pad.PadId);

            if (SaveManager.Write(data))
            {
                LastSavedAt = data.savedAt;
                if (announce) UIManager.Instance?.Toast("Game saved");
            }
            else if (announce) UIManager.Instance?.Toast("Save failed. Check disk space.");
        }

        // ── Scene flow ──

        /// <summary>Stops all saving for the rest of this run (used by the automated playtest).</summary>
        public void SuspendSaving() => InGame = false;

        public static void NewGame() => Reload(StartMode.NewGame);
        public static void ContinueGame() => Reload(StartMode.Continue);

        public static void ReturnToMenu()
        {
            Instance?.SaveGame(false);
            Reload(StartMode.Menu);
        }

        static void Reload(StartMode mode)
        {
            PendingStart = mode;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void Quit()
        {
            Instance?.SaveGame(false);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void OnApplicationQuit()
        {
            if (InGame) SaveGame(false);
        }
    }
}
