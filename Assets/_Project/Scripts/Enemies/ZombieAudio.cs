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
        float nextGroan;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();

            // Cada zombi suena distinto; el mecanico grave y la paciente aguda
            voicePitch = Random.Range(0.85f, 1.15f);
            if (name.Contains("Mecanico")) voicePitch = Random.Range(0.72f, 0.82f);
            else if (name.Contains("Paciente")) voicePitch = Random.Range(1.2f, 1.3f);

            nextGroan = Time.time + Random.Range(1f, 6f);
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
