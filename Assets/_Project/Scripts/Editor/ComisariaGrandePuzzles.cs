#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase D (menu Horror/Comisaria grande/4 Puzles): las cerraduras y piezas del recorrido.
    ///  - Candados (cizalla del garaje): puerta este del vestibulo, puerta del pasillo sur en x 8 y la del callejon.
    ///  - Tarjeta de seguridad (oficina de seguridad): pasillo de seguridad y la escalera norte de la planta baja.
    ///  - Tarjeta del jefe de seguridad (su despacho, por la escalera de incendios): calabozos.
    ///  - Taquillas con codigo: armeria (escopeta, 4519), biblioteca (medallon, 0832), laboratorio (fusible, 7258); y taquillas normales.
    ///  - Memorial: tres medallones (calabozos, biblioteca, caseta de la azotea) -> reja de la escalera del archivo.
    ///  - Jefe 1 en la arena del archivo -> llave del ascensor -> sotano.
    ///  - Cuadro electrico: tres fusibles (almacen, laboratorio, galeria de tuberias) -> puerta del pasillo de calderas.
    ///  - Jefe 2 en la sala de calderas -> llave maestra -> porton del tunel (fin).
    ///  - Sala de pruebas a oscuras con su interruptor. Notas con las pistas.
    /// Repetible: rehace "Puzles" y lo que cuelga de las puertas.
    /// </summary>
    public static class ComisariaGrandePuzzles
    {
        const string Pz = "Assets/_Project/Art/Props/Puzzle/";
        const string Data = "Assets/_Project/Data/";
        const string Mats = "Assets/_Project/Materials/";
        const float G = ComisariaGrande.G, F1 = ComisariaGrande.F1, F2 = ComisariaGrande.F2, B = ComisariaGrande.B, PIT = -6.5f;
        public const string CodeArmory = "4519", CodeLibrary = "0832", CodeLab = "7258";

        static Transform level, root;
        static Material metal, paperMat;
        static readonly List<string> log = new List<string>();
        static RuntimeNavMesh rt;

        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        static ItemData Item(string n) => AssetDatabase.LoadAssetAtPath<ItemData>(Data + n + ".asset");

        static ItemData MakeItem(string name, string display, ItemType type, Color tint, string description, string objective, string model, float scale = 1f, int maxStack = 1)
        {
            var it = Item(name);
            if (it == null) { it = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(it, Data + name + ".asset"); }
            it.displayName = display; it.type = type; it.tint = tint; it.description = description; it.pickupObjective = objective; it.maxStack = maxStack;
            var w = model != null ? AssetDatabase.LoadAssetAtPath<GameObject>(model) : null; if (w != null) it.worldPrefab = w;
            it.worldScale = scale;
            EditorUtility.SetDirty(it);
            return it;
        }

        static Door DoorNamed(string n) { var t = level.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == n); return t != null ? t.GetComponentInChildren<Door>() : null; }

        static GameObject Model(string path, Transform parent, Vector3 pos, float yaw, bool collider)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path)); holder.transform.SetParent(parent);
            holder.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform); m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            return holder;
        }

        static GameObject Led(string name, Transform parent, Vector3 pos, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent); g.transform.position = pos; g.transform.localScale = Vector3.one * 0.035f;
            g.GetComponent<Renderer>().sharedMaterial = Call<Material>("Mat", "Led_" + ColorUtility.ToHtmlStringRGB(c), c);
            var l = new GameObject("Luz").AddComponent<Light>(); l.transform.SetParent(g.transform, false); l.transform.localPosition = Vector3.zero;
            l.type = LightType.Point; l.color = c; l.range = 1.5f; l.intensity = 0.8f; l.shadows = LightShadows.None;
            return g;
        }

        /// <summary>Quita los muebles que se meten en este rectangulo (para hacer sitio a una taquilla o a una pieza).</summary>
        static void ClearAround(Rect r, float y)
        {
            var mob = level.Find("Props/Mobiliario"); if (mob == null) return;
            foreach (var t in mob.Cast<Transform>().ToList())
            {
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
                if (b.max.y < y - 0.5f || b.min.y > y + 2.5f) continue;
                if (r.Overlaps(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z))) Object.DestroyImmediate(t.gameObject);
            }
        }

        static Vector3 Surface(float x, float yTop, float z)
        {
            var hits = Physics.RaycastAll(new Vector3(x, yTop, z), Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
            return hits.Length > 0 ? hits[0].point : new Vector3(x, yTop - 1f, z);
        }

        static Pickup Put(ItemData item, int n, Vector3 p)
        {
            if (item == null) { log.Add("falta un objeto"); return null; }
            var pk = Pickup.Spawn(item, n, p + Vector3.up * 0.1f); pk.transform.SetParent(root); return pk;
        }
        static Pickup PutOn(ItemData item, int n, float x, float yTop, float z) => Put(item, n, Surface(x, yTop, z));

        static void Paper(string name, NoteData n, float x, float yTop, float z, float yaw)
        {
            var p = Surface(x, yTop, z);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root);
            go.transform.SetPositionAndRotation(p + Vector3.up * 0.003f, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = new Vector3(0.21f, 0.004f, 0.297f);
            go.GetComponent<Renderer>().sharedMaterial = paperMat;
            go.GetComponent<BoxCollider>().size = new Vector3(1.4f, 12f, 1.2f);
            go.AddComponent<ReadableNote>().note = n;
        }

        /// <summary>Taquilla (con codigo si 'code' no es vacio) contra una pared; deja dentro (en la balda) los objetos dados.</summary>
        static GameObject Locker(Vector3 pos, float yaw, string code, params (ItemData item, int n)[] inside)
        {
            ClearAround(new Rect(pos.x - 0.7f, pos.z - 0.7f, 1.4f, 1.4f), pos.y);
            var l = string.IsNullOrEmpty(code) ? Call<GameObject>("Locker", root, metal, pos, yaw, false) : ArchiveSetup.MakeCodeLocker(root, metal, pos, yaw, code);
            l.transform.SetParent(root);
            float h = 1.42f;
            foreach (var (item, n) in inside) { var pk = Pickup.Spawn(item, n, l.transform.TransformPoint(new Vector3(0f, h, 0.02f))); pk.transform.SetParent(root); h -= 0.45f; }
            return l;
        }

        // ------------------------------------------------------------------ cerraduras de las puertas
        static void Padlock(string doorName, ItemData cutter, float sideYaw, Vector3 offset)
        {
            var d = DoorNamed(doorName); if (d == null) { log.Add("falta puerta " + doorName); return; }
            d.requiredKey = cutter; d.consumeKey = false; EditorUtility.SetDirty(d);
            var leaf = d.transform.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Leaf") ?? d.transform;
            var center = d.GetComponentInChildren<Renderer>().bounds.center;
            var chain = Model(Pz + "PadlockChain.fbx", leaf, new Vector3(center.x, d.transform.position.y, center.z) + offset, sideYaw, false);
            chain.name = "Candado";
            var lv = d.gameObject.AddComponent<LockVisual>(); lv.door = d; lv.lockedVisual = chain;
        }

        static void CardLock(string doorName, ItemData card, Vector3 readerPos, float yaw)
        {
            var d = DoorNamed(doorName); if (d == null) { log.Add("falta puerta " + doorName); return; }
            d.requiredKey = card; d.consumeKey = false; EditorUtility.SetDirty(d);
            var reader = Model(Pz + "CardReader.fbx", root, readerPos, yaw, false); reader.name = "Lector_" + doorName;
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var red = Led("Piloto_Rojo", reader.transform, readerPos + fwd * 0.03f + Vector3.up * 0.08f, new Color(1f, 0.1f, 0.05f));
            var green = Led("Piloto_Verde", reader.transform, readerPos + fwd * 0.03f + Vector3.up * 0.08f, new Color(0.2f, 1f, 0.3f));
            var lv = d.gameObject.AddComponent<LockVisual>(); lv.door = d; lv.lockedVisual = red; lv.unlockedVisual = green;
        }

        // ------------------------------------------------------------------ entrada
        [MenuItem("Horror/Comisaria grande/4 Puzles")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            log.Clear();
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase A";
            level = rootGo.transform; rt = rootGo.GetComponent<RuntimeNavMesh>();
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            paperMat = AssetDatabase.LoadAssetAtPath<Material>(Mats + "NotePaper.mat") ?? metal;
            // limpieza (repetible)
            var old = level.Find("Puzles"); if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (var lv in level.GetComponentsInChildren<LockVisual>(true)) Object.DestroyImmediate(lv);
            foreach (var ps in level.GetComponentsInChildren<ProgressSeal>(true)) Object.DestroyImmediate(ps);
            foreach (var t in level.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "Candado").ToList()) if (t != null) Object.DestroyImmediate(t.gameObject);
            foreach (var d in level.GetComponentsInChildren<Door>(true)) { d.requiredKey = null; d.sealedPrompt = "Atrancada"; d.sealedMessage = "La puerta se ha atrancado"; }
            root = new GameObject("Puzles").transform; root.SetParent(level);
            ItemTextureKit.Apply();

            // ---- objetos
            var cutter = MakeItem("I_BoltCutter", "Cizalla", ItemType.Key, Color.white, "Cizalla de mangos largos. Corta los candados de las puertas.", "Con la cizalla puedes cortar los candados (vestibulo este, pasillo sur y la puerta trasera de los vestuarios).", Pz + "BoltCutter.fbx");
            var cardSec = MakeItem("I_CardSecurity", "Tarjeta de seguridad", ItemType.Key, new Color(0.3f, 0.6f, 1f), "Tarjeta de acceso del personal de seguridad. Abre las puertas con lector.", "La tarjeta abre la puerta del pasillo de seguridad (al este del pasillo sur) y la de la escalera norte.", Pz + "KeyCard.fbx", 2.5f);
            var cardChief = MakeItem("I_CardChief", "Tarjeta del jefe de seguridad", ItemType.Key, new Color(1f, 0.75f, 0.2f), "Tarjeta dorada del jefe de seguridad. Abre los calabozos.", "Con la tarjeta del jefe de seguridad se abren los calabozos (pasillo norte de la planta baja).", Pz + "KeyCard.fbx", 2.5f);
            var medalModel = "Assets/_Project/Art/Props/Memorial/MemorialMedallion.fbx";
            string medalObj = "Lleva los medallones al monumento del memorial (primera planta, donde sale la escalera de caracol).";
            var medals = new[] { "I", "II", "III" }.Select((n, i) => MakeItem("I_Medal" + "ABC"[i], "Medallon de bronce (" + n + ")", ItemType.Key, new Color(0.85f, 0.65f, 0.3f), "Medallon con la estrella de la policia. Encaja en uno de los huecos del monumento del memorial.", medalObj, medalModel)).ToArray();
            string fuseObj = "Pon los fusibles en el cuadro electrico del sotano (ala este) para dar corriente a la puerta de las calderas.";
            var fuses = new[] { "A", "B", "C" }.Select(n => MakeItem("I_Fuse" + n, "Fusible (" + n + ")", ItemType.Key, new Color(1f, 0.9f, 0.7f), "Fusible de ceramica de alto amperaje.", fuseObj, Pz + "Fuse.fbx", 2f)).ToArray();
            var elevKey = Item("I_KeyElevator") ?? MakeItem("I_KeyElevator", "Llave del ascensor", ItemType.Key, new Color(0.35f, 0.8f, 1f), "Llave de la botonera del ascensor de carga.", "", null);
            elevKey.pickupObjective = "Usa la llave en el ascensor de carga (planta baja, junto a los aseos) para bajar al sotano."; EditorUtility.SetDirty(elevKey);
            var keyFinal = Item("I_KeyFinal");
            if (keyFinal != null) { keyFinal.pickupObjective = "Abre el porton del tunel de servicio, en la pared oeste de la sala de calderas."; EditorUtility.SetDirty(keyFinal); }
            AssetDatabase.SaveAssets();
            var newItems = new[] { cutter, cardSec, cardChief }.Concat(fuses).ToArray();
            ItemIcons.Generate(newItems.Concat(medals).ToArray());

            // ---- notas
            NoteData N(string id, string title, NoteCategory c, string body, string hl, string obj) => ArchiveSetup.Note(id, title, c, body, hl, obj);
            var nTurno = N("note_cg_turno", "Parte del turno de noche", NoteCategory.Story,
                "03:40. Han roto la verja del patio. Cerramos el ala este con cadena y candado para frenarlos.\n\nLa cizalla del garaje la dejamos en el banco de trabajo (pasillo sur, al oeste). El que vuelva, que no la pierda.\n\nEl comisario no contesta.", "cizalla", "La cizalla esta en el garaje (pasillo sur, al oeste).");
            var nArmero = N("note_cg_armeria", "Inventario de la armeria", NoteCategory.Puzzle,
                "Revisado el armero. Falta un fusil.\n\nLa escopeta de reserva y sus cartuchos estan en la taquilla con teclado del fondo de la armeria.\n\nCombinacion: 4 5 1 9", "4519", "La taquilla con teclado de la armeria (ala este) se abre con 4519.");
            var nJefe = N("note_cg_jefe", "Mensaje del jefe de seguridad", NoteCategory.Story,
                "Me encierro en mi despacho de la primera planta con la tarjeta de los calabozos. He atrancado la puerta interior con todo lo que tenia.\n\nSi alguien queda: la unica forma de llegar es la escalera de incendios del callejon. Se sale por la puerta trasera de los vestuarios, la del candado.", "escalera de incendios", "Al despacho del jefe de seguridad se llega por la escalera de incendios del callejon (puerta trasera de los vestuarios).");
            var nVest = N("note_cg_vestuarios", "Nota en una taquilla", NoteCategory.Puzzle,
                "Henderson:\n\nTu medallon esta a salvo en la taquilla de la biblioteca (primera planta). La combinacion es la de siempre: 0 8 3 2.\n\nNo se la des a nadie.", "0832", "La taquilla con teclado de la biblioteca se abre con 0832.");
            var nMem = N("note_cg_memorial", "Aviso del sargento", NoteCategory.Puzzle,
                "A todo el personal:\n\nHe cerrado la escalera del archivo con la reja del memorial. Solo se abre con los tres medallones en el monumento.\n\nUno lo tiene Henderson (lo vi en los calabozos), otro esta guardado en la biblioteca y el tercero se lo llevo Miller a la caseta de la azotea.\n\nAhi arriba hay algo. No subais.", "tres medallones", "Busca los tres medallones: calabozos, biblioteca y la caseta de la azotea.");
            var nArch = N("note_cg_archivo", "Nota del archivero", NoteCategory.Story,
                "Dia 3.\n\nDesde la antesala se le oye arrastrar las estanterias. Era Morrison, el de mantenimiento: lleva colgada la llave del ascensor de carga. Sin ella no se baja al sotano.\n\nEl ascensor esta en la planta baja, junto a los aseos.", "llave del ascensor", "");
            var nMaq = N("note_cg_maquinas", "Parte de mantenimiento", NoteCategory.Puzzle,
                "La puerta de seguridad del pasillo de las calderas va con el cuadro electrico del ala este, y le faltan los tres fusibles.\n\nUno quedo en el almacen, otro lo guarde en la taquilla del laboratorio (7 2 5 8) y el ultimo se cayo en la galeria de tuberias.", "7258", "Busca los tres fusibles: almacen, taquilla del laboratorio (7258) y galeria de tuberias.");
            var nTun = N("note_cg_tunel", "Plano de evacuacion", NoteCategory.Story,
                "Salida de emergencia: tunel de servicio desde la sala de calderas hasta la calle de atras.\n\nEl porton se abre con la llave maestra del jefe de turno. El jefe de turno bajo a revisar la caldera hace dos dias y no ha vuelto.", "llave maestra", "");
            var db = Object.FindFirstObjectByType<ItemDatabase>();
            if (db != null)
            {
                var its = (db.items ?? new ItemData[0]).Where(i => i != null).ToList();
                foreach (var k in newItems.Concat(medals).Concat(new[] { elevKey, keyFinal })) if (k != null && !its.Contains(k)) its.Add(k);
                db.items = its.ToArray();
                var notes = (db.notes ?? new NoteData[0]).Where(n => n != null).ToList();
                foreach (var n in new[] { nTurno, nArmero, nJefe, nVest, nMem, nArch, nMaq, nTun }) if (!notes.Contains(n)) notes.Add(n);
                db.notes = notes.ToArray();
                EditorUtility.SetDirty(db);
            }

            // ---- puertas: candados, tarjetas y la de las calderas sin corriente
            Padlock("Puerta_Vestibulo_E", cutter, -90f, new Vector3(-0.08f, 0, 0));
            Padlock("Puerta_Pasillo_Candado", cutter, -90f, new Vector3(-0.08f, 0, 0));
            Padlock("Puerta_Callejon", cutter, 90f, new Vector3(0.08f, 0, 0));
            CardLock("Puerta_Tarjeta", cardSec, new Vector3(31.8f, G + 1.3f, 14.36f), 180f);
            CardLock("Puerta_Escalera_Norte", cardSec, new Vector3(8.0f, G + 1.3f, 28.86f), 180f);
            CardLock("Puerta_Calabozos", cardChief, new Vector3(-5.9f, G + 1.3f, 28.86f), 180f);
            var power = DoorNamed("Puerta_Sin_Corriente");
            if (power != null)
            {
                power.sealedPrompt = "Sin corriente"; power.sealedMessage = "La puerta de seguridad no tiene corriente. El cuadro electrico esta en el ala este del sotano.";
                EditorUtility.SetDirty(power);
                var ps = power.gameObject.AddComponent<ProgressSeal>(); ps.door = power; ps.flag = "corriente";
                ps.unpoweredVisual = Led("Piloto_SinCorriente", root, new Vector3(-11.85f, B + 2.75f, 27.75f), new Color(1f, 0.1f, 0.05f));
                ps.poweredVisual = Led("Piloto_Corriente", root, new Vector3(-11.85f, B + 2.75f, 27.75f), new Color(0.2f, 1f, 0.3f));
            }

            // ---- sala de pruebas a oscuras, con su interruptor junto a la puerta
            var darkLamps = level.GetComponentsInChildren<CeilingLamp>(true).Where(l => l.name == "Lamp_G_Dark").ToArray();
            var sw = ComisariaGrande.Box("Interruptor", root, new Vector3(-12.8f, G + 1.3f, 14.65f), new Vector3(0.1f, 0.15f, 0.04f), metal, 0f);
            var ls = sw.AddComponent<LightSwitch>(); ls.lamps = darkLamps; ls.startOn = false;

            // ---- taquillas con codigo y normales
            var sg = Item("I_Shotgun"); var sgAmmo = Item("I_ShotgunAmmo"); var hgAmmo = Item("I_HandgunAmmo"); var spray = Item("I_Spray"); var bag = Item("I_Bag");
            Locker(new Vector3(31.55f, G, 9.8f), -90f, CodeArmory, (sg, 1), (sgAmmo, 6));                 // armeria
            Locker(new Vector3(-8.45f, F1, 25.6f), -90f, CodeLibrary, (medals[1], 1));                      // biblioteca
            Locker(new Vector3(21.55f, B, 16.0f), -90f, CodeLab, (fuses[1], 1));                            // laboratorio
            Locker(new Vector3(-31.55f, G, 31.0f), 90f, "", (bag, 1));                                      // vestuarios: rinonera
            Locker(new Vector3(-31.55f, G, 32.3f), 90f, "", (hgAmmo, 10));
            Locker(new Vector3(19.55f, G, 15.6f), -90f, "", (spray, 1));                                    // seguridad
            Locker(new Vector3(29.1f, G, 15.2f), -90f, "", (hgAmmo, 8));                                    // descanso
            Locker(new Vector3(-20.45f, F1, 25.6f), -90f, "", (sgAmmo, 4));                                  // despacho del jefe
            Locker(new Vector3(-31.55f, B, 25.6f), 90f, "", (bag, 1));                                      // almacen del sotano: 2a rinonera
            Locker(new Vector3(31.55f, B, 2.2f), -90f, "", (sgAmmo, 6));                                    // taller del sotano

            // ---- piezas sueltas
            Physics.SyncTransforms();
            PutOn(cutter, 1, -31.0f, G + 1.6f, 20.5f);                       // garaje: banco de trabajo (pared oeste)
            PutOn(cardSec, 1, 14.0f, G + 1.6f, 25.6f);                       // seguridad: mesa de monitores
            PutOn(cardChief, 1, -25.0f, F1 + 1.6f, 20.5f);                   // despacho del jefe: su mesa
            PutOn(medals[0], 1, -9.75f, G + 1.6f, 43.0f);                    // calabozos: catre de la segunda celda
            PutOn(medals[2], 1, -21.0f, F2 + 1.6f, 31.5f);                   // caseta de la azotea
            PutOn(fuses[0], 1, -23.0f, B + 2.8f, 20.5f);                     // almacen del sotano
            PutOn(fuses[2], 1, 30.2f, B + 1.6f, 42.6f);                      // galeria de tuberias, al fondo
            Paper("Nota_Turno", nTurno, -3.6f, G + 1.6f, 9.9f, 15f);
            Paper("Nota_Armeria", nArmero, -18.7f, G + 1.6f, 25.6f, -10f);
            Paper("Nota_Jefe", nJefe, 12.5f, G + 1.6f, 25.8f, 20f);
            Paper("Nota_Vestuarios", nVest, -25.0f, G + 1.6f, 34.3f, 5f);
            Paper("Nota_Memorial", nMem, -0.6f, F1 + 1.6f, 2.0f, -80f);
            Paper("Nota_Archivero", nArch, 17.0f, F2 + 1.6f, 34.6f, 15f);
            Paper("Nota_Maquinas", nMaq, 7.4f, B + 3.0f, 5.5f, 0f);
            Paper("Nota_Tunel", nTun, -7.0f, B + 1.6f, 25.0f, 30f);

            // ---- memorial: monumento con los huecos y la reja de la escalera del archivo
            var monument = level.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Memorial_Monumento");
            if (monument != null)
            {
                foreach (var c in monument.GetComponents<MedallionMonument>()) Object.DestroyImmediate(c);
                foreach (var t in monument.Cast<Transform>().Where(t => t.name.StartsWith("Medallon_Puesto")).ToList()) Object.DestroyImmediate(t.gameObject);
                var mm = monument.gameObject.AddComponent<MedallionMonument>(); mm.medallions = medals; mm.placedVisuals = new GameObject[3];
                mm.doneMessage = "Al encajar el ultimo medallon se oye una reja subiendo al otro lado del pasillo norte.";
                mm.doneObjective = "Sube al archivo por la escalera del pasillo norte de la primera planta.";
                float[] sx = { -0.27f, 0f, 0.27f };
                for (int i = 0; i < 3; i++)
                {
                    var v = new GameObject("Medallon_Puesto_" + i); v.transform.SetParent(monument, false); v.transform.localPosition = new Vector3(sx[i], 1.66f, 0.2f);
                    var mdl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(medalModel), v.transform); mdl.transform.localPosition = Vector3.zero;
                    v.SetActive(false); mm.placedVisuals[i] = v;
                }
            }
            else log.Add("AVISO: no hay monumento");
            var gate = new GameObject("Reja_Archivo"); gate.transform.SetParent(root); gate.transform.position = new Vector3(-12.85f, F1, 31.3f);
            var bars = new GameObject("Barrotes").transform; bars.SetParent(gate.transform, false);
            for (float x = -13.95f; x <= -11.75f; x += 0.14f) ComisariaGrande.Box("Barrote", bars, new Vector3(x, F1 + 1.3f, 31.3f), new Vector3(0.04f, 2.6f, 0.04f), metal, 0f, false);
            foreach (float yy in new[] { 0.1f, 1.3f, 2.55f }) ComisariaGrande.Box("Travesano", bars, new Vector3(-12.85f, F1 + yy, 31.3f), new Vector3(2.3f, 0.06f, 0.06f), metal, 0f, false);
            var gc = gate.AddComponent<BoxCollider>(); gc.center = new Vector3(0, 1.3f, 0); gc.size = new Vector3(2.3f, 2.6f, 0.12f);
            var obs = gate.AddComponent<NavMeshObstacle>(); obs.carving = true; obs.shape = NavMeshObstacleShape.Box; obs.center = new Vector3(0, 1.3f, 0); obs.size = new Vector3(2.3f, 2.6f, 0.3f);
            var gsv = gate.AddComponent<ServiceGate>(); gsv.bars = bars; gsv.flag = "memorial";
            gsv.lockedMessage = "Una reja de seguridad cierra la escalera del archivo. Tiene una placa: \"Se abre desde el memorial.\"";

            // ---- cuadro electrico del sotano (tres fusibles)
            var fb = Model(Pz + "FuseBox.fbx", root, new Vector3(22.25f, B + 1.3f, 20.5f), 90f, true); fb.name = "Cuadro_Fusibles";
            var fm = fb.AddComponent<MedallionMonument>();
            fm.medallions = fuses; fm.flagPrefix = "fusible_"; fm.doneFlag = "corriente"; fm.placedVisuals = new GameObject[3];
            fm.placePrompt = "E  Poner fusible"; fm.examinePrompt = "E  Examinar cuadro electrico";
            fm.emptyText = "Cuadro de la puerta de seguridad de las calderas. Le faltan los tres fusibles.";
            fm.missingText = "Faltan {0} fusibles."; fm.placedText = "Pones el fusible ({0}/{1}).";
            fm.doneMessage = "Subes la palanca: se oye la puerta de seguridad de las calderas desbloqueandose.";
            fm.doneObjective = "Ve a la sala de calderas (pasillo norte del sotano, al oeste).";
            float[] fx = { -0.18f, 0f, 0.18f };
            for (int i = 0; i < 3; i++)
            {
                var v = new GameObject("Fusible_Puesto_" + i); v.transform.SetParent(fb.transform, false); v.transform.localPosition = new Vector3(fx[i], 0.12f, 0.09f); v.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                var mdl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Pz + "Fuse.fbx"), v.transform); mdl.transform.localPosition = Vector3.zero;
                v.SetActive(false); fm.placedVisuals[i] = v;
            }

            // ---- ascensor (planta baja <-> sotano)
            ElevatorPanel Panel(string name, float y, string dest)
            {
                var p = ComisariaGrande.Box(name, root, new Vector3(4.86f, y + 1.3f, 26.0f), new Vector3(0.06f, 0.5f, 0.32f), metal, 0f);
                ComisariaGrande.Box("Boton", root, new Vector3(4.82f, y + 1.38f, 26.0f), new Vector3(0.03f, 0.07f, 0.07f), AssetDatabase.LoadAssetAtPath<Material>(Mats + "Bulb_FFF2D9.mat") ?? metal, 0f, false);
                var ep = p.AddComponent<ElevatorPanel>(); ep.requiredKey = elevKey; ep.destinationName = dest;
                ep.unlockedObjective = "Baja al sotano en el ascensor. Hay que dar corriente a la puerta de las calderas.";
                return ep;
            }
            Transform Arrival(string name, float y) { var a = new GameObject(name).transform; a.SetParent(root); a.SetPositionAndRotation(new Vector3(3.6f, y + 1.05f, 24.75f), Quaternion.Euler(0, -90f, 0)); return a; }
            var pG = Panel("Ascensor_Botonera_Baja", G, "sotano"); var pB = Panel("Ascensor_Botonera_Sotano", B, "planta baja");
            pG.arrival = Arrival("Ascensor_Llegada_Sotano", B); pB.arrival = Arrival("Ascensor_Llegada_Baja", G);

            // ---- jefes
            var bosses = new GameObject("Jefes").transform; bosses.SetParent(root);
            var b1 = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Boss.prefab"), bosses);
            b1.name = "Boss"; b1.transform.SetPositionAndRotation(new Vector3(15f, F2 + 0.05f, 29f), Quaternion.Euler(0, -90f, 0));
            var ai1 = b1.GetComponent<ZombieAI>(); ai1.dropOnDeath = elevKey; EditorUtility.SetDirty(ai1); PrefabUtility.RecordPrefabInstancePropertyModifications(ai1);
            var t1 = new GameObject("BossRoomTrigger"); t1.transform.SetParent(bosses); t1.transform.position = new Vector3(15f, F2 + 1.5f, 29f);
            var bc1 = t1.AddComponent<BoxCollider>(); bc1.isTrigger = true; bc1.size = new Vector3(13.6f, 3f, 13.6f);
            var brt1 = t1.AddComponent<BossRoomTrigger>(); brt1.boss = ai1; var da = DoorNamed("Puerta_Archivo"); brt1.sealDoors = da != null ? new[] { da } : new Door[0];
            var b2 = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Zombie_BossPxl.prefab"), bosses);
            b2.name = "Boss_2"; b2.transform.SetPositionAndRotation(new Vector3(-27f, PIT + 0.05f, 41.5f), Quaternion.Euler(0, 140f, 0));
            var ai2 = b2.GetComponent<ZombieAI>(); if (keyFinal != null) ai2.dropOnDeath = keyFinal; EditorUtility.SetDirty(ai2); PrefabUtility.RecordPrefabInstancePropertyModifications(ai2);
            var t2 = new GameObject("BossRoomTrigger_2"); t2.transform.SetParent(bosses); t2.transform.position = new Vector3(-22f, B + 1.5f, 30.3f);
            var bc2 = t2.AddComponent<BoxCollider>(); bc2.isTrigger = true; bc2.size = new Vector3(3.6f, 3f, 2.2f);
            var brt2 = t2.AddComponent<BossRoomTrigger>(); brt2.boss = ai2; var dc = DoorNamed("Puerta_Calderas"); brt2.sealDoors = dc != null ? new[] { dc } : new Door[0];

            // ---- porton del tunel (pared oeste del foso de calderas)
            var exitGate = ComisariaGrande.Box("Porton_Tunel", root, new Vector3(-31.75f, PIT + 1.3f, 37f), new Vector3(0.14f, 2.6f, 4.2f), metal, 1.5f);
            exitGate.AddComponent<ExitDoor>().requiredKey = keyFinal;
            foreach (var (z, h) in new[] { (39.15f, 0f), (34.85f, 0f) }) ComisariaGrande.Box("Porton_Marco", root, new Vector3(-31.7f, PIT + 1.35f, z), new Vector3(0.2f, 2.7f, 0.16f), metal, 1f, false);
            ComisariaGrande.Box("Porton_Marco", root, new Vector3(-31.7f, PIT + 2.72f, 37f), new Vector3(0.2f, 0.16f, 4.46f), metal, 1f, false);
            var el = new GameObject("Luz_Porton").AddComponent<Light>(); el.transform.SetParent(root); el.transform.position = new Vector3(-30.8f, PIT + 3.2f, 37f);
            el.type = LightType.Point; el.range = 6f; el.intensity = 3f; el.color = new Color(0.5f, 1f, 0.55f);

            // ---- que se asienten los objetos, NavMesh y guardar
            var oldMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script; Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList(); leaves.Add(gate); rt.disableDuringBake = leaves.Distinct().ToArray(); EditorUtility.SetDirty(rt);
            var surface = rootGo.GetComponent<NavMeshSurface>();
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase D: " + root.GetComponentsInChildren<Pickup>().Length + " objetos, " + root.GetComponentsInChildren<LockerDoor>().Length + " taquillas, " + level.GetComponentsInChildren<LockVisual>().Length + " cerraduras | " + string.Join(" | ", log);
        }
    }
}
#endif
