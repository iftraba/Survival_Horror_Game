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
    /// Tramo tras el primer jefe. La puerta del norte de la arena (antes la salida y fin de la partida) pasa a ser una
    /// puerta normal con la llave de salida; detras hay un pasillo, un vestibulo y tres salas:
    ///   oeste: sala segura 4 (telefono + baul + notas), centro: sala de control (llave del garaje, taquilla con codigo
    ///   con la 2ª riñonera, zombis), este: garaje, cuyo porton (puerta con la llave del garaje) da a la zona 2 (Zone2Wing,
    ///   que se ejecuta DESPUES: sala segura 5, laboratorio, almacen y la arena del segundo jefe, donde esta el final).
    /// Edita la ESCENA (fuente de verdad). Idempotente: rehace PostBoss_*, los zombis "PB_*" y el botin al norte de z = 20.5.
    /// </summary>
    public static class PostBossWing
    {
        const string P = "Assets/_Project/Prefabs/";
        const string Mats = "Assets/_Project/Materials/";
        const string Data = "Assets/_Project/Data/";
        public const string ControlCode = "0316";

        const float ZMin = 20f, ZMax = 39f;          // el nuevo tramo ocupa z 20..39 (muro norte nuevo en 38.5)
        const float HallZ0 = 25f, RoomZ = 29f;       // vestibulo z 25..29; salas z 29..38
        const float CxWest = -10.3f, CxControl = 0f, CxGarage = 10.3f;

        static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;
        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, Priv).Invoke(null, args);

        static GameObject Inst(string prefab, Transform parent, Vector3 pos, float yaw, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (name != null) go.name = name;
            return go;
        }

        static ItemData Item(string n) => AssetDatabase.LoadAssetAtPath<ItemData>(Data + n + ".asset");
        static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(Mats + n + ".mat");

        [MenuItem("Horror/Tramo final (despues del jefe)")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var details = GameObject.Find("Details").transform;
            var wall = M("Env_Wall"); var wood = M("Env_Wood"); var floorMat = M("Env_Floor"); var ceilMat = M("Env_Ceiling"); var metal = M("Env_Metal");

            // ------------------------------------------------ limpieza (idempotente)
            foreach (var n in new[] { "PostBoss_Solid", "PostBoss_Props", "PostBoss_Extras" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            foreach (var z in Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None).Where(z => z.name.StartsWith("PB_")).ToArray()) Object.DestroyImmediate(z.gameObject);
            // solo lo de este tramo (z 20.5 a 39): lo de mas al norte es de Zone2Wing
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(p => p.transform.position.z > 20.5f && p.transform.position.z < 39f).ToArray()) Object.DestroyImmediate(pk.gameObject);
            foreach (var l in Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Where(l => l.transform.position.z > 20.6f && l.transform.position.z < 39f).ToArray()) Object.DestroyImmediate(l.gameObject);
            // la salida vieja (caja con ExitDoor y su marco) y el muro norte de una pieza se sustituyen
            foreach (var t in level.Cast<Transform>().ToArray())
                if (t.name == "ExitDoor" || t.name.StartsWith("ExitFrame_") || t.name == "Wall_N") Object.DestroyImmediate(t.gameObject);

            var solid = new GameObject("PostBoss_Solid").transform; solid.SetParent(level);
            var props = new GameObject("PostBoss_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;   // los muebles no son suelo
            var extras = new GameObject("PostBoss_Extras").transform; extras.SetParent(level);

            GameObject Box(string n, Vector3 c, Vector3 s, Material m, Transform par, float tile = 3f, bool col = true) => Call<GameObject>("Box", n, c, s, m, par, tile, col);
            void Wall(string n, float x0, float z0, float x1, float z1) => Call<object>("Partition", solid, details, wall, wood, n, new Vector2(x0, z0), new Vector2(x1, z1));
            void Lintel(float cx, float z) => Call<object>("Lintel", solid, wall, new Vector3(cx, 0f, z), 1.5f, true);
            GameObject Prop(string n, float x, float z, float yaw) => Call<GameObject>("Prop", n, new Vector3(x, 0f, z), yaw, props, 1f);

            // ------------------------------------------------ suelo, techo y muros exteriores
            float zc = (ZMin + ZMax) / 2f, zl = ZMax - ZMin;
            Box("Ground_Post", new Vector3(0f, -0.5f, zc), new Vector3(30f, 1f, zl), floorMat, solid, 4f);
            Box("Ceiling_Post", new Vector3(0f, 3.5f, zc), new Vector3(30f, 1f, zl), ceilMat, solid, 2.4f);
            Box("Wall_W_Post", new Vector3(-15f, 1.5f, zc + 0.5f), new Vector3(1f, 3f, zl - 1f), wall, solid, 3f);
            Box("Wall_E_Post", new Vector3(15f, 1.5f, zc + 0.5f), new Vector3(1f, 3f, zl - 1f), wall, solid, 3f);
            // muro norte del tramo (z = 38.5) con el hueco de la puerta del garaje (x 9.55..11.05) hacia la zona 2
            Box("Wall_N_Post_W", new Vector3(-2.975f, 1.5f, 38.5f), new Vector3(25.05f, 3f, 1f), wall, solid, 3f);
            Box("Wall_N_Post_E", new Vector3(13.275f, 1.5f, 38.5f), new Vector3(4.45f, 3f, 1f), wall, solid, 3f);
            Box("Wall_N_Post_Top", new Vector3(CxGarage, 2.7f, 38.5f), new Vector3(1.5f, 0.6f, 1f), wall, solid, 3f);
            // muro norte de la arena (antes de una pieza) con el hueco de la puerta de salida
            Box("Wall_N_W", new Vector3(-7.875f, 1.5f, 20f), new Vector3(14.25f, 3f, 1f), wall, solid, 3f);
            Box("Wall_N_E", new Vector3(7.875f, 1.5f, 20f), new Vector3(14.25f, 3f, 1f), wall, solid, 3f);
            Box("Wall_N_Top", new Vector3(0f, 2.7f, 20f), new Vector3(1.5f, 0.6f, 1f), wall, solid, 3f);

            // ------------------------------------------------ pasillo, vestibulo y salas
            Wall("Cor_W", -2f, 20.5f, -2f, HallZ0);
            Wall("Cor_E", 2f, 20.5f, 2f, HallZ0);
            Wall("Hall_S_W", -14.5f, HallZ0, -2f, HallZ0);
            Wall("Hall_S_E", 2f, HallZ0, 14.5f, HallZ0);
            // pared de las salas (z = 29) con tres huecos de 1.5 m
            Wall("Rooms_A", -14.5f, RoomZ, CxWest - 0.75f, RoomZ);
            Wall("Rooms_B", CxWest + 0.75f, RoomZ, CxControl - 0.75f, RoomZ);
            Wall("Rooms_C", CxControl + 0.75f, RoomZ, CxGarage - 0.75f, RoomZ);
            Wall("Rooms_D", CxGarage + 0.75f, RoomZ, 14.5f, RoomZ);
            foreach (float cx in new[] { CxWest, CxControl, CxGarage }) Lintel(cx, RoomZ);
            Wall("Div_W", -6f, RoomZ, -6f, 38f);
            Wall("Div_E", 6f, RoomZ, 6f, 38f);

            // ------------------------------------------------ puertas
            var doorExit = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(-0.75f, 0f, 20f), 0f, "Door_Exit");
            var dExit = doorExit.GetComponentInChildren<Door>();
            dExit.requiredKey = Item("I_KeyExit"); dExit.consumeKey = true; dExit.zombiesCanForce = false;
            dExit.openedObjective = "Hay mas edificio al otro lado. Busca la salida: el porton del garaje, al este.";
            var doorW = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(CxWest - 0.75f, 0f, RoomZ), 0f, "Door_SafeRoom4");
            doorW.GetComponentInChildren<Door>().zombiesCanForce = false;
            var doorC = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(CxControl - 0.75f, 0f, RoomZ), 0f, "Door_Control");
            var doorG = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(CxGarage - 0.75f, 0f, RoomZ), 0f, "Door_Garage");
            var rt = Object.FindFirstObjectByType<RuntimeNavMesh>();
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList();
            foreach (var d in new[] { doorExit, doorW, doorC, doorG }) leaves.Add(d.GetComponentInChildren<Door>().gameObject);
            rt.disableDuringBake = leaves.Distinct().ToArray();
            EditorUtility.SetDirty(rt);

            // ------------------------------------------------ llave del garaje, notas y datos
            var keyGarage = AssetDatabase.LoadAssetAtPath<ItemData>(Data + "I_KeyGarage.asset");
            if (keyGarage == null) { keyGarage = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(keyGarage, Data + "I_KeyGarage.asset"); }
            keyGarage.displayName = "Llave del garaje"; keyGarage.type = ItemType.Key; keyGarage.tint = new Color(0.9f, 0.85f, 0.15f);
            keyGarage.description = "Abre el porton del garaje, al este del vestibulo. Es la ultima salida.";
            keyGarage.pickupObjective = "Tienes la llave del garaje. Abre el porton del garaje, en la sala del este.";
            EditorUtility.SetDirty(keyGarage);

            var comunicado = ArchiveSetup.Note("note_comunicado_sector7", "Comunicado interno", NoteCategory.Story,
                "A TODO EL PERSONAL DEL SECTOR 7\n\nPor orden de la Direccion de Grimheim, el sector queda en cuarentena desde las 23:00. Nadie entra ni sale del edificio hasta nueva orden.\n\nLos afectados NO deben ser trasladados al calabozo. Repito: NO trasladar.\n\nSi lees esto y aun puedes caminar, ve al garaje. La llave la tiene el operador de la sala de control.",
                "", "");
            var armario = ArchiveSetup.Note("note_armario_mando", "Nota del operador", NoteCategory.Puzzle,
                "Cerre el armario de mando con candado de numeros por si venian a por las llaves de reserva.\n\nLa clave es la hora de la alarma: las tres y dieciseis de la madrugada.\n\nNo la apunto en otro sitio.",
                "0 3 1 6", "Hay un armario de mando con teclado en la sala de control (la del centro).");
            AssetDatabase.SaveAssets();

            var db = Object.FindFirstObjectByType<ItemDatabase>();
            if (db != null)
            {
                var items = (db.items ?? new ItemData[0]).Where(i => i != null).ToList(); if (!items.Contains(keyGarage)) items.Add(keyGarage); db.items = items.ToArray();
                var notes = (db.notes ?? new NoteData[0]).Where(n => n != null).ToList();
                foreach (var n in new[] { comunicado, armario }) if (!notes.Contains(n)) notes.Add(n);
                db.notes = notes.ToArray();
                EditorUtility.SetDirty(db);
            }
            else log.Add("AVISO: no hay ItemDatabase");

            // ------------------------------------------------ sala segura 4 (oeste): telefono, baul, notas
            Inst("Interactables/SavePhone.prefab", solid, new Vector3(CxWest, 0f, 37.4f), 180f, "SavePhone_Safe4");
            Inst("Interactables/ItemBox.prefab", solid, new Vector3(-14.0f, 0f, 33.0f), 90f, "ItemBox_Safe4");
            Prop("Desk", -9.0f, 35.8f, 0f); Prop("Chair", -9.0f, 34.9f, 180f);
            Prop("Crate", -12.5f, 31.0f, 15f);

            // ------------------------------------------------ sala de control (centro)
            foreach (float x in new[] { -3.5f, 0f, 3.5f }) { Prop("Desk", x, 36.9f, 0f); Prop("Chair", x, 35.8f, 180f + (x * 4f)); }
            Prop("FilingCabinet", -5.55f, 31.0f, 90f); Prop("FilingCabinet", -5.55f, 32.0f, 90f);
            var locker = ArchiveSetup.MakeCodeLocker(extras, metal, new Vector3(5.65f, 0f, 33.5f), -90f, ControlCode);
            locker.name = "Locker_Code_Control";

            // ------------------------------------------------ garaje (este) y porton de salida
            Prop("Crate", 13.4f, 31.0f, 20f); Prop("Crate", 9.0f, 31.4f, -10f); Prop("Barrel", 13.2f, 36.5f, 0f); Prop("Barrel", 12.2f, 36.8f, 0f);
            Prop("Shelf", 6.6f, 33.5f, -90f);
            // el porton del garaje ya no termina la partida: es una puerta con llave hacia la zona 2 (ver Zone2Wing)
            var doorGate = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(CxGarage - 0.75f, 0f, 38.5f), 0f, "Door_GarageGate");
            var dGate = doorGate.GetComponentInChildren<Door>();
            dGate.requiredKey = keyGarage; dGate.consumeKey = true; dGate.zombiesCanForce = false;
            dGate.openedObjective = "Instalaciones tecnicas. Sigue adelante: la arena del fondo guarda la salida.";
            leaves.Add(dGate.gameObject);
            rt.disableDuringBake = leaves.Distinct().ToArray();
            EditorUtility.SetDirty(rt);

            // ------------------------------------------------ luces
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).First(l => l.transform.position.y < 3.2f && l.transform.position.z < 20f);
            CeilingLamp NewLamp(float x, float z, float intensity, float range, float fillRange, float fillIntensity, Color color, bool flicker = false)
            {
                var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                go.name = "Lamp";
                go.transform.position = new Vector3(x, lampSrc.transform.position.y, z);
                var l = go.GetComponent<CeilingLamp>(); var light = go.GetComponent<Light>();
                l.ratedIntensity = intensity; light.intensity = intensity; light.range = range; light.color = color; l.flicker = flicker;
                foreach (var f in go.GetComponentsInChildren<Light>(true))
                {
                    if (f == light) continue;
                    f.range = fillRange; f.intensity = fillIntensity; f.color = Color.Lerp(color, Color.white, 0.3f);
                    var fr = f.GetComponent<FillLightRating>(); if (fr != null) fr.rated = fillIntensity;
                    var p = f.transform.position; f.transform.position = new Vector3(x, p.y, z);
                }
                return l;
            }
            var warm = new Color(1f, 0.74f, 0.5f); var cold = new Color(0.8f, 0.92f, 1f);
            NewLamp(0f, 22.7f, 25f, 9f, 5f, 18f, cold);                       // pasillo
            NewLamp(-8f, 27f, 25f, 9f, 7f, 20f, cold);                        // vestibulo
            NewLamp(8f, 27f, 25f, 9f, 7f, 20f, cold, true);                   // vestibulo (parpadea)
            NewLamp(CxWest, 33.5f, 26f, 10f, 7.5f, 20f, warm);                // sala segura 4
            NewLamp(CxControl, 33.5f, 26f, 10f, 8.5f, 20f, cold, true);       // sala de control (parpadea)
            NewLamp(CxGarage, 33.5f, 26f, 10f, 7.5f, 20f, cold);              // garaje

            // ------------------------------------------------ botin
            var itemsRoot = GameObject.Find("Items");
            void Put(string item, int n, float x, float y, float z) { var pk = Pickup.Spawn(Item(item), n, new Vector3(x, y, z)); if (itemsRoot != null) pk.transform.SetParent(itemsRoot.transform); }
            Put("I_Spray", 1, -9.3f, 0.95f, 35.8f);                // mesa de la sala segura
            Put("I_HandgunAmmo", 12, -8.6f, 0.95f, 35.8f);
            Put("I_KeyGarage", 1, 0.0f, 0.95f, 36.9f);             // mesa central de control
            Put("I_ShotgunAmmo", 8, -3.5f, 0.95f, 36.9f);
            Put("I_Spray", 1, 3.5f, 0.95f, 36.9f);
            Put("I_HandgunAmmo", 12, 13.4f, 0.95f, 31.0f);         // caja del garaje
            Put("I_ShotgunAmmo", 6, 9.0f, 0.95f, 31.4f);
            var bag = Item("I_Bag");
            if (bag != null) { var pk = Pickup.Spawn(bag, 1, locker.transform.TransformPoint(new Vector3(0f, 1.42f, 0.02f))); if (itemsRoot != null) pk.transform.SetParent(itemsRoot.transform); }

            // las notas: papeles sobre las mesas (la pista del armario en la sala segura; el comunicado en el vestibulo)
            var paper = Call<Material>("Mat", "NotePaper", new Color(0.86f, 0.8f, 0.64f));
            Physics.SyncTransforms();
            Vector3 Surface(float x, float z, float fromY)
            {
                var hits = Physics.RaycastAll(new Vector3(x, fromY, z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                return hits.Length > 0 ? hits[0].point : new Vector3(x, 0.8f, z);
            }
            void Paper(string name, NoteData n, float x, float z, float yaw)
            {
                var p = Surface(x, z, 2f);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name; go.transform.SetParent(extras);
                go.transform.SetPositionAndRotation(p + Vector3.up * 0.003f, Quaternion.Euler(0f, yaw, 0f));
                go.transform.localScale = new Vector3(0.21f, 0.004f, 0.297f);
                go.GetComponent<Renderer>().sharedMaterial = paper;
                go.GetComponent<BoxCollider>().size = new Vector3(1.4f, 12f, 1.2f);
                go.AddComponent<ReadableNote>().note = n;
            }
            Paper("Note_Armario", armario, -9.5f, 35.7f, 18f);      // mesa de la sala segura 4
            Paper("Note_Comunicado", comunicado, -12.5f, 31.0f, 8f); // caja de la sala segura 4 (se ve al entrar)

            // se asienta la fisica para que la escena arranque con todo en reposo
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;

            // ------------------------------------------------ zombis
            var zr = GameObject.Find("Zombies").transform;
            var spots = new (string prefab, string name, Vector3 pos, float yaw)[]
            {
                ("Zombie_Cop",  "PB_Zombie_Cop",    new Vector3(-6f, 1f, 27.2f), 90f),    // vestibulo
                ("Zombie_Girl", "PB_Zombie_Girl",   new Vector3(3.0f, 1f, 34.5f), 200f),  // sala de control
                ("Zombie_Cop",  "PB_Zombie_Cop_2",  new Vector3(-3.0f, 1f, 33.0f), 160f), // sala de control
                ("Zombie_Girl", "PB_Zombie_Girl_2", new Vector3(10.3f, 1f, 34.0f), 180f), // garaje
            };
            foreach (var sp in spots)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + "Characters/" + sp.prefab + ".prefab"), zr);
                go.name = sp.name;
                go.transform.SetPositionAndRotation(sp.pos, Quaternion.Euler(0f, sp.yaw, 0f));
            }

            log.Add("tramo final creado: pasillo, vestibulo, sala segura 4, sala de control (codigo " + ControlCode + "), garaje; " + spots.Length + " zombis");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }
    }
}
#endif
