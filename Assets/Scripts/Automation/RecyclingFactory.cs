using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// Input: the base warehouse. Processing: sorts and refines scrap at a steady rate.
    /// Output: credits at a bonus over depot prices, paid every few seconds. Costs credits to run.
    /// </summary>
    public class RecyclingFactory : Machine
    {
        [SerializeField] ScrapStorage input;
        [SerializeField] float unitsPerSecond = 3f;
        [Tooltip("Refined scrap sells for this much more than raw scrap.")]
        [SerializeField] float valueBonus = 0.3f;
        [SerializeField] Transform stackLight;

        float unitBudget;
        float pendingCredits;
        int pendingUnits;
        float payoutTimer;

        public float ValueBonus => valueBonus;
        public float UnitsPerSecond => unitsPerSecond;
        protected override string IdleReason => "Waiting for scrap in the warehouse";

        public void Setup(ScrapStorage warehouse, Transform light)
        {
            input = warehouse;
            stackLight = light;
        }

        protected override bool Process(float dt)
        {
            payoutTimer += dt;
            if (payoutTimer >= 4f) Payout();

            if (input == null || input.TotalUnits == 0) return pendingUnits > 0;
            unitBudget += unitsPerSecond * dt;
            while (unitBudget >= 1f && input.TotalUnits > 0)
            {
                unitBudget -= 1f;
                foreach (var r in GameDatabase.Instance.resources)
                {
                    if (input.Count(r) <= 0) continue;
                    input.SetCount(r, input.Count(r) - 1);
                    pendingCredits += r.unitValue * (1f + valueBonus);
                    pendingUnits++;
                    break;
                }
            }
            if (stackLight != null) stackLight.localScale = Vector3.one * (0.35f + Mathf.PingPong(Time.time, 0.5f) * 0.3f);
            return true;
        }

        void Payout()
        {
            payoutTimer = 0f;
            int credits = Mathf.FloorToInt(pendingCredits);
            if (credits <= 0) return;
            pendingCredits -= credits;
            Wallet.Instance.Add(credits, $"Factory output ({pendingUnits} units refined)");
            pendingUnits = 0;
            AudioManager.Play("sell_register", 0.35f, 1.2f);
        }
    }
}
