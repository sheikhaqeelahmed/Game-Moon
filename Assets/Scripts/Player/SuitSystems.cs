using System;
using PlanetScrappers.Core;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Player
{
    /// <summary>
    /// Health and oxygen. Oxygen drains in the open and refills near the base. With no oxygen,
    /// health drains; at zero the suit fails, the Rookie is brought back to base and half the
    /// backpack is lost. Health slowly regenerates while oxygen is available.
    /// </summary>
    public class SuitSystems : MonoBehaviour
    {
        public static SuitSystems Instance { get; private set; }

        [SerializeField] PlayerController controller;
        [SerializeField] Transform baseCentre;

        public float Health { get; private set; } = 100f;
        public float Oxygen { get; private set; } = 100f;
        public bool NearBase { get; private set; }
        public event Action SuitFailed;

        bool warnedLow, warnedEmpty;

        void Awake() => Instance = this;

        public void SetBase(Transform t) => baseCentre = t;

        public void SetState(float health, float oxygen)
        {
            Health = Mathf.Clamp(health, 1f, 100f);
            Oxygen = Mathf.Clamp(oxygen, 0f, 100f);
        }

        void Update()
        {
            var db = GameDatabase.Instance;
            float dt = Time.deltaTime;
            NearBase = baseCentre != null && (transform.position - baseCentre.position).sqrMagnitude < db.baseSafeRadius * db.baseSafeRadius;

            if (NearBase)
            {
                Oxygen = Mathf.Min(100f, Oxygen + db.oxygenRefillPerSecond * dt);
                if (Oxygen > 50f) warnedLow = warnedEmpty = false;
            }
            else
            {
                Oxygen = Mathf.Max(0f, Oxygen - 100f / db.oxygenSeconds * dt);
            }

            if (Oxygen > 0f) Health = Mathf.Min(100f, Health + 2f * dt);
            else Health -= 6f * dt;

            if (!warnedLow && Oxygen < 25f)
            {
                warnedLow = true;
                AudioManager.Play("warning", 0.8f);
                GameEvents.Say("Oxygen at 25%. The base refills your tanks. I'd head back. I don't breathe, but you do.", 7f);
            }
            if (!warnedEmpty && Oxygen <= 0f)
            {
                warnedEmpty = true;
                AudioManager.Play("warning", 1f);
                GameEvents.Say("Oxygen empty! Your suit is losing health. Get to the base, now!", 7f);
            }

            if (Health <= 0f) Fail();
        }

        void Fail()
        {
            PlayerInventory.Instance?.LoseFraction(0.5f);
            Health = 100f;
            Oxygen = 100f;
            warnedLow = warnedEmpty = false;
            if (baseCentre != null)
                controller.Teleport(baseCentre.position + baseCentre.forward * 8f, baseCentre.rotation);
            SuitFailed?.Invoke();
            GameEvents.Say("Suit failure. I dragged you back to base. Some scrap fell out on the way. Sorry. Not sorry.", 8f);
        }
    }
}
