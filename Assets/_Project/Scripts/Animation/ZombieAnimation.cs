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
        /// <summary>Velocidad (m/s) forzada para la animacion; negativa = usar la del agente.</summary>
        [System.NonSerialized] public float speedOverride = -1f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");
        static readonly int AlertId = Animator.StringToHash("Alert");
        static readonly int HeadHitId = Animator.StringToHash("HeadHit");
        static readonly int StunId = Animator.StringToHash("Stun");
        static readonly int KnockdownId = Animator.StringToHash("Knockdown");
        static readonly int GrabId = Animator.StringToHash("Grab");
        static readonly int DeathVariantId = Animator.StringToHash("DeathVariant");

        ZombieAI ai;
        ZombieHitZones zones;
        NavMeshAgent agent;
        Health health;
        float playback = 1f;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            zones = GetComponent<ZombieHitZones>();
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
            ai.Reacted += OnReacted;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            ai.Attacked -= OnAttacked;
            ai.Alerted -= OnAlerted;
            ai.Reacted -= OnReacted;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (animator == null || agent == null || !agent.enabled) return;
            if (ai.Busy) { animator.speed = playback = 1f; return; }      // derribado, aturdido o mordiendo: las animaciones a su velocidad
            // los ataques especiales del jefe lo mueven con agent.Move, que no actualiza agent.velocity: ellos fijan la velocidad aqui
            float v = speedOverride >= 0f ? speedOverride : agent.velocity.magnitude;
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

        // ------------------------------------------------------------------ reptantes: que no se hundan en el suelo
        // Los clips de arrastrarse de Mixamo dejan el cuerpo por debajo de la raiz (hasta 30 cm, la cabeza llega a meterse en el
        // suelo y no se le puede dar). Tras animar, si algun hueso queda bajo el suelo se eleva el modelo lo justo.
        static readonly HumanBodyBones[] GroundBones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head, HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
        };
        Transform[] groundBones;
        float lift;
        CapsuleCollider body;

        void LateUpdate()
        {
            if (animator == null || !animator.isHuman) return;
            if (!ai.LowPose || health.IsDead) { if (lift > 0f && !health.IsDead) SetLift(0f); return; }
            if (groundBones == null)
            {
                groundBones = new Transform[GroundBones.Length];
                for (int i = 0; i < GroundBones.Length; i++) groundBones[i] = animator.GetBoneTransform(GroundBones[i]);
                body = GetComponent<CapsuleCollider>();
            }
            float sc = animator.transform.lossyScale.y;
            float ground = transform.position.y + (body != null ? body.center.y - body.height * 0.5f : 0f);
            float min = float.MaxValue;
            for (int i = 0; i < groundBones.Length; i++)
            {
                if (groundBones[i] == null) continue;
                float r = (GroundBones[i] == HumanBodyBones.Head ? 0.11f : 0.06f) * sc;      // grosor aproximado alrededor del hueso
                min = Mathf.Min(min, groundBones[i].position.y - r);
            }
            if (min == float.MaxValue) return;
            float need = Mathf.Max(0f, ground + 0.01f - (min - lift));
            // sube rapido (que no se vea hundido) y baja despacio (sin temblar con cada braceo)
            SetLift(Mathf.MoveTowards(lift, need, (need > lift ? 2.5f : 0.4f) * Time.deltaTime));
        }

        void SetLift(float v)
        {
            var m = animator.transform;
            m.localPosition += Vector3.up * (v - lift);
            lift = v;
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

        // parametros que tiene el controlador (el del jefe no tiene las reacciones nuevas)
        readonly System.Collections.Generic.HashSet<int> has = new System.Collections.Generic.HashSet<int>();
        RuntimeAnimatorController hasFor;
        bool Has(int id)
        {
            if (animator == null) return false;
            if (hasFor != animator.runtimeAnimatorController)
            {
                hasFor = animator.runtimeAnimatorController;
                has.Clear();
                foreach (var p in animator.parameters) has.Add(p.nameHash);
            }
            return has.Contains(id);
        }

        void OnDamaged(Vector3 _)
        {
            if (animator == null || ai.Busy) return;                     // en el suelo o aturdido no se interrumpe su animacion
            // disparo en la cabeza estando de pie: reaccion de cabeza; si no, la de golpe normal
            if (zones != null && zones.LastPart == ZombieHitZones.Part.Head && !ai.LowPose && Has(HeadHitId)) animator.SetTrigger(HeadHitId);
            else animator.SetTrigger(HitId);
        }

        void OnReacted(string what)
        {
            if (animator == null) return;
            animator.speed = playback = 1f;
            animator.ResetTrigger(HitId); animator.ResetTrigger(AttackId);           // el golpe del mismo disparo no debe cortar la reaccion
            if (Has(HeadHitId)) animator.ResetTrigger(HeadHitId);
            if (what == "Knockdown" && Has(KnockdownId)) animator.SetTrigger(KnockdownId);
            else if (what == "Stun" && Has(StunId)) animator.SetTrigger(StunId);
            else if (what == "Grab" && Has(GrabId)) animator.SetBool(GrabId, true);
            else if (what == "GrabEnd" && Has(GrabId)) animator.SetBool(GrabId, false);
        }

        void OnDied()
        {
            if (animator == null) return;
            animator.speed = 1f;
            if (Has(GrabId)) animator.SetBool(GrabId, false);
            // muerto en el suelo tras un escopetazo: se queda tumbado (sin animacion de muerte de pie)
            if (ai.KnockedDown && animator.HasState(0, DownedId)) { animator.CrossFade(DownedId, 0.25f, 0, 0.99f); return; }
            if (Has(DeathVariantId)) animator.SetInteger(DeathVariantId, PickDeath());
            animator.SetBool(DeadId, true);
        }

        static readonly int DownedId = Animator.StringToHash("Downed");

        /// <summary>
        /// Muerte segun donde entro el ultimo disparo: en el torso/vientre se dobla y cae (1); en otro sitio, al azar entre la suya (0),
        /// desplomarse (2) y tambalearse hasta caer (3). Los reptantes, la suya.
        /// </summary>
        int PickDeath()
        {
            if (ai.LowPose || zones == null) return 0;
            if (zones.LastPart == ZombieHitZones.Part.Torso) return 1;
            int[] pool = { 0, 0, 2, 3 };
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
