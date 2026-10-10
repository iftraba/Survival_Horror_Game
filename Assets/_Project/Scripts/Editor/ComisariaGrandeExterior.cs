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
    /// Comisaria grande, fase B (menu Horror/Comisaria grande/2 Exterior e intro): lo de ComisariaV2Exterior adaptado al edificio
    /// de 64 x 44 m. Patio delantero con la verja y los zombis, coche patrulla ardiendo, farolas, calle, edificios alrededor, fuegos
    /// y humo; fachada de hormigon con ventanas (con los huecos de las puertas del callejon y del balcon); luz del callejon; noche y
    /// niebla; escena de camara del principio (mas alta y abierta) y la puerta principal que se atranca al entrar.
    /// Repetible: rehace "Exterior" e "Intro".
    /// </summary>
    public static class ComisariaGrandeExterior
    {
        const string Ext = "Assets/_Project/Art/Props/Exterior/";
        const string Props = "Assets/_Project/Art/Props/";
        const string Mats = "Assets/_Project/Materials/";
        const float X0 = ComisariaGrande.X0, X1 = ComisariaGrande.X1, Z1 = ComisariaGrande.Z1;
        const float FacadeTop = 8.6f;           // planta baja + primera + pretil de la azotea

        static Transform ext;
        static GameObject Box(string n, Transform p, Vector3 c, Vector3 s, Material m, float tile, bool col = true) => ComisariaV2Exterior.Box(n, p, c, s, m, tile, col);
        static GameObject Model(string path, Transform p, Vector3 pos, float yaw, bool col, string name = null) => ComisariaV2Exterior.Model(path, p, pos, yaw, col, name);

        /// <summary>Revestimiento de una fachada (a 5 cm del muro, sin colision) con huecos [centro, y0, ancho, alto].</summary>
        static void Clad(string name, bool alongX, float fixedC, float s0, float s1, Material m, params (float c, float y0, float w, float h)[] gaps)
        {
            var cuts = new List<float> { s0 };
            foreach (var g in gaps.OrderBy(g => g.c)) { cuts.Add(g.c - g.w / 2f); cuts.Add(g.c + g.w / 2f); }
            cuts.Add(s1);
            for (int i = 0; i + 1 < cuts.Count; i += 2) Piece(name, alongX, fixedC, cuts[i], cuts[i + 1], 0f, FacadeTop, m);
            foreach (var g in gaps)
            {
                if (g.y0 > 0.05f) Piece(name, alongX, fixedC, g.c - g.w / 2f, g.c + g.w / 2f, 0f, g.y0, m);
                Piece(name, alongX, fixedC, g.c - g.w / 2f, g.c + g.w / 2f, g.y0 + g.h, FacadeTop, m);
            }
        }
        static void Piece(string name, bool alongX, float fixedC, float a, float b, float y0, float y1, Material m)
        {
            if (b - a < 0.01f || y1 - y0 < 0.01f) return;
            var c = alongX ? new Vector3((a + b) / 2f, (y0 + y1) / 2f, fixedC) : new Vector3(fixedC, (y0 + y1) / 2f, (a + b) / 2f);
            var s = alongX ? new Vector3(b - a, y1 - y0, 0.06f) : new Vector3(0.06f, y1 - y0, b - a);
            Box(name, ext, c, s, m, 6f, false);
        }

        static void Exterior(Material fire, Material smoke)
        {
            var wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            var metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            var facA = ComisariaV2Exterior.CityMat("Env_FacadeA", "tex_facade_a", true);
            var facB = ComisariaV2Exterior.CityMat("Env_FacadeB", "tex_facade_b", true);
            var asphalt = ComisariaV2Exterior.CityMat("Env_Asphalt", "tex_asphalt", false);
            var sidewalk = ComisariaV2Exterior.CityMat("Env_Sidewalk", "tex_sidewalk", false);

            // suelos: patio delantero (8 m), franjas a los lados y detras dentro de la verja, acera, calzada y acera de enfrente
            Box("Patio", ext, new Vector3(-2.5f, -0.1f, -4f), new Vector3(79f, 0.2f, 8f), sidewalk, 4f);
            Box("Patio_E", ext, new Vector3(34.25f, -0.1f, 22f), new Vector3(4.5f, 0.2f, 44f), sidewalk, 4f);
            // el patio oeste ya no cubre la explanada ni la rampa del garaje (x -41,5..-32, z 0..5,5 y la calzada inclinada al oeste de x -37,2)
            Box("Patio_O", ext, new Vector3(-34.6f, -0.1f, 6.75f), new Vector3(5.2f, 0.2f, 2.5f), sidewalk, 4f);
            Box("Patio_N", ext, new Vector3(-2.5f, -0.1f, 46.25f), new Vector3(79f, 0.2f, 4.5f), sidewalk, 4f);
            Box("Acera", ext, new Vector3(0, -0.1f, -9f), new Vector3(180f, 0.2f, 2f), sidewalk, 4f);
            Box("Calzada", ext, new Vector3(0, -0.15f, -14f), new Vector3(180f, 0.2f, 8f), asphalt, 4f);
            Box("Acera_Enfrente", ext, new Vector3(0, -0.1f, -19f), new Vector3(180f, 0.2f, 2f), sidewalk, 4f);
            Box("Bordillo", ext, new Vector3(0, 0.0f, -9.95f), new Vector3(180f, 0.2f, 0.12f), wall, 3f);
            Box("Solar_O", ext, new Vector3(-62f, -0.12f, 22f), new Vector3(40f, 0.2f, 64f), asphalt, 4f);
            Box("Solar_E", ext, new Vector3(57f, -0.12f, 22f), new Vector3(40f, 0.2f, 64f), asphalt, 4f);
            Box("Solar_N", ext, new Vector3(-2.5f, -0.12f, 60f), new Vector3(79f, 0.2f, 24f), asphalt, 4f);

            // fachada: sur con la puerta principal, norte, este y oeste (puerta del callejon en la baja y la del balcon en la primera)
            Clad("Fachada_S", true, -0.23f, X0, X1, facB, (0f, 0f, 3.2f, 2.5f));
            Clad("Fachada_N", true, Z1 + 0.23f, X0, X1, facB);
            Clad("Fachada_E", false, X1 + 0.23f, 0f, Z1, facB);
            Clad("Fachada_O", false, X0 - 0.23f, 0f, Z1, facB, (40f, 0f, 1.7f, 2.5f), (20f, 4f, 1.7f, 2.5f));
            Box("Marquesina", ext, new Vector3(0f, 3.0f, -1.4f), new Vector3(6f, 0.18f, 2.6f), metal, 1f, false);
            foreach (float x in new[] { -2.8f, 2.8f }) Box("Pilar_Marquesina", ext, new Vector3(x, 1.45f, -2.55f), new Vector3(0.18f, 2.9f, 0.18f), metal, 1f);
            Box("Rotulo", ext, new Vector3(0f, 3.55f, -0.32f), new Vector3(5.2f, 0.7f, 0.12f), metal, 1f, false);
            var sl = new GameObject("Luz_Rotulo"); sl.transform.SetParent(ext); sl.transform.position = new Vector3(0, 3.1f, -1.2f);
            var sll = sl.AddComponent<Light>(); sll.type = LightType.Point; sll.color = new Color(0.7f, 0.85f, 1f); sll.intensity = 3f; sll.range = 7f;
            // luz del callejon (una lampara de pared junto a la puerta trasera) y otra al pie de la escalera de incendios
            foreach (var (p, c) in new[] { (new Vector3(-32.6f, 3.0f, 40f), new Color(1f, 0.75f, 0.45f)), (new Vector3(-36.5f, 3.4f, 30f), new Color(0.7f, 0.8f, 1f)) })
            {
                var g = new GameObject("Luz_Callejon"); g.transform.SetParent(ext); g.transform.position = p;
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = 3.5f; l.range = 9f; l.shadows = LightShadows.Soft;
                g.AddComponent<FireLight>();
            }

            // verja alrededor (delante, a los lados y detras)
            var fence = new GameObject("Verja").transform; fence.SetParent(ext);
            void FenceLine(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a; float len = d.magnitude; int n = Mathf.CeilToInt(len / 3f);
                float yaw = Quaternion.LookRotation(Vector3.Cross(Vector3.up, d.normalized)).eulerAngles.y;
                for (int i = 0; i < n; i++)
                {
                    var seg = Model(Ext + "FenceSegment.fbx", fence, a + d * ((i + 0.5f) / n), yaw, false, "Verja_Tramo");
                    if (seg == null) continue;
                    var bc = seg.AddComponent<BoxCollider>(); var mf = seg.GetComponentInChildren<MeshFilter>();
                    bc.center = mf.sharedMesh.bounds.center; bc.size = Vector3.Scale(mf.sharedMesh.bounds.size, new Vector3(1f, 6f, 1f));
                }
            }
            FenceLine(new Vector3(-42f, 0, -8), new Vector3(36.5f, 0, -8));
            FenceLine(new Vector3(-42f, 0, -8), new Vector3(-42f, 0, 48.5f));
            FenceLine(new Vector3(36.5f, 0, -8), new Vector3(36.5f, 0, 48.5f));
            FenceLine(new Vector3(-42f, 0, 48.5f), new Vector3(36.5f, 0, 48.5f));

            // coches, farolas, edificios alrededor y caos
            Model(Ext + "PoliceCar.fbx", ext, new Vector3(4.6f, 0f, -4.4f), 25f, true, "CochePatrulla");
            ComisariaV2Exterior.Fire(ext, new Vector3(4.4f, 1.1f, -5.5f), 1.0f, fire, smoke);
            Model(Ext + "PoliceCar.fbx", ext, new Vector3(-20f, 0f, -14f), 100f, true, "CocheAbandonado");
            ComisariaV2Exterior.Fire(ext, new Vector3(-19f, 0.6f, -14.3f), 0.6f, fire, smoke);
            Model(Ext + "PoliceCar.fbx", ext, new Vector3(-14f, 0f, -3.6f), -70f, true, "CochePatrulla2");
            for (int i = 0; i < 13; i++)
            {
                float x = -72f + i * 12f;
                Model(Ext + "StreetLamp.fbx", ext, new Vector3(x, 0f, -9.3f), 0f, true, "Farola");
                if (i % 3 == 1) continue;
                var lg = new GameObject("Luz_Farola"); lg.transform.SetParent(ext); lg.transform.position = new Vector3(x, 5.7f, -10.6f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Spot; l.spotAngle = 110f; l.color = new Color(1f, 0.78f, 0.5f); l.intensity = 9f; l.range = 13f;
                lg.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            var rng = new System.Random(7);
            void Building(string n, float x0, float x1, float z0, float z1, float h)
            {
                var m = rng.NextDouble() < 0.5 ? facA : facB;
                Box(n, ext, new Vector3((x0 + x1) / 2f, h / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, h, z1 - z0), m, 6f);
                Box(n + "_Azotea", ext, new Vector3((x0 + x1) / 2f, h + 0.4f, (z0 + z1) / 2f), new Vector3(x1 - x0 + 0.4f, 0.8f, z1 - z0 + 0.4f), wall, 3f, false);
            }
            float[] fx = { -90f, -70f, -50f, -30f, -10f, 10f, 30f, 50f, 70f, 90f };
            for (int i = 0; i < fx.Length - 1; i++) Building("Edificio_Frente_" + i, fx[i] + 0.5f, fx[i + 1] - 0.5f, -36f, -20.5f, 12f + (float)rng.NextDouble() * 12f);
            Building("Edificio_O", -72f, -46f, -6f, 20f, 20f);
            Building("Edificio_O2", -72f, -46f, 22f, 52f, 15f);
            Building("Edificio_E", 41f, 66f, -6f, 18f, 17f);
            Building("Edificio_E2", 41f, 66f, 20f, 52f, 22f);
            Building("Edificio_N", -40f, 34f, 54f, 70f, 19f);
            ComisariaV2Exterior.Fire(ext, new Vector3(26f, 0.3f, -16f), 0.8f, fire, smoke);
            ComisariaV2Exterior.Fire(ext, new Vector3(-30f, 0.3f, -12f), 0.7f, fire, smoke);
            foreach (var p in new[] { new Vector3(-58f, 26f, 4f), new Vector3(52f, 24f, -30f), new Vector3(8f, 28f, 62f) })
                ComisariaV2Exterior.Particles("Columna_Humo", ext, p, smoke, 6f, new Vector2(8f, 14f), new Vector2(2f, 4f), new Vector2(6f, 12f),
                    new Color(0.08f, 0.08f, 0.08f, 0.6f), new Color(0.2f, 0.2f, 0.2f, 0.2f), 3f, -0.03f, 80);
            var junk = new (string path, Vector3 p, float yaw)[]
            {
                (Props + "Rubble.fbx", new Vector3(-8f, 0f, -14f), 30f), (Props + "Rubble.fbx", new Vector3(18f, 0f, -16f), 140f),
                (Props + "Barrel.fbx", new Vector3(-5f, 0f, -12.6f), 0f), (Props + "Barrel.fbx", new Vector3(28f, 0f, -12.8f), 0f),
                (Props + "Crate.fbx", new Vector3(9f, 0f, -12.4f), 35f), (Props + "Crate.fbx", new Vector3(-24f, 0f, -5f), 10f),
                (Props + "Crate.fbx", new Vector3(14f, 0f, -6f), 60f), (Props + "Rubble.fbx", new Vector3(-10f, 0f, -3f), 80f),
                (Props + "Barrel.fbx", new Vector3(-35.6f, 0f, 12.5f), 0f), (Props + "Crate.fbx", new Vector3(-35.2f, 0f, 38.5f), 20f),   // el callejon mide ahora 5 m (x -37..-32): la rampa del garaje va al oeste
                (Props + "Barrel.fbx", new Vector3(-34.2f, 0f, 41.5f), 0f), (Props + "Rubble.fbx", new Vector3(-35.8f, 0f, 14.5f), 45f),
            };
            foreach (var (path, p, yaw) in junk) Model(path, ext, p, yaw, true);

            // zombis agolpados tras la verja de delante
            var zr = new GameObject("Zombis_Verja").transform; zr.SetParent(ext);
            string[] types = { "Zombie_Civil", "Zombie_Girl", "Zombie_Cop", "Zombie_Oficial", "Zombie_Pxl1", "Zombie_Cop", "Zombie_Civil", "Zombie_Girl", "Zombie_Pxl2", "Zombie_Oficial" };   // solo los aprobados
            for (int i = 0; i < types.Length; i++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + types[i] + ".prefab");
                if (pf == null) continue;
                var z = (GameObject)PrefabUtility.InstantiatePrefab(pf, zr);
                z.name = "Verja_" + types[i];
                z.transform.SetPositionAndRotation(new Vector3(-24f + i * 5.2f + (i % 2) * 0.4f, 1f, -8.75f - (i % 3) * 0.25f), Quaternion.identity);
                z.AddComponent<FenceRattler>().faceDir = Vector3.forward;
            }
        }

        [MenuItem("Horror/Comisaria grande/2 Exterior e intro")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase A";
            var level = rootGo.transform;
            foreach (var n in new[] { "Exterior", "Intro" }) { var o = level.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
            ItemTextureKit.Apply();
            ext = new GameObject("Exterior").transform; ext.SetParent(level);
            ComisariaV2Exterior.ext = ext; ComisariaV2Exterior.level = level;
            var fire = ComisariaV2Exterior.ParticleMat("FX_Fuego", new Color(1f, 0.6f, 0.25f, 1f), true);
            var smoke = ComisariaV2Exterior.ParticleMat("FX_Humo", new Color(0.3f, 0.3f, 0.3f, 1f), false);
            Exterior(fire, smoke);

            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null) { sun.color = new Color(0.55f, 0.62f, 0.8f); sun.intensity = 0.25f; sun.transform.rotation = Quaternion.Euler(38f, 160f, 0f); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.011f; RenderSettings.fogColor = new Color(0.07f, 0.06f, 0.06f);

            var intro = new GameObject("Intro"); intro.transform.SetParent(level);
            var ic = intro.AddComponent<IntroCutscene>();
            ic.focus = new Vector3(0f, 5f, 18f); ic.radius = 52f; ic.height = 34f; ic.endPoint = new Vector3(0f, 2.0f, -7.2f);
            ic.sealOnEnd = new Door[0];
            var mainDoors = new[] { "Puerta_Principal_O", "Puerta_Principal_E" }.Select(n => level.Find(n)).Where(t => t != null).Select(t => t.GetComponentInChildren<Door>()).ToArray();
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.transform.SetPositionAndRotation(new Vector3(-1.2f, 1.05f, -5.6f), Quaternion.identity);
            var flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null) { flow.startObjective = "Entra en la comisaría."; EditorUtility.SetDirty(flow); }
            var seal = new GameObject("Atrancar_Entrada"); seal.transform.SetParent(intro.transform);
            seal.transform.position = new Vector3(0f, 1.5f, 2.6f);
            var sbc = seal.AddComponent<BoxCollider>(); sbc.isTrigger = true; sbc.size = new Vector3(3.4f, 3f, 1.2f);
            var se = seal.AddComponent<SealOnEnter>(); se.doors = mainDoors; se.objective = "Explora la comisaría y busca una salida.";

            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase B: exterior (" + ext.childCount + " piezas), intro y puerta que se atranca (" + mainDoors.Length + " hojas)";
        }
    }
}
#endif
