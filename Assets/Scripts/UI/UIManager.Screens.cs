using PlanetScrappers.Automation;
using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.Player;
using PlanetScrappers.Save;
using PlanetScrappers.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetScrappers.UI
{
    public partial class UIManager
    {
        // ───────────────────────── Shared layout ─────────────────────────

        RectTransform Card(float width, float height, string eyebrow, string title, Color eyebrowColour, bool closeButton = true)
        {
            var veil = UIKit.Panel(screenRoot, "Veil", UIKit.Veil, false);
            veil.rectTransform.Stretch();

            var card = UIKit.Panel(screenRoot, "Card", UIKit.PanelColor);
            UIKit.Outline(card, UIKit.Line);
            card.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            UIKit.VLayout(card.gameObject, 14f, 32);

            var head = UIKit.Row(card.transform, 16f);
            var titles = UIKit.Node("Titles", head);
            UIKit.VLayout(titles.gameObject, 2f, 0);
            UIKit.Size(titles.gameObject, -1, -1, -1, -1, 1f);
            UIKit.Label(titles, eyebrow.ToUpperInvariant(), 15, eyebrowColour, FontStyle.Bold);
            UIKit.Label(titles, title.ToUpperInvariant(), 44, UIKit.Ink, FontStyle.Bold);
            if (closeButton)
            {
                var close = UIKit.Button(head, "Close  (Esc)", Back, false, 18, 46);
                UIKit.Size(close.gameObject, 170, 46, 170, 46, 0f);
            }
            Divider(card.transform);
            return card.rectTransform;
        }

        static void Divider(Transform parent)
        {
            var line = UIKit.Panel(parent, "Divider", UIKit.Line, false);
            UIKit.Size(line.gameObject, -1, 2, -1, 2);
        }

        static RectTransform Column(Transform parent, float flex, float spacing = 10f)
        {
            var col = UIKit.Node("Column", parent);
            UIKit.VLayout(col.gameObject, spacing, 0);
            UIKit.Size(col.gameObject, -1, -1, -1, -1, flex);
            return col;
        }

        static RectTransform Columns(Transform parent, float spacing = 28f)
        {
            var row = UIKit.Node("Columns", parent);
            var l = UIKit.HLayout(row.gameObject, spacing, 0, TextAnchor.UpperLeft);
            l.childForceExpandHeight = true;
            UIKit.Size(row.gameObject, -1, -1, -1, -1, 1f);
            row.GetComponent<LayoutElement>().flexibleHeight = 1f;
            return row;
        }

        // ───────────────────────── Main menu ─────────────────────────

        bool confirmNewGame;

        void BuildMainMenu()
        {
            var panel = UIKit.Panel(screenRoot, "Main Menu Panel", UIKit.PanelColor, false);
            panel.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(640, 0));
            UIKit.VLayout(panel.gameObject, 14f, 72, TextAnchor.MiddleLeft);

            UIKit.Label(panel.transform, "OMNISALVAGE LTD. PRESENTS", 17, UIKit.Amber, FontStyle.Bold);
            UIKit.Label(panel.transform, "PLANET\nSCRAPPERS", 72, UIKit.AccentText, FontStyle.Bold).lineSpacing = 0.85f;
            UIKit.Label(panel.transform, "NOTHING LASTS. EVERYTHING HAS VALUE.", 22, UIKit.Muted, FontStyle.Bold);
            Spacer(panel.transform, 18);

            bool hasSave = SaveManager.HasSave;
            if (hasSave)
            {
                var data = SaveManager.Read();
                string info = data != null ? $"Continue   ·   {data.credits:N0} cr   ·   saved {data.savedAt}" : "Continue";
                UIKit.Button(panel.transform, info, GameManager.ContinueGame, true, 22, 62);
            }
            UIKit.Button(panel.transform, confirmNewGame ? "Start over? Click again to confirm" : "New Game", () =>
            {
                if (hasSave && !confirmNewGame)
                {
                    confirmNewGame = true;
                    Refresh();
                    return;
                }
                confirmNewGame = false;
                GameManager.NewGame();
            }, !hasSave, 24, 58);
            UIKit.Button(panel.transform, "Settings", () => Open(GameScreen.Settings), false, 24, 58);
            UIKit.Button(panel.transform, "Credits", () => Open(GameScreen.Credits), false, 24, 58);
            UIKit.Button(panel.transform, "Exit", GameManager.Quit, false, 24, 58);
            Spacer(panel.transform, 18);
            UIKit.Label(panel.transform, $"v{GameVersion.Version}   ·   {GameVersion.Milestone}   ·   Windows", 16, UIKit.Muted);
        }

        static void Spacer(Transform parent, float height)
        {
            var s = UIKit.Node("Spacer", parent);
            UIKit.Size(s.gameObject, -1, height, -1, height);
        }

        void BuildCredits()
        {
            var card = Card(900, 640, "Planet Scrappers", "Credits", UIKit.AccentText);
            UIKit.Label(card, "Design and development: the Planet Scrappers team, with Claude (Anthropic).", 20, UIKit.Ink);
            UIKit.Label(card, "Engine: Unity 6 with the Universal Render Pipeline.", 20, UIKit.Ink);
            UIKit.Label(card, "Characters, story, places and the Rust Valley layout are original to Planet Scrappers.", 20, UIKit.Ink);
            UIKit.Label(card, "Textures and sound effects in this build are generated by our own code. Placeholder models will be replaced in the art pass.", 20, UIKit.Muted);
            UIKit.Label(card, "Thanks for testing. Please report bugs with the build version shown in the corner.", 20, UIKit.Muted);
        }

        // ───────────────────────── Inventory ─────────────────────────

        void BuildInventory()
        {
            var inv = PlayerInventory.Instance;
            var card = Card(1180, 780, "Inventory", "Backpack", UIKit.Muted);
            var cols = Columns(card);
            var left = Column(cols, 1.6f);
            var right = Column(cols, 1f);

            var cap = UIKit.Row(left, 12f);
            UIKit.Label(cap, "Capacity", 20, UIKit.Ink, FontStyle.Bold).Flex();
            UIKit.Label(cap, $"{inv.TotalUnits} / {inv.Capacity} units", 20, inv.IsFull ? UIKit.AccentText : UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight).Width(240);
            UIKit.Bar(left, UIKit.Hex("#F4C430"), 14f, out var capFill);
            UIKit.SetFill(capFill, inv.Capacity > 0 ? (float)inv.TotalUnits / inv.Capacity : 0f);

            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = inv.Count(r);
                var tile = UIKit.Panel(left, r.displayName, new Color(1, 1, 1, 0.7f));
                UIKit.Outline(tile, UIKit.Line);
                var row = tile.gameObject;
                UIKit.HLayout(row, 16f, 14);
                UIKit.Swatch(tile.transform, r.color, 44);
                var text = Column(tile.transform, 1f, 2f);
                UIKit.Label(text, r.displayName, 22, UIKit.Ink, FontStyle.Bold);
                UIKit.Label(text, r.description, 15, UIKit.Muted);
                UIKit.Label(text, $"{r.unitValue} cr each   ·   {r.rarity}", 15, UIKit.Muted);
                var qty = Column(tile.transform, 0f, 2f);
                UIKit.Size(qty.gameObject, 130, -1, 130, -1, 0f);
                UIKit.Label(qty, "×" + n, 34, UIKit.Ink, FontStyle.Bold, TextAnchor.MiddleRight);
                UIKit.Label(qty, $"{n * r.unitValue} cr", 17, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            }

            var worth = UIKit.Row(left, 12f);
            UIKit.Label(worth, "Value if sold now", 19, UIKit.Muted).Flex();
            UIKit.Label(worth, $"{inv.TotalValue():N0} cr", 28, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight).Width(200);

            UIKit.Label(right, "TOOLS ON THE SALVAGE ARM", 15, UIKit.Muted, FontStyle.Bold);
            var eq = PlayerEquipment.Instance;
            bool any = false;
            foreach (var t in GameDatabase.Instance.tools)
            {
                if (t == null || !eq.Owns(t)) continue;
                any = true;
                var row = UIKit.Row(right, 10f, 52);
                UIKit.Label(row, $"{t.level}  {t.displayName}\n<size=14>tier {t.tier} · {t.DamagePerSecond:0} dps · reach {t.range:0.0} m</size>", 19, UIKit.Ink, FontStyle.Bold).Flex();
                bool equipped = eq.Equipped == t;
                var b = UIKit.Button(row, equipped ? "Equipped" : "Equip", () => { eq.Equip(t); Refresh(); }, !equipped, 17, 44, !equipped);
                UIKit.Size(b.gameObject, 120, 44, 120, 44, 0f);
            }
            if (!any) UIKit.Label(right, "No tools yet. The rusty wrench is on the crate west of the base road.", 17, UIKit.Muted);
            Spacer(right, 12);
            UIKit.Label(right, "Tab or Esc to close. Number keys 1–5 switch tools while playing.", 15, UIKit.Muted);
        }

        // ───────────────────────── Recycling Depot ─────────────────────────

        void BuildDepot()
        {
            var inv = PlayerInventory.Instance;
            var wallet = Wallet.Instance;
            var card = Card(1240, 760, "Sell hopper · Rust Valley", "Recycling Depot", UIKit.Amber);
            var cols = Columns(card);
            var left = Column(cols, 1.7f);
            var right = Column(cols, 1f, 12f);

            var head = UIKit.Row(left, 10f, 28);
            UIKit.Label(head, "RESOURCE", 14, UIKit.Muted, FontStyle.Bold).Flex();
            UIKit.Label(head, "IN BACKPACK", 14, UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleRight).Width(130);
            UIKit.Label(head, "UNIT PRICE", 14, UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleRight).Width(120);
            UIKit.Label(head, "TOTAL", 14, UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleRight).Width(120);
            UIKit.Label(head, "", 14, UIKit.Muted).Width(110);
            Divider(left);

            int grand = 0;
            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = inv.Count(r);
                grand += n * r.unitValue;
                var row = UIKit.Row(left, 10f, 54);
                UIKit.Swatch(row, r.color, 20);
                UIKit.Label(row, r.displayName, 21, UIKit.Ink, FontStyle.Bold).Flex();
                UIKit.Label(row, n.ToString(), 21, UIKit.Ink, FontStyle.Normal, TextAnchor.MiddleRight).Width(130);
                UIKit.Label(row, $"{r.unitValue} cr", 21, UIKit.Ink, FontStyle.Normal, TextAnchor.MiddleRight).Width(120);
                UIKit.Label(row, $"{n * r.unitValue} cr", 21, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight).Width(120);
                var res = r;
                var b = UIKit.Button(row, "Sell", () => { Trading.SellResource(res); Refresh(); }, false, 17, 44, n > 0);
                UIKit.Size(b.gameObject, 110, 44, 110, 44, 0f);
            }
            Divider(left);
            UIKit.Label(left, "Prices are fixed by OmniSalvage. Captain Bolt keeps 0%. For now.", 16, UIKit.Muted);

            UIKit.Label(right, "RECEIPT", 15, UIKit.Muted, FontStyle.Bold);
            var r1 = UIKit.Row(right, 8f);
            UIKit.Label(r1, $"{inv.TotalUnits} units", 19, UIKit.Muted).Flex();
            UIKit.Label(r1, $"{grand:N0} cr", 19, UIKit.Ink, FontStyle.Normal, TextAnchor.MiddleRight).Width(160);
            var r2 = UIKit.Row(right, 8f);
            UIKit.Label(r2, "Current balance", 19, UIKit.Muted).Flex();
            UIKit.Label(r2, $"{wallet.Credits:N0} cr", 19, UIKit.Ink, FontStyle.Normal, TextAnchor.MiddleRight).Width(160);
            var r3 = UIKit.Row(right, 8f);
            UIKit.Label(r3, "Balance after sale", 20, UIKit.Ink, FontStyle.Bold).Flex();
            UIKit.Label(r3, $"{wallet.Credits + grand:N0} cr", 30, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight).Width(200);
            UIKit.Button(right, grand > 0 ? $"Sell all for {grand:N0} cr" : "Backpack is empty", () => { Trading.SellAll(); Refresh(); }, true, 24, 64, grand > 0);

            // Stored loads: warehouse always, truck bin when parked nearby.
            var loads = new System.Collections.Generic.List<PlanetScrappers.Inventory.ScrapStorage>();
            var warehouse = PlanetScrappers.Inventory.ScrapStorage.Find("warehouse");
            if (warehouse != null) loads.Add(warehouse);
            var truck = SalvageTruck.Instance;
            var hopperPos = interactor != null && interactor.Current != null ? interactor.Current.transform.position : player.transform.position;
            bool truckNear = truck != null && (truck.transform.position - hopperPos).magnitude < 40f;
            if (truck != null) loads.Add(truck.Bin);
            foreach (var store in loads)
            {
                bool isTruck = truck != null && store == truck.Bin;
                bool canSell = store.TotalUnits > 0 && (!isTruck || truckNear);
                var row = UIKit.Row(right, 10f, 50);
                string note = isTruck && !truckNear ? "  (park within 40 m)" : "";
                UIKit.Label(row, $"{store.DisplayName}: {store.TotalUnits}/{store.Capacity} units · {store.TotalValue():N0} cr{note}", 17, UIKit.Ink, FontStyle.Bold).Flex();
                var s1 = store;
                var b = UIKit.Button(row, "Sell load", () => { Trading.SellStorage(s1); Refresh(); }, false, 16, 44, canSell);
                UIKit.Size(b.gameObject, 130, 44, 130, 44, 0f);
            }

            Spacer(right, 8);
            UIKit.Label(right, "RECENT TRANSACTIONS", 15, UIKit.Muted, FontStyle.Bold);
            int shown = 0;
            foreach (var t in wallet.Log)
            {
                if (shown++ >= 4) break;
                var row = UIKit.Row(right, 8f);
                UIKit.Label(row, $"{t.when}  {t.label}", 16, UIKit.Ink).Flex();
                UIKit.Label(row, (t.amount > 0 ? "+" : "") + t.amount.ToString("N0") + " cr", 16, t.amount > 0 ? UIKit.Amber : UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleRight).Width(120);
            }
            if (wallet.Log.Count == 0) UIKit.Label(right, "No transactions yet.", 16, UIKit.Muted);
        }

        // ───────────────────────── Upgrade Station ─────────────────────────

        void BuildShop()
        {
            var wallet = Wallet.Instance;
            var eq = PlayerEquipment.Instance;
            var inv = PlayerInventory.Instance;
            var card = Card(1640, 900, "Nova's workshop", "Upgrade Station", UIKit.Teal);

            var nova = UIKit.Row(card, 12f);
            UIKit.Label(nova, "<b>ENGINEER NOVA:</b>  Bring me credits, I'll bring you tools that respect you. And robots. And factories.", 19, UIKit.Teal).Flex();
            UIKit.Label(nova, $"Balance  {wallet.Credits:N0} cr", 26, UIKit.Amber, FontStyle.Bold, TextAnchor.MiddleRight).Width(320);

            var cols = Columns(card, 24f);
            var left = Column(cols, 1.15f, 8f);
            var right = Column(cols, 1f, 8f);

            UIKit.Label(left, "TOOLS", 15, UIKit.Muted, FontStyle.Bold);
            foreach (var t in GameDatabase.Instance.tools)
            {
                if (t == null) continue;
                string label;
                bool primary = false, enabled = false;
                var tool = t;
                UnityEngine.Events.UnityAction action = null;
                if (eq.Equipped == t) label = "Equipped";
                else if (eq.Owns(t)) { label = "Equip"; enabled = true; action = () => { eq.Equip(tool); Refresh(); }; }
                else if (!t.availableInPrototype) label = t.lockedReason;
                else
                {
                    label = $"Buy  {t.price:N0} cr";
                    enabled = wallet.CanAfford(t.price);
                    primary = enabled;
                    action = () => { if (Trading.BuyTool(tool) == PurchaseResult.Ok) GameEvents.Say(ShopLine(tool), 6f); Refresh(); };
                }
                ShopTile(left, $"{t.level}  {t.displayName}",
                    $"Tier {t.tier}  ·  {t.DamagePerSecond:0} dps  ·  reach {t.range:0.0} m  ·  +{t.recoveryBonus * 100f:0}% scrap  ·  {t.description}",
                    label, action, primary, enabled, eq.Equipped == t);
            }

            UIKit.Label(right, "BACKPACK", 15, UIKit.Muted, FontStyle.Bold);
            bool canPack = inv.CanUpgrade && wallet.CanAfford(inv.NextUpgradePrice);
            ShopTile(right, inv.CanUpgrade ? $"Bigger backpack: {inv.Capacity} → {inv.NextCapacity}" : $"Backpack: {inv.Capacity} units (largest)",
                "More pockets, fewer trips to the depot.",
                inv.CanUpgrade ? $"Buy  {inv.NextUpgradePrice:N0} cr" : "Max size",
                () => { if (Trading.BuyBackpack() == PurchaseResult.Ok) GameEvents.Say("More pockets. Same posture.", 4f); Refresh(); },
                canPack, canPack, false);

            UIKit.Label(right, "AUTOMATION", 15, UIKit.Muted, FontStyle.Bold);
            var factory = Machine.Find("factory");
            if (factory != null)
            {
                bool can = !factory.Built && wallet.CanAfford(factory.Price);
                ShopTile(right, "Recycling Factory",
                    factory.Built ? $"{factory.Status}  ·  switch it on or off at its control panel  ·  {factory.RunningCostPerMinute:0} cr/min while working"
                                  : $"Refines warehouse scrap for 30% more than the depot pays  ·  {factory.RunningCostPerMinute:0} cr/min while working",
                    factory.Built ? "Built" : $"Build  {factory.Price:N0} cr",
                    () => { if (Trading.BuyMachine(factory) == PurchaseResult.Ok) GameEvents.Say("Factory built south of the base. It's already switched on.", 6f); Refresh(); },
                    can, can, false);
            }

            var dock = PlanetScrappers.Automation.RobotDock.Instance;
            if (dock != null)
            {
                bool can = dock.Count < dock.MaxRobots && wallet.CanAfford(dock.Price);
                ShopTile(right, $"Salvage Robot  ({dock.Count}/{dock.MaxRobots})",
                    "Flies out, takes apart tier 1-3 junk and brings the scrap to the warehouse. No running cost.",
                    dock.Count >= dock.MaxRobots ? "Crew full" : $"Buy  {dock.Price:N0} cr",
                    () => { if (Trading.BuyRobot() == PurchaseResult.Ok) GameEvents.Say("Robot deployed from the dock by the warehouse.", 5f); Refresh(); },
                    can, can, false);
            }

            foreach (var mach in Machine.All)
            {
                if (!(mach is ConveyorLink link)) continue;
                bool can = !link.Built && link.CanBuild && wallet.CanAfford(link.Price);
                string state = link.Built ? "Built" : !link.CanBuild ? link.CannotBuildReason : $"Build  {link.Price:N0} cr";
                ShopTile(right, link.DisplayName,
                    link.Built ? $"{link.Status}  ·  carries collector scrap to the warehouse" : "Carries this collector's scrap to the warehouse on its own  ·  10 cr/min while moving",
                    state, () => { Trading.BuyMachine(link); Refresh(); }, can, can, false);
            }
        }

        void ShopTile(Transform parent, string title, string detail, string buttonLabel, UnityEngine.Events.UnityAction action, bool primary, bool enabled, bool highlight)
        {
            var tile = UIKit.Panel(parent, title, highlight ? new Color(1f, 0.9f, 0.82f, 0.9f) : new Color(1, 1, 1, 0.7f));
            UIKit.Outline(tile, UIKit.Line);
            UIKit.HLayout(tile.gameObject, 14f, 10);
            var text = Column(tile.transform, 1f, 1f);
            UIKit.Label(text, title, 19, enabled || highlight ? UIKit.Ink : UIKit.Muted, FontStyle.Bold);
            UIKit.Label(text, detail, 14, UIKit.Muted);
            var b = UIKit.Button(tile.transform, buttonLabel, action, primary, 16, 44, enabled);
            UIKit.Size(b.gameObject, 190, 44, 190, 44, 0f);
        }

        static string ShopLine(ToolDefinition t) => t.id switch
        {
            "scrap_hammer" => "Finally, a tool with self-esteem. Tier 2 means trucks and containers now.",
            "electric_dismantler" => "The Dismantler cuts in half the time. And Machine Row's gate just opened.",
            "plasma_cutter" => "Plasma Cutter fitted. The Spaceport Graveyard gate in the east fence just unlocked.",
            "industrial_crusher" => "The Crusher. Ship hulls, engines, mining rigs. The Deep Core gate in the west fence is open.",
            _ => "New tool fitted. Go break something expensive."
        };

        // ───────────────────────── Missions ─────────────────────────

        void BuildMissions()
        {
            var mm = MissionManager.Instance;
            var list = GameDatabase.Instance.missions;
            int chapterEnd = 8, chapterStart = 0;
            string chapterName = "First shift";
            if (mm.Index >= 8) { chapterStart = 8; chapterEnd = 14; chapterName = "Phase 2 contracts"; }
            if (mm.Index >= 14) { chapterStart = 14; chapterEnd = list.Length; chapterName = "Phase 3 contracts"; }
            var card = Card(1240, 780, $"{chapterName} · {mm.CompletedCount} of {list.Length} missions done", "Missions", UIKit.AccentText);
            var cols = Columns(card);
            var left = Column(cols, 1f, 8f);
            var right = Column(cols, 1.3f, 12f);

            for (int i = chapterStart; i < Mathf.Min(chapterEnd, list.Length); i++)
            {
                var m = list[i];
                bool done = i < mm.Index, active = i == mm.Index;
                var tile = UIKit.Panel(left, m.title, active ? new Color(1f, 0.9f, 0.82f, 0.9f) : new Color(1, 1, 1, 0.6f));
                UIKit.Outline(tile, active ? UIKit.Accent : UIKit.Line);
                UIKit.HLayout(tile.gameObject, 12f, 10);
                UIKit.Label(tile.transform, done ? "√" : m.number.ToString(), 20, done ? UIKit.Green : active ? UIKit.AccentText : UIKit.Muted, FontStyle.Bold, TextAnchor.MiddleCenter).Width(30);
                UIKit.Label(tile.transform, m.title, 19, done || active ? UIKit.Ink : UIKit.Muted, active ? FontStyle.Bold : FontStyle.Normal).Flex();
                UIKit.Label(tile.transform, done ? "Done" : active ? "Active" : "Locked", 15, UIKit.Muted, FontStyle.Normal, TextAnchor.MiddleRight).Width(70);
            }

            var cur = mm.Current;
            if (cur == null)
            {
                UIKit.Label(right, "ALL CONTRACTS COMPLETE", 15, UIKit.Green, FontStyle.Bold);
                UIKit.Label(right, $"All {list.Length} missions done", 36, UIKit.Ink, FontStyle.Bold);
                UIKit.Label(right, "Keep salvaging every zone. Captain Bolt drops fresh junk every few minutes, your robots and factory keep earning, and the Mega-Factory opens in the final phase.", 19, UIKit.Muted);
                return;
            }
            UIKit.Label(right, $"MISSION {cur.number} · ACTIVE", 15, UIKit.Muted, FontStyle.Bold);
            UIKit.Label(right, cur.title.ToUpperInvariant(), 36, UIKit.Ink, FontStyle.Bold);
            UIKit.Label(right, cur.description, 19, UIKit.Muted);
            UIKit.Label(right, "OBJECTIVE", 15, UIKit.Muted, FontStyle.Bold);
            UIKit.Label(right, cur.count > 1 ? $"{cur.objectiveText}   ({Mathf.Min(mm.Progress, cur.count)} / {cur.count})" : cur.objectiveText, 21, UIKit.Ink, FontStyle.Bold);
            UIKit.Label(right, "REWARD", 15, UIKit.Muted, FontStyle.Bold);
            UIKit.Label(right, $"+{cur.rewardCredits} credits", 24, UIKit.Amber, FontStyle.Bold);
            Spacer(right, 8);
            UIKit.Label(right, "<b>CAPTAIN BOLT:</b>  Every bolt you loosen goes toward my bonus. Our bonus. The company's bonus.", 17, UIKit.Amber);
        }

        // ───────────────────────── Pause ─────────────────────────

        void BuildPause()
        {
            var gm = GameManager.Instance;
            var card = Card(920, 640, "Shift in progress · Rust Valley", "Paused", UIKit.Amber, false);
            var cols = Columns(card);
            var left = Column(cols, 1f, 10f);
            var right = Column(cols, 1f, 12f);

            UIKit.Button(left, "Resume", Back, true, 24, 58);
            UIKit.Button(left, "Settings", () => Open(GameScreen.Settings), false, 22, 54);
            UIKit.Button(left, "Save Game", () => { gm.SaveGame(true); Refresh(); }, false, 22, 54);
            UIKit.Button(left, "Main Menu", GameManager.ReturnToMenu, false, 22, 54);
            UIKit.Button(left, "Exit to Desktop", GameManager.Quit, false, 22, 54);

            UIKit.Label(right, "THIS SESSION", 15, UIKit.Muted, FontStyle.Bold);
            Stat(right, $"{(int)(gm.SessionSeconds / 60)}:{(int)(gm.SessionSeconds % 60):00}", "Time played");
            Stat(right, gm.SessionDismantled.ToString(), "Objects dismantled");
            Stat(right, gm.SessionScrap.ToString(), "Scrap collected");
            Stat(right, $"{gm.SessionEarned:N0} cr", "Credits earned");
            if (gm.LastSavedAt != null) UIKit.Label(right, $"Last saved {gm.LastSavedAt}. The game also saves after every sale, purchase and mission.", 16, UIKit.Muted);
            var m = MissionManager.Instance.Current;
            UIKit.Label(right, m != null ? $"Active mission: {m.title}" : "All missions complete", 17, UIKit.AccentText, FontStyle.Bold);
        }

        static void Stat(Transform parent, string value, string label)
        {
            var row = UIKit.Row(parent, 12f);
            UIKit.Label(row, value, 30, UIKit.Ink, FontStyle.Bold).Width(170);
            UIKit.Label(row, label, 17, UIKit.Muted).Flex();
        }

        // ───────────────────────── Settings ─────────────────────────

        void BuildSettings()
        {
            var s = SettingsManager.Current;
            var card = Card(1100, 820, "Graphics · audio · controls", "Settings", UIKit.Muted);
            var resolutions = Screen.resolutions;

            Stepper(card, "Resolution", $"{s.resolutionWidth} × {s.resolutionHeight}", dir =>
            {
                if (resolutions.Length == 0) return;
                int i = System.Array.FindIndex(resolutions, r => r.width == s.resolutionWidth && r.height == s.resolutionHeight);
                i = (Mathf.Max(0, i) + dir + resolutions.Length) % resolutions.Length;
                s.resolutionWidth = resolutions[i].width;
                s.resolutionHeight = resolutions[i].height;
            });
            Stepper(card, "Display mode", SettingsManager.DisplayModeNames[Mathf.Clamp(s.displayMode, 0, 2)], dir => s.displayMode = (s.displayMode + dir + 3) % 3);
            Stepper(card, "Graphics quality", QualitySettings.names[Mathf.Clamp(s.quality, 0, QualitySettings.names.Length - 1)],
                dir => s.quality = Mathf.Clamp(s.quality + dir, 0, QualitySettings.names.Length - 1));
            Stepper(card, "Frame-rate cap", s.vSync ? "Off (V-sync on)" : s.frameRateCap <= 0 ? "Unlimited" : s.frameRateCap + " fps", dir =>
            {
                int i = System.Array.IndexOf(SettingsManager.FrameRateOptions, s.frameRateCap);
                i = (Mathf.Max(0, i) + dir + SettingsManager.FrameRateOptions.Length) % SettingsManager.FrameRateOptions.Length;
                s.frameRateCap = SettingsManager.FrameRateOptions[i];
            });
            Stepper(card, "V-sync", s.vSync ? "On" : "Off", dir => s.vSync = !s.vSync);
            Stepper(card, "Camera", s.firstPerson ? "First-person" : "Third-person (V switches)", dir => s.firstPerson = !s.firstPerson);
            Stepper(card, "Mouse sensitivity", s.mouseSensitivity.ToString("0.0"), dir => s.mouseSensitivity = Mathf.Clamp(Mathf.Round((s.mouseSensitivity + dir * 0.1f) * 10f) / 10f, 0.2f, 3f));
            Stepper(card, "Invert vertical look", s.invertY ? "On" : "Off", dir => s.invertY = !s.invertY);
            Stepper(card, "Master volume", Mathf.RoundToInt(s.masterVolume * 100f) + "%", dir => s.masterVolume = Mathf.Clamp01(Mathf.Round((s.masterVolume + dir * 0.1f) * 10f) / 10f));

            Spacer(card, 6);
            var foot = UIKit.Row(card, 12f);
            UIKit.Label(foot, "Changes apply straight away and are saved when you go back.", 16, UIKit.Muted).Flex();
            var reset = UIKit.Button(foot, "Reset to defaults", () =>
            {
                var fresh = new GameSettings
                {
                    resolutionWidth = Screen.currentResolution.width,
                    resolutionHeight = Screen.currentResolution.height,
                    quality = QualitySettings.names.Length - 1
                };
                CopySettings(fresh, s);
                SettingsManager.Apply(player.Camera);
                Refresh();
            }, false, 18, 48);
            UIKit.Size(reset.gameObject, 220, 48, 220, 48, 0f);
            var back = UIKit.Button(foot, "Back", Back, true, 20, 48);
            UIKit.Size(back.gameObject, 160, 48, 160, 48, 0f);
        }

        static void CopySettings(GameSettings from, GameSettings to)
        {
            to.resolutionWidth = from.resolutionWidth; to.resolutionHeight = from.resolutionHeight;
            to.displayMode = from.displayMode; to.quality = from.quality; to.frameRateCap = from.frameRateCap;
            to.vSync = from.vSync; to.mouseSensitivity = from.mouseSensitivity; to.invertY = from.invertY;
            to.firstPerson = from.firstPerson; to.masterVolume = from.masterVolume;
        }

        void Stepper(Transform parent, string label, string value, System.Action<int> change)
        {
            var row = UIKit.Row(parent, 12f, 50);
            UIKit.Label(row, label, 21, UIKit.Ink, FontStyle.Bold).Flex();
            var minus = UIKit.Button(row, "‹", () => { change(-1); SettingsManager.Apply(player.Camera); Refresh(); }, false, 26, 46);
            UIKit.Size(minus.gameObject, 56, 46, 56, 46, 0f);
            UIKit.Label(row, value, 20, UIKit.Ink, FontStyle.Normal, TextAnchor.MiddleCenter).Width(320);
            var plus = UIKit.Button(row, "›", () => { change(1); SettingsManager.Apply(player.Camera); Refresh(); }, false, 26, 46);
            UIKit.Size(plus.gameObject, 56, 46, 56, 46, 0f);
        }
    }
}
