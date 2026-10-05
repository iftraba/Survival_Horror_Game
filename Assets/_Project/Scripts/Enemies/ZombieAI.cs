using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    [RequireComponent(typeof(Health))]
    public class ZombieAI : MonoBehaviour
    {
        public float detectRange = 14f;
        public float loseRange = 22f;
        public float attackRange = 1.6f;
        public float attackDamage = 15f;
        public float attackCooldown = 1.3f;
        public float walkSpeed = 1.1f;
        public float chaseSpeed = 1.4f;
        public float staggerTime = 0.45f;
        [Tooltip("A esta distancia te oye aunque haya paredes en medio.")]
        public float hearingRange = 4.5f;

        public event System.Action Attacked;

        /// <summary>Zombis activos (vivos): lo usa el audio para subir la musica de tension.</summary>
        public static readonly System.Collections.Generic.List<ZombieAI> All = new System.Collections.Generic.List<ZombieAI>();
        public bool IsChasing => chasing && health != null && !health.IsDead;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        NavMeshAgent agent;
        Health health;
        Transform player;
        Health playerHealth;
        float nextAttack, staggerUntil;
        bool chasing;

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            health.Died += OnDied;
            health.Damaged += _ => { staggerUntil = Time.time + staggerTime; chasing = true; };
        }

        void Start()
        {
            // El agente coloca el PIVOTE sobre el NavMesh. Aqui el pivote esta en el centro de la capsula,
            // asi que hay que elevarlo la distancia hasta los pies o el modelo queda hundido en el suelo.
            var cap = GetComponent<CapsuleCollider>();
            if (agent != null && cap != null) agent.baseOffset = cap.height * 0.5f - cap.center.y;

            // Si el agente se inicializo antes de que existiera el NavMesh, se reengancha ahora
            if (agent != null && !agent.isOnNavMesh)
            {
                agent.enabled = false;
                agent.enabled = true;
            }

            CompensateNavMeshLift();
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                playerHealth = pc.GetComponent<Health>();
            }
        }

        void Update()
        {
            if (health.IsDead || player == null || playerHealth == null || playerHealth.IsDead)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);
            // Te detecta si te ve (sin paredes en medio) o si estas tan cerca que te oye
            if (!chasing && (dist <= hearingRange || (dist <= detectRange && Time.time >= nextSightCheck && CanSeePlayer())))
                chasing = true;
            else if (chasing && dist > loseRange) chasing = false;

            bool staggered = Time.time < staggerUntil;
            bool canNav = agent != null && agent.isOnNavMesh;

            if (!chasing || staggered)
            {
                if (canNav) agent.isStopped = true;
                return;
            }

            if (dist <= attackRange)
            {
                if (canNav) agent.isStopped = true;
                FaceTarget();
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    Attacked?.Invoke();
                    playerHealth.TakeDamage(attackDamage, transform.position);
                }
                return;
            }

            if (canNav)
            {
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);
            }
            else
            {
                FaceTarget();
                transform.position += transform.forward * (walkSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// El NavMesh queda unos centimetros por encima del suelo real (margen de voxeles al hornearlo). Se mide la
        /// diferencia con un rayo hacia abajo y se resta del offset del agente para que los pies pisen el suelo.
        /// </summary>
        void CompensateNavMeshLift()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            Vector3 onMesh = agent.nextPosition - Vector3.up * agent.baseOffset;
            var hits = Physics.RaycastAll(onMesh + Vector3.up * 0.5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            float groundY = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.rigidbody != null || h.collider is CharacterController || h.collider.GetComponentInParent<ZombieAI>() != null) continue;
                if (h.point.y > groundY) groundY = h.point.y;
            }
            if (float.IsNegativeInfinity(groundY)) return;
            float lift = Mathf.Clamp(onMesh.y - groundY, 0f, 0.3f);
            agent.baseOffset -= lift;
        }

        float nextSightCheck;

        /// <summary>Linea de vision de los ojos al pecho del jugador; solo bloquean el nivel y los muebles.</summary>
        bool CanSeePlayer()
        {
            nextSightCheck = Time.time + 0.25f;
            Vector3 eye = transform.position + Vector3.up * 0.6f;
            Vector3 chest = player.position + Vector3.up * 0.4f;
            Vector3 d = chest - eye;
            foreach (var h in Physics.RaycastAll(eye, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController) continue;      // objetos sueltos y el jugador
                if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;           // otros zombis no tapan
                return false;
            }
            return true;
        }

        /// <summary>Golpea una puerta cerrada que le corta el paso (misma animacion y sonido que el ataque).</summary>
        public void BashDoor() => Attacked?.Invoke();

        /// <summary>Un ruido fuerte (disparo) despierta a los zombis que esten dentro del radio, haya o no paredes.</summary>
        public static void Noise(Vector3 position, float radius)
        {
            foreach (var z in All)
                if (z != null && !z.chasing && Vector3.Distance(z.transform.position, position) <= radius) z.chasing = true;
        }

        void FaceTarget()
        {
            Vector3 d = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 8f * Time.deltaTime);
        }

        void OnDied()
        {
            if (agent != null) agent.enabled = false;
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            // Sin modelo animado, tumbamos la capsula; con animator la animacion de muerte lo hace.
            if (GetComponentInChildren<Animator>() == null)
            {
                transform.rotation = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);
                transform.position += Vector3.down * 0.5f;
            }
            enabled = false;
            Destroy(gameObject, 15f);
        }
    }
}
