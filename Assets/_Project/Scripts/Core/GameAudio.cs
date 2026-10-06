using UnityEngine;

namespace Horror
{
    public enum Sfx
    {
        Footstep, DryFire,
        ZombieGroan, ZombieAttack, ZombieHurt, ZombieDeath,
        PlayerHurt, PlayerDeath,
        DoorOpen, DoorClose, DoorLocked, DoorUnlock,
        Pickup, Flashlight, Switch, LampZap,
        StairStep, PhoneRing, PhoneDial, PhonePickup, BossRoar, BossStep,
    }

    /// <summary>
    /// Gestor de audio: musica de ambiente, capa de tension cuando te persiguen, sustos lejanos
    /// aleatorios y un pool de fuentes para efectos 2D/3D. Las llamadas son seguras si no hay gestor.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Header("Musica")]
        public AudioClip ambientLoop;
        public AudioClip tensionLoop;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        [Header("Efectos")]
        public AudioClip[] footsteps;
        public AudioClip[] zombieGroans;
        public AudioClip[] zombieHurts;
        public AudioClip dryFire, zombieAttack, zombieDeath, playerHurt, playerDeath;
        public AudioClip doorOpen, doorClose, doorLocked, doorUnlock;
        public AudioClip pickup, flashlight, lightSwitch, lampZap;
        public AudioClip lampHum, heartbeat;
        [Header("Telefono, escalera y jefe")]
        public AudioClip[] stairSteps;
        public AudioClip phoneRing, phoneDial, phonePickup, bossRoar, bossStep;

        [Header("Sustos lejanos")]
        public AudioClip[] stingers;
        public Vector2 stingerInterval = new Vector2(18f, 40f);

        AudioSource ambient, tension;
        AudioSource[] pool;
        int nextSource;
        float tensionLevel, nextStinger;
        Transform player;

        void Awake()
        {
            Instance = this;

            ambient = MakeMusicSource(ambientLoop);
            tension = MakeMusicSource(tensionLoop);
            tension.volume = 0f;

            pool = new AudioSource[24];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("SfxSource" + i);
                go.transform.SetParent(transform);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = 2f;
                s.maxDistance = 35f;
                pool[i] = s;
            }
            ScheduleStinger();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource MakeMusicSource(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.spatialBlend = 0f;
            s.playOnAwake = false;
            s.volume = musicVolume;
            if (clip != null) s.Play();
            return s;
        }

        void Update()
        {
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }

            // Tension: sube rapido cuando algun zombi te persigue cerca, baja despacio al perderlos
            float target = 0f;
            if (player != null && !GameState.PlayerDead)
            {
                foreach (var z in ZombieAI.All)
                {
                    if (z.IsChasing && (z.transform.position - player.position).sqrMagnitude < 20f * 20f)
                    {
                        target = 1f;
                        break;
                    }
                }
            }
            tensionLevel = Mathf.MoveTowards(tensionLevel, target, Time.unscaledDeltaTime * (target > tensionLevel ? 0.45f : 0.12f));

            float master = GameState.PlayerDead ? 0.25f : 1f;
            ambient.volume = Mathf.MoveTowards(ambient.volume, musicVolume * master * Mathf.Lerp(1f, 0.4f, tensionLevel), Time.unscaledDeltaTime * 0.5f);
            tension.volume = Mathf.MoveTowards(tension.volume, musicVolume * master * 0.9f * tensionLevel, Time.unscaledDeltaTime * 0.5f);

            if (stingers != null && stingers.Length > 0 && player != null && !GameState.InputBlocked && Time.time >= nextStinger)
            {
                // Un ruido lejano en una direccion aleatoria alrededor del jugador
                Vector2 dir = Random.insideUnitCircle.normalized;
                Vector3 pos = player.position + new Vector3(dir.x, 0f, dir.y) * Random.Range(14f, 24f);
                Emit(stingers[Random.Range(0, stingers.Length)], pos, Random.Range(0.6f, 0.9f), Random.Range(0.85f, 1.1f), true);
                ScheduleStinger();
            }
        }

        void ScheduleStinger() => nextStinger = Time.time + Random.Range(stingerInterval.x, stingerInterval.y);

        // ---- API estatica -------------------------------------------------------------------------

        /// <summary>Reproduce un efecto. spatial=false para sonidos del propio jugador (se oyen "en la cabeza").</summary>
        public static void Play(Sfx sfx, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (Instance == null) return;
            var clip = Instance.Resolve(sfx);
            if (clip != null) Instance.Emit(clip, position, volume, pitch, spatial);
        }

        public static void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (Instance == null || clip == null) return;
            Instance.Emit(clip, position, volume, pitch, spatial);
        }

        // ---- Internos -----------------------------------------------------------------------------

        void Emit(AudioClip clip, Vector3 position, float volume, float pitch, bool spatial)
        {
            var s = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;
            s.transform.position = position;
            s.spatialBlend = spatial ? 1f : 0f;
            s.pitch = pitch;
            s.volume = volume;
            s.clip = clip;
            s.Play();
        }

        static AudioClip Any(AudioClip[] clips) => clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;

        AudioClip Resolve(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Footstep: return Any(footsteps);
                case Sfx.DryFire: return dryFire;
                case Sfx.ZombieGroan: return Any(zombieGroans);
                case Sfx.ZombieAttack: return zombieAttack;
                case Sfx.ZombieHurt: return Any(zombieHurts);
                case Sfx.ZombieDeath: return zombieDeath;
                case Sfx.PlayerHurt: return playerHurt;
                case Sfx.PlayerDeath: return playerDeath;
                case Sfx.DoorOpen: return doorOpen;
                case Sfx.DoorClose: return doorClose;
                case Sfx.DoorLocked: return doorLocked;
                case Sfx.DoorUnlock: return doorUnlock;
                case Sfx.Pickup: return pickup;
                case Sfx.Flashlight: return flashlight;
                case Sfx.Switch: return lightSwitch;
                case Sfx.LampZap: return lampZap;
                case Sfx.StairStep: return Any(stairSteps);
                case Sfx.PhoneRing: return phoneRing;
                case Sfx.PhoneDial: return phoneDial;
                case Sfx.PhonePickup: return phonePickup;
                case Sfx.BossRoar: return bossRoar;
                case Sfx.BossStep: return bossStep;
                default: return null;
            }
        }
    }
}
