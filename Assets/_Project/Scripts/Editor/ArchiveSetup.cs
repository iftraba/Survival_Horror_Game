using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Monta el puzzle de la rinonera: dos notas (una de historia, una pista con el codigo), la taquilla con codigo de la
    /// sala de reuniones (planta alta) y la rinonera dentro. Se puede repetir sin duplicar.
    /// </summary>
    public static class ArchiveSetup
    {
        const float Y0 = 4f;
        const string NotesDir = "Assets/_Project/Data/Notes";
        public const string TaquillaCode = "4719";

        const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;
        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, Priv).Invoke(null, args);

        [MenuItem("Horror/Notas y taquilla con codigo")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static NoteData Note(string id, string title, NoteCategory cat, string body, string highlight, string objective)
        {
            if (!AssetDatabase.IsValidFolder(NotesDir)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Notes");
            string path = NotesDir + "/" + id + ".asset";
            var n = AssetDatabase.LoadAssetAtPath<NoteData>(path);
            if (n == null) { n = ScriptableObject.CreateInstance<NoteData>(); AssetDatabase.CreateAsset(n, path); }
            n.id = id; n.title = title; n.category = cat; n.body = body; n.highlight = highlight; n.objective = objective;
            EditorUtility.SetDirty(n);
            return n;
        }

        /// <summary>Taquilla bloqueada con teclado numerico: panel con luz verde en la puerta (gira con ella). La riñonera u otro botin va dentro.</summary>
        public static GameObject MakeCodeLocker(Transform parent, Material metal, Vector3 pos, float yaw, string code)
        {
            var locker = Call<GameObject>("Locker", parent, metal, pos, yaw, false);
            locker.name = "Locker_Code";
            var ld = locker.GetComponent<LockerDoor>();
            ld.code = code;
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "KeypadPanel";
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            panel.transform.SetParent(ld.hinge, false);
            panel.transform.localPosition = new Vector3(0.62f, 1.25f, 0.035f);
            panel.transform.localScale = new Vector3(0.15f, 0.22f, 0.02f);
            panel.GetComponent<Renderer>().sharedMaterial = Call<Material>("Mat", "KeypadPanel", new Color(0.06f, 0.07f, 0.08f));
            var led = GameObject.CreatePrimitive(PrimitiveType.Cube);
            led.name = "KeypadLed";
            Object.DestroyImmediate(led.GetComponent<Collider>());
            led.transform.SetParent(ld.hinge, false);
            led.transform.localPosition = new Vector3(0.62f, 1.33f, 0.047f);
            led.transform.localScale = new Vector3(0.09f, 0.03f, 0.008f);
            led.GetComponent<Renderer>().sharedMaterial = Call<Material>("Mat", "KeypadLed", new Color(0.3f, 1f, 0.45f));
            var glow = new GameObject("KeypadGlow").AddComponent<Light>();
            glow.transform.SetParent(ld.hinge, false);
            glow.transform.localPosition = new Vector3(0.62f, 1.3f, 0.2f);
            glow.type = LightType.Point; glow.color = new Color(0.3f, 1f, 0.45f); glow.range = 1.4f; glow.intensity = 0.5f; glow.shadows = LightShadows.None;
            return locker;
        }

        public static string Build()
        {
            var log = new System.Collections.Generic.List<string>();
            var level = GameObject.Find("--- LEVEL ---");
            if (level == null) return "no hay --- LEVEL ---";

            // ---- notas (datos)
            var pista = Note("note_taquilla_reuniones", "Nota arrugada", NoteCategory.Puzzle,
                "Taquilla del fondo de la sala de reuniones.\nLa cerré con candado de números y, claro, ya no me acordaba.\nPor si se me olvida otra vez:",
                "4 7 1 9",
                "Hay una taquilla con código en la sala de reuniones, en la planta de arriba.");
            var parte = Note("note_parte_guardia", "Parte de guardia", NoteCategory.Story,
                "TURNO DE NOCHE. Tres detenidos en el calabozo desde las diez; ninguno quiere hablar.\n\nDesde medianoche se oyen ruidos abajo y el teléfono de la sala del fondo no deja de sonar. Nadie lo coge.\n\nEl capitán ha cerrado esa sala con llave y se ha ido sin decir palabra. Mañana vienen de la central.\n\nNo me fío de lo que hay ahí abajo.\n— R.",
                "", "");
            AssetDatabase.SaveAssets();

            // ---- registro en ItemDatabase
            var db = Object.FindFirstObjectByType<ItemDatabase>();
            if (db != null)
            {
                var list = (db.notes ?? new NoteData[0]).Where(n => n != null).ToList();
                foreach (var n in new[] { pista, parte }) if (!list.Contains(n)) list.Add(n);
                db.notes = list.ToArray();
                EditorUtility.SetDirty(db);
            }
            else log.Add("AVISO: no hay ItemDatabase");

            // ---- contenedor y limpieza previa (idempotente)
            var extras = level.transform.Find("UpperFloor_Extras");
            if (extras == null) { extras = new GameObject("UpperFloor_Extras").transform; extras.SetParent(level.transform); }
            foreach (var t in extras.Cast<Transform>().ToArray()) Object.DestroyImmediate(t.gameObject);
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(p => p.item != null && p.item.name == "I_Bag" && p.transform.position.y > 3.5f).ToArray())   // solo la de la planta alta (la del tramo final es de PostBossWing)
                Object.DestroyImmediate(pk.gameObject);

            Physics.SyncTransforms();

            // ---- papeles sobre las mesas
            var paper = Call<Material>("Mat", "NotePaper", new Color(0.86f, 0.8f, 0.64f));
            Vector3 Surface(float x, float z)
            {
                var hits = Physics.RaycastAll(new Vector3(x, Y0 + 2f, z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                return hits.Length > 0 ? hits[0].point : new Vector3(x, Y0 + 0.8f, z);
            }
            void Paper(string name, NoteData n, float x, float z, float yaw)
            {
                var p = Surface(x, z);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(extras);
                go.transform.SetPositionAndRotation(p + Vector3.up * 0.003f, Quaternion.Euler(0f, yaw, 0f));
                go.transform.localScale = new Vector3(0.21f, 0.004f, 0.297f);
                go.GetComponent<Renderer>().sharedMaterial = paper;
                var col = go.GetComponent<BoxCollider>();
                col.size = new Vector3(1.4f, 12f, 1.2f);   // area de interaccion algo mas grande que el papel
                go.AddComponent<ReadableNote>().note = n;
                log.Add(name + " en " + go.transform.position.ToString("F2"));
            }
            Paper("Note_Taquilla", pista, -8.1f, -9.3f, 25f);       // mesa del archivo
            Paper("Note_ParteGuardia", parte, 1.45f, -10.4f, -12f);  // mesa del interrogatorio

            // ---- taquilla con codigo en la sala de reuniones (pared este)
            var metal = AssetDatabase.FindAssets("Env_Metal t:Material").Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
            var locker = MakeCodeLocker(extras, metal, new Vector3(14.25f, Y0, -1.2f), -90f, TaquillaCode);
            log.Add("taquilla con codigo en " + locker.transform.position.ToString("F2"));

            // ---- la rinonera dentro (en la balda), asentada con fisica
            var bagItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/I_Bag.asset");
            var items = GameObject.Find("Items");
            if (bagItem != null)
            {
                var pk = Pickup.Spawn(bagItem, 1, locker.transform.TransformPoint(new Vector3(0f, 1.42f, 0.02f)));
                if (items != null) pk.transform.SetParent(items.transform);
                var old = Physics.simulationMode;
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
                Physics.simulationMode = old;
                log.Add("rinonera en " + pk.transform.position.ToString("F2"));
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
