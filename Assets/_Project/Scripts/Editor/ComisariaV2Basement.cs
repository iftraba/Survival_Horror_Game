#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria v2, fase 5: el sotano y el segundo jefe (menu Horror/Comisaria v2/5 Sotano y jefe 2). Repetible: rehace
    /// "Props/Sotano_Props", "Sotano_Calderas" y "Sotano_Jefe".
    ///  - Las cuatro salas del sotano son las instalaciones de la antigua zona 2: cuarto de bombas (suroeste), sala de maquinas
    ///    (sureste), laboratorio (norte) y almacen (noreste).
    ///  - La sala de calderas (x -20..-6, z 12..32) se hunde 2 m: queda un foso de 5,5 m de alto como la arena antigua. Desde su
    ///    puerta se sale a un rellano con barandilla y se baja por una escalera. En el foso, la caldera, filas de armarios electricos
    ///    y grupos electrogenos como cobertura, colectores de tuberias y luz roja de emergencia.
    ///  - El jefe 2 (Zombie_BossPxl: embestida, escupitajo, fases) espera al fondo; despierta al pisar el rellano, la puerta se
    ///    atranca hasta que muere y suelta la llave maestra.
    /// </summary>
    public static class ComisariaV2Basement
    {
        const string B = "Assets/_Project/Art/Props/Basement/";
        const string A = "Assets/_Project/Art/Props/Archive/";
        const string O = "Assets/_Project/Art/Props/Office/";
        const string P = "Assets/_Project/Art/Props/";
        const string Mats = "Assets/_Project/Materials/";
        static readonly float Y = ComisariaV2Builder.BasementY;    // -4,5
        const float PitY = -6.5f;                                   // suelo del foso de la sala de calderas
        static readonly Rect Pit = Rect.MinMaxRect(-20f, 12f, -6f, 32f);
        static readonly Rect Landing = Rect.MinMaxRect(-9.5f, 12f, -6f, 16.2f);
        const float StairZ0 = 12.15f, StairZ1 = 14.4f, StairX1 = -13.5f;   // la escalera baja hacia el oeste desde x -9,5

        static Transform root, solid;
        static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Material wall, floorM, metal, wood;

        static GameObject Place(Transform parent, string path, Vector3 pos, float yaw, bool collider = true, float jitter = 1f, bool tint = true)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[Horror] falta " + path); return null; }
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(parent);
            holder.transform.SetPositionAndRotation(pos + new Vector3(R(-0.04f, 0.04f), 0f, R(-0.04f, 0.04f)) * jitter, Quaternion.Euler(0, yaw + R(-2.5f, 2.5f) * jitter, 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
            m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            if (tint)
            {
                float v = R(0.8f, 1.05f);
                holder.AddComponent<PropVariant>().tint = new Color(v * R(0.96f, 1.03f), v, v * R(0.96f, 1.03f));
            }
            foreach (var t in holder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return holder;
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

        // ------------------------------------------------------------------ salas
        static Vector3 V(float x, float z) => new Vector3(x, Y, z);

        static void Rooms()
        {
            // cuarto de bombas (x -20..3, z 0..12; puerta en x -2, z 12)
            foreach (float x in new[] { -17.0f, -13.4f, -9.8f }) Place(root, B + "WaterPump.fbx", V(x, 6.0f), 0f);
            Place(root, B + "WaterPump.fbx", V(-6.2f, 6.0f), 180f);
            foreach (float x in new[] { -15.5f, -8.5f, -1.5f }) Place(root, B + "PipeValves.fbx", V(x, 0.35f), 0f, true, 0.3f);
            Place(root, B + "Workbench.fbx", V(-19.2f, 9.5f), 90f);
            Place(root, B + "MetalRack.fbx", V(1.4f, 1.3f), 0f);
            foreach (var (x, z) in new[] { (-19.2f, 2.0f), (-18.5f, 1.6f), (-19.3f, 3.0f), (2.4f, 8.0f) }) Place(root, P + "Barrel.fbx", V(x, z), R(0, 360));
            Place(root, P + "Crate.fbx", V(-11.0f, 10.6f), 15f); Place(root, P + "Crate.fbx", V(-10.0f, 11.0f), -20f);
            // sala de maquinas (x 3..20, z 0..12; puerta en x 8, z 12)
            Place(root, B + "Generator.fbx", V(7.0f, 4.0f), 0f);
            Place(root, B + "Generator.fbx", V(12.8f, 4.0f), 0f);
            foreach (float z in new[] { 2.2f, 5.6f, 9.0f }) Place(root, B + "ControlPanel.fbx", V(19.55f, z), -90f, true, 0.3f);
            Place(root, B + "PipeValves.fbx", V(3.3f, 6.5f), 90f, true, 0.3f);
            Place(root, B + "Workbench.fbx", V(14.0f, 11.2f), 180f);
            Place(root, P + "Crate.fbx", V(4.2f, 10.8f), 25f); Place(root, P + "Barrel.fbx", V(16.8f, 0.8f), 0f);
            // laboratorio (x -6..3, z 16..32; puerta en x -2, z 16)
            foreach (float z in new[] { 21.5f, 26.0f }) Place(root, B + "LabBench.fbx", V(-2.4f, z), 0f);
            Place(root, B + "LabBench.fbx", V(-2.4f, 30.9f), 180f);
            foreach (float z in new[] { 18.4f, 19.5f, 23.6f, 24.7f, 28.3f }) Place(root, B + "LabCabinet.fbx", V(-5.6f, z), 90f);
            foreach (float z in new[] { 19.0f, 20.1f }) Place(root, B + "LabCabinet.fbx", V(2.6f, z), -90f);
            Place(root, P + "Desk.fbx", V(1.8f, 27.0f), -90f); Place(root, P + "Chair.fbx", V(1.0f, 27.0f), 90f + R(-20, 20));
            Place(root, O + "DeskLamp.fbx", new Vector3(1.9f, Y + 0.76f, 26.6f), -60f, false, 0.5f, false);
            Place(root, O + "PaperStack.fbx", new Vector3(1.8f, Y + 0.76f, 27.3f), -100f, false, 0.5f, false);
            Place(root, P + "FilingCabinet.fbx", V(2.6f, 30.8f), -90f); Place(root, P + "FilingCabinet.fbx", V(2.6f, 30.2f), -90f);
            Place(root, A + "PaperScatter.fbx", V(-0.5f, 23.8f), R(0, 360), false);
            // almacen (x 3..20, z 16..32; puerta en x 8, z 16)
            foreach (float z in new[] { 20.6f, 25.0f })
                foreach (float x in new[] { 6.0f, 8.5f, 13.0f, 15.5f }) Place(root, B + "MetalRack.fbx", V(x, z), rng.NextDouble() < 0.5 ? 0f : 180f);
            foreach (float x in new[] { 5.0f, 7.5f, 10.0f, 12.5f, 15.0f, 17.5f }) Place(root, B + "MetalRack.fbx", V(x, 31.5f), 180f, true, 0.3f);
            foreach (float z in new[] { 18.5f, 22.8f, 27.2f }) Place(root, B + "MetalRack.fbx", V(19.5f, z), -90f, true, 0.3f);
            foreach (var (x, z) in new[] { (10.8f, 18.0f), (11.2f, 28.4f), (4.0f, 28.0f), (17.0f, 28.6f) }) Place(root, P + "Crate.fbx", V(x, z), R(0, 90));
            foreach (var (x, z) in new[] { (4.2f, 17.0f), (4.8f, 17.4f), (17.8f, 17.0f) }) Place(root, P + "Barrel.fbx", V(x, z), 0f);
            // pasillo: algo de tuberia y cajas pegadas a las paredes
            Place(root, B + "PipeValves.fbx", V(2.0f, 15.75f), 180f, true, 0.3f);
            Place(root, P + "Crate.fbx", V(13.5f, 15.4f), 10f);
        }

        // ------------------------------------------------------------------ sala de calderas hundida
        static void SinkBoilerRoom(Transform level, Transform details)
        {
            // 1) losa del sotano: se quitan los trozos que pisan el foso y se rehacen alrededor (el rellano conserva su suelo)
            var holes = new[] { Rect.MinMaxRect(Pit.xMin, Pit.yMin, Landing.xMin, Pit.yMax), Rect.MinMaxRect(Landing.xMin, Landing.yMax, Pit.xMax, Pit.yMax) };
            var floors = level.GetComponentsInChildren<Transform>().Where(t => t.name == "Sotano_Floor").ToList();
            var removed = new List<Rect>();
            foreach (var f in floors)
            {
                var b = f.GetComponent<Renderer>().bounds;
                var r = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
                if (!holes.Any(hl => hl.Overlaps(r))) continue;
                removed.Add(r);
                Object.DestroyImmediate(f.gameObject);
            }
            foreach (var r in removed)
            {
                var xs = new List<float> { r.xMin, r.xMax }; var zs = new List<float> { r.yMin, r.yMax };
                foreach (var hl in holes) { xs.Add(Mathf.Clamp(hl.xMin, r.xMin, r.xMax)); xs.Add(Mathf.Clamp(hl.xMax, r.xMin, r.xMax)); zs.Add(Mathf.Clamp(hl.yMin, r.yMin, r.yMax)); zs.Add(Mathf.Clamp(hl.yMax, r.yMin, r.yMax)); }
                xs = xs.Distinct().OrderBy(v => v).ToList(); zs = zs.Distinct().OrderBy(v => v).ToList();
                for (int i = 0; i < xs.Count - 1; i++)
                    for (int j = 0; j < zs.Count - 1; j++)
                    {
                        var cell = Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]);
                        if (cell.width < 0.01f || cell.height < 0.01f || holes.Any(hl => hl.Contains(cell.center))) continue;
                        Box("Sotano_Floor", level, new Vector3(cell.center.x, Y - 0.15f, cell.center.y), new Vector3(cell.width, 0.3f, cell.height), floorM, 4f);
                    }
            }
            // 2) los rodapies que quedan colgando en el lado del foso
            foreach (var t in details.GetComponentsInChildren<Transform>().Where(t => t.name.EndsWith("_Base")).ToList())
            {
                var p = t.position;
                if (Mathf.Abs(p.y - (Y + 0.07f)) < 0.05f && Pit.Contains(new Vector2(p.x, p.z)) && !Landing.Contains(new Vector2(p.x, p.z))) Object.DestroyImmediate(t.gameObject);
            }
            // 3) foso: suelo, faldones de los muros hasta el suelo nuevo y el bloque bajo el rellano
            float y0 = PitY - 0.3f, h = Y - y0;
            Box("Calderas_Suelo", solid, new Vector3(Pit.center.x, PitY - 0.15f, Pit.center.y), new Vector3(Pit.width, 0.3f, Pit.height), floorM, 4f);
            Box("Calderas_Muro_O", solid, new Vector3(-20f, y0 + h / 2f, 22f), new Vector3(0.35f, h, 20f), wall, 3f);
            Box("Calderas_Muro_N", solid, new Vector3(-13f, y0 + h / 2f, 32f), new Vector3(14f, h, 0.35f), wall, 3f);
            Box("Calderas_Muro_S", solid, new Vector3(-13f, y0 + h / 2f, 12f), new Vector3(14f, h, 0.25f), wall, 3f);
            Box("Calderas_Muro_E", solid, new Vector3(-6f, y0 + h / 2f, 24f), new Vector3(0.25f, h, 16f), wall, 3f);
            Box("Calderas_Rellano", solid, new Vector3(Landing.center.x, y0 + (Y - 0.3f - y0) / 2f, Landing.center.y), new Vector3(Landing.width, Y - 0.3f - y0, Landing.height), wall, 3f);
            // 4) escalera: peldanos de chapa (decorado) y una rampa invisible que es la que se pisa
            int n = 10; float run = (Landing.xMin - StairX1) / n, rise = (Y - PitY) / n, zc = (StairZ0 + StairZ1) / 2f, w = StairZ1 - StairZ0;
            for (int k = 0; k < n; k++)
            {
                float top = Y - rise * (k + 0.5f), x1 = Landing.xMin - run * k, x0 = x1 - run;
                Box("Calderas_Peldano", solid, new Vector3((x0 + x1) / 2f, (top + y0) / 2f, zc), new Vector3(run, top - y0, w), metal, 1f, false);
            }
            float len = Mathf.Sqrt((Landing.xMin - StairX1) * (Landing.xMin - StairX1) + (Y - PitY) * (Y - PitY)), ang = Mathf.Atan2(Y - PitY, Landing.xMin - StairX1) * Mathf.Rad2Deg;
            var ramp = new GameObject("Calderas_Rampa"); ramp.transform.SetParent(solid);
            ramp.transform.SetPositionAndRotation(new Vector3((Landing.xMin + StairX1) / 2f, (Y + PitY) / 2f + 0.02f, zc), Quaternion.Euler(0, 0, ang));
            var rc = ramp.AddComponent<BoxCollider>(); rc.size = new Vector3(len, 0.1f, w); rc.center = new Vector3(0, -0.05f, 0);
            ramp.isStatic = true;
            // 5) barandillas: borde del rellano y lado abierto de la escalera
            Rail(new Vector3(Landing.xMin, Y, Landing.yMax), new Vector3(Landing.xMax, Y, Landing.yMax));
            Rail(new Vector3(Landing.xMin, Y, StairZ1), new Vector3(Landing.xMin, Y, Landing.yMax));
            Rail(new Vector3(Landing.xMin, Y, StairZ1), new Vector3(StairX1, PitY, StairZ1));
        }

        /// <summary>Barandilla de chapa (puede ir en pendiente): pies derechos, pasamanos, barra intermedia y colision fina invisible.</summary>
        static void Rail(Vector3 a, Vector3 b)
        {
            var g = new GameObject("Calderas_Barandilla").transform; g.SetParent(solid);
            Vector3 d = b - a; float len = d.magnitude;
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                Box("Pie", g, p + Vector3.up * 0.5f, new Vector3(0.05f, 1.0f, 0.05f), metal, 0f, false);
            }
            var rot = Quaternion.LookRotation(d / len);
            foreach (float hy in new[] { 1.0f, 0.5f })
            {
                var bar = Box("Barra", g, (a + b) / 2f + Vector3.up * hy, new Vector3(0.05f, 0.05f, len), hy > 0.9f ? wood : metal, 0f, false);
                bar.transform.rotation = rot;
            }
            var col = new GameObject("Colision"); col.transform.SetParent(g);
            col.transform.SetPositionAndRotation((a + b) / 2f + Vector3.up * 0.6f, rot);
            col.AddComponent<BoxCollider>().size = new Vector3(0.08f, 1.2f, len);
        }

        static void BoilerFloor()
        {
            Vector3 Pv(float x, float z) => new Vector3(x, PitY, z);
            Place(solid, B + "Boiler.fbx", Pv(-13.4f, 23.6f), 90f, true, 0f, false);          // caldera central (fija)
            // filas de cobertura con pasillos de ~2,6 m: armarios electricos y grupos electrogenos
            Place(root, B + "ControlPanel.fbx", Pv(-17.6f, 18.4f), 90f);
            Place(root, B + "ControlPanel.fbx", Pv(-9.2f, 19.6f), -90f);
            Place(root, B + "Generator.fbx", Pv(-9.0f, 25.6f), 90f);
            Place(root, B + "ControlPanel.fbx", Pv(-17.6f, 28.8f), 90f);
            Place(root, B + "Generator.fbx", Pv(-12.8f, 29.6f), 0f);
            Place(root, B + "WaterPump.fbx", Pv(-17.8f, 24.0f), 90f);
            foreach (var (x, z, yaw) in new[] { (-19.6f, 14.5f, 90f), (-15.0f, 31.6f, 180f), (-6.4f, 29.0f, -90f), (-6.4f, 22.0f, -90f) })
                Place(solid, B + "PipeValves.fbx", Pv(x, z), yaw, true, 0.3f);
            foreach (var (x, z) in new[] { (-19.3f, 31.2f), (-18.5f, 31.4f), (-7.0f, 31.2f), (-19.3f, 21.0f) }) Place(root, P + "Barrel.fbx", Pv(x, z), R(0, 360));
            Place(root, P + "Crate.fbx", Pv(-7.0f, 17.4f), 20f);
            // luz de emergencia roja, baja, y el resplandor del hogar
            var lights = new GameObject("Calderas_Luces").transform; lights.SetParent(solid);
            void L(Vector3 p, Color c, float range, float inten, bool flicker)
            {
                var go = new GameObject("Emergencia"); go.transform.SetParent(lights); go.transform.position = p;
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = inten; l.shadows = LightShadows.None;
                if (flicker) go.AddComponent<FireLight>();
            }
            var red = new Color(1f, 0.25f, 0.15f);
            L(new Vector3(-17f, -2.2f, 15f), red, 10f, 4f, false);
            L(new Vector3(-9f, -2.2f, 22f), red, 10f, 4f, true);
            L(new Vector3(-17f, -2.2f, 27f), red, 10f, 4f, false);
            L(new Vector3(-10f, -2.2f, 31f), red, 9f, 3.5f, true);
            L(new Vector3(-11.6f, PitY + 1.1f, 24.2f), new Color(1f, 0.45f, 0.15f), 7f, 6f, true);   // hogar de la caldera (da al este)
        }

        [MenuItem("Horror/Comisaria v2/5 Sotano y jefe 2")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaV2Builder.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaV2Builder.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase 1";
            if (rootGo.transform.Find("Sotano_Calderas") != null) return "la sala de calderas ya esta hundida: para repetir, reconstruir desde la fase 1 o borrar a mano 'Sotano_Calderas' sabiendo que el suelo ya tiene el hueco";
            wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            floorM = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Floor.mat");
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat") ?? AssetDatabase.LoadAssetAtPath<Material>(Mats + "Metal.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            var props = rootGo.transform.Find("Props");
            foreach (var n in new[] { "Props/Sotano_Props", "Sotano_Jefe" }) { var old = rootGo.transform.Find(n); if (old != null) Object.DestroyImmediate(old.gameObject); }
            root = new GameObject("Sotano_Props").transform; root.SetParent(props);
            solid = new GameObject("Sotano_Calderas").transform; solid.SetParent(rootGo.transform);
            rng = new System.Random(59);
            ItemTextureKit.Apply();

            SinkBoilerRoom(rootGo.transform, rootGo.transform.Find("Details") ?? rootGo.transform);
            Rooms();
            BoilerFloor();

            // ---- jefe 2 al fondo del foso, mirando a la escalera; despierta al pisar el rellano y atranca la puerta
            var bossRoot = new GameObject("Sotano_Jefe").transform; bossRoot.SetParent(rootGo.transform);
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Zombie_BossPxl.prefab"), bossRoot);
            boss.name = "Boss_2";
            boss.transform.SetPositionAndRotation(new Vector3(-15.5f, PitY + 0.05f, 30.3f), Quaternion.Euler(0, 150f, 0));
            var ai = boss.GetComponent<ZombieAI>();
            var keyFinal = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/I_KeyFinal.asset");
            if (keyFinal != null) { ai.dropOnDeath = keyFinal; EditorUtility.SetDirty(ai); PrefabUtility.RecordPrefabInstancePropertyModifications(ai); }
            var trig = new GameObject("BossRoomTrigger_2"); trig.transform.SetParent(bossRoot);
            trig.transform.position = new Vector3(-8.0f, Y + 1.5f, 14.1f);
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(2.6f, 3f, 3.8f);
            var brt = trig.AddComponent<BossRoomTrigger>(); brt.boss = ai;
            var door = GameObject.Find("Puerta_Calderas");
            brt.sealDoors = door != null ? door.GetComponentsInChildren<Door>() : new Door[0];

            // ---- NavMesh
            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            surface.overrideVoxelSize = true; surface.voxelSize = 0.1f;
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase 5: " + root.childCount + " muebles, sala de calderas hundida, jefe 2 (llave " + (keyFinal != null ? keyFinal.name : "FALTA") + ", " + brt.sealDoors.Length + " puerta)";
        }
    }
}
#endif
