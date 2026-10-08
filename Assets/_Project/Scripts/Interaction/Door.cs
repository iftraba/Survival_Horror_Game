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

        float usedAngle = -1f;       // angulo de apertura real (limitado si hay una pared al lado)
        Quaternion OpenRot() => closedRot * Quaternion.Euler(0f, (usedAngle > 0f ? usedAngle : openAngle) * openSign, 0f);

        /// <summary>Elige el sentido de giro para que la hoja se aleje de 'from'.</summary>
        void SwingAwayFrom(Vector3 from)
        {
            // normal de la hoja cerrada (eje Z local del padre de la bisagra) y lado en el que esta quien abre
            Vector3 normal = (transform.parent != null ? transform.parent.rotation : Quaternion.identity) * closedRot * Vector3.forward;
            Vector3 center = LeafCenter();
            float side = Vector3.Dot(from - center, normal);
            // girar +Y desplaza la hoja hacia -normal (con la hoja extendida hacia +X local)
            float preferred = side >= 0f ? 1f : -1f;
            // se mira cuanto puede abrirse hacia cada lado sin meter la hoja en una pared o un mueble: se elige el lado
            // preferido si se abre del todo; si no, el que deje mas hueco (y se limita el angulo) para no quedar incrustada
            float freePref = FreeAngle(preferred), freeOther = FreeAngle(-preferred);
            float cap = Mathf.Min(openAngle, 90f);      // a mas de 90 grados la hoja se inclina hacia el muro de su lado y se mete en el (los muros gruesos, 11 cm)
            if (freePref >= cap - 1f || freePref >= freeOther) { openSign = preferred; usedAngle = Mathf.Min(cap, freePref); }
            else { openSign = -preferred; usedAngle = Mathf.Min(cap, freeOther); }
            usedAngle = Mathf.Max(usedAngle, 20f);
        }

        /// <summary>Angulo (0-openAngle) hasta el que la hoja puede girar hacia 'sign' sin chocar con el nivel (paredes, muebles).</summary>
        float FreeAngle(float sign)
        {
            var boxes = new System.Collections.Generic.List<BoxCollider>();
            foreach (var c in leafColliders) if (c is BoxCollider b) boxes.Add(b);
            if (boxes.Count == 0) return openAngle;
            Matrix4x4 hingeWorld = transform.localToWorldMatrix;
            var toHinge = new Matrix4x4[boxes.Count];
            for (int i = 0; i < boxes.Count; i++) toHinge[i] = transform.worldToLocalMatrix * boxes[i].transform.localToWorldMatrix;
            Matrix4x4 parent = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
            float free = 0f, maxA = Mathf.Min(openAngle, 90f);
            bool clear = true;
            for (float a = 8f; a <= maxA + 0.01f; a += 8f)
            {
                var rot = closedRot * Quaternion.Euler(0f, a * sign, 0f);
                Matrix4x4 test = parent * Matrix4x4.TRS(transform.localPosition, rot, transform.localScale);
                bool hit = false;
                for (int i = 0; i < boxes.Count && !hit; i++)
                {
                    var b = boxes[i];
                    Matrix4x4 w = test * toHinge[i];
                    Vector3 center = w.MultiplyPoint3x4(b.center);
                    Vector3 half = Vector3.Scale(b.size, new Vector3(w.GetColumn(0).magnitude, w.GetColumn(1).magnitude, w.GetColumn(2).magnitude)) * 0.5f;
                    // el volumen de prueba se encoge (grosor al 60 %, ancho al 88 %): si no, la esquina de la bisagra roza el extremo de su propio muro
                    int thin = half.x <= half.y && half.x <= half.z ? 0 : half.y <= half.z ? 1 : 2;
                    int wide = thin == 0 ? (half.y <= half.z ? 1 : 2) : thin == 1 ? (half.x <= half.z ? 0 : 2) : (half.x <= half.y ? 0 : 1);
                    half[thin] *= 0.6f; half[wide] *= 0.88f;
                    foreach (var o in Physics.OverlapBox(center, half, w.rotation, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (o.transform.IsChildOf(transform) || o.transform.IsChildOf(transform.parent) && o.GetComponentInParent<Door>() != null) continue;   // ella misma y su marco/hoja gemela
                        if (o is CharacterController || o.attachedRigidbody != null) continue;                                                              // el jugador y objetos sueltos
                        if (o.GetComponentInParent<ZombieAI>() != null || o.GetComponentInParent<Pickup>() != null) continue;
                        if (o.GetComponentInParent<Door>() != null) continue;
                        hit = true; break;
                    }
                }
                if (hit) { clear = false; break; }
                free = a;
            }
            return clear ? maxA : free;
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
            Vector3 from = who.transform.position;
            System.Action swing = () =>
            {
                if (moving) return;
                Toggle(from);
                if (partner != null && !partner.moving && partner.open != open) partner.Toggle(from);
            };
            // el jugador abre con animacion (la hoja gira cuando la mano llega al pomo); en la sala de un jefe, cruza hasta dentro
            var actions = who.GetComponent<PlayerActions>();
            if (!open && actions != null)
            {
                var boss = BossBehind(from, out Vector3 inside);
                if (boss != null) actions.EnterBossRoom(this, inside, swing);
                else actions.OpenDoor(this, swing);
                return;
            }
            swing();
        }

        /// <summary>Centro de la hoja (para mirar hacia la puerta).</summary>
        public Vector3 Center => LeafCenter();
        /// <summary>Normal de la hoja cerrada (horizontal): hacia un lado de la puerta.</summary>
        public Vector3 Normal
        {
            get
            {
                Vector3 n = (transform.parent != null ? transform.parent.rotation : Quaternion.identity) * closedRot * Vector3.forward;
                n.y = 0f;
                return n.sqrMagnitude > 0.001f ? n.normalized : Vector3.forward;
            }
        }

        /// <summary>Disparador de la sala de un jefe todavia dormido que esta al otro lado de esta puerta (null si no hay).</summary>
        BossRoomTrigger BossBehind(Vector3 from, out Vector3 inside)
        {
            inside = default;
            Vector3 c = LeafCenter();
            foreach (var trig in FindObjectsByType<BossRoomTrigger>(FindObjectsSortMode.None))
            {
                if (trig.boss == null || !trig.boss.IsDormant || trig.sealDoors == null) continue;
                bool mine = false;
                foreach (var d in trig.sealDoors) if (d == this || (d != null && d == partner)) mine = true;
                if (!mine) continue;
                var col = trig.GetComponent<Collider>();
                Vector3 p = col != null ? col.bounds.center : trig.transform.position;
                Vector3 toIn = Vector3.ProjectOnPlane(p - c, Vector3.up), toMe = Vector3.ProjectOnPlane(from - c, Vector3.up);
                if (Vector3.Dot(toIn, toMe) >= 0f) continue;              // el jugador ya esta del lado de dentro
                inside = p;
                return trig;
            }
            return null;
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
            // atrancada: si estaba abierta, se cierra de golpe (si el jugador la esta cruzando con la animacion, al terminar)
            if (Sealed && open && !moving && !PlayerActions.Locked) { open = false; bashTime = 0f; StartCoroutine(Swing(closedRot)); return; }
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
