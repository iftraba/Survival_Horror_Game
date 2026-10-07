using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Ataques especiales del segundo jefe, ademas del golpe a cuerpo a cuerpo de ZombieAI:
    /// - Embestida: se para y ruge (el cuerpo brilla en rojo), echa a correr en linea recta hacia donde estabas y,
    ///   si choca con una columna o la caldera, se queda aturdido (recibe mas dano).
    /// - Escupitajo de acido: proyectil que hace dano y deja un charco donde cae (3 a la vez en la ultima fase).
    /// - Fases: al 66 % y al 33 % de vida se enfurece (mas rapido, ataques mas seguidos, rastro mas denso).
    /// Mientras ejecuta un ataque la IA normal queda suspendida (ZombieAI.Suspended).
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class BossAttacks : MonoBehaviour
    {
        [Header("Embestida")]
        public float chargeSpeed = 8f;
        public float chargeDamage = 45f;
        public float chargeWindup = 1.0f;
        public float chargeMaxTime = 2.2f;
        [Tooltip("Segundos aturdido tras estrellarse")] public float stunTime = 3f;
        [Tooltip("Multiplicador de dano recibido mientras esta aturdido")] public float stunDamageMultiplier = 1.8f;

        [Header("Escupitajo")]
        public float spitDamage = 12f;
        public float spitSpeed = 12f;
        public float spitWindup = 0.6f;

        [Header("Sonidos (vacio = los genericos del jefe)")]
        public AudioClip chargeSound, crashSound, spitSound;

        [Header("Ritmo")]
        [Tooltip("Pausa entre ataques especiales (segundos, min-max)")] public Vector2 pause = new Vector2(3.5f, 6f);

        public int Phase { get; private set; } = 1;

        ZombieAI ai;
        Health health;
        NavMeshAgent agent;
        ToxicTrail trail;
        ZombieAnimation anim;
        Transform player;
        Health playerHealth;
        Renderer[] renderers;
        MaterialPropertyBlock mpb;
        float nextAction, speedScale = 1f;
        int pendingPhase = 1;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            trail = GetComponent<ToxicTrail>();
            anim = GetComponent<ZombieAnimation>();
            renderers = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) { player = pc.transform; playerHealth = pc.GetComponent<Health>(); }
            nextAction = Time.time + 2f;
            StartCoroutine(Run());
        }

        void Update()
        {
            if (health.IsDead || ai.IsDormant) return;
            float f = health.Current / health.maxHealth;
            if (f < 0.33f && pendingPhase < 3) pendingPhase = 3;
            else if (f < 0.66f && pendingPhase < 2) pendingPhase = 2;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        bool LineClear()
        {
            Vector3 from = transform.position + Vector3.up * 1.6f;
            Vector3 to = player.position + Vector3.up * 1f;
            Vector3 d = to - from;
            foreach (var h in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController) continue;
                if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                return false;
            }
            return true;
        }

        // Aviso visual: el cuerpo brilla en rojo antes de atacar
        void Tell(bool on)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (on) { mpb.SetColor(EmissionId, new Color(2.2f, 0.12f, 0.08f)); r.SetPropertyBlock(mpb); }
                else r.SetPropertyBlock(null);
            }
        }

        IEnumerator Face(float seconds)
        {
            for (float t = 0f; t < seconds && !health.IsDead; t += Time.deltaTime)
            {
                Vector3 d = Flat(player.position - transform.position);
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
                yield return null;
            }
        }

        IEnumerator Run()
        {
            while (!health.IsDead)
            {
                yield return null;
                if (ai.IsDormant || player == null || playerHealth == null || playerHealth.IsDead || !ai.IsChasing) continue;

                if (pendingPhase > Phase) { yield return Enrage(pendingPhase); continue; }
                if (Time.time < nextAction) continue;

                float dist = Flat(player.position - transform.position).magnitude;
                if (dist <= ai.attackRange + 0.6f) { nextAction = Time.time + 0.8f; continue; }   // pegado a el: manda el golpe normal
                if (!LineClear()) { nextAction = Time.time + 0.8f; continue; }

                if (dist >= 5f && dist <= 18f && Random.value < 0.55f) yield return Charge();
                else if (dist >= 6f) yield return Spit();
                else { nextAction = Time.time + 0.8f; continue; }

                nextAction = Time.time + Random.Range(pause.x, pause.y) / (1f + 0.3f * (Phase - 1));
            }
        }

        IEnumerator Enrage(int phase)
        {
            ai.Suspended = true;
            Phase = phase;
            Tell(true);
            GameAudio.Play(Sfx.BossRoar, transform.position, 1f, 0.9f, false);
            Hud.Message(phase == 2 ? "La abominación se enfurece" : "La abominación está fuera de sí");
            yield return Face(1.4f);
            Tell(false);
            ai.chaseSpeed *= 1.12f;
            ai.attackCooldown *= 0.85f;
            speedScale *= 0.85f;
            if (trail != null) trail.spacing *= 0.8f;
            ai.Suspended = false;
            nextAction = Time.time + 1.5f;
        }

        IEnumerator Charge()
        {
            ai.Suspended = true;
            Tell(true);
            if (chargeSound != null) GameAudio.PlayClip(chargeSound, transform.position, 1f, 1f, false);
            else GameAudio.Play(Sfx.BossRoar, transform.position, 1f, 1.1f, false);
            yield return Face(chargeWindup * speedScale);
            Tell(false);

            Vector3 dir = Flat(player.position - transform.position).normalized;
            bool hit = false, blocked = false;
            int slow = 0;
            if (anim != null) anim.speedOverride = chargeSpeed;   // animacion de carrera durante la embestida
            for (float t = 0f; t < chargeMaxTime && !health.IsDead; t += Time.deltaTime)
            {
                if (agent == null || !agent.isOnNavMesh) break;
                Vector3 step = dir * chargeSpeed * Time.deltaTime;
                Vector3 before = transform.position;
                agent.Move(step);
                transform.rotation = Quaternion.LookRotation(dir);
                if (t > 0.25f && Flat(transform.position - before).magnitude < step.magnitude * 0.35f)
                {
                    if (++slow >= 3) { blocked = true; break; }
                }
                else slow = 0;

                if (!hit && playerHealth != null && Flat(player.position - transform.position).magnitude < 1.8f && Mathf.Abs(player.position.y - transform.position.y) < 2f)
                {
                    hit = true;
                    playerHealth.TakeDamage(chargeDamage, transform.position);
                }
                yield return null;
            }

            if (anim != null) anim.speedOverride = -1f;
            if (blocked && !health.IsDead)
            {
                if (crashSound != null) GameAudio.PlayClip(crashSound, transform.position, 1f, 1f, false);
                else GameAudio.Play(Sfx.BossStep, transform.position, 1f, 0.6f, false);
                Hud.Message("¡Se ha estrellado!");
                health.damageTakenMultiplier = stunDamageMultiplier;
                yield return new WaitForSeconds(stunTime);
                health.damageTakenMultiplier = 1f;
            }
            else yield return new WaitForSeconds(0.5f);
            ai.Suspended = false;
        }

        IEnumerator Spit()
        {
            ai.Suspended = true;
            Tell(true);
            if (spitSound != null) GameAudio.PlayClip(spitSound, transform.position, 0.9f, 1f, true);
            else GameAudio.Play(Sfx.ZombieAttack, transform.position, 1f, 0.6f, true);
            yield return Face(spitWindup * speedScale);
            Tell(false);
            if (!health.IsDead && player != null)
            {
                Vector3 from = transform.position + Vector3.up * 2.2f + transform.forward * 0.6f;
                Vector3 aim = (player.position + Vector3.up * 1f - from).normalized;
                int n = Phase >= 3 ? 3 : 1;
                for (int i = 0; i < n; i++)
                {
                    float yaw = n == 1 ? 0f : (i - 1) * 13f;
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "AcidSpit";
                    Destroy(go.GetComponent<Collider>());
                    go.transform.position = from;
                    go.transform.localScale = Vector3.one * 0.4f;
                    if (trail != null && trail.spitMaterial != null) { var rr = go.GetComponent<Renderer>(); rr.sharedMaterial = trail.spitMaterial; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                    var glow = new GameObject("Glow").AddComponent<Light>();     // brillo verde: la bola ilumina lo que cruza
                    glow.transform.SetParent(go.transform, false); glow.type = LightType.Point; glow.color = new Color(0.4f, 1f, 0.2f); glow.range = 4f; glow.intensity = 3f; glow.shadows = LightShadows.None;
                    var s = go.AddComponent<AcidSpit>();
                    s.velocity = Quaternion.Euler(0f, yaw, 0f) * aim * spitSpeed;
                    s.damage = spitDamage;
                    s.trail = trail;
                    s.owner = transform;
                }
            }
            yield return new WaitForSeconds(0.4f);
            ai.Suspended = false;
        }

        void OnDisable()
        {
            if (health != null) health.damageTakenMultiplier = 1f;
        }
    }

    /// <summary>Proyectil de acido del jefe: avanza en linea recta, hace dano al jugador y deja un charco donde cae.</summary>
    public class AcidSpit : MonoBehaviour
    {
        public Vector3 velocity;
        public float damage = 12f;
        public ToxicTrail trail;
        public Transform owner;
        float life;

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            Vector3 step = velocity * dt;
            Vector3 p = transform.position;
            bool hitSomething = false;
            foreach (var h in Physics.SphereCastAll(p, 0.18f, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && h.collider.transform.IsChildOf(owner)) continue;
                if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                var pc = h.collider.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    var hp = pc.GetComponent<Health>();
                    if (hp != null) hp.TakeDamage(damage, p);
                }
                if (trail != null) trail.AddPuddle(h.point == Vector3.zero ? p : h.point);
                hitSomething = true;
                break;
            }
            if (hitSomething || life > 4f) { Destroy(gameObject); return; }
            transform.position = p + step;
            transform.Rotate(180f * dt, 260f * dt, 0f);   // gira: se nota la textura veteada
        }
    }
}
