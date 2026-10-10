using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Muebles que el jefe destroza al embestir o caer encima (2026-10-08): cajas, bidones, sillas, mesas, archivadores, taquillas...
    /// Solo los que cuelgan de un grupo "Props" o "*_Props" (las paredes, columnas, estanterias de obra y puertas no cuentan).
    /// El mueble desaparece y deja trozos con su propio material que saltan, caen y se quedan en el suelo como restos.
    /// Despues se recalcula el NavMesh (en segundo plano) para que se pueda pasar por donde estaba.
    /// </summary>
    public static class PropBreaker
    {
        const int MaxDebris = 160;                                   // trozos sueltos en la escena como mucho (los mas viejos se van)
        static readonly Queue<GameObject> debris = new Queue<GameObject>();
        static float navUpdateAt = -1f;

        /// <summary>Raiz del mueble al que pertenece el collider (hijo directo de un grupo de props), o null si no se puede romper.</summary>
        public static Transform PropRoot(Collider c)
        {
            if (c == null || c.isTrigger || c is CharacterController) return null;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return null;           // objetos sueltos y restos
            if (c.GetComponentInParent<ZombieAI>() != null || c.GetComponentInParent<Pickup>() != null || c.GetComponentInParent<Door>() != null) return null;
            var t = c.transform;
            while (t.parent != null)
            {
                string pn = t.parent.name;
                if (pn == "Props" || pn == "Mobiliario" || pn.EndsWith("_Props"))      // "Mobiliario": el mobiliario de la comisaria grande (Props/Mobiliario)
                {
                    if (t.GetComponentInChildren<IInteractable>() != null) return null;                // taquillas con codigo, baul, terminal...
                    var b = Bounds(t);
                    if (b.size.x > 3.2f || b.size.z > 3.2f || b.size.y > 2.9f) return null;           // nada grande (las estanterias metalicas miden 2,64 m)
                    return t;
                }
                t = t.parent;
            }
            return null;
        }

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.5f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>
        /// Linea libre entre dos puntos para el jefe: solo la cortan el nivel y las estanterias de obra; los muebles rompibles, los
        /// zombis, el jugador y los objetos sueltos no cuentan (los arrasa o salta por encima).
        /// </summary>
        public static bool ClearForBoss(Vector3 from, Vector3 to, Transform self, float jumpOver = 0f)
        {
            Vector3 d = to - from;
            if (d.sqrMagnitude < 0.0001f) return true;
            foreach (var h in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (self != null && h.collider.transform.IsChildOf(self)) continue;
                if (h.rigidbody != null || h.collider is CharacterController || h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                if (PropRoot(h.collider) != null) continue;
                if (jumpOver > 0f && h.collider.bounds.max.y < Mathf.Min(from.y, to.y) - 1.6f + jumpOver) continue;   // lo bajo lo salta (estanterias)
                return false;
            }
            return true;
        }

        /// <summary>Rompe todos los muebles que toquen la esfera. Devuelve cuantos.</summary>
        public static int BreakInSphere(Vector3 center, float radius, Vector3 from)
        {
            var done = new HashSet<Transform>();
            foreach (var c in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var root = PropRoot(c);
                if (root == null || done.Contains(root)) continue;
                done.Add(root);
                Break(root, from);
            }
            return done.Count;
        }

        /// <summary>Hace pedazos un mueble: lo quita y lanza trozos con su material desde 'from' hacia fuera.</summary>
        public static void Break(Transform prop, Vector3 from)
        {
            if (prop == null) return;
            var b = Bounds(prop);
            var r0 = prop.GetComponentInChildren<Renderer>();
            Material mat = r0 != null ? r0.sharedMaterial : null;
            float vol = b.size.x * b.size.y * b.size.z;
            int n = Mathf.Clamp(Mathf.RoundToInt(vol * 14f), 6, 16);
            Vector3 push = b.center - from; push.y = 0f;
            push = push.sqrMagnitude > 0.01f ? push.normalized : Random.insideUnitSphere;
            for (int i = 0; i < n; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Restos";
                // tablas y astillas: alargados, de tamano proporcional al mueble
                float s = Mathf.Clamp(Mathf.Pow(vol, 1f / 3f) * Random.Range(0.18f, 0.38f), 0.06f, 0.45f);
                g.transform.localScale = new Vector3(s * Random.Range(0.6f, 2.2f), s * Random.Range(0.15f, 0.5f), s * Random.Range(0.5f, 1.4f));
                g.transform.SetPositionAndRotation(
                    b.center + Vector3.Scale(Random.insideUnitSphere, b.extents * 0.8f), Random.rotation);
                var rend = g.GetComponent<Renderer>();
                if (mat != null) rend.sharedMaterial = mat;
                var rb = g.AddComponent<Rigidbody>();
                rb.mass = 1.5f;
                rb.linearVelocity = push * Random.Range(2.5f, 6f) + Vector3.up * Random.Range(1.5f, 4f) + Random.insideUnitSphere * 1.5f;
                rb.angularVelocity = Random.insideUnitSphere * 12f;
                g.AddComponent<DebrisSettle>();
                debris.Enqueue(g);
                while (debris.Count > MaxDebris) { var old = debris.Dequeue(); if (old != null) Object.Destroy(old); }
            }
            GameAudio.Play(Sfx.BossStep, b.center, 1f, 1.35f, false);
            Object.Destroy(prop.gameObject);
            RequestNavUpdate();
        }

        static void RequestNavUpdate()
        {
            bool first = navUpdateAt < Time.time;
            navUpdateAt = Time.time + 2f;                     // se espera a que los restos se asienten
            if (first)
            {
                var nav = Object.FindFirstObjectByType<RuntimeNavMesh>();
                if (nav != null) nav.StartCoroutine(NavUpdate(nav));
            }
        }

        static IEnumerator NavUpdate(RuntimeNavMesh nav)
        {
            while (Time.time < navUpdateAt) yield return null;
            nav.Refresh();
        }
    }

    /// <summary>Resto de un mueble roto: cuando deja de moverse se queda quieto en el suelo (sin fisica) y no estorba al jugador.</summary>
    public class DebrisSettle : MonoBehaviour
    {
        float born;
        Rigidbody rb;

        void Start() { born = Time.time; rb = GetComponent<Rigidbody>(); }

        void Update()
        {
            if (rb == null) { enabled = false; return; }
            if (Time.time - born < 1.2f) return;
            if (rb.linearVelocity.sqrMagnitude < 0.02f || Time.time - born > 6f)
            {
                Destroy(rb);
                var c = GetComponent<Collider>();
                if (c != null) c.enabled = false;            // restos decorativos: no frenan al jugador ni a los zombis ni entran en el NavMesh
                enabled = false;
            }
        }
    }
}
