using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Rastro toxico del segundo jefe: mientras anda va dejando charcos que hacen dano al jugador si los pisa y se
    /// secan solos. Sin fisica: la distancia al jugador se comprueba aqui, asi no estorban al NavMesh ni a las balas.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ToxicTrail : MonoBehaviour
    {
        public Material puddleMaterial;
        [Tooltip("Cada cuantos metros recorridos deja un charco")] public float spacing = 1.4f;
        public float radius = 0.85f;
        [Tooltip("Segundos que dura un charco")] public float lifetime = 9f;
        [Tooltip("Dano por golpe y segundos entre golpes mientras se esta dentro")] public float damage = 6f;
        public float tickTime = 0.5f;

        struct Puddle { public Transform t; public float born; }
        readonly List<Puddle> puddles = new List<Puddle>();
        ZombieAI ai;
        Transform player;
        Health playerHealth;
        Vector3 lastDrop;
        float nextTick;
        bool started;

        void Awake() => ai = GetComponent<ZombieAI>();

        void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) { player = pc.transform; playerHealth = pc.GetComponent<Health>(); }
            lastDrop = transform.position;
        }

        void Update()
        {
            bool alive = ai != null && !ai.IsDormant && ai.Hp != null && !ai.Hp.IsDead;
            if (alive)
            {
                // el primer charco no sale hasta que empieza a moverse (no se deja uno al despertar)
                Vector3 d = transform.position - lastDrop; d.y = 0f;
                if (d.magnitude >= spacing) { if (started) Drop(); lastDrop = transform.position; started = true; }
            }

            for (int i = puddles.Count - 1; i >= 0; i--)
            {
                float age = Time.time - puddles[i].born;
                if (age >= lifetime) { Destroy(puddles[i].t.gameObject); puddles.RemoveAt(i); continue; }
                // se encoge al final de su vida
                float k = Mathf.Clamp01((lifetime - age) / 2f);
                puddles[i].t.localScale = new Vector3(radius * 2f * k, 0.01f, radius * 2f * k);
            }

            if (player == null || playerHealth == null || playerHealth.IsDead || Time.time < nextTick) return;
            foreach (var p in puddles)
            {
                Vector3 d = player.position - p.t.position; d.y = 0f;
                if (d.magnitude > radius * 0.9f || Mathf.Abs(player.position.y - p.t.position.y) > 1.2f) continue;
                playerHealth.TakeDamage(damage, p.t.position);
                Hud.Message("Ácido");
                nextTick = Time.time + tickTime;
                break;
            }
        }

        void Drop() => AddPuddle(transform.position);

        /// <summary>Deja un charco en el suelo bajo 'at' (lo usan el rastro y el escupitajo del jefe).</summary>
        public void AddPuddle(Vector3 at)
        {
            Vector3 pos = at + Vector3.up * 0.5f;
            float y = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(pos, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController || h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                y = Mathf.Max(y, h.point.y);
            }
            if (float.IsNegativeInfinity(y)) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ToxicPuddle";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(at.x, y + 0.02f, at.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            if (puddleMaterial != null) go.GetComponent<Renderer>().sharedMaterial = puddleMaterial;
            puddles.Add(new Puddle { t = go.transform, born = Time.time });
        }

        void OnDestroy()
        {
            foreach (var p in puddles) if (p.t != null) Destroy(p.t.gameObject);
        }
    }
}
