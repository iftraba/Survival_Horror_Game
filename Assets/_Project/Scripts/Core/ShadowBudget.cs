using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Limita cuantas luces proyectan sombra a la vez. Las plazas se reparten en dos grupos, por distancia al jugador:
    ///  - A: luces que lo alcanzan con linea de vision (dan las sombras de los muebles de la sala donde esta);
    ///  - B: luces que lo alcanzan pero con una pared o un suelo en medio (si no tuvieran sombra, su luz atravesaria la pared).
    /// Si un grupo no llena sus plazas, las sobrantes pasan al otro y despues a las luces que no lo alcanzan.
    /// Se crea sola al cargar la escena. Solo toca Light.shadows (no intensidad ni color), asi que no choca
    /// con el parpadeo ni los interruptores de CeilingLamp. Ver docs/sombras.md.
    /// </summary>
    public class ShadowBudget : MonoBehaviour
    {
        [Tooltip("Maximo de luces con sombra a la vez")]
        public int maxShadowed = 8;
        [Tooltip("Plazas para luces con linea de vision al jugador (el resto, hasta maxShadowed, para las que tienen una pared en medio)")]
        public int visibleSlots = 4;
        [Tooltip("Una luz con sombra solo la cede si otra le gana por mas de esta distancia (m)")]
        public float hysteresis = 1f;
        public float interval = 0.2f;
        [Tooltip("Desplazamiento vertical desde la posicion del jugador (su centro, ~1 m sobre el suelo) del punto con el que se mide si una luz lo alcanza y su linea de vision. Negativo = hacia el suelo: el cono de una lampara llega a mas sitio cuanto mas bajo, y es el suelo bajo el jugador lo que se ilumina a traves de las paredes")]
        public float probeOffset = -0.8f;
        [Tooltip("Segundo punto de prueba, a la altura de la cabeza (sobre la posicion del jugador): una luz cuenta si alcanza cualquiera de los dos")]
        public float headOffset = 0.5f;

        class Entry
        {
            public Light light;
            public LightShadows original;
            public bool shadowed;
            public float dist;
            public float key;
            public int group;   // 0 = A (con vision), 1 = B (con pared), 2 = no lo alcanza
        }

        static readonly RaycastHit[] hitBuffer = new RaycastHit[8];

        readonly List<Entry> entries = new List<Entry>();
        readonly List<Entry> ga = new List<Entry>(), gb = new List<Entry>(), gc = new List<Entry>(), pool = new List<Entry>();
        Transform player;
        float next;

        public int ShadowedCount { get; private set; }
        public int VisibleCount { get; private set; }
        public int BlockedCount { get; private set; }

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

            Vector3 eye = player.position + Vector3.up * probeOffset;     // suelo bajo el jugador
            Vector3 head = player.position + Vector3.up * headOffset;     // altura de la cabeza
            ga.Clear(); gb.Clear(); gc.Clear();
            foreach (var e in entries)
            {
                if (e.light == null) continue;
                // Una luz apagada (parpadeo, interruptor) no ocupa un hueco de sombra
                if (!e.light.enabled || !e.light.gameObject.activeInHierarchy) { Set(e, false); continue; }
                Vector3 from = e.light.transform.position;
                float d = Vector3.Distance(from, eye);
                e.dist = d;
                e.key = e.shadowed ? Mathf.Max(0f, d - hysteresis) : d;   // las que ya tienen sombra compiten con ventaja
                float dh = Vector3.Distance(from, head);
                bool reachF = Reaches(e.light, from, eye, d), reachH = Reaches(e.light, from, head, dh);
                if (!reachF && !reachH) { e.group = 2; gc.Add(e); continue; }
                bool blockedAny = (reachF && Blocked(from, eye, d)) || (reachH && Blocked(from, head, dh));
                if (blockedAny) { e.group = 1; gb.Add(e); } else { e.group = 0; ga.Add(e); }
            }
            ga.Sort(ByKey); gb.Sort(ByKey); gc.Sort(ByKey);

            int slotsA = Mathf.Clamp(visibleSlots, 0, maxShadowed);
            int slotsB = Mathf.Max(0, maxShadowed - slotsA);

            int chosen = 0;
            pool.Clear();
            Mark(ga, slotsA, ref chosen, pool);
            Mark(gb, slotsB, ref chosen, pool);
            // plazas sobrantes: primero las luces sobrantes de A y B (por distancia), despues las que no lo alcanzan
            pool.Sort(ByKey);
            int left = Mathf.Max(0, maxShadowed - chosen);
            for (int i = 0; i < pool.Count; i++) { bool want = i < left; Set(pool[i], want); if (want) chosen++; }
            int leftC = Mathf.Max(0, maxShadowed - chosen);
            for (int i = 0; i < gc.Count; i++) { bool want = i < leftC; Set(gc[i], want); if (want) chosen++; }

            ShadowedCount = chosen;
            VisibleCount = ga.Count;
            BlockedCount = gb.Count;
        }

        // Las `slots` primeras de la lista (ya ordenada) reciben sombra; el resto va al conjunto de sobrantes
        void Mark(List<Entry> list, int slots, ref int chosen, List<Entry> leftovers)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i < slots) { Set(list[i], true); chosen++; }
                else leftovers.Add(list[i]);   // se decide despues, para no tocar Light.shadows dos veces en la misma pasada
            }
        }

        static int ByKey(Entry a, Entry b) => a.key.CompareTo(b.key);

        static bool Reaches(Light l, Vector3 from, Vector3 to, float d)
        {
            if (d > l.range) return false;
            if (l.type == LightType.Spot && Vector3.Angle(l.transform.forward, to - from) > l.spotAngle * 0.5f) return false;
            return true;
        }

        // Hay algo entre la luz y los ojos que no sea el propio jugador, un enemigo ni un trigger
        static bool Blocked(Vector3 from, Vector3 to, float d)
        {
            Vector3 dir = (to - from) / Mathf.Max(d, 0.001f);
            float len = d - 0.6f;   // se queda antes de la capsula del jugador
            if (len <= 0.05f) return false;
            int n = Physics.RaycastNonAlloc(from + dir * 0.15f, dir, hitBuffer, len - 0.15f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hitBuffer[i].collider;
                if (c is CharacterController || c.GetComponentInParent<Health>() != null) continue;
                return true;
            }
            return false;
        }

        static void Set(Entry e, bool on)
        {
            if (e.shadowed == on) return;
            e.shadowed = on;
            e.light.shadows = on ? e.original : LightShadows.None;
        }
    }
}
