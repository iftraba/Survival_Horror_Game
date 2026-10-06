using UnityEngine;

namespace Horror
{
    /// <summary>Voz del zombi: gruñidos (mas frecuentes si te persigue), ataque, quejido y muerte, en 3D.</summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieAudio : MonoBehaviour
    {
        ZombieAI ai;
        Health health;
        Transform player;
        float voicePitch;
        float nextGroan, nextStep;
        UnityEngine.AI.NavMeshAgent agent;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();

            // Cada zombi suena distinto; el mecanico grave y la paciente aguda
            voicePitch = Random.Range(0.85f, 1.15f);
            if (name.Contains("Mecanico")) voicePitch = Random.Range(0.72f, 0.82f);
            else if (name.Contains("Paciente")) voicePitch = Random.Range(1.2f, 1.3f);

            if (!string.IsNullOrEmpty(ai.bossName)) voicePitch = Random.Range(0.62f, 0.7f);   // el jefe: voz de gigante
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            nextGroan = Time.time + Random.Range(1f, 6f);
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

        void OnAlerted()
        {
            if (health.IsDead) return;
            if (!string.IsNullOrEmpty(ai.bossName)) GameAudio.Play(Sfx.BossRoar, transform.position, 1f, 1f, false);   // el rugido se oye en toda la sala
            else GameAudio.Play(Sfx.ZombieGroan, transform.position, 1f, voicePitch * 0.92f);
        }

        void Update()
        {
            // pasos pesados del jefe mientras se desplaza
            if (!string.IsNullOrEmpty(ai.bossName) && !health.IsDead && agent != null && agent.enabled && agent.velocity.magnitude > 0.5f && Time.time >= nextStep)
            {
                nextStep = Time.time + Mathf.Clamp(1.1f / agent.velocity.magnitude, 0.4f, 0.9f);
                GameAudio.Play(Sfx.BossStep, transform.position, 1f, Random.Range(0.92f, 1.08f));
            }
            if (health.IsDead || Time.time < nextGroan) return;
            nextGroan = Time.time + (ai.IsChasing ? Random.Range(2.5f, 5f) : Random.Range(6f, 14f));

            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }
            if (player != null && (player.position - transform.position).sqrMagnitude < 32f * 32f)
                GameAudio.Play(Sfx.ZombieGroan, transform.position, 1f, voicePitch);
        }

        void OnAttacked() => GameAudio.Play(Sfx.ZombieAttack, transform.position, 1f, voicePitch);

        void OnDamaged(Vector3 _)
        {
            if (!health.IsDead) GameAudio.Play(Sfx.ZombieHurt, transform.position, 1f, voicePitch);
        }

        void OnDied() => GameAudio.Play(Sfx.ZombieDeath, transform.position, 1f, voicePitch);
    }
}
