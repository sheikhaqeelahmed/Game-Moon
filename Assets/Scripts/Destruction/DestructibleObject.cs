using System.Collections.Generic;
using System.Text;
using PlanetScrappers.Core;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    public enum HitResult { TooWeak, Damaged, Broken }

    /// <summary>
    /// Anything the player can take apart. Durability drops with each tool hit; at 66%, 33% and
    /// 10% the object visibly loses parts (dented, stripped, bare frame). At zero it breaks,
    /// spawns its scrap and later respawns as a fresh salvage drop.
    /// </summary>
    public class DestructibleObject : MonoBehaviour
    {
        public static readonly List<DestructibleObject> All = new List<DestructibleObject>();

        [SerializeField] DismantlableDefinition definition;
        [Tooltip("Stable id used by save files.")]
        [SerializeField] string uniqueId;

        static readonly float[] StageThresholds = { 0.66f, 0.33f, 0.10f };
        static readonly float[] StageHiddenShare = { 0.25f, 0.5f, 0.8f };
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Renderer[] parts;
        Collider[] colliders;
        Color[] baseColors;
        MaterialPropertyBlock block;
        float durability;
        int stage;
        float shakeTime;
        Vector3 restPosition;
        float respawnTimer;
        float lastShieldHint = -100f;

        public DismantlableDefinition Definition => definition;
        public string UniqueId => uniqueId;
        public float Durability => durability;
        public float MaxDurability => definition != null ? definition.durability : 1f;
        public float Fraction => Mathf.Clamp01(durability / MaxDurability);
        public int Stage => stage;
        public bool IsDismantled { get; private set; }
        public float RespawnTimer => respawnTimer;
        public Vector3 Center { get; private set; }

        public string StageName => stage switch { 0 => "Intact", 1 => "Dented", 2 => "Stripped", _ => "Bare frame" };

        public void Setup(DismantlableDefinition def, string id)
        {
            definition = def;
            uniqueId = id;
        }

        void Awake()
        {
            parts = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            block = new MaterialPropertyBlock();
            baseColors = new Color[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var mat = parts[i].sharedMaterial;
                baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            }
            restPosition = transform.position;
            durability = MaxDurability;
            Center = ComputeCenter();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        Vector3 ComputeCenter()
        {
            if (parts.Length == 0) return transform.position;
            var b = parts[0].bounds;
            foreach (var r in parts) b.Encapsulate(r.bounds);
            return b.center;
        }

        public HitResult Hit(ToolDefinition tool, Vector3 point, Vector3 normal)
        {
            if (IsDismantled || tool == null) return HitResult.TooWeak;
            var shield = GetComponent<CoreShield>();
            if (shield != null && shield.Active)
            {
                AudioManager.PlayVaried("hit_bonk", 0.7f * Audible());
                DestructionFx.Instance?.Sparks(point, normal, 6);
                if (Time.time - lastShieldHint > 6f)
                {
                    lastShieldHint = Time.time;
                    GameEvents.Say($"Shielded. {shield.PylonsStanding} power pylon{(shield.PylonsStanding == 1 ? "" : "s")} still feeding it. Take them down first.", 5f);
                }
                return HitResult.TooWeak;
            }
            if (tool.tier < definition.tier)
            {
                AudioManager.PlayVaried("hit_bonk", 0.9f * Audible());
                DestructionFx.Instance?.Sparks(point, normal, 3);
                shakeTime = 0.08f;
                return HitResult.TooWeak;
            }

            durability -= tool.damage;
            shakeTime = 0.15f;
            AudioManager.PlayVaried("hit_metal", 0.75f * Audible(), 0.12f);
            DestructionFx.Instance?.Sparks(point, normal, 10);
            UpdateStage(point);

            if (durability <= 0f)
            {
                Break(tool);
                return HitResult.Broken;
            }
            return HitResult.Damaged;
        }

        /// <summary>Volume factor by distance from the player: full within 10 m, silent beyond 45 m.</summary>
        float Audible()
        {
            var p = PlayerInventory.Instance;
            if (p == null) return 1f;
            float d = Vector3.Distance(p.transform.position, Center);
            return Mathf.Clamp01(1f - (d - 10f) / 35f);
        }

        void UpdateStage(Vector3 hitPoint)
        {
            int newStage = 0;
            for (int i = 0; i < StageThresholds.Length; i++)
                if (Fraction <= StageThresholds[i]) newStage = i + 1;
            if (newStage == stage) return;
            stage = newStage;
            ApplyStage(true, hitPoint);
        }

        void ApplyStage(bool withDebris, Vector3 from)
        {
            // Part 0 is the main body and always stays; the rest come off in a fixed order.
            int removable = parts.Length - 1;
            int hide = stage == 0 ? 0 : Mathf.RoundToInt(removable * StageHiddenShare[stage - 1]);
            for (int i = 1; i < parts.Length; i++)
            {
                bool shouldShow = (parts.Length - i) > hide;
                if (parts[i].enabled && !shouldShow && withDebris)
                    DestructionFx.Instance?.Debris(parts[i].bounds.center, baseColors[i], from);
                parts[i].enabled = shouldShow;
            }

            float darken = 1f - 0.12f * stage;
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, baseColors[i] * new Color(darken, darken * 0.97f, darken * 0.93f, 1f));
                parts[i].SetPropertyBlock(block);
            }
        }

        void Break(ToolDefinition tool)
        {
            IsDismantled = true;
            durability = 0f;
            respawnTimer = definition.respawnSeconds;
            SetPresent(false);
            AudioManager.PlayVaried("break_crunch", Audible(), 0.06f);
            DestructionFx.Instance?.BreakBurst(Center, parts.Length > 0 ? baseColors[0] : Color.grey);
            ScrapSpawner.Instance?.SpawnYield(definition, tool.recoveryBonus, Center);
            GameEvents.RaiseObjectDismantled(this);
        }

        void SetPresent(bool present)
        {
            foreach (var r in parts) r.enabled = present;
            foreach (var c in colliders) c.enabled = present;
        }

        /// <summary>Puts the object back as new (salvage drop or save load).</summary>
        public void Restore()
        {
            IsDismantled = false;
            durability = MaxDurability;
            stage = 0;
            respawnTimer = 0f;
            SetPresent(true);
            ApplyStage(false, Vector3.zero);
        }

        /// <summary>Applies saved state: dismantled with a respawn timer, or a damaged durability.</summary>
        public void LoadState(bool dismantled, float savedDurability, float savedRespawn)
        {
            if (dismantled)
            {
                IsDismantled = true;
                durability = 0f;
                respawnTimer = savedRespawn;
                SetPresent(false);
                return;
            }
            Restore();
            durability = Mathf.Clamp(savedDurability, 1f, MaxDurability);
            UpdateStage(Center);
        }

        void Update()
        {
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                bool calm = SettingsManager.Current.reduceMotion;
                transform.position = shakeTime > 0f && !calm ? restPosition + Random.insideUnitSphere * 0.03f : restPosition;
            }

            if (IsDismantled && definition.respawnSeconds > 0f)
            {
                respawnTimer -= Time.deltaTime;
                var player = PlayerInventory.Instance;
                bool playerFar = player == null || (player.transform.position - Center).sqrMagnitude > 30f * 30f;
                if (respawnTimer <= 0f && playerFar) Restore();
            }
        }

        /// <summary>SCRAP-BOT's scan read-out.</summary>
        public string ScanText(ToolDefinition tool)
        {
            var sb = new StringBuilder();
            var sh = GetComponent<CoreShield>();
            if (sh != null && sh.Active) sb.Append($"Shield up, powered by {sh.PylonsStanding} pylons. ");
            sb.Append($"{definition.displayName}: tier {definition.tier}, {Mathf.CeilToInt(durability)}/{definition.durability:0}. Inside: ");
            for (int i = 0; i < definition.yields.Length; i++)
            {
                var y = definition.yields[i];
                if (i > 0) sb.Append(", ");
                sb.Append(y.chance < 1f ? $"maybe {y.amount} {y.resource.displayName.ToLower()}" : $"{y.amount} {y.resource.displayName.ToLower()}");
            }
            sb.Append('.');
            if (tool == null) sb.Append(" You'll need a tool first.");
            else if (tool.tier < definition.tier) sb.Append($" Your {tool.displayName} won't scratch it. Needs tier {definition.tier}.");
            return sb.ToString();
        }
    }
}
