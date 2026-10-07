#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Rediseno de las arenas de los jefes para que no se pueda huir dando vueltas por una sala enorme.
    /// - Arena 1 (primer jefe, z 9-20): tres carriles de unos 3,5 m en zigzag (dos muros largos con paso alternado, una
    ///   sola ruta de la puerta a la salida), columnas y cajas escalonadas. La puerta del jefe se atranca al empezar el
    ///   combate (BossRoomTrigger.sealDoors). Menu: Horror/Redisenar arena del primer jefe (idempotente: rehace ArenaMaze1_*).
    /// - Arena 2 (sala de calderas, z 66-85): bancos de maquinas en filas con pasillos de ~2,6 m (MachineBanks, lo llama
    ///   Zone2Wing). Entre filas se puede esquivar y rodear la caldera, pero no ir en linea recta ni correr en circulos sin apuros.
    /// </summary>
    public static class ArenaRework
    {
        const string Mats = "Assets/_Project/Materials/";
        static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;

        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, Priv).Invoke(null, args);
        static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(Mats + n + ".mat");

        static Material Emissive(string name, Color c)
        {
            string p = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c * 0.3f); m.SetColor("_EmissionColor", c * 2.2f); m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Cyl(string n, Transform parent, Vector3 pos, Vector3 size, bool col, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = n; go.transform.SetParent(parent); go.transform.position = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!col) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // ------------------------------------------------------------------ bancos de maquinas (arena 2)
        /// <summary>Un banco de maquinas: cuerpo macizo (colision), tapa, tuberias/depositos encima y luces de estado a los dos lados.</summary>
        public static void Machine(Transform parent, string name, float cx, float cz, float sx, float sz, float h, Material metal, Material wall, Material red, Material green)
        {
            Call<GameObject>("Box", name, new Vector3(cx, h * 0.5f, cz), new Vector3(sx, h, sz), metal, parent, 1.5f, true);
            Call<GameObject>("Box", name + "_Cap", new Vector3(cx, h + 0.08f, cz), new Vector3(sx + 0.12f, 0.16f, sz + 0.12f), wall, parent, 1.5f, false);
            // depositos y tuberias sobre el banco (sin colision: van por encima de la cabeza)
            int tanks = Mathf.Max(1, Mathf.RoundToInt(sx / 3.2f));
            for (int i = 0; i < tanks; i++)
            {
                float tx = cx - sx * 0.5f + (i + 0.5f) * sx / tanks;
                float th = 0.7f + ((i * 37 + (int)(cx * 3f)) % 5) * 0.12f;
                Cyl(name + "_Tank", parent, new Vector3(tx, h + 0.16f + th * 0.5f, cz), new Vector3(Mathf.Min(sz * 0.8f, 1.1f), th * 0.5f, Mathf.Min(sz * 0.8f, 1.1f)), false, metal);
            }
            // luces de estado a ambos lados (hacia los pasillos)
            int lights = Mathf.Max(2, Mathf.RoundToInt(sx / 1.8f));
            for (int i = 0; i < lights; i++)
            {
                float lx = cx - sx * 0.5f + (i + 0.5f) * sx / lights;
                var mat = (i + (int)(cz)) % 3 == 0 ? red : green;
                foreach (float side in new[] { -1f, 1f })
                    Call<GameObject>("Box", name + "_Led", new Vector3(lx, 1.45f, cz + side * (sz * 0.5f + 0.012f)), new Vector3(0.16f, 0.1f, 0.03f), mat, parent, 1f, false);
            }
            // rendijas de ventilacion oscuras en la cara: un par de tiras por cada lado
            foreach (float side in new[] { -1f, 1f })
                Call<GameObject>("Box", name + "_Vent", new Vector3(cx, 0.55f, cz + side * (sz * 0.5f + 0.01f)), new Vector3(sx * 0.7f, 0.45f, 0.02f), wall, parent, 1f, false);
        }

        /// <summary>Filas de maquinas de la sala de calderas (z 66-85). Devuelve la lista de bancos creados.</summary>
        public static void MachineBanks(Transform parent)
        {
            var metal = M("Env_Metal"); var wall = M("Env_Wall");
            var red = Emissive("Machine_LedRed", new Color(1f, 0.15f, 0.08f)); var green = Emissive("Machine_LedGreen", new Color(0.2f, 1f, 0.35f));
            // fila 1 (entrada): deja un pasillo central de ~5 m
            Machine(parent, "Bank_1W", -8.5f, 70.1f, 11.8f, 1.8f, 2.4f, metal, wall, red, green);
            Machine(parent, "Bank_1E", 8.5f, 70.1f, 11.8f, 1.8f, 2.4f, metal, wall, red, green);
            // fila 2: alrededor de la caldera quedan dos pasos de ~2,5 m
            Machine(parent, "Bank_2W", -9.7f, 74.5f, 9.6f, 1.8f, 2.7f, metal, wall, red, green);
            Machine(parent, "Bank_2E", 9.7f, 74.5f, 9.6f, 1.8f, 2.7f, metal, wall, red, green);
            // fila 3: tras la caldera; detras queda el ultimo carril hacia el porton
            Machine(parent, "Bank_3W", -9.7f, 78.9f, 9.6f, 1.8f, 2.4f, metal, wall, red, green);
            Machine(parent, "Bank_3E", 9.7f, 78.9f, 9.6f, 1.8f, 2.4f, metal, wall, red, green);
            // columnas en los pasillos: obligan a zigzaguear y paran las embestidas
            foreach (var c in new[] { new Vector2(-9.7f, 72.3f), new Vector2(9.7f, 72.3f), new Vector2(-9.7f, 76.7f), new Vector2(9.7f, 76.7f) })
                Cyl("Pillar", parent, new Vector3(c.x, 2.75f, c.y), new Vector3(1.0f, 2.75f, 1.0f), true, wall);
        }

        // ------------------------------------------------------------------ arena 1
        [MenuItem("Horror/Redisenar arena del primer jefe")]
        public static void Arena1Menu() { Debug.Log("[Horror] " + Arena1()); }

        public static string Arena1()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var details = GameObject.Find("Details").transform;
            var wall = M("Env_Wall"); var wood = M("Env_Wood");

            foreach (var n in new[] { "ArenaMaze1_Solid", "ArenaMaze1_Props" }) { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }
            // props viejos de la arena (cajas y barriles sueltos que quedarian dentro de los muros nuevos)
            int removed = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (t == null || t.parent == null || t.parent.name != "BossWing_Props") continue;
                if (!(t.name.StartsWith("Crate") || t.name.StartsWith("Barrel"))) continue;
                if (t.position.z < 9.4f || t.position.z > 20f) continue;
                Object.DestroyImmediate(t.gameObject); removed++;
            }
            // las tres lamparas antiguas de la arena (z = 12,5) quedarian sobre el muro nuevo
            foreach (var l in Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Where(l => l.transform.position.z > 12f && l.transform.position.z < 13.1f && Mathf.Abs(l.transform.position.x) < 14f).ToArray())
            { Object.DestroyImmediate(l.gameObject); removed++; }
            log.Add("retirados " + removed + " objetos viejos");

            var solid = new GameObject("ArenaMaze1_Solid").transform; solid.SetParent(level);
            var props = new GameObject("ArenaMaze1_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;

            void Wall(string n, float x0, float z0, float x1, float z1) => Call<object>("Partition", solid, details, wall, wood, n, new Vector2(x0, z0), new Vector2(x1, z1));
            // zigzag: de la puerta (x 0, z 9) hacia el este por el carril 1, vuelta al oeste por el 2 y al este otra vez por el 3 hasta la salida (x 0, z 20)
            Wall("Maze_A", -14.5f, 12.5f, 10.5f, 12.5f);   // paso al este (x 10,5-14,5)
            Wall("Maze_B", -10.5f, 16.0f, 14.5f, 16.0f);   // paso al oeste (x -14,5--10,5)
            GameObject Prop(string n, float x, float z, float yaw) => Call<GameObject>("Prop", n, new Vector3(x, 0f, z), yaw, props, 1f);
            // Un solo obstaculo por seccion, alternando lados, para dejar siempre >= 2 m de paso (carriles de 3,5 m; el NavMesh resta ~0,45 m a cada lado)
            // carril 1 (z 9-12,5): un grupo pegado al muro sur y otro pegado al muro del zigzag
            Prop("Crate", -9.0f, 10.0f, 15f); Prop("Barrel", -7.9f, 9.9f, 0f); Prop("Crate", 6.5f, 11.8f, -20f); Prop("Barrel", 7.6f, 11.9f, 0f);
            // carril 2 (z 12,5-16): columnas en x +-5,5 (ya existen) y un obstaculo en cada extremo
            Prop("Crate", 8.2f, 15.0f, -15f); Prop("Barrel", -11.5f, 13.2f, 0f); Prop("Crate", -8.6f, 15.2f, 25f);
            // carril 3 (z 16-20): sin cajas (los escritorios que ya hay pegados al muro norte lo estrechan bastante)

            // luces tenues como el resto de la arena
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).FirstOrDefault(l => l.transform.position.z > 16f && l.transform.position.z < 17.5f && l.transform.position.y < 3.4f);
            if (lampSrc != null)
            {
                foreach (var xz in new[] { new Vector2(-7f, 10.8f), new Vector2(7f, 10.8f), new Vector2(-6f, 14.3f), new Vector2(6f, 14.3f) })
                {
                    var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                    go.name = "Lamp";
                    go.transform.position = new Vector3(xz.x, lampSrc.transform.position.y, xz.y);
                    foreach (var f in go.GetComponentsInChildren<Light>(true))
                        if (f != go.GetComponent<Light>()) { var p = f.transform.position; f.transform.position = new Vector3(xz.x, p.y, xz.y); }
                }
                log.Add("4 lamparas nuevas");
            }
            else log.Add("AVISO: no se encontro una lampara de referencia");

            // jefe en el carril 2, mirando al este; la puerta de la sala se atranca al empezar el combate
            var boss = GameObject.Find("Boss");
            if (boss != null) { boss.transform.SetPositionAndRotation(new Vector3(-2f, 1.4f, 14.2f), Quaternion.Euler(0f, 90f, 0f)); EditorUtility.SetDirty(boss); }
            var trig = GameObject.Find("BossRoomTrigger");
            var door = GameObject.Find("Door_Boss");
            if (trig != null && door != null)
            {
                var brt = trig.GetComponent<BossRoomTrigger>();
                brt.sealDoors = new[] { door.GetComponentInChildren<Door>() };
                EditorUtility.SetDirty(brt);
                log.Add("puerta del jefe atrancable");
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }
    }
}
#endif
