using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Conecta el zombi con su Animator.
    /// Modo "gait" (animaciones de Mixamo): el parametro Speed del animador es la MARCHA (0 reposo, 1 andar, 2 correr),
    /// y la velocidad de reproduccion se ajusta a la velocidad real del agente para que los pies no patinen
    /// (el andar de un zombi dura mucho mas que el avance real: 0.33 m/s en el clip; se acelera).
    /// Modo antiguo: Speed = velocidad / strideSpeed.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieAnimation : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Velocidad (m/s) a la que avanza la zancada de su animacion de andar. Speed del Animator = velocidad / strideSpeed, asi 1 = andar sin patinar.")]
        public float strideSpeed = 1.4f;

        [Header("Modo marcha (Mixamo)")]
        public bool gaitMode;
        [Tooltip("Velocidad real (m/s) de la animacion de andar a velocidad normal")] public float walkClipSpeed = 0.33f;
        [Tooltip("Velocidad real (m/s) de la animacion de correr a velocidad normal")] public float runClipSpeed = 2.84f;
        [Tooltip("A partir de esta velocidad el zombi corre en lugar de andar")] public float runAbove = 1.6f;
        public float maxWalkPlayback = 3.2f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");
        static readonly int AlertId = Animator.StringToHash("Alert");

        ZombieAI ai;
        NavMeshAgent agent;
        Health health;
        float playback = 1f;

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
            ai.Alerted += OnAlerted;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            ai.Attacked -= OnAttacked;
            ai.Alerted -= OnAlerted;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (animator == null || agent == null || !agent.enabled) return;
            float v = agent.velocity.magnitude;
            if (!gaitMode)
            {
                animator.SetFloat(SpeedId, v / Mathf.Max(0.1f, strideSpeed), 0.1f, Time.deltaTime);
                return;
            }

            // marcha: 0 reposo, 1 andar, 2 correr (mezcla suave en los extremos)
            float gait;
            if (v < 0.12f) gait = v / 0.12f;
            else if (v < runAbove) gait = 1f;
            else gait = 1f + Mathf.Clamp01((v - runAbove) / 0.5f);
            animator.SetFloat(SpeedId, gait, 0.12f, Time.deltaTime);

            // velocidad de reproduccion: que el avance del clip coincida con el del agente
            float target = 1f;
            if (v >= 0.12f)
                // con el suelo de velocidad de GameFlow los que andan suben a ~1 m/s: el tope de reproduccion de andar se amplia a 3x
                target = v < runAbove ? Mathf.Clamp(v / walkClipSpeed, 0.6f, Mathf.Max(maxWalkPlayback, 3f)) : Mathf.Clamp(v / runClipSpeed, 0.6f, 1.6f);
            playback = Mathf.MoveTowards(playback, target, 4f * Time.deltaTime);
            animator.speed = playback;
        }

        static readonly int VariantId = Animator.StringToHash("AttackVariant");
        int hasVariantParam = -1;

        void OnAttacked()
        {
            if (animator == null) return;
            if (hasVariantParam < 0)
            {
                hasVariantParam = 0;
                foreach (var p in animator.parameters) if (p.nameHash == VariantId) hasVariantParam = 1;
            }
            if (hasVariantParam == 1) animator.SetInteger(VariantId, ai.LastAttackVariant);
            animator.SetTrigger(AttackId);
        }

        // Solo los controladores nuevos (Mixamo) tienen grito; en los antiguos se ignora sin avisos
        void OnAlerted()
        {
            if (animator == null) return;
            foreach (var p in animator.parameters) if (p.nameHash == AlertId) { animator.SetTrigger(AlertId); return; }
        }

        void OnDamaged(Vector3 _) => animator?.SetTrigger(HitId);

        void OnDied()
        {
            animator?.SetBool(DeadId, true);
            if (animator != null) animator.speed = 1f;
        }
    }
}
