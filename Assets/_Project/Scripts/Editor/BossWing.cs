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
    /// Reforma la zona del fondo: pasillo corto tras la puerta reforzada con dos puertas (izquierda: sala segura;
    /// de frente: la arena del jefe), sala segura nueva (terminal + baul), arena iluminada con el jefe bailando,
    /// y reparto del botin (escopeta y municion ANTES, la llave de salida la suelta el jefe).
    /// Edita la ESCENA directamente (la escena es la fuente de verdad). Idempotente: rehace "BossWing_*".
    /// </summary>
    public static class BossWing
    {
        const string P = "Assets/_Project/Prefabs/";
        const string Mats = "Assets/_Project/Materials/";
        static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;

        static T Call<T>(string method, params object[] args)
        {
            var m = typeof(TestSceneBuilder).GetMethod(method, Priv);
            return (T)m.Invoke(null, args);
        }

        static GameObject Inst(string prefab, Transform parent, Vector3 pos, float yaw, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (name != null) go.name = name;
            return go;
        }

        /// <summary>Borra los objetos con ese nombre cuyo centro (XZ) esta a menos de 'r' m del punto.</summary>
        static int RemoveNear(string name, Vector3 pos, float r, bool prefix = false)
        {
            int n = 0;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t == null) continue;
                bool nameOk = prefix ? t.name.StartsWith(name) : t.name == name;
                if (!nameOk) continue;
                if (Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(pos.x, pos.z)) > r) continue;
                Object.DestroyImmediate(t.gameObject);
                n++;
            }
            return n;
        }

        [MenuItem("Horror/Reformar zona del jefe")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var details = GameObject.Find("Details").transform;
            var wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");

            // ------------------------------------------------ limpieza (la anterior reforma y la sala segura vieja)
            foreach (var n in new[] { "BossWing_Solid", "BossWing_Props", "BossWing_Boss" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            int rm = 0;
            rm += RemoveNear("Safe2_", new Vector3(-11f, 0f, 16.5f), 6f, true);
            rm += RemoveNear("Lintel", new Vector3(-10f, 0f, 15.7f), 1f);
            rm += RemoveNear("Door_SafeRoom2", new Vector3(-10f, 0f, 15f), 1f);
            rm += RemoveNear("ItemBox", new Vector3(-14.1f, 0f, 15.4f), 1f);
            rm += RemoveNear("SaveTerminal_Group", new Vector3(-11.4f, 0f, 19.1f), 1f);
            rm += RemoveNear("Lamp", new Vector3(-12.3f, 0f, 17f), 1f);
            rm += RemoveNear("Crate", new Vector3(-13.3f, 0f, 9f), 1.2f);
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(x => x.transform.position.z > 9f).ToArray())
            {
                Object.DestroyImmediate(pk.gameObject); rm++;   // todo el botin del fondo se reparte de nuevo
            }
            log.Add("retirados " + rm + " objetos de la zona vieja");

            var solid = new GameObject("BossWing_Solid").transform; solid.SetParent(level);
            var props = new GameObject("BossWing_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;   // los muebles no son suelo

            // ------------------------------------------------ pasillo y muros
            void Wall(string n, float x0, float z0, float x1, float z1) => Call<object>("Partition", solid, details, wall, wood, n, new Vector2(x0, z0), new Vector2(x1, z1));
            Wall("Hall_E", 2.5f, 5.5f, 2.5f, 9.1f);
            Wall("Hall_W_S", -2.5f, 5.5f, -2.5f, 6.3f);
            Wall("Hall_W_N", -2.5f, 7.7f, -2.5f, 9.1f);
            Wall("Arena_S_W", -14.5f, 9f, -0.75f, 9f);
            Wall("Arena_S_E", 0.75f, 9f, 14.5f, 9f);
            Wall("Safe_W", -9.5f, 5.5f, -9.5f, 9.1f);
            Call<object>("Lintel", solid, wall, new Vector3(-2.5f, 0f, 7.0f), 1.4f, false);
            Call<object>("Lintel", solid, wall, new Vector3(0f, 0f, 9f), 1.5f, true);

            // ------------------------------------------------ puertas
            var doorSafe = Inst("Doors/Door_Wood_140.prefab", solid, new Vector3(-2.5f, 0f, 6.3f), -90f, "Door_SafeRoom");
            doorSafe.GetComponentInChildren<Door>().zombiesCanForce = false;
            var doorBoss = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(-0.75f, 0f, 9f), 0f, "Door_Boss");
            var bossDoor = doorBoss.GetComponentInChildren<Door>();
            bossDoor.zombiesCanForce = true; bossDoor.zombieForceTime = 2.5f;

            // las hojas nuevas tambien deben quedar fuera del horneado del NavMesh en juego
            var rt = Object.FindFirstObjectByType<RuntimeNavMesh>();
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList();
            leaves.Add(doorSafe.GetComponentInChildren<Door>().gameObject);
            leaves.Add(bossDoor.gameObject);
            rt.disableDuringBake = leaves.ToArray();
            EditorUtility.SetDirty(rt);

            // ------------------------------------------------ sala segura
            var terminal = Inst("Interactables/SaveTerminal.prefab", solid, new Vector3(-6.0f, 0f, 8.6f), 180f, "SaveTerminal_Safe2");
            var box = Inst("Interactables/ItemBox.prefab", solid, new Vector3(-9.1f, 0f, 7.2f), 90f, "ItemBox_Safe2");
            Call<GameObject>("Prop", "Chair", new Vector3(-4.2f, 0f, 6.3f), 40f, props, 1f);
            Call<GameObject>("Prop", "Chair", new Vector3(-7.6f, 0f, 6.1f), -30f, props, 1f);

            // ------------------------------------------------ luces: pasillo, sala segura y arena (siempre encendidas)
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).FirstOrDefault(l => Mathf.Abs(l.transform.position.x + 11.5f) < 0.3f && Mathf.Abs(l.transform.position.z - 1f) < 0.3f)
                          ?? Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).First();
            CeilingLamp NewLamp(Vector3 xz, float intensity, float range, float fillRange, float fillIntensity, Color? color = null)
            {
                var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                go.name = "Lamp";
                go.transform.position = new Vector3(xz.x, lampSrc.transform.position.y, xz.z);
                var l = go.GetComponent<CeilingLamp>(); var light = go.GetComponent<Light>();
                l.ratedIntensity = intensity; light.intensity = intensity; light.range = range;
                if (color.HasValue) light.color = color.Value;
                foreach (var f in go.GetComponentsInChildren<Light>(true))
                {
                    if (f == light) continue;
                    f.range = fillRange; f.intensity = fillIntensity;
                    var fr = f.GetComponent<FillLightRating>(); if (fr != null) fr.rated = fillIntensity;
                    if (color.HasValue) f.color = Color.Lerp(color.Value, Color.white, 0.3f);
                    var p = f.transform.position; f.transform.position = new Vector3(xz.x, p.y, xz.z);
                }
                return l;
            }
            var warm = new Color(1f, 0.72f, 0.45f);
            var hallLamp = NewLamp(new Vector3(0f, 0f, 7.3f), 45f, 9f, 4.6f, 18f, warm);
            NewLamp(new Vector3(-6.0f, 0f, 7.2f), 45f, 9f, 5.2f, 18f, warm);               // sala segura
            foreach (float x in new[] { -8f, 0f, 8f }) NewLamp(new Vector3(x, 0f, 16.8f), 56f, 14f, 8.5f, 22f, new Color(0.8f, 0.92f, 1f));   // arena, fondo
            // la arena ya tenia tres lamparas (z = 12.5) controladas por el interruptor: ahora siempre encendidas
            var sw = Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None).FirstOrDefault(s => Mathf.Abs(s.transform.position.x - 2f) < 0.3f && Mathf.Abs(s.transform.position.z - 5.5f) < 0.3f);
            if (sw != null) { sw.lamps = new[] { hallLamp }; sw.startOn = true; EditorUtility.SetDirty(sw); }

            // ------------------------------------------------ arena: algo de cobertura
            foreach (var c in new[] { new Vector2(-5.5f, 14f), new Vector2(5.5f, 14f) })
            {
                var col = Call<GameObject>("Box", "Column", new Vector3(c.x, 1.5f, c.y), new Vector3(0.7f, 3f, 0.7f), wall, solid, 3f, true);
            }
            Call<GameObject>("Prop", "Crate", new Vector3(-10.5f, 0f, 12.5f), 20f, props, 1f);
            Call<GameObject>("Prop", "Crate", new Vector3(-9.4f, 0f, 12.9f), -8f, props, 1f);
            Call<GameObject>("Prop", "Crate", new Vector3(10.2f, 0f, 15.5f), 35f, props, 1f);
            Call<GameObject>("Prop", "Barrel", new Vector3(9.4f, 0f, 16f), 0f, props, 1f);

            // ------------------------------------------------ jefe + disparador de combate
            var zRoot = GameObject.Find("Zombies").transform;
            var boss = Inst("Characters/Boss.prefab", zRoot, new Vector3(0f, 1.4f, 15.2f), 180f, "Boss");
            var trigger = new GameObject("BossRoomTrigger");
            trigger.transform.SetParent(zRoot);
            trigger.transform.position = new Vector3(0f, 1.5f, 10.3f);
            var bc = trigger.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(28f, 3f, 2f);
            trigger.AddComponent<BossRoomTrigger>().boss = boss.GetComponent<ZombieAI>();

            // texto del objetivo al abrir la puerta reforzada
            var main = GameObject.Find("Door_Main");
            if (main != null) { var d = main.GetComponentInChildren<Door>(); if (d != null) { d.openedObjective = "Al fondo esta el jefe y tiene la llave de salida. La sala segura queda a la izquierda."; EditorUtility.SetDirty(d); } }

            log.Add("pasillo, 2 puertas, sala segura, arena y jefe creados");
            log.Add(Loot());
            log.Add(Zombies());
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }

        static ItemData Item(string n) => AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + n + ".asset");

        /// <summary>
        /// Botin: la escopeta y los cartuchos van ANTES del jefe (despacho del capitan, almacen, oficina y sala segura);
        /// dentro de la arena queda un taquillon con cartuchos y un spray. La llave de salida la suelta el jefe.
        /// </summary>
        public static string Loot()
        {
            var items = GameObject.Find("Items").transform;
            // la escopeta y los cartuchos previos se recolocan (se eliminan los que hubiera antes del fondo para no duplicar)
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).ToArray())
                if (pk.item != null && (pk.item.name == "I_Shotgun" || pk.item.name == "I_ShotgunAmmo" || pk.item.name == "I_KeyExit")) Object.DestroyImmediate(pk.gameObject);
            void Put(string item, int n, float x, float y, float z) { var pk = Pickup.Spawn(Item(item), n, new Vector3(x, y, z)); pk.transform.SetParent(items); }
            Put("I_Shotgun", 1, -11.5f, 1.0f, 1.45f);            // mesa del despacho del capitan
            Put("I_ShotgunAmmo", 8, -11.5f, 1.0f, 0.55f);        // la misma mesa
            Put("I_ShotgunAmmo", 8, 10.4f, 0.75f, -19.2f);       // balda del almacen
            Put("I_ShotgunAmmo", 6, 5.6f, 0.95f, -10.5f);        // puesto de la oficina
            Put("I_ShotgunAmmo", 8, -5.0f, 0.6f, 6.4f);          // sala segura
            Put("I_Spray", 1, -5.6f, 0.6f, 7.0f);
            Put("I_HandgunAmmo", 12, -4.6f, 0.6f, 7.5f);
            Put("I_ShotgunAmmo", 8, 14.2f, 1.3f, 12.0f);         // taquilla de la arena (cerrada)
            Put("I_Spray", 1, 9.0f, 0.95f, 19.1f);               // escritorio de la arena
            // la riñonera esta en la taquilla con codigo de la sala de reuniones (ver ArchiveSetup)
            // se asienta la fisica para que la escena arranque con todo en reposo
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;
            return "botin repartido (escopeta en el despacho, cartuchos: 38 en total, la llave la suelta el jefe)";
        }

        /// <summary>Zombis de los cuatro tipos repartidos por el nivel (el jefe ya esta en su arena).</summary>
        public static string Zombies()
        {
            var zr = GameObject.Find("Zombies").transform;
            foreach (var z in zr.GetComponentsInChildren<ZombieAI>(true).Where(z => z.GetComponent<ZombieAI>().bossName == "").ToArray())
                Object.DestroyImmediate(z.gameObject);
            var spots = new (string prefab, string name, Vector3 pos, float yaw)[]
            {
                // Civil (verde) retirado de momento: se bugeaba (pasillo de la oficina (-2.5, -8.5) y barricada (5.2, -2.0))
                ("Zombie_Girl",  "Zombie_Girl",    new Vector3(11.5f, 1f, 1.5f), 250f),    // oficina este
                ("Zombie_Girl",  "Zombie_Girl_2",  new Vector3(-9.5f, 1f, -15.5f), 40f),   // vestuario (a oscuras)
                ("Zombie_Cop",   "Zombie_Cop",     new Vector3(-12.5f, 1f, -0.8f), 60f),   // despacho del capitan (con la escopeta)
                ("Zombie_Cop",   "Zombie_Cop_2",   new Vector3(-8.5f, 1f, -5.0f), 110f),   // oficina oeste
                ("Zombie_Yaku",  "Zombie_Yaku",    new Vector3(11.8f, 1f, -17.2f), 30f),   // almacen (guarda la llave de la sala)
            };
            foreach (var sp in spots)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + "Characters/" + sp.prefab + ".prefab"), zr);
                go.name = sp.name;
                go.transform.SetPositionAndRotation(sp.pos, Quaternion.Euler(0f, sp.yaw, 0f));
            }
            return spots.Length + " zombis colocados";
        }
    }
}
#endif
