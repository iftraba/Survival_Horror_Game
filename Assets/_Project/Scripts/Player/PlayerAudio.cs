using UnityEngine;

namespace Horror
{
    /// <summary>Quejidos del jugador al recibir dano, muerte y latido que aparece con poca salud.</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerAudio : MonoBehaviour
    {
        [Range(0f, 1f)] public float heartbeatBelow = 0.4f;
        public float hurtCooldown = 0.35f;

        Health health;
        AudioSource heart;
        float nextHurt;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void Start()
        {
            var audio = GameAudio.Instance;
            if (audio != null && audio.heartbeat != null)
            {
                heart = gameObject.AddComponent<AudioSource>();
                heart.clip = audio.heartbeat;
                heart.loop = true;
                heart.spatialBlend = 0f;
                heart.volume = 0f;
                heart.Play();
            }
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(Vector3 _)
        {
            if (health.IsDead || Time.time < nextHurt) return;
            nextHurt = Time.time + hurtCooldown;
            GameAudio.Play(Sfx.PlayerHurt, transform.position, 1f, Random.Range(0.95f, 1.05f), false);
        }

        void OnDied() => GameAudio.Play(Sfx.PlayerDeath, transform.position, 1f, 1f, false);

        void Update()
        {
            if (heart == null) return;
            float ratio = health.maxHealth > 0f ? health.Current / health.maxHealth : 1f;
            float target = health.IsDead ? 0f : Mathf.Clamp01((heartbeatBelow - ratio) / heartbeatBelow);
            heart.volume = Mathf.MoveTowards(heart.volume, target * 0.9f, Time.unscaledDeltaTime * 0.8f);
            heart.pitch = Mathf.Lerp(1f, 1.35f, target);   // se acelera cuanto mas grave estas
        }
    }
}
