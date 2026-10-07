using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Puerta abatible (va en la bisagra). Puede requerir una llave.
    /// - Se abre hacia el lado contrario a quien la abre, y su hoja no colisiona mientras gira: no empuja al jugador.
    /// - Cerrada bloquea el NavMesh (los zombis no la cruzan). Si no tiene llave, los zombis que te persiguen la
    ///   golpean y, tras unos segundos, la abren a la fuerza. Las puertas con llave los detienen siempre.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        public ItemData requiredKey;
        public bool consumeKey = true;
        [Tooltip("Segunda hoja de una puerta doble: se abre, cierra y desbloquea a la vez")]
        public Door partner;
        public float openAngle = 100f;
        public float openTime = 0.8f;
        [Tooltip("Segundos que tarda un zombi en forzar la puerta (si no tiene llave)")]
        public float zombieForceTime = 4f;
        [Tooltip("Las puertas de las salas seguras no se pueden forzar")]
        public bool zombiesCanForce = true;
        [Tooltip("Distancia a la hoja a la que un zombi empieza a golpearla")]
        public float zombieReach = 1.5f;

        bool open, moving;
        Quaternion closedRot;
        float openSign = 1f;
        // Mientras la puerta esta cerrada bloquea el NavMesh; al abrirla los enemigos pueden pasar
        NavMeshObstacle obstacle;
        Collider[] leafColliders;
        float bashTime, nextBang;

        [Tooltip("Objetivo que se muestra cuando se abre la puerta por primera vez")]
        public string openedObjective;

        public string Prompt => moving ? "" : Sealed ? "Atrancada" : open ? "E  Cerrar puerta" : requiredKey != null ? "E  Abrir puerta (cerrada con llave)" : "E  Abrir puerta";
        public bool IsOpen => open;
        /// <summary>Atrancada: se cierra de golpe y no se puede abrir (el combate con el jefe encierra al jugador). Unseal() la libera.</summary>
        public bool Sealed { get; private set; }
        bool prevForce;
        public void Seal() { if (Sealed) return; Sealed = true; prevForce = zombiesCanForce; zombiesCanForce = false; if (partner != null) partner.Seal(); }
        public void Unseal() { if (!Sealed) return; Sealed = false; zombiesCanForce = prevForce; if (partner != null) partner.Unseal(); }
        public bool IsUnlocked => requiredKey == null;

        /// <summary>Restaura el estado al cargar una partida, sin animacion ni sonido.</summary>
        public void ApplySaved(bool unlocked, bool isOpen)
        {
            if (unlocked) requiredKey = null;
            if (isOpen) openSign = 1f;
            open = isOpen;
            moving = false;
            transform.localRotation = isOpen ? OpenRot() : closedRot;
            if (obstacle != null) obstacle.enabled = !isOpen;
        }

        void Awake()
        {
            closedRot = transform.localRotation;
            obstacle = GetComponentInChildren<NavMeshObstacle>();
            leafColliders = GetComponentsInChildren<Collider>();
        }

        Quaternion OpenRot() => closedRot * Quaternion.Euler(0f, openAngle * openSign, 0f);

        /// <summary>Elige el sentido de giro para que la hoja se aleje de 'from'.</summary>
        void SwingAwayFrom(Vector3 from)
        {
            // normal de la hoja cerrada (eje Z local del padre de la bisagra) y lado en el que esta quien abre
            Vector3 normal = (transform.parent != null ? transform.parent.rotation : Quaternion.identity) * closedRot * Vector3.forward;
            Vector3 center = LeafCenter();
            float side = Vector3.Dot(from - center, normal);
            // girar +Y desplaza la hoja hacia -normal (con la hoja extendida hacia +X local)
            openSign = side >= 0f ? 1f : -1f;
        }

        Vector3 LeafCenter()
        {
            var r = GetComponentInChildren<Renderer>();
            return r != null ? r.bounds.center : transform.position;
        }

        public void Interact(GameObject who)
        {
            if (moving) return;
            if (Sealed)
            {
                Hud.Message("La puerta se ha atrancado");
                GameAudio.Play(Sfx.DoorLocked, transform.position);
                return;
            }
            if (!open && requiredKey != null)
            {
                var inv = who.GetComponent<Inventory>();
                if (inv == null || !inv.Has(requiredKey))
                {
                    Hud.Message($"Cerrada. Necesitas: {requiredKey.displayName}");
                    GameAudio.Play(Sfx.DoorLocked, transform.position);
                    return;
                }
                // la llave se gasta aunque solo una de las dos hojas de una puerta doble tenga consumeKey (si no, abrirla por el otro lado la dejaba en el inventario)
                if (consumeKey || (partner != null && partner.consumeKey)) inv.Remove(requiredKey);
                requiredKey = null;
                if (partner != null) partner.requiredKey = null;
                Hud.Message("Desbloqueaste la puerta");
                GameAudio.Play(Sfx.DoorUnlock, transform.position);
            }
            Toggle(who.transform.position);
            if (partner != null && !partner.moving && partner.open != open) partner.Toggle(who.transform.position);
        }

        void Toggle(Vector3 from)
        {
            if (!open)
            {
                SwingAwayFrom(from);
                GameAudio.Play(Sfx.DoorOpen, transform.position);
                if (!string.IsNullOrEmpty(openedObjective))
                {
                    Objectives.Set(openedObjective);
                    openedObjective = null;   // solo la primera vez
                }
                if (obstacle != null) obstacle.enabled = false;   // se abre: el paso queda libre
            }
            StartCoroutine(Swing(open ? closedRot : OpenRot()));
            open = !open;
            bashTime = 0f;
        }

        void Update()
        {
            // atrancada: si estaba abierta, se cierra de golpe
            if (Sealed && open && !moving) { open = false; bashTime = 0f; StartCoroutine(Swing(closedRot)); return; }
            if (open || moving || requiredKey != null || !zombiesCanForce) { bashTime = 0f; return; }
            // Zombis que te persiguen y estan pegados a la puerta cerrada: la golpean hasta abrirla
            ZombieAI basher = null;
            Vector3 c = LeafCenter();
            foreach (var z in ZombieAI.All)
            {
                if (z == null || !z.IsChasing) continue;
                Vector3 d = z.transform.position - c; d.y = 0f;
                if (d.magnitude <= zombieReach) { basher = z; break; }
            }
            if (basher == null) { bashTime = Mathf.Max(0f, bashTime - Time.deltaTime); return; }

            bashTime += Time.deltaTime;
            if (Time.time >= nextBang)
            {
                nextBang = Time.time + Random.Range(0.9f, 1.4f);
                basher.BashDoor();
                GameAudio.Play(Sfx.DoorLocked, c, 1f, Random.Range(0.55f, 0.7f));
                StartCoroutine(Shake());
            }
            if (bashTime >= zombieForceTime)
            {
                Hud.Message("Han forzado una puerta");
                Toggle(basher.transform.position);
                if (partner != null && !partner.open && !partner.moving) partner.Toggle(basher.transform.position);
            }
        }

        IEnumerator Shake()
        {
            if (moving) yield break;
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                transform.localRotation = closedRot * Quaternion.Euler(0f, Mathf.Sin(t * 90f) * 2.5f, 0f);
                yield return null;
            }
            if (!open && !moving) transform.localRotation = closedRot;
        }

        IEnumerator Swing(Quaternion to)
        {
            moving = true;
            foreach (var c in leafColliders) c.enabled = false;   // la hoja no empuja a nadie mientras gira
            var from = transform.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openTime)
            {
                transform.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localRotation = to;
            foreach (var c in leafColliders) c.enabled = true;
            moving = false;
            if (!open && obstacle != null) obstacle.enabled = true;     // cerrada del todo: vuelve a bloquear
            if (!open) GameAudio.Play(Sfx.DoorClose, transform.position);
        }
    }
}
