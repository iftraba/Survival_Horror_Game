using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Acciones con animacion propia que quitan el control un momento (2026-10-08):
    /// - Abrir una puerta: el personaje alarga la mano y la empuja (la hoja gira cuando la mano llega al pomo).
    /// - Entrar en la sala de un jefe: abre la puerta y cruza hasta dentro sin poder volverse (el combate empieza al pasar).
    /// - Giro de 180 grados corriendo (Q mientras corres sin apuntar), voltereta al caer de altura.
    /// - Agarre de un zombi (mordisco al cuello): hay que pulsar E muchas veces para soltarse; si no, muerde fuerte.
    /// </summary>
    public class PlayerActions : MonoBehaviour
    {
        public static PlayerActions Instance { get; private set; }
        /// <summary>Hay una accion en curso: sin moverse, apuntar, disparar ni interactuar.</summary>
        public static bool Locked => Instance != null && Instance.locked;
        public static bool Grabbed => Instance != null && Instance.grabber != null;

        [Header("Abrir puertas (clip Opening, la primera mitad)")]
        public float doorAnimSpeed = 1.8f;
        [Tooltip("Segundos hasta que la mano llega al pomo y la hoja empieza a girar")] public float doorSwingAt = 0.75f;
        [Tooltip("Segundos hasta devolver el control")] public float doorUnlockAt = 1.4f;
        [Tooltip("Velocidad al acercarse a la puerta antes de abrirla")] public float doorWalkSpeed = 1.8f;

        [Header("Entrar en la sala del jefe (clip Opening Door Inwards)")]
        public float enterSpeed = 1.25f;
        [Tooltip("Segundos del clip (a velocidad 1) en que empuja la puerta")] public float enterPushAt = 2.85f;

        [Header("Giro corriendo (clip Running To Turn) y voltereta (Falling To Roll)")]
        public float runTurnTime = 1.6f;
        public float rollTime = 1.3f;
        public float rollDistance = 1.4f;

        [Header("Coberturas (V)")]
        [Tooltip("Velocidad al moverse pegado a la pared (de pie / agachado)")] public float coverSpeed = 1.1f, coverCrouchSpeed = 0.9f;
        [Tooltip("Distancia del centro del jugador a la pared en cobertura")] public float coverGap = 0.42f;
        /// <summary>0 sin cobertura, 1 de pie (pared alta), 2 agachado (obstaculo bajo).</summary>
        public int CoverType { get; private set; }
        /// <summary>Direccion del desplazamiento en cobertura para la animacion: +1 hacia su izquierda, -1 hacia su derecha.</summary>
        public float CoverMove { get; private set; }

        [Header("Agarre")]
        public int mashNeeded = 10;
        public float grabTime = 4f;
        public float biteDamage = 4f;
        public float biteEvery = 0.8f;
        [Tooltip("Dano extra si no te sueltas a tiempo")] public float failDamage = 18f;
        [Tooltip("Segundos sin otro agarre despues de uno")] public float grabCooldown = 8f;

        bool locked;
        ZombieAI grabber;
        float nextGrab;
        CharacterController body;
        PlayerController player;
        PlayerAnimation anim;
        WeaponController weapons;
        Health health;

        // avance (fraccion del recorrido) del clip de entrar por la puerta, segun la cadera: coge el pomo, empuja (2,9 s) y cruza
        static readonly AnimationCurve EnterProfile = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(1f, 0.12f), new Keyframe(2f, 0.16f), new Keyframe(3.1f, 0.17f), new Keyframe(3.4f, 0.29f),
            new Keyframe(3.8f, 0.48f), new Keyframe(4.2f, 0.78f), new Keyframe(4.5f, 0.88f), new Keyframe(4.9f, 0.98f), new Keyframe(5.2f, 1f));

        void Awake()
        {
            Instance = this;
            body = GetComponent<CharacterController>();
            player = GetComponent<PlayerController>();
            anim = GetComponent<PlayerAnimation>();
            weapons = GetComponent<WeaponController>();
            health = GetComponent<Health>();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Trigger(string name) { if (anim != null) anim.Trigger(name); }

        void FaceTowards(Vector3 point, float t)
        {
            Vector3 d = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (d.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), t);
        }

        void MoveTo(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            body.Move(d + Vector3.down * 0.05f);
        }

        // ------------------------------------------------------------------ puertas
        /// <summary>Abre una puerta con la animacion; 'swing' gira la hoja cuando la mano llega al pomo.</summary>
        public void OpenDoor(Door door, System.Action swing)
        {
            if (locked) { swing(); return; }
            StartCoroutine(OpenDoorRoutine(door, swing));
        }

        IEnumerator OpenDoorRoutine(Door door, System.Action swing)
        {
            locked = true;
            // primero se acerca andando hasta quedar delante del pomo (a 0,6 m de la hoja, en su lado), y se gira hacia la puerta
            Vector3 c = door.Center;
            Vector3 handle = door.transform.position + (c - door.transform.position) * 1.4f;      // la hoja va de la bisagra al pomo
            Vector3 normal = door.Normal;
            if (Vector3.Dot(transform.position - c, normal) < 0f) normal = -normal;
            Vector3 stand = handle + normal * 0.6f; stand.y = transform.position.y;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                Vector3 to = stand - transform.position; to.y = 0f;
                if (to.magnitude < 0.06f) break;
                Vector3 step = Vector3.ClampMagnitude(to, doorWalkSpeed * Time.deltaTime);
                body.Move(step + Vector3.down * 0.05f);
                if (to.magnitude > 0.25f) FaceTowards(transform.position + to, 10f * Time.deltaTime);
                if (health != null && health.IsDead) break;
                yield return null;
            }
            for (float t = 0f; t < 0.2f; t += Time.deltaTime) { FaceTowards(handle - normal * 0.5f, 14f * Time.deltaTime); yield return null; }
            Trigger("OpenDoor");
            weapons?.SetHeldVisible(false);                  // la mano derecha va al pomo
            bool swung = false;
            for (float t = 0f; t < doorUnlockAt; t += Time.deltaTime)
            {
                FaceTowards(c, 10f * Time.deltaTime);
                if (!swung && t >= doorSwingAt) { swung = true; swing(); }
                if (health != null && health.IsDead) break;
                yield return null;
            }
            if (!swung) swing();
            weapons?.SetHeldVisible(true);
            locked = false;
        }

        /// <summary>
        /// Entra en la sala de un jefe: se coloca delante de la puerta, la abre y cruza hasta 'inside' (pasado el disparador del
        /// combate). No se puede asomar y volver: el combate empieza al cruzar y la puerta se atranca detras.
        /// </summary>
        public void EnterBossRoom(Door door, Vector3 inside, System.Action swing)
        {
            if (locked) return;
            StartCoroutine(EnterRoutine(door, inside, swing));
        }

        IEnumerator EnterRoutine(Door door, Vector3 inside, System.Action swing)
        {
            locked = true;
            weapons?.SetHeldVisible(false);
            Vector3 c = door.Center; c.y = transform.position.y;
            inside.y = transform.position.y;
            Vector3 inward = Vector3.ProjectOnPlane(inside - c, Vector3.up).normalized;
            Vector3 start = c - inward * 0.75f;
            Vector3 end = c + inward * (Vector3.Distance(c, inside) + 0.6f);
            // colocarse delante de la puerta, de cara a ella
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                MoveTo(Vector3.Lerp(transform.position, start, t / 0.3f));
                FaceTowards(transform.position + inward, t / 0.3f);
                yield return null;
            }
            transform.rotation = Quaternion.LookRotation(inward);
            if (player != null && player.cam != null) player.cam.SetYaw(Quaternion.LookRotation(inward).eulerAngles.y);
            Trigger("EnterDoor");
            float total = Vector3.Distance(start, end);
            float clipEnd = EnterProfile.keys[EnterProfile.length - 1].time;
            bool swung = false;
            for (float t = 0f; t * enterSpeed < clipEnd; t += Time.deltaTime)
            {
                float ct = t * enterSpeed;
                if (!swung && ct >= enterPushAt) { swung = true; swing(); }
                MoveTo(start + inward * (total * EnterProfile.Evaluate(ct)));
                if (health != null && health.IsDead) break;
                yield return null;
            }
            if (!swung) swing();
            // la segunda parte del clip se gira a mirar atras y vuelve al frente: se deja terminar sin moverse
            for (float t = 0f; t < 0.9f / enterSpeed; t += Time.deltaTime) yield return null;
            weapons?.SetHeldVisible(true);
            locked = false;
        }

        // ------------------------------------------------------------------ giro corriendo y voltereta
        /// <summary>Giro de 180 grados corriendo: frena derrapando, se da la vuelta y sale hacia el otro lado.</summary>
        public void RunTurn(float runSpeed)
        {
            if (locked) return;
            StartCoroutine(RunTurnRoutine(runSpeed));
        }

        IEnumerator RunTurnRoutine(float runSpeed)
        {
            locked = true;
            Trigger("RunTurn");
            Vector3 fwd = transform.forward;
            Quaternion from = transform.rotation;
            float turned = 0f;
            for (float t = 0f; t < runTurnTime; t += Time.deltaTime)
            {
                // el clip gira a la izquierda entre 0,3 y 1,5 s mientras sigue deslizandose hacia delante y frena
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1.5f, t));
                float ang = -180f * k;
                transform.rotation = from * Quaternion.Euler(0f, ang, 0f);
                if (player != null && player.cam != null) player.cam.AddYaw(ang - turned);
                turned = ang;
                float sp = runSpeed * 0.75f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 1.2f, t)));
                body.Move((fwd * sp + Vector3.down * 2f) * Time.deltaTime);
                if (health != null && health.IsDead) break;
                yield return null;
            }
            locked = false;
        }

        /// <summary>Voltereta al aterrizar de una caida alta (amortigua el golpe).</summary>
        public void Roll()
        {
            if (locked) return;
            StartCoroutine(RollRoutine());
        }

        IEnumerator RollRoutine()
        {
            locked = true;
            Trigger("Roll");
            Vector3 fwd = transform.forward;
            for (float t = 0f; t < rollTime; t += Time.deltaTime)
            {
                float sp = rollDistance / rollTime * 2f * (1f - t / rollTime);
                body.Move((fwd * sp + Vector3.down * 2f) * Time.deltaTime);
                yield return null;
            }
            locked = false;
        }

        /// <summary>Aterrizaje de una caida muy alta: se queda un momento agachado amortiguando el golpe.</summary>
        public void HardLand()
        {
            if (locked) return;
            StartCoroutine(HardLandRoutine());
        }

        IEnumerator HardLandRoutine()
        {
            locked = true;
            Trigger("HardLand");
            for (float t = 0f; t < 1.3f; t += Time.deltaTime) { body.Move(Vector3.down * 2f * Time.deltaTime); yield return null; }
            locked = false;
        }

        // ------------------------------------------------------------------ coberturas
        static bool IsLevel(Collider c) => c != null && c.attachedRigidbody == null && !(c is CharacterController)
            && c.GetComponentInParent<ZombieAI>() == null && c.GetComponentInParent<Pickup>() == null;

        /// <summary>Busca una pared u obstaculo justo delante y se cubre en el (de pie si es alto; agachado si es bajo o va agachado).</summary>
        public void TryCover(bool crouching)
        {
            if (locked) return;
            Vector3 low = transform.position + Vector3.down * 0.45f, high = transform.position + Vector3.up * 0.5f;
            Vector3 dir = transform.forward;
            if (!Physics.Raycast(low, dir, out var hit, 1.3f, ~0, QueryTriggerInteraction.Ignore) || !IsLevel(hit.collider) || Mathf.Abs(hit.normal.y) > 0.3f)
            {
                Hud.Message("No hay donde cubrirse");
                return;
            }
            bool tall = Physics.Raycast(high, dir, out var hh, 1.6f, ~0, QueryTriggerInteraction.Ignore) && IsLevel(hh.collider);
            StartCoroutine(CoverRoutine(hit.point, hit.normal, tall && !crouching ? 1 : 2));
        }

        IEnumerator CoverRoutine(Vector3 point, Vector3 normal, int type)
        {
            locked = true;
            CoverType = type;
            CoverMove = 0f;
            Trigger("CoverIn");
            Vector3 n = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
            Vector3 target = point + n * coverGap; target.y = transform.position.y;
            Quaternion from = transform.rotation, to = Quaternion.LookRotation(n);     // de espaldas a la pared
            const float enter = 0.85f;
            for (float t = 0f; t < enter; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / enter);
                MoveTo(Vector3.Lerp(transform.position, target, k));
                transform.rotation = Quaternion.Slerp(from, to, k);
                yield return null;
            }
            transform.rotation = to;
            Vector3 side = Vector3.Cross(Vector3.up, n);                              // su derecha (mirando hacia fuera de la pared)
            float sp = type == 1 ? coverSpeed : coverCrouchSpeed;
            bool aimOut = false;
            while (true)
            {
                if (health != null && health.IsDead) break;
                var kb = Keyboard.current; var ms = Mouse.current;
                if (GameState.InputBlocked || kb == null) { yield return null; continue; }
                if (ms != null && ms.rightButton.isPressed) { aimOut = true; break; }    // apuntar: sale al momento
                if (kb.vKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) break;
                // A/D (segun la camara) mueven a lo largo de la pared; solo si la pared sigue (en el borde se para)
                var camT = player != null && player.cam != null ? player.cam.transform : Camera.main.transform;
                Vector3 cf = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized, cr = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;
                float ix = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f), iy = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                Vector3 want = cr * ix + cf * iy;
                float along = Vector3.Dot(want, side);
                if (Vector3.Dot(want, n) > 0.7f && Mathf.Abs(along) < 0.3f) break;       // empujar hacia fuera de la pared: sale
                float move = 0f;
                if (Mathf.Abs(along) > 0.2f)
                {
                    Vector3 next = transform.position + side * (Mathf.Sign(along) * sp * Time.deltaTime);
                    Vector3 probe = next + side * (Mathf.Sign(along) * 0.25f) + Vector3.down * 0.45f;
                    if (Physics.Raycast(probe, -n, out var wall, coverGap + 0.4f, ~0, QueryTriggerInteraction.Ignore) && IsLevel(wall.collider))
                    {
                        body.Move(next - transform.position + Vector3.down * 0.05f);
                        move = Mathf.Sign(along);
                    }
                }
                CoverMove = Mathf.MoveTowards(CoverMove, -move, 6f * Time.deltaTime);    // +1 = hacia su izquierda
                yield return null;
            }
            CoverType = 0;
            CoverMove = 0f;
            if (!aimOut && (health == null || !health.IsDead))
            {
                Trigger("CoverOut");
                Vector3 away = transform.position + n * 0.35f;
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { MoveTo(Vector3.Lerp(transform.position, away, t / 0.5f)); yield return null; }
            }
            locked = false;
        }

        // ------------------------------------------------------------------ agarre
        /// <summary>Un zombi intenta agarrarte para morderte el cuello. Devuelve false si ahora no puede (otra accion, recien soltado...).</summary>
        public static bool TryGrab(ZombieAI z)
        {
            var a = Instance;
            if (a == null || a.locked || z == null || Time.time < a.nextGrab || GameState.InputBlocked) return false;
            if (a.health != null && a.health.IsDead) return false;
            a.StartCoroutine(a.GrabRoutine(z));
            return true;
        }

        IEnumerator GrabRoutine(ZombieAI z)
        {
            locked = true;
            grabber = z;
            Vector3 dir = Vector3.ProjectOnPlane(z.transform.position - transform.position, Vector3.up);
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dir);
            z.BeginGrab(transform.position + dir * 0.6f, transform.position);
            int presses = 0;
            float t = 0f, nextBite = 0.5f;
            const string text = "¡Te muerde!  Pulsa E repetidamente para soltarte";
            Hud.SetQte(text, 0f);
            while (t < grabTime && presses < mashNeeded)
            {
                if (health != null && health.IsDead) break;
                if (z == null || z.Hp.IsDead) break;
                if (!GameState.InputBlocked)
                {
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) presses++;
                    t += Time.deltaTime;
                    if (t >= nextBite) { nextBite += biteEvery; health?.TakeDamage(biteDamage, z.transform.position); }
                }
                Hud.SetQte(text, presses / (float)mashNeeded);
                yield return null;
            }
            bool freed = presses >= mashNeeded;
            if (!freed && z != null && !z.Hp.IsDead && health != null && !health.IsDead) health.TakeDamage(failDamage, z.transform.position);
            Hud.ClearQte();
            if (z != null) z.EndGrab(freed);
            if (freed) Hud.Message("Te has soltado de un empujon");
            grabber = null;
            nextGrab = Time.time + grabCooldown;
            locked = false;
        }
    }
}
