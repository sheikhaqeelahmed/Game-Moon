using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// A pooled chunk of scrap carrying some units of one resource. It pops out in a low-gravity
    /// arc, rests on the ground, then flies into the backpack when the player comes close.
    /// </summary>
    public class ScrapPiece : MonoBehaviour
    {
        const float Gravity = 4.5f;
        const float MagnetRadius = 5.5f;
        const float CollectRadius = 0.6f;

        enum State { Flying, Resting, Magnet }

        ResourceDefinition resource;
        int amount;
        Vector3 velocity;
        Vector3 spin;
        State state;
        float restTimer;
        ScrapSpawner owner;

        public bool Active => gameObject.activeSelf;

        public void Launch(ScrapSpawner from, ResourceDefinition res, int units, Vector3 position, Vector3 initialVelocity)
        {
            owner = from;
            resource = res;
            amount = units;
            transform.position = position;
            velocity = initialVelocity;
            spin = Random.insideUnitSphere * 360f;
            state = State.Flying;
            restTimer = 0.35f;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var inv = PlayerInventory.Instance;
            Vector3 chest = inv != null ? inv.transform.position + Vector3.up * 1.1f : Vector3.zero;

            switch (state)
            {
                case State.Flying:
                    velocity.y -= Gravity * dt;
                    transform.position += velocity * dt;
                    transform.Rotate(spin * dt, Space.World);
                    if (transform.position.y <= 0.09f)
                    {
                        var p = transform.position;
                        transform.position = new Vector3(p.x, 0.09f, p.z);
                        if (velocity.y < -1.2f) velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f);
                        else state = State.Resting;
                    }
                    break;

                case State.Resting:
                    restTimer -= dt;
                    if (inv != null && restTimer <= 0f && !inv.IsFull && (chest - transform.position).sqrMagnitude < MagnetRadius * MagnetRadius)
                    {
                        state = State.Magnet;
                        velocity = Vector3.up * 2f;
                    }
                    break;

                case State.Magnet:
                    if (inv == null || inv.IsFull) { state = State.Resting; break; }
                    Vector3 to = chest - transform.position;
                    velocity = Vector3.Lerp(velocity, to.normalized * 14f, 1f - Mathf.Exp(-8f * dt));
                    transform.position += velocity * dt;
                    transform.localScale = Vector3.Lerp(transform.localScale, owner.ScaleFor(resource) * 0.6f, dt * 4f);
                    if (to.magnitude < CollectRadius) Collect(inv);
                    break;
            }
        }

        void Collect(PlayerInventory inv)
        {
            int added = inv.Add(resource, amount);
            amount -= added;
            if (added > 0) owner.OnCollected();
            if (amount <= 0) owner.Release(this);
            else
            {
                state = State.Resting;
                restTimer = 1f;
            }
        }
    }
}
