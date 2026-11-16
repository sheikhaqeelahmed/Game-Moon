using PlanetScrappers.Automation;
using PlanetScrappers.Core;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;

namespace PlanetScrappers.Economy
{
    public enum PurchaseResult { Ok, AlreadyOwned, Locked, CannotAfford, MaxLevel }

    /// <summary>Selling at the Recycling Depot and buying from Nova's Upgrade Station.</summary>
    public static class Trading
    {
        public static int SellAll()
        {
            var inv = PlayerInventory.Instance;
            int total = 0, units = 0;
            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = inv.Count(r);
                if (n <= 0) continue;
                inv.Remove(r, n);
                total += n * r.unitValue;
                units += n;
            }
            Complete(total, units);
            return total;
        }

        public static int SellResource(ResourceDefinition r)
        {
            var inv = PlayerInventory.Instance;
            int n = inv.Count(r);
            if (n <= 0) return 0;
            inv.Remove(r, n);
            int total = n * r.unitValue;
            Complete(total, n, r.displayName);
            return total;
        }

        /// <summary>Sells everything in a storage (truck bin, warehouse).</summary>
        public static int SellStorage(ScrapStorage storage)
        {
            if (storage == null) return 0;
            int units = storage.TotalUnits;
            int total = 0;
            foreach (var kv in storage.TakeAll()) total += kv.Key.unitValue * kv.Value;
            Complete(total, units, "from the " + storage.DisplayName.ToLowerInvariant());
            return total;
        }

        static void Complete(int total, int units, string what = null)
        {
            if (total <= 0) return;
            Wallet.Instance.Add(total, what == null ? $"Sold {units} units" : $"Sold {units} {what}");
            AudioManager.Play("sell_register", 0.8f);
            GameEvents.RaiseScrapSold(total);
        }

        public static PurchaseResult CanBuy(ToolDefinition tool)
        {
            var eq = PlayerEquipment.Instance;
            if (eq.Owns(tool)) return PurchaseResult.AlreadyOwned;
            if (!tool.availableInPrototype) return PurchaseResult.Locked;
            if (!Wallet.Instance.CanAfford(tool.price)) return PurchaseResult.CannotAfford;
            return PurchaseResult.Ok;
        }

        public static PurchaseResult BuyTool(ToolDefinition tool)
        {
            var result = CanBuy(tool);
            if (result != PurchaseResult.Ok) return result;
            Wallet.Instance.TrySpend(tool.price, "Bought " + tool.displayName);
            PlayerEquipment.Instance.Grant(tool, true);
            AudioManager.Play("purchase", 0.9f);
            GameEvents.RaiseToolPurchased(tool);
            return PurchaseResult.Ok;
        }

        public static PurchaseResult BuyMachine(Machine m)
        {
            if (m == null) return PurchaseResult.Locked;
            if (m.Built) return PurchaseResult.AlreadyOwned;
            if (!m.CanBuild) return PurchaseResult.Locked;
            if (!Wallet.Instance.TrySpend(m.Price, "Built " + m.DisplayName)) return PurchaseResult.CannotAfford;
            m.SetBuilt(true, true);
            AudioManager.Play("purchase", 0.9f);
            GameEvents.RaiseInteracted("build_" + m.MachineId);
            return PurchaseResult.Ok;
        }

        public static PurchaseResult BuyRobot()
        {
            var dock = RobotDock.Instance;
            if (dock == null || dock.Count >= dock.MaxRobots) return PurchaseResult.MaxLevel;
            if (!Wallet.Instance.TrySpend(dock.Price, "Bought Salvage Robot")) return PurchaseResult.CannotAfford;
            dock.Spawn();
            AudioManager.Play("purchase", 0.9f);
            GameEvents.RaiseInteracted("buy_robot");
            return PurchaseResult.Ok;
        }

        public static PurchaseResult BuyBackpack()
        {
            var inv = PlayerInventory.Instance;
            if (!inv.CanUpgrade) return PurchaseResult.MaxLevel;
            int price = inv.NextUpgradePrice;
            if (!Wallet.Instance.TrySpend(price, $"Backpack upgrade to {inv.NextCapacity} units"))
                return PurchaseResult.CannotAfford;
            inv.SetCapacityLevel(inv.CapacityLevel + 1);
            AudioManager.Play("purchase", 0.9f);
            GameEvents.RaiseBackpackUpgraded(inv.CapacityLevel);
            return PurchaseResult.Ok;
        }
    }
}
