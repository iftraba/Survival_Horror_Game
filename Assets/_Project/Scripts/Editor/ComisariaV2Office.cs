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
    /// Comisaria v2, fase 3: la primera planta (menu Horror/Comisaria v2/3 Primera planta). Repetible (rehace "Primera_Props").
    ///  - Oficinas: islas de mesas enfrentadas con silla giratoria o normal, ordenador (monitor, teclado y raton) o portatil, papeles,
    ///    lampara, telefono y papelera al azar; archivadores, pizarra, fuente de agua, perchero.
    ///  - Despacho del comisario: mesa grande con sillon de piel, sillas de visita, alfombra, librerias, vitrina de trofeos y banderas.
    ///  - Sala de conferencias: filas de sillas con pasillo central, estrado con atril y microfono, mapa de la ciudad y 4 banderas.
    /// Variedad: cada mueble con un pequeno giro y desplazamiento al azar y un tono distinto (PropVariant); los objetos de encima
    /// de las mesas cambian de una a otra. Al final rehace el NavMesh.
    /// </summary>
    public static class ComisariaV2Office
    {
        const string O = "Assets/_Project/Art/Props/Office/";
        const string P = "Assets/_Project/Art/Props/";
        const string Mats = "Assets/_Project/Materials/";
        const string Tex = "Assets/_Project/Art/Textures/";
        static readonly float Y = ComisariaV2Builder.FirstY;
        const float DeskTop = 0.76f;

        static Transform root, walkRoot;   // walkRoot: lo que se pisa (estrado), fuera de "Props", que el NavMesh trata como no transitable
        static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>Modelo colocado (contenedor con el giro; el FBX conserva el suyo), con colision opcional y tono al azar.</summary>
        static GameObject Place(string path, Vector3 pos, float yaw, bool collider = true, float jitter = 1f, bool tint = true)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[Horror] falta " + path); return null; }
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(root);
            holder.transform.SetPositionAndRotation(pos + new Vector3(R(-0.03f, 0.03f), 0f, R(-0.03f, 0.03f)) * jitter, Quaternion.Euler(0, yaw + R(-3f, 3f) * jitter, 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
            m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            if (tint)
            {
                float v = R(0.82f, 1.06f);
                holder.AddComponent<PropVariant>().tint = new Color(v * R(0.96f, 1.03f), v, v * R(0.96f, 1.03f));
            }
            foreach (var t in holder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return holder;
        }

        // ------------------------------------------------------------------ oficinas
        /// <summary>Puesto de trabajo: mesa con la persona sentada en 'worker' (direccion), silla y cosas encima al azar.</summary>
        static void Desk(Vector3 c, float yaw)
        {
            var desk = Place(P + "Desk.fbx", c, yaw);
            var rot = Quaternion.Euler(0, yaw, 0);
            Vector3 worker = rot * Vector3.forward, side = rot * Vector3.right;
            // silla (giratoria casi siempre), algo separada y torcida
            bool swivel = rng.NextDouble() < 0.7;
            Place(swivel ? O + "SwivelChair.fbx" : P + "Chair.fbx", c + worker * R(0.75f, 1.0f) + side * R(-0.25f, 0.25f), yaw + 180f + R(-30f, 30f), true, 1f);
            float top = c.y + DeskTop;
            double k = rng.NextDouble();
            if (k < 0.6) Place(O + "DeskComputer.fbx", new Vector3(c.x, top, c.z) - worker * 0.08f + side * R(-0.3f, 0.1f), yaw + R(-8f, 8f), false, 0.5f, false);
            else if (k < 0.85) Place(O + "Laptop.fbx", new Vector3(c.x, top, c.z) + worker * 0.05f + side * R(-0.3f, 0.3f), yaw + R(-20f, 20f), false, 0.5f, false);
            if (rng.NextDouble() < 0.55) Place(O + "PaperStack.fbx", new Vector3(c.x, top, c.z) + side * R(0.35f, 0.5f) * (rng.NextDouble() < 0.5 ? -1 : 1) + worker * R(-0.1f, 0.15f), yaw + R(-40f, 40f), false, 0.5f, false);
            if (rng.NextDouble() < 0.3) Place(O + "DeskLamp.fbx", new Vector3(c.x, top, c.z) + side * 0.6f - worker * 0.2f, yaw + R(-30f, 30f), false, 0.5f, false);
            if (rng.NextDouble() < 0.25) Place(P + "Phone.fbx", new Vector3(c.x, top + 0.0f, c.z) - side * 0.55f, yaw + R(-30f, 30f), false, 0.5f, false);
            if (rng.NextDouble() < 0.45) Place(O + "TrashBin.fbx", c + side * R(0.85f, 0.95f) * (rng.NextDouble() < 0.5 ? -1 : 1) + worker * 0.3f, R(0f, 360f), true, 0.5f);
        }

        /// <summary>Isla de dos mesas enfrentadas (una mira al norte y otra al sur) en x, con su centro en z.</summary>
        static void Pod(float x, float z)
        {
            Desk(new Vector3(x, Y, z - 0.375f), 180f);
            Desk(new Vector3(x, Y, z + 0.375f), 0f);
        }

        static void Offices()
        {
            foreach (float x in new[] { -6.4f, -4.85f }) { Pod(x, 3.0f); Pod(x, 11.6f); }   // al oeste, dejando libre el paso de la puerta (z = 8)
            foreach (float x in new[] { -1.9f, -0.35f }) { Pod(x, 2.6f); Pod(x, 7.4f); }   // pasillo ancho entre las dos filas
            foreach (float x in new[] { 3.2f, 4.75f }) Pod(x, 3.0f);                          // al sur del hueco de la escalera
            for (int i = 0; i < 4; i++) Place(P + "FilingCabinet.fbx", new Vector3(-7.6f, Y, 13.4f + i * 0.62f), 90f);
            Place(O + "Whiteboard.fbx", new Vector3(2.6f, Y, 0.45f), 0f);
            Place(O + "WaterCooler.fbx", new Vector3(7.5f, Y, 0.45f), 0f);
            Place(O + "CoatRack.fbx", new Vector3(-7.4f, Y, 0.6f), R(0, 90));
            Place(P + "Crate.fbx", new Vector3(5.6f, Y, 15.4f), 20f);                          // en la esquina: el paso al este del hueco queda libre
            Place(P + "Shelf.fbx", new Vector3(-3.0f, Y, 15.6f), 180f);
            Place(P + "Shelf.fbx", new Vector3(-4.3f, Y, 15.6f), 180f);
        }

        // ------------------------------------------------------------------ despacho del comisario
        static void Commissioner(Material policeFlag, Material cityFlag)
        {
            float cx = -14.5f, cz = 8f;
            Place(O + "Rug.fbx", new Vector3(cx + 0.6f, Y, cz), 90f, false, 0.3f);
            Place(O + "ExecutiveDesk.fbx", new Vector3(cx - 1.2f, Y, cz), -90f, true, 0.3f);          // el comisario se sienta al oeste
            Place(O + "ExecutiveChair.fbx", new Vector3(cx - 2.1f, Y, cz + 0.2f), 90f + R(-15, 15));
            Place(P + "Chair.fbx", new Vector3(cx + 0.1f, Y, cz - 0.7f), -90f + R(-15, 15));
            Place(P + "Chair.fbx", new Vector3(cx + 0.1f, Y, cz + 0.7f), -90f + R(-15, 15));
            float top = Y + 0.78f;
            Place(O + "DeskComputer.fbx", new Vector3(cx - 1.25f, top, cz + 0.55f), -90f, false, 0.3f, false);
            Place(O + "PaperStack.fbx", new Vector3(cx - 1.1f, top, cz - 0.5f), -70f, false, 0.3f, false);
            Place(O + "DeskLamp.fbx", new Vector3(cx - 1.5f, top, cz - 0.8f), -120f, false, 0.3f, false);
            Place(P + "Phone.fbx", new Vector3(cx - 1.0f, top, cz + 0.05f), -100f, false, 0.3f, false);
            foreach (float z in new[] { 2.6f, 3.85f, 12.15f, 13.4f }) Place(O + "Bookcase.fbx", new Vector3(-19.6f, Y, z), 90f);
            Place(O + "TrophyCabinet.fbx", new Vector3(-15.5f, Y, 15.6f), 180f);
            Place(P + "FilingCabinet.fbx", new Vector3(-12.6f, Y, 15.6f), 180f);
            Place(O + "CoatRack.fbx", new Vector3(-9.0f, Y, 15.0f), R(0, 90));
            Place(P + "Locker.fbx", new Vector3(-9.0f, Y, 0.6f), 0f);
            // mesa de reuniones (dos mesas juntas) con sus sillas, banco, mas librerias, pizarra y fuente de agua
            Place(P + "Desk.fbx", new Vector3(-11.6f, Y, 2.6f), 0f, true, 0.3f);
            Place(P + "Desk.fbx", new Vector3(-11.6f, Y, 3.35f), 180f, true, 0.3f);
            foreach (float dx in new[] { -0.45f, 0.45f })
            {
                Place(P + "Chair.fbx", new Vector3(-11.6f + dx, Y, 1.75f), R(-20, 20));
                Place(P + "Chair.fbx", new Vector3(-11.6f + dx, Y, 4.2f), 180f + R(-20, 20));
            }
            Place(O + "PaperStack.fbx", new Vector3(-11.9f, Y + DeskTop, 2.9f), R(0, 360), false, 0.3f, false);
            Place(O + "Laptop.fbx", new Vector3(-11.2f, Y + DeskTop, 3.1f), 180f + R(-20, 20), false, 0.3f, false);
            Place(P + "Exterior/WaitingBench.fbx", new Vector3(-11.0f, Y, 15.5f), 180f);
            foreach (float x in new[] { -18.9f, -17.65f }) Place(O + "Bookcase.fbx", new Vector3(x, Y, 15.6f), 180f);
            Place(O + "Whiteboard.fbx", new Vector3(-13.5f, Y, 0.45f), 0f);
            Place(O + "WaterCooler.fbx", new Vector3(-19.4f, Y, 0.5f), 0f);
            Flag(new Vector3(-19.0f, Y, 6.2f), -90f, policeFlag);
            Flag(new Vector3(-19.0f, Y, 9.8f), -90f, cityFlag);
        }

        // ------------------------------------------------------------------ sala de conferencias
        /// <summary>Bandera: mastil (modelo) y tela colgando del remate con pliegues (malla generada con UV).</summary>
        static void Flag(Vector3 basePos, float yaw, Material cloth)
        {
            var pole = Place(O + "FlagPole.fbx", basePos, yaw, true, 0.3f, false);
            var go = new GameObject("Tela"); go.transform.SetParent(pole.transform, false);
            go.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            go.transform.localRotation = Quaternion.identity;                 // la tela queda paralela a la pared
            float w = 1.15f, h = 0.75f; int nx = 16, ny = 8;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float u = i / (float)nx, t = j / (float)ny;
                    // cae: cuanto mas lejos del mastil, mas cuelga; pliegues verticales
                    float x = u * w * 0.8f, y = -t * h - u * u * 0.45f, z = Mathf.Sin(u * Mathf.PI * 3.5f) * 0.06f * u;
                    v.Add(new Vector3(x, y, z)); uv.Add(new Vector2(u, 1f - t));
                    if (i < nx && j < ny) { int a = j * (nx + 1) + i; tri.AddRange(new[] { a, a + 1, a + nx + 1, a + 1, a + nx + 2, a + nx + 1 }); }   // cara de delante hacia la sala
                }
            var mesh = new Mesh { name = "TelaBandera" }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = "Assets/_Project/Art/Props/Office/TelaBandera.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (old == null) AssetDatabase.CreateAsset(mesh, mp); else { old.Clear(); old.SetVertices(v); old.SetUVs(0, uv); old.SetTriangles(tri, 0); old.RecalculateNormals(); old.RecalculateBounds(); EditorUtility.SetDirty(old); mesh = old; }
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = cloth;
        }

        static Material TexMat(string name, string tex, bool doubleSided, float smooth)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(Tex + tex + ".png");
            if (ti != null) { ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 2048; ti.anisoLevel = 4; ti.SaveAndReimport(); }
            string path = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png"));
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
            if (doubleSided) m.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Conference(Material policeFlag, Material cityFlag, Material map)
        {
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            float sx0 = 16.6f, sx1 = 19.85f, stageH = 0.42f;
            // estrado al fondo (este) con dos escalones a los lados
            var stage = Box("Estrado", walkRoot, new Vector3((sx0 + sx1) / 2f, Y + stageH / 2f, 6f), new Vector3(sx1 - sx0, stageH, 11.2f), wood, 1f);
            foreach (float z in new[] { 1.4f, 10.6f }) Box("Escalon", walkRoot, new Vector3(sx0 - 0.3f, Y + stageH / 4f, z), new Vector3(0.6f, stageH / 2f, 1.2f), wood, 1f);
            float sy = Y + stageH;
            Place(O + "Lectern.fbx", new Vector3(17.4f, sy, 6f), -90f, true, 0.2f);
            // mapa de la ciudad en la pared del fondo, con marco
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Mapa"; Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(root); q.transform.SetPositionAndRotation(new Vector3(19.76f, sy + 1.6f, 6f), Quaternion.Euler(0, 90f, 0));
            q.transform.localScale = new Vector3(3.4f, 2.1f, 1f); q.GetComponent<Renderer>().sharedMaterial = map;
            foreach (var (dz, dy, s) in new[] { (0f, 1.08f, new Vector3(0.06f, 0.08f, 3.6f)), (0f, -1.08f, new Vector3(0.06f, 0.08f, 3.6f)), (1.76f, 0f, new Vector3(0.06f, 2.24f, 0.08f)), (-1.76f, 0f, new Vector3(0.06f, 2.24f, 0.08f)) })
                Box("Marco_Mapa", root, new Vector3(19.74f, sy + 1.6f + dy, 6f + dz), s, wood, 0f, false);
            // cuatro banderas, dos a cada lado del mapa
            float[] fz = { 2.0f, 3.25f, 8.75f, 10.0f };
            for (int i = 0; i < 4; i++) Flag(new Vector3(19.3f, sy, fz[i]), 90f, i % 2 == 0 ? policeFlag : cityFlag);
            // filas de sillas mirando al estrado, con pasillo central (z 5,3 - 6,7); alguna volcada o descolocada
            for (int row = 0; row < 6; row++)
            {
                float x = 15.0f - row * 1.0f;
                for (int side = 0; side < 2; side++)
                    for (int i = 0; i < 7; i++)
                    {
                        float z = side == 0 ? 0.8f + i * 0.64f : 6.9f + i * 0.64f;
                        if (rng.NextDouble() < 0.06) continue;                                           // hueco
                        var ch = Place(O + "AuditoriumChair.fbx", new Vector3(x, Y, z), 90f, true, 1.5f);
                        if (rng.NextDouble() < 0.07)                                                     // volcada
                        {
                            ch.transform.rotation = Quaternion.Euler(0, R(0, 360), 0) * Quaternion.Euler(R(80, 95), 0, 0);
                            ch.transform.position += Vector3.up * 0.25f + new Vector3(R(-0.4f, 0.4f), 0, R(-0.3f, 0.3f));
                        }
                    }
            }
            Place(O + "WaterCooler.fbx", new Vector3(8.5f, Y, 0.5f), 0f);
            Place(O + "TrashBin.fbx", new Vector3(8.5f, Y, 11.4f), 0f);
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

        [MenuItem("Horror/Comisaria v2/3 Primera planta")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaV2Builder.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaV2Builder.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase 1";
            var props = rootGo.transform.Find("Props");
            var old = props.Find("Primera_Props"); if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("Primera_Props").transform; root.SetParent(props);
            var oldWalk = rootGo.transform.Find("Primera_Estrado"); if (oldWalk != null) Object.DestroyImmediate(oldWalk.gameObject);
            walkRoot = new GameObject("Primera_Estrado").transform; walkRoot.SetParent(rootGo.transform);
            rng = new System.Random(31);
            ItemTextureKit.Apply();
            var policeFlag = TexMat("Bandera_Policia", "tex_flag_police", true, 0.15f);
            var cityFlag = TexMat("Bandera_Ciudad", "tex_flag_city", true, 0.15f);
            var map = TexMat("Mapa_Ciudad", "tex_city_map", false, 0.25f);
            Offices();
            Commissioner(policeFlag, cityFlag);
            Conference(policeFlag, cityFlag, map);
            int n = root.GetComponentsInChildren<PropVariant>().Length;
            // NavMesh de nuevo (las mesas y sillas lo recortan)
            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            Physics.SyncTransforms();                                   // las cajas recien creadas (estrado) aun no estan en su sitio para la fisica
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase 3: " + root.childCount + " objetos (" + n + " con tono propio)";
        }
    }
}
#endif
