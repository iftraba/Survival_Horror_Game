using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Embestida del primer jefe (2026-10-08): a media distancia ruge un instante y echa a correr hacia donde estas, arrasando los
    /// muebles que encuentra (PropBreaker). Si te alcanza, golpe fuerte y te empuja; si choca con una pared, se queda aturdido un
    /// momento. Mientras, la IA normal queda suspendida. Comparte turno con el salto (BossLeap): no hace los dos a la vez.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class BossRush : MonoBehaviour
    {
        public float minDistance = 5f;
        public float maxDistance = 16f;
        [Tooltip("Pausa entre embestidas (segundos, min-max)")] public Vector2 cooldown = new Vector2(6f, 10f);
        public float speed = 7.5f;
        public float maxTime = 2.2f;
        public float windup = 0.6f;
        public float damage = 35f;
        [Tooltip("Metros que empuja al jugador si le alcanza")] public float knockback = 2.2f;
        public float wallStun = 1.6f;
        public AudioClip crashSound;

        ZombieAI ai;
        Health health;
        NavMeshAgent agent;
        ZombieAnimation za;
        BossLeap leap;
        Transform player;
        Health playerHealth;
        CharacterController playerBody;
        float next;
        bool wasDormant = true;

        public bool Busy { get; private set; }

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            za = GetComponent<ZombieAnimation>();
            leap = GetComponent<BossLeap>();
        }

        void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) { player = pc.transform; playerHealth = pc.GetComponent<Health>(); playerBody = pc.GetComponent<CharacterController>(); }
            next = Time.time + 6f;
        }

        void Update()
        {
            if (wasDormant && !ai.IsDormant) next = Mathf.Max(next, Time.time + ai.alertTime + 3f);   // no embiste mientras ruge
            wasDormant = ai.IsDormant;
            if (Busy || health.IsDead || ai.IsDormant || !ai.IsChasing || ai.Suspended || player == null || playerHealth == null || playerHealth.IsDead) return;
            if (leap != null && leap.Busy) return;
            if (Time.time < next) return;
            Vector3 d = player.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < minDistance || dist > maxDistance || Mathf.Abs(player.position.y - transform.position.y) > 1.5f
                || !PropBreaker.ClearForBoss(transform.position + Vector3.up * 0.2f, player.position + Vector3.up * 0.2f, transform))   // camino sin paredes (los muebles los arrasa)
            {
                next = Time.time + 0.5f;
                return;
            }
            StartCoroutine(Rush());
        }

        IEnumerator Rush()
        {
            Busy = true;
            ai.Suspended = true;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (leap != null) leap.Delay(3f);
            GameAudio.Play(Sfx.BossRoar, transform.position, 1f, 0.85f, false);
            // se encara y toma impulso
            if (za != null) za.speedOverride = 0f;
            for (float t = 0f; t < windup && !health.IsDead; t += Time.deltaTime)
            {
                Vector3 to = player.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
                yield return null;
            }
            if (agent != null) agent.enabled = false;
            var cap = GetComponent<CapsuleCollider>();
            float radius = cap != null ? cap.radius : 0.8f;
            Vector3 dir = transform.forward;
            bool hitPlayer = false, hitWall = false;
            for (float t = 0f; t < maxTime && !health.IsDead; t += Time.deltaTime)
            {
                // corrige un poco el rumbo al principio (no es un misil: se puede esquivar a un lado)
                if (t < 0.35f)
                {
                    Vector3 to = player.position - transform.position; to.y = 0f;
                    if (to.sqrMagnitude > 0.01f) dir = Vector3.RotateTowards(dir, to.normalized, 1.2f * Time.deltaTime, 0f);
                    transform.rotation = Quaternion.LookRotation(dir);
                }
                if (za != null) za.speedOverride = speed;
                float step = speed * Time.deltaTime;
                Vector3 chest = transform.position + Vector3.up * 0.3f;
                PropBreaker.BreakInSphere(chest + dir * (radius + 0.3f), radius + 0.2f, transform.position);
                // paredes y obstaculos que no se rompen: se estrella
                foreach (var h in Physics.SphereCastAll(chest, radius * 0.8f, dir, step + 0.25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(transform) || h.collider is CharacterController || h.rigidbody != null) continue;
                    if (h.collider.GetComponentInParent<ZombieAI>() != null || PropBreaker.PropRoot(h.collider) != null) continue;
                    hitWall = true; break;
                }
                if (hitWall) break;
                transform.position += dir * step;
                Vector3 pd = player.position - transform.position; pd.y = 0f;
                if (pd.magnitude < radius + 0.7f && Vector3.Dot(pd, dir) > -0.2f) { hitPlayer = true; break; }
                yield return null;
            }
            if (za != null) za.speedOverride = 0f;
            if (agent != null)
            {
                agent.enabled = true;
                if (NavMesh.SamplePosition(transform.position, out var nh, 2.5f, NavMesh.AllAreas)) agent.Warp(nh.position);
            }
            if (hitPlayer && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damage, transform.position);
                Hud.Message("¡Te ha arrollado!");
                if (playerBody != null) StartCoroutine(Shove(dir));
                yield return new WaitForSeconds(0.8f);
            }
            else if (hitWall)
            {
                if (crashSound != null) GameAudio.PlayClip(crashSound, transform.position, 0.9f, 0.9f, false);
                else GameAudio.Play(Sfx.BossStep, transform.position, 1f, 0.5f, false);
                yield return new WaitForSeconds(wallStun);                                   // aturdido: momento para dispararle
            }
            else yield return new WaitForSeconds(0.5f);
            if (za != null) za.speedOverride = -1f;
            ai.Suspended = false;
            Busy = false;
            next = Time.time + Random.Range(cooldown.x, cooldown.y);
        }

        IEnumerator Shove(Vector3 dir)
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                if (playerBody == null || !playerBody.enabled) yield break;
                playerBody.Move((dir * (knockback / 0.3f) + Vector3.down * 2f) * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>Retrasa la siguiente embestida (lo llama el salto para no encadenarlos).</summary>
        public void Delay(float seconds) => next = Mathf.Max(next, Time.time + seconds);
    }
}
