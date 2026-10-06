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
    /// Planta superior de la comisaria: escalera recta en la esquina este del hall (20 peldaños de 0.2 m, sube 4 m, con espacio libre al pie),
    /// hueco en el forjado, y encima un pasillo con seis salas: archivo, interrogatorio, descanso, despacho del jefe
    /// (con la llave de la sala), sala de reuniones y una sala segura con telefono y baul.
    /// Edita la ESCENA directamente. Idempotente: rehace todo lo que cuelga de "UpperFloor_*" y "CeilingSlab".
    /// </summary>
    public static class UpperFloor
    {
        const string P = "Assets/_Project/Prefabs/";
        const string Mats = "Assets/_Project/Materials/";
        const float Y0 = 4f;            // altura del suelo de la planta alta (cara superior del forjado)
        const float H = 3f;             // altura libre de la planta alta
        static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;

        // escalera: sube hacia +Z pegada al muro este
        const float StairX0 = 13.0f, StairX1 = 14.4f, StairZ0 = -11.2f, Tread = 0.28f, Riser = 0.2f;
        const int Steps = 20;
        // hueco del forjado: desde donde ya no cabe la cabeza hasta el final de la escalera
        const float HoleX0 = 12.8f, HoleX1 = 14.5f, HoleZ0 = StairZ0, HoleZ1 = StairZ0 + Steps * Tread;   // -5.6

        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, Priv).Invoke(null, args);
        static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(Mats + n + ".mat");

        static GameObject Inst(string prefab, Transform parent, Vector3 pos, float yaw, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (name != null) go.name = name;
            return go;
        }

        [MenuItem("Horror/Construir planta superior")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var wall = M("Env_Wall"); var wood = M("Env_Wood"); var ceilMat = M("Env_Ceiling"); var floorMat = M("Env_Floor"); var metal = M("Env_Metal");

            foreach (var n in new[] { "UpperFloor_Solid", "UpperFloor_Props", "UpperFloor_Stairs", "CeilingSlab" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            var solid = new GameObject("UpperFloor_Solid").transform; solid.SetParent(level);
            var stairs = new GameObject("UpperFloor_Stairs").transform; stairs.SetParent(level);
            var props = new GameObject("UpperFloor_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;
            var slab = new GameObject("CeilingSlab").transform; slab.SetParent(level);

            GameObject Box(string n, Vector3 c, Vector3 s, Material m, Transform parent, float tile = 3f, bool col = true)
                => Call<GameObject>("Box", n, c, s, m, parent, tile, col);

            // ---------------------------------------------------------------- 1) forjado con hueco para la escalera
            var oldCeiling = GameObject.Find("Ceiling");
            if (oldCeiling != null) oldCeiling.SetActive(false);   // se conserva desactivado como copia de seguridad
            void Slab(string n, float x0, float z0, float x1, float z1)
                => Box(n, new Vector3((x0 + x1) / 2f, 3.5f, (z0 + z1) / 2f), new Vector3(x1 - x0, 1f, z1 - z0), ceilMat, slab, 3f);
            Slab("Slab_W", -15f, -20f, HoleX0, 20f);
            Slab("Slab_E", HoleX1, -20f, 15f, 20f);
            Slab("Slab_S", HoleX0, -20f, HoleX1, HoleZ0);
            Slab("Slab_N", HoleX0, HoleZ1, HoleX1, 20f);
            // baldosas del suelo de la planta alta (finas, sin colision: el forjado ya la tiene)
            void Tiles(string n, float x0, float z0, float x1, float z1)
                => Box(n, new Vector3((x0 + x1) / 2f, Y0 + 0.01f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.02f, z1 - z0), floorMat, solid, 2f, false);
            Tiles("Tiles_W", -14.5f, -13f, HoleX0, 4.5f);
            Tiles("Tiles_E", HoleX0, HoleZ1, 14.5f, 4.5f);
            Tiles("Tiles_S", HoleX0, -13f, 14.5f, HoleZ0);

            // se retiran los dos archivadores que estorbaban en la escalera
            int rm = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray())
                if (t != null && t.name == "FilingCabinet" && t.position.x > 12.8f && t.position.z > -11.6f && t.position.z < -5.2f && t.position.y < 2f)
                { Object.DestroyImmediate(t.gameObject); rm++; }
            log.Add("retirados " + rm + " archivadores");

            // ---------------------------------------------------------------- 2) escalera
            for (int i = 0; i < Steps; i++)
            {
                float top = Riser * (i + 1);
                Box("Step_" + (i + 1), new Vector3((StairX0 + StairX1) / 2f, top / 2f, StairZ0 + Tread * (i + 0.5f)),
                    new Vector3(StairX1 - StairX0, top, Tread), wall, stairs, 1f);
            }
            // rampa invisible bajo los peldaños: el NavMesh (horneado con colisionadores) no cruza bien peldaños de 20 cm,
            // asi que los zombis suben por esta superficie lisa (35 grados); el jugador sigue pisando los peldaños
            {
                var p0 = new Vector2(StairZ0 + Tread / 2f, Riser);                    // (z, y)
                var p1 = new Vector2(StairZ0 + Steps * Tread - Tread / 2f, Y0);
                var tg = (p1 - p0).normalized;
                var nrmY = tg.x; var nrmZ = -tg.y;                                      // normal de la superficie (hacia arriba)
                var mid = (p0 + p1) / 2f;
                float len = (p1 - p0).magnitude;
                var cz = mid.x - nrmZ * 0.15f; var cy = mid.y - nrmY * 0.15f;
                var ramp = Box("Stair_NavRamp", new Vector3((StairX0 + StairX1) / 2f, cy, cz), new Vector3(StairX1 - StairX0, 0.3f, len), wall, stairs, 0f, true);
                ramp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(tg.y, tg.x) * Mathf.Rad2Deg, 0f, 0f);
                Object.DestroyImmediate(ramp.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(ramp.GetComponent<MeshFilter>());
            }
            // pasamanos del lado oeste: barra inclinada sobre dos montantes
            float run = Steps * Tread, rise = Steps * Riser;
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var rail = Box("Handrail", new Vector3(StairX0 - 0.05f, rise / 2f + 0.95f, StairZ0 + run / 2f), new Vector3(0.05f, 0.05f, Mathf.Sqrt(run * run + rise * rise)), metal, stairs, 0f, false);
            rail.transform.rotation = Quaternion.Euler(-ang, 0f, 0f);
            foreach (float f in new[] { 0.05f, 0.5f, 0.95f })
            {
                float zz = StairZ0 + run * f, yy = rise * f;
                Box("Post", new Vector3(StairX0 - 0.05f, yy + 0.475f, zz), new Vector3(0.05f, 0.95f, 0.05f), metal, stairs, 0f, false);
            }
            log.Add("escalera de " + Steps + " peldaños");

            // ---------------------------------------------------------------- 3) muros de la planta alta
            const float Th = 0.2f;
            void Wall(string n, float x0, float z0, float x1, float z1)
            {
                bool alongX = Mathf.Abs(x1 - x0) > Mathf.Abs(z1 - z0);
                float len = alongX ? Mathf.Abs(x1 - x0) : Mathf.Abs(z1 - z0);
                if (len < 0.05f) return;
                var c = new Vector3((x0 + x1) / 2f, Y0 + H / 2f, (z0 + z1) / 2f);
                Box(n, c, alongX ? new Vector3(len, H, Th) : new Vector3(Th, H, len), wall, solid, 3f);
                foreach (int side in new[] { -1, 1 })
                {
                    var off = alongX ? new Vector3(0f, 0f, side * (Th / 2f + 0.02f)) : new Vector3(side * (Th / 2f + 0.02f), 0f, 0f);
                    Box(n + "_Base", new Vector3(c.x, Y0 + 0.07f, c.z) + off, alongX ? new Vector3(len, 0.14f, 0.04f) : new Vector3(0.04f, 0.14f, len), wood, solid, 1f, false);
                }
            }
            var doors = new List<GameObject>();
            // pared con huecos de puerta: gaps = centros; cada hueco mide 1.4 m y lleva dintel y puerta
            void WallDoors(string n, bool alongX, float fixedC, float a, float b, float[] gaps, string[] doorNames)
            {
                var edges = new List<float> { a };
                foreach (var g in gaps) { edges.Add(g - 0.7f); edges.Add(g + 0.7f); }
                edges.Add(b);
                for (int i = 0; i < edges.Count; i += 2)
                {
                    if (alongX) Wall(n + "_" + i, edges[i], fixedC, edges[i + 1], fixedC);
                    else Wall(n + "_" + i, fixedC, edges[i], fixedC, edges[i + 1]);
                }
                for (int i = 0; i < gaps.Length; i++)
                {
                    var lintelC = alongX ? new Vector3(gaps[i], Y0 + 2.7f, fixedC) : new Vector3(fixedC, Y0 + 2.7f, gaps[i]);
                    Box("Lintel", lintelC, alongX ? new Vector3(1.4f, 0.6f, 0.2f) : new Vector3(0.2f, 0.6f, 1.4f), wall, solid, 3f);
                    var pos = alongX ? new Vector3(gaps[i] - 0.7f, Y0, fixedC) : new Vector3(fixedC, Y0, gaps[i] - 0.7f);
                    var d = Inst("Doors/Door_Wood_140.prefab", solid, pos, alongX ? 0f : -90f, doorNames[i]);
                    doors.Add(d.GetComponentInChildren<Door>().gameObject);
                }
            }
            // exterior: 0.5 m de grueso para quedar a ras del muro de la planta baja
            void Outer(string n, float x0, float z0, float x1, float z1)
            {
                bool alongX = Mathf.Abs(x1 - x0) > Mathf.Abs(z1 - z0);
                float len = alongX ? Mathf.Abs(x1 - x0) : Mathf.Abs(z1 - z0);
                Box(n, new Vector3((x0 + x1) / 2f, Y0 + H / 2f, (z0 + z1) / 2f), alongX ? new Vector3(len, H, 0.5f) : new Vector3(0.5f, H, len), wall, solid, 3f);
            }
            Outer("Up_Outer_E", 14.75f, -13.5f, 14.75f, 5.0f);
            Outer("Up_Outer_W", -14.75f, -13.5f, -14.75f, 5.0f);
            Outer("Up_Outer_S", -15f, -13.25f, 15f, -13.25f);
            Outer("Up_Outer_N", -15f, 4.75f, 15f, 4.75f);
            // forjado de la cubierta de la planta alta
            Box("Up_Roof", new Vector3(0f, Y0 + H + 0.2f, -4.25f), new Vector3(30f, 0.4f, 18.5f), ceilMat, solid, 3f);

            // pasillo central z -7 .. -4
            WallDoors("Corr_S", true, -7f, -14.5f, 12.8f, new[] { -9f, 1f, 9f }, new[] { "Door_Archivo", "Door_Interrogatorio", "Door_Descanso" });
            WallDoors("Corr_N", true, -4f, -14.5f, 14.5f, new[] { -8f, 0f, 9f }, new[] { "Door_Jefe", "Door_SalaSegura3", "Door_Reuniones" });
            Wall("Div_S1", -3f, -13f, -3f, -7f);
            Wall("Div_S2", 5f, -13f, 5f, -7f);
            Wall("Stair_W", HoleX0, -13f, HoleX0, -7f);
            // el hueco se abre al pasillo en su extremo norte: baranda en el borde oeste (z -7 .. -5.6) para no caer; se pasa por el extremo
            Box("Rail_Hole", new Vector3(HoleX0, Y0 + 0.5f, (-7f + HoleZ1) / 2f), new Vector3(0.06f, 1.0f, HoleZ1 + 7f), metal, solid, 0f, true);
            Wall("Div_N1", -2.5f, -4f, -2.5f, 4.5f);
            Wall("Div_N2", 2.5f, -4f, 2.5f, 4.5f);

            // las hojas nuevas tambien fuera del horneado del NavMesh
            var rt = Object.FindFirstObjectByType<RuntimeNavMesh>();
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList();
            leaves.AddRange(doors);
            rt.disableDuringBake = leaves.Distinct().ToArray();
            EditorUtility.SetDirty(rt);

            // ---------------------------------------------------------------- 4) luces
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Where(l => l.transform.position.y < 3.2f).First();
            float dy = (Y0 + 2.88f) - lampSrc.transform.position.y;
            CeilingLamp NewLamp(Vector2 xz, float intensity, float range, float fillRange, float fillIntensity, Color color, bool flicker = false)
            {
                var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                go.name = "Lamp";
                go.transform.position = new Vector3(xz.x, lampSrc.transform.position.y + dy, xz.y);
                var l = go.GetComponent<CeilingLamp>(); var light = go.GetComponent<Light>();
                l.ratedIntensity = intensity; light.intensity = intensity; light.range = range; light.color = color; l.flicker = flicker;
                foreach (var f in go.GetComponentsInChildren<Light>(true))
                {
                    if (f == light) continue;
                    f.range = fillRange; f.intensity = fillIntensity; f.color = Color.Lerp(color, Color.white, 0.3f);
                    var fr = f.GetComponent<FillLightRating>(); if (fr != null) fr.rated = fillIntensity;
                    f.transform.position = new Vector3(xz.x, Y0 + 1.9f, xz.y);
                }
                return l;
            }
            var warm = new Color(1f, 0.74f, 0.5f); var cold = new Color(0.8f, 0.92f, 1f);
            NewLamp(new Vector2(-10f, -5.5f), 40f, 9f, 5f, 16f, cold);
            NewLamp(new Vector2(-2f, -5.5f), 40f, 9f, 5f, 16f, cold, true);          // parpadea
            NewLamp(new Vector2(5f, -5.5f), 40f, 9f, 5f, 16f, cold);
            NewLamp(new Vector2(11f, -5.5f), 36f, 8f, 4.5f, 14f, cold);
            NewLamp(new Vector2(13.7f, -9.5f), 34f, 7f, 3.5f, 12f, warm);            // hueco de la escalera
            NewLamp(new Vector2(-8.5f, -10f), 44f, 10f, 5.5f, 18f, warm, true);      // archivo: parpadea
            NewLamp(new Vector2(1f, -10f), 44f, 9f, 5f, 18f, cold);
            NewLamp(new Vector2(8.5f, -10f), 44f, 9f, 5f, 18f, warm);
            NewLamp(new Vector2(-8.5f, 0.3f), 48f, 11f, 6f, 20f, warm);              // despacho del jefe
            NewLamp(new Vector2(0f, 0.3f), 48f, 9f, 5f, 20f, warm);                  // sala segura
            NewLamp(new Vector2(8.5f, 0.3f), 48f, 11f, 6f, 20f, cold);               // reuniones
            log.Add("luces de la planta alta");

            // ---------------------------------------------------------------- 5) mobiliario
            void Prop(string n, float x, float z, float yaw, float scale = 1f) => Call<GameObject>("Prop", n, new Vector3(x, Y0, z), yaw, props, scale);
            // archivo: estanterias y archivadores
            foreach (float x in new[] { -13.2f, -10.8f, -8.4f, -6.0f }) Prop("Shelf", x, -12.6f, 0f);
            foreach (float z in new[] { -12.0f, -11.0f, -10.0f, -9.0f }) Prop("FilingCabinet", -14.2f, z, 90f);
            Prop("Desk", -8.5f, -9.2f, 0f); Prop("Chair", -8.5f, -8.3f, 180f); Prop("Crate", -4.2f, -8.0f, 20f);
            // interrogatorio
            Prop("Desk", 1f, -10.5f, 0f); Prop("Chair", 1f, -11.6f, 0f); Prop("Chair", 1f, -9.4f, 180f);
            // descanso
            Prop("Cot", 7.0f, -12.0f, 0f); Prop("Cot", 9.2f, -12.0f, 0f); Prop("Cot", 11.4f, -12.0f, 0f);
            Prop("Crate", 6.0f, -8.0f, 15f); Prop("Barrel", 12.3f, -8.0f, 0f);
            // despacho del jefe
            Prop("Desk", -11.5f, 0.0f, 90f); Prop("Chair", -10.3f, 0.0f, -90f);
            Prop("Shelf", -13.9f, 3.7f, 0f); Prop("FilingCabinet", -14.2f, -2.5f, 90f); Prop("FilingCabinet", -14.2f, -1.5f, 90f);
            Prop("Chair", -6.5f, 2.5f, 200f);
            // sala de reuniones
            Prop("Desk", 7.5f, 0.5f, 0f); Prop("Desk", 9.5f, 0.5f, 0f);
            foreach (float x in new[] { 6.9f, 8.0f, 9.0f, 10.1f }) { Prop("Chair", x, 1.7f, 180f); Prop("Chair", x, -0.7f, 0f); }
            Prop("Shelf", 13.7f, 3.7f, 0f);
            // pasillo
            Prop("Crate", -13.7f, -5.5f, 10f); Prop("Barrel", -13.9f, -4.5f, 0f); Prop("FilingCabinet", 14.2f, -4.5f, -90f);
            // sala segura: telefono, baul y sillas
            var phone = Inst("Interactables/SavePhone.prefab", solid, new Vector3(0f, Y0, 4.0f), 180f, "SavePhone_Upper");
            var box = Inst("Interactables/ItemBox.prefab", solid, new Vector3(-2.1f, Y0, 0.5f), 90f, "ItemBox_Upper");
            Prop("Chair", 1.0f, 2.4f, 130f); Prop("Chair", -1.0f, 2.8f, -150f);
            log.Add("mobiliario, telefono y baul");

            // ---------------------------------------------------------------- 6) botin y zombis
            log.Add(Loot());
            log.Add(Zombies());
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }

        static ItemData Item(string n) => AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + n + ".asset");

        /// <summary>La llave de la sala pasa a la planta alta (despacho del jefe) y se reparte botin arriba.</summary>
        public static string Loot()
        {
            var items = GameObject.Find("Items").transform;
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).ToArray())
                if (pk.transform.position.y > 3.5f || (pk.item != null && pk.item.name == "I_KeyRoom")) Object.DestroyImmediate(pk.gameObject);
            void Put(string item, int n, float x, float y, float z) { var pk = Pickup.Spawn(Item(item), n, new Vector3(x, y, z)); pk.transform.SetParent(items); }
            Put("I_KeyRoom", 1, -11.5f, Y0 + 0.95f, 0.3f);       // mesa del despacho del jefe
            Put("I_HandgunAmmo", 10, -8.5f, Y0 + 0.95f, -9.2f);  // mesa del archivo
            Put("I_Spray", 1, 1.0f, Y0 + 0.95f, -10.5f);         // mesa del interrogatorio
            Put("I_ShotgunAmmo", 6, 9.0f, Y0 + 0.95f, 0.5f);     // mesa de reuniones
            Put("I_HandgunAmmo", 8, 7.0f, Y0 + 0.6f, -12.0f);    // catre del descanso
            Put("I_Spray", 1, -0.4f, Y0 + 0.3f, 1.2f);           // sala segura
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;
            return "botin arriba (la llave de la sala esta en el despacho del jefe)";
        }

        public static string Zombies()
        {
            var zr = GameObject.Find("Zombies").transform;
            foreach (var z in zr.GetComponentsInChildren<ZombieAI>(true).Where(z => z.transform.position.y > 3f).ToArray())
                Object.DestroyImmediate(z.gameObject);
            // el Yaku del almacen se muda al despacho del jefe: guarda la llave
            var yaku = zr.GetComponentsInChildren<ZombieAI>(true).FirstOrDefault(z => z.name == "Zombie_Yaku");
            var spots = new (string prefab, string name, Vector3 pos, float yaw)[]
            {
                ("Zombie_Civil", "Zombie_Civil_Up",   new Vector3(4.5f, Y0 + 1f, -5.5f), 90f),     // pasillo
                ("Zombie_Girl",  "Zombie_Girl_Up",    new Vector3(-9.5f, Y0 + 1f, -11f), 20f),     // archivo (parpadea)
                ("Zombie_Cop",   "Zombie_Cop_Up",     new Vector3(2.0f, Y0 + 1f, -11.8f), 180f),   // interrogatorio
                ("Zombie_Civil", "Zombie_Civil_Up_2", new Vector3(11.0f, Y0 + 1f, 2.5f), 200f),    // reuniones
                ("Zombie_Yaku",  "Zombie_Yaku",       new Vector3(-12.0f, Y0 + 1f, 2.3f), 120f),   // despacho del jefe
            };
            foreach (var sp in spots)
            {
                if (sp.name == "Zombie_Yaku" && yaku != null) Object.DestroyImmediate(yaku.gameObject);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + "Characters/" + sp.prefab + ".prefab"), zr);
                go.name = sp.name;
                go.transform.SetPositionAndRotation(sp.pos, Quaternion.Euler(0f, sp.yaw, 0f));
            }
            return spots.Length + " zombis arriba";
        }
    }
}
#endif
