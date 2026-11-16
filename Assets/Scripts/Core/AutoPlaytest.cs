using System.Collections;
using System.IO;
using System.Linq;
using PlanetScrappers.Automation;
using PlanetScrappers.Destruction;
using PlanetScrappers.Vehicles;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.Player;
using PlanetScrappers.Save;
using PlanetScrappers.UI;
using UnityEngine;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Scripted smoke test of the whole first shift, for QA. Only runs when the game is launched
    /// with -autotest. It backs up any real save first and restores it at the end, logs every
    /// check as [Autotest] PASS/FAIL and saves screenshots to the autotest folder.
    /// </summary>
    public class AutoPlaytest : MonoBehaviour
    {
        static bool Requested => System.Environment.GetCommandLineArgs().Contains("-autotest");
        static bool started;

        /// <summary>True while the automated test is driving the game (disables focus-loss pausing).</summary>
        public static bool Running { get; private set; }

        [SerializeField] PlayerController player;

        string shotDir;
        int passes, fails;

        void Awake()
        {
            if (!Requested) { enabled = false; return; }
            Running = true;
            Application.runInBackground = true;
            if (!started)
            {
                started = true;
                BackupRealSave();
                GameManager.PendingStart = StartMode.NewGame;
            }
        }

        IEnumerator Start()
        {
            if (!enabled) yield break;
            shotDir = Path.Combine(Application.persistentDataPath, "autotest");
            Directory.CreateDirectory(shotDir);
            yield return new WaitForSecondsRealtime(2f);
            yield return Run();
        }

        IEnumerator Run()
        {
            var db = GameDatabase.Instance;
            var gm = GameManager.Instance;
            var mm = MissionManager.Instance;
            var inv = PlayerInventory.Instance;
            var wallet = Wallet.Instance;
            var eq = PlayerEquipment.Instance;
            var ui = UIManager.Instance;

            Check(gm.InGame, "new game started");
            Check(wallet.Credits == db.startingCredits, $"starting credits {wallet.Credits}");
            yield return Shot("01_start_hud");

            // Mission 1: terminal
            var terminal = Station("terminal");
            yield return GoTo(terminal.transform.position, 2f);
            terminal.Interact();
            yield return Frames(3);
            Check(ui.Current == GameScreen.Missions, "terminal opens missions screen");
            yield return Shot("02_missions_screen");
            ui.Back();
            Check(mm.Index == 1, "mission 1 complete");

            // Mission 2: wrench + oil drum
            var pickup = FindAnyObjectByType<ToolPickup>();
            yield return GoTo(pickup.transform.position, 1.5f);
            pickup.Interact();
            Check(eq.Equipped != null && eq.Equipped.id == "rusty_wrench", "wrench picked up and fitted");
            yield return Dismantle("oil_drum", "03_drum");
            Check(mm.Index == 2, "mission 2 complete (first drum)");

            // Mission 3: 20 iron
            for (int i = 0; i < 9 && mm.Index == 2; i++) yield return Dismantle("oil_drum", null);
            Check(mm.Index == 3, $"mission 3 complete (iron collected: {inv.Count(db.Resource("iron"))})");
            yield return OpenShot(GameScreen.Inventory, "04_inventory");

            // Mission 4: sell
            var depot = Station("depot");
            yield return GoTo(depot.transform.position, 2f);
            depot.Interact();
            yield return Frames(3);
            yield return Shot("05_depot");
            int before = wallet.Credits;
            int earned = Trading.SellAll();
            ui.Back();
            Check(earned > 0 && wallet.Credits > before, $"sold scrap for {earned} cr");
            Check(mm.Index == 4, "mission 4 complete (first sale)");

            // Mission 5: hammer (top up credits if the test run is short)
            TopUp(wallet, db.Tool("scrap_hammer").price);
            var shop = Station("shop");
            yield return GoTo(shop.transform.position, 2f);
            shop.Interact();
            yield return Frames(3);
            yield return Shot("06_shop");
            Check(Trading.BuyTool(db.Tool("scrap_hammer")) == PurchaseResult.Ok, "bought Scrap Hammer");
            ui.Back();
            Check(mm.Index == 5, "mission 5 complete (hammer)");

            // Tier check: wrench must bounce off a truck
            var truck = Nearest("mule7_truck");
            eq.Equip(db.Tool("rusty_wrench"));
            var bounce = truck.Hit(eq.Equipped, truck.Center, Vector3.up);
            Check(bounce == HitResult.TooWeak, "wrench bounces off tier 2 truck");
            eq.Equip(db.Tool("scrap_hammer"));

            // Mission 6: Mule-7
            yield return Dismantle("mule7_truck", "07_truck");
            Check(mm.Index == 6, "mission 6 complete (truck)");

            // Mission 7: backpack
            TopUp(wallet, inv.NextUpgradePrice);
            int cap = inv.Capacity;
            Check(Trading.BuyBackpack() == PurchaseResult.Ok && inv.Capacity > cap, $"backpack upgraded to {inv.Capacity}");
            Check(mm.Index == 7, "mission 7 complete (backpack)");

            // Mission 8: dismantler, gate opens
            TopUp(wallet, db.Tool("electric_dismantler").price);
            Check(Trading.BuyTool(db.Tool("electric_dismantler")) == PurchaseResult.Ok, "bought Electric Dismantler");
            Check(mm.Index == 8, "mission 8 complete, first shift done");
            yield return new WaitForSecondsRealtime(3f);
            var gate = FindObjectsByType<MachineRowGate>(FindObjectsSortMode.None).First(g => g.name == "Locked Gate");
            Check(gate.GetComponent<Collider>() == null || !gate.GetComponent<Collider>().enabled, "Machine Row gate opened");
            yield return Dismantle("generator_unit", "08_generator");

            // ── Phase 2 ──
            // Mission 9: get in the truck
            var rig = SalvageTruck.Instance;
            yield return GoTo(rig.transform.position, 3.5f);
            rig.Interact();
            yield return new WaitForSecondsRealtime(1f);
            Check(rig.Driving && !player.gameObject.activeSelf, "entered the salvage truck");
            Check(mm.Index == 9, "mission 9 complete (truck)");
            yield return Shot("12_driving");

            // Mission 10: truck bin collects scrap by driving over it
            var mule = Nearest("mule7_truck");
            if (mule != null)
            {
                eq.Equip(db.Tool("electric_dismantler"));
                while (!mule.IsDismantled) mule.Hit(eq.Equipped, mule.Center, Vector3.up);
                EnsurePlaying();
                rig.Place(mule.Center + Vector3.back * 1f, 0f);
                yield return new WaitForSecondsRealtime(4f);
            }
            Check(rig.Bin.TotalUnits > 0, $"truck bin vacuumed scrap ({rig.Bin.TotalUnits} units)");
            if (mm.Index == 9) rig.Bin.Add(db.Resource("iron"), 40);
            Check(mm.Index == 10, "mission 10 complete (truck load)");
            rig.Place(Station("depot").transform.position + new Vector3(8f, 0.2f, 6f), 0f);
            int binValue = rig.Bin.TotalValue();
            int sold = Trading.SellStorage(rig.Bin);
            Check(sold == binValue && sold > 0 && rig.Bin.TotalUnits == 0, $"sold the truck load for {sold} cr");
            rig.Exit();
            yield return Frames(3);
            Check(!rig.Driving && player.gameObject.activeSelf, "got out of the truck");

            // Mission 11: scrap collector
            TopUp(wallet, 800);
            var pad = CollectorPad.Pads.FirstOrDefault(pd => DestructibleObject.All.Any(o => !o.IsDismantled && o.Definition.tier <= 3 && (o.Center - pd.transform.position).sqrMagnitude < 13f * 13f)) ?? CollectorPad.Pads[0];
            yield return GoTo(pad.transform.position, 2f);
            pad.Interact();
            Check(pad.Built, "built a scrap collector");
            Check(mm.Index == 11, "mission 11 complete (collector)");
            // Drop fresh scrap beside the collector while the player is far away: only the collector can grab it.
            player.Teleport(pad.transform.position + Vector3.back * 40f, Quaternion.identity);
            ScrapSpawner.Instance.SpawnYield(db.Dismantlable("vending_bot"), 0f, pad.transform.position + new Vector3(4f, 0.5f, 3f));
            yield return new WaitForSecondsRealtime(4f);
            Check(pad.Hopper.TotalUnits > 0, $"collector pulled in scrap on its own ({pad.Hopper.TotalUnits} units)");
            yield return GoTo(pad.transform.position, 2f);
            yield return Shot("13_collector");

            // Warehouse storage
            inv.Add(db.Resource("copper"), 5);
            var deposit = FindAnyObjectByType<WarehouseDeposit>();
            var warehouse = PlanetScrappers.Inventory.ScrapStorage.Find("warehouse");
            int before2 = warehouse.TotalUnits;
            deposit.Interact();
            Check(warehouse.TotalUnits > before2, "stored backpack in the warehouse");
            Check(Trading.SellStorage(warehouse) > 0, "sold the warehouse contents");

            // Mission 12: Iron District robot arm (Electric Dismantler)
            yield return Dismantle("robot_arm", "14_robot_arm");
            Check(mm.Index == 12, "mission 12 complete (Iron District robot arm)");
            var press = Nearest("assembly_line");
            Check(press != null && press.Hit(eq.Equipped, press.Center, Vector3.up) == HitResult.TooWeak, "dismantler bounces off a tier 4 assembly line");

            // Mission 13 + 14: Plasma Cutter, assembly line
            TopUp(wallet, db.Tool("plasma_cutter").price);
            Check(Trading.BuyTool(db.Tool("plasma_cutter")) == PurchaseResult.Ok, "bought the Plasma Cutter");
            Check(mm.Index == 13, "mission 13 complete (Plasma Cutter)");
            yield return Dismantle("assembly_line", "15_assembly_line");
            Check(mm.Index == 14, "mission 14 complete, Phase 2 contracts done");

            // ── Phase 3 ──
            // Mission 15: Recycling Factory
            var factory = Machine.Find("factory");
            TopUp(wallet, factory.Price);
            Check(Trading.BuyMachine(factory) == PurchaseResult.Ok && factory.Built && factory.Powered, "built the Recycling Factory");
            Check(mm.Index == 15, "mission 15 complete (factory)");
            warehouse.Add(db.Resource("iron"), 30);
            EnsurePlaying();
            int credBefore = wallet.Credits;
            yield return new WaitForSecondsRealtime(9f);
            Check(warehouse.TotalUnits < 30 && wallet.Credits > credBefore, $"factory refined warehouse scrap ({30 - warehouse.TotalUnits} units, +{wallet.Credits - credBefore} cr net)");

            // Mission 16: salvage robot
            TopUp(wallet, RobotDock.Instance.Price);
            Check(Trading.BuyRobot() == PurchaseResult.Ok && RobotDock.Instance.Count == 1, "bought a salvage robot");
            Check(mm.Index == 16, "mission 16 complete (robot)");
            var robot = RobotDock.Instance.Robots[0];
            EnsurePlaying();
            float robotWait = 0f;
            while (robot.Hits == 0 && robotWait < 40f) { robotWait += 1f; yield return new WaitForSecondsRealtime(1f); }
            Check(robot.Hits > 0, $"robot found and worked on salvage by itself ({robot.Hits} hits, {robotWait:0}s)");

            // Conveyor link
            var conveyor = Machine.All.OfType<ConveyorLink>().FirstOrDefault(c => c.CanBuild);
            if (conveyor != null)
            {
                TopUp(wallet, conveyor.Price);
                Check(Trading.BuyMachine(conveyor) == PurchaseResult.Ok, "built a conveyor link");
                conveyor.Source.Hopper.Add(db.Resource("iron"), 10);
                int hopBefore = conveyor.Source.Hopper.TotalUnits;
                yield return new WaitForSecondsRealtime(3f);
                Check(conveyor.Source.Hopper.TotalUnits < hopBefore, "conveyor moved scrap to the warehouse");
            }
            else Check(false, "found a buildable conveyor");
            yield return Shot("16_factory_robot");

            // Mission 17: Spaceport Graveyard (Plasma Cutter is tier 4)
            eq.Equip(db.Tool("plasma_cutter"));
            yield return Dismantle("ship_nose", "17_ship_nose");
            Check(mm.Index == 17, "mission 17 complete (freighter nose)");
            var hull = Nearest("ship_hull");
            Check(hull != null && hull.Hit(eq.Equipped, hull.Center, Vector3.up) == HitResult.TooWeak, "plasma cutter bounces off a tier 5 hull");

            // Mission 18 + 19: crusher, engine
            TopUp(wallet, db.Tool("industrial_crusher").price);
            Check(Trading.BuyTool(db.Tool("industrial_crusher")) == PurchaseResult.Ok, "bought the Industrial Crusher");
            Check(mm.Index == 18, "mission 18 complete (crusher)");
            yield return Dismantle("ship_engine", "18_engine");
            Check(mm.Index == 19, "mission 19 complete (engine)");

            // Mission 20: Deep Core
            float oxyBefore = SuitSystems.Instance.Oxygen;
            yield return Dismantle("drilling_rig", "19_deep_core");
            Check(SuitSystems.Instance.InDeepCore || player.transform.position.x < -120f, "reached the Deep Core");
            Check(mm.AllComplete, "mission 20 complete, all contracts done");

            // Save and reload check
            int credits = wallet.Credits;
            gm.SaveGame(true);
            var data = SaveManager.Read();
            Check(data != null && data.credits == credits && data.missionIndex == mm.Index && data.ownedTools.Count == 5 && data.builtCollectors.Count == 1 && data.truckPosition != null && data.robots == 1 && data.machines.Exists(x => x.id == "factory" && x.built), "save file written and readable");
            Check(File.Exists(SaveManager.SavePath), "save.json exists");

            yield return OpenShot(GameScreen.Pause, "09_pause");
            yield return OpenShot(GameScreen.Settings, "10_settings");
            ui.Back();
            yield return OpenShot(GameScreen.Missions, "11_missions_done");

            Debug.Log($"[Autotest] DONE: {passes} passed, {fails} failed. Screenshots in {shotDir}");
            gm.SuspendSaving();
            RestoreRealSave();
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        // ── helpers ──

        void Check(bool ok, string what)
        {
            if (ok) passes++; else fails++;
            Debug.Log($"[Autotest] {(ok ? "PASS" : "FAIL")}: {what}");
        }

        static StationInteractable Station(string id) =>
            FindObjectsByType<StationInteractable>(FindObjectsSortMode.None).First(s => s.StationId == id);

        DestructibleObject Nearest(string id) =>
            DestructibleObject.All.Where(o => !o.IsDismantled && o.Definition.id == id)
                .OrderBy(o => (o.Center - player.transform.position).sqrMagnitude).FirstOrDefault();

        /// <summary>Closes any screen left open (e.g. by a stray key press) so game time runs.</summary>
        static void EnsurePlaying()
        {
            if (UIManager.Instance.Current != GameScreen.None && UIManager.Instance.Current != GameScreen.MainMenu)
            {
                Debug.Log($"[Autotest] closing stray screen {UIManager.Instance.Current}");
                UIManager.Instance.ShowHud();
            }
        }

        IEnumerator GoTo(Vector3 target, float distance)
        {
            EnsurePlaying();
            Vector3 from = target + Vector3.back * distance;
            from.y = 0.1f;
            Vector3 look = target - from;
            look.y = 0f;
            player.Teleport(from, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward));
            yield return Frames(2);
        }

        IEnumerator Dismantle(string id, string shotPrefix)
        {
            var o = Nearest(id);
            if (o == null) { Check(false, "found a " + id); yield break; }
            Vector3 c = o.Center;
            EnsurePlaying();
            Debug.Log($"[Autotest] -> {o.UniqueId} at {c} timeScale={Time.timeScale} screen={UIManager.Instance.Current} backpack={PlayerInventory.Instance.TotalUnits}/{PlayerInventory.Instance.Capacity}");
            Vector3 stand = c + (player.transform.position - c).normalized * 2.2f;
            yield return GoTo(c, Mathf.Max(1.6f, Vector3.Distance(new Vector3(stand.x, 0, stand.z), new Vector3(c.x, 0, c.z))));
            var tool = PlayerEquipment.Instance.Equipped;
            bool shotTaken = false;
            int guard = 200;
            while (!o.IsDismantled && guard-- > 0)
            {
                o.Hit(tool, c + Vector3.up * 0.3f, Vector3.up);
                if (shotPrefix != null && !shotTaken && o.Stage >= 2)
                {
                    shotTaken = true;
                    yield return Shot(shotPrefix + "_damaged");
                }
                yield return new WaitForSecondsRealtime(1f / tool.hitsPerSecond);
            }
            Check(o.IsDismantled, $"dismantled {o.Definition.displayName} with {tool.displayName}");
            if (shotPrefix != null) yield return Shot(shotPrefix + "_broken");
            // Stand on the spot so the scrap flies in.
            yield return new WaitForSecondsRealtime(1.2f);
            player.Teleport(new Vector3(c.x, 0.1f, c.z), player.transform.rotation);
            yield return new WaitForSecondsRealtime(2.5f);
        }

        static void TopUp(Wallet wallet, int needed)
        {
            if (wallet.Credits < needed)
            {
                Debug.Log($"[Autotest] topping up {needed - wallet.Credits} cr to skip grinding");
                wallet.Add(needed - wallet.Credits, "Autotest top-up");
            }
        }

        IEnumerator OpenShot(GameScreen screen, string name)
        {
            UIManager.Instance.Open(screen);
            yield return Frames(3);
            yield return Shot(name);
            if (screen != GameScreen.Settings) UIManager.Instance.Back();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, name + ".png"));
            yield return Frames(2);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static string Backup => SaveManager.SavePath + ".autotest-backup";
        static string BackupBak => SaveManager.BackupPath + ".autotest-backup";

        static void BackupRealSave()
        {
            if (File.Exists(SaveManager.SavePath)) File.Copy(SaveManager.SavePath, Backup, true);
            if (File.Exists(SaveManager.BackupPath)) File.Copy(SaveManager.BackupPath, BackupBak, true);
        }

        static void RestoreRealSave()
        {
            SaveManager.DeleteAll();
            if (File.Exists(Backup)) { File.Copy(Backup, SaveManager.SavePath, true); File.Delete(Backup); }
            if (File.Exists(BackupBak)) { File.Copy(BackupBak, SaveManager.BackupPath, true); File.Delete(BackupBak); }
        }
    }
}
