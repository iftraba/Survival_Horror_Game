using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>Busca el interactuable mas cercano delante del jugador.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public float radius = 2.2f;
        public float maxAngle = 75f;

        public static IInteractable Current { get; private set; }

        void OnDisable() => Current = null;

        void Update()
        {
            if (GameState.InputBlocked)
            {
                Current = null;
                return;
            }

            IInteractable best = null;
            float bestScore = float.MaxValue;
            var hits = Physics.OverlapSphere(transform.position + Vector3.up, radius, ~0, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                var it = h.GetComponentInParent<IInteractable>();
                if (it == null || string.IsNullOrEmpty(it.Prompt)) continue;   // sin accion disponible (taquilla ya abierta...)
                Vector3 to = h.bounds.center - (transform.position + Vector3.up);
                float angle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(to, Vector3.up));
                if (angle > maxAngle) continue;
                float score = to.magnitude + angle * 0.02f;
                if (score >= bestScore) continue;
                if (!Visible(h, it)) continue;   // nada a traves de puertas, paredes o taquillas cerradas
                bestScore = score; best = it;
            }
            Current = best;

            if (Current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Current.Interact(gameObject);
        }

        /// <summary>Linea de vision desde el pecho del jugador hasta el objeto; solo cuenta lo que no es el propio objeto.</summary>
        bool Visible(Collider target, IInteractable it)
        {
            var owner = (it as Component)?.transform;
            Vector3 from = transform.position + Vector3.up * 0.6f;
            Vector3 d = target.bounds.center - from;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var t = hit.collider.transform;
                if (t.IsChildOf(transform)) continue;                                   // el propio jugador
                if (owner != null && t.IsChildOf(owner)) continue;                      // el objeto en si
                if (hit.rigidbody != null) continue;                                    // otros objetos sueltos
                if (hit.collider.GetComponentInParent<ZombieAI>() != null) continue;
                return false;
            }
            return true;
        }
    }
}
