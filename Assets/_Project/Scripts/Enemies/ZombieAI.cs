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
        [Tooltip("OBSOLETO (ya no se usa): antes te detectaba a esta distancia aunque hubiera paredes en medio.")]
        public float hearingRange = 4.5f;
        [Tooltip("Angulo de vision (grados): te ve si estas delante, dentro de este cono, a menos de detectRange y sin paredes en medio")]
        public float viewAngle = 140f;
        [Tooltip("Muy cerca te nota aunque estes a su espalda (pero nunca a traves de una pared)")]
        public float closeSense = 1.8f;

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

        [Header("Escopetazos y agarre")]
        [Tooltip("Un escopetazo a menos de esta distancia lo tira al suelo (se levanta con la animacion de incorporarse)")]
        public float knockdownRange = 2.6f;
        [Tooltip("Segundos en el suelo hasta volver a perseguir: caida (1,4 s) + incorporarse (4,4 s a 1,3x)")]
        public float knockdownTime = 4.5f;
        [Tooltip("Probabilidad de quedarse aturdido con un escopetazo de mas lejos")] [Range(0f, 1f)] public float stunChance = 0.3f;
        public float stunTime = 2.4f;
        [Tooltip("Probabilidad de que un ataque a quemarropa sea un agarre con mordisco al cuello (hay que soltarse pulsando E)")]
        [Range(0f, 1f)] public float grabChance = 0.3f;
        public float grabRange = 1.1f;

        [Header("Jefe / estados especiales")]
        [Tooltip("Letargo: no detecta ni persigue hasta que se le despierte (Wake), le disparen o haya un ruido fuerte cerca. El jefe baila.")]
        public bool dormant;
        [Tooltip("En letargo solo despierta si le disparan (no por ruidos ni por verte): el que se come un cadaver")]
        public bool wakeOnlyWhenShot;
        [Header("Sala (comisaria grande)")]
        [Tooltip("Si el jugador entra en la sala (o grupo de salas) donde esta el zombi, este se lanza a por el: todos los de la sala a la vez. Hace falta el plano (MapData)")]
        public bool roomAggro = true;
        [Tooltip("Antes de detectarte deambula despacio por su sala en vez de quedarse parado (no los que comen ni los jefes)")]
        public bool wanderInRoom = true;
        [Tooltip("Segundos de espera entre un destino y el siguiente al deambular")] public Vector2 wanderPause = new Vector2(1.5f, 5f);
        [Tooltip("Un zombi en letargo (no jefe, no el que se come un cadaver) despierta si el jugador esta a menos de esta distancia con linea directa (agachado, a la mitad). 0 = no")]
        public float proximityWakeRange = 3.5f;
        [Tooltip("Objeto que suelta al morir (la llave de salida)")] public ItemData dropOnDeath;
        [Tooltip("Nombre para la barra de vida (solo jefes)")] public string bossName;

        /// <summary>El jefe que esta peleando ahora (para la barra de vida del HUD).</summary>
        public static ZombieAI ActiveBoss { get; private set; }
        public Health Hp => health;
        public bool IsDormant => dormant;
        /// <summary>Un script externo (ataques especiales del jefe) controla al enemigo: la IA normal se detiene.</summary>
        public bool Suspended { get; set; }
        /// <summary>En el suelo tras un escopetazo, aturdido o agarrando al jugador: no reacciona a los disparos con la animacion de golpe.</summary>
        public bool Busy => knocked || stunned || Grabbing;
        public bool KnockedDown => knocked;
        public bool Grabbing { get; private set; }
        /// <summary>Cuerpo bajo (reptante o con las piernas rotas): sin derribos, agarres ni muertes de pie.</summary>
        public bool LowPose { get { var c = GetComponent<CapsuleCollider>(); return c != null && c.height < 1.2f; } }
        bool knocked, stunned;
        /// <summary>Reacciones especiales para la animacion: "Knockdown", "Stun".</summary>
        public event System.Action<string> Reacted;

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
            if (MapData.Instance != null && MapData.Instance.TryRoomAt(transform.position.x, transform.position.z, transform.position.y - (cap != null ? cap.height * 0.5f : 1f), out homeRoom)) hasHome = true;
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

            if (dormant || Suspended)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                // el letargo normal se rompe al acercarte (el jefe solo lo despierta su sala; el carronero, solo un disparo)
                if (dormant && !Suspended && !wakeOnlyWhenShot && proximityWakeRange > 0f && string.IsNullOrEmpty(bossName) && Time.time >= nextSightCheck)
                {
                    nextSightCheck = Time.time + 0.2f;
                    float range = PlayerController.CrouchingNow ? proximityWakeRange * 0.5f : proximityWakeRange;
                    var feet = player.position;
                    if (Mathf.Abs(feet.y - transform.position.y) < 2.2f && Vector3.Distance(transform.position, feet) <= range && HasLineTo(player.position + Vector3.up * 0.4f)) Wake();
                }
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);
            // Si entras en su sala, ataca (todos los de la sala a la vez), aunque no te vea
            if (roomAggro && !chasing && hasHome && MapTracker.CurrentGroup == homeRoom.group && Mathf.Abs(player.position.y - transform.position.y) < 2.6f) StartChase(true);
            // Te detecta si te ve (sin paredes en medio) o si estas tan cerca que te oye
            if (!chasing && dist <= detectRange && Time.time >= nextSightCheck && CanSeePlayer(dist))
                StartChase(true);
            else if (chasing && dist > loseRange) chasing = false;

            bool staggered = Time.time < staggerUntil;
            bool canNav = agent != null && agent.isOnNavMesh;

            if (!chasing && !staggered && wanderInRoom && hasHome && canNav && dist < 45f && string.IsNullOrEmpty(bossName)) { Wander(); return; }
            if (!chasing || staggered)
            {
                if (canNav) agent.isStopped = true;
                if (chasing) FaceTarget();   // gritando o aturdido: sigue mirandote
                return;
            }

            if (dist <= attackRange && HasLineTo(player.position + Vector3.up * 0.4f))
            {
                if (canNav) agent.isStopped = true;
                FaceTarget();
                if (Time.time >= nextAttack && dist <= grabRange && CanSpecial && Random.value < grabChance && PlayerActions.TryGrab(this))
                {
                    nextAttack = Time.time + 2.5f;
                    return;
                }
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

        // ---- deambular por la sala
        MapRoom homeRoom; bool hasHome, hasWanderTarget; float wanderWaitUntil;

        void Wander()
        {
            if (Time.time < wanderWaitUntil) { agent.isStopped = true; return; }
            if (hasWanderTarget)
            {
                if (agent.pathPending) return;
                if (agent.remainingDistance > agent.stoppingDistance + 0.35f && agent.hasPath) { agent.isStopped = false; agent.speed = Mathf.Max(0.4f, walkSpeed * SpeedMultiplier * 0.8f); return; }
                hasWanderTarget = false; wanderWaitUntil = Time.time + Random.Range(wanderPause.x, wanderPause.y); agent.isStopped = true; return;
            }
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(Random.Range(homeRoom.x0 + 1.2f, homeRoom.x1 - 1.2f), homeRoom.floorY + 0.2f, Random.Range(homeRoom.z0 + 1.2f, homeRoom.z1 - 1.2f));
                if (!NavMesh.SamplePosition(p, out var hit, 1.2f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - transform.position.y) > 1.6f || !homeRoom.Contains(hit.position.x, hit.position.z)) continue;
                agent.isStopped = false; agent.speed = Mathf.Max(0.4f, walkSpeed * SpeedMultiplier * 0.8f);
                if (agent.SetDestination(hit.position)) { hasWanderTarget = true; return; }
            }
            wanderWaitUntil = Time.time + 2f;
        }

        /// <summary>Ojos: algo por encima del centro de la capsula (los jefes, mas altos, a su altura).</summary>
        Vector3 Eye()
        {
            var cap = GetComponent<CapsuleCollider>();
            float up = cap != null ? cap.height * 0.3f : 0.6f;
            return transform.position + Vector3.up * up;
        }

        /// <summary>Linea directa de los ojos al punto: solo la tapan el nivel, las puertas y los muebles (no otros zombis ni objetos sueltos).</summary>
        public bool HasLineTo(Vector3 target)
        {
            Vector3 eye = Eye();
            Vector3 d = target - eye;
            if (d.sqrMagnitude < 0.0001f) return true;
            foreach (var h in Physics.RaycastAll(eye, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.rigidbody != null || h.collider is CharacterController) continue;      // objetos sueltos y el jugador
                if (h.collider.GetComponentInParent<ZombieAI>() != null) continue;           // otros zombis no tapan
                return false;
            }
            return true;
        }

        /// <summary>
        /// Te ve si hay linea directa y estas dentro de su cono de vision (o muy cerca, aunque sea a su espalda). Una pared o una
        /// puerta cerrada lo impiden siempre: los zombis de una sala no reaccionan hasta que entras o abres la puerta.
        /// </summary>
        bool CanSeePlayer(float dist)
        {
            nextSightCheck = Time.time + 0.2f;
            Vector3 to = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            // agachado y algo lejos, no te distingue (sigilo); de cerca te nota igual
            if (PlayerController.CrouchingNow && dist > detectRange * 0.55f) return false;
            bool inCone = dist <= closeSense || Vector3.Angle(transform.forward, to) <= viewAngle * 0.5f;
            return inCone && HasLineTo(player.position + Vector3.up * 0.4f);
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

        /// <summary>Zombi normal de pie, vivo y libre (no jefes ni reptantes): admite derribos, aturdimientos y agarres.</summary>
        bool CanSpecial => !health.IsDead && !dormant && string.IsNullOrEmpty(bossName) && !Suspended && !Busy && !LowPose;

        /// <summary>Le ha dado un escopetazo (lo llama el arma una vez por disparo). De cerca cae al suelo; de lejos, a veces se queda aturdido.</summary>
        public void ShotgunBlast(float distance)
        {
            if (!CanSpecial) return;
            if (distance <= knockdownRange) StartCoroutine(Special(true));
            else if (Random.value < stunChance) StartCoroutine(Special(false));
        }

        /// <summary>Lo tira al suelo (escopetazo a quemarropa o al soltarse el jugador de un agarre).</summary>
        public void Knockdown() { if (!health.IsDead && !knocked) StartCoroutine(Special(true)); }

        System.Collections.IEnumerator Special(bool knockdown)
        {
            if (knockdown) knocked = true; else stunned = true;
            if (pendingHit != null) { StopCoroutine(pendingHit); pendingHit = null; }       // el golpe que iba a dar se pierde
            Suspended = true;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            Reacted?.Invoke(knockdown ? "Knockdown" : "Stun");
            yield return new WaitForSeconds(knockdown ? knockdownTime : stunTime);
            knocked = false; stunned = false;
            if (health.IsDead) yield break;
            Suspended = false;
            staggerUntil = 0f;
            nextAttack = Time.time + 0.6f;
            StartChase(false);
        }

        /// <summary>Agarre: lo empieza y lo termina PlayerActions (el zombi se queda pegado mordiendo).</summary>
        public void BeginGrab(Vector3 at, Vector3 lookAt)
        {
            Grabbing = true;
            Suspended = true;
            if (pendingHit != null) { StopCoroutine(pendingHit); pendingHit = null; }
            if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.Warp(at); } else transform.position = at;
            Vector3 d = Vector3.ProjectOnPlane(lookAt - at, Vector3.up);
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
            Reacted?.Invoke("Grab");
        }

        /// <summary>Fin del agarre: si el jugador se solto de un empujon, cae al suelo; si no, sigue atacando tras una pausa.</summary>
        public void EndGrab(bool shoved)
        {
            if (!Grabbing) return;
            Grabbing = false;
            Reacted?.Invoke("GrabEnd");
            if (health.IsDead) return;
            Suspended = false;
            nextAttack = Time.time + 2f;
            if (shoved) Knockdown();
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
            if (!HasLineTo(player.position + Vector3.up * 0.4f)) yield break;                              // una pared o una puerta en medio: no hay golpe
            playerHealth.TakeDamage(damage, transform.position);
        }

        /// <summary>Golpea una puerta cerrada que le corta el paso (misma animacion y sonido que el ataque).</summary>
        public void BashDoor() => Attacked?.Invoke();

        /// <summary>Un ruido fuerte (disparo) alerta a los zombis del radio que tengan linea directa con el ruido; a traves de paredes
        /// solo a los que esten muy cerca (35 % del radio).</summary>
        public static void Noise(Vector3 position, float radius)
        {
            foreach (var z in All)
            {
                if (z == null || z.chasing) continue;
                if (!string.IsNullOrEmpty(z.bossName)) continue;                                   // al jefe solo lo despierta su sala (BossRoomTrigger)
                float d = Vector3.Distance(z.transform.position, position);
                if (Mathf.Abs(z.transform.position.y - position.y) > 2.5f && !z.HasLineTo(position)) continue;   // otra planta y sin linea directa: no se oye
                if (d > radius || (d > radius * 0.35f && !z.HasLineTo(position))) continue;
                if (z.dormant) { if (!z.wakeOnlyWhenShot) z.Wake(); }
                else z.StartChase(true);
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
