#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, lavado de cara del exterior, fase 3 (menu Horror/Comisaria grande/11 Fachadas y azoteas): detalle de los edificios de la
    /// ciudad que rodean el patio. Cada edificio es una caja con la textura de fachada; la textura esta anclada al mundo con 6 m de baldosa, asi que las
    /// ventanas estan en coordenadas conocidas: a lo largo de la fachada en x (o z) = 1, 3 y 5 modulo 6, y a alturas de 0,9 a 2,4 m de cada planta de 3 m
    /// (las plantas empiezan en 0, 3, 6...; entre ventanas y entre plantas queda hueco libre para las molduras). Pone en las fachadas que miran a la calle,
    /// al patio y al callejon: zocalo, molduras de planta, cornisa, toldos, tablones en ventanas, escaleras de incendios, pintadas, carteles, chorretones
    /// y hollin; y en las azoteas: aires acondicionados, depositos de agua, chimeneas, antenas, parabolicas y casetas. Todo cuelga de "Ciudad_Detalle".
    /// Repetible: rehace solo "Ciudad_Detalle".
    /// </summary>
    public static class ComisariaGrandeCity
    {
        const string City = "Assets/_Project/Art/Props/City/";
        const string Tex = "Assets/_Project/Art/Textures/City/";
        const string Mats = "Assets/_Project/Materials/";

        struct Face { public string name; public bool alongX; public float fixedC, a0, a1, h; public int dir; }   // dir: +1 normal hacia +Z (o +X), -1 hacia -Z (o -X)

        static Transform root;
        static readonly List<(GameObject go, float x, float z, float r)> roofItems = new List<(GameObject, float, float, float)>();

        [MenuItem("Horror/Comisaria grande/11 Fachadas y azoteas")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        static Material DecalMat(string name, string tex, float smooth)
        {
            string path = Mats + "Ciudad_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png"));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_BlendModePreserveSpecular", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Importers()
        {
            foreach (var f in System.IO.Directory.GetFiles(Tex, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 1024;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
        }

        static GameObject Place(string model, Vector3 pos, float yaw, string name = null, float scale = 1f)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(City + model + ".fbx");
            if (pf == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root);
            go.name = name ?? model;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * pf.transform.localRotation);
            go.transform.localScale = Vector3.one * scale;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return go;
        }

        static int order;
        static void DecalQuad(Material m, string name, Face f, float c, float y, float w, float h, float off)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name; go.transform.SetParent(root);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Vector3 n = f.alongX ? new Vector3(0, 0, f.dir) : new Vector3(f.dir, 0, 0);       // normal de la fachada
            Vector3 p = f.alongX ? new Vector3(c, y, f.fixedC) : new Vector3(f.fixedC, y, c);
            go.transform.position = p + n * (off + (order++ % 30) * 0.0006f);
            go.transform.rotation = Quaternion.LookRotation(-n, Vector3.up);                    // el quad mira hacia fuera (su cara visible es la -Z local)
            go.transform.localScale = new Vector3(w, h, 1f);
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
            go.isStatic = true;
        }

        static float Yaw(Face f) => f.alongX ? (f.dir > 0 ? 0f : 180f) : (f.dir > 0 ? 90f : -90f);

        static Vector3 FacePoint(Face f, float c, float y, double off)
        {
            float o = (float)off;
            return f.alongX ? new Vector3(c, y, f.fixedC + f.dir * o) : new Vector3(f.fixedC + f.dir * o, y, c);
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---");
            if (level == null) return "falta el nivel";
            var old = level.transform.Find("Ciudad_Detalle");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("Ciudad_Detalle").transform; root.SetParent(level.transform);
            Importers();
            order = 0; roofItems.Clear();
            var wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            var graf = new[] { DecalMat("Pintada_A", "city_graffiti_a", 0.1f), DecalMat("Pintada_B", "city_graffiti_b", 0.1f), DecalMat("Pintada_C", "city_graffiti_c", 0.1f) };
            var poster = DecalMat("Cartel", "city_poster", 0.05f);
            var streak = DecalMat("Chorreton", "city_streak", 0.05f);
            var soot = DecalMat("Hollin", "city_soot", 0.05f);
            var rng = new System.Random(77);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // edificios: caja con centro y escala conocidos (ComisariaGrandeExterior.Building)
            var faces = new List<Face>(); var roofs = new List<(string n, float x0, float x1, float z0, float z1, float top)>();
            foreach (Transform t in level.transform.Find("Exterior"))
            {
                if (!t.name.StartsWith("Edificio_") || t.name.EndsWith("_Azotea")) continue;
                var c = t.position; var s = t.localScale;
                float x0 = c.x - s.x / 2f, x1 = c.x + s.x / 2f, z0 = c.z - s.z / 2f, z1 = c.z + s.z / 2f, h = s.y;
                if (t.name.StartsWith("Edificio_Frente")) faces.Add(new Face { name = t.name, alongX = true, fixedC = z1, a0 = x0, a1 = x1, h = h, dir = 1 });
                else if (t.name.StartsWith("Edificio_O")) faces.Add(new Face { name = t.name, alongX = false, fixedC = x1, a0 = z0, a1 = z1, h = h, dir = 1 });
                else if (t.name.StartsWith("Edificio_E")) faces.Add(new Face { name = t.name, alongX = false, fixedC = x0, a0 = z0, a1 = z1, h = h, dir = -1 });
                else if (t.name.StartsWith("Edificio_N")) faces.Add(new Face { name = t.name, alongX = true, fixedC = z0, a0 = x0, a1 = x1, h = h, dir = -1 });
                roofs.Add((t.name, x0, x1, z0, z1, h + 0.8f));
            }

            int awn = 0, boards = 0, gra = 0, posters = 0, esc = 0;
            for (int fi = 0; fi < faces.Count; fi++)
            {
                var f = faces[fi];
                float len = f.a1 - f.a0, mid = (f.a0 + f.a1) / 2f;
                // zocalo, molduras de planta y cornisa: cajas largas pegadas a la pared (sin colision)
                void Band(string n, float y, float hh, float protrude)
                {
                    var size = f.alongX ? new Vector3(len + 0.1f, hh, protrude) : new Vector3(protrude, hh, len + 0.1f);
                    var pos = f.alongX ? new Vector3(mid, y, f.fixedC + f.dir * protrude / 2f) : new Vector3(f.fixedC + f.dir * protrude / 2f, y, mid);
                    var b = ComisariaV2Exterior.Box(n, root, pos, size, wall, 3f, false);
                    b.isStatic = true;
                }
                Band("Zocalo", 0.3f, 0.6f, 0.2f);
                for (float y = 3f; y < f.h - 1.5f; y += 3f) Band("Moldura", y, 0.16f, 0.14f);
                Band("Cornisa", f.h - 0.25f, 0.5f, 0.45f);

                // ventanas conocidas: coordenada = 6k + {1, 3, 5}; plantas de 3 m
                var wins = new List<float>();
                for (float k = Mathf.Floor(f.a0 / 6f) * 6f; k < f.a1; k += 6f) foreach (float o in new[] { 1f, 3f, 5f }) { float cc = k + o; if (cc > f.a0 + 1f && cc < f.a1 - 1f) wins.Add(cc); }
                foreach (float cc in wins)
                {
                    double q = rng.NextDouble();
                    if (q < 0.30) { Place(rng.NextDouble() < 0.35 ? "AwningTorn" : "Awning", FacePoint(f, cc, 2.65f, 0.03), Yaw(f)); awn++; }
                    else if (q < 0.42) { Place("BoardsX", FacePoint(f, cc, 1.65f, 0.02), Yaw(f)); boards++; }
                    int floors = Mathf.FloorToInt(f.h / 3f);
                    for (int fl = 1; fl < Mathf.Min(floors, 4); fl++)
                        if (rng.NextDouble() < 0.05) { Place("BoardsX", FacePoint(f, cc, 1.65f + 3f * fl, 0.02), Yaw(f)); boards++; }
                    if (rng.NextDouble() < 0.10 && floors > 2) DecalQuad(streak, "Chorreton", f, cc, 3f * Mathf.Min(floors - 1, 3) + 0.9f, 1.4f, 5.6f, 0.04f);
                    if (rng.NextDouble() < 0.08 && floors > 2) DecalQuad(soot, "Hollin", f, cc, 3f * rng.Next(1, Mathf.Min(floors, 4)) + 2.9f, 1.6f, 0.8f, 0.04f);
                }
                // pintadas y carteles en los huecos de pared entre ventanas (coordenada 0, 2 y 4 modulo 6) de la planta baja
                for (int g = 0; g < 4; g++)
                {
                    float cc = Mathf.Floor(R(f.a0 + 2f, f.a1 - 2f) / 2f) * 2f;
                    DecalQuad(graf[rng.Next(3)], "Pintada", f, cc, R(0.9f, 1.6f), 1.05f, 1.05f, 0.035f); gra++;
                }
                for (int g = 0; g < 3; g++)
                {
                    float cc = Mathf.Floor(R(f.a0 + 2f, f.a1 - 2f) / 2f) * 2f + R(-0.2f, 0.2f);
                    DecalQuad(poster, "Cartel", f, cc, R(1.3f, 1.8f), 0.55f, 0.82f, 0.035f); posters++;
                }
                // escalera de incendios en algunos edificios altos (3 plantas), delante de un grupo de ventanas
                if (f.h >= 12f && rng.NextDouble() < 0.4 && wins.Count > 3)
                {
                    float cc = wins[rng.Next(1, wins.Count - 1)];
                    var go = Place("FireEscape", FacePoint(f, cc, 0f, 0.05), Yaw(f));
                    if (go != null) esc++;
                }
            }

            // azoteas: la losa del pretil acaba en h + 0,8; se reparten piezas sin solaparse
            int roofCount = 0;
            foreach (var rf in roofs)
            {
                float cx0 = rf.x0 + 2.5f, cx1 = rf.x1 - 2.5f, cz0 = rf.z0 + 2.5f, cz1 = rf.z1 - 2.5f;
                if (cx1 <= cx0 || cz1 <= cz0) continue;
                bool Free(float x, float z, float r) { foreach (var it in roofItems) if (Vector2.Distance(new Vector2(x, z), new Vector2(it.x, it.z)) < r + it.r) return false; return true; }
                void Put(string model, float r, float scale = 1f)
                {
                    for (int tries = 0; tries < 12; tries++)
                    {
                        float x = R(cx0, cx1), z = R(cz0, cz1);
                        if (!Free(x, z, r)) continue;
                        var go = Place(model, new Vector3(x, rf.top, z), R(0f, 360f), model, scale);
                        if (go == null) return;
                        roofItems.Add((go, x, z, r)); roofCount++; return;
                    }
                }
                float area = (rf.x1 - rf.x0) * (rf.z1 - rf.z0);
                int ac = Mathf.Clamp(Mathf.RoundToInt(area / 220f), 1, 5);
                for (int i = 0; i < ac; i++) Put("RoofAC", 1.5f);
                if (rng.NextDouble() < 0.5) Put("WaterTower", 2.0f);
                for (int i = 0, n = rng.Next(0, 3); i < n; i++) Put("Chimney", 0.9f);
                for (int i = 0, n = rng.Next(0, 3); i < n; i++) Put("Dish", 0.8f);
                for (int i = 0, n = rng.Next(0, 3); i < n; i++) Put("Antenna", 0.8f);
                if (area > 400f) Put("RoofAccess", 2.0f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "ciudad: " + faces.Count + " fachadas, " + awn + " toldos, " + boards + " ventanas tapiadas, " + gra + " pintadas, " + posters + " carteles, " + esc + " escaleras de incendios, " + roofCount + " piezas en azoteas";
        }
    }
}
#endif
