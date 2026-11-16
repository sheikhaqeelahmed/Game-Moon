using System.IO;
using System.Linq;
using PlanetScrappers.Companion;
using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using PlanetScrappers.Missions;
using PlanetScrappers.UI;
using PlanetScrappers.Vehicles;
using PlanetScrappers.Automation;
using PlanetScrappers.Environment;
using PlanetScrappers.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        const float IronHalfWidth = 70f; // Iron District x extent
        const float IronNorth = 175f;    // Iron District north fence

        static Font signFont;
        static GameDatabase db;
        static Mats m;

        class Mats
        {
            public Material Crystal, CaveRock, Regolith, Road, Rim, CraterFloor, BaseWhite, SuitOrange, SuitOrangeDark, SuitGrey, VisorBlack, TrimOrange, TrimTeal, TrimYellow, Glass, Screen,
                Rust, RustDark, Metal, MetalDark, Rubber, ContainerRed, ContainerTeal, ContainerBlue, VendingRed,
                ScooterOrange, TruckGrey, LockedGrey, Hazard, Halcyon, BotYellow, BotWhite, BotEye, WrenchRust, Fence,
                Skin, Hair, HairGrey, Leather, Gold, Headlight, TealDark;
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
            BuildIronDistrict();
            BuildCollectorPads();
            BuildSpaceport();
            BuildDeepCore();
            BuildAutomation();
            BuildMegaFactory();
            BuildSky();
            BuildPostProcessing();
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
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.95f; // no atmosphere: deep, crisp shadows
            // Sun in the north-east, so building fronts (which face north into the valley) are lit.
            sunGo.transform.rotation = Quaternion.Euler(32f, 215f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.42f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.11f);
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        /// <summary>Bright, slightly filmic look from the approved screen designs.</summary>
        static void BuildPostProcessing()
        {
            const string path = "Assets/Settings/RustValleyLook.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var c in profile.components.ToArray()) { profile.Remove(c.GetType()); Object.DestroyImmediate(c, true); }

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.6f);
            var colour = profile.Add<ColorAdjustments>(true);
            colour.postExposure.Override(0.45f);
            colour.saturation.Override(12f);
            colour.contrast.Override(8f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Post Processing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
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
                float rx = 300f + (float)rng.NextDouble() * 25f;
                float rz = (Mathf.Sin(a) > 0.2f ? 215f : Mathf.Sin(a) < -0.2f ? 265f : 130f) + (float)rng.NextDouble() * 20f;
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
            BuildCrater(root, new Vector3(-160f, 0f, -150f), 22f);
            BuildCrater(root, new Vector3(140f, 0f, -90f), 18f);
            BuildCrater(root, new Vector3(-165f, 0f, 120f), 26f);
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
            Side(sw, new Vector3(-6f, 0f, -HalfDepth));
            Side(new Vector3(6f, 0f, -HalfDepth), se);
            Side(new Vector3(-MegaHalfWidth, 0f, -HalfDepth), new Vector3(-MegaHalfWidth, 0f, MegaFar));
            Side(new Vector3(-MegaHalfWidth, 0f, MegaFar), new Vector3(MegaHalfWidth, 0f, MegaFar));
            Side(new Vector3(MegaHalfWidth, 0f, MegaFar), new Vector3(MegaHalfWidth, 0f, -HalfDepth));
            Side(se, new Vector3(HalfWidth, 0f, -6f));
            Side(new Vector3(HalfWidth, 0f, 6f), ne);
            Side(new Vector3(HalfWidth, 0f, -ZoneHalfDepth), new Vector3(ZoneFar, 0f, -ZoneHalfDepth));
            Side(new Vector3(ZoneFar, 0f, -ZoneHalfDepth), new Vector3(ZoneFar, 0f, ZoneHalfDepth));
            Side(new Vector3(ZoneFar, 0f, ZoneHalfDepth), new Vector3(HalfWidth, 0f, ZoneHalfDepth));
            // North side has a gap behind Machine Row that leads into the Iron District.
            Side(ne, new Vector3(6f, 0f, HalfDepth));
            Side(new Vector3(-6f, 0f, HalfDepth), nw);
            Side(new Vector3(-IronHalfWidth, 0f, HalfDepth), new Vector3(-IronHalfWidth, 0f, IronNorth));
            Side(new Vector3(-IronHalfWidth, 0f, IronNorth), new Vector3(IronHalfWidth, 0f, IronNorth));
            Side(new Vector3(IronHalfWidth, 0f, IronNorth), new Vector3(IronHalfWidth, 0f, HalfDepth));
            Side(nw, new Vector3(-HalfWidth, 0f, 6f));
            Side(new Vector3(-HalfWidth, 0f, -6f), sw);
            Side(new Vector3(-HalfWidth, 0f, ZoneHalfDepth), new Vector3(-ZoneFar, 0f, ZoneHalfDepth));
            Side(new Vector3(-ZoneFar, 0f, ZoneHalfDepth), new Vector3(-ZoneFar, 0f, -ZoneHalfDepth));
            Side(new Vector3(-ZoneFar, 0f, -ZoneHalfDepth), new Vector3(-HalfWidth, 0f, -ZoneHalfDepth));
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
            PlaceNpc("CaptainBolt", "Captain Bolt", root, basePos + new Vector3(-6.5f, 0f, 7.7f), 20f);

            // Upgrade Station (Nova).
            var upgradePos = MapToWorld(200, 335);
            Building(root, "Upgrade Station", upgradePos, new Vector3(14f, 5f, 10f), m.TrimTeal, "UPGRADES");
            var bench = Box("Workbench", root, upgradePos + new Vector3(0f, 0.5f, 6.5f), new Vector3(3f, 1f, 1.2f), m.Metal);
            bench.AddComponent<StationInteractable>().Setup("shop", "Upgrade Station (Nova)");
            Sign(root, "NOVA'S UPGRADES", upgradePos + new Vector3(0f, 1.6f, 7.2f), 180f, 0.45f);
            PlaceNpc("Nova", "Engineer Nova", root, upgradePos + new Vector3(2.4f, 0f, 6.7f), -20f);

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
            var door = new GameObject("Warehouse Drop-off");
            door.transform.SetParent(root, true);
            door.transform.position = warehousePos + new Vector3(0f, 0f, 9f);
            var warehouse = door.AddComponent<ScrapStorage>();
            warehouse.Setup("warehouse", "Warehouse", 600);
            door.AddComponent<WarehouseDeposit>().Setup(warehouse);
            Box("Drop-off Bay", root, warehousePos + new Vector3(0f, 0.15f, 9f), new Vector3(4f, 0.3f, 2.5f), m.Hazard, collider: false);

            // Starter salvage truck (drivable in a later milestone).
            var truck = new GameObject("Starter Salvage Truck").transform;
            truck.SetParent(root);
            truck.position = MapToWorld(347, 274);
            truck.rotation = Quaternion.Euler(0f, 90f, 0f);
            var truckModel = InstantiateModel("SH01Truck", truck, "SH-01 Truck");
            if (truckModel != null)
            {
                var modelWheels = new GameObject[4];
                string[] names = { "Wheel FL", "Wheel FR", "Wheel RL", "Wheel RR" };
                for (int i = 0; i < 4; i++) modelWheels[i] = FindDeep(truckModel, names[i]).gameObject;
                SetupTruck(truck, modelWheels, Vector3.right);
            }
            else
            {
            BoxLocal("Body", truck, new Vector3(0f, 1.4f, 0f), new Vector3(2.3f, 1.8f, 4.8f), m.BaseWhite, collider: false);
            BoxLocal("Scrap Bin", truck, new Vector3(0f, 2.55f, -1.2f), new Vector3(2.1f, 0.5f, 2.2f), m.Rust, collider: false);
            BoxLocal("Stripe", truck, new Vector3(0f, 1.2f, 0f), new Vector3(2.34f, 0.35f, 4.84f), m.TrimOrange, collider: false);
            BoxLocal("Windscreen", truck, new Vector3(0f, 1.9f, 2.41f), new Vector3(2f, 0.6f, 0.02f), m.Glass, collider: false);
            BoxLocal("Roof Rack", truck, new Vector3(0f, 2.4f, -0.3f), new Vector3(2f, 0.15f, 3.4f), m.MetalDark, collider: false);
            var truckWheels = Wheels(truck, 1.15f, 1.6f, 0.45f);
            SetupTruck(truck, truckWheels);
            }

            // Rusty wrench pickup (becomes interactive in Module 2).
            var wrenchPos = MapToWorld(304, 279); // in view just ahead-left of the spawn
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
            var model = InstantiateModel("Buildings/" + name.Replace(" ", ""), b, "Model");
            if (model != null)
            {
                ColliderBox(b, "Shell", new Vector3(0f, size.y * 0.5f, 0f), size);
                foreach (var tr in b.GetComponentsInChildren<Transform>()) MarkStatic(tr.gameObject);
                Sign(b, label, groundPos + new Vector3(0f, size.y * 0.72f + 0.9f, size.z * 0.5f + 0.1f), 180f, 1f);
                return;
            }
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
            float zBack = HalfDepth;
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
            if (PropModel(t, "OilDrum", index % 3 == 0 ? "Rust" : null, "ContainerTeal"))
            {
                ColliderBox(t, "Drum", new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f));
                return;
            }
            CylLocal("Drum", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), index % 3 == 0 ? m.ContainerTeal : m.Rust);
            CylLocal("Band", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.62f, 0.03f, 0.62f), m.RustDark, collider: false);
            CylLocal("Band", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.03f, 0.62f), m.RustDark, collider: false);
        }

        static void Scooter(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T1_HoverScooter_{index:00}", pos, yaw);
            if (PropModel(t, "HoverScooter"))
            {
                ColliderBox(t, "Deck", new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.25f, 1.8f));
                return;
            }
            BoxLocal("Deck", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.25f, 1.8f), m.ScooterOrange);
            BoxLocal("Hover Pad", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.15f, 1.4f), m.MetalDark, collider: false);
            BoxLocal("Stem", t, new Vector3(0f, 0.95f, 0.75f), new Vector3(0.12f, 0.9f, 0.12f), m.Metal, collider: false);
            BoxLocal("Bars", t, new Vector3(0f, 1.4f, 0.75f), new Vector3(0.7f, 0.08f, 0.08f), m.MetalDark, collider: false);
            BoxLocal("Rust Patch", t, new Vector3(0.2f, 0.58f, -0.3f), new Vector3(0.3f, 0.02f, 0.5f), m.Rust, collider: false);
        }

        static void Mule7(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T2_Mule7Truck_{index:00}", pos, yaw);
            if (PropModel(t, "Mule7Truck"))
            {
                ColliderBox(t, "Cargo Box", new Vector3(0f, 1.9f, -1.1f), new Vector3(2.6f, 2.4f, 5f));
                ColliderBox(t, "Cab", new Vector3(0f, 1.5f, 2.4f), new Vector3(2.4f, 1.9f, 2f));
                ColliderBox(t, "Chassis", new Vector3(0f, 0.65f, 0.3f), new Vector3(2.3f, 0.4f, 7.2f));
                ColliderBox(t, "Loose Wheel", new Vector3(2.6f, 0.15f, -3.5f), new Vector3(1.1f, 0.3f, 1.1f));
                t.rotation *= Quaternion.Euler(0f, 0f, -2.5f);
                return;
            }
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
            if (PropModel(t, "CargoContainer", "ContainerRed", colour.name))
            {
                ColliderBox(t, "Shell", new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.6f, 6f));
                return;
            }
            BoxLocal("Shell", t, new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.6f, 6f), colour);
            for (int i = -4; i <= 4; i++)
                BoxLocal("Rib", t, new Vector3(1.21f, 1.3f, i * 0.65f), new Vector3(0.04f, 2.5f, 0.12f), colour, collider: false);
            BoxLocal("Door Bars", t, new Vector3(0f, 1.3f, 3.01f), new Vector3(1.6f, 2.3f, 0.03f), m.MetalDark, collider: false);
            BoxLocal("Rust", t, new Vector3(-1.21f, 0.5f, 1.2f), new Vector3(0.02f, 0.8f, 2.2f), m.Rust, collider: false);
        }

        static void VendingBot(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T1_VendingBot_{index:00}", pos, yaw);
            if (PropModel(t, "VendingBot"))
            {
                ColliderBox(t, "Body", new Vector3(0f, 1f, 0f), new Vector3(1.1f, 2f, 0.8f));
                return;
            }
            BoxLocal("Body", t, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 2f, 0.8f), m.VendingRed);
            BoxLocal("Screen", t, new Vector3(0f, 1.5f, 0.41f), new Vector3(0.7f, 0.4f, 0.02f), m.Screen, collider: false);
            BoxLocal("Slot", t, new Vector3(0f, 0.4f, 0.41f), new Vector3(0.6f, 0.2f, 0.02f), m.MetalDark, collider: false);
            CylLocal("Head", t, new Vector3(0f, 2.15f, 0f), new Vector3(0.5f, 0.15f, 0.5f), m.Metal, collider: false);
        }

        static void Generator(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T3_GeneratorUnit_{index:00}", pos, 0f);
            if (PropModel(t, "GeneratorUnit"))
            {
                ColliderBox(t, "Housing", new Vector3(0f, 1.4f, 0f), new Vector3(4f, 2.8f, 3f));
                return;
            }
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
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
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
            var model = InstantiateModel("Rookie", player, "Rookie");
            if (model != null)
            {
                var v = model.gameObject.AddComponent<PlayerVisual>();
                SetRef(v, "body", FindDeep(model, "Body"));
                SetRef(v, "leftLeg", FindDeep(model, "Leg L"));
                SetRef(v, "rightLeg", FindDeep(model, "Leg R"));
                SetRef(v, "leftArm", FindDeep(model, "Arm L"));
                SetRef(v, "rightArm", FindDeep(model, "Salvage Arm R"));
                SetRef(v, "leftFoot", FindDeep(model, "Foot L"));
                SetRef(v, "rightFoot", FindDeep(model, "Foot R"));
                return v;
            }

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

        const string ModelDir = "Assets/Art/Models";

        /// <summary>
        /// Instantiates a Blender model from Assets/Art/Models and gives each mesh the builder
        /// material named after its "__Key" suffix. Returns null when the model is missing,
        /// so callers fall back to the primitive placeholder.
        /// </summary>
        static Transform InstantiateModel(string file, Transform parent, string name, string remapFrom = null, string remapTo = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{file}.fbx");
            if (asset == null) return null;
            var go = (GameObject)Object.Instantiate(asset, parent, false);
            go.name = name;
            // The FBX root node keeps the exporter's axis rotation; the children are already in Unity space.
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                int cut = r.name.LastIndexOf("__", System.StringComparison.Ordinal);
                string key = cut >= 0 ? r.name.Substring(cut + 2) : "";
                if (remapFrom != null && key == remapFrom) key = remapTo;
                var field = typeof(Mats).GetField(key);
                var mat = field != null ? field.GetValue(m) as Material : null;
                if (mat == null) { Debug.LogWarning($"[Models] {file}: no material '{key}' for {r.name}"); mat = m.MetalDark; }
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            SortChildren(go.transform);
            return go.transform;
        }

        /// <summary>Orders children by name so prop part groups (P00, P01, ...) come off in sequence.</summary>
        static void SortChildren(Transform t)
        {
            var kids = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in t) kids.Add(c);
            kids.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < kids.Count; i++) { kids[i].SetSiblingIndex(i); SortChildren(kids[i]); }
        }

        /// <summary>Puts a prop model under t. False when the model file is missing (primitive fallback).</summary>
        static bool PropModel(Transform t, string file, string remapFrom = null, string remapTo = null) =>
            InstantiateModel("Props/" + file, t, "Model", remapFrom, remapTo) != null;

        /// <summary>An invisible box collider in the prop's local space (keeps gameplay shapes unchanged).</summary>
        static void ColliderBox(Transform t, string name, Vector3 localPos, Vector3 size, float zTilt = 0f) =>
            ColliderBox(t, name, localPos, size, new Vector3(0f, 0f, zTilt));

        static void ColliderBox(Transform t, string name, Vector3 localPos, Vector3 size, Vector3 euler)
        {
            var go = new GameObject(name + " Collider");
            go.transform.SetParent(t, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>The flat wing model on a tilted child, with a matching collider.</summary>
        static bool WingModel(Transform p, Vector3 centre, Vector3 euler)
        {
            var tilt = new GameObject("Tilt").transform;
            tilt.SetParent(p, false);
            tilt.localPosition = centre;
            tilt.localRotation = Quaternion.Euler(euler);
            if (!PropModel(tilt, "ShipWing")) { Object.DestroyImmediate(tilt.gameObject); return false; }
            ColliderBox(tilt, "Wing", Vector3.zero, new Vector3(14f, 0.8f, 9f));
            return true;
        }

        static void PlaceNpc(string file, string name, Transform parent, Vector3 pos, float yaw)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, true);
            holder.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var model = InstantiateModel(file, holder, name + " Model");
            if (model == null) { Object.DestroyImmediate(holder.gameObject); return; }
            var col = holder.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.35f;
            holder.gameObject.AddComponent<NpcIdle>().Setup(FindDeep(model, "Body"), FindDeep(model, "Head"));
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

            if (InstantiateModel("ScrapBot", model, "SCRAP-BOT") != null) goto Follower;

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

        Follower:;
            var follower = bot.AddComponent<ScrapBotFollower>();
            var camTransform = player.Camera != null ? player.Camera.transform : null;
            follower.SetTarget(player.transform, camTransform);
            SetRef(follower, "player", player.transform);
            SetRef(follower, "playerCamera", camTransform);
        }

        // ───────────────────────────── Phase 2: truck, collectors, Iron District ─────────────────────────────

        static void SetupTruck(Transform truck, GameObject[] wheels, Vector3? wheelAxis = null)
        {
            var cc = truck.gameObject.AddComponent<CharacterController>();
            cc.height = 2.6f;
            cc.radius = 1.25f;
            cc.center = new Vector3(0f, 1.3f, 0f);
            cc.stepOffset = 0.5f;
            cc.slopeLimit = 40f;

            var bin = truck.gameObject.AddComponent<ScrapStorage>();
            bin.Setup("truck", "Truck bin", 150);

            var binTop = new GameObject("Bin Intake").transform;
            binTop.SetParent(truck, false);
            binTop.localPosition = new Vector3(0f, 2.9f, -1.2f);
            var sink = truck.gameObject.AddComponent<StorageScrapSink>();
            sink.Setup(bin, binTop, 7f);

            var exit = new GameObject("Exit Point").transform;
            exit.SetParent(truck, false);
            exit.localPosition = new Vector3(-2.6f, 0.1f, 0.8f);

            var t = truck.gameObject.AddComponent<SalvageTruck>();
            SetRef(t, "bin", bin);
            SetRef(t, "binMagnet", sink);
            SetRef(t, "exitPoint", exit);
            var so = new SerializedObject(t);
            var w = so.FindProperty("wheels");
            int alive = 0;
            foreach (var g in wheels) if (g != null) alive++;
            w.arraySize = alive;
            int k = 0;
            foreach (var g in wheels) if (g != null) w.GetArrayElementAtIndex(k++).objectReferenceValue = g.transform;
            so.FindProperty("wheelAxis").vector3Value = wheelAxis ?? Vector3.up;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildCollectorPads()
        {
            var root = new GameObject("Collector Pads").transform;
            Pad(root, "collector_graveyard", MapToWorld(190, 178));
            Pad(root, "collector_containers", MapToWorld(470, 178));
            Pad(root, "collector_debris", MapToWorld(165, 268));
        }

        static void Pad(Transform parent, string id, Vector3 pos)
        {
            var pad = new GameObject("Collector Pad " + id);
            pad.transform.SetParent(parent, true);
            pad.transform.position = pos;
            Primitive(PrimitiveType.Cylinder, "Pad Base", pad.transform, pos + Vector3.up * 0.05f, new Vector3(3.2f, 0.05f, 3.2f), m.MetalDark, collider: false);
            Primitive(PrimitiveType.Cylinder, "Pad Ring", pad.transform, pos + Vector3.up * 0.06f, new Vector3(3.4f, 0.04f, 3.4f), m.Hazard, collider: false);

            var model = new GameObject("Collector");
            model.transform.SetParent(pad.transform, false);
            Part(PrimitiveType.Cylinder, "Tower", model.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 1.2f, 0.9f), m.MetalDark);
            Part(PrimitiveType.Cube, "Hopper", model.transform, new Vector3(0f, 0.6f, 0.9f), new Vector3(1.2f, 0.8f, 0.8f), m.Hazard);
            var dish = new GameObject("Dish").transform;
            dish.SetParent(model.transform, false);
            dish.localPosition = new Vector3(0f, 2.7f, 0f);
            Part(PrimitiveType.Cylinder, "Dish Plate", dish, Vector3.zero, new Vector3(2f, 0.06f, 2f), m.Metal);
            Part(PrimitiveType.Sphere, "Core", dish, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.35f, m.BotEye);
            Part(PrimitiveType.Cube, "Vane A", dish, new Vector3(0.7f, 0.1f, 0f), new Vector3(0.6f, 0.08f, 0.15f), m.Hazard);
            Part(PrimitiveType.Cube, "Vane B", dish, new Vector3(-0.7f, 0.1f, 0f), new Vector3(0.6f, 0.08f, 0.15f), m.Hazard);

            var hopper = pad.AddComponent<ScrapStorage>();
            hopper.Setup(id, "Scrap Collector", 120);
            var sink = pad.AddComponent<StorageScrapSink>();
            sink.Setup(hopper, dish, 15f);
            var c = pad.AddComponent<CollectorPad>();
            SetRef(c, "model", model);
            SetRef(c, "dish", dish);
            SetRef(c, "hopper", hopper);
            SetRef(c, "magnet", sink);
            var so = new SerializedObject(c);
            so.FindProperty("padId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildIronDistrict()
        {
            var area = new GameObject("Iron District").transform;
            area.SetParent(SalvageRoot());
            Sign(area, "IRON DISTRICT", new Vector3(0f, 4.2f, HalfDepth + 4f), 0f, 1.2f);
            Sign(area, "MACHINE ROW → IRON DISTRICT", new Vector3(0f, 3.2f, HalfDepth - 1.5f), 0f, 0.5f);

            // Road from the gap up the middle of the district.
            var road = Box("District Road", area, new Vector3(0f, 0.02f, (HalfDepth + IronNorth) * 0.5f), new Vector3(9f, 0.04f, IronNorth - HalfDepth), m.Road, collider: false);
            MarkStatic(road);

            Hall(area, "Hall West", new Vector3(-42f, 0f, 125f), 34f, 52f);
            Hall(area, "Hall East", new Vector3(42f, 0f, 125f), 34f, 52f);

            int n = 1;
            foreach (float x in new[] { -42f, 42f })
            {
                AssemblyLine(area, new Vector3(x, 0f, 130f), 0f, n);
                RobotArm(area, new Vector3(x - 6f, 0f, 112f), 30f, n * 10 + 1);
                RobotArm(area, new Vector3(x + 6f, 0f, 112f), -30f, n * 10 + 2);
                RobotArm(area, new Vector3(x - 6f, 0f, 146f), 150f, n * 10 + 3);
                HydraulicPress(area, new Vector3(x - 10f, 0f, 102f), 0f, n * 10 + 1);
                HydraulicPress(area, new Vector3(x + 10f, 0f, 146f), 0f, n * 10 + 2);
                n++;
            }
            FactoryCrane(area, new Vector3(0f, 0f, 118f), 1);
            FactoryCrane(area, new Vector3(0f, 0f, 158f), 2);
            Container(area, new Vector3(-12f, 0f, 92f), 0f, m.ContainerBlue, 21);
            Container(area, new Vector3(12f, 0f, 96f), 15f, m.ContainerRed, 22);
            Drum(area, new Vector3(-8f, 0f, 165f), 31);
            Drum(area, new Vector3(9f, 0f, 168f), 32);
            Drum(area, new Vector3(20f, 0f, 84f), 33);
        }

        /// <summary>A roofless factory hall: three walls and a front wall with a wide doorway.</summary>
        static void Hall(Transform parent, string name, Vector3 centre, float width, float depth)
        {
            var hall = new GameObject(name).transform;
            hall.SetParent(parent, true);
            const float h = 7f, t = 0.6f;
            void Wall(Vector3 pos, Vector3 size) => MarkStatic(Box("Wall", hall, centre + pos + Vector3.up * h * 0.5f, size, m.LockedGrey));
            Wall(new Vector3(-width * 0.5f, 0f, 0f), new Vector3(t, h, depth));
            Wall(new Vector3(width * 0.5f, 0f, 0f), new Vector3(t, h, depth));
            Wall(new Vector3(0f, 0f, depth * 0.5f), new Vector3(width, h, t));
            float side = (width - 10f) * 0.5f;
            Wall(new Vector3(-(10f + side) * 0.5f, 0f, -depth * 0.5f), new Vector3(side, h, t));
            Wall(new Vector3((10f + side) * 0.5f, 0f, -depth * 0.5f), new Vector3(side, h, t));
            MarkStatic(Box("Rust Stripe", hall, centre + new Vector3(0f, h - 0.6f, -depth * 0.5f - 0.32f), new Vector3(width, 0.5f, 0.05f), m.Rust, collider: false));
            Sign(hall, name.ToUpperInvariant(), centre + new Vector3(0f, h + 0.8f, -depth * 0.5f - 0.4f), 0f, 0.8f);
        }

        static void RobotArm(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T3_RobotArm_{index:00}", pos, yaw);
            if (PropModel(t, "RobotArm"))
            {
                ColliderBox(t, "Base", new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.6f, 1.4f));
                ColliderBox(t, "Turret", new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 0.5f, 0.9f));
                ColliderBox(t, "Lower Arm", new Vector3(0f, 1.9f, 0.3f), new Vector3(0.4f, 2f, 0.4f), new Vector3(18f, 0f, 0f));
                return;
            }
            CylLocal("Base", t, new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.3f, 1.4f), m.MetalDark);
            CylLocal("Turret", t, new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 0.25f, 0.9f), m.Hazard);
            var lower = BoxLocal("Lower Arm", t, new Vector3(0f, 1.9f, 0.3f), new Vector3(0.4f, 2f, 0.4f), m.Hazard);
            lower.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            var elbow = Part(PrimitiveType.Sphere, "Elbow", t, new Vector3(0f, 2.9f, 0.65f), Vector3.one * 0.55f, m.MetalDark);
            var upper = BoxLocal("Upper Arm", t, new Vector3(0f, 2.75f, 1.45f), new Vector3(0.32f, 0.32f, 1.6f), m.Hazard, collider: false);
            upper.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            BoxLocal("Gripper L", t, new Vector3(-0.15f, 2.45f, 2.25f), new Vector3(0.08f, 0.35f, 0.12f), m.MetalDark, collider: false);
            BoxLocal("Gripper R", t, new Vector3(0.15f, 2.45f, 2.25f), new Vector3(0.08f, 0.35f, 0.12f), m.MetalDark, collider: false);
            BoxLocal("Cable", t, new Vector3(0.25f, 1.5f, 0f), new Vector3(0.08f, 1.4f, 0.08f), m.Rubber, collider: false);
            BoxLocal("Rust", t, new Vector3(0.21f, 1.6f, 0.35f), new Vector3(0.02f, 0.6f, 0.3f), m.Rust, collider: false);
        }

        static void HydraulicPress(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T3_HydraulicPress_{index:00}", pos, yaw);
            if (PropModel(t, "HydraulicPress"))
            {
                ColliderBox(t, "Bed", new Vector3(0f, 0.5f, 0f), new Vector3(3.4f, 1f, 2f));
                ColliderBox(t, "Pillar L", new Vector3(-1.5f, 2.6f, 0f), new Vector3(0.5f, 4.2f, 1.2f));
                ColliderBox(t, "Pillar R", new Vector3(1.5f, 2.6f, 0f), new Vector3(0.5f, 4.2f, 1.2f));
                ColliderBox(t, "Crown", new Vector3(0f, 4.9f, 0f), new Vector3(3.6f, 0.9f, 1.5f));
                return;
            }
            BoxLocal("Bed", t, new Vector3(0f, 0.5f, 0f), new Vector3(3.4f, 1f, 2f), m.MetalDark);
            BoxLocal("Pillar L", t, new Vector3(-1.5f, 2.6f, 0f), new Vector3(0.5f, 4.2f, 1.2f), m.LockedGrey);
            BoxLocal("Pillar R", t, new Vector3(1.5f, 2.6f, 0f), new Vector3(0.5f, 4.2f, 1.2f), m.LockedGrey);
            BoxLocal("Crown", t, new Vector3(0f, 4.9f, 0f), new Vector3(3.6f, 0.9f, 1.5f), m.LockedGrey);
            BoxLocal("Ram", t, new Vector3(0f, 3.4f, 0f), new Vector3(2.2f, 1.2f, 1.1f), m.Hazard, collider: false);
            CylLocal("Piston", t, new Vector3(0f, 4.2f, 0f), new Vector3(0.5f, 0.5f, 0.5f), m.Metal, collider: false);
            BoxLocal("Control Box", t, new Vector3(2.3f, 1.4f, 0.6f), new Vector3(0.6f, 0.9f, 0.4f), m.VendingRed, collider: false);
            BoxLocal("Rust", t, new Vector3(-1.76f, 2.0f, 0f), new Vector3(0.02f, 1.6f, 0.8f), m.Rust, collider: false);
        }

        static void AssemblyLine(Transform parent, Vector3 pos, float yaw, int index)
        {
            var t = Prop(parent, $"T4_AssemblyLine_{index:00}", pos, yaw);
            if (PropModel(t, "AssemblyLine"))
            {
                ColliderBox(t, "Conveyor", new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 0.4f, 22f));
                return;
            }
            BoxLocal("Conveyor", t, new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 0.4f, 22f), m.MetalDark);
            BoxLocal("Belt", t, new Vector3(0f, 0.92f, 0f), new Vector3(1.5f, 0.04f, 21.6f), m.Rubber, collider: false);
            for (int i = -5; i <= 5; i++)
            {
                BoxLocal("Leg", t, new Vector3(-0.8f, 0.25f, i * 2f), new Vector3(0.12f, 0.5f, 0.12f), m.Metal, collider: false);
                BoxLocal("Leg", t, new Vector3(0.8f, 0.25f, i * 2f), new Vector3(0.12f, 0.5f, 0.12f), m.Metal, collider: false);
            }
            for (int i = -2; i <= 2; i++)
                BoxLocal("Crate", t, new Vector3(0f, 1.2f, i * 4f + 1f), new Vector3(0.9f, 0.55f, 0.9f), i % 2 == 0 ? m.Rust : m.ContainerTeal, collider: false);
            BoxLocal("Gantry L", t, new Vector3(-1.4f, 2f, -4f), new Vector3(0.3f, 4f, 0.3f), m.Hazard, collider: false);
            BoxLocal("Gantry R", t, new Vector3(1.4f, 2f, -4f), new Vector3(0.3f, 4f, 0.3f), m.Hazard, collider: false);
            BoxLocal("Gantry Top", t, new Vector3(0f, 4f, -4f), new Vector3(3.1f, 0.35f, 0.4f), m.Hazard, collider: false);
            BoxLocal("Scanner", t, new Vector3(0f, 3.5f, -4f), new Vector3(0.7f, 0.5f, 0.5f), m.Screen, collider: false);
            BoxLocal("Rust", t, new Vector3(0.91f, 0.7f, 5f), new Vector3(0.02f, 0.3f, 4f), m.Rust, collider: false);
        }

        static void FactoryCrane(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T4_FactoryCrane_{index:00}", pos, 0f);
            if (PropModel(t, "FactoryCrane"))
            {
                ColliderBox(t, "Main Beam", new Vector3(0f, 8.6f, 0f), new Vector3(10f, 0.8f, 1f));
                foreach (float x in new[] { -4.6f, 4.6f })
                foreach (float z in new[] { -3f, 3f })
                    ColliderBox(t, "Leg", new Vector3(x, 4.3f, z), new Vector3(0.5f, 8.6f, 0.5f));
                return;
            }
            BoxLocal("Main Beam", t, new Vector3(0f, 8.6f, 0f), new Vector3(10f, 0.8f, 1f), m.Hazard);
            foreach (float x in new[] { -4.6f, 4.6f })
            foreach (float z in new[] { -3f, 3f })
                BoxLocal("Leg", t, new Vector3(x, 4.3f, z), new Vector3(0.5f, 8.6f, 0.5f), m.LockedGrey);
            BoxLocal("Side Beam L", t, new Vector3(-4.6f, 8.6f, 0f), new Vector3(0.6f, 0.6f, 6.5f), m.LockedGrey, collider: false);
            BoxLocal("Side Beam R", t, new Vector3(4.6f, 8.6f, 0f), new Vector3(0.6f, 0.6f, 6.5f), m.LockedGrey, collider: false);
            BoxLocal("Trolley", t, new Vector3(1.2f, 8f, 0f), new Vector3(1.4f, 0.8f, 1.4f), m.MetalDark, collider: false);
            CylLocal("Cable", t, new Vector3(1.2f, 5.6f, 0f), new Vector3(0.08f, 2.4f, 0.08f), m.Rubber, collider: false);
            BoxLocal("Hook Block", t, new Vector3(1.2f, 3f, 0f), new Vector3(0.6f, 0.7f, 0.4f), m.Hazard, collider: false);
            BoxLocal("Rust", t, new Vector3(0f, 8.6f, -0.51f), new Vector3(4f, 0.4f, 0.02f), m.Rust, collider: false);
        }

        // ───────────────────────────── Phase 3: zones and machines ─────────────────────────────

        const float ZoneHalfDepth = 60f;
        const float ZoneFar = 245f;

        static void ZoneGate(Transform parent, string name, float x, int tier, string label, float signYaw)
        {
            var gate = Box(name, parent, new Vector3(x, 1.4f, 0f), new Vector3(0.4f, 2.8f, 12f), m.Hazard);
            var g = gate.AddComponent<MachineRowGate>();
            var so = new SerializedObject(g);
            so.FindProperty("requiredTier").intValue = tier;
            so.ApplyModifiedPropertiesWithoutUndo();
            Sign(parent, label, new Vector3(x + (signYaw > 0 ? 1.5f : -1.5f), 3.6f, 0f), signYaw, 0.6f);
        }

        static void BuildSpaceport()
        {
            var area = new GameObject("Spaceport Graveyard").transform;
            area.SetParent(SalvageRoot());
            ZoneGate(area, "Spaceport Gate", HalfWidth + 0.5f, 4, "SPACEPORT GRAVEYARD  ·  T4 TOOL REQUIRED", -90f);
            MarkStatic(Box("Spaceport Road", area, new Vector3((63f + ZoneFar) * 0.5f, 0.02f, -1.75f), new Vector3(ZoneFar - 63f, 0.04f, 7f), m.Road, collider: false));

            foreach (var (pos, r) in new[] { (new Vector3(195f, 0f, -26f), 26f), (new Vector3(168f, 0f, 32f), 14f), (new Vector3(222f, 0f, 30f), 11f) })
            {
                MarkStatic(Primitive(PrimitiveType.Cylinder, "Landing Pad", area, pos + Vector3.up * 0.04f, new Vector3(r * 2f, 0.04f, r * 2f), m.LockedGrey, collider: false));
                MarkStatic(Primitive(PrimitiveType.Cylinder, "Pad Ring", area, pos + Vector3.up * 0.05f, new Vector3(r * 2f + 1f, 0.03f, r * 2f + 1f), m.Hazard, collider: false));
            }

            // Crashed freighter, cut apart section by section.
            const float z = -26f;
            ShipPart(area, "T4_ShipNose_01", new Vector3(156f, 0f, z), p =>
            {
                if (PropModel(p, "ShipNose")) { ColliderBox(p, "Cockpit", new Vector3(0f, 3.2f, 0f), new Vector3(7f, 7f, 7f)); return; }
                CylLocal("Cockpit", p, new Vector3(0f, 3.2f, 0f), new Vector3(7f, 3f, 7f), m.BaseWhite).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Part(PrimitiveType.Sphere, "Nose Cone", p, new Vector3(-3.4f, 3.2f, 0f), new Vector3(5f, 6.2f, 6.2f), m.BaseWhite);
                BoxLocal("Canopy", p, new Vector3(-2.5f, 5.4f, 0f), new Vector3(3f, 1f, 3.6f), m.Glass, collider: false);
                BoxLocal("Stripe", p, new Vector3(0f, 3.2f, 3.52f), new Vector3(6f, 0.6f, 0.05f), m.TrimOrange, collider: false);
                BoxLocal("Rust", p, new Vector3(1f, 1.6f, -3.3f), new Vector3(3f, 1.2f, 0.4f), m.Rust, collider: false);
            });
            for (int i = 0; i < 2; i++)
            {
                float x = 172f + i * 18f;
                ShipPart(area, $"T5_ShipHull_{i + 1:00}", new Vector3(x, 0f, z), p =>
                {
                    if (PropModel(p, "ShipHull")) { ColliderBox(p, "Hull", new Vector3(0f, 3.6f, 0f), new Vector3(18f, 8f, 8f)); return; }
                    CylLocal("Hull", p, new Vector3(0f, 3.6f, 0f), new Vector3(8f, 9f, 8f), m.TruckGrey).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    for (int k = -3; k <= 3; k++)
                        CylLocal("Rib", p, new Vector3(k * 2.4f, 3.6f, 0f), new Vector3(8.2f, 0.2f, 8.2f), m.MetalDark, collider: false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    BoxLocal("Cargo Door", p, new Vector3(0f, 3f, 4.02f), new Vector3(6f, 3.6f, 0.1f), m.Rust, collider: false);
                    BoxLocal("Breach", p, new Vector3(2f, 6.4f, 2f), new Vector3(3f, 1.4f, 2f), m.MetalDark, collider: false);
                });
            }
            ShipPart(area, "T5_ShipEngine_01", new Vector3(208f, 0f, z), p =>
            {
                if (PropModel(p, "ShipEngine")) { ColliderBox(p, "Engine Block", new Vector3(0f, 3.6f, 0f), new Vector3(8f, 7.4f, 7.4f)); return; }
                CylLocal("Engine Block", p, new Vector3(0f, 3.6f, 0f), new Vector3(7.4f, 4f, 7.4f), m.LockedGrey).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                foreach (var off in new[] { new Vector2(1.8f, 1.8f), new Vector2(-1.8f, 1.8f), new Vector2(1.8f, -1.8f), new Vector2(-1.8f, -1.8f) })
                    CylLocal("Nozzle", p, new Vector3(5f, 3.6f + off.y, off.x), new Vector3(2.4f, 1.4f, 2.4f), m.MetalDark, collider: false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Part(PrimitiveType.Sphere, "Reactor Glow", p, new Vector3(0f, 6.6f, 0f), Vector3.one * 1.2f, m.Crystal);
                BoxLocal("Rust", p, new Vector3(0f, 1.5f, 3.4f), new Vector3(4f, 1.5f, 0.6f), m.Rust, collider: false);
            });
            ShipPart(area, "T4_ShipWing_01", new Vector3(184f, 0f, z + 14f), p =>
            {
                if (WingModel(p, new Vector3(0f, 1.6f, 0f), new Vector3(0f, 0f, -6f))) return;
                BoxLocal("Wing", p, new Vector3(0f, 1.6f, 0f), new Vector3(14f, 0.8f, 9f), m.BaseWhite).transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
                BoxLocal("Flap", p, new Vector3(5f, 1.1f, 0f), new Vector3(3f, 0.4f, 8f), m.TrimOrange, collider: false);
            });
            ShipPart(area, "T4_ShipWing_02", new Vector3(186f, 0f, z - 15f), p =>
            {
                if (WingModel(p, new Vector3(0f, 0.9f, 0f), new Vector3(8f, 0f, 4f))) return;
                BoxLocal("Wing", p, new Vector3(0f, 0.9f, 0f), new Vector3(14f, 0.8f, 9f), m.BaseWhite).transform.localRotation = Quaternion.Euler(8f, 0f, 4f);
                BoxLocal("Rust", p, new Vector3(-3f, 1.35f, 0f), new Vector3(4f, 0.05f, 5f), m.Rust, collider: false);
            });

            // A small shuttle on the north pad.
            ShipPart(area, "T4_ShuttleCabin_01", new Vector3(168f, 0f, 32f), p =>
            {
                if (PropModel(p, "ShuttleCabin")) { ColliderBox(p, "Cabin", new Vector3(0f, 2.2f, 0f), new Vector3(8f, 3.4f, 4.4f)); return; }
                BoxLocal("Cabin", p, new Vector3(0f, 2.2f, 0f), new Vector3(8f, 3.4f, 4.4f), m.BaseWhite);
                BoxLocal("Windows", p, new Vector3(-4.02f, 2.6f, 0f), new Vector3(0.05f, 1f, 3.6f), m.Glass, collider: false);
                BoxLocal("Stripe", p, new Vector3(0f, 2.2f, 2.22f), new Vector3(7.6f, 0.5f, 0.05f), m.TrimOrange, collider: false);
                foreach (float s in new[] { -1.5f, 1.5f })
                    BoxLocal("Skid", p, new Vector3(0f, 0.25f, s), new Vector3(8.4f, 0.3f, 0.3f), m.MetalDark, collider: false);
            });

            int n = 1;
            foreach (var pos in new[] { new Vector3(140f, 0f, 10f), new Vector3(150f, 0f, 45f), new Vector3(228f, 0f, 30f), new Vector3(232f, 0f, -50f), new Vector3(140f, 0f, -45f), new Vector3(205f, 0f, 48f) })
            {
                ShipPart(area, $"T3_CargoPod_{n++:00}", pos, p =>
                {
                    if (PropModel(p, "CargoPod")) { ColliderBox(p, "Pod", new Vector3(0f, 1.2f, 0f), new Vector3(4.4f, 2.2f, 2.2f)); return; }
                    Part(PrimitiveType.Capsule, "Pod", p, new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 1.6f, 2.2f), m.ContainerBlue).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    var col = new GameObject("Collider");
                    col.transform.SetParent(p, false);
                    col.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    col.AddComponent<BoxCollider>().size = new Vector3(4.4f, 2.2f, 2.2f);
                    BoxLocal("Band", p, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 2.3f, 2.3f), m.Hazard, collider: false);
                    BoxLocal("Rust", p, new Vector3(1.2f, 2.1f, 0f), new Vector3(1.2f, 0.4f, 1.4f), m.Rust, collider: false);
                }, Random.Range(0f, 360f));
            }
        }

        static void ShipPart(Transform parent, string name, Vector3 pos, System.Action<Transform> build, float yaw = 0f)
        {
            var t = Prop(parent, name, pos, yaw);
            build(t);
        }

        static void BuildDeepCore()
        {
            var area = new GameObject("Deep Core").transform;
            area.SetParent(SalvageRoot());
            ZoneGate(area, "Deep Core Gate", -HalfWidth - 0.5f, 5, "DEEP CORE MINE  ·  T5 TOOL REQUIRED", 90f);
            MarkStatic(Box("Mine Road", area, new Vector3((-59.5f - ZoneFar) * 0.5f, 0.02f, -1.75f), new Vector3(ZoneFar - 59.5f, 0.04f, 7f), m.Road, collider: false));

            // Cavern: tall rock walls and a roof so the mine is in shadow, lit by work lamps.
            var cave = new GameObject("Cavern").transform;
            cave.SetParent(area, true);
            float x0 = -ZoneFar, x1 = -HalfWidth, cx = (x0 + x1) * 0.5f, w = x1 - x0;
            MarkStatic(Box("Roof", cave, new Vector3(cx, 12f, 0f), new Vector3(w, 2f, ZoneHalfDepth * 2f), m.CaveRock));
            MarkStatic(Box("Wall North", cave, new Vector3(cx, 6f, ZoneHalfDepth), new Vector3(w, 12f, 2f), m.CaveRock));
            MarkStatic(Box("Wall South", cave, new Vector3(cx, 6f, -ZoneHalfDepth), new Vector3(w, 12f, 2f), m.CaveRock));
            MarkStatic(Box("Wall West", cave, new Vector3(x0, 6f, 0f), new Vector3(2f, 12f, ZoneHalfDepth * 2f), m.CaveRock));
            MarkStatic(Box("Wall East N", cave, new Vector3(x1 - 1f, 6f, (6f + ZoneHalfDepth) * 0.5f), new Vector3(2f, 12f, ZoneHalfDepth - 6f), m.CaveRock));
            MarkStatic(Box("Wall East S", cave, new Vector3(x1 - 1f, 6f, -(6f + ZoneHalfDepth) * 0.5f), new Vector3(2f, 12f, ZoneHalfDepth - 6f), m.CaveRock));
            MarkStatic(Box("Lintel", cave, new Vector3(x1 - 1f, 9f, 0f), new Vector3(2f, 6f, 12f), m.CaveRock));
            MarkStatic(Box("Cave Floor", cave, new Vector3(cx, 0.015f, 0f), new Vector3(w, 0.02f, ZoneHalfDepth * 2f), m.CaveRock, collider: false));
            var rng = new System.Random(31);
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector3(Mathf.Lerp(x0 + 8f, x1 - 14f, (float)rng.NextDouble()), 6f, Mathf.Lerp(-50f, 50f, (float)rng.NextDouble()));
                if (Mathf.Abs(p.z) < 8f) continue; // keep the main tunnel clear
                var pillar = Box("Rock Pillar", cave, p, new Vector3(3f + (float)rng.NextDouble() * 3f, 12f, 3f + (float)rng.NextDouble() * 3f), m.CaveRock, (float)rng.NextDouble() * 90f);
                MarkStatic(pillar);
            }
            foreach (var lp in new[] { new Vector3(-130f, 8f, 0f), new Vector3(-160f, 8f, 25f), new Vector3(-160f, 8f, -25f), new Vector3(-195f, 8f, 0f), new Vector3(-225f, 8f, 30f), new Vector3(-225f, 8f, -30f), new Vector3(-195f, 8f, 45f), new Vector3(-195f, 8f, -45f) })
            {
                var l = new GameObject("Work Lamp").AddComponent<Light>();
                l.transform.SetParent(cave, true);
                l.transform.position = lp;
                l.type = LightType.Point;
                l.color = new Color(1f, 0.78f, 0.5f);
                l.range = 30f;
                l.intensity = 3f;
                l.shadows = LightShadows.None;
                Box("Lamp", cave, lp + Vector3.up * 0.6f, new Vector3(1f, 0.4f, 1f), m.TrimYellow, collider: false);
            }

            int n = 1;
            foreach (var p in new[] { new Vector3(-150f, 0f, 30f), new Vector3(-185f, 0f, -30f), new Vector3(-222f, 0f, 12f) })
                DrillingRig(area, p, n++);
            n = 1;
            foreach (var p in new[] { new Vector3(-170f, 0f, 46f), new Vector3(-210f, 0f, -46f) })
                OreCrusher(area, p, n++);
            n = 1;
            foreach (var p in new[] { new Vector3(-138f, 0f, -20f), new Vector3(-176f, 0f, 14f), new Vector3(-200f, 0f, 36f), new Vector3(-232f, 0f, -24f), new Vector3(-158f, 0f, -48f), new Vector3(-214f, 0f, 50f) })
                CrystalVein(area, p, n++);
            n = 1;
            foreach (var p in new[] { new Vector3(-132f, 0f, 8f), new Vector3(-165f, 0f, -10f), new Vector3(-205f, 0f, -8f), new Vector3(-238f, 0f, 4f) })
                MineCart(area, p, n++);
        }

        static void DrillingRig(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T5_DrillingRig_{index:00}", pos, 0f);
            if (PropModel(t, "DrillingRig"))
            {
                ColliderBox(t, "Base", new Vector3(0f, 0.6f, 0f), new Vector3(5f, 1.2f, 5f));
                ColliderBox(t, "Tower Top", new Vector3(0f, 9f, 0f), new Vector3(2.6f, 0.6f, 2.6f));
                return;
            }
            BoxLocal("Base", t, new Vector3(0f, 0.6f, 0f), new Vector3(5f, 1.2f, 5f), m.MetalDark);
            foreach (float x in new[] { -1.8f, 1.8f })
            foreach (float z in new[] { -1.8f, 1.8f })
                BoxLocal("Tower Leg", t, new Vector3(x * 0.6f, 5f, z * 0.6f), new Vector3(0.3f, 8f, 0.3f), m.Hazard, collider: false);
            BoxLocal("Tower Top", t, new Vector3(0f, 9f, 0f), new Vector3(2.6f, 0.6f, 2.6f), m.Hazard);
            CylLocal("Drill Shaft", t, new Vector3(0f, 4f, 0f), new Vector3(0.6f, 4f, 0.6f), m.Metal, collider: false);
            CylLocal("Drill Bit", t, new Vector3(0f, 0.2f, 0f), new Vector3(1.4f, 0.4f, 1.4f), m.MetalDark, collider: false);
            BoxLocal("Motor", t, new Vector3(1.8f, 1.8f, 1.8f), new Vector3(1.4f, 1.2f, 1.4f), m.VendingRed, collider: false);
            BoxLocal("Rust", t, new Vector3(2.51f, 0.6f, 0f), new Vector3(0.02f, 0.8f, 3f), m.Rust, collider: false);
        }

        static void OreCrusher(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T5_OreCrusher_{index:00}", pos, 0f);
            if (PropModel(t, "OreCrusher"))
            {
                ColliderBox(t, "Housing", new Vector3(0f, 2f, 0f), new Vector3(6f, 4f, 4f));
                return;
            }
            BoxLocal("Housing", t, new Vector3(0f, 2f, 0f), new Vector3(6f, 4f, 4f), m.LockedGrey);
            BoxLocal("Hopper", t, new Vector3(0f, 4.6f, 0f), new Vector3(4f, 1.2f, 3f), m.Hazard, collider: false);
            CylLocal("Roller", t, new Vector3(0f, 3.2f, 2.05f), new Vector3(1.2f, 2.6f, 1.2f), m.MetalDark, collider: false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            BoxLocal("Chute", t, new Vector3(3.6f, 1f, 0f), new Vector3(1.6f, 0.6f, 1.6f), m.Metal, collider: false);
            BoxLocal("Rust", t, new Vector3(-3.01f, 1.6f, 0f), new Vector3(0.02f, 2f, 2.4f), m.Rust, collider: false);
        }

        static void CrystalVein(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T4_CrystalVein_{index:00}", pos, index * 47f);
            if (PropModel(t, "CrystalVein"))
            {
                ColliderBox(t, "Rock", new Vector3(0f, 0.8f, 0f), new Vector3(3f, 1.6f, 2.4f));
                return;
            }
            BoxLocal("Rock", t, new Vector3(0f, 0.8f, 0f), new Vector3(3f, 1.6f, 2.4f), m.CaveRock);
            var rng = new System.Random(index * 13);
            for (int i = 0; i < 6; i++)
            {
                var c = BoxLocal("Crystal", t, new Vector3((float)rng.NextDouble() * 2f - 1f, 1.6f + (float)rng.NextDouble(), (float)rng.NextDouble() * 1.6f - 0.8f),
                    new Vector3(0.35f, 1.2f + (float)rng.NextDouble(), 0.35f), m.Crystal, collider: false);
                c.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 40f - 20f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 40f - 20f);
            }
        }

        static void MineCart(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T3_MineCart_{index:00}", pos, 90f);
            if (PropModel(t, "MineCart"))
            {
                ColliderBox(t, "Tub", new Vector3(0f, 1f, 0f), new Vector3(1.6f, 1.1f, 2.4f));
                return;
            }
            BoxLocal("Tub", t, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 1.1f, 2.4f), m.Rust);
            BoxLocal("Ore", t, new Vector3(0f, 1.6f, 0f), new Vector3(1.4f, 0.3f, 2.2f), m.CaveRock, collider: false);
            Wheels(t, 0.7f, 0.8f, 0.3f);
            BoxLocal("Rail", t, new Vector3(-0.6f, 0.05f, 0f), new Vector3(0.1f, 0.1f, 6f), m.Metal, collider: false);
            BoxLocal("Rail", t, new Vector3(0.6f, 0.05f, 0f), new Vector3(0.1f, 0.1f, 6f), m.Metal, collider: false);
        }

        static void BuildAutomation()
        {
            var root = new GameObject("Automation").transform;
            var warehouse = GameObject.Find("Warehouse Drop-off").GetComponent<ScrapStorage>();

            // Recycling Factory south of the base, drawing from the warehouse.
            var fpos = new Vector3(-30f, 0f, -66f);
            var factoryGo = new GameObject("Recycling Factory");
            factoryGo.transform.SetParent(root, true);
            factoryGo.transform.position = fpos;
            var model = new GameObject("Model");
            model.transform.SetParent(factoryGo.transform, false);
            Part(PrimitiveType.Cube, "Hall", model.transform, new Vector3(0f, 3f, 0f), new Vector3(22f, 6f, 9f), m.BaseWhite);
            Part(PrimitiveType.Cube, "Stripe", model.transform, new Vector3(0f, 4.3f, 4.52f), new Vector3(22f, 0.6f, 0.05f), m.TrimTeal);
            Part(PrimitiveType.Cylinder, "Stack", model.transform, new Vector3(8f, 8f, -1f), new Vector3(1.4f, 4f, 1.4f), m.LockedGrey);
            var lightTip = Part(PrimitiveType.Sphere, "Stack Light", model.transform, new Vector3(8f, 12.4f, -1f), Vector3.one * 0.5f, m.BotEye).transform;
            Part(PrimitiveType.Cube, "Intake", model.transform, new Vector3(-8f, 1.5f, 5f), new Vector3(3f, 3f, 1.5f), m.Hazard);
            var shell = Box("Factory Foundation", factoryGo.transform, fpos + new Vector3(0f, 0.15f, 0f), new Vector3(23f, 0.3f, 10f), m.MetalDark);
            MarkStatic(shell);
            var hall = model.transform.Find("Hall").gameObject;
            hall.AddComponent<BoxCollider>();
            Sign(factoryGo.transform, "RECYCLING FACTORY", fpos + new Vector3(0f, 6.9f, 4.6f), 180f, 0.8f);
            var factory = factoryGo.AddComponent<RecyclingFactory>();
            factory.Setup(warehouse, lightTip);
            var fso = new SerializedObject(factory);
            fso.FindProperty("machineId").stringValue = "factory";
            fso.FindProperty("displayName").stringValue = "Recycling Factory";
            fso.FindProperty("price").intValue = 6000;
            fso.FindProperty("runningCostPerMinute").floatValue = 40f;
            fso.FindProperty("model").objectReferenceValue = model;
            fso.ApplyModifiedPropertiesWithoutUndo();
            var panel = Box("Factory Control Panel", factoryGo.transform, fpos + new Vector3(-4f, 0.8f, 5.6f), new Vector3(1.2f, 1.6f, 0.6f), m.MetalDark);
            BoxLocal("Screen", panel.transform, new Vector3(0f, 0.15f, 0.51f), new Vector3(0.7f, 0.35f, 0.05f), m.Screen, collider: false);
            panel.AddComponent<MachineControl>().Setup(factory);

            // Robot dock beside the warehouse.
            var dockGo = new GameObject("Robot Dock");
            dockGo.transform.SetParent(root, true);
            dockGo.transform.position = new Vector3(100f, 0f, -38f);
            MarkStatic(Primitive(PrimitiveType.Cylinder, "Dock Pad", dockGo.transform, new Vector3(100f, 0.05f, -38f), new Vector3(7f, 0.05f, 7f), m.TrimTeal, collider: false));
            Sign(dockGo.transform, "ROBOT DOCK", new Vector3(100f, 2.4f, -34f), 180f, 0.6f);
            var drop = new GameObject("Robot Drop Point").transform;
            drop.SetParent(dockGo.transform, true);
            drop.position = warehouse.transform.position + Vector3.up * 1f;
            var dock = dockGo.AddComponent<RobotDock>();
            dock.Setup(warehouse, drop);
            SetRef(dock, "bodyMaterial", m.BotYellow);
            SetRef(dock, "accentMaterial", m.TrimTeal);
            SetRef(dock, "eyeMaterial", m.BotEye);
            SetRef(dock, "darkMaterial", m.MetalDark);

            // Conveyor links from each collector pad to the warehouse.
            foreach (var pad in Object.FindObjectsByType<CollectorPad>(FindObjectsSortMode.InstanceID))
            {
                var link = new GameObject("Conveyor " + pad.PadId);
                link.transform.SetParent(root, true);
                Vector3 a = pad.transform.position + Vector3.right * 2f;
                Vector3 b = warehouse.transform.position + Vector3.left * 3f;
                var belt = new GameObject("Belt");
                belt.transform.SetParent(link.transform, false);
                Vector3 mid = (a + b) * 0.5f;
                float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
                float len = Vector3.Distance(a, b);
                var beltBox = Box("Belt Surface", belt.transform, mid + Vector3.up * 0.55f, new Vector3(1.1f, 0.12f, len), m.Rubber, yaw, collider: false);
                Box("Belt Frame", belt.transform, mid + Vector3.up * 0.4f, new Vector3(1.3f, 0.2f, len), m.Hazard, yaw, collider: false);
                int legs = Mathf.CeilToInt(len / 6f);
                for (int i = 0; i <= legs; i++)
                    Box("Leg", belt.transform, Vector3.Lerp(a, b, i / (float)legs) + Vector3.up * 0.2f, new Vector3(0.15f, 0.4f, 0.15f), m.Metal, yaw, collider: false);
                var crates = new Transform[8];
                for (int i = 0; i < crates.Length; i++)
                {
                    var c = Box("Crate", belt.transform, a, new Vector3(0.6f, 0.4f, 0.6f), i % 2 == 0 ? m.Rust : m.Metal, yaw, collider: false);
                    crates[i] = c.transform;
                }
                var conveyor = link.AddComponent<ConveyorLink>();
                string nice = pad.PadId.Replace("collector_", "");
                conveyor.Setup("conveyor_" + nice, $"Conveyor ({char.ToUpper(nice[0]) + nice.Substring(1)})", pad, warehouse, a, b, crates);
                var cso = new SerializedObject(conveyor);
                cso.FindProperty("price").intValue = 1500;
                cso.FindProperty("runningCostPerMinute").floatValue = 10f;
                cso.FindProperty("model").objectReferenceValue = belt;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ───────────────────────────── Phase 5: the Mega-Factory ─────────────────────────────

        const float MegaHalfWidth = 70f;
        const float MegaFar = -225f;

        static void BuildMegaFactory()
        {
            var area = new GameObject("Mega-Factory").transform;
            area.SetParent(SalvageRoot());

            var gate = Box("Mega-Factory Gate", area, new Vector3(0f, 1.4f, -HalfDepth - 0.5f), new Vector3(12f, 2.8f, 0.4f), m.Hazard);
            var g = gate.AddComponent<MachineRowGate>();
            var gso = new SerializedObject(g);
            gso.FindProperty("requiredTier").intValue = 5;
            gso.FindProperty("requiredMissionIndex").intValue = 20;
            gso.ApplyModifiedPropertiesWithoutUndo();
            Sign(area, "MEGA-FACTORY  ·  FINAL CONTRACT", new Vector3(0f, 3.6f, -HalfDepth + 1.5f), 180f, 0.6f);
            MarkStatic(Box("Mega Road", area, new Vector3(0f, 0.02f, (-HalfDepth + MegaFar) * 0.5f), new Vector3(9f, 0.04f, -MegaFar - HalfDepth), m.Road, collider: false));
            MarkStatic(Box("Base Link Road", area, new Vector3(0f, 0.02f, -63f), new Vector3(9f, 0.04f, 18f), m.Road, collider: false));

            // The building: tall walls, a wide north entrance, smoke stacks visible across the valley.
            var hall = new GameObject("Mega-Factory Hall").transform;
            hall.SetParent(area, true);
            float x0 = -58f, x1 = 58f, zN = -92f, zS = -210f, h = 18f, t = 1.2f;
            float cz = (zN + zS) * 0.5f, depth = zN - zS;
            MarkStatic(Box("Wall West", hall, new Vector3(x0, h * 0.5f, cz), new Vector3(t, h, depth), m.LockedGrey));
            MarkStatic(Box("Wall East", hall, new Vector3(x1, h * 0.5f, cz), new Vector3(t, h, depth), m.LockedGrey));
            MarkStatic(Box("Wall South", hall, new Vector3(0f, h * 0.5f, zS), new Vector3(x1 - x0, h, t), m.LockedGrey));
            float side = (x1 - x0 - 16f) * 0.5f;
            MarkStatic(Box("Wall North W", hall, new Vector3(x0 + side * 0.5f, h * 0.5f, zN), new Vector3(side, h, t), m.LockedGrey));
            MarkStatic(Box("Wall North E", hall, new Vector3(x1 - side * 0.5f, h * 0.5f, zN), new Vector3(side, h, t), m.LockedGrey));
            MarkStatic(Box("Entrance Lintel", hall, new Vector3(0f, h - 3f, zN), new Vector3(16f, 6f, t), m.LockedGrey));
            MarkStatic(Box("Rust Band", hall, new Vector3(0f, h - 1.5f, zN + 0.7f), new Vector3(x1 - x0, 1f, 0.1f), m.Rust, collider: false));
            Sign(hall, "MEGA-FACTORY", new Vector3(0f, h + 1.5f, zN + 0.8f), 0f, 2.4f);
            foreach (var sx in new[] { -45f, -15f, 15f, 45f })
            {
                MarkStatic(Primitive(PrimitiveType.Cylinder, "Smoke Stack", hall, new Vector3(sx, 16f, zS + 8f), new Vector3(5f, 16f, 5f), m.MetalDark, collider: false));
                MarkStatic(Primitive(PrimitiveType.Cylinder, "Stack Band", hall, new Vector3(sx, 28f, zS + 8f), new Vector3(5.2f, 0.8f, 5.2f), m.Hazard, collider: false));
            }

            // Reactor core in the middle, fed by four power pylons.
            var corePos = new Vector3(0f, 0f, -160f);
            var pylons = new DestructibleObject[4];
            int pi = 0;
            foreach (var off in new[] { new Vector3(-20f, 0f, 14f), new Vector3(20f, 0f, 14f), new Vector3(-20f, 0f, -14f), new Vector3(20f, 0f, -14f) })
            {
                var p = Prop(area, $"T5_FactoryPylon_{pi + 1:00}", corePos + off, 0f);
                if (PropModel(p, "FactoryPylon"))
                {
                    ColliderBox(p, "Pylon Base", new Vector3(0f, 1f, 0f), new Vector3(3.6f, 2f, 3.6f));
                    ColliderBox(p, "Pylon Column", new Vector3(0f, 6f, 0f), new Vector3(1.6f, 8f, 1.6f));
                }
                else
                {
                    BoxLocal("Pylon Base", p, new Vector3(0f, 1f, 0f), new Vector3(3.6f, 2f, 3.6f), m.MetalDark);
                    BoxLocal("Pylon Column", p, new Vector3(0f, 6f, 0f), new Vector3(1.6f, 8f, 1.6f), m.LockedGrey);
                    Part(PrimitiveType.Sphere, "Pylon Orb", p, new Vector3(0f, 10.6f, 0f), Vector3.one * 2f, m.BotEye);
                    BoxLocal("Hazard", p, new Vector3(0f, 2.2f, 1.82f), new Vector3(3f, 0.6f, 0.05f), m.Hazard, collider: false);
                }
                var cable = BoxLocal("Cable", p, new Vector3(-off.x * 0.25f, 0.3f, -off.z * 0.25f), new Vector3(0.4f, 0.3f, off.magnitude * 0.5f), m.Rubber, collider: false);
                cable.transform.localRotation = Quaternion.LookRotation(-off);
                pylons[pi++] = p.gameObject.AddComponent<DestructibleObject>();
                pylons[pi - 1].Setup(db.Dismantlable("factory_pylon"), p.name);
            }

            var core = Prop(area, "T5_ReactorCore_01", corePos, 0f);
            if (PropModel(core, "ReactorCore"))
            {
                ColliderBox(core, "Core Base", new Vector3(0f, 1.5f, 0f), new Vector3(14f, 3f, 14f));
                ColliderBox(core, "Core Body", new Vector3(0f, 5.5f, 0f), new Vector3(9f, 5f, 9f));
            }
            else
            {
            CylLocal("Core Base", core, new Vector3(0f, 1.5f, 0f), new Vector3(14f, 1.5f, 14f), m.MetalDark);
            CylLocal("Core Body", core, new Vector3(0f, 7f, 0f), new Vector3(9f, 5f, 9f), m.LockedGrey, collider: false);
            for (int i = 0; i < 4; i++)
                CylLocal("Core Ring", core, new Vector3(0f, 4f + i * 2.4f, 0f), new Vector3(10f, 0.3f, 10f), i % 2 == 0 ? m.Hazard : m.TrimOrange, collider: false);
            Part(PrimitiveType.Sphere, "Core Heart", core, new Vector3(0f, 13.5f, 0f), Vector3.one * 5f, m.Crystal);
            CylLocal("Core Cap", core, new Vector3(0f, 12.2f, 0f), new Vector3(6f, 0.5f, 6f), m.MetalDark, collider: false);
            }
            var coreObj = core.gameObject.AddComponent<DestructibleObject>();
            coreObj.Setup(db.Dismantlable("reactor_core"), core.name);

            var particleBase = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            var shieldMat = ParticleMaterial("CoreShield", particleBase, AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}/SoftDot.png"), new Color(0.35f, 0.8f, 1f, 0.22f));
            var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "Shield Bubble";
            Object.DestroyImmediate(bubble.GetComponent<Collider>());
            bubble.transform.SetParent(area, false);
            bubble.transform.position = corePos + Vector3.up * 7f;
            bubble.transform.localScale = Vector3.one * 22f;
            bubble.GetComponent<Renderer>().sharedMaterial = shieldMat;
            bubble.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            core.gameObject.AddComponent<CoreShield>().Setup(pylons, bubble);

            int n = 1;
            foreach (var p in new[] { new Vector3(-48f, 0f, -110f), new Vector3(48f, 0f, -110f), new Vector3(-48f, 0f, -195f), new Vector3(48f, 0f, -195f) })
                Smelter(area, p, n++);
            // Older machinery on the factory floor.
            HydraulicPress(area, new Vector3(-30f, 0f, -105f), 0f, 41);
            HydraulicPress(area, new Vector3(30f, 0f, -105f), 0f, 42);
            AssemblyLine(area, new Vector3(-44f, 0f, -150f), 0f, 41);
            AssemblyLine(area, new Vector3(44f, 0f, -150f), 0f, 42);
            RobotArm(area, new Vector3(-38f, 0f, -125f), 60f, 41);
            RobotArm(area, new Vector3(38f, 0f, -125f), -60f, 42);
            RobotArm(area, new Vector3(-38f, 0f, -178f), 120f, 43);
            RobotArm(area, new Vector3(38f, 0f, -178f), -120f, 44);
            FactoryCrane(area, new Vector3(0f, 0f, -120f), 41);
            FactoryCrane(area, new Vector3(0f, 0f, -198f), 42);
        }

        static void Smelter(Transform parent, Vector3 pos, int index)
        {
            var t = Prop(parent, $"T5_Smelter_{index:00}", pos, 0f);
            if (PropModel(t, "Smelter"))
            {
                ColliderBox(t, "Furnace", new Vector3(0f, 3.5f, 0f), new Vector3(6f, 7f, 6f));
                return;
            }
            CylLocal("Furnace", t, new Vector3(0f, 3.5f, 0f), new Vector3(6f, 3.5f, 6f), m.MetalDark);
            CylLocal("Crucible", t, new Vector3(0f, 7.6f, 0f), new Vector3(4.4f, 0.8f, 4.4f), m.Rust, collider: false);
            Part(PrimitiveType.Sphere, "Molten Glow", t, new Vector3(0f, 8.2f, 0f), new Vector3(3.6f, 0.6f, 3.6f), m.TrimOrange);
            BoxLocal("Pour Spout", t, new Vector3(3.4f, 5f, 0f), new Vector3(2f, 0.6f, 0.8f), m.Metal, collider: false);
            BoxLocal("Hazard", t, new Vector3(0f, 1.2f, 3.05f), new Vector3(4f, 0.6f, 0.05f), m.Hazard, collider: false);
            CylLocal("Vent", t, new Vector3(-1.8f, 9.5f, 0f), new Vector3(0.8f, 1.6f, 0.8f), m.LockedGrey, collider: false);
        }

        // ───────────────────────────── Gameplay wiring ─────────────────────────────

        /// <summary>Turns every T1_/T2_/T3_ prop into a dismantlable object with a stable save id.</summary>
        static void MakeDismantlable()
        {
            var map = new (string prefix, string id)[]
            {
                ("OilDrum", "oil_drum"), ("VendingBot", "vending_bot"), ("HoverScooter", "hover_scooter"),
                ("CargoContainer", "cargo_container"), ("Mule7Truck", "mule7_truck"), ("GeneratorUnit", "generator_unit"),
                ("RobotArm", "robot_arm"), ("HydraulicPress", "hydraulic_press"), ("AssemblyLine", "assembly_line"), ("FactoryCrane", "factory_crane"),
                ("CargoPod", "cargo_pod"), ("ShipNose", "ship_nose"), ("ShipWing", "ship_wing"), ("ShipHull", "ship_hull"), ("ShipEngine", "ship_engine"),
                ("ShuttleCabin", "shuttle_cabin"), ("MineCart", "mine_cart"), ("CrystalVein", "crystal_vein"), ("OreCrusher", "ore_crusher"), ("DrillingRig", "drilling_rig"), ("Smelter", "smelter")
            };
            int count = 0;
            foreach (var t in SalvageRoot().GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Length < 4 || t.name[0] != 'T' || t.name[2] != '_') continue;
                if (t.GetComponent<DestructibleObject>() != null) { count++; continue; } // already set up (pylons, core)
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

            var inventory = go.AddComponent<PlayerInventory>();
            var playerSink = go.AddComponent<PlayerScrapSink>();
            SetRef(playerSink, "inventory", inventory);
            var truck = Object.FindAnyObjectByType<SalvageTruck>();
            if (truck != null) SetRef(truck, "player", player);
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
            systems.AddComponent<AchievementManager>();
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
                Regolith = Lit("Regolith", Hex("#D6D8DB"), 0.06f, tex: regolithTex, tiling: new Vector2(200f, 200f)),
                Road = Lit("Road", Hex("#B4B8BD"), 0.1f, tex: regolithTex, tiling: new Vector2(1f, 12f)),
                Rim = Lit("CraterRim", Hex("#C3C6CA"), 0.04f, tex: regolithTex, tiling: new Vector2(2f, 2f)),
                CraterFloor = Lit("CraterFloor", Hex("#B6B9BD"), 0.05f, tex: regolithTex, tiling: new Vector2(4f, 4f)),
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
                Crystal = Lit("Crystal", Hex("#9B7BFF"), 0.9f, emission: Hex("#7A5BFF") * 1.6f),
                CaveRock = Lit("CaveRock", Hex("#5E6066"), 0.05f, tex: regolithTex, tiling: new Vector2(6f, 6f)),
                SuitOrange = Lit("SuitOrange", Hex("#EC6E24"), 0.35f),
                SuitOrangeDark = Lit("SuitOrangeDark", Hex("#C9561A"), 0.3f),
                SuitGrey = Lit("SuitGrey", Hex("#9AA3AB"), 0.5f, metallic: 0.3f),
                VisorBlack = Lit("VisorBlack", Hex("#07090C"), 0.95f, metallic: 0.2f),
                BotEye = Lit("BotEye", Hex("#57C7FF"), 0.8f, emission: Hex("#57C7FF") * 2.5f),
                WrenchRust = Lit("WrenchRust", Hex("#A0522D"), 0.2f, metallic: 0.3f),
                Fence = Lit("Fence", Hex("#6B737B"), 0.3f, metallic: 0.5f),
                Skin = Lit("Skin", Hex("#D9A27E"), 0.3f),
                Hair = Lit("Hair", Hex("#C8402A"), 0.25f),
                HairGrey = Lit("HairGrey", Hex("#C9CCD0"), 0.2f),
                Leather = Lit("Leather", Hex("#5B3A26"), 0.3f),
                Gold = Lit("Gold", Hex("#E8B52E"), 0.7f, metallic: 0.9f),
                Headlight = Lit("Headlight", Hex("#FFF4D6"), 0.9f, emission: Hex("#FFE9B0") * 1.5f),
                TealDark = Lit("TealDark", Hex("#14726B"), 0.3f),
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
            // Deep navy overhead fading to a brighter blue haze at the horizon (client asked for a less black sky).
            for (int y = 0; y < h; y++)
            {
                float up = Mathf.Abs(y / (float)(h - 1) - 0.5f) * 2f; // 0 at horizon, 1 at zenith/nadir
                float k = Mathf.Pow(1f - up, 3f);
                var c = new Color32((byte)Mathf.Lerp(18, 120, k), (byte)Mathf.Lerp(28, 150, k), (byte)Mathf.Lerp(58, 200, k), 255);
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }

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
                // Soft powdery regolith: gentle large-scale variation plus fine grain.
                float n = 0.55f * TileNoise(u, v, cells / 4) + 0.3f * TileNoise(u, v, cells / 2) + 0.15f * TileNoise(u, v, cells);
                float grain = TileNoise(u, v, cells * 8) - 0.5f;
                shade[y * size + x] = 0.9f + (n - 0.5f) * 0.1f + grain * 0.05f;
            }

            // A few small, faint craters (wrapping at the edges) so the tiling never shows.
            var rng = new System.Random(77);
            for (int c = 0; c < 22; c++)
            {
                float cx = (float)rng.NextDouble() * size, cy = (float)rng.NextDouble() * size;
                float r = 2.5f + Mathf.Pow((float)rng.NextDouble(), 3f) * 12f;
                int ir = Mathf.CeilToInt(r * 1.3f);
                for (int dy = -ir; dy <= ir; dy++)
                for (int dx = -ir; dx <= ir; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (d > 1.3f) continue;
                    float delta = d < 0.85f ? -0.05f * (1f - d / 0.85f) - 0.012f : 0.035f * (1f - Mathf.Abs(d - 1.05f) / 0.25f);
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
