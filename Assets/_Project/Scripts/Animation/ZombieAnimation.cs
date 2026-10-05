using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>Conecta el zombi con el Animator (ZombieAnimator.controller).</summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieAnimation : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Velocidad (m/s) a la que avanza la zancada de su animacion de andar. Speed del Animator = velocidad / strideSpeed, asi 1 = andar sin patinar.")]
        public float strideSpeed = 1.4f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");

        ZombieAI ai;
        NavMeshAgent agent;
        Health health;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        void Start()
        {
            // Cada zombi arranca su ciclo en un punto distinto: dos del mismo tipo no se mueven al unisono
            if (animator != null) animator.Play(0, 0, Random.value);
        }

        void OnEnable()
        {
            ai.Attacked += OnAttacked;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            ai.Attacked -= OnAttacked;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (animator == null || agent == null || !agent.enabled) return;
            animator.SetFloat(SpeedId, agent.velocity.magnitude / Mathf.Max(0.1f, strideSpeed), 0.1f, Time.deltaTime);
        }

        void OnAttacked() => animator?.SetTrigger(AttackId);
        void OnDamaged(Vector3 _) => animator?.SetTrigger(HitId);
        void OnDied() => animator?.SetBool(DeadId, true);
    }
}
