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
    /// Zona 2 (z 39 a 85), tras la puerta del garaje de PostBossWing:
    ///   z 39-44: cuarto de bombas (oeste) y sala segura 5 (este, telefono + baul);
    ///   z 44-54: sala de maquinas (vestibulo grande con zombis);
    ///   z 54-66: laboratorio (oeste: tarjeta de acceso + pista del codigo), pasillo central y almacen (este: taquilla con
    ///            codigo con la 3ª riñonera);
    ///   z 66-84: SALA DE CALDERAS, arena del SEGUNDO JEFE (Zombie_BossPxl, ver PxlZombieKit.BuildBoss; techo de 5.5 m, caldera central, luz roja de emergencia; suelta la llave maestra) con el
    ///            porton final, que ahora es el fin de la partida.
    /// Edita la ESCENA. Idempotente: rehace Zone2_*, zombis "Z2_*"/"Boss_2" y el botin al norte de z = 39.5.
    /// Orden de ejecucion: BossWing -> PostBossWing -> Zone2Wing.
    /// </summary>
    public static class Zone2Wing
    {
        const string P = "Assets/_Project/Prefabs/";
        const string Mats = "Assets/_Project/Materials/";
        const string Data = "Assets/_Project/Data/";
        public const string StoreCode = "7258";

        const float Z0 = 39f, Z1 = 85f;
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

        static ItemData MakeKey(string name, string display, Color tint, string description, string objective)
        {
            var it = AssetDatabase.LoadAssetAtPath<ItemData>(Data + name + ".asset");
            if (it == null) { it = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(it, Data + name + ".asset"); }
            it.displayName = display; it.type = ItemType.Key; it.tint = tint; it.description = description; it.pickupObjective = objective;
            EditorUtility.SetDirty(it);
            return it;
        }

        [MenuItem("Horror/Zona 2 (segundo jefe)")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var details = GameObject.Find("Details").transform;
            var wall = M("Env_Wall"); var wood = M("Env_Wood"); var floorMat = M("Env_Floor"); var ceilMat = M("Env_Ceiling"); var metal = M("Env_Metal");

            // ------------------------------------------------ limpieza (idempotente)
            foreach (var n in new[] { "Zone2_Solid", "Zone2_Props", "Zone2_Extras" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            foreach (var z in Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None).Where(z => z.name.StartsWith("Z2_") || z.name == "Boss_2").ToArray()) Object.DestroyImmediate(z.gameObject);
            var oldTrig = GameObject.Find("BossRoomTrigger_2"); if (oldTrig != null) Object.DestroyImmediate(oldTrig);
            foreach (var pk in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(p => p.transform.position.z > 39.5f).ToArray()) Object.DestroyImmediate(pk.gameObject);
            foreach (var l in Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Where(l => l.transform.position.z > 39.6f).ToArray()) Object.DestroyImmediate(l.gameObject);

            var solid = new GameObject("Zone2_Solid").transform; solid.SetParent(level);
            var props = new GameObject("Zone2_Props").transform; props.SetParent(level);
            var nm = props.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            nm.overrideArea = true; nm.area = 1; nm.applyToChildren = true;
            var extras = new GameObject("Zone2_Extras").transform; extras.SetParent(level);

            GameObject Box(string n, Vector3 c, Vector3 s, Material m, Transform par, float tile = 3f, bool col = true) => Call<GameObject>("Box", n, c, s, m, par, tile, col);
            void Wall(string n, float x0, float z0, float x1, float z1) => Call<object>("Partition", solid, details, wall, wood, n, new Vector2(x0, z0), new Vector2(x1, z1));
            void Lintel(float cx, float z) => Call<object>("Lintel", solid, wall, new Vector3(cx, 0f, z), 1.5f, true);
            GameObject Prop(string n, float x, float z, float yaw) => Call<GameObject>("Prop", n, new Vector3(x, 0f, z), yaw, props, 1f);

            // ------------------------------------------------ suelo, techo y muros exteriores
            float zc = (Z0 + Z1) / 2f, zl = Z1 - Z0;
            Box("Ground_Z2", new Vector3(0f, -0.5f, zc), new Vector3(30f, 1f, zl), floorMat, solid, 4f);
            // salas y pasillos (z 39-66) a 3 m; la sala de calderas (z 66-85) a 5.5 m
            const float AH = 5.5f;
            Box("Ceiling_Z2", new Vector3(0f, 3.5f, 52.5f), new Vector3(30f, 1f, 27f), ceilMat, solid, 2.4f);
            Box("Wall_W_Z2", new Vector3(-15f, 1.5f, 52.5f), new Vector3(1f, 3f, 27f), wall, solid, 3f);
            Box("Wall_E_Z2", new Vector3(15f, 1.5f, 52.5f), new Vector3(1f, 3f, 27f), wall, solid, 3f);
            Box("Ceiling_Arena", new Vector3(0f, AH + 0.5f, 75.5f), new Vector3(30f, 1f, 19f), ceilMat, solid, 2.4f);
            Box("Wall_W_Arena", new Vector3(-15f, AH / 2f, 75.5f), new Vector3(1f, AH, 19f), wall, solid, 3f);
            Box("Wall_E_Arena", new Vector3(15f, AH / 2f, 75.5f), new Vector3(1f, AH, 19f), wall, solid, 3f);
            Box("Wall_N_Z2", new Vector3(0f, AH / 2f, 84.5f), new Vector3(31f, AH, 1f), wall, solid, 3f);

            // ------------------------------------------------ estructura
            // z = 44: pared sur de la sala de maquinas, con puertas a la sala segura 5 (x 10.3) y al cuarto de bombas (x -4)
            Wall("Hub_S_A", -14.5f, 44f, -4.75f, 44f);
            Wall("Hub_S_B", -3.25f, 44f, 9.55f, 44f);
            Wall("Hub_S_C", 11.05f, 44f, 14.5f, 44f);
            Lintel(-4f, 44f); Lintel(10.3f, 44f);
            Wall("Safe5_W", 6f, 39f, 6f, 44f);
            // z = 54: pared norte de la sala de maquinas: puertas al laboratorio (x -8) y al almacen (x 8) y el pasillo central abierto (x -1.5..1.5)
            Wall("Hub_N_A", -14.5f, 54f, -8.75f, 54f);
            Wall("Hub_N_B", -7.25f, 54f, -1.5f, 54f);
            Wall("Hub_N_C", 1.5f, 54f, 7.25f, 54f);
            Wall("Hub_N_D", 8.75f, 54f, 14.5f, 54f);
            Lintel(-8f, 54f); Lintel(8f, 54f);
            Wall("Pass_W", -1.5f, 54f, -1.5f, 66f);
            Wall("Pass_E", 1.5f, 54f, 1.5f, 66f);
            // z = 66: pared de la arena con la puerta de la tarjeta (x 0)
            Wall("Arena_S_W", -14.5f, 66f, -0.75f, 66f);
            Wall("Arena_S_E", 0.75f, 66f, 14.5f, 66f);
            Lintel(0f, 66f);
            // la pared de la arena sube hasta el techo alto (las particiones miden 3 m)
            Box("Arena_S_Up_W", new Vector3(-7.625f, (3f + AH) / 2f, 66f), new Vector3(13.75f, AH - 3f, 0.2f), wall, solid, 3f);
            Box("Arena_S_Up_E", new Vector3(7.625f, (3f + AH) / 2f, 66f), new Vector3(13.75f, AH - 3f, 0.2f), wall, solid, 3f);
            Box("Arena_S_Up_Door", new Vector3(0f, (2.6f + AH) / 2f, 66f), new Vector3(1.5f, AH - 2.6f, 0.2f), wall, solid, 3f);

            // ------------------------------------------------ llaves y datos
            var keyCard = MakeKey("I_KeyCard", "Tarjeta de acceso", new Color(0.2f, 0.85f, 1f),
                "Tarjeta magnetica del laboratorio. Abre la puerta de la arena del fondo.", "Tienes la tarjeta de acceso. Abre la puerta de la arena, al final del pasillo central.");
            var keyFinal = MakeKey("I_KeyFinal", "Llave maestra", new Color(1f, 0.25f, 0.2f),
                "La llave maestra del complejo. Abre el porton de salida, al norte de la arena.", "Tienes la llave maestra. Abre el porton de salida, al norte de la arena.");
            var registro = ArchiveSetup.Note("note_registro_mantenimiento", "Registro de mantenimiento", NoteCategory.Story,
                "REGISTRO DE MANTENIMIENTO — INSTALACIONES TECNICAS\n\nLos ruidos vienen de la arena del fondo. Dejamos de abrirla cuando los sensores marcaron algo vivo que no debia estar ahi.\n\nEl laboratorio guarda la tarjeta de acceso. El almacen, lo poco que queda de material util.\n\nNo vayas al fondo sin armas.",
                "", "");
            var pruebas = ArchiveSetup.Note("note_hoja_pruebas", "Hoja de pruebas", NoteCategory.Puzzle,
                "Resultado de la ultima prueba: lote 7, muestra 25, nivel 8.\n\nLa taquilla del almacen usa esos tres valores seguidos para el candado. No la dejes anotada en ningun otro sitio.",
                "7 2 5 8", "Hay una taquilla con teclado en el almacen (al este del pasillo central).");
            AssetDatabase.SaveAssets();
            var db = Object.FindFirstObjectByType<ItemDatabase>();
            if (db != null)
            {
                var its = (db.items ?? new ItemData[0]).Where(i => i != null).ToList();
                foreach (var k in new[] { keyCard, keyFinal }) if (!its.Contains(k)) its.Add(k);
                db.items = its.ToArray();
                var notes = (db.notes ?? new NoteData[0]).Where(n => n != null).ToList();
                foreach (var n in new[] { registro, pruebas }) if (!notes.Contains(n)) notes.Add(n);
                db.notes = notes.ToArray();
                EditorUtility.SetDirty(db);
            }
            else log.Add("AVISO: no hay ItemDatabase");

            // ------------------------------------------------ puertas
            var rt = Object.FindFirstObjectByType<RuntimeNavMesh>();
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList();
            Door MakeDoor(string name, float cx, float z, bool force, ItemData key = null, string objective = null)
            {
                var go = Inst("Doors/Door_Wood_150.prefab", solid, new Vector3(cx - 0.75f, 0f, z), 0f, name);
                var d = go.GetComponentInChildren<Door>();
                d.zombiesCanForce = force; d.requiredKey = key; d.consumeKey = true;
                if (!string.IsNullOrEmpty(objective)) d.openedObjective = objective;
                leaves.Add(d.gameObject);
                return d;
            }
            MakeDoor("Door_Safe5", 10.3f, 44f, false);
            MakeDoor("Door_Bombas", -4f, 44f, true);
            MakeDoor("Door_Lab", -8f, 54f, true);
            MakeDoor("Door_Almacen", 8f, 54f, true);
            MakeDoor("Door_Arena2", 0f, 66f, false, keyCard, "La arena del fondo. El jefe duerme... pero no por mucho.");
            rt.disableDuringBake = leaves.Distinct().ToArray();
            EditorUtility.SetDirty(rt);

            // ------------------------------------------------ sala segura 5
            Inst("Interactables/SavePhone.prefab", solid, new Vector3(14.0f, 0f, 41.5f), -90f, "SavePhone_Safe5");
            Inst("Interactables/ItemBox.prefab", solid, new Vector3(6.6f, 0f, 41.0f), 90f, "ItemBox_Safe5");
            Prop("Desk", 8.0f, 43.2f, 0f); Prop("Chair", 8.0f, 42.3f, 180f);

            // ------------------------------------------------ cuarto de bombas (oeste, z 39-44)
            Prop("Barrel", -13.6f, 40.2f, 0f); Prop("Barrel", -12.6f, 40.0f, 0f); Prop("Barrel", -13.7f, 41.3f, 0f);
            Prop("Crate", -8.0f, 40.4f, 20f); Prop("Crate", -7.0f, 41.0f, -10f); Prop("Shelf", -13.9f, 42.8f, 90f);
            Prop("Barrel", 2.6f, 40.0f, 0f); Prop("Barrel", 3.6f, 40.3f, 0f);

            // ------------------------------------------------ sala de maquinas (z 44-54)
            foreach (var c in new[] { new Vector2(-9f, 49f), new Vector2(-3f, 49f), new Vector2(3f, 49f), new Vector2(9f, 49f) })
                Box("Column", new Vector3(c.x, 1.5f, c.y), new Vector3(0.8f, 3f, 0.8f), wall, solid, 3f);
            Prop("Crate", -12.0f, 46.0f, 15f); Prop("Crate", -11.0f, 46.6f, -20f); Prop("Crate", 12.5f, 52.4f, 35f);
            Prop("Barrel", 13.5f, 46.5f, 0f); Prop("Barrel", -13.5f, 52.5f, 0f); Prop("Barrel", -12.5f, 52.8f, 0f);

            // ------------------------------------------------ laboratorio (oeste, z 54-66)
            foreach (float x in new[] { -12.0f, -8.5f, -5.0f }) { Prop("Desk", x, 64.8f, 0f); Prop("Chair", x, 63.8f, 180f + (x * 3f)); }
            Prop("FilingCabinet", -14.2f, 58.0f, 90f); Prop("FilingCabinet", -14.2f, 59.0f, 90f); Prop("FilingCabinet", -14.2f, 60.0f, 90f);
            Prop("Crate", -3.0f, 56.5f, 20f);

            // ------------------------------------------------ almacen (este, z 54-66) con la taquilla del codigo
            foreach (float z in new[] { 57.0f, 60.0f, 63.0f }) Prop("Shelf", 14.2f, z, -90f);
            Prop("Crate", 6.0f, 62.0f, 10f); Prop("Crate", 7.2f, 62.4f, -15f); Prop("Crate", 10.5f, 57.0f, 30f); Prop("Barrel", 11.5f, 64.5f, 0f);
            var locker = ArchiveSetup.MakeCodeLocker(extras, metal, new Vector3(1.85f, 0f, 60.5f), 90f, StoreCode);
            locker.name = "Locker_Code_Almacen";

            // ------------------------------------------------ arena del segundo jefe (z 66-84) y porton final
            GameObject Cyl(string n, Vector3 pos, Vector3 size, Quaternion rot, bool col, Material m)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = n; go.transform.SetParent(solid); go.transform.SetPositionAndRotation(pos, rot); go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = m;
                if (!col) Object.DestroyImmediate(go.GetComponent<Collider>());
                return go;
            }
            // caldera central (5 m de diametro): cobertura grande alrededor de la que se mueve el jefe
            Cyl("Boiler", new Vector3(0f, 2.5f, 76f), new Vector3(5f, 2.5f, 5f), Quaternion.identity, true, metal);
            Cyl("BoilerCap", new Vector3(0f, 5.05f, 76f), new Vector3(5.6f, 0.15f, 5.6f), Quaternion.identity, false, metal);
            // bancos de maquinas en filas con pasillos de ~2,6 m y columnas (ArenaRework): hay que ir por medio de pasillos de maquinas
            ArenaRework.MachineBanks(solid);
            // tuberias pegadas al techo y a las paredes (sin colision: van por encima de la cabeza)
            foreach (float y in new[] { 4.55f, 5.0f }) foreach (float x in new[] { -14.1f, 14.1f })
                Cyl("Pipe", new Vector3(x, y, 75.5f), new Vector3(0.45f, 9.2f, 0.45f), Quaternion.Euler(90f, 0f, 0f), false, metal);
            foreach (float z in new[] { 70f, 76f, 82f })
                Cyl("PipeCross", new Vector3(0f, 5.0f, z), new Vector3(0.4f, 14.5f, 0.4f), Quaternion.Euler(0f, 0f, 90f), false, metal);
            // cajas con municion al fondo de los pasillos ciegos (riesgo/recompensa) y en el ultimo carril
            Prop("Crate", -13.2f, 72.3f, 0f); Prop("Crate", 13.2f, 76.7f, 0f); Prop("Crate", 11.5f, 82.6f, 35f); Prop("Crate", -12.5f, 82.0f, -20f);
            Prop("Barrel", -13.6f, 76.7f, 0f); Prop("Barrel", 13.6f, 72.3f, 0f); Prop("Barrel", 12.6f, 81.3f, 0f); Prop("Barrel", -13.5f, 83.0f, 0f);
            var gate = Box("ExitGate", new Vector3(0f, 1.3f, 83.9f), new Vector3(4.2f, 2.6f, 0.14f), metal, solid, 1.5f);
            gate.AddComponent<ExitDoor>().requiredKey = keyFinal;
            Box("ExitGate_L", new Vector3(-2.18f, 1.3f, 83.9f), new Vector3(0.16f, 2.7f, 0.2f), metal, solid, 1f, false);
            Box("ExitGate_R", new Vector3(2.18f, 1.3f, 83.9f), new Vector3(0.16f, 2.7f, 0.2f), metal, solid, 1f, false);
            Box("ExitGate_T", new Vector3(0f, 2.67f, 83.9f), new Vector3(4.54f, 0.16f, 0.2f), metal, solid, 1f, false);

            // ------------------------------------------------ luces
            var lampSrc = Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).First(l => l.transform.position.y < 3.2f && l.transform.position.z < 20f);
            CeilingLamp NewLamp(float x, float z, float intensity, float range, float fillRange, float fillIntensity, Color color, bool flicker = false, float lampY = -1f, float fillY = -1f)
            {
                var go = (GameObject)Object.Instantiate(lampSrc.gameObject, level);
                go.name = "Lamp";
                go.transform.position = new Vector3(x, lampY > 0f ? lampY : lampSrc.transform.position.y, z);
                var l = go.GetComponent<CeilingLamp>(); var light = go.GetComponent<Light>();
                l.ratedIntensity = intensity; light.intensity = intensity; light.range = range; light.color = color; l.flicker = flicker;
                foreach (var f in go.GetComponentsInChildren<Light>(true))
                {
                    if (f == light) continue;
                    f.range = fillRange; f.intensity = fillIntensity; f.color = Color.Lerp(color, Color.white, 0.3f);
                    var fr = f.GetComponent<FillLightRating>(); if (fr != null) fr.rated = fillIntensity;
                    var p = f.transform.position; f.transform.position = new Vector3(x, fillY > 0f ? fillY : p.y, z);
                }
                return l;
            }
            var warm = new Color(1f, 0.74f, 0.5f); var cold = new Color(0.8f, 0.92f, 1f); var green = new Color(0.75f, 1f, 0.85f);
            NewLamp(10.3f, 41.5f, 26f, 10f, 7f, 20f, warm);                  // sala segura 5
            NewLamp(-4f, 41.5f, 24f, 10f, 9f, 20f, cold, true);              // cuarto de bombas (parpadea)
            foreach (float x in new[] { -9f, 0f, 9f }) NewLamp(x, 49f, 26f, 11f, 8f, 20f, cold);   // sala de maquinas
            NewLamp(-8f, 60f, 26f, 11f, 8.5f, 20f, green, true);             // laboratorio (parpadea)
            NewLamp(0f, 60f, 22f, 8f, 5.5f, 18f, cold);                      // pasillo central
            NewLamp(8f, 60f, 26f, 11f, 8.5f, 20f, cold);                     // almacen
            // sala de calderas: techo a 5.5 m, luz roja-naranja de emergencia, tenue; dos lamparas parpadean
            var emergency = new Color(1f, 0.42f, 0.28f);
            // una lampara por pasillo entre bancos (a 3 m del suelo la luz de relleno: si no, los bancos de 2,4-2,7 m las tapan) y otra en el ultimo carril
            foreach (float x in new[] { -9.5f, 9.5f }) foreach (float z in new[] { 72.3f, 76.7f }) NewLamp(x, z, 52f, 17f, 11f, 20f, emergency, (x > 0f) == (z > 74.5f), 5.35f, 3.0f);
            NewLamp(-6.5f, 82.0f, 46f, 15f, 9f, 18f, emergency, false, 5.35f, 3.2f); NewLamp(6.5f, 82.0f, 46f, 15f, 9f, 18f, emergency, false, 5.35f, 3.2f);
            NewLamp(0f, 68.5f, 40f, 15f, 9f, 16f, emergency, false, 5.35f, 3.2f);    // entrada
            NewLamp(0f, 83.0f, 40f, 15f, 9f, 16f, emergency, false, 5.35f, 3.2f);    // porton final
            var boilerGlow = new GameObject("BoilerGlow").AddComponent<Light>();     // resplandor de la caldera
            boilerGlow.transform.SetParent(extras); boilerGlow.transform.position = new Vector3(0f, 0.8f, 76f);
            boilerGlow.type = LightType.Point; boilerGlow.color = new Color(1f, 0.3f, 0.12f); boilerGlow.range = 9f; boilerGlow.intensity = 6f; boilerGlow.shadows = LightShadows.None;

            // ------------------------------------------------ botin
            var itemsRoot = GameObject.Find("Items");
            void Put(string item, int n, float x, float y, float z) { var pk = Pickup.Spawn(Item(item), n, new Vector3(x, y, z)); if (itemsRoot != null) pk.transform.SetParent(itemsRoot.transform); }
            Put("I_Spray", 1, 8.4f, 0.95f, 43.2f); Put("I_HandgunAmmo", 12, 7.6f, 0.95f, 43.2f);            // sala segura 5
            Put("I_ShotgunAmmo", 8, -8.0f, 1.0f, 40.4f); Put("I_HandgunAmmo", 12, -7.0f, 1.0f, 41.0f);      // cuarto de bombas
            Put("I_Spray", 1, -12.0f, 0.95f, 46.0f);                                                         // sala de maquinas
            Put("I_KeyCard", 1, -8.5f, 0.95f, 64.8f);                                                        // laboratorio: tarjeta
            Put("I_Spray", 1, -12.0f, 0.95f, 64.8f);
            Put("I_ShotgunAmmo", 8, 6.0f, 1.0f, 62.0f); Put("I_HandgunAmmo", 12, 7.2f, 1.0f, 62.4f); Put("I_ShotgunAmmo", 6, 10.5f, 1.0f, 57.0f);   // almacen
            Put("I_ShotgunAmmo", 8, -13.2f, 1.0f, 72.3f); Put("I_HandgunAmmo", 12, 13.2f, 1.0f, 76.7f);    // arena: municion en los pasillos ciegos
            Put("I_Spray", 1, 11.5f, 1.0f, 82.6f); Put("I_ShotgunAmmo", 6, -12.5f, 1.0f, 82.0f);
            var bag = Item("I_Bag");
            if (bag != null) { var pk = Pickup.Spawn(bag, 1, locker.transform.TransformPoint(new Vector3(0f, 1.42f, 0.02f))); if (itemsRoot != null) pk.transform.SetParent(itemsRoot.transform); }

            var paper = Call<Material>("Mat", "NotePaper", new Color(0.86f, 0.8f, 0.64f));
            Physics.SyncTransforms();
            Vector3 Surface(float x, float z)
            {
                var hits = Physics.RaycastAll(new Vector3(x, 2f, z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                return hits.Length > 0 ? hits[0].point : new Vector3(x, 0.8f, z);
            }
            void Paper(string name, NoteData n, float x, float z, float yaw)
            {
                var p = Surface(x, z);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name; go.transform.SetParent(extras);
                go.transform.SetPositionAndRotation(p + Vector3.up * 0.003f, Quaternion.Euler(0f, yaw, 0f));
                go.transform.localScale = new Vector3(0.21f, 0.004f, 0.297f);
                go.GetComponent<Renderer>().sharedMaterial = paper;
                go.GetComponent<BoxCollider>().size = new Vector3(1.4f, 12f, 1.2f);
                go.AddComponent<ReadableNote>().note = n;
            }
            Paper("Note_Registro", registro, 8.0f, 43.3f, 12f);   // mesa de la sala segura 5
            Paper("Note_Pruebas", pruebas, -5.0f, 64.8f, -15f);    // mesa del laboratorio

            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;

            // ------------------------------------------------ zombis y segundo jefe
            var zr = GameObject.Find("Zombies").transform;
            var spots = new (string prefab, string name, Vector3 pos, float yaw)[]
            {
                ("Zombie_Pxl2", "Z2_Pxl2_Bombas",  new Vector3(-2f, 1f, 42f), 150f),     // cuarto de bombas
                ("Zombie_Cop",  "Z2_Cop_Maquinas", new Vector3(-6f, 1f, 48f), 200f),     // sala de maquinas
                ("Zombie_Pxl3", "Z2_Pxl3_Maquinas", new Vector3(6f, 1f, 51f), 20f),
                ("Zombie_Cop",  "Z2_Cop_Lab",      new Vector3(-9f, 1f, 61f), 160f),     // laboratorio
                ("Zombie_Pxl1", "Z2_Pxl1_Lab",     new Vector3(-4f, 1f, 58f), 110f),
                ("Zombie_Pxl3", "Z2_Pxl3_Almacen", new Vector3(10f, 1f, 60f), 250f),    // almacen (antes el Yaku, retirado)
            };
            foreach (var sp in spots)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P + "Characters/" + sp.prefab + ".prefab"), zr);
                go.name = sp.name;
                go.transform.SetPositionAndRotation(sp.pos, Quaternion.Euler(0f, sp.yaw, 0f));
            }
            // segundo jefe: el coloso verde del pack de Pxltiger (PxlZombieKit.BuildBoss); suelta la llave maestra
            var boss = Inst("Characters/Zombie_BossPxl.prefab", zr, new Vector3(2f, 1.55f, 82.2f), 180f, "Boss_2");   // en el ultimo carril, tras la fila de maquinas
            var ai = boss.GetComponent<ZombieAI>();
            ai.dropOnDeath = keyFinal;
            EditorUtility.SetDirty(ai);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
            var trigger = new GameObject("BossRoomTrigger_2");
            trigger.transform.SetParent(zr);
            trigger.transform.position = new Vector3(0f, 1.5f, 67.6f);
            var bc = trigger.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(28f, 3f, 2f);
            var brt2 = trigger.AddComponent<BossRoomTrigger>(); brt2.boss = ai;
            var doorA2 = GameObject.Find("Door_Arena2");
            if (doorA2 != null) brt2.sealDoors = new[] { doorA2.GetComponentInChildren<Door>() };   // la puerta se atranca al empezar el combate

            log.Add("zona 2 creada: " + spots.Length + " zombis + jefe 2; codigo del almacen " + StoreCode);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }
    }
}
#endif
