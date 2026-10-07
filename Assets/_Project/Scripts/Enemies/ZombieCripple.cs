using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Zombi al que se le pueden romper las piernas: tras 'legHitsToCripple' disparos en las piernas (un golpe por disparo,
    /// aunque la escopeta meta varios perdigones) cae hacia atras (animacion de caida) y sigue arrastrandose: otro
    /// controlador de animacion, cuerpo bajo (capsula y agente), mas lento y con mordiscos.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieCripple : MonoBehaviour
    {
        [Tooltip("Controlador (override) con las animaciones de arrastrarse")] public RuntimeAnimatorController crawlController;
        public int legHitsToCripple = 2;
        [Tooltip("Segundos de caida antes de empezar a arrastrarse")] public float fallTime = 1.0f;
        public float crawlHeight = 0.9f, crawlRadius = 0.45f;
        [Tooltip("Velocidad de persecucion arrastrandose (la global la multiplica)")] public float crawlChase = 1.0f;
        public float crawlAttackRange = 1.4f;
        [Tooltip("Velocidad (m/s, a escala 1) de las zancadas de los clips de arrastrarse")] public float crawlWalkClip = 0.5f, crawlRunClip = 1.6f;
        public ZombieAI.AttackVariant[] crawlVariants;

        public bool Crippled { get; private set; }

        ZombieAI ai;
        Health health;
        ZombieHitZones zones;
        Animator anim;
        CapsuleCollider cap;
        NavMeshAgent agent;
        ZombieAnimation za;
        int hits;
        float lastHit = -10f;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            zones = GetComponent<ZombieHitZones>();
            cap = GetComponent<CapsuleCollider>();
            agent = GetComponent<NavMeshAgent>();
            za = GetComponent<ZombieAnimation>();
            anim = GetComponentInChildren<Animator>();
        }

        void OnEnable() { if (zones != null) zones.LegHit += OnLegHit; }
        void OnDisable() { if (zones != null) zones.LegHit -= OnLegHit; }

        void OnLegHit()
        {
            if (Crippled || health.IsDead || Time.time - lastHit < 0.3f) return;   // un golpe por disparo
            lastHit = Time.time;
            if (++hits >= legHitsToCripple) StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            Crippled = true;
            ai.Suspended = true;
            if (anim != null) anim.SetBool("Dead", true);             // la animacion de muerte hace de caida
            yield return new WaitForSeconds(fallTime);
            if (health.IsDead) yield break;

            if (anim != null)
            {
                anim.SetBool("Dead", false);
                if (crawlController != null) anim.runtimeAnimatorController = crawlController;
                var m = anim.transform;
                m.localPosition = new Vector3(m.localPosition.x, -crawlHeight * 0.5f, m.localPosition.z);
            }
            cap.height = crawlHeight; cap.radius = crawlRadius;
            if (agent != null)
            {
                agent.height = crawlHeight; agent.radius = Mathf.Min(crawlRadius, 0.6f);
                agent.baseOffset = crawlHeight * 0.5f;
            }
            ai.chaseSpeed = crawlChase;
            ai.attackRange = crawlAttackRange;
            if (crawlVariants != null && crawlVariants.Length > 0) ai.attackVariants = crawlVariants;
            if (za != null && anim != null)
            {
                float sc = anim.transform.lossyScale.y;
                za.walkClipSpeed = crawlWalkClip * sc; za.runClipSpeed = crawlRunClip * sc; za.runAbove = 1.0f;
            }
            ai.Suspended = false;
        }
    }

    /// <summary>Deja el Animator en el ultimo fotograma de su clip (cadaveres: la caida de la animacion de muerte, ya tumbados).</summary>
    public class HoldLastFrame : MonoBehaviour
    {
        void Start()
        {
            var a = GetComponentInChildren<Animator>();
            if (a == null) return;
            a.Play(0, 0, 1f);
            a.Update(0f);
        }
    }
}
