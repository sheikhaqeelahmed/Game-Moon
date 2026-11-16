using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Missions;
using UnityEditor;
using UnityEngine;

namespace PlanetScrappers.EditorTools
{
    /// <summary>
    /// Creates (or updates) the ScriptableObject game data from the approved concept numbers:
    /// resources, tools, dismantlable objects and the eight first-shift missions.
    /// After this runs, designers tune values in the assets under Assets/ScriptableObjects.
    /// </summary>
    public static class GameDataBuilder
    {
        const string Root = "Assets/ScriptableObjects";

        [MenuItem("Planet Scrappers/Build Game Data", priority = 1)]
        public static GameDatabase Build()
        {
            Folder(Root);
            Folder(Root + "/Resources");
            Folder(Root + "/Tools");
            Folder(Root + "/Dismantlables");
            Folder(Root + "/Missions");
            Folder("Assets/Resources");

            var iron = Res("iron", "Iron", 2, 50, "Common", "#8E979E", "Plates, beams and bolts. Most of every machine.");
            var copper = Res("copper", "Copper", 5, 50, "Medium value", "#C8743A", "Wiring and pipes from vehicles and machines.");
            var elec = Res("electronics", "Electronics", 12, 25, "Valuable", "#2FA36B", "Circuit boards from robots, dashboards and terminals.");
            var alloy = Res("rare_alloy", "Rare Alloy", 40, 10, "Rare", "#9B7BFF", "Engine and core parts. Rare, heavy, very valuable.");

            var tools = new[]
            {
                Tool("rusty_wrench", "Rusty Wrench", 1, 1, 10f, 1.2f, 2.2f, 0f, 0, true, "Small objects. Heavy, loyal, rusty."),
                Tool("scrap_hammer", "Scrap Hammer", 2, 2, 22f, 1.0f, 2.4f, 0.10f, 250, true, "Metal components, trucks and containers."),
                Tool("electric_dismantler", "Electric Dismantler", 3, 3, 14f, 3.0f, 2.6f, 0.20f, 1200, true, "Fast pulses. Opens Machine Row."),
                Tool("plasma_cutter", "Plasma Cutter", 4, 4, 25f, 3.0f, 3.0f, 0.30f, 4500, false, "Large machinery.", "Iron District, Phase 2"),
                Tool("industrial_crusher", "Industrial Crusher", 5, 5, 260f, 0.5f, 3.2f, 0.40f, 12000, false, "Vehicles and heavy structures.", "Phase 3"),
                Tool("salvage_robot", "Automated Salvage Robot", 6, 5, 25f, 1.0f, 3.0f, 0f, 30000, false, "Collects on its own.", "Phase 3")
            };

            var objects = new[]
            {
                Obj("oil_drum", "Oil Drum", 1, 30f, 180f, Y(iron, 3)),
                Obj("vending_bot", "Busted Vending Bot", 1, 60f, 240f, Y(iron, 3), Y(copper, 2), Y(elec, 1)),
                Obj("hover_scooter", "Zipline-50 Hover Scooter", 1, 120f, 240f, Y(iron, 6), Y(copper, 3), Y(elec, 1)),
                Obj("cargo_container", "Cargo Container", 2, 300f, 300f, Y(iron, 18), Y(copper, 2)),
                Obj("mule7_truck", "Mule-7 Hauler Truck", 2, 450f, 300f, Y(iron, 20), Y(copper, 8), Y(elec, 3), Y(alloy, 1, 0.25f)),
                Obj("generator_unit", "Generator Unit", 3, 600f, 360f, Y(copper, 14), Y(elec, 6), Y(alloy, 1))
            };

            var missions = new[]
            {
                Mission(1, "Welcome to Rust Valley", "Captain Bolt wants every new hire signed in before they touch company junk.",
                    "Check in at Captain Bolt's terminal by the base door", ObjectiveType.Interact, "terminal", 1, 10,
                    "Morning, new hire. Bolt wants you signed in at the terminal by the base door. WASD to walk, mouse to look, E to use.",
                    "Paperwork done. Bolt says welcome. Bolt also says hurry."),
                Mission(2, "Find Your First Scrap", "Every Scrapper starts with a rusty wrench and a barrel that has it coming.",
                    "Pick up the rusty wrench and break an oil drum", ObjectiveType.Dismantle, "oil_drum", 1, 25,
                    "The wrench is on the crate with the warm light, just west of the road. Press E to grab it, then hold the left mouse button on a drum.",
                    "Look at it fly. Scrap pops out of everything you break."),
                Mission(3, "Collect 20 Iron", "Iron is cheap, but it's everywhere. Fill your pockets.",
                    "Collect 20 iron", ObjectiveType.CollectResource, "iron", 20, 40,
                    "Walk near scrap and it flies into your backpack. Collect 20 iron. Tab shows what you're carrying.",
                    "Twenty iron. You're practically a tycoon."),
                Mission(4, "Sell Your First Materials", "The Recycling Depot pays cash for anything in your backpack.",
                    "Sell scrap at the Recycling Depot hopper", ObjectiveType.SellAny, "", 1, 50,
                    "The Recycling Depot is the yellow-striped building at the base. Walk to the hopper and press E.",
                    "Credits! Don't spend them all on snacks."),
                Mission(5, "Upgrade Your Wrench", "Nova's Scrap Hammer handles tier 2 salvage: containers and trucks.",
                    "Buy the Scrap Hammer from Nova", ObjectiveType.BuyTool, "scrap_hammer", 1, 75,
                    "Nova's Upgrade Station has the teal stripe. The Scrap Hammer costs 250 credits. Break and sell a few more things first.",
                    "Finally, a tool with self-esteem."),
                Mission(6, "Dismantle Your First Vehicle", "The Mule-7 haulers in the Vehicle Graveyard are full of iron and copper wiring.",
                    "Take apart a Mule-7 hauler truck", ObjectiveType.Dismantle, "mule7_truck", 1, 150,
                    "Mule-7 trucks are in the Vehicle Graveyard, left at the crossroads. Hammer only, the wrench just bounces off.",
                    "It had a good life. Now it has a good price."),
                Mission(7, "Expand Your Backpack", "Fewer trips, more scrap. Nova sells a 50-unit backpack.",
                    "Buy a bigger backpack from Nova", ObjectiveType.BuyBackpack, "", 1, 100,
                    "Your backpack fills fast now. Nova sells a 50-unit one for 300 credits.",
                    "More pockets. Same posture."),
                Mission(8, "Unlock Industrial Machinery", "Machine Row's generators need a tier 3 tool. Nova's Electric Dismantler opens the gate.",
                    "Buy the Electric Dismantler", ObjectiveType.BuyTool, "electric_dismantler", 1, 300,
                    "Machine Row needs a tier 3 tool. Save up 1,200 credits for Nova's Electric Dismantler. Containers and trucks pay best.",
                    "The gate's open. Bolt says there's a whole Iron District past this one. Bolt says a lot.")
            };

            var db = Load<GameDatabase>("Assets/Resources/GameDatabase.asset");
            db.startingCredits = 100;
            db.backpackCapacities = new[] { 30, 50, 80, 120 };
            db.backpackPrices = new[] { 0, 300, 900, 2500 };
            db.resources = new[] { iron, copper, elec, alloy };
            db.tools = tools;
            db.dismantlables = objects;
            db.missions = missions;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameData] Game database built.");
            return db;
        }

