using System.Collections.Generic;
using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// Owns the pool of scrap pieces and turns an object's yield table into flying chunks.
    /// At most a few chunks per resource, each carrying several units, so big objects don't
    /// flood the scene with physics.
    /// </summary>
    public class ScrapSpawner : MonoBehaviour
    {
        public static ScrapSpawner Instance { get; private set; }

        [SerializeField] Material scrapMaterial;
        [SerializeField] int poolSize = 120;
        [SerializeField] int maxChunksPerResource = 4;

        readonly List<ScrapPiece> pool = new List<ScrapPiece>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        Mesh cube, cylinder, sphere;
        float comboTime;
        int combo;
        float fullWarningCooldown;

        void Awake()
        {
            Instance = this;
            cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            for (int i = 0; i < poolSize; i++) pool.Add(CreatePiece(i));
        }

        ScrapPiece CreatePiece(int index)
        {
            var go = new GameObject("Scrap " + index);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = scrapMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.SetActive(false);
            return go.AddComponent<ScrapPiece>();
        }

        public Vector3 ScaleFor(ResourceDefinition r) => r.id switch
        {
            "iron" => new Vector3(0.26f, 0.12f, 0.2f),
            "copper" => new Vector3(0.2f, 0.07f, 0.2f),
            "electronics" => new Vector3(0.22f, 0.04f, 0.16f),
            _ => new Vector3(0.16f, 0.16f, 0.16f)
        };

        Mesh MeshFor(ResourceDefinition r) => r.id switch
        {
            "copper" => cylinder,
            "rare_alloy" => sphere,
            _ => cube
        };

        public void SpawnYield(DismantlableDefinition def, float recoveryBonus, Vector3 origin)
        {
            foreach (var y in def.yields)
            {
                if (y.resource == null || Random.value > y.chance) continue;
                int total = Mathf.Max(1, Mathf.RoundToInt(y.amount * (1f + recoveryBonus)));
                int chunks = Mathf.Clamp(total, 1, maxChunksPerResource);
                for (int i = 0; i < chunks; i++)
                {
                    int units = total / chunks + (i < total % chunks ? 1 : 0);
                    Vector3 dir = Random.insideUnitSphere;
                    dir.y = Mathf.Abs(dir.y) + 1.4f;
                    Spawn(y.resource, units, origin + Vector3.up * 0.3f, dir.normalized * Random.Range(2.2f, 3.6f));
                }
            }
        }

        void Spawn(ResourceDefinition r, int units, Vector3 pos, Vector3 vel)
        {
            var piece = pool.Find(p => !p.Active);
            if (piece == null) return; // pool exhausted: very unlikely, and safe to skip
            piece.GetComponent<MeshFilter>().sharedMesh = MeshFor(r);
            piece.transform.localScale = ScaleFor(r);
            var renderer = piece.GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, r.color);
            block.SetColor(EmissionId, r.id == "rare_alloy" ? r.color * 0.8f : Color.black);
            renderer.SetPropertyBlock(block);
            piece.Launch(this, r, units, pos, vel);
        }

        public void Release(ScrapPiece piece) => piece.gameObject.SetActive(false);

        /// <summary>Rising "tink" for pickups in quick succession.</summary>
        public void OnCollected()
        {
            combo = Time.time - comboTime < 0.6f ? Mathf.Min(combo + 1, 10) : 0;
            comboTime = Time.time;
            AudioManager.Play("pickup_tink", 0.55f, 1f + combo * 0.06f);
        }

        void Update()
        {
            fullWarningCooldown -= Time.deltaTime;
            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.IsFull || fullWarningCooldown > 0f) return;
            foreach (var p in pool)
            {
                if (!p.Active) continue;
                if ((p.transform.position - inv.transform.position).sqrMagnitude < 16f)
                {
                    GameEvents.Say("Backpack's full. Sell at the Recycling Depot, or ask Nova for a bigger one.", 5f);
                    AudioManager.Play("warning", 0.6f);
                    fullWarningCooldown = 20f;
                    break;
                }
            }
        }
    }
}
