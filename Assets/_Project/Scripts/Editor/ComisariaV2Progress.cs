#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria v2, fase 6: el recorrido (menu Horror/Comisaria v2/6 Recorrido). Lo que hace que se pueda jugar de principio a fin:
    ///  1. Planta baja amueblada: sala de espera, sala segura, oficina, vestibulo del ascensor, vestuarios y calabozos.
    ///  2. Puzle del memorial: tres medallones (despacho del comisario, estrado de conferencias y una celda de los calabozos).
    ///     Con los tres en el monumento se abre la reja de la escalera de servicio de la sala de ordenadores.
    ///  3. Escalera de servicio de la primera planta al archivo (hueco en las losas, rampa, barandillas y un cuarto arriba con
    ///     puerta). Al salir de ese cuarto despierta el jefe 1 y la puerta se atranca; suelta la llave del ascensor.
    ///  4. Ascensor de carga: botoneras en la planta baja y el sotano; se pone en marcha con esa llave.
    ///  5. Jefe 2 en la sala de calderas: suelta la llave maestra, que abre el porton del tunel de servicio = fin de la partida.
    ///  6. Salas seguras (planta baja, sala de ordenadores, sotano junto al ascensor), botin, notas y zombis.
    /// Repetible: rehace "Recorrido" y "Props/Recorrido_Props". La escalera de servicio (que agujerea las losas) se hace una vez
    /// ("Escalera_Servicio"); si ya existe no se toca.
    /// </summary>
    public static class ComisariaV2Progress
    {
        const string P = "Assets/_Project/Art/Props/";
        const string O = P + "Office/", M = P + "Memorial/", B = P + "Basement/", A = P + "Archive/";
        const string Pre = "Assets/_Project/Prefabs/";
        const string Data = "Assets/_Project/Data/";
        const string Mats = "Assets/_Project/Materials/";
        const float G = 0f, F1 = 4f, AR = 7.5f, SB = -4.5f, PIT = -6.5f;
        // escalera de servicio: de la sala de ordenadores (x 10, primera planta) al archivo (x 17), pegada a la pared norte
        const float SX0 = 10f, SX1 = 17f, SZ0 = 29.95f, SZ1 = 31.8f;

        static Transform rec, props, stairRoot;
        static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Material wall, floorM, metal, wood, ceil;
        static readonly List<string> log = new List<string>();

        // ------------------------------------------------------------------ utilidades
        static GameObject Place(Transform parent, string path, Vector3 pos, float yaw, bool collider = true, bool tint = true)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { log.Add("falta " + path); return null; }
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(parent);
            holder.transform.SetPositionAndRotation(pos + new Vector3(R(-0.03f, 0.03f), 0f, R(-0.03f, 0.03f)), Quaternion.Euler(0, yaw + R(-3f, 3f), 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
            m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            if (tint) { float v = R(0.82f, 1.05f); holder.AddComponent<PropVariant>().tint = new Color(v * R(0.96f, 1.03f), v, v * R(0.96f, 1.03f)); }
            foreach (var t in holder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return holder;
        }

        static GameObject Inst(string prefab, Transform parent, Vector3 pos, float yaw, string name)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + prefab);
            if (pf == null) { log.Add("falta " + prefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.name = name;
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material m, float tile, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = c; go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (tile > 0f) go.GetComponent<MeshFilter>().sharedMesh = (Mesh)typeof(TestSceneBuilder).GetMethod("TiledUnitCube", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { c, s, tile });
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        static void Rail(Transform parent, Vector3 a, Vector3 b)
        {
            var g = new GameObject("Barandilla").transform; g.SetParent(parent);
            Vector3 d = b - a; float len = d.magnitude;
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.2f) + 1);
            for (int i = 0; i < posts; i++) Box("Pie", g, Vector3.Lerp(a, b, i / (float)(posts - 1)) + Vector3.up * 0.5f, new Vector3(0.05f, 1.0f, 0.05f), metal, 0f, false);
            var rot = Quaternion.LookRotation(d / len);
            foreach (float hy in new[] { 1.0f, 0.5f }) { var bar = Box("Barra", g, (a + b) / 2f + Vector3.up * hy, new Vector3(0.05f, 0.05f, len), hy > 0.9f ? wood : metal, 0f, false); bar.transform.rotation = rot; }
            var col = new GameObject("Colision"); col.transform.SetParent(g);
            col.transform.SetPositionAndRotation((a + b) / 2f + Vector3.up * 0.6f, rot);
            col.AddComponent<BoxCollider>().size = new Vector3(0.08f, 1.2f, len);
        }

        /// <summary>Quita de una losa (trozos con ese nombre) el rectangulo 'hole' y rehace el resto.</summary>
        static void CutSlab(Transform level, string slabName, Rect hole, Material m, float tile)
        {
            foreach (var f in level.GetComponentsInChildren<Transform>().Where(t => t.name == slabName).ToList())
            {
                var b = f.GetComponent<Renderer>().bounds;
                var r = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
                if (!hole.Overlaps(r)) continue;
                float y0 = b.min.y, y1 = b.max.y;
                Object.DestroyImmediate(f.gameObject);
                var xs = new List<float> { r.xMin, r.xMax, Mathf.Clamp(hole.xMin, r.xMin, r.xMax), Mathf.Clamp(hole.xMax, r.xMin, r.xMax) }.Distinct().OrderBy(v => v).ToList();
                var zs = new List<float> { r.yMin, r.yMax, Mathf.Clamp(hole.yMin, r.yMin, r.yMax), Mathf.Clamp(hole.yMax, r.yMin, r.yMax) }.Distinct().OrderBy(v => v).ToList();
                for (int i = 0; i < xs.Count - 1; i++)
                    for (int j = 0; j < zs.Count - 1; j++)
                    {
                        var cell = Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]);
                        if (cell.width < 0.01f || cell.height < 0.01f || hole.Contains(cell.center)) continue;
                        Box(slabName, level, new Vector3(cell.center.x, (y0 + y1) / 2f, cell.center.y), new Vector3(cell.width, y1 - y0, cell.height), m, tile);
                    }
            }
        }

        static ItemData Item(string n) => AssetDatabase.LoadAssetAtPath<ItemData>(Data + n + ".asset");

        static ItemData MakeItem(string name, string display, ItemType type, Color tint, string description, string objective, GameObject world)
        {
            var it = Item(name);
            if (it == null) { it = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(it, Data + name + ".asset"); }
            it.displayName = display; it.type = type; it.tint = tint; it.description = description; it.pickupObjective = objective;
            if (world != null) it.worldPrefab = world;
            EditorUtility.SetDirty(it);
            return it;
        }

        /// <summary>Punto sobre la primera superficie bajo (x, yTop, z).</summary>
        static Vector3 Surface(float x, float yTop, float z)
        {
            var hits = Physics.RaycastAll(new Vector3(x, yTop, z), Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
            return hits.Length > 0 ? hits[0].point : new Vector3(x, yTop - 1f, z);
        }

        static void Put(ItemData item, int n, float x, float yTop, float z)
        {
            if (item == null) { log.Add("falta un objeto"); return; }
            var p = Surface(x, yTop, z) + Vector3.up * 0.12f;
            var pk = Pickup.Spawn(item, n, p);
            pk.transform.SetParent(rec);
        }

        static Material paperMat;
        static void Paper(string name, NoteData n, float x, float yTop, float z, float yaw)
        {
            var p = Surface(x, yTop, z);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(rec);
            go.transform.SetPositionAndRotation(p + Vector3.up * 0.003f, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = new Vector3(0.21f, 0.004f, 0.297f);
            go.GetComponent<Renderer>().sharedMaterial = paperMat;
            go.GetComponent<BoxCollider>().size = new Vector3(1.4f, 12f, 1.2f);
            go.AddComponent<ReadableNote>().note = n;
        }

        static void Zombie(string prefab, float x, float y, float z, float yaw)
        {
            var go = Inst("Characters/" + prefab + ".prefab", rec, new Vector3(x, y + 0.05f, z), yaw, "Z_" + prefab);
            if (go != null) go.transform.SetParent(zRoot);
        }
        static Transform zRoot;

        // ------------------------------------------------------------------ 1. planta baja
        static void GroundRooms()
        {
            // sala de espera (x -20..-8, z 0..10)
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(-19.55f, G, 3.6f), 90f);
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(-19.55f, G, 7.2f), 90f);
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(-14.0f, G, 0.5f), 0f);
            Place(props, M + "PottedPlant.fbx", new Vector3(-19.3f, G, 9.3f), R(0, 360));
            Place(props, M + "PottedPlant.fbx", new Vector3(-8.7f, G, 0.7f), R(0, 360));
            Place(props, O + "WaterCooler.fbx", new Vector3(-11.0f, G, 0.45f), 0f);
            Place(props, O + "TrashBin.fbx", new Vector3(-12.2f, G, 0.5f), 0f);
            // sala segura (x -20..-8, z 10..16; puerta en x -14)
            Inst("Interactables/SaveTerminal.prefab", rec, new Vector3(-19.4f, G, 13.0f), 90f, "SaveTerminal_Baja");
            Inst("Interactables/ItemBox.prefab", rec, new Vector3(-9.3f, G, 12.6f), -90f, "ItemBox_Baja");
            Place(props, P + "Cot.fbx", new Vector3(-16.6f, G, 15.3f), 90f);
            Place(props, P + "Desk.fbx", new Vector3(-11.6f, G, 15.3f), 180f);
            Place(props, P + "Chair.fbx", new Vector3(-11.6f, G, 14.4f), R(-20, 20));
            Place(props, O + "DeskLamp.fbx", new Vector3(-12.1f, G + 0.76f, 15.4f), 160f, false, false);
            // oficina este (x 8..20, z 0..8)
            Place(props, P + "Desk.fbx", new Vector3(15.0f, G, 2.4f), 180f); Place(props, O + "SwivelChair.fbx", new Vector3(15.0f, G, 3.3f), 170f);
            Place(props, O + "DeskComputer.fbx", new Vector3(15.0f, G + 0.76f, 2.3f), 180f, false, false);
            Place(props, P + "Desk.fbx", new Vector3(11.0f, G, 2.4f), 180f); Place(props, P + "Chair.fbx", new Vector3(11.0f, G, 3.3f), 200f);
            foreach (float z in new[] { 1.0f, 1.62f, 2.24f }) Place(props, P + "FilingCabinet.fbx", new Vector3(19.55f, G, z), -90f);
            Place(props, P + "Shelf.fbx", new Vector3(19.5f, G, 5.6f), -90f);
            // vestibulo del ascensor (x 8..17, z 8..16)
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(8.5f, G, 12.0f), 90f);
            Place(props, M + "PottedPlant.fbx", new Vector3(8.7f, G, 15.3f), R(0, 360));
            // vestuarios (x -20..0, z 16..32): taquillas en la pared norte y bancos en medio
            for (int i = 0; i < 11; i++) Place(props, P + "Locker.fbx", new Vector3(-18.6f + i * 0.62f, G, 31.5f), 180f);
            for (int i = 0; i < 9; i++) Place(props, P + "Locker.fbx", new Vector3(-7.5f + i * 0.62f, G, 31.5f), 180f);
            for (int i = 0; i < 6; i++) Place(props, P + "Locker.fbx", new Vector3(-19.55f, G, 18.0f + i * 0.62f), 90f);
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(-13.0f, G, 25.0f), 0f);
            Place(props, P + "Exterior/WaitingBench.fbx", new Vector3(-7.0f, G, 25.0f), 0f);
            // calabozos (x 0..20, z 16..32): tres celdas al norte (rejas en z 27) y una mesa de interrogatorio
            var cells = new GameObject("Celdas").transform; cells.SetParent(rec);
            float cz = 27f;
            float[] doorsX = { 5.0f, 10.6f, 16.2f };
            for (float x = 1.0f; x < 19.8f; x += 0.16f)
            {
                if (doorsX.Any(d => Mathf.Abs(x - d) < 0.68f)) continue;
                Box("Barrote", cells, new Vector3(x, G + 1.3f, cz), new Vector3(0.035f, 2.6f, 0.035f), metal, 0f, false);
            }
            foreach (float y in new[] { 0.06f, 1.2f, 2.55f }) Box("Travesano", cells, new Vector3(10.4f, G + y, cz), new Vector3(18.8f, 0.06f, 0.06f), metal, 0f, false);
            // colisiones de las rejas (huecos de puerta abiertos)
            var xs = new List<float> { 1.0f }; foreach (var d in doorsX) { xs.Add(d - 0.65f); xs.Add(d + 0.65f); } xs.Add(19.8f);
            for (int i = 0; i + 1 < xs.Count; i += 2) { var c = new GameObject("Reja_Col"); c.transform.SetParent(cells); c.transform.position = new Vector3((xs[i] + xs[i + 1]) / 2f, G + 1.3f, cz); c.AddComponent<BoxCollider>().size = new Vector3(xs[i + 1] - xs[i], 2.6f, 0.1f); c.isStatic = true; }
            foreach (float x in new[] { 7.8f, 13.4f }) Box("Celda_Muro", cells, new Vector3(x, G + 1.5f, 29.5f), new Vector3(0.2f, 3.0f, 5.0f), wall, 3f);
            foreach (float x in new[] { 3.5f, 10.6f, 16.2f }) Place(props, P + "Cot.fbx", new Vector3(x, G, 31.0f), 0f);
            Place(props, P + "Desk.fbx", new Vector3(6.0f, G, 20.5f), 0f);
            Place(props, P + "Chair.fbx", new Vector3(6.0f, G, 19.6f), 0f); Place(props, P + "Chair.fbx", new Vector3(6.0f, G, 21.4f), 180f);
            Place(props, O + "DeskLamp.fbx", new Vector3(6.3f, G + 0.76f, 20.4f), 30f, false, false);
        }

        // ------------------------------------------------------------------ 3. escalera de servicio (una vez)
        static ServiceGate ServiceStair(Transform level, RuntimeNavMesh rt, out Door topDoor)
        {
            topDoor = null;
            var existing = level.Find("Escalera_Servicio");
            if (existing != null) { topDoor = existing.GetComponentInChildren<Door>(); log.Add("escalera de servicio ya hecha"); return existing.GetComponentInChildren<ServiceGate>(); }
            stairRoot = new GameObject("Escalera_Servicio").transform; stairRoot.SetParent(level);
            var hole = Rect.MinMaxRect(SX0, SZ0, SX1, 32f);
            CutSlab(level, "Primera_Ceiling", hole, ceil, 2.4f);
            CutSlab(level, "Archivo_Floor", hole, floorM, 4f);
            // peldanos (decorado, macizos hasta el suelo) y rampa invisible
            int n = 17; float run = (SX1 - SX0) / n, rise = (AR - F1) / n, zc = (SZ0 + SZ1) / 2f, w = SZ1 - SZ0;
            for (int k = 0; k < n; k++)
            {
                float top = F1 + rise * (k + 0.5f), x0 = SX0 + run * k;
                Box("Peldano", stairRoot, new Vector3(x0 + run / 2f, (top + F1) / 2f, zc), new Vector3(run, top - F1, w), metal, 1f, false);
            }
            float len = Mathf.Sqrt((SX1 - SX0) * (SX1 - SX0) + (AR - F1) * (AR - F1)), ang = Mathf.Atan2(AR - F1, SX1 - SX0) * Mathf.Rad2Deg;
            var ramp = new GameObject("Rampa"); ramp.transform.SetParent(stairRoot);
            ramp.transform.SetPositionAndRotation(new Vector3((SX0 + SX1) / 2f, (F1 + AR) / 2f + 0.02f, zc), Quaternion.Euler(0, 0, ang));
            var rc = ramp.AddComponent<BoxCollider>(); rc.size = new Vector3(len, 0.1f, w); rc.center = new Vector3(0, -0.05f, 0); ramp.isStatic = true;
            // barandillas: lado abierto de la escalera (primera planta) y borde del hueco (archivo)
            Rail(stairRoot, new Vector3(SX0, F1, SZ0 - 0.05f), new Vector3(SX1, AR, SZ0 - 0.05f));
            Rail(stairRoot, new Vector3(SX0, AR, SZ0 - 0.05f), new Vector3(SX1, AR, SZ0 - 0.05f));
            Rail(stairRoot, new Vector3(SX0, AR, SZ0 - 0.05f), new Vector3(SX0, AR, 31.8f));
            // reja abajo (se abre con el memorial)
            var gate = new GameObject("Reja_Servicio"); gate.transform.SetParent(stairRoot);
            gate.transform.position = new Vector3(SX0 - 0.3f, F1, zc);
            var bars = new GameObject("Barrotes").transform; bars.SetParent(gate.transform, false);
            for (float z = SZ0 + 0.05f; z < SZ1; z += 0.14f) Box("Barrote", bars, new Vector3(SX0 - 0.3f, F1 + 1.3f, z), new Vector3(0.04f, 2.6f, 0.04f), metal, 0f, false);
            foreach (float y in new[] { 0.1f, 1.3f, 2.55f }) Box("Travesano", bars, new Vector3(SX0 - 0.3f, F1 + y, zc), new Vector3(0.06f, 0.06f, w), metal, 0f, false);
            var gcol = gate.AddComponent<BoxCollider>(); gcol.center = new Vector3(0, 1.3f, 0); gcol.size = new Vector3(0.12f, 2.6f, w);
            var obs = gate.AddComponent<NavMeshObstacle>(); obs.carving = true; obs.shape = NavMeshObstacleShape.Box; obs.center = new Vector3(0, 1.3f, 0); obs.size = new Vector3(0.3f, 2.6f, w);
            var sg = gate.AddComponent<ServiceGate>(); sg.bars = bars;
            Box("Reja_Marco", stairRoot, new Vector3(SX0 - 0.3f, F1 + 2.75f, zc), new Vector3(0.12f, 0.3f, w + 0.1f), metal, 0f, false);
            // cuarto de llegada en el archivo, con puerta (se atranca durante el combate con el jefe 1)
            float wx0 = SX0 - 0.4f, wx1 = 19.83f, wz = 28.6f, top2 = 14.0f;
            float doorX = 18.4f;
            Box("CuartoEsc_S_O", stairRoot, new Vector3((wx0 + doorX - 0.8f) / 2f, (AR + top2) / 2f, wz), new Vector3(doorX - 0.8f - wx0, top2 - AR, 0.25f), wall, 3f);
            Box("CuartoEsc_S_E", stairRoot, new Vector3((doorX + 0.8f + wx1) / 2f, (AR + top2) / 2f, wz), new Vector3(wx1 - doorX - 0.8f, top2 - AR, 0.25f), wall, 3f);
            Box("CuartoEsc_Dintel", stairRoot, new Vector3(doorX, (AR + 2.45f + top2) / 2f, wz), new Vector3(1.6f, top2 - AR - 2.45f, 0.25f), wall, 3f);
            Box("CuartoEsc_O", stairRoot, new Vector3(wx0, (AR + top2) / 2f, (wz + 32f) / 2f), new Vector3(0.25f, top2 - AR, 32f - wz), wall, 3f);
            var door = Inst("Doors/Door_Wood_150.prefab", stairRoot, new Vector3(doorX - 0.75f, AR, wz), 0f, "Puerta_Escalera_Archivo");
            topDoor = door.GetComponentInChildren<Door>();
            var leaves = (rt.disableDuringBake ?? new GameObject[0]).Where(g => g != null).ToList();
            leaves.Add(topDoor.gameObject); leaves.Add(gate);
            rt.disableDuringBake = leaves.Distinct().ToArray();
            EditorUtility.SetDirty(rt);
            var lamp = new GameObject("Luz_Escalera").AddComponent<Light>(); lamp.transform.SetParent(stairRoot);
            lamp.transform.position = new Vector3(15f, AR + 2.6f, 30.9f); lamp.type = LightType.Point; lamp.range = 8f; lamp.intensity = 2.5f; lamp.color = new Color(1f, 0.85f, 0.65f);
            // las estanterias de obra del archivo que quedaban sobre el hueco se quitan
            var shelves = level.Find("Archivo_Estanterias");
            if (shelves != null) foreach (var t in shelves.Cast<Transform>().ToList()) { var p = t.position; if (p.z > 31f && p.x > SX0 - 2.2f && p.x < 19.9f) Object.DestroyImmediate(t.gameObject); }
            log.Add("escalera de servicio hecha");
            return sg;
        }

        // ------------------------------------------------------------------ menu
        [MenuItem("Horror/Comisaria v2/6 Recorrido")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            log.Clear();
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaV2Builder.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaV2Builder.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---").transform;
            wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            floorM = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Floor.mat");
            ceil = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Ceiling.mat");
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            paperMat = AssetDatabase.LoadAssetAtPath<Material>(Mats + "NotePaper.mat") ?? wood;
            foreach (var nm in new[] { "Recorrido", "Props/Recorrido_Props" }) { var old = level.Find(nm); if (old != null) Object.DestroyImmediate(old.gameObject); }
            rec = new GameObject("Recorrido").transform; rec.SetParent(level);
            props = new GameObject("Recorrido_Props").transform; props.SetParent(level.Find("Props"));
            zRoot = new GameObject("Zombis").transform; zRoot.SetParent(rec);
            rng = new System.Random(71);
            var rt = level.GetComponent<RuntimeNavMesh>();

            // ---- objetos nuevos: medallones y llave del ascensor
            var medalModel = AssetDatabase.LoadAssetAtPath<GameObject>(M + "MemorialMedallion.fbx");
            string medalObj = "Lleva los medallones al monumento del memorial (primera planta, donde sale la escalera de caracol).";
            var medals = new[]
            {
                MakeItem("I_MedalA", "Medallon de bronce (I)", ItemType.Key, new Color(0.85f, 0.65f, 0.3f), "Medallon con la estrella de la policia. Encaja en uno de los huecos del monumento del memorial.", medalObj, medalModel),
                MakeItem("I_MedalB", "Medallon de bronce (II)", ItemType.Key, new Color(0.85f, 0.65f, 0.3f), "Medallon con la estrella de la policia. Encaja en uno de los huecos del monumento del memorial.", medalObj, medalModel),
                MakeItem("I_MedalC", "Medallon de bronce (III)", ItemType.Key, new Color(0.85f, 0.65f, 0.3f), "Medallon con la estrella de la policia. Encaja en uno de los huecos del monumento del memorial.", medalObj, medalModel),
            };
            var keyExit = Item("I_KeyExit");
            var elevKey = MakeItem("I_KeyElevator", "Llave del ascensor", ItemType.Key, new Color(0.35f, 0.8f, 1f),
                "Llave de la botonera del ascensor de carga. Lo pone en marcha.", "Usa la llave en el ascensor de carga (planta baja, ala este) para bajar al sotano.", keyExit != null ? keyExit.worldPrefab : null);
            var keyFinal = Item("I_KeyFinal");
            if (keyFinal != null) { keyFinal.description = "La llave maestra. Abre el porton del tunel de servicio de la sala de calderas."; keyFinal.pickupObjective = "Abre el porton del tunel de servicio, en la pared oeste de la sala de calderas."; EditorUtility.SetDirty(keyFinal); }
            foreach (var it in medals.Concat(new[] { elevKey })) it.worldScale = 1f;
            AssetDatabase.SaveAssets();
            ItemIcons.Generate(medals.Concat(new[] { elevKey }).ToArray());

            // ---- notas
            var nMemorial = ArchiveSetup.Note("note_v2_memorial", "Aviso del sargento", NoteCategory.Puzzle,
                "A todo el personal:\n\nHe cerrado la escalera de servicio del archivo con la reja del memorial. Solo se abre con los tres medallones de la placa en el monumento.\n\nLos repartimos para que nadie suba solo: uno lo tiene el comisario en su despacho, otro quedo en el estrado de la sala de conferencias y el tercero lo guardo Henderson en los calabozos.\n\nAhi arriba hay algo. No subais.",
                "tres medallones", "Busca los tres medallones: despacho del comisario, sala de conferencias y calabozos.");
            var nArchive = ArchiveSetup.Note("note_v2_archivo", "Nota del archivero", NoteCategory.Story,
                "Dia 3.\n\nCerre por dentro. Desde la escalera se oye como arrastra las estanterias. Era Morrison, el de mantenimiento. Lleva colgada la llave del ascensor de carga; sin ella no se baja al sotano.\n\nSi alguien lee esto: el ascensor esta en la planta baja, ala este.",
                "llave del ascensor", "");
            var nBasement = ArchiveSetup.Note("note_v2_sotano", "Parte de mantenimiento", NoteCategory.Story,
                "El tunel de servicio sale desde la sala de calderas a la calle de atras. El porton se abre con la llave maestra del jefe de turno.\n\nEl jefe de turno bajo a revisar la caldera hace dos dias y no ha vuelto a subir. Desde entonces la luz de emergencia esta encendida y algo golpea los armarios electricos.",
                "llave maestra", "");
            var db = Object.FindFirstObjectByType<ItemDatabase>();
            if (db != null)
            {
                var its = (db.items ?? new ItemData[0]).Where(i => i != null).ToList();
                foreach (var k in medals.Concat(new[] { elevKey, keyFinal })) if (k != null && !its.Contains(k)) its.Add(k);
                db.items = its.ToArray();
                var notes = (db.notes ?? new NoteData[0]).Where(n => n != null).ToList();
                foreach (var n in new[] { nMemorial, nArchive, nBasement }) if (!notes.Contains(n)) notes.Add(n);
                db.notes = notes.ToArray();
                EditorUtility.SetDirty(db);
            }
            else log.Add("AVISO: no hay ItemDatabase");

            // ---- 1. planta baja y 3. escalera de servicio
            GroundRooms();
            var gate = ServiceStair(level, rt, out var topDoor);

            // ---- 2. monumento del memorial
            var monument = level.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Memorial_Monumento");
            if (monument != null)
            {
                foreach (var c in monument.GetComponents<MedallionMonument>()) Object.DestroyImmediate(c);
                foreach (var t in monument.Cast<Transform>().Where(t => t.name.StartsWith("Medallon_Puesto")).ToList()) Object.DestroyImmediate(t.gameObject);
                var mm = monument.gameObject.AddComponent<MedallionMonument>();
                mm.medallions = medals;
                mm.placedVisuals = new GameObject[3];
                float[] sx = { -0.27f, 0f, 0.27f };
                for (int i = 0; i < 3; i++)
                {
                    var v = new GameObject("Medallon_Puesto_" + i); v.transform.SetParent(monument, false);
                    v.transform.localPosition = new Vector3(sx[i], 1.66f, 0.2f);
                    var mdl = (GameObject)PrefabUtility.InstantiatePrefab(medalModel, v.transform);
                    mdl.transform.localPosition = Vector3.zero;
                    v.SetActive(false);
                    mm.placedVisuals[i] = v;
                }
                EditorUtility.SetDirty(mm);
            }
            else log.Add("AVISO: no encuentro el monumento");

            // ---- 3b. jefe 1: el disparador pasa a la salida del cuarto de la escalera y suelta la llave del ascensor
            var boss1 = level.Find("Archivo_Jefe/Boss");
            var trig1 = level.Find("Archivo_Jefe/BossRoomTrigger");
            if (boss1 != null && trig1 != null)
            {
                var ai1 = boss1.GetComponent<ZombieAI>(); ai1.dropOnDeath = elevKey; EditorUtility.SetDirty(ai1); PrefabUtility.RecordPrefabInstancePropertyModifications(ai1);
                trig1.position = new Vector3(18.4f, AR + 1.5f, 27.5f);
                var bc = trig1.GetComponent<BoxCollider>(); bc.size = new Vector3(3.2f, 3f, 1.4f);
                var brt = trig1.GetComponent<BossRoomTrigger>(); brt.sealDoors = topDoor != null ? new[] { topDoor } : new Door[0];
                EditorUtility.SetDirty(brt);
                boss1.rotation = Quaternion.Euler(0, 45f, 0);       // mirando hacia el cuarto de la escalera
            }
            else log.Add("AVISO: falta el jefe 1");

            // ---- 4. ascensor: botoneras (planta baja <-> sotano)
            ElevatorPanel Panel(string name, float y, string dest)
            {
                var p = Box(name, rec, new Vector3(16.86f, y + 1.3f, 14.75f), new Vector3(0.06f, 0.5f, 0.32f), metal, 0f);
                Box("Boton", rec, new Vector3(16.82f, y + 1.38f, 14.75f), new Vector3(0.03f, 0.07f, 0.07f), AssetDatabase.LoadAssetAtPath<Material>(Mats + "Bulb_FFF2D9.mat") ?? metal, 0f, false);
                Box("Cerradura", rec, new Vector3(16.82f, y + 1.18f, 14.75f), new Vector3(0.03f, 0.05f, 0.05f), wood, 0f, false);
                var ep = p.AddComponent<ElevatorPanel>(); ep.requiredKey = elevKey; ep.destinationName = dest;
                ep.unlockedObjective = "Baja al sotano en el ascensor. La salida esta mas alla de la sala de calderas.";
                return ep;
            }
            var pGround = Panel("Ascensor_Botonera_Baja", G, "sotano");
            var pBasement = Panel("Ascensor_Botonera_Sotano", SB, "planta baja");
            Transform Arrival(string name, float y) { var a = new GameObject(name).transform; a.SetParent(rec); a.SetPositionAndRotation(new Vector3(15.6f, y + 1.05f, 13.5f), Quaternion.Euler(0, -90f, 0)); return a; }
            pGround.arrival = Arrival("Ascensor_Llegada_Sotano", SB);
            pBasement.arrival = Arrival("Ascensor_Llegada_Baja", G);

            // ---- 5. porton del tunel de servicio (pared oeste del foso de calderas)
            var gateExit = Box("Porton_Tunel", rec, new Vector3(-19.72f, PIT + 1.3f, 26.3f), new Vector3(0.14f, 2.6f, 4.2f), metal, 1.5f);
            gateExit.AddComponent<ExitDoor>().requiredKey = keyFinal;
            Box("Porton_Marco_N", rec, new Vector3(-19.7f, PIT + 1.35f, 28.45f), new Vector3(0.2f, 2.7f, 0.16f), metal, 1f, false);
            Box("Porton_Marco_S", rec, new Vector3(-19.7f, PIT + 1.35f, 24.15f), new Vector3(0.2f, 2.7f, 0.16f), metal, 1f, false);
            Box("Porton_Marco_T", rec, new Vector3(-19.7f, PIT + 2.72f, 26.3f), new Vector3(0.2f, 0.16f, 4.46f), metal, 1f, false);
            var exitLight = new GameObject("Luz_Porton").AddComponent<Light>(); exitLight.transform.SetParent(rec);
            exitLight.transform.position = new Vector3(-18.8f, PIT + 3.2f, 26.3f); exitLight.type = LightType.Point; exitLight.range = 6f; exitLight.intensity = 3f; exitLight.color = new Color(0.5f, 1f, 0.55f);

            // ---- 6. salas seguras de las otras plantas
            Inst("Interactables/SaveTerminal.prefab", rec, new Vector3(4.3f, F1, 31.4f), 180f, "SaveTerminal_Ordenadores");
            Inst("Interactables/ItemBox.prefab", rec, new Vector3(2.8f, F1, 31.4f), 180f, "ItemBox_Ordenadores");
            Inst("Interactables/SavePhone.prefab", rec, new Vector3(14.0f, SB, 12.35f), 0f, "SavePhone_Sotano");
            Inst("Interactables/ItemBox.prefab", rec, new Vector3(15.6f, SB, 12.4f), 0f, "ItemBox_Sotano");

            // ---- botin, medallones y notas (se dejan caer con la fisica sobre las superficies)
            Physics.SyncTransforms();
            var pistolAmmo = Item("I_HandgunAmmo"); var sgAmmo = Item("I_ShotgunAmmo"); var spray = Item("I_Spray"); var shotgun = Item("I_Shotgun"); var bag = Item("I_Bag");
            // planta baja
            Put(pistolAmmo, 12, -2.6f, 1.6f, 12.5f);                // mostrador de recepcion
            Put(spray, 1, -11.6f, 1.6f, 15.3f); Put(pistolAmmo, 12, -11.2f, 1.6f, 15.2f);   // sala segura
            Put(pistolAmmo, 12, 11.0f, 1.6f, 2.4f);                 // oficina este
            Put(bag, 1, -13.0f, 1.6f, 25.0f);                       // vestuarios: rinonera en un banco
            Put(medals[2], 1, 10.6f, 1.6f, 31.0f);                   // calabozos: en el catre de la celda del medio
            Put(spray, 1, 6.0f, 1.6f, 20.5f);                       // mesa de interrogatorio
            // primera planta
            Put(medals[0], 1, -15.4f, F1 + 1.6f, 7.7f);              // mesa del comisario
            Put(shotgun, 1, -11.8f, F1 + 1.6f, 2.9f); Put(sgAmmo, 8, -11.3f, F1 + 1.6f, 3.2f);   // mesa de reuniones del despacho
            Put(medals[1], 1, 17.0f, F1 + 1.6f, 7.2f);               // estrado de conferencias
            Put(pistolAmmo, 12, 12.0f, F1 + 1.6f, 5.0f);             // sala de conferencias (una silla)
            Put(pistolAmmo, 12, -15.6f, F1 + 1.6f, 20.4f); Put(spray, 1, 3.6f, F1 + 1.6f, 19.6f); Put(sgAmmo, 6, 9.6f, F1 + 1.6f, 26.6f);   // ordenadores
            // archivo
            Put(sgAmmo, 8, 3.2f, AR + 1.6f, 26.3f); Put(spray, 1, 2.6f, AR + 1.6f, 26.5f); Put(pistolAmmo, 12, 18.6f, AR + 1.6f, 30.6f);
            // sotano
            Put(spray, 1, 13.2f, SB + 1.6f, 12.6f); Put(pistolAmmo, 12, 12.6f, SB + 1.6f, 12.8f);      // junto al ascensor
            Put(sgAmmo, 8, -19.2f, SB + 1.6f, 9.5f);                // banco del cuarto de bombas
            Put(pistolAmmo, 12, 14.0f, SB + 1.6f, 11.2f);            // banco de la sala de maquinas
            Put(spray, 1, -2.4f, SB + 1.6f, 21.5f);                  // laboratorio
            Put(sgAmmo, 8, 10.8f, SB + 1.6f, 18.0f); Put(sgAmmo, 6, 11.2f, SB + 1.6f, 28.4f);           // almacen
            Put(sgAmmo, 8, -7.0f, PIT + 1.6f, 17.4f); Put(spray, 1, -19.3f, PIT + 1.6f, 21.0f); Put(pistolAmmo, 12, -7.0f, PIT + 1.6f, 31.2f);   // foso
            Paper("Nota_Memorial", nMemorial, 0.6f, F1 + 1.6f, 4.6f, -80f);       // banco del memorial
            Paper("Nota_Archivero", nArchive, 2.8f, AR + 1.6f, 26.1f, 15f);       // mesa del archivero
            Paper("Nota_Sotano", nBasement, 12.4f, SB + 1.6f, 15.5f, 20f);        // junto al ascensor del sotano
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;

            // ---- zombis
            Zombie("Zombie_Civil", -14.0f, G, 5.0f, 40f);
            Zombie("Zombie_Ejecutivo", 13.5f, G, 5.5f, 200f);
            Zombie("Zombie_Policia", -10.0f, G, 21.0f, 120f);
            Zombie("Zombie_Infectado", 8.0f, G, 23.0f, 250f);
            Zombie("Zombie_Paciente", 16.0f, G, 29.5f, 180f);
            Zombie("Zombie_Oficial", -6.0f, F1, 13.5f, 140f);
            Zombie("Zombie_Cop", -12.0f, F1, 23.5f, 90f);
            Zombie("Zombie_Civil", 6.5f, F1, 23.5f, 200f);
            Zombie("Zombie_OficialReptante", 12.5f, F1, 29.0f, 230f);
            Zombie("Zombie_Girl", 12.0f, F1, 9.0f, 270f);
            Zombie("Zombie_Ejecutivo", -17.0f, F1, 13.0f, 60f);
            Zombie("Zombie_Mecanico", -12.0f, SB, 9.0f, 30f);
            Zombie("Zombie_Policia", 10.0f, SB, 8.0f, 160f);
            Zombie("Zombie_Infectado", 16.5f, SB, 2.0f, 300f);
            Zombie("Zombie_Paciente", -1.0f, SB, 23.5f, 180f);
            Zombie("Zombie_Cop", 10.7f, SB, 22.8f, 90f);

            // ---- objetivo al entrar
            var seal = Object.FindFirstObjectByType<SealOnEnter>();
            if (seal != null) { seal.objective = "Explora la comisaria y busca una salida."; EditorUtility.SetDirty(seal); }

            // ---- NavMesh
            var surface = level.GetComponent<NavMeshSurface>();
            surface.overrideVoxelSize = true; surface.voxelSize = 0.1f;
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            int pickups = rec.GetComponentsInChildren<Pickup>().Length;
            return "fase 6: " + pickups + " objetos, " + zRoot.childCount + " zombis, " + props.childCount + " muebles en la planta baja | " + string.Join(" | ", log);
        }
    }
}
#endif
