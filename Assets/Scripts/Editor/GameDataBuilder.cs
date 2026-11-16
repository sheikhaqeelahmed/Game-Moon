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
                Tool("plasma_cutter", "Plasma Cutter", 4, 4, 25f, 3.0f, 3.0f, 0.30f, 4500, true, "Large machinery and assembly lines."),
                Tool("industrial_crusher", "Industrial Crusher", 5, 5, 260f, 0.5f, 3.4f, 0.40f, 12000, true, "Ship hulls, engines and mining rigs."),
                Tool("salvage_robot", "Automated Salvage Robot", 6, 3, 27f, 1.0f, 3.0f, 0f, 6000, false, "Drones that salvage on their own.", "Buy in Automation →")
            };

            var objects = new[]
            {
                Obj("oil_drum", "Oil Drum", 1, 30f, 180f, Y(iron, 3)),
                Obj("vending_bot", "Busted Vending Bot", 1, 60f, 240f, Y(iron, 3), Y(copper, 2), Y(elec, 1)),
                Obj("hover_scooter", "Zipline-50 Hover Scooter", 1, 120f, 240f, Y(iron, 6), Y(copper, 3), Y(elec, 1)),
                Obj("cargo_container", "Cargo Container", 2, 300f, 300f, Y(iron, 18), Y(copper, 2)),
                Obj("mule7_truck", "Mule-7 Hauler Truck", 2, 450f, 300f, Y(iron, 20), Y(copper, 8), Y(elec, 3), Y(alloy, 1, 0.25f)),
                Obj("generator_unit", "Generator Unit", 3, 600f, 360f, Y(copper, 14), Y(elec, 6), Y(alloy, 1)),
                Obj("robot_arm", "Robot Arm", 3, 500f, 300f, Y(copper, 8), Y(elec, 8), Y(alloy, 1, 0.5f)),
                Obj("hydraulic_press", "Hydraulic Press", 3, 800f, 360f, Y(iron, 25), Y(copper, 10), Y(elec, 4)),
                Obj("assembly_line", "Assembly Line", 4, 1400f, 420f, Y(iron, 40), Y(copper, 16), Y(elec, 10), Y(alloy, 2)),
                Obj("factory_crane", "Factory Crane", 4, 2000f, 480f, Y(iron, 70), Y(copper, 12), Y(alloy, 3)),
                Obj("cargo_pod", "Cargo Pod", 3, 450f, 300f, Y(iron, 8), Y(copper, 4), Y(elec, 6)),
                Obj("ship_nose", "Freighter Nose", 4, 1500f, 600f, Y(iron, 30), Y(copper, 6), Y(elec, 10)),
                Obj("ship_wing", "Freighter Wing", 4, 1200f, 600f, Y(iron, 35), Y(copper, 8), Y(alloy, 1)),
                Obj("shuttle_cabin", "Shuttle Cabin", 4, 1000f, 540f, Y(iron, 15), Y(copper, 6), Y(elec, 12)),
                Obj("ship_hull", "Freighter Hull Section", 5, 2600f, 720f, Y(iron, 70), Y(copper, 20), Y(alloy, 2)),
                Obj("ship_engine", "Freighter Engine", 5, 3500f, 720f, Y(copper, 30), Y(elec, 16), Y(alloy, 6)),
                Obj("mine_cart", "Mine Cart", 3, 400f, 300f, Y(iron, 20), Y(copper, 6)),
                Obj("crystal_vein", "Crystal Vein", 4, 900f, 480f, Y(alloy, 5), Y(elec, 2)),
                Obj("ore_crusher", "Ore Crusher", 5, 2400f, 600f, Y(iron, 60), Y(copper, 18), Y(elec, 6), Y(alloy, 2)),
                Obj("drilling_rig", "Drilling Rig", 5, 2800f, 720f, Y(iron, 50), Y(copper, 24), Y(elec, 12), Y(alloy, 4)),
                Obj("smelter", "Mega-Smelter", 5, 3000f, 720f, Y(iron, 80), Y(copper, 30), Y(alloy, 3)),
                Obj("factory_pylon", "Power Pylon", 5, 3000f, 0f, Y(copper, 20), Y(elec, 20), Y(alloy, 2)),
                Obj("reactor_core", "Mega-Factory Reactor Core", 5, 9000f, 0f, Y(copper, 40), Y(elec, 40), Y(alloy, 25))
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
                    "The gate's open. Bolt says there's a whole Iron District past this one. Bolt says a lot."),
                Mission(9, "Take the Wheel", "The SH-01 salvage truck carries 150 units and vacuums scrap as you drive.",
                    "Get in the salvage truck parked by the base", ObjectiveType.Interact, "truck", 1, 100,
                    "New contract from Bolt: use the company truck. It's parked east of the base road. Walk up and press E.",
                    "Seatbelt optional. Physics, sadly, is not."),
                Mission(10, "Haul Day", "Fill the truck's bin by driving over loose scrap, then sell the load at the depot.",
                    "Collect 40 units of scrap in the truck bin", ObjectiveType.CollectToStorage, "truck", 40, 150,
                    "Break things, then drive over the scrap. The bin pulls it in. Park within 40 metres of the depot to sell the load.",
                    "Forty units without bending over once. Progress."),
                Mission(11, "Set Up a Collector", "Scrap Collectors pull in loose scrap on their own. They are the start of your automated operation.",
                    "Build a Scrap Collector on a collector pad", ObjectiveType.Interact, "build_collector", 1, 200,
                    "There are collector pads in the Vehicle Graveyard, the Container Yard and the debris field. A collector costs 800 credits.",
                    "It works while you don't. Bolt calls that management."),
                Mission(12, "Into the Iron District", "North of Machine Row, through the gap in the fence, the old factories are full of robot arms and presses.",
                    "Dismantle a robot arm in the Iron District", ObjectiveType.Dismantle, "robot_arm", 1, 250,
                    "Walk through Machine Row and out the north gap in the fence. Robot arms need your Electric Dismantler.",
                    "Welcome to the Iron District. Mind the presses."),
                Mission(13, "Cutting Edge", "Assembly lines and factory cranes are tier 4. Nova's Plasma Cutter slices through them.",
                    "Buy the Plasma Cutter from Nova", ObjectiveType.BuyTool, "plasma_cutter", 1, 300,
                    "Tier 4 machines need the Plasma Cutter: 4,500 credits at Nova's. Presses and robot arms pay well.",
                    "Plasma. Now we're cooking. Literally, a little."),
                Mission(14, "Line Break", "Take apart a full assembly line to prove the Iron District can be cleared.",
                    "Dismantle an assembly line", ObjectiveType.Dismantle, "assembly_line", 1, 600,
                    "Assembly lines run along the big factory halls. Bring the Plasma Cutter.",
                    "That's the Iron District open for business. Spaceport Graveyard is next. Bolt's already counting."),
                Mission(15, "Power Up", "The Recycling Factory refines warehouse scrap and pays 30% more than the depot. It costs credits to run.",
                    "Build the Recycling Factory (Nova's Automation tab)", ObjectiveType.Interact, "build_factory", 1, 400,
                    "Phase 3, Scrapper. Nova can build a Recycling Factory south of the base for 6,000 credits. It turns your warehouse into income.",
                    "Factory online. Feed the warehouse and watch the credits trickle in. The running cost comes out of your wallet."),
                Mission(16, "Robot Crew", "Salvage robots fly out, take apart tier 1-3 junk and bring the scrap to the warehouse.",
                    "Buy a Salvage Robot (Nova's Automation tab)", ObjectiveType.Interact, "buy_robot", 1, 500,
                    "Bolt wants a robot crew. Nova sells salvage drones for 6,000 credits each, up to three.",
                    "A robot that works without complaining. I'm not jealous. I'm not."),
                Mission(17, "Spaceport Graveyard", "East of the valley, past the gate, crashed freighters wait to be cut apart section by section.",
                    "Cut the nose off the crashed freighter", ObjectiveType.Dismantle, "ship_nose", 1, 600,
                    "The Spaceport Graveyard gate opens for tier 4 tools. Take the road east from the crossroads. Start with the freighter's nose.",
                    "One nose down. The rest of the ship is tier 5. You'll need something bigger."),
                Mission(18, "Heavy Metal", "Hull sections, engines and mining rigs are tier 5. Only the Industrial Crusher can handle them.",
                    "Buy the Industrial Crusher from Nova", ObjectiveType.BuyTool, "industrial_crusher", 1, 800,
                    "The Industrial Crusher costs 12,000 credits. Robots and the factory help you get there faster.",
                    "Now that is a tool. Try not to crush anything we need."),
                Mission(19, "Engine Room", "The freighter's engine is full of copper, electronics and rare alloy.",
                    "Dismantle the freighter's engine", ObjectiveType.Dismantle, "ship_engine", 1, 1200,
                    "The engine sits at the freighter's tail end. Big payout, lots of rare alloy.",
                    "Rare alloy everywhere. Bolt is smiling. It's unsettling."),
                Mission(20, "Deep Core", "West of the valley, the Deep Core mine: drilling rigs and crystal veins. Oxygen drains fast underground.",
                    "Dismantle a drilling rig in the Deep Core", ObjectiveType.Dismantle, "drilling_rig", 1, 2000,
                    "The Deep Core gate in the west fence opens for tier 5 tools. Fill your oxygen at base first. It drains twice as fast down there.",
                    "Deep Core cleared. Bolt just called: the Mega-Factory gate in the south fence is open. This is the big one."),
                Mission(21, "The Mega-Factory", "South of the base stands the Mega-Factory, the last and largest structure on Kessler-9.",
                    "Dismantle a Mega-Smelter inside the Mega-Factory", ObjectiveType.Dismantle, "smelter", 1, 2500,
                    "The Mega-Factory gate is in the south fence, behind the base. Smelters line the walls inside. Crusher only.",
                    "The smelters are falling. The core in the middle is shielded, though."),
                Mission(22, "Pull the Plug", "Four power pylons feed the reactor core's shield. All four must come down.",
                    "Dismantle all four power pylons", ObjectiveType.Dismantle, "factory_pylon", 4, 3000,
                    "Four pylons surround the reactor core. Each one you break weakens the shield. They don't come back.",
                    "Shield's down. Nothing protects the core now. Except physics. And Bolt's insurance."),
                Mission(23, "Core Breach", "Take apart the Mega-Factory's reactor core: the final contract on Kessler-9.",
                    "Dismantle the Mega-Factory reactor core", ObjectiveType.Dismantle, "reactor_core", 1, 10000,
                    "The core is the biggest thing you'll ever take apart. 9,000 durability. Bring the Crusher and patience.",
                    "That's it. The Mega-Factory is scrap. Kessler-9 is officially cleared. Captain Bolt is crying. Happy tears, I think.")
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
