using System.Collections.Generic;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// Pooled hit and break effects: sparks, dust bursts and pieces that pop off at each damage stage.
    /// Debris is purely visual and never collides, so it costs almost nothing.
    /// </summary>
    public class DestructionFx : MonoBehaviour
    {
        public static DestructionFx Instance { get; private set; }

        [SerializeField] ParticleSystem sparks;
        [SerializeField] ParticleSystem dust;
        [SerializeField] Material debrisMaterial;
        [SerializeField] int debrisPool = 40;

        class Chunk
        {
            public Transform t;
            public Vector3 v, spin;
            public float life;
        }

        readonly List<Chunk> chunks = new List<Chunk>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        void Awake()
        {
            Instance = this;
            block = new MaterialPropertyBlock();
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int i = 0; i < debrisPool; i++)
            {
                var go = new GameObject("Debris " + i);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = debrisMaterial;
                go.SetActive(false);
                chunks.Add(new Chunk { t = go.transform });
            }
        }

        public void Sparks(Vector3 point, Vector3 normal, int count)
        {
            if (sparks == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 v = (normal + Random.insideUnitSphere * 0.9f).normalized * Random.Range(2f, 5f);
                sparks.Emit(new ParticleSystem.EmitParams { position = point, velocity = v, applyShapeToPosition = false }, 1);
            }
        }

        public void Debris(Vector3 from, Color colour, Vector3 hitPoint)
        {
            var c = chunks.Find(x => !x.t.gameObject.activeSelf);
            if (c == null) return;
            c.t.position = from;
            c.t.localScale = Vector3.one * Random.Range(0.12f, 0.3f);
            Vector3 away = (from - hitPoint);
            away.y = 0f;
            c.v = (away.normalized + Vector3.up * 1.4f).normalized * Random.Range(2.5f, 4.5f);
            c.spin = Random.insideUnitSphere * 400f;
            c.life = 1.4f;
            var r = c.t.GetComponent<MeshRenderer>();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, colour);
            r.SetPropertyBlock(block);
            c.t.gameObject.SetActive(true);
            Puff(from, 6);
        }

        public void BreakBurst(Vector3 center, Color colour)
        {
            for (int i = 0; i < 6; i++) Debris(center + Random.insideUnitSphere * 0.6f, colour, center + Random.insideUnitSphere);
            Puff(center, 24);
            Sparks(center, Vector3.up, 20);
        }

        public void Puff(Vector3 at, int count)
        {
            if (dust == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 v = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.3f, 1.4f), Random.Range(-1.2f, 1.2f));
                dust.Emit(new ParticleSystem.EmitParams { position = at + Random.insideUnitSphere * 0.4f, velocity = v, applyShapeToPosition = false }, 1);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var c in chunks)
            {
                if (!c.t.gameObject.activeSelf) continue;
                c.life -= dt;
                c.v.y -= 4.5f * dt;
                c.t.position += c.v * dt;
                c.t.Rotate(c.spin * dt, Space.World);
                if (c.t.position.y < 0.05f)
                {
                    c.t.position = new Vector3(c.t.position.x, 0.05f, c.t.position.z);
                    c.v *= 0.3f;
                }
                if (c.life < 0.4f) c.t.localScale *= 1f - dt * 6f;
                if (c.life <= 0f) c.t.gameObject.SetActive(false);
            }
        }
    }
}
