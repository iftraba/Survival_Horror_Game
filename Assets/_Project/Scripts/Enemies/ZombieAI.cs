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
        /// <summary>Multiplicador de velocidad de todos los enemigos (lo fija GameFlow al empezar la partida).</summary>
        public static float SpeedMultiplier = 1f;
        /// <summary>Suelo de velocidad de persecucion (m/s): ningun enemigo persigue mas lento que esto (lo fija GameFlow).</summary>
        public static float MinSpeed = 0f;
        public float staggerTime = 0.45f;
        [Tooltip("A esta distancia te oye aunque haya paredes en medio.")]
        public float hearingRange = 4.5f;

        [System.Serializable]
        public struct AttackVariant
        {
            [Tooltip("Segundos desde que empieza la animacion hasta que el golpe conecta")] public float hitDelay;
            [Tooltip("Multiplicador del dano de ataque")] public float damageMultiplier;
            [Tooltip("Pausa hasta el siguiente ataque (0 = usa attackCooldown)")] public float cooldown;
        }

        [Header("Ataques")]
        [Tooltip("Variantes de ataque (cada una con su animacion). Vacio = un ataque que golpea al instante.")]
        public AttackVariant[] attackVariants;
        /// <summary>Variante elegida en el ultimo ataque: la lee la animacion antes de lanzar el ataque.</summary>
        public int LastAttackVariant { get; private set; }

        [Header("Jefe / estados especiales")]
        [Tooltip("Letargo: no detecta ni persigue hasta que se le despierte (Wake), le disparen o haya un ruido fuerte cerca. El jefe baila.")]
        public bool dormant;
        [Tooltip("Objeto que suelta al morir (la llave de salida)")] public ItemData dropOnDeath;
        [Tooltip("Nombre para la barra de vida (solo jefes)")] public string bossName;

        /// <summary>El jefe que esta peleando ahora (para la barra de vida del HUD).</summary>
        public static ZombieAI ActiveBoss { get; private set; }
        public Health Hp => health;
        public bool IsDormant => dormant;

        public event System.Action Attacked;
        /// <summary>Empieza a perseguir al jugador (grito de alerta). Lo usa la animacion.</summary>
        public event System.Action Alerted;
        [Tooltip("Segundos que se queda gritando al detectarte antes de ir a por ti (0 = sin grito)")]
        public float alertTime = 1.6f;

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
        Coroutine pendingHit;

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            health.Died += OnDied;
            health.Damaged += _ =>
            {
                if (dormant) { Wake(); return; }                       // le han dado: se despierta (con rugido)
                if (staggerTime > 0f) staggerUntil = Time.time + staggerTime;
                StartChase(false);
            };
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

            if (dormant)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);
            // Te detecta si te ve (sin paredes en medio) o si estas tan cerca que te oye
            if (!chasing && (dist <= hearingRange || (dist <= detectRange && Time.time >= nextSightCheck && CanSeePlayer())))
                StartChase(true);
            else if (chasing && dist > loseRange) chasing = false;

            bool staggered = Time.time < staggerUntil;
            bool canNav = agent != null && agent.isOnNavMesh;

            if (!chasing || staggered)
            {
                if (canNav) agent.isStopped = true;
                if (chasing) FaceTarget();   // gritando o aturdido: sigue mirandote
                return;
            }

            if (dist <= attackRange)
            {
                if (canNav) agent.isStopped = true;
                FaceTarget();
                if (Time.time >= nextAttack)
                {
                    int n = attackVariants != null ? attackVariants.Length : 0;
                    LastAttackVariant = n > 0 ? Random.Range(0, n) : 0;
                    var v = n > 0 ? attackVariants[LastAttackVariant] : new AttackVariant { damageMultiplier = 1f };
                    nextAttack = Time.time + (v.cooldown > 0f ? v.cooldown : attackCooldown);
                    Attacked?.Invoke();
                    if (pendingHit != null) StopCoroutine(pendingHit);
                    pendingHit = StartCoroutine(LandHit(v.hitDelay, attackDamage * (v.damageMultiplier > 0f ? v.damageMultiplier : 1f)));
                }
                return;
            }

            if (canNav)
            {
                agent.isStopped = false;
                agent.speed = Mathf.Max(chaseSpeed * SpeedMultiplier, MinSpeed);
                agent.SetDestination(player.position);
            }
            else
            {
                FaceTarget();
                transform.position += transform.forward * (walkSpeed * SpeedMultiplier * Time.deltaTime);
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

        /// <summary>Pasa a perseguir. Si 'scream', se queda quieto gritando un momento antes (si le disparan, no).</summary>
        void StartChase(bool scream)
        {
            if (chasing || dormant) return;
            chasing = true;
            if (scream && alertTime > 0f)
            {
                staggerUntil = Mathf.Max(staggerUntil, Time.time + alertTime);
                Alerted?.Invoke();
            }
        }

        /// <summary>Despierta del letargo: ruge (animacion de alerta) y pasa a combatir.</summary>
        public void Wake()
        {
            if (!dormant) return;
            dormant = false;
            if (!string.IsNullOrEmpty(bossName))
            {
                ActiveBoss = this;
                Objectives.Set("Derrota al jefe: tiene la llave de la salida.");
            }
            StartChase(true);
        }

        /// <summary>El golpe conecta tras 'delay' segundos, si sigue vivo y el jugador sigue a tiro.</summary>
        System.Collections.IEnumerator LandHit(float delay, float damage)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            pendingHit = null;
            if (health.IsDead || playerHealth == null || playerHealth.IsDead || player == null) yield break;
            if (Time.time < staggerUntil && staggerTime > 0f && delay > 0f) yield break;      // un disparo lo interrumpe
            if (Vector3.Distance(transform.position, player.position) > attackRange * 1.35f) yield break;   // esquivado
            playerHealth.TakeDamage(damage, transform.position);
        }

        /// <summary>Golpea una puerta cerrada que le corta el paso (misma animacion y sonido que el ataque).</summary>
        public void BashDoor() => Attacked?.Invoke();

        /// <summary>Un ruido fuerte (disparo) despierta a los zombis que esten dentro del radio, haya o no paredes.</summary>
        public static void Noise(Vector3 position, float radius)
        {
            foreach (var z in All)
            {
                if (z == null || z.chasing || Vector3.Distance(z.transform.position, position) > radius) continue;
                if (z.dormant) z.Wake(); else z.StartChase(true);
            }
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
            if (ActiveBoss == this) ActiveBoss = null;
            if (dropOnDeath != null)
            {
                Objectives.Set("Recoge la llave que ha soltado el jefe y sal por la puerta del fondo.");
                // cae delante de donde murio, algo elevado para que no quede dentro del cuerpo
                var items = GameObject.Find("Items");
                var pk = Pickup.Spawn(dropOnDeath, 1, transform.position + transform.forward * 0.9f + Vector3.up * 1.2f, transform.forward * 0.6f);
                if (items != null) pk.transform.SetParent(items.transform);
                Hud.Message("El jefe ha soltado: " + dropOnDeath.displayName);
            }
            enabled = false;
            Destroy(gameObject, 15f);
        }
    }
}
