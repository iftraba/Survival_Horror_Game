using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Ataque en salto del primer jefe (clip "Mutant Jump Attack" del Creature Pack, 2026-10-08): si el jugador esta a media
    /// distancia y a la vista, se agacha, salta y cae donde estabas al despegar (sin aviso en el suelo: hay que verle saltar y apartarse).
    /// Al caer destroza los muebles de alrededor (PropBreaker).
    /// El clip sube el cuerpo; el avance lo hace este script. Mientras, la IA normal queda suspendida.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class BossLeap : MonoBehaviour
    {
        public float minDistance = 4.5f;
        public float maxDistance = 11f;
        [Tooltip("Pausa entre saltos (segundos, min-max)")] public Vector2 cooldown = new Vector2(7f, 11f);
        public float damage = 45f;
        [Tooltip("Radio del aplastamiento al caer")] public float radius = 2.6f;
        [Tooltip("Velocidad del estado JumpAttack del controlador (los tiempos de abajo se dividen por ella)")] public float animSpeed = 1.1f;
        [Tooltip("Segundos del clip (a velocidad 1): despega, aterriza y vuelve a estar listo")] public float takeoff = 0.55f, land = 1.6f, recover = 2.7f;
        [Tooltip("Cae un poco antes del punto marcado (no encima del jugador)")] public float landShort = 1.0f;
        [Tooltip("Ya no se usa: el salto no avisa con un circulo en el suelo")] public Material warningMaterial;
        [Tooltip("Subida de la cadera en el clip a escala 1 (m): el salto se aplana para no atravesar el techo")] public float clipRise = 2.0f;

        static readonly int LeapId = Animator.StringToHash("Leap");

        ZombieAI ai;
        Health health;
        NavMeshAgent agent;
        Animator anim;
        ZombieAnimation za;
        Transform player;
        Health playerHealth;
        float next;
        bool busy, wasDormant = true;
        BossRush rush;
        public bool Busy => busy;
        /// <summary>Retrasa el siguiente salto (lo llama la embestida para no encadenarlos).</summary>
        public void Delay(float seconds) => next = Mathf.Max(next, Time.time + seconds);
        // aplanado del salto: la altura la pone el clip y en las salas con techo bajo se reduce en proporcion
        Transform model, hips;
        float hipsRest, riseScale = 1f, shift;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            za = GetComponent<ZombieAnimation>();
            rush = GetComponent<BossRush>();
            anim = GetComponentInChildren<Animator>();
            if (anim != null) { model = anim.transform; if (anim.isHuman) hips = anim.GetBoneTransform(HumanBodyBones.Hips); }
        }

        void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) { player = pc.transform; playerHealth = pc.GetComponent<Health>(); }
            next = Time.time + 5f;
        }

        void Update()
        {
            // al despertar ruge: no salta hasta que acabe el rugido
            if (wasDormant && !ai.IsDormant) next = Mathf.Max(next, Time.time + ai.alertTime + 1.5f);
            wasDormant = ai.IsDormant;
            if (busy || health.IsDead || ai.IsDormant || !ai.IsChasing || ai.Suspended || player == null || playerHealth == null || playerHealth.IsDead) return;
            if (Time.time < next || (rush != null && rush.Busy)) return;
            Vector3 d = player.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < minDistance || dist > maxDistance || Mathf.Abs(player.position.y - transform.position.y) > 1.5f
                || !PropBreaker.ClearForBoss(transform.position + Vector3.up * 1.6f, player.position + Vector3.up * 0.6f, transform, 2.6f))   // te ve por encima de las estanterias
            {
                next = Time.time + 0.5f;
                return;
            }
            StartCoroutine(Leap());
        }

        IEnumerator Leap()
        {
            busy = true;
            ai.Suspended = true;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (za != null) za.speedOverride = 0f;
            // objetivo: donde esta el jugador ahora (marcado en el suelo), un poco antes para no caer encima
            Vector3 from = transform.position;
            Vector3 target = player.position;
            Vector3 dir = target - from; dir.y = 0f;
            float len = dir.magnitude;
            dir = len > 0.01f ? dir / len : transform.forward;
            Vector3 landAt = from + dir * Mathf.Max(0f, len - landShort);
            if (NavMesh.SamplePosition(landAt, out var hit, 2f, NavMesh.AllAreas)) landAt = new Vector3(hit.position.x, landAt.y, hit.position.z);
            if (rush != null) rush.Delay(3f);
            // techo: cuanto puede subir sin meterse en el
            if (hips != null)
            {
                hipsRest = hips.position.y - transform.position.y;
                float top = float.MaxValue;
                foreach (var h in Physics.RaycastAll(transform.position, Vector3.up, 12f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(transform) || h.rigidbody != null || h.collider is CharacterController) continue;
                    top = Mathf.Min(top, h.point.y); 
                }
                var cap = GetComponent<CapsuleCollider>();
                float headTop = transform.position.y + (cap != null ? cap.height * 0.5f : 1.5f);
                float allowed = Mathf.Max(0.3f, top - headTop - 0.15f);
                float rise = clipRise * model.lossyScale.y;
                riseScale = Mathf.Clamp01(allowed / rise);
            }
            if (anim != null) anim.SetTrigger(LeapId);
            GameAudio.Play(Sfx.BossRoar, transform.position, 0.9f, 1.1f, false);

            // agachado: se encara al objetivo
            float t0 = takeoff / animSpeed;
            for (float t = 0f; t < t0 && !health.IsDead; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
                yield return null;
            }
            // en el aire: avance horizontal hasta el punto de caida (la altura la pone el clip)
            if (agent != null) agent.enabled = false;
            float air = (land - takeoff) / animSpeed;
            for (float t = 0f; t < air && !health.IsDead; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / air);
                var p = Vector3.Lerp(from, landAt, k); p.y = from.y;
                transform.position = p;
                yield return null;
            }
            if (agent != null)
            {
                agent.enabled = true;
                if (NavMesh.SamplePosition(transform.position, out var h2, 2f, NavMesh.AllAreas)) agent.Warp(h2.position);
            }
            if (!health.IsDead)
            {
                GameAudio.Play(Sfx.BossStep, transform.position, 1f, 0.45f, false);
                PropBreaker.BreakInSphere(transform.position, radius * 0.85f, transform.position);   // aplasta los muebles donde cae
                Vector3 pd = player.position - transform.position; pd.y = 0f;
                if (pd.magnitude <= radius && Mathf.Abs(player.position.y - transform.position.y) < 2f && !playerHealth.IsDead)
                {
                    playerHealth.TakeDamage(damage, transform.position);
                    Hud.Message("¡Te ha caído encima!");
                }
                yield return new WaitForSeconds((recover - land) / animSpeed);
            }
            if (za != null) za.speedOverride = -1f;
            if (model != null) { model.localPosition += Vector3.up * shift; shift = 0f; }
            riseScale = 1f;
            ai.Suspended = false;
            busy = false;
            next = Time.time + Random.Range(cooldown.x, cooldown.y);
        }

        void LateUpdate()
        {
            if (!busy || hips == null || model == null) return;
            // la cadera sube por encima de su altura de reposo: se baja el modelo en proporcion (riseScale 1 = salto completo)
            model.localPosition += Vector3.up * shift;
            float excess = Mathf.Max(0f, hips.position.y - transform.position.y - hipsRest);
            shift = excess * (1f - riseScale);
            model.localPosition -= Vector3.up * shift;
        }
    }
}
