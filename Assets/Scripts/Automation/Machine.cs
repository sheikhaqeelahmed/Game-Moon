using System.Collections.Generic;
using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// Base for automated machines: built through Nova's shop, switched on and off at the machine,
    /// and charged a running cost per minute while they are actually working.
    /// Subclasses define input, processing and output.
    /// </summary>
    public abstract class Machine : MonoBehaviour
    {
        public static readonly List<Machine> All = new List<Machine>();

        [SerializeField] protected string machineId = "machine";
        [SerializeField] protected string displayName = "Machine";
        [SerializeField] protected int price = 1000;
        [Tooltip("Credits charged per minute while the machine is working.")]
        [SerializeField] protected float runningCostPerMinute = 30f;
        [SerializeField] protected GameObject model;

        float costOwed;

        public string MachineId => machineId;
        public string DisplayName => displayName;
        public int Price => price;
        public float RunningCostPerMinute => runningCostPerMinute;
        public bool Built { get; private set; }
        public bool Powered { get; private set; }
        public bool Working { get; protected set; }
        public virtual bool CanBuild => true;
        public virtual string CannotBuildReason => "";
        public string Status => !Built ? "Not built" : !Powered ? "Switched off" : Working ? "Working" : IdleReason;
        protected virtual string IdleReason => "Idle";

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public static Machine Find(string id) => All.Find(m => m.machineId == id);

        protected virtual void Start() => SetBuilt(Built, Powered);

        public void SetBuilt(bool built, bool powered)
        {
            Built = built;
            Powered = built && powered;
            if (model != null) model.SetActive(built);
            OnStateChanged();
        }

        public void SetPowered(bool on)
        {
            if (!Built) return;
            Powered = on;
            OnStateChanged();
        }

        protected virtual void OnStateChanged() { }

        void Update()
        {
            Working = false;
            if (!Built || !Powered || Time.timeScale == 0f) return;
            Working = Process(Time.deltaTime);
            if (!Working) return;

            costOwed += runningCostPerMinute / 60f * Time.deltaTime;
            if (costOwed >= 1f)
            {
                int whole = Mathf.FloorToInt(costOwed);
                if (Wallet.Instance.TrySpend(whole, displayName + " running cost")) costOwed -= whole;
                else
                {
                    SetPowered(false);
                    GameEvents.Say($"The {displayName} switched off: not enough credits to run it.", 6f);
                }
            }
        }

        /// <summary>Does one frame of work. Returns true if there was anything to do.</summary>
        protected abstract bool Process(float dt);
    }
}
