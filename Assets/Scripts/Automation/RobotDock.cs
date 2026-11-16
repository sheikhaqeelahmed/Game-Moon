using System.Collections.Generic;
using PlanetScrappers.Destruction;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// Home of the salvage robots beside the warehouse. Builds the drones (bought at Nova's),
    /// keeps two robots from chasing the same object, and limits them to Rust Valley and the
    /// Iron District.
    /// </summary>
    public class RobotDock : MonoBehaviour
    {
        public static RobotDock Instance { get; private set; }

        [SerializeField] ScrapStorage warehouse;
        [SerializeField] Transform dropPoint;
        [SerializeField] int maxRobots = 3;
        [SerializeField] int price = 6000;
        [SerializeField] Material bodyMaterial, accentMaterial, eyeMaterial, darkMaterial;
        [SerializeField] Vector2 workMin = new Vector2(-115f, -75f);
        [SerializeField] Vector2 workMax = new Vector2(115f, 175f);

        readonly List<SalvageRobot> robots = new List<SalvageRobot>();

        public ScrapStorage Warehouse => warehouse;
        public Vector3 DropPoint => dropPoint != null ? dropPoint.position : transform.position;
        public int Count => robots.Count;
        public int MaxRobots => maxRobots;
        public int Price => price;
        public IReadOnlyList<SalvageRobot> Robots => robots;

        void Awake() => Instance = this;

        public void Setup(ScrapStorage store, Transform drop) { warehouse = store; dropPoint = drop; }

        public bool InWorkArea(Vector3 p) => p.x >= workMin.x && p.x <= workMax.x && p.z >= workMin.y && p.z <= workMax.y;

        public bool IsClaimed(DestructibleObject o, SalvageRobot asking)
        {
            foreach (var r in robots) if (r != asking && r.Target == o) return true;
            return false;
        }

        public void SetCount(int count)
        {
            while (robots.Count < Mathf.Min(count, maxRobots)) Spawn();
        }

        public SalvageRobot Spawn()
        {
            if (robots.Count >= maxRobots) return null;
            var go = new GameObject($"Salvage Robot {robots.Count + 1}");
            go.transform.position = transform.position + Vector3.up * (3f + robots.Count) + Vector3.right * robots.Count * 2f;

            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            Part(PrimitiveType.Sphere, model, Vector3.zero, new Vector3(1.1f, 0.6f, 1.1f), bodyMaterial);
            Part(PrimitiveType.Cylinder, model, new Vector3(0f, -0.1f, 0f), new Vector3(1.4f, 0.06f, 1.4f), accentMaterial);
            Part(PrimitiveType.Cube, model, new Vector3(0f, 0.05f, 0.5f), new Vector3(0.5f, 0.18f, 0.08f), darkMaterial);
            Part(PrimitiveType.Sphere, model, new Vector3(-0.12f, 0.06f, 0.55f), Vector3.one * 0.1f, eyeMaterial);
            Part(PrimitiveType.Sphere, model, new Vector3(0.12f, 0.06f, 0.55f), Vector3.one * 0.1f, eyeMaterial);
            Part(PrimitiveType.Cube, model, new Vector3(0f, -0.45f, 0.2f), new Vector3(0.08f, 0.6f, 0.08f), darkMaterial);
            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(model, false);
            rotor.localPosition = new Vector3(0f, 0.4f, 0f);
            Part(PrimitiveType.Cube, rotor, Vector3.zero, new Vector3(1.6f, 0.03f, 0.12f), darkMaterial);
            Part(PrimitiveType.Cube, rotor, Vector3.zero, new Vector3(0.12f, 0.03f, 1.6f), darkMaterial);

            var hopper = go.AddComponent<ScrapStorage>();
            hopper.Setup($"robot_{robots.Count + 1}", "Robot hopper", 40);
            var magnet = go.AddComponent<StorageScrapSink>();
            magnet.Setup(hopper, go.transform, 7f);
            var robot = go.AddComponent<SalvageRobot>();
            robot.Init(this, hopper, magnet, rotor);
            robots.Add(robot);
            return robot;
        }

        static void Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(type);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
