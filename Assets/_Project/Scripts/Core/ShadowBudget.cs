using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Limita cuantas luces proyectan sombra a la vez: solo las N mas cercanas al jugador la conservan.
    /// Se crea sola al cargar la escena. Solo toca Light.shadows (no intensidad ni color), asi que no choca
    /// con el parpadeo ni los interruptores de CeilingLamp. Ver docs/sombras.md.
    /// </summary>
    public class ShadowBudget : MonoBehaviour
    {
        [Tooltip("Maximo de luces con sombra a la vez")]
        public int maxShadowed = 8;
        [Tooltip("Una luz con sombra solo la cede si otra le gana por mas de esta distancia (m)")]
        public float hysteresis = 1f;
        public float interval = 0.2f;

        class Entry
        {
            public Light light;
            public LightShadows original;
            public bool shadowed;
            public float dist;
        }

        readonly List<Entry> entries = new List<Entry>();
        Transform player;
        float next;

        public int ShadowedCount { get; private set; }

        public static ShadowBudget Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Create();
        }

        // El menu y la escena de juego son escenas distintas: se recrea en cada carga para recoger sus luces
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Create();

        static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("ShadowBudget");
            go.AddComponent<ShadowBudget>();
        }

        void Awake()
        {
            Instance = this;
            var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional || l.shadows == LightShadows.None) continue;
                entries.Add(new Entry { light = l, original = l.shadows, shadowed = true });
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var e in entries) if (e.light != null) e.light.shadows = e.original;
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;

            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }

            Vector3 pos = player.position;
            var live = new List<Entry>(entries.Count);
            foreach (var e in entries)
            {
                if (e.light == null) continue;
                e.dist = (e.light.transform.position - pos).sqrMagnitude;
                // Una luz apagada (parpadeo, interruptor) no ocupa un hueco de sombra
                if (!e.light.enabled || !e.light.gameObject.activeInHierarchy) { Set(e, false); continue; }
                live.Add(e);
            }

            // Las que ya tienen sombra compiten con ventaja: su distancia se reduce en la histeresis
            float h = hysteresis;
            live.Sort((a, b) => Key(a, h).CompareTo(Key(b, h)));

            int shown = 0;
            for (int i = 0; i < live.Count; i++)
            {
                bool want = i < maxShadowed;
                Set(live[i], want);
                if (want) shown++;
            }
            ShadowedCount = shown;
        }

        static float Key(Entry e, float h)
        {
            float d = Mathf.Sqrt(e.dist);
            return e.shadowed ? Mathf.Max(0f, d - h) : d;
        }

        static void Set(Entry e, bool on)
        {
            if (e.shadowed == on) return;
            e.shadowed = on;
            e.light.shadows = on ? e.original : LightShadows.None;
        }
    }
}
