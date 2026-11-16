using System.IO;
using PlanetScrappers.Companion;
using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.UI;
using PlanetScrappers.Environment;
using PlanetScrappers.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetScrappers.EditorTools
{
    /// <summary>
    /// Builds the Rust Valley grey-box scene from the approved concept map.
    /// Everything (materials, generated textures, scene) is created from code so the
    /// layout is reproducible and easy to review. Re-running overwrites the scene.
    ///
    /// Map → world: 1 map pixel = 0.35 m, map centre (320, 200) = world origin, north = +Z.
    /// Playable area ≈ 224 × 140 m.
    /// </summary>
    public static class RustValleyGreyboxBuilder
    {
        const string ScenePath = "Assets/Scenes/RustValley.unity";
        const string MaterialDir = "Assets/Art/Materials/Greybox";
        const string TextureDir = "Assets/Art/Textures/Generated";

        const float HalfWidth = 115f;  // x extent of the fence
        const float HalfDepth = 75f;   // z extent of the fence

        static Font signFont;
        static GameDatabase db;
        static Mats m;

        class Mats
        {
            public Material Regolith, Road, Rim, CraterFloor, BaseWhite, SuitOrange, SuitOrangeDark, SuitGrey, VisorBlack, TrimOrange, TrimTeal, TrimYellow, Glass, Screen,
                Rust, RustDark, Metal, MetalDark, Rubber, ContainerRed, ContainerTeal, ContainerBlue, VendingRed,
                ScooterOrange, TruckGrey, LockedGrey, Hazard, Halcyon, BotYellow, BotWhite, BotEye, WrenchRust, Fence;
        }

        [MenuItem("Planet Scrappers/Build Rust Valley Greybox", priority = 0)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        /// <summary>Entry point for batch mode: Unity.exe -batchmode -executeMethod ...BuildFromCommandLine -quit</summary>
        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
                Debug.LogWarning("[Greybox] No render pipeline asset assigned. Materials fall back to the Standard shader. Assign the URP asset in Project Settings > Graphics.");

            EnsureFolder(MaterialDir);
            EnsureFolder(TextureDir);
            EnsureFolder("Assets/Scenes");

            signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signMaterial = null;
            db = GameDataBuilder.Build();
            m = CreateMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();
            BuildGround();
            BuildRoads();
            BuildPerimeter();
            BuildBaseCluster();
            BuildVehicleGraveyard();
            BuildContainerYard();
            BuildDebrisFields();
            BuildMachineRow();
            BuildSky();
            var player = BuildPlayer();
            BuildScrapBot(player);

            MakeDismantlable();
            WireGameplay(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Greybox] Rust Valley built and saved to {ScenePath}");
        }

        // ───────────────────────────── Map helpers ─────────────────────────────

        /// <summary>Converts concept-map pixel coordinates to world XZ.</summary>
        static Vector3 MapToWorld(float mapX, float mapY, float y = 0f) =>
            new Vector3((mapX - 320f) * 0.35f, y, (200f - mapY) * 0.35f);

        // ───────────────────────────── Lighting & sky ─────────────────────────────

        static void SetupLighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.95f; // no atmosphere: deep, crisp shadows
            // Sun in the north-east, so building fronts (which face north into the valley) are lit.
            sunGo.transform.rotation = Quaternion.Euler(32f, 215f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.25f, 0.33f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.27f, 0.29f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.11f);
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        static void BuildSky()
        {
            var stars = GenerateStarfield();
            var skyMat = SaveMaterial(new Material(Shader.Find("Skybox/Panoramic")), "Sky_Starfield");
            skyMat.SetTexture("_MainTex", stars);
            skyMat.SetFloat("_Exposure", 1f);
            RenderSettings.skybox = skyMat;

            // Halcyon: big, low on the north-east horizon, as in the approved horizon reference.
            var halcyon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halcyon.name = "Halcyon (sky planet)";
            Object.DestroyImmediate(halcyon.GetComponent<Collider>());
            Vector3 dir = Quaternion.Euler(-9f, 25f, 0f) * Vector3.forward;
            halcyon.transform.position = dir * 1500f;
            halcyon.transform.localScale = Vector3.one * 520f;
            halcyon.transform.rotation = Quaternion.Euler(15f, 0f, 20f);
            var r = halcyon.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.Halcyon;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            halcyon.AddComponent<SlowSpin>();
        }

        // ───────────────────────────── Ground & boundary ─────────────────────────────

        static void BuildGround()
        {
            var root = new GameObject("Terrain").transform;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Regolith";
            ground.transform.SetParent(root);
            ground.transform.localScale = new Vector3(200f, 1f, 200f); // 2 × 2 km, edge never visible
            ground.GetComponent<Renderer>().sharedMaterial = m.Regolith;
            MarkStatic(ground);

            // Outer crater wall: overlapping slabs of rock tilted towards the valley, well beyond the fence.
            var rim = new GameObject("Crater Rim").transform;
            rim.SetParent(root);
            var rng = new System.Random(9);
            int count = 110;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                float rx = 180f + (float)rng.NextDouble() * 25f;
                float rz = 130f + (float)rng.NextDouble() * 20f;
                var pos = new Vector3(Mathf.Cos(a) * rx, -1f, Mathf.Sin(a) * rz);
                float s = 16f + (float)rng.NextDouble() * 16f;
                var rock = Primitive(PrimitiveType.Cube, "RimRock", rim, pos,
                    new Vector3(s * 1.7f, s * (0.35f + (float)rng.NextDouble() * 0.4f), s), m.Rim, collider: false);
                float facing = -a * Mathf.Rad2Deg + 90f; // long side along the ring
                rock.transform.rotation = Quaternion.Euler(-12f + (float)rng.NextDouble() * 24f, facing + (float)rng.NextDouble() * 30f - 15f, -10f + (float)rng.NextDouble() * 20f);
                MarkStatic(rock);
            }

            // A few small walk-around craters inside the valley.
            BuildCrater(root, MapToWorld(110, 330), 7f);
            BuildCrater(root, MapToWorld(600, 200), 9f);
            BuildCrater(root, MapToWorld(250, 140), 5f);
            BuildCrater(root, MapToWorld(420, 240), 4f);
            // Larger craters outside the fence for the view.
            BuildCrater(root, new Vector3(-150f, 0f, 40f), 22f);
            BuildCrater(root, new Vector3(140f, 0f, -90f), 18f);
            BuildCrater(root, new Vector3(30f, 0f, 110f), 26f);
        }

        static void BuildCrater(Transform parent, Vector3 centre, float radius)
        {
            var crater = new GameObject("Crater").transform;
            crater.SetParent(parent);
            crater.position = centre;
            int stones = Mathf.Clamp(Mathf.RoundToInt(radius * 2.2f), 10, 48);
            for (int i = 0; i < stones; i++)
            {
                float a = i / (float)stones * Mathf.PI * 2f;
                var p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                float size = radius * 0.45f;
                var stone = Primitive(PrimitiveType.Sphere, "RimStone", crater, p,
                    new Vector3(size, size * 0.28f, size * 0.8f), m.Rim, collider: false);
                stone.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                MarkStatic(stone);
            }
            // Dark floor disc reads as a shallow bowl in flat grey-box lighting.
            var floor = Primitive(PrimitiveType.Cylinder, "CraterFloor", crater, centre + Vector3.up * 0.01f,
                new Vector3(radius * 1.9f, 0.01f, radius * 1.9f), m.CraterFloor, collider: false);
            MarkStatic(floor);
        }

        static void BuildPerimeter()
        {
            var root = new GameObject("Perimeter Fence").transform;
            const float spacing = 5f;
            const float postHeight = 1.8f;

            void Side(Vector3 from, Vector3 to)
            {
                float length = Vector3.Distance(from, to);
                int posts = Mathf.CeilToInt(length / spacing);
                Vector3 step = (to - from) / posts;
                for (int i = 0; i <= posts; i++)
                {
                    var p = from + step * i;
                    var post = Primitive(PrimitiveType.Cylinder, "Post", root, p + Vector3.up * postHeight * 0.5f,
                        new Vector3(0.18f, postHeight * 0.5f, 0.18f), (i % 4 == 0) ? m.Hazard : m.Fence, collider: false);
                    MarkStatic(post);
                }
                Vector3 mid = (from + to) * 0.5f;
                float yaw = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
                foreach (float h in new[] { 0.7f, 1.5f })
                {
                    var rail = Box("Rail", root, mid + Vector3.up * h, new Vector3(0.08f, 0.08f, length), m.Fence, yaw, collider: false);
                    MarkStatic(rail);
                }
                // Invisible wall keeps the player inside the playable area (QA-010).
                var wall = new GameObject("BoundaryWall");
                wall.transform.SetParent(root);
                wall.transform.SetPositionAndRotation(mid + Vector3.up * 5f, Quaternion.Euler(0f, yaw, 0f));
                var col = wall.AddComponent<BoxCollider>();
                col.size = new Vector3(1f, 10f, length + 1f);
            }

            var sw = new Vector3(-HalfWidth, 0f, -HalfDepth);
            var se = new Vector3(HalfWidth, 0f, -HalfDepth);
            var ne = new Vector3(HalfWidth, 0f, HalfDepth);
            var nw = new Vector3(-HalfWidth, 0f, HalfDepth);
            Side(sw, se);
            Side(se, ne);
            Side(ne, nw);
            Side(nw, sw);
        }

        static void BuildRoads()
        {
            var root = new GameObject("Roads").transform;
            void Road(Vector3 a, Vector3 b)
            {
                Vector3 mid = (a + b) * 0.5f + Vector3.up * 0.02f;
                float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
                float len = Vector3.Distance(a, b) + 7f;
                var road = Box("Road", root, mid, new Vector3(7f, 0.04f, len), m.Road, yaw, collider: false);
                MarkStatic(road);
            }

            Vector3 basePt = MapToWorld(320, 330);
            Vector3 plaza = MapToWorld(320, 205);
            Road(basePt, plaza);
            Road(plaza, MapToWorld(150, 205));
            Road(MapToWorld(150, 205), MapToWorld(150, 150));
            Road(plaza, MapToWorld(500, 205));
            Road(MapToWorld(500, 205), MapToWorld(500, 150));
            Road(plaza, MapToWorld(320, 100));
            // Service road linking the base cluster.
            Road(MapToWorld(200, 290), MapToWorld(550, 290));
        }

        // ───────────────────────────── Base cluster ─────────────────────────────

        static void BuildBaseCluster()
        {
            var root = new GameObject("Base Cluster").transform;

            // Base: spawn, Captain Bolt's terminal, save point (Module 4).
            var basePos = MapToWorld(320, 340);
            Building(root, "Base", basePos, new Vector3(20f, 6f, 12f), m.TrimOrange, "BASE");
            var baseCentre = new GameObject("Base Centre").transform;
            baseCentre.SetParent(root, true);
            baseCentre.SetPositionAndRotation(basePos + new Vector3(0f, 0.1f, 9f), Quaternion.identity);
            var terminal = Box("Bolt Terminal", root, basePos + new Vector3(-5f, 0.75f, 7.5f), new Vector3(1.2f, 1.5f, 0.6f), m.MetalDark);
            terminal.AddComponent<StationInteractable>().Setup("terminal", "Missions terminal (Captain Bolt)");
            BoxLocal("Terminal Screen", terminal.transform, new Vector3(0f, 0.15f, 0.51f), new Vector3(0.7f, 0.35f, 0.05f), m.Screen, collider: false);
            Sign(root, "MISSIONS", basePos + new Vector3(-5f, 2f, 7.6f), 180f, 0.5f);

            // Upgrade Station (Nova).
            var upgradePos = MapToWorld(200, 335);
            Building(root, "Upgrade Station", upgradePos, new Vector3(14f, 5f, 10f), m.TrimTeal, "UPGRADES");
            var bench = Box("Workbench", root, upgradePos + new Vector3(0f, 0.5f, 6.5f), new Vector3(3f, 1f, 1.2f), m.Metal);
            bench.AddComponent<StationInteractable>().Setup("shop", "Upgrade Station (Nova)");
            Sign(root, "NOVA'S UPGRADES", upgradePos + new Vector3(0f, 1.6f, 7.2f), 180f, 0.45f);

            // Recycling Depot with sell hopper.
            var depotPos = MapToWorld(440, 335);
            Building(root, "Recycling Depot", depotPos, new Vector3(14f, 5f, 10f), m.TrimYellow, "RECYCLING");
            var hopper = new GameObject("Sell Hopper").transform;
            hopper.gameObject.AddComponent<StationInteractable>().Setup("depot", "Sell scrap at the Recycling Depot");
            hopper.SetParent(root);
            hopper.position = depotPos + new Vector3(0f, 0f, 7.5f);
            Box("Hopper Base", hopper, hopper.position + Vector3.up * 0.6f, new Vector3(2.4f, 1.2f, 2.4f), m.MetalDark);
            var funnel = Box("Hopper Funnel", hopper, hopper.position + Vector3.up * 1.6f, new Vector3(2.4f, 0.8f, 2.4f), m.Hazard);
            funnel.transform.localScale = new Vector3(3.2f, 0.8f, 3.2f);

            // Warehouse (storage later).
            var warehousePos = MapToWorld(550, 330);
            Building(root, "Warehouse", warehousePos, new Vector3(18f, 8f, 16f), m.TrimOrange, "STORAGE");

            // Starter salvage truck (drivable in a later milestone).
            var truck = new GameObject("Starter Salvage Truck").transform;
            truck.SetParent(root);
            truck.position = MapToWorld(347, 274);
            truck.rotation = Quaternion.Euler(0f, 90f, 0f);
            BoxLocal("Body", truck, new Vector3(0f, 1.4f, 0f), new Vector3(2.3f, 1.8f, 4.8f), m.BaseWhite);
            BoxLocal("Stripe", truck, new Vector3(0f, 1.2f, 0f), new Vector3(2.34f, 0.35f, 4.84f), m.TrimOrange, collider: false);
            BoxLocal("Windscreen", truck, new Vector3(0f, 1.9f, 2.41f), new Vector3(2f, 0.6f, 0.02f), m.Glass, collider: false);
            BoxLocal("Roof Rack", truck, new Vector3(0f, 2.4f, -0.3f), new Vector3(2f, 0.15f, 3.4f), m.MetalDark, collider: false);
            Wheels(truck, 1.15f, 1.6f, 0.45f);

            // Rusty wrench pickup (becomes interactive in Module 2).
            var wrenchPos = MapToWorld(284, 275);
            var crate = Box("Wrench Crate", root, wrenchPos + Vector3.up * 0.4f, new Vector3(1.2f, 0.8f, 0.8f), m.Rust);
            var wrench = new GameObject("Rusty Wrench (pickup)").transform;
            wrench.SetParent(root);
            wrench.position = wrenchPos + new Vector3(0f, 0.86f, 0f);
            wrench.rotation = Quaternion.Euler(0f, 30f, 0f);
            BoxLocal("Handle", wrench, Vector3.zero, new Vector3(0.12f, 0.06f, 0.9f), m.WrenchRust, collider: false);
            BoxLocal("JawL", wrench, new Vector3(-0.08f, 0f, 0.5f), new Vector3(0.06f, 0.06f, 0.2f), m.WrenchRust, collider: false);
            BoxLocal("JawR", wrench, new Vector3(0.08f, 0f, 0.5f), new Vector3(0.06f, 0.06f, 0.2f), m.WrenchRust, collider: false);
            var glow = new GameObject("Pickup Light").AddComponent<Light>();
            glow.transform.SetParent(wrench, false);
            glow.transform.localPosition = Vector3.up * 0.8f;
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.3f);
            glow.range = 4f;
            glow.intensity = 2f;
            crate.AddComponent<ToolPickup>().Setup(db.Tool("rusty_wrench"), wrench.gameObject);
        }

        static void Building(Transform parent, string name, Vector3 groundPos, Vector3 size, Material trim, string label)
        {
            var b = new GameObject(name).transform;
            b.SetParent(parent);
            b.position = groundPos;
            MarkStatic(BoxLocal("Shell", b, new Vector3(0f, size.y * 0.5f, 0f), size, m.BaseWhite));
            MarkStatic(BoxLocal("Trim", b, new Vector3(0f, size.y * 0.72f, 0f), new Vector3(size.x + 0.06f, 0.5f, size.z + 0.06f), trim, collider: false));
            MarkStatic(BoxLocal("Roof Vents", b, new Vector3(0f, size.y + 0.25f, 0f), new Vector3(size.x * 0.6f, 0.5f, size.z * 0.3f), m.MetalDark, collider: false));
            MarkStatic(BoxLocal("Door", b, new Vector3(0f, 1.3f, size.z * 0.5f + 0.02f), new Vector3(2.2f, 2.6f, 0.05f), m.MetalDark, collider: false));
            for (int i = -1; i <= 1; i += 2)
                MarkStatic(BoxLocal("Window", b, new Vector3(i * size.x * 0.3f, size.y * 0.45f, size.z * 0.5f + 0.02f), new Vector3(size.x * 0.18f, 1.6f, 0.05f), m.Glass, collider: false));
            Sign(b, label, groundPos + new Vector3(0f, size.y * 0.72f + 0.9f, size.z * 0.5f + 0.1f), 180f, 1f);
        }

        // ───────────────────────────── Salvage areas ─────────────────────────────

        static Transform salvageRoot;

        static Transform SalvageRoot()
        {
            if (salvageRoot == null) salvageRoot = new GameObject("Salvage").transform;
            return salvageRoot;
        }

        static void BuildVehicleGraveyard()
        {
            salvageRoot = null;
            var area = new GameObject("Vehicle Graveyard").transform;
            area.SetParent(SalvageRoot());
            Sign(area, "VEHICLE GRAVEYARD", MapToWorld(150, 165, 3f), 0f, 0.8f);

            Scooter(area, MapToWorld(86, 140), 20f, 1);
            Scooter(area, MapToWorld(108, 132), -35f, 2);
            Scooter(area, MapToWorld(130, 146), 70f, 3);
            Scooter(area, MapToWorld(224, 132), 160f, 4);
            Mule7(area, MapToWorld(168, 128), 15f, 1);
            Mule7(area, MapToWorld(198, 140), -20f, 2);
            Drum(area, MapToWorld(75, 120), 1);
            Drum(area, MapToWorld(240, 150), 2);
        }

        static void BuildContainerYard()
        {
            var area = new GameObject("Container Yard").transform;
            area.SetParent(SalvageRoot());
            Sign(area, "CONTAINER YARD", MapToWorld(500, 165, 3f), 0f, 0.8f);

            Container(area, MapToWorld(422, 128), 90f, m.ContainerRed, 1);
            Container(area, MapToWorld(446, 128), 90f, m.ContainerTeal, 2);
            Container(area, MapToWorld(434, 128, 2.6f), 90f, m.ContainerBlue, 3);
            Container(area, MapToWorld(536, 132), 5f, m.ContainerRed, 4);
            VendingBot(area, MapToWorld(480, 140), 200f, 1);
            VendingBot(area, MapToWorld(505, 146), 170f, 2);
            VendingBot(area, MapToWorld(566, 142), 190f, 3);
            Drum(area, MapToWorld(470, 118), 3);
            Drum(area, MapToWorld(585, 120), 4);
        }

        static void BuildDebrisFields()
        {
            var area = new GameObject("Debris Fields").transform;
            area.SetParent(SalvageRoot());
            int i = 10;
            foreach (var p in new[] { (120, 250), (150, 262), (196, 246), (230, 270), (100, 280), (430, 252), (470, 244), (520, 266), (560, 250) })
                Drum(area, MapToWorld(p.Item1, p.Item2), i++);
            // Loose scrap scattered around for visual noise (not salvageable).
            var rng = new System.Random(4);
            for (int k = 0; k < 40; k++)
            {
                var pos = MapToWorld(60 + (float)rng.NextDouble() * 520, 110 + (float)rng.NextDouble() * 180, 0.1f);
                var bit = Box("Scrap Bit", area, pos, new Vector3(0.3f + (float)rng.NextDouble(), 0.12f, 0.3f + (float)rng.NextDouble()),
                    rng.Next(3) == 0 ? m.Rust : m.Metal, (float)rng.NextDouble() * 360f, collider: false);
                bit.transform.rotation *= Quaternion.Euler((float)rng.NextDouble() * 20f, 0f, (float)rng.NextDouble() * 20f);
                MarkStatic(bit);
            }
        }

        static void BuildMachineRow()
        {
            var area = new GameObject("Machine Row (locked)").transform;
            area.SetParent(SalvageRoot());
            Sign(area, "MACHINE ROW  ·  T3 TOOL REQUIRED", MapToWorld(320, 108, 3.4f), 0f, 0.7f);

            for (int i = 0; i < 5; i++)
                Generator(area, MapToWorld(260 + i * 30, 70), i + 1);

            // Inner fence with a locked gate; opened by Mission 8 in Module 4.
            var fence = new GameObject("Machine Row Fence").transform;
            fence.SetParent(area);
            float z = MapToWorld(0, 102).z;
            float x0 = MapToWorld(220, 0).x, x1 = MapToWorld(420, 0).x;
            float gateHalf = 4f;
            MarkStatic(Box("Fence West", fence, new Vector3((x0 - gateHalf) * 0.5f, 1.2f, z), new Vector3(-gateHalf - x0, 2.4f, 0.3f), m.Fence));
            MarkStatic(Box("Fence East", fence, new Vector3((x1 + gateHalf) * 0.5f, 1.2f, z), new Vector3(x1 - gateHalf, 2.4f, 0.3f), m.Fence));
            float zBack = MapToWorld(0, 38).z;
            MarkStatic(Box("Fence Side W", fence, new Vector3(x0, 1.2f, (z + zBack) * 0.5f), new Vector3(0.3f, 2.4f, zBack - z), m.Fence));
            MarkStatic(Box("Fence Side E", fence, new Vector3(x1, 1.2f, (z + zBack) * 0.5f), new Vector3(0.3f, 2.4f, zBack - z), m.Fence));
            var gate = Box("Locked Gate", fence, new Vector3(0f, 1.4f, z), new Vector3(gateHalf * 2f, 2.8f, 0.35f), m.Hazard);
            gate.AddComponent<MachineRowGate>();
            for (int s = -3; s <= 3; s++)
                BoxLocal("Stripe", gate.transform, new Vector3(s * 0.13f, 0f, 0.51f), new Vector3(0.05f, 1f, 0.02f), m.MetalDark, collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        // ───────────────────────────── Prop builders ─────────────────────────────
        // Placeholder shapes. Module 2 adds DestructibleObject components and stage meshes.

        static Transform Prop(Transform parent, string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            t.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }

        static void Drum(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T1_OilDrum_{index:00}", pos, index * 37f);
            CylLocal("Drum", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), index % 3 == 0 ? m.ContainerTeal : m.Rust);
            CylLocal("Band", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.62f, 0.03f, 0.62f), m.RustDark, collider: false);
            CylLocal("Band", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.03f, 0.62f), m.RustDark, collider: false);
        }

        static void Scooter(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T1_HoverScooter_{index:00}", pos, yaw);
            BoxLocal("Deck", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.25f, 1.8f), m.ScooterOrange);
            BoxLocal("Hover Pad", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.15f, 1.4f), m.MetalDark, collider: false);
            BoxLocal("Stem", t, new Vector3(0f, 0.95f, 0.75f), new Vector3(0.12f, 0.9f, 0.12f), m.Metal, collider: false);
            BoxLocal("Bars", t, new Vector3(0f, 1.4f, 0.75f), new Vector3(0.7f, 0.08f, 0.08f), m.MetalDark, collider: false);
            BoxLocal("Rust Patch", t, new Vector3(0.2f, 0.58f, -0.3f), new Vector3(0.3f, 0.02f, 0.5f), m.Rust, collider: false);
        }

        static void Mule7(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T2_Mule7Truck_{index:00}", pos, yaw);
            BoxLocal("Cargo Box", t, new Vector3(0f, 1.9f, -1.1f), new Vector3(2.6f, 2.4f, 5f), m.TruckGrey);
            BoxLocal("Rust", t, new Vector3(1.31f, 1.6f, -1.6f), new Vector3(0.02f, 1.2f, 1.8f), m.Rust, collider: false);
            BoxLocal("Cab", t, new Vector3(0f, 1.5f, 2.4f), new Vector3(2.4f, 1.9f, 2f), m.TruckGrey);
            BoxLocal("Windscreen", t, new Vector3(0f, 1.9f, 3.41f), new Vector3(2f, 0.7f, 0.02f), m.Glass, collider: false);
            BoxLocal("Chassis", t, new Vector3(0f, 0.65f, 0.3f), new Vector3(2.3f, 0.4f, 7.2f), m.MetalDark);
            BoxLocal("Lamp", t, new Vector3(0.9f, 1f, 3.41f), new Vector3(0.3f, 0.2f, 0.03f), m.Hazard, collider: false);
            var wheels = Wheels(t, 1.25f, 2.5f, 0.55f, zOffset: 0.3f);
            // One wheel missing and lying on the ground nearby.
            Object.DestroyImmediate(wheels[3]);
            var loose = CylLocal("Loose Wheel", t, new Vector3(2.6f, 0.15f, -3.5f), new Vector3(1.1f, 0.15f, 1.1f), m.Rubber);
            loose.transform.localRotation = Quaternion.identity;
            t.rotation *= Quaternion.Euler(0f, 0f, -2.5f);
        }

        static void Container(Transform parent, Vector3 pos, float yaw, Material colour, int index)
        {
            var t = Prop(parent, $"T2_CargoContainer_{index:00}", pos, yaw);
            BoxLocal("Shell", t, new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.6f, 6f), colour);
            for (int i = -4; i <= 4; i++)
                BoxLocal("Rib", t, new Vector3(1.21f, 1.3f, i * 0.65f), new Vector3(0.04f, 2.5f, 0.12f), colour, collider: false);
            BoxLocal("Door Bars", t, new Vector3(0f, 1.3f, 3.01f), new Vector3(1.6f, 2.3f, 0.03f), m.MetalDark, collider: false);
            BoxLocal("Rust", t, new Vector3(-1.21f, 0.5f, 1.2f), new Vector3(0.02f, 0.8f, 2.2f), m.Rust, collider: false);
        }

        static void VendingBot(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T1_VendingBot_{index:00}", pos, yaw);
            BoxLocal("Body", t, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 2f, 0.8f), m.VendingRed);
            BoxLocal("Screen", t, new Vector3(0f, 1.5f, 0.41f), new Vector3(0.7f, 0.4f, 0.02f), m.Screen, collider: false);
            BoxLocal("Slot", t, new Vector3(0f, 0.4f, 0.41f), new Vector3(0.6f, 0.2f, 0.02f), m.MetalDark, collider: false);
            CylLocal("Head", t, new Vector3(0f, 2.15f, 0f), new Vector3(0.5f, 0.15f, 0.5f), m.Metal, collider: false);
        }

        static void Generator(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T3_GeneratorUnit_{index:00}", pos, 0f);
            BoxLocal("Housing", t, new Vector3(0f, 1.4f, 0f), new Vector3(4f, 2.8f, 3f), m.LockedGrey);
            CylLocal("Turbine", t, new Vector3(0f, 3.2f, 0f), new Vector3(1.8f, 0.4f, 1.8f), m.MetalDark, collider: false);
            BoxLocal("Hazard Panel", t, new Vector3(0f, 1.6f, -1.51f), new Vector3(1.6f, 1f, 0.02f), m.Hazard, collider: false);
            BoxLocal("Rust", t, new Vector3(2.01f, 0.8f, 0.3f), new Vector3(0.02f, 1.2f, 1.6f), m.Rust, collider: false);
        }

        static GameObject[] Wheels(Transform vehicle, float halfTrack, float halfBase, float radius, float zOffset = 0f)
        {
            var list = new GameObject[4];
            int n = 0;
            foreach (float z in new[] { halfBase, -halfBase })
            foreach (float x in new[] { -halfTrack, halfTrack })
            {
                var w = CylLocal("Wheel", vehicle, new Vector3(x, radius, z + zOffset), new Vector3(radius * 2f, 0.18f, radius * 2f), m.Rubber, collider: false);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                list[n++] = w;
            }
            return list;
        }

        // ───────────────────────────── Player & companion ─────────────────────────────

        static PlayerController BuildPlayer()
        {
            var spawn = MapToWorld(320, 300) + Vector3.up * 0.1f;
            var player = new GameObject("Player (Rookie Scrapper)");
            player.transform.SetPositionAndRotation(spawn, Quaternion.identity); // facing north into the valley
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            var visual = BuildRookie(player.transform);
            var controller = player.AddComponent<PlayerController>();
            SetRef(visual, "controller", controller);

            BuildStepEffects(player.transform, visual);

            var camGo = new GameObject("Player Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 3000f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var playerCam = camGo.AddComponent<PlayerCamera>();
            SetRef(playerCam, "target", player.transform);
            SetRef(playerCam, "visual", visual);
            SetRef(controller, "playerCamera", playerCam);

            Physics.SyncTransforms();
            playerCam.SetTarget(player.transform, visual);
            playerCam.SnapToPose();
            return controller;
        }

        static void SetRef(Object component, string field, Object value)
        {
            var so = new SerializedObject(component);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Placeholder Rookie built from primitives, styled after the approved reference:
        /// orange suit and round helmet, black visor, grey ear caps, black harness and backpack,
        /// mechanical salvage arm on the left. Pivots sit at hips and shoulders for PlayerVisual.
        /// </summary>
        static PlayerVisual BuildRookie(Transform player)
        {
            var root = new GameObject("Rookie (placeholder model)").transform;
            root.SetParent(player, false);

            var body = new GameObject("Body").transform;
            body.SetParent(root, false);

            // Torso and kit
            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 1.28f, 0f), new Vector3(0.64f, 0.36f, 0.48f), m.SuitOrange);
            Part(PrimitiveType.Cylinder, "Belt", body, new Vector3(0f, 1.0f, 0f), new Vector3(0.66f, 0.05f, 0.5f), m.Rubber);
            Part(PrimitiveType.Cube, "Belt Buckle", body, new Vector3(0f, 1.0f, 0.25f), new Vector3(0.14f, 0.08f, 0.02f), m.MetalDark);
            foreach (float x in new[] { -0.15f, 0.15f })
            {
                Part(PrimitiveType.Cube, "Harness Strap", body, new Vector3(x, 1.32f, 0.235f), new Vector3(0.07f, 0.52f, 0.02f), m.Rubber);
                Part(PrimitiveType.Cube, "Chest Pocket", body, new Vector3(x, 1.4f, 0.24f), new Vector3(0.14f, 0.1f, 0.03f), m.SuitOrangeDark);
            }
            Part(PrimitiveType.Cylinder, "Collar", body, new Vector3(0f, 1.64f, 0f), new Vector3(0.44f, 0.04f, 0.44f), m.SuitGrey);
            Part(PrimitiveType.Cube, "Backpack", body, new Vector3(0f, 1.32f, -0.3f), new Vector3(0.5f, 0.56f, 0.24f), m.Rubber);
            foreach (float x in new[] { -0.12f, 0.12f })
                Part(PrimitiveType.Cylinder, "Oxygen Tank", body, new Vector3(x, 1.36f, -0.43f), new Vector3(0.14f, 0.24f, 0.14f), m.MetalDark);

            // Helmet
            var head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 1.88f, 0f);
            Part(PrimitiveType.Sphere, "Helmet", head, Vector3.zero, Vector3.one * 0.6f, m.SuitOrange);
            Part(PrimitiveType.Sphere, "Visor", head, new Vector3(0f, -0.01f, 0.17f), new Vector3(0.48f, 0.42f, 0.3f), m.VisorBlack);
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Part(PrimitiveType.Cylinder, "Ear Cap", head, new Vector3(x, 0f, 0f), new Vector3(0.2f, 0.05f, 0.2f), m.SuitGrey, new Vector3(0f, 0f, 90f));
                Part(PrimitiveType.Cylinder, "Ear Cap Centre", head, new Vector3(x * 1.08f, 0f, 0f), new Vector3(0.1f, 0.05f, 0.1f), m.Rubber, new Vector3(0f, 0f, 90f));
            }

            // Left arm: suit sleeve and glove
            var armL = Pivot("Arm L", body, new Vector3(-0.4f, 1.52f, 0f));
            Part(PrimitiveType.Capsule, "Sleeve", armL, new Vector3(0f, -0.26f, 0f), new Vector3(0.21f, 0.26f, 0.21f), m.SuitOrange);
            Part(PrimitiveType.Cylinder, "Cuff", armL, new Vector3(0f, -0.47f, 0f), new Vector3(0.23f, 0.03f, 0.23f), m.SuitOrangeDark);
            Part(PrimitiveType.Sphere, "Glove", armL, new Vector3(0f, -0.57f, 0f), Vector3.one * 0.2f, m.Rubber);

            // Right arm: mechanical salvage arm, as in the reference (tools mount here in Module 2)
            var armR = Pivot("Salvage Arm R", body, new Vector3(0.4f, 1.52f, 0f));
            Part(PrimitiveType.Cylinder, "Shoulder Gear", armR, new Vector3(0.05f, 0f, 0f), new Vector3(0.28f, 0.04f, 0.28f), m.Metal, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Capsule, "Upper Rod", armR, new Vector3(0f, -0.22f, 0f), new Vector3(0.12f, 0.2f, 0.12f), m.Metal);
            Part(PrimitiveType.Cylinder, "Elbow Gear", armR, new Vector3(0.04f, -0.42f, 0f), new Vector3(0.21f, 0.04f, 0.21f), m.Metal, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Capsule, "Forearm", armR, new Vector3(0f, -0.58f, 0f), new Vector3(0.11f, 0.16f, 0.11f), m.MetalDark);
            var tool = Pivot("Tool Mount", armR, new Vector3(0f, -0.74f, 0f));
            Part(PrimitiveType.Cube, "Claw A", tool, new Vector3(-0.05f, -0.05f, 0.03f), new Vector3(0.04f, 0.13f, 0.04f), m.MetalDark, new Vector3(0f, 0f, 12f));
            Part(PrimitiveType.Cube, "Claw B", tool, new Vector3(0.05f, -0.05f, 0.03f), new Vector3(0.04f, 0.13f, 0.04f), m.MetalDark, new Vector3(0f, 0f, -12f));
            Part(PrimitiveType.Cube, "Claw C", tool, new Vector3(0f, -0.05f, -0.05f), new Vector3(0.04f, 0.13f, 0.04f), m.MetalDark, new Vector3(-12f, 0f, 0f));

            // Legs
            Transform Leg(string side, float x, out Transform foot)
            {
                var leg = Pivot("Leg " + side, root, new Vector3(x, 0.92f, 0f));
                Part(PrimitiveType.Capsule, "Trouser", leg, new Vector3(0f, -0.4f, 0f), new Vector3(0.25f, 0.4f, 0.25f), m.SuitOrange);
                Part(PrimitiveType.Sphere, "Knee Pad", leg, new Vector3(0f, -0.42f, 0.1f), new Vector3(0.21f, 0.17f, 0.12f), m.SuitOrangeDark);
                Part(PrimitiveType.Cube, "Boot", leg, new Vector3(0f, -0.83f, 0.04f), new Vector3(0.27f, 0.18f, 0.38f), m.Rubber);
                foot = Pivot("Foot " + side, leg, new Vector3(0f, -0.92f, 0.04f));
                return leg;
            }
            var legL = Leg("L", -0.14f, out var footL);
            var legR = Leg("R", 0.14f, out var footR);

            var visual = root.gameObject.AddComponent<PlayerVisual>();
            SetRef(visual, "body", body);
            SetRef(visual, "leftLeg", legL);
            SetRef(visual, "rightLeg", legR);
            SetRef(visual, "leftArm", armL);
            SetRef(visual, "rightArm", armR);
            SetRef(visual, "leftFoot", footL);
            SetRef(visual, "rightFoot", footR);
            return visual;
        }

        static Transform Pivot(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        /// <summary>A collider-free primitive in local space, for character parts.</summary>
        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void BuildStepEffects(Transform player, PlayerVisual visual)
        {
            var root = new GameObject("Step Effects").transform;
            root.SetParent(player, false);

            var particleBase = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            var printMat = ParticleMaterial("Footprint", particleBase, GenerateFootprint(), new Color(0.16f, 0.17f, 0.19f, 0.8f));
            var dustMat = ParticleMaterial("RegolithDust", particleBase, GenerateSoftDot(), new Color(0.86f, 0.87f, 0.89f, 0.55f));

            var prints = Particles("Footprints", root, printMat, lifetime: 25f, max: 160, horizontal: true, gravity: 0f);
            var dust = Particles("Dust", root, dustMat, lifetime: 1.6f, max: 200, horizontal: false, gravity: 0.12f);
            var dustMain = dust.main;
            dustMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.42f);
            var size = dust.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var limit = dust.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 1.2f;

            var fx = player.gameObject.AddComponent<FootprintsAndDust>();
            SetRef(fx, "visual", visual);
            SetRef(fx, "prints", prints);
            SetRef(fx, "dust", dust);
        }

        static ParticleSystem Particles(string name, Transform parent, Material mat, float lifetime, int max, bool horizontal, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = 0.34f;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, horizontal ? 0.6f : 0.2f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = horizontal ? ParticleSystemRenderMode.HorizontalBillboard : ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static Material ParticleMaterial(string name, Material baseMat, Texture2D tex, Color tint)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            // Alpha-blended transparent surface.
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D GenerateFootprint()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x - 64f) / 64f, v = (y - 64f) / 64f;
                // Sole: oval toe box (top) and smaller heel (bottom); texture "up" = walking direction.
                float toe = (u * u) / (0.30f * 0.30f) + ((v - 0.32f) * (v - 0.32f)) / (0.52f * 0.52f);
                float heel = (u * u) / (0.24f * 0.24f) + ((v + 0.55f) * (v + 0.55f)) / (0.28f * 0.28f);
                float d = Mathf.Min(toe, heel);
                float a = Mathf.Clamp01((1.05f - d) * 6f);
                bool tread = ((int)((v + 1f) * 14f)) % 2 == 0;
                a *= tread ? 1f : 0.55f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            return SaveTexture(tex, "Footprint", TextureWrapMode.Clamp, mipmaps: true, alpha: true);
        }

        static Texture2D GenerateSoftDot()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                float a = Mathf.Clamp01(1f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
            tex.SetPixels32(px);
            return SaveTexture(tex, "SoftDot", TextureWrapMode.Clamp, mipmaps: true, alpha: true);
        }

        /// <summary>
        /// SCRAP-BOT placeholder styled after the approved reference: white head with a black
        /// screen face and cyan slanted eyes, yellow ear discs and chest, cyan core light,
        /// tapered white lower body, flipper arms. Hovers at shoulder height beside the player.
        /// </summary>
        static void BuildScrapBot(PlayerController player)
        {
            var bot = new GameObject("SCRAP-BOT (placeholder model)");
            bot.transform.position = player.transform.position + new Vector3(1.15f, 1.65f, 0.9f);
            var model = new GameObject("Model").transform;
            model.SetParent(bot.transform, false);
            model.localScale = Vector3.one * 0.8f;

            // Rounded-box head like the reference: a cube core softened by a sphere.
            Part(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.18f, 0f), new Vector3(0.6f, 0.5f, 0.5f), m.BotWhite);
            Part(PrimitiveType.Cube, "Head Core", model, new Vector3(0f, 0.18f, -0.02f), new Vector3(0.5f, 0.4f, 0.42f), m.BotWhite);
            Part(PrimitiveType.Cube, "Head Crest", model, new Vector3(0f, 0.43f, 0.02f), new Vector3(0.16f, 0.04f, 0.3f), m.BotYellow);
            // Flat black screen set into the front of the head.
            Part(PrimitiveType.Cube, "Face Screen", model, new Vector3(0f, 0.18f, 0.2f), new Vector3(0.4f, 0.28f, 0.06f), m.VisorBlack);
            Part(PrimitiveType.Cube, "Eye L", model, new Vector3(-0.085f, 0.2f, 0.235f), new Vector3(0.11f, 0.045f, 0.02f), m.BotEye, new Vector3(0f, 0f, -18f));
            Part(PrimitiveType.Cube, "Eye R", model, new Vector3(0.085f, 0.2f, 0.235f), new Vector3(0.11f, 0.045f, 0.02f), m.BotEye, new Vector3(0f, 0f, 18f));
            foreach (float x in new[] { -0.29f, 0.29f })
            {
                Part(PrimitiveType.Cylinder, "Ear Disc", model, new Vector3(x, 0.18f, 0f), new Vector3(0.19f, 0.04f, 0.19f), m.BotYellow, new Vector3(0f, 0f, 90f));
                Part(PrimitiveType.Cylinder, "Ear Centre", model, new Vector3(x * 1.06f, 0.18f, 0f), new Vector3(0.09f, 0.04f, 0.09f), m.Rubber, new Vector3(0f, 0f, 90f));
                Part(PrimitiveType.Sphere, "Shoulder", model, new Vector3(x * 0.83f, -0.1f, 0f), Vector3.one * 0.13f, m.Rubber);
                Part(PrimitiveType.Capsule, "Flipper", model, new Vector3(x * 1.1f, -0.28f, 0f), new Vector3(0.1f, 0.16f, 0.08f), m.BotWhite, new Vector3(0f, 0f, x > 0 ? 20f : -20f));
            }
            Part(PrimitiveType.Sphere, "Chest", model, new Vector3(0f, -0.18f, 0f), new Vector3(0.42f, 0.3f, 0.36f), m.BotYellow);
            Part(PrimitiveType.Sphere, "Core Light", model, new Vector3(0f, -0.16f, 0.17f), Vector3.one * 0.1f, m.BotEye);
            Part(PrimitiveType.Capsule, "Lower Body", model, new Vector3(0f, -0.42f, 0f), new Vector3(0.3f, 0.18f, 0.28f), m.BotWhite);

            var follower = bot.AddComponent<ScrapBotFollower>();
            var camTransform = player.Camera != null ? player.Camera.transform : null;
            follower.SetTarget(player.transform, camTransform);
            SetRef(follower, "player", player.transform);
            SetRef(follower, "playerCamera", camTransform);
        }

        // ───────────────────────────── Gameplay wiring ─────────────────────────────

        /// <summary>Turns every T1_/T2_/T3_ prop into a dismantlable object with a stable save id.</summary>
        static void MakeDismantlable()
        {
            var map = new (string prefix, string id)[]
            {
                ("OilDrum", "oil_drum"), ("VendingBot", "vending_bot"), ("HoverScooter", "hover_scooter"),
                ("CargoContainer", "cargo_container"), ("Mule7Truck", "mule7_truck"), ("GeneratorUnit", "generator_unit")
            };
            int count = 0;
            foreach (var t in SalvageRoot().GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Length < 4 || t.name[0] != 'T' || t.name[2] != '_') continue;
                foreach (var (prefix, id) in map)
                {
                    if (!t.name.Substring(3).StartsWith(prefix)) continue;
                    t.gameObject.AddComponent<DestructibleObject>().Setup(db.Dismantlable(id), t.name);
                    count++;
                    break;
                }
            }
            Debug.Log($"[Greybox] {count} dismantlable objects placed.");
        }

        static void WireGameplay(PlayerController player)
        {
            var go = player.gameObject;
            var visual = go.GetComponentInChildren<PlayerVisual>();
            var camera = player.Camera;

            go.AddComponent<PlayerInventory>();
            go.AddComponent<Wallet>();
            var equipment = go.AddComponent<PlayerEquipment>();
            SetRef(equipment, "toolMount", FindDeep(go.transform, "Tool Mount"));
            SetRef(equipment, "rustMaterial", m.WrenchRust);
            SetRef(equipment, "metalMaterial", m.Metal);
            SetRef(equipment, "darkMaterial", m.MetalDark);
            SetRef(equipment, "glowMaterial", m.BotEye);

            var toolUser = go.AddComponent<ToolUser>();
            SetRef(toolUser, "controller", player);
            SetRef(toolUser, "visual", visual);
            SetRef(toolUser, "equipment", equipment);

            var interactor = go.AddComponent<PlayerInteractor>();
            SetRef(interactor, "controller", player);

            var suit = go.AddComponent<SuitSystems>();
            SetRef(suit, "controller", player);
            SetRef(suit, "baseCentre", GameObject.Find("Base Centre").transform);

            var systems = new GameObject("_Systems");
            systems.AddComponent<BuildInfoOverlay>();
            systems.AddComponent<InputDiagnostics>();

            var gm = systems.AddComponent<GameManager>();
            SetRef(gm, "player", player);
            systems.AddComponent<MissionManager>();
            var autotest = systems.AddComponent<AutoPlaytest>();
            SetRef(autotest, "player", player);

            var audio = systems.AddComponent<AudioManager>();
            var names = new[] { "hit_metal", "hit_bonk", "break_crunch", "pickup_tink", "sell_register", "purchase", "mission_complete", "ui_click", "footstep", "swing_whoosh", "ambience_loop", "warning" };
            var so = new SerializedObject(audio);
            var clips = so.FindProperty("clips");
            clips.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var el = clips.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("name").stringValue = names[i];
                el.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Generated/{names[i]}.wav");
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var spawner = new GameObject("Scrap Pool").AddComponent<ScrapSpawner>();
            spawner.transform.SetParent(systems.transform, false);
            SetRef(spawner, "scrapMaterial", Lit("Scrap", Color.white, 0.45f, metallic: 0.4f, emission: Color.black));

            var fxRoot = new GameObject("Destruction FX");
            fxRoot.transform.SetParent(systems.transform, false);
            var fx = fxRoot.AddComponent<DestructionFx>();
            var particleBase = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            var softDot = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}/SoftDot.png");
            var sparkMat = ParticleMaterial("Spark", particleBase, softDot, new Color(1f, 0.82f, 0.4f, 1f));
            var sparks = Particles("Sparks", fxRoot.transform, sparkMat, lifetime: 0.45f, max: 400, horizontal: false, gravity: 0.5f);
            var sm = sparks.main;
            sm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            var dustMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/RegolithDust.mat");
            var dust = Particles("Break Dust", fxRoot.transform, dustMat, lifetime: 2f, max: 300, horizontal: false, gravity: 0.08f);
            var dm = dust.main;
            dm.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            var dsize = dust.sizeOverLifetime;
            dsize.enabled = true;
            dsize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            var drag = dust.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.4f;
            SetRef(fx, "sparks", sparks);
            SetRef(fx, "dust", dust);
            SetRef(fx, "debrisMaterial", Lit("Debris", Color.white, 0.3f));

            var ui = systems.AddComponent<UIManager>();
            SetRef(ui, "player", player);
            SetRef(ui, "toolUser", toolUser);
            SetRef(ui, "interactor", interactor);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // ───────────────────────────── Primitive helpers ─────────────────────────────

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 size, Material mat, float yaw = 0f, bool collider = true)
        {
            var go = Primitive(PrimitiveType.Cube, name, parent, worldPos, size, mat, collider);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static GameObject BoxLocal(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject CylLocal(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            else
            {
                // Capsule colliders on squat cylinders are poor; swap for a box.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        static void Sign(Transform parent, string text, Vector3 worldPos, float yaw, float size)
        {
            var go = new GameObject($"Sign {text}");
            go.transform.SetParent(parent, true);
            // yaw 0 = readable from the south (viewer looking north), 180 = readable from the north.
            go.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f));
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = signFont;
            tm.fontSize = 64;
            tm.characterSize = 0.18f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.1f, 0.12f, 0.14f);
            go.GetComponent<MeshRenderer>().sharedMaterial = SignMaterial();
            go.AddComponent<WorldTextFont>();
        }

        static Material signMaterial;

        static Material SignMaterial()
        {
            if (signMaterial != null) return signMaterial;
            const string path = MaterialDir + "/SignText.mat";
            signMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (signMaterial == null)
            {
                signMaterial = new Material(Shader.Find("PlanetScrappers/WorldText"));
                AssetDatabase.CreateAsset(signMaterial, path);
            }
            signMaterial.mainTexture = signFont.material.mainTexture;
            return signMaterial;
        }

        static void MarkStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ───────────────────────────── Materials ─────────────────────────────

        static Shader LitShader()
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            return urp != null && GraphicsSettings.defaultRenderPipeline != null ? urp : Shader.Find("Standard");
        }

        static Mats CreateMaterials()
        {
            var regolithTex = GenerateRegolith();
            var halcyonTex = GenerateHalcyon();

            return new Mats
            {
                Regolith = Lit("Regolith", Hex("#9EA1A6"), 0.08f, tex: regolithTex, tiling: new Vector2(200f, 200f)),
                Road = Lit("Road", Hex("#6C7076"), 0.12f, tex: regolithTex, tiling: new Vector2(1f, 12f)),
                Rim = Lit("CraterRim", Hex("#83868B"), 0.04f, tex: regolithTex, tiling: new Vector2(2f, 2f)),
                CraterFloor = Lit("CraterFloor", Hex("#76797E"), 0.05f, tex: regolithTex, tiling: new Vector2(4f, 4f)),
                BaseWhite = Lit("BaseWhite", Hex("#E3E6E9"), 0.45f),
                TrimOrange = Lit("TrimOrange", Hex("#F07A2E"), 0.35f),
                TrimTeal = Lit("TrimTeal", Hex("#1FA197"), 0.35f),
                TrimYellow = Lit("TrimYellow", Hex("#F4C430"), 0.35f),
                Glass = Lit("Glass", Hex("#1C2A35"), 0.9f),
                Screen = Lit("Screen", Hex("#103040"), 0.6f, emission: Hex("#57C7FF") * 1.2f),
                Rust = Lit("Rust", Hex("#A8552A"), 0.1f),
                RustDark = Lit("RustDark", Hex("#6E3418"), 0.1f),
                Metal = Lit("Metal", Hex("#9AA3AB"), 0.5f, metallic: 0.6f),
                MetalDark = Lit("MetalDark", Hex("#353A40"), 0.4f, metallic: 0.4f),
                Rubber = Lit("Rubber", Hex("#23272B"), 0.15f),
                ContainerRed = Lit("ContainerRed", Hex("#9C3B2C"), 0.25f),
                ContainerTeal = Lit("ContainerTeal", Hex("#2E7C76"), 0.25f),
                ContainerBlue = Lit("ContainerBlue", Hex("#3D5E86"), 0.25f),
                VendingRed = Lit("VendingRed", Hex("#C0473A"), 0.4f),
                ScooterOrange = Lit("ScooterOrange", Hex("#E2873A"), 0.4f),
                TruckGrey = Lit("TruckGrey", Hex("#7F9097"), 0.3f),
                LockedGrey = Lit("LockedGrey", Hex("#5A626A"), 0.3f, metallic: 0.3f),
                Hazard = Lit("Hazard", Hex("#F4C430"), 0.3f),
                Halcyon = Lit("Halcyon", Color.white, 0.3f, tex: halcyonTex, emission: Hex("#0A1830")),
                BotYellow = Lit("BotYellow", Hex("#F5B41C"), 0.45f),
                BotWhite = Lit("BotWhite", Hex("#F2F2EC"), 0.6f),
                SuitOrange = Lit("SuitOrange", Hex("#EC6E24"), 0.35f),
                SuitOrangeDark = Lit("SuitOrangeDark", Hex("#C9561A"), 0.3f),
                SuitGrey = Lit("SuitGrey", Hex("#9AA3AB"), 0.5f, metallic: 0.3f),
                VisorBlack = Lit("VisorBlack", Hex("#07090C"), 0.95f, metallic: 0.2f),
                BotEye = Lit("BotEye", Hex("#57C7FF"), 0.8f, emission: Hex("#57C7FF") * 2.5f),
                WrenchRust = Lit("WrenchRust", Hex("#A0522D"), 0.2f, metallic: 0.3f),
                Fence = Lit("Fence", Hex("#6B737B"), 0.3f, metallic: 0.5f),
            };
        }

        static Material Lit(string name, Color colour, float smoothness, float metallic = 0f,
            Texture2D tex = null, Vector2? tiling = null, Color? emission = null)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(LitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = LitShader();
            }

            mat.enableInstancing = true;
            SetIfPresent(mat, "_BaseColor", colour);
            SetIfPresent(mat, "_Color", colour);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (tex != null)
            {
                foreach (var prop in new[] { "_BaseMap", "_MainTex" })
                {
                    if (!mat.HasProperty(prop)) continue;
                    mat.SetTexture(prop, tex);
                    mat.SetTextureScale(prop, tiling ?? Vector2.one);
                }
            }

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material SaveMaterial(Material mat, string name)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void SetIfPresent(Material mat, string prop, Color c)
        {
            if (mat.HasProperty(prop)) mat.SetColor(prop, c);
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ───────────────────────────── Generated textures ─────────────────────────────

        static Texture2D GenerateStarfield()
        {
            const int w = 4096, h = 2048;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var px = new Color32[w * h];
            var bg = new Color32(3, 5, 10, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            var rng = new System.Random(2026);
            for (int s = 0; s < 9000; s++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                float b = Mathf.Pow((float)rng.NextDouble(), 3f);
                byte v = (byte)(90 + b * 165);
                bool big = b > 0.7f;
                var tint = rng.Next(5) == 0 ? new Color32((byte)(v * 0.8f), (byte)(v * 0.9f), v, 255) : new Color32(v, v, v, 255);
                Plot(px, w, h, x, y, tint);
                if (big)
                {
                    var dim = new Color32((byte)(tint.r / 2), (byte)(tint.g / 2), (byte)(tint.b / 2), 255);
                    Plot(px, w, h, x + 1, y, dim); Plot(px, w, h, x - 1, y, dim);
                    Plot(px, w, h, x, y + 1, dim); Plot(px, w, h, x, y - 1, dim);
                }
            }
            tex.SetPixels32(px);
            return SaveTexture(tex, "Starfield", TextureWrapMode.Clamp, mipmaps: false);
        }

        static void Plot(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            x = (x + w) % w;
            if (y < 0 || y >= h) return;
            px[y * w + x] = c;
        }

        static Texture2D GenerateRegolith()
        {
            const int size = 512;
            const int cells = 32; // lattice period for tileable value noise
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            var shade = new float[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float n = 0.55f * TileNoise(u, v, cells / 4) + 0.3f * TileNoise(u, v, cells / 2) + 0.15f * TileNoise(u, v, cells);
                shade[y * size + x] = 0.82f + (n - 0.5f) * 0.25f;
            }

            // Tileable craters: darker floors, bright rims, wrapping at the edges.
            var rng = new System.Random(77);
            for (int c = 0; c < 70; c++)
            {
                float cx = (float)rng.NextDouble() * size, cy = (float)rng.NextDouble() * size;
                float r = 4f + Mathf.Pow((float)rng.NextDouble(), 2.5f) * 40f;
                int ir = Mathf.CeilToInt(r * 1.3f);
                for (int dy = -ir; dy <= ir; dy++)
                for (int dx = -ir; dx <= ir; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (d > 1.3f) continue;
                    float delta = d < 0.85f ? -0.12f * (1f - d / 0.85f) - 0.03f : 0.1f * (1f - Mathf.Abs(d - 1.05f) / 0.25f);
                    int px = ((int)cx + dx + size) % size, py = ((int)cy + dy + size) % size;
                    shade[py * size + px] += Mathf.Max(delta, -0.2f);
                }
            }

            var cols = new Color[size * size];
            for (int i = 0; i < cols.Length; i++)
            {
                float s = Mathf.Clamp01(shade[i]);
                cols[i] = new Color(s, s, s * 1.02f);
            }
            tex.SetPixels(cols);
            return SaveTexture(tex, "Regolith", TextureWrapMode.Repeat, mipmaps: true);
        }

        static Texture2D GenerateHalcyon()
        {
            const int w = 1024, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            var cols = new Color[w * h];
            // Fictional palette: teal lowlands and copper highlands, so Halcyon never reads as Earth.
            Color ocean = Hex("#2A56B0"), deep = Hex("#172C6A"), land = Hex("#3E8C7A"), desert = Hex("#C98E5A"), ice = Hex("#E8F0F6");

            for (int y = 0; y < h; y++)
            {
                float lat = (y / (float)(h - 1) - 0.5f) * Mathf.PI;
                for (int x = 0; x < w; x++)
                {
                    float lon = x / (float)w * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    float continent = Fbm(p * 2.2f + new Vector3(11f, 3f, 7f), 5);
                    float cloud = Fbm(p * 4.5f + new Vector3(-5f, 9f, 2f), 4);
                    float polar = Mathf.Abs(Mathf.Sin(lat));

                    Color c = Color.Lerp(deep, ocean, Mathf.InverseLerp(0.3f, 0.5f, continent));
                    if (continent > 0.54f)
                        c = Color.Lerp(land, desert, Mathf.InverseLerp(0.6f, 0.75f, continent + (1f - polar) * 0.08f));
                    if (polar > 0.88f) c = ice;
                    float cl = Mathf.InverseLerp(0.55f, 0.72f, cloud);
                    c = Color.Lerp(c, Color.white, cl * 0.85f);
                    cols[y * w + x] = c;
                }
            }
            tex.SetPixels(cols);
            return SaveTexture(tex, "Halcyon", TextureWrapMode.Repeat, mipmaps: true);
        }

        static Texture2D SaveTexture(Texture2D tex, string name, TextureWrapMode wrap, bool mipmaps, bool alpha = false)
        {
            tex.Apply();
            string path = $"{TextureDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = wrap;
            importer.mipmapEnabled = mipmaps;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = alpha;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Tileable 2D value noise on a periodic lattice.
        static float TileNoise(float u, float v, int period)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash2(x0 % period, y0 % period), b = Hash2((x0 + 1) % period, y0 % period);
            float c = Hash2(x0 % period, (y0 + 1) % period), d = Hash2((x0 + 1) % period, (y0 + 1) % period);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Hash2(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        static float Hash3(int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        static float ValueNoise3(Vector3 p)
        {
            int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
            float fx = p.x - x0, fy = p.y - y0, fz = p.z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);
            float Lerp3(int dz)
            {
                float a = Mathf.Lerp(Hash3(x0, y0, z0 + dz), Hash3(x0 + 1, y0, z0 + dz), fx);
                float b = Mathf.Lerp(Hash3(x0, y0 + 1, z0 + dz), Hash3(x0 + 1, y0 + 1, z0 + dz), fx);
                return Mathf.Lerp(a, b, fy);
            }
            return Mathf.Lerp(Lerp3(0), Lerp3(1), fz);
        }

        static float Fbm(Vector3 p, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise3(p) * amp;
                norm += amp;
                p *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        // ───────────────────────────── Project helpers ─────────────────────────────

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
