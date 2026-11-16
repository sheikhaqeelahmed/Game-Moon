using PlanetScrappers.Economy;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// A pooled chunk of scrap carrying some units of one resource. It pops out in a low-gravity
    /// arc, rests on the ground, then flies to the nearest scrap magnet in range: the player's
    /// backpack, the truck bin while driving, or a scrap collector.
    /// </summary>
    public class ScrapPiece : MonoBehaviour
    {
        const float Gravity = 4.5f;
        const float CollectRadius = 0.7f;

        enum State { Flying, Resting, Magnet }

        ResourceDefinition resource;
        int amount;
        Vector3 velocity;
        Vector3 spin;
        State state;
        float restTimer;
        float searchTimer;
        ScrapSink target;
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
            target = null;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;

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
                    searchTimer -= dt;
                    if (restTimer > 0f || searchTimer > 0f) break;
                    searchTimer = 0.15f; // cheap: look for a magnet a few times a second
                    target = ScrapSink.Nearest(transform.position);
                    if (target != null)
                    {
                        state = State.Magnet;
                        velocity = Vector3.up * 2f;
                    }
                    break;

                case State.Magnet:
                    if (target == null || !target.CanAccept) { state = State.Resting; target = null; break; }
                    Vector3 to = target.AttractPoint - transform.position;
                    velocity = Vector3.Lerp(velocity, to.normalized * 16f, 1f - Mathf.Exp(-8f * dt));
                    transform.position += velocity * dt;
                    transform.localScale = Vector3.Lerp(transform.localScale, owner.ScaleFor(resource) * 0.6f, dt * 4f);
                    if (to.magnitude < CollectRadius) Collect();
                    break;
            }
        }

        void Collect()
        {
            int added = target.Accept(resource, amount);
            amount -= added;
            if (added > 0) owner.OnCollected();
            if (amount <= 0) owner.Release(this);
            else
            {
                state = State.Resting;
                restTimer = 1f;
                target = null;
            }
        }
    }
}
