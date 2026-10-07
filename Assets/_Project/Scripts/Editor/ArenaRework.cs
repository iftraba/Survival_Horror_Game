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
    /// - Arena 1 (primer jefe, z 9-20): recinto recto con tres islas de estanterias (una larga y dos cortas) y estanterias bajas
    ///   junto al muro sur; 3-5 m entre todo (jefe y jugador se cruzan y se rodean las islas), sin callejones. La puerta del jefe se atranca al empezar el
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
        /// <summary>Una maquina: cuerpo macizo (colision), tapa, depositos encima y luces de estado y rejillas en las dos caras largas.</summary>
        public static void Machine(Transform parent, string name, float cx, float cz, float sx, float sz, float h, Material metal, Material wall, Material red, Material green)
        {
            Call<GameObject>("Box", name, new Vector3(cx, h * 0.5f, cz), new Vector3(sx, h, sz), metal, parent, 1.5f, true);
            Call<GameObject>("Box", name + "_Cap", new Vector3(cx, h + 0.08f, cz), new Vector3(sx + 0.12f, 0.16f, sz + 0.12f), wall, parent, 1.5f, false);
            bool alongX = sx >= sz;                                            // eje largo de la maquina
            float len = alongX ? sx : sz, thick = alongX ? sz : sx;
            int tanks = Mathf.Max(1, Mathf.RoundToInt(len / 3.2f));
            for (int i = 0; i < tanks; i++)
            {
                float off = -len * 0.5f + (i + 0.5f) * len / tanks;
                float th = 0.7f + ((i * 37 + (int)(cx * 3f + cz)) % 5) * 0.12f;
                float d = Mathf.Min(thick * 0.8f, 1.1f);
                Vector3 pos = alongX ? new Vector3(cx + off, h + 0.16f + th * 0.5f, cz) : new Vector3(cx, h + 0.16f + th * 0.5f, cz + off);
                Cyl(name + "_Tank", parent, pos, new Vector3(d, th * 0.5f, d), false, metal);
            }
            int lights = Mathf.Max(2, Mathf.RoundToInt(len / 1.8f));
            for (int i = 0; i < lights; i++)
            {
                float off = -len * 0.5f + (i + 0.5f) * len / lights;
                var mat = (i + (int)cz) % 3 == 0 ? red : green;
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 pos = alongX ? new Vector3(cx + off, 1.45f, cz + side * (sz * 0.5f + 0.012f)) : new Vector3(cx + side * (sx * 0.5f + 0.012f), 1.45f, cz + off);
                    Vector3 size = alongX ? new Vector3(0.16f, 0.1f, 0.03f) : new Vector3(0.03f, 0.1f, 0.16f);
                    Call<GameObject>("Box", name + "_Led", pos, size, mat, parent, 1f, false);
                }
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 pos = alongX ? new Vector3(cx, 0.55f, cz + side * (sz * 0.5f + 0.01f)) : new Vector3(cx + side * (sx * 0.5f + 0.01f), 0.55f, cz);
                Vector3 size = alongX ? new Vector3(sx * 0.7f, 0.45f, 0.02f) : new Vector3(0.02f, 0.45f, sz * 0.7f);
                Call<GameObject>("Box", name + "_Vent", pos, size, wall, parent, 1f, false);
            }
        }

        /// <summary>
        /// Sala de calderas (z 66-85): ISLAS de maquinas sueltas, de tamanos distintos y colocadas sin simetria, con la caldera
        /// descentrada. Ninguna toca un muro y entre todo queda >= 2 m: se puede rodear cada maquina por los cuatro lados (nunca
        /// un callejon sin salida donde el jefe te encierre) y no hay recta larga para correr.
        /// </summary>
        public static void MachineBanks(Transform parent)
        {
            var metal = M("Env_Metal"); var wall = M("Env_Wall");
            var red = Emissive("Machine_LedRed", new Color(1f, 0.15f, 0.08f)); var green = Emissive("Machine_LedGreen", new Color(0.2f, 1f, 0.35f));
            Machine(parent, "Mach_A", -1.0f, 70.8f, 3.4f, 1.8f, 2.4f, metal, wall, red, green);    // frente a la puerta: obliga a elegir lado
            Machine(parent, "Mach_B", -8.0f, 70.2f, 4.2f, 2.4f, 2.7f, metal, wall, red, green);
            Machine(parent, "Mach_C", 7.5f, 70.5f, 6.0f, 2.0f, 2.4f, metal, wall, red, green);
            Machine(parent, "Mach_D", -10.0f, 76.5f, 4.5f, 2.0f, 2.4f, metal, wall, red, green);
            Machine(parent, "Mach_E", 9.5f, 76.2f, 2.2f, 5.5f, 2.5f, metal, wall, red, green);     // larga en vertical
            Machine(parent, "Mach_F", 2.5f, 80.5f, 7.0f, 2.2f, 2.6f, metal, wall, red, green);
            Machine(parent, "Mach_G", -10.5f, 80.8f, 3.0f, 1.8f, 2.4f, metal, wall, red, green);
            // deposito cilindrico suelto y una columna estructural, fuera de eje
            Cyl("Tank_Island", parent, new Vector3(4.5f, 1.5f, 74.5f), new Vector3(2.0f, 1.5f, 2.0f), true, metal);
            Cyl("Tank_IslandCap", parent, new Vector3(4.5f, 3.05f, 74.5f), new Vector3(2.2f, 0.08f, 2.2f), false, wall);
            Cyl("Pillar", parent, new Vector3(-5.8f, 2.75f, 80.2f), new Vector3(1.0f, 2.75f, 1.0f), true, wall);
        }

        // ------------------------------------------------------------------ arena 1
        [MenuItem("Horror/Redisenar arena del primer jefe")]
        public static void Arena1Menu() { Debug.Log("[Horror] " + Arena1()); }

        /// <summary>
        /// Una estanteria industrial: lomo central, postes, baldas y carga de cajas a los dos lados. Solo lleva UN collider invisible
        /// del volumen completo (decoracion sin colision: son cientos de piezas). El eje largo es el de mayor medida.
        /// </summary>
        public static void Rack(Transform parent, string name, float cx, float cz, float sx, float sz, Material metal, Material wood, int seed)
        {
            bool alongX = sx >= sz;
            float len = alongX ? sx : sz, thick = alongX ? sz : sx;
            var body = Call<GameObject>("Box", name, new Vector3(cx, 1.15f, cz), new Vector3(sx, 2.3f, sz), metal, parent, 1.5f, true);
            body.GetComponent<Renderer>().enabled = false;                       // solo colision
            // lomo central
            Call<GameObject>("Box", name + "_Spine", new Vector3(cx, 1.15f, cz), alongX ? new Vector3(sx, 2.3f, 0.12f) : new Vector3(0.12f, 2.3f, sz), metal, parent, 1.5f, false);
            var rnd = new System.Random(seed);
            float half = thick * 0.5f;
            // postes
            int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.8f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float off = -len * 0.5f + i * len / (posts - 1);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 pos = alongX ? new Vector3(cx + off, 1.15f, cz + side * (half - 0.04f)) : new Vector3(cx + side * (half - 0.04f), 1.15f, cz + off);
                    Call<GameObject>("Box", name + "_Post", pos, new Vector3(0.07f, 2.3f, 0.07f), metal, parent, 1f, false);
                }
            }
            // baldas y carga
            float boardDepth = Mathf.Max(0.2f, half - 0.08f);
            foreach (float y in new[] { 0.3f, 0.95f, 1.6f, 2.25f })
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 c = alongX ? new Vector3(cx, y, cz + side * (0.06f + boardDepth * 0.5f)) : new Vector3(cx + side * (0.06f + boardDepth * 0.5f), y, cz);
                    Vector3 size = alongX ? new Vector3(len, 0.04f, boardDepth) : new Vector3(boardDepth, 0.04f, len);
                    Call<GameObject>("Box", name + "_Board", c, size, wood, parent, 1f, false);
                    if (y > 2.2f) continue;                                          // la de arriba va vacia
                    float t = 0.15f;
                    while (t < len - 0.3f)
                    {
                        float w = 0.3f + (float)rnd.NextDouble() * 0.45f, h = 0.2f + (float)rnd.NextDouble() * 0.28f, d = boardDepth * (0.55f + (float)rnd.NextDouble() * 0.4f);
                        if (rnd.NextDouble() < 0.7 && t + w < len)
                        {
                            float along = -len * 0.5f + t + w * 0.5f;
                            float face = side * (0.06f + d * 0.5f);
                            Vector3 p = alongX ? new Vector3(cx + along, y + 0.02f + h * 0.5f, cz + face) : new Vector3(cx + face, y + 0.02f + h * 0.5f, cz + along);
                            Vector3 s = alongX ? new Vector3(w, h, d) : new Vector3(d, h, w);
                            Call<GameObject>("Box", name + "_Cargo", p, s, rnd.NextDouble() < 0.5 ? wood : metal, parent, 1f, false);
                        }
                        t += w + 0.12f + (float)rnd.NextDouble() * 0.3f;
                    }
                }
        }

        public static string Arena1()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var wall = M("Env_Wall"); var wood = M("Env_Wood"); var metal = M("Env_Metal");

            foreach (var n in new[] { "ArenaMaze1_Solid", "ArenaMaze1_Props" }) { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }
            int removed = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (t == null) continue;
                // props viejos de la arena (cajas y barriles sueltos de BossWing) y las columnas de x +-5,5
                bool oldProp = t.parent != null && t.parent.name == "BossWing_Props" && (t.name.StartsWith("Crate") || t.name.StartsWith("Barrel")) && t.position.z > 9.4f && t.position.z < 20f;
                bool oldTrim = t.name.StartsWith("Maze_A") || t.name.StartsWith("Maze_B");   // molduras que dejaron los muros del zigzag (viven en Details)
                if (oldTrim) { Object.DestroyImmediate(t.gameObject); removed++; continue; }
                bool oldColumn = t.name == "Column" && t.position.z > 9f && t.position.z < 20f && Mathf.Abs(Mathf.Abs(t.position.x) - 5.5f) < 0.3f;
                if (oldProp || oldColumn) { Object.DestroyImmediate(t.gameObject); removed++; }
            }
            // lamparas anadidas por versiones anteriores de este rediseno (z 10,8 y 14,3) y las tres viejas de z = 12,5
            foreach (var l in Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Where(l => Mathf.Abs(l.transform.position.x) < 14f && l.transform.position.z > 10f && l.transform.position.z < 15f).ToArray())
            { Object.DestroyImmediate(l.gameObject); removed++; }
            log.Add("retirados " + removed + " objetos viejos");

            var solid = new GameObject("ArenaMaze1_Solid").transform; solid.SetParent(level);
            var props = new GameObject("ArenaMaze1_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;

            // Recinto recto con ISLAS de estanterias: una larga en el centro y dos cortas hacia los extremos. Entre islas y muros
            // quedan 3-5 m: el jefe y tu os podeis cruzar y se puede rodear cada isla; no hay callejon sin salida.
            Rack(solid, "Rack_Centro", 0f, 14.5f, 9.0f, 1.7f, metal, wood, 11);          // x -4,5..4,5
            Rack(solid, "Rack_Oeste", -10.0f, 14.5f, 1.7f, 3.6f, metal, wood, 23);       // z 12,7..16,3
            Rack(solid, "Rack_Este", 10.0f, 14.3f, 1.7f, 3.8f, metal, wood, 37);
            // estanterias bajas pegadas al muro sur, a los lados de la puerta (ambiente; sin huecos detras)
            Rack(solid, "Rack_Sur_O", -9.5f, 9.45f, 7.0f, 0.7f, metal, wood, 41);
            Rack(solid, "Rack_Sur_E", 9.0f, 9.45f, 6.0f, 0.7f, metal, wood, 53);
            GameObject Prop(string n, float x, float z, float yaw) => Call<GameObject>("Prop", n, new Vector3(x, 0f, z), yaw, props, 1f);
            // cajas y barriles en las esquinas, pegados al muro (el locker de la arena esta en x 14,2, z 12)
            Prop("Crate", -13.9f, 19.1f, 0f); Prop("Crate", -13.9f, 18.0f, 20f); Prop("Barrel", -13.9f, 12.0f, 0f); Prop("Barrel", 13.9f, 19.2f, 0f); Prop("Crate", 13.9f, 17.9f, -15f);

            // luces tenues como el resto de la arena (las tres de z = 16,8 ya existen)
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).FirstOrDefault(l => l.transform.position.z > 16f && l.transform.position.z < 17.5f && l.transform.position.y < 3.4f);
            if (lampSrc != null)
            {
                foreach (var xz in new[] { new Vector2(-9.5f, 11.2f), new Vector2(9.5f, 11.2f), new Vector2(0f, 11.5f) })
                {
                    var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                    go.name = "Lamp";
                    go.transform.position = new Vector3(xz.x, lampSrc.transform.position.y, xz.y);
                    foreach (var f in go.GetComponentsInChildren<Light>(true))
                        if (f != go.GetComponent<Light>()) { var p = f.transform.position; f.transform.position = new Vector3(xz.x, p.y, xz.y); }
                }
                log.Add("3 lamparas nuevas");
            }
            else log.Add("AVISO: no se encontro una lampara de referencia");

            // jefe en el pasillo norte este, mirando al oeste; la puerta de la sala se atranca al empezar el combate
            var boss = GameObject.Find("Boss");
            if (boss != null) { boss.transform.SetPositionAndRotation(new Vector3(7.0f, 1.4f, 17.6f), Quaternion.Euler(0f, -90f, 0f)); EditorUtility.SetDirty(boss); }
            var trig = GameObject.Find("BossRoomTrigger");
            var door = GameObject.Find("Door_Boss");
            if (trig != null && door != null)
            {
                var brt = trig.GetComponent<BossRoomTrigger>();
                brt.sealDoors = new[] { door.GetComponentInChildren<Door>() };
                EditorUtility.SetDirty(brt);
                log.Add("puerta del jefe atrancable");
            }
            log.Add(ArenaAudit.Run(true).Replace("\n", " "));   // huecos donde el jugador cabe y el jefe no: se sellan
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }
    }
}
#endif