        static ScrapYield Y(ResourceDefinition r, int amount, float chance = 1f) =>
            new ScrapYield { resource = r, amount = amount, chance = chance };

        static ResourceDefinition Res(string id, string name, int value, int stack, string rarity, string hex, string desc)
        {
            var r = Load<ResourceDefinition>($"{Root}/Resources/{name.Replace(" ", "")}.asset");
            r.id = id; r.displayName = name; r.unitValue = value; r.stackLimit = stack; r.rarity = rarity; r.description = desc;
            ColorUtility.TryParseHtmlString(hex, out r.color);
            EditorUtility.SetDirty(r);
            return r;
        }

        static ToolDefinition Tool(string id, string name, int level, int tier, float dmg, float hps, float range, float bonus, int price, bool available, string desc, string locked = "")
        {
            var t = Load<ToolDefinition>($"{Root}/Tools/{level}_{name.Replace(" ", "")}.asset");
            t.id = id; t.displayName = name; t.level = level; t.tier = tier; t.damage = dmg; t.hitsPerSecond = hps;
            t.range = range; t.recoveryBonus = bonus; t.price = price; t.availableInPrototype = available; t.description = desc;
            t.lockedReason = string.IsNullOrEmpty(locked) ? "Not available yet" : locked;
            EditorUtility.SetDirty(t);
            return t;
        }

        static DismantlableDefinition Obj(string id, string name, int tier, float durability, float respawn, params ScrapYield[] yields)
        {
            var d = Load<DismantlableDefinition>($"{Root}/Dismantlables/{id}.asset");
            d.id = id; d.displayName = name; d.tier = tier; d.durability = durability; d.respawnSeconds = respawn; d.yields = yields;
            EditorUtility.SetDirty(d);
            return d;
        }

        static MissionDefinition Mission(int n, string title, string desc, string objective, ObjectiveType type, string target, int count, int reward, string intro, string complete)
        {
            var m = Load<MissionDefinition>($"{Root}/Missions/Mission{n:00}.asset");
            m.number = n; m.title = title; m.description = desc; m.objectiveText = objective; m.type = type;
            m.targetId = target; m.count = count; m.rewardCredits = reward; m.botIntro = intro; m.botComplete = complete;
            EditorUtility.SetDirty(m);
            return m;
        }

        static T Load<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
