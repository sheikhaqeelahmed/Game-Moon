using System.Linq;
using PlanetScrappers.Destruction;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// A hovering salvage drone. It flies to the nearest object it can handle (tier 3 or lower),
    /// takes it apart, vacuums up the scrap into its own small hopper, and flies back to the
    /// warehouse to unload when full or when nothing is left nearby. Flies over obstacles,
    /// so it needs no pathfinding.
    /// </summary>
    public class SalvageRobot : MonoBehaviour
    {
        enum State { Seek, Fly, Work, Collect, Return, Unload }

        [SerializeField] float speed = 7f;
        [SerializeField] float cruiseHeight = 6f;
        [SerializeField] int maxTier = 3;

        RobotDock dock;
        ScrapStorage hopper;
        StorageScrapSink magnet;
        ToolDefinition tool;
        DestructibleObject target;
        State state;
        float timer;
        Transform rotor;

        public int Hits { get; private set; }
        public int Delivered { get; private set; }
        public string StateName => state.ToString();

        public void Init(RobotDock owner, ScrapStorage ownHopper, StorageScrapSink ownMagnet, Transform rotorVisual)
        {
            dock = owner;
            hopper = ownHopper;
            magnet = ownMagnet;
            rotor = rotorVisual;
            tool = ScriptableObject.CreateInstance<ToolDefinition>();
            tool.id = "robot_cutter";
            tool.displayName = "Robot Cutter";
            tool.tier = maxTier;
            tool.damage = 18f;
            tool.hitsPerSecond = 1.5f;
            magnet.Active = false;
            state = State.Seek;
        }

        void Update()
        {
            if (rotor != null) rotor.Rotate(Vector3.up, 900f * Time.deltaTime, Space.Self);
            float dt = Time.deltaTime;
            timer -= dt;

            switch (state)
            {
                case State.Seek:
                    if (timer > 0f) { Hover(dock.transform.position + Vector3.up * cruiseHeight, dt); break; }
                    target = FindTarget();
                    if (target != null) state = State.Fly;
                    else { timer = 3f; if (hopper.TotalUnits > 0) state = State.Return; }
                    break;

                case State.Fly:
                    if (target == null || target.IsDismantled) { state = State.Seek; break; }
                    Vector3 above = target.Center + Vector3.up * 3f;
                    if (Hover(above, dt)) { state = State.Work; timer = 0f; }
                    break;

                case State.Work:
                    if (target == null) { state = State.Seek; break; }
                    if (target.IsDismantled) { state = State.Collect; timer = 3.5f; magnet.Active = true; break; }
                    Hover(target.Center + Vector3.up * 3f, dt);
                    if (timer <= 0f)
                    {
                        timer = 1f / tool.hitsPerSecond;
                        var result = target.Hit(tool, target.Center + Vector3.up * 0.5f, Vector3.up);
                        if (result == HitResult.TooWeak) { target = null; state = State.Seek; }
                        else Hits++;
                    }
                    break;

                case State.Collect:
                    Hover(transform.position, dt);
                    if (timer <= 0f || hopper.IsFull)
                    {
                        magnet.Active = false;
                        state = hopper.TotalUnits >= hopper.Capacity * 0.6f ? State.Return : State.Seek;
                    }
                    break;

                case State.Return:
                    if (Hover(dock.DropPoint + Vector3.up * 2f, dt)) { state = State.Unload; timer = 1f; }
                    break;

                case State.Unload:
                    if (timer <= 0f)
                    {
                        int before = hopper.TotalUnits;
                        foreach (var kv in hopper.TakeAll()) dock.Warehouse.Add(kv.Key, kv.Value);
                        Delivered += before;
                        state = State.Seek;
                        timer = 1f;
                    }
                    break;
            }
        }

        DestructibleObject FindTarget()
        {
            Vector3 p = transform.position;
            return DestructibleObject.All
                .Where(o => !o.IsDismantled && o.Definition.tier <= maxTier && dock.InWorkArea(o.Center) && !dock.IsClaimed(o, this))
                .OrderBy(o => (o.Center - p).sqrMagnitude)
                .FirstOrDefault();
        }

        public DestructibleObject Target => target;

        /// <summary>Flies toward a point, staying at cruise height until close. Returns true on arrival.</summary>
        bool Hover(Vector3 goal, float dt)
        {
            Vector3 flat = goal - transform.position;
            flat.y = 0f;
            float targetY = flat.magnitude > 4f ? Mathf.Max(goal.y, cruiseHeight) : goal.y;
            Vector3 next = Vector3.MoveTowards(transform.position, new Vector3(goal.x, targetY, goal.z), speed * dt);
            next.y = Mathf.MoveTowards(transform.position.y, targetY, speed * 0.6f * dt);
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 1f - Mathf.Exp(-5f * dt));
            transform.position = next;
            return (transform.position - goal).sqrMagnitude < 0.6f;
        }
    }
}
