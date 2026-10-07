using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Casquillo o cartucho vacio que sale despedido del arma: cae con fisica, rebota en el suelo, hace "clin" al primer golpe
    /// y se encoge y desaparece al cabo de unos segundos. Hay un tope de casquillos a la vez (se borra el mas antiguo).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EjectedCasing : MonoBehaviour
    {
        [Tooltip("Sonido al golpear el suelo (vacio = ninguno)")] public AudioClip clinkSound;
        public float lifetime = 9f;
        public float shrinkTime = 1.2f;
        public const int MaxAlive = 40;

        static readonly List<EjectedCasing> alive = new List<EjectedCasing>();
        float born;
        bool clinked;
        Vector3 baseScale;

        void OnEnable()
        {
            alive.Add(this);
            born = Time.time;
            baseScale = transform.localScale;
            while (alive.Count > MaxAlive) { var old = alive[0]; alive.RemoveAt(0); if (old != null && old != this) Destroy(old.gameObject); }
        }

        void OnDisable() => alive.Remove(this);

        void Update()
        {
            float age = Time.time - born;
            if (age >= lifetime) { Destroy(gameObject); return; }
            float left = lifetime - age;
            if (left < shrinkTime) transform.localScale = baseScale * Mathf.Clamp01(left / shrinkTime);
        }

        void OnCollisionEnter(Collision c)
        {
            if (clinked || clinkSound == null || c.relativeVelocity.magnitude < 0.8f) return;
            clinked = true;
            GameAudio.PlayClip(clinkSound, transform.position, 0.35f, Random.Range(0.9f, 1.15f), true);
        }
    }
}
