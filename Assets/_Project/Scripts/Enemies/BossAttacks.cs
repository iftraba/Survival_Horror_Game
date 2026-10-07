using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Ataques especiales del segundo jefe, ademas del golpe a cuerpo a cuerpo de ZombieAI. Mientras ejecuta uno de los
    /// suyos la IA normal queda suspendida (ZombieAI.Suspended). Cada ataque avisa antes (el cuerpo brilla en rojo y, los de
    /// area, un circulo rojo en el suelo):
    /// - Embestida: carga en linea recta; si choca con una columna o una maquina se aturde (recibe mas dano).
    /// - Escupitajo: abanico de bolas de acido (3 / 5 / 7 segun la fase, en 1-3 oleadas) que dejan charcos.
    /// - Pisotón: onda en area alrededor del jefe; la tapa cualquier muro o maquina que haya en medio.
    /// - Lluvia de acido: circulos de aviso alrededor (y debajo) del jugador que explotan al cabo de un segundo.
    /// - Fases al 66 % y 33 % de vida: mas rapido, ataques mas seguidos, mas bolas y mas oleadas.
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
        [Tooltip("Bolas por oleada en cada fase")] public int[] spitCount = { 3, 5, 7 };
        [Tooltip("Oleadas seguidas en cada fase")] public int[] spitWaves = { 1, 2, 3 };

        [Header("Pisoton (area alrededor del jefe)")]
        public float slamRadius = 5.2f;
        public float slamDamage = 40f;
        public float slamWindup = 0.9f;

        [Header("Lluvia de acido")]
        public int rainCount = 5;
        public float rainRadius = 1.5f;
        public float rainDamage = 30f;
        public float rainDelay = 1.1f;

        [Header("Sonidos (vacio = los genericos del jefe)")]
        [Tooltip("Multiplica el volumen de todos los efectos de los ataques del jefe (0,7 = 30 % mas bajos)")] [Range(0f, 1f)] public float soundVolume = 0.7f;
        public AudioClip chargeSound, crashSound, spitSound;
        [Tooltip("Material del circulo rojo de aviso")] public Material warningMaterial;

        [Header("Ritmo")]
        [Tooltip("Pausa entre ataques especiales (segundos, min-max)")] public Vector2 pause = new Vector2(2.5f, 4.5f);

        public int Phase { get; private set; } = 1;

        ZombieAI ai;
        Health health;
        NavMeshAgent agent;
        ToxicTrail trail;
        ZombieAnimation anim;
        Transform player;
        Health playerHealth;
        Renderer[] renderers;
        MaterialPropertyBlock mpb, mpbRing;
        float nextAction, speedScale = 1f;
        int pendingPhase = 1;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            trail = GetComponent<ToxicTrail>();
            anim = GetComponent<ZombieAnimation>();
            renderers = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            mpbRing = new MaterialPropertyBlock();
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
                if (r == null || r.GetComponent<ParticleSystem>() != null) continue;
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

        float GroundY(Vector3 at)
        {
            float y = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(at + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController || h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                if (h.normal.y > 0.5f) y = Mathf.Max(y, h.point.y);
            }
            return float.IsNegativeInfinity(y) ? transform.position.y - 1f : y;
        }

        // ---------------------------------------------------------------- bucle de decision
        IEnumerator Run()
        {
            while (!health.IsDead)
            {
                yield return null;
                if (ai.IsDormant || player == null || playerHealth == null || playerHealth.IsDead || !ai.IsChasing) continue;

                if (pendingPhase > Phase) { yield return Enrage(pendingPhase); continue; }
                if (Time.time < nextAction) continue;

                float dist = Flat(player.position - transform.position).magnitude;
                bool los = LineClear();
                var options = new List<int>();   // 0 embestida, 1 escupitajo, 2 pisoton, 3 lluvia
                if (los && dist >= 5f && dist <= 18f) { options.Add(0); options.Add(0); }
                if (los && dist >= 4f) { options.Add(1); options.Add(1); }
                if (dist <= slamRadius + 1.2f) { options.Add(2); options.Add(2); if (dist <= ai.attackRange + 1.5f) options.Add(2); }
                if (los && dist >= 3f) { options.Add(3); options.Add(3); }   // la lluvia solo si te ve
                if (options.Count == 0) { nextAction = Time.time + 0.6f; continue; }

                switch (options[Random.Range(0, options.Count)])
                {
                    case 0: yield return Charge(); break;
                    case 1: yield return Spit(); break;
                    case 2: yield return Slam(); break;
                    default: yield return Rain(); break;
                }
                nextAction = Time.time + Random.Range(pause.x, pause.y) / (1f + 0.3f * (Phase - 1));
            }
        }

        IEnumerator Enrage(int phase)
        {
            ai.Suspended = true;
            Phase = phase;
            Tell(true);
            GameAudio.Play(Sfx.BossRoar, transform.position, soundVolume * 1f, 0.9f, false);
            Hud.Message(phase == 2 ? "La abominación se enfurece" : "La abominación está fuera de sí");
            yield return Face(1.4f);
            Tell(false);
            ai.chaseSpeed *= 1.12f;
            ai.attackCooldown *= 0.85f;
            speedScale *= 0.85f;
            if (trail != null) trail.spacing *= 0.8f;
            ai.Suspended = false;
            nextAction = Time.time + 1.2f;
        }

        // ---------------------------------------------------------------- embestida
        IEnumerator Charge()
        {
            ai.Suspended = true;
            Tell(true);
            if (chargeSound != null) GameAudio.PlayClip(chargeSound, transform.position, soundVolume * 1f, 1f, false);
            else GameAudio.Play(Sfx.BossRoar, transform.position, soundVolume * 1f, 1.1f, false);
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
                if (crashSound != null) GameAudio.PlayClip(crashSound, transform.position, soundVolume * 1f, 1f, false);
                else GameAudio.Play(Sfx.BossStep, transform.position, soundVolume * 1f, 0.6f, false);
                Hud.Message("¡Se ha estrellado!");
                health.damageTakenMultiplier = stunDamageMultiplier;
                yield return new WaitForSeconds(stunTime);
                health.damageTakenMultiplier = 1f;
            }
            else yield return new WaitForSeconds(0.5f);
            ai.Suspended = false;
        }

        // ---------------------------------------------------------------- escupitajo en abanico
        IEnumerator Spit()
        {
            ai.Suspended = true;
            Tell(true);
            if (spitSound != null) GameAudio.PlayClip(spitSound, transform.position, soundVolume * 0.9f, 1f, true);
            else GameAudio.Play(Sfx.ZombieAttack, transform.position, soundVolume * 1f, 0.6f, true);
            yield return Face(spitWindup * speedScale);
            int ph = Mathf.Clamp(Phase, 1, 3) - 1;
            int n = spitCount[Mathf.Min(ph, spitCount.Length - 1)], waves = spitWaves[Mathf.Min(ph, spitWaves.Length - 1)];
            for (int w = 0; w < waves && !health.IsDead && player != null; w++)
            {
                FireFan(n, 9f);
                if (w < waves - 1) yield return Face(0.45f);                // vuelve a apuntar entre oleadas
            }
            Tell(false);
            yield return new WaitForSeconds(0.4f);
            ai.Suspended = false;
        }

        void FireFan(int n, float stepDeg)
        {
            Vector3 from = transform.position + Vector3.up * 1.1f + transform.forward * 0.7f;   // la boca: ~2,6 m del suelo (la capsula del jefe esta centrada a 1,5 m)
            Vector3 aim = (player.position + Vector3.up * 1f - from).normalized;
            for (int i = 0; i < n; i++)
            {
                float yaw = (i - (n - 1) * 0.5f) * stepDeg + Random.Range(-2f, 2f);
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "AcidSpit";
                Destroy(go.GetComponent<Collider>());
                go.transform.position = from;
                go.transform.localScale = Vector3.one * 0.4f;
                if (trail != null && trail.spitMaterial != null) { var rr = go.GetComponent<Renderer>(); rr.sharedMaterial = trail.spitMaterial; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                if (trail != null && trail.spitTrailMaterial != null)                      // estela verde que se desvanece
                {
                    var tr = go.AddComponent<TrailRenderer>();
                    tr.sharedMaterial = trail.spitTrailMaterial;
                    tr.time = 0.22f; tr.minVertexDistance = 0.04f;
                    tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.34f), new Keyframe(1f, 0f));
                    var g = new Gradient();
                    g.SetKeys(new[] { new GradientColorKey(new Color(0.75f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.15f, 0.6f, 0.05f), 1f) },
                              new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
                    tr.colorGradient = g;
                    tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
                }
                if (n <= 5)   // con muchas bolas no se pone luz a cada una (coste)
                {
                    var glow = new GameObject("Glow").AddComponent<Light>();
                    glow.transform.SetParent(go.transform, false); glow.type = LightType.Point; glow.color = new Color(0.4f, 1f, 0.2f); glow.range = 4f; glow.intensity = 3f; glow.shadows = LightShadows.None;
                }
                var s = go.AddComponent<AcidSpit>();
                s.velocity = Quaternion.Euler(0f, yaw, 0f) * aim * spitSpeed * Random.Range(0.92f, 1.1f);
                s.damage = spitDamage;
                s.trail = trail;
                s.owner = transform;
            }
        }

        // ---------------------------------------------------------------- pisoton (area)
        IEnumerator Slam()
        {
            ai.Suspended = true;
            Tell(true);
            GameAudio.Play(Sfx.BossRoar, transform.position, soundVolume * 1f, 0.8f, false);
            var ring = SpawnWarning(transform.position, slamRadius);
            float wind = slamWindup * speedScale;
            for (float t = 0f; t < wind && !health.IsDead; t += Time.deltaTime)
            {
                PulseWarning(ring, t / wind);
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
            Tell(false);
            if (health.IsDead) { ai.Suspended = false; yield break; }

            if (crashSound != null) GameAudio.PlayClip(crashSound, transform.position, soundVolume * 1f, 0.9f, false);
            else GameAudio.Play(Sfx.BossStep, transform.position, soundVolume * 1f, 0.5f, false);
            Vector3 c = transform.position + Vector3.up * 1.2f;
            if (playerHealth != null && Flat(player.position - transform.position).magnitude <= slamRadius && Mathf.Abs(player.position.y - transform.position.y) < 2.5f && AreaReaches(c, player.position + Vector3.up * 1f))
            {
                playerHealth.TakeDamage(slamDamage, transform.position);
                Hud.Message("¡Onda de choque!");
            }
            if (trail != null)                                                        // anillos de acido que dejan el suelo peligroso
                for (int ringIdx = 0; ringIdx < 2; ringIdx++)
                {
                    float rad = slamRadius * (ringIdx == 0 ? 0.45f : 0.8f); int k = ringIdx == 0 ? 6 : 9;
                    for (int i = 0; i < k; i++)
                    {
                        float a = (i + ringIdx * 0.5f) / k * Mathf.PI * 2f;
                        Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad;
                        if (AreaReaches(c, p + Vector3.up * 0.5f)) trail.AddPuddle(p);
                    }
                }
            yield return new WaitForSeconds(0.6f);
            ai.Suspended = false;
        }

        // la onda no atraviesa muros ni maquinas: sirve resguardarse detras
        bool AreaReaches(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            foreach (var h in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController) continue;
                if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                if (h.collider.GetComponentInParent<EjectedCasing>() != null) continue;
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- lluvia de acido
        IEnumerator Rain()
        {
            ai.Suspended = true;
            Tell(true);
            if (spitSound != null) GameAudio.PlayClip(spitSound, transform.position, soundVolume * 1f, 0.8f, true);
            else GameAudio.Play(Sfx.BossRoar, transform.position, soundVolume * 1f, 1.2f, false);
            yield return Face(0.7f * speedScale);
            Tell(false);
            ai.Suspended = false;                                                   // el jefe sigue persiguiendo mientras caen
            if (player == null) yield break;
            int n = rainCount + (Phase - 1) * 2;
            for (int i = 0; i < n; i++)
            {
                Vector3 center = player.position;
                bool track = i < 2;                                                  // los dos primeros persiguen al jugador
                if (i >= 2) { var off = Random.insideUnitCircle * 5.5f; center += new Vector3(off.x, 0f, off.y); }
                StartCoroutine(RainStrike(center, 0.15f * i, track));
            }
        }

        IEnumerator RainStrike(Vector3 center, float delay, bool track)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            center.y = GroundY(center);
            var ring = SpawnWarning(center, rainRadius);
            float wait = rainDelay + (track ? 0.5f : 0f);
            for (float t = 0f; t < wait; t += Time.deltaTime)
            {
                // sigue al jugador (algo mas despacio de lo que anda) y se fija en los ultimos 0,35 s: se puede esquivar
                if (track && t < wait - 0.35f && player != null)
                {
                    Vector3 goal = new Vector3(player.position.x, center.y, player.position.z);
                    center = Vector3.MoveTowards(center, goal, 3.4f * Time.deltaTime);
                    if (ring != null) ring.position = new Vector3(center.x, GroundY(center) + 0.04f, center.z);
                }
                PulseWarning(ring, t / wait);
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
            if (playerHealth != null && !playerHealth.IsDead && Flat(player.position - center).magnitude <= rainRadius && Mathf.Abs(player.position.y - center.y) < 2.5f)
            {
                playerHealth.TakeDamage(rainDamage, center);
                Hud.Message("Ácido");
            }
            if (trail != null)
            {
                trail.AddPuddle(center);
                if (trail.sizzleSound != null) GameAudio.PlayClip(trail.sizzleSound, center, soundVolume * 0.8f, Random.Range(0.9f, 1.1f), true);
            }
        }

        // ---------------------------------------------------------------- circulos de aviso
        Transform SpawnWarning(Vector3 at, float radius)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "AcidWarning";
            Destroy(q.GetComponent<Collider>());
            q.transform.position = new Vector3(at.x, GroundY(at) + 0.04f, at.z);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            var r = q.GetComponent<Renderer>();
            if (warningMaterial != null) r.sharedMaterial = warningMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        void PulseWarning(Transform ring, float k)
        {
            if (ring == null) return;
            ring.Rotate(Vector3.forward, 70f * Time.deltaTime, Space.Self);   // la marca gira
            var r = ring.GetComponent<Renderer>();
            float a = Mathf.Lerp(0.45f, 1f, k) * (0.8f + 0.2f * Mathf.Sin(Time.time * 18f));
            mpbRing.SetColor(BaseColorId, new Color(1f, 1f, 1f, a));
            r.SetPropertyBlock(mpbRing);
        }

        void OnDisable()
        {
            if (health != null) health.damageTakenMultiplier = 1f;
        }
    }

    /// <summary>
    /// Proyectil de acido del jefe: avanza en linea recta y se detiene en lo PRIMERO que toca (muros, maquinas, columnas...); si es
    /// el jugador le hace dano. Solo deja un charco si cae al suelo (superficie horizontal a ras de suelo), no en paredes ni maquinas.
    /// </summary>
    public class AcidSpit : MonoBehaviour
    {
        public Vector3 velocity;
        public float damage = 12f;
        public ToxicTrail trail;
        public Transform owner;
        float life, baseScale;
        const float Radius = 0.18f;

        bool Ignored(Collider c)
        {
            if (owner != null && c.transform.IsChildOf(owner)) return true;
            return c.GetComponentInParent<ZombieAI>() != null || c.GetComponentInParent<EjectedCasing>() != null || c.GetComponentInParent<Pickup>() != null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            Vector3 step = velocity * dt;
            Vector3 p = transform.position;

            // 1) ya esta dentro de algo (nace pegada a una maquina o una pared): impacto inmediato
            foreach (var c in Physics.OverlapSphere(p, Radius, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignored(c)) { Impact(c, p, Vector3.up); return; }
            // 2) barrido ordenado por distancia: el primer obstaculo manda (antes se tomaba el primero del array, sin ordenar)
            var hits = Physics.SphereCastAll(p, Radius, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (Ignored(h.collider)) continue;
                Impact(h.collider, h.distance <= 0f ? p : h.point, h.normal);
                return;
            }
            if (life > 4f) { Destroy(gameObject); return; }
            transform.position = p + step;
            // gota en vuelo: alargada en la direccion de avance, gira sobre ese eje y "late" un poco (liquido)
            if (baseScale <= 0f) baseScale = transform.localScale.x;
            float w = Mathf.Sin(life * 26f) * 0.08f;
            transform.rotation = Quaternion.LookRotation(velocity.sqrMagnitude > 0.01f ? velocity : Vector3.forward) * Quaternion.Euler(0f, 0f, life * 420f);
            transform.localScale = new Vector3(baseScale * (0.88f + w), baseScale * (0.88f - w), baseScale * 1.35f);
        }

        void Impact(Collider c, Vector3 point, Vector3 normal)
        {
            var pc = c.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                var hp = pc.GetComponent<Health>();
                if (hp != null) hp.TakeDamage(damage, transform.position);
            }
            // charco solo si cae al suelo: superficie casi horizontal y a ras de suelo (no en paredes, tapas de maquinas ni cajas)
            bool floor = normal.y > 0.7f && point.y < 0.4f;
            if (trail != null && (floor || pc != null)) trail.AddPuddle(pc != null ? new Vector3(point.x, 0f, point.z) : point);
            Destroy(gameObject);
        }
    }
}
