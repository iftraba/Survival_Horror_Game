#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria v2, fase 2 (menu Horror/Comisaria v2/2 Vestibulo y exterior). Sobre la escena de la fase 1:
    ///  - Vestibulo: mostrador de recepcion en L, bancos de espera, archivadores y sillas detras del mostrador.
    ///  - Exterior: fachada de la comisaria, patio de acera, verja de barrotes alrededor con zombis agolpados detras (FenceRattler),
    ///    coche patrulla quemado y ardiendo en la entrada, otro coche abandonado en la calle, farolas, escombros, cajas y bidones,
    ///    edificios alrededor (fachadas de ladrillo con ventanas, algunas encendidas), fuegos y columnas de humo.
    ///  - Escena de camara del principio (IntroCutscene) que acaba con el jugador en el vestibulo y la puerta principal atrancada.
    /// Repetible: rehace los grupos "Exterior", "Vestibulo_Props" e "Intro" y vuelve a hornear el NavMesh.
    /// </summary>
    public static class ComisariaV2Exterior
    {
        const string Ext = "Assets/_Project/Art/Props/Exterior/";
        const string Props = "Assets/_Project/Art/Props/";
        const string Mats = "Assets/_Project/Materials/";
        const string Tex = "Assets/_Project/Art/Textures/";

        static Transform ext, lobby, level;

        static T Call<T>(string method, params object[] args) =>
            (T)typeof(TestSceneBuilder).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

        static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material m, float tile, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = c; go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (tile > 0f) go.GetComponent<MeshFilter>().sharedMesh = Call<Mesh>("TiledUnitCube", c, s, tile);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        /// <summary>Modelo (FBX horneado) colocado con su giro de Blender; con colision de caja opcional.</summary>
        static GameObject Model(string path, Transform parent, Vector3 pos, float yaw, bool collider, string name = null)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[Horror] falta " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            if (name != null) go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0) * pf.transform.localRotation);
            if (collider)
            {
                var r = go.GetComponentInChildren<Renderer>();
                var bc = go.AddComponent<BoxCollider>();
                // caja en ejes locales del objeto a partir de los limites de la malla
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null) { bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size; }
            }
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return go;
        }

        // ------------------------------------------------------------------ materiales de la ciudad
        static Material CityMat(string name, string tex, bool emission)
        {
            foreach (var (suffix, normal, linear) in new[] { ("", false, false), ("_n", true, true), ("_ms", false, true), ("_ao", false, true), ("_emis", false, false) })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(Tex + tex + suffix + ".png");
                if (ti == null) continue;
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !(normal || linear);
                ti.alphaSource = suffix == "_ms" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.anisoLevel = 8; ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            string path = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            Texture2D T(string s) => AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + s + ".png");
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", T(""));
            m.SetTexture("_BumpMap", T("_n")); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", T("_ms")); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_SmoothnessTextureChannel", 0); m.SetFloat("_Smoothness", 1); m.SetFloat("_Metallic", 1);
            m.SetTexture("_OcclusionMap", T("_ao")); m.EnableKeyword("_OCCLUSIONMAP");
            if (emission && T("_emis") != null)
            {
                m.SetTexture("_EmissionMap", T("_emis")); m.SetColor("_EmissionColor", new Color(1.6f, 1.3f, 0.9f)); m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ fuego y humo
        static Material ParticleMat(string name, Color color, bool additive)
        {
            string path = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "particle_soft.png"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f); m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", additive ? 1f : 5f); m.SetFloat("_DstBlend", additive ? 1f : 10f);
            m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        static ParticleSystem Particles(string name, Transform parent, Vector3 pos, Material mat, float rate, Vector2 life, Vector2 speed, Vector2 size,
                                        Color c0, Color c1, float radius, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent); go.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, 0, 0));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true; main.loop = true;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = radius;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(c0.a, 0.12f), new GradientAlphaKey(c1.a, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        /// <summary>Fuego con humo, chispas y luz que parpadea. 'scale' 1 = fuego de un coche.</summary>
        static void Fire(Transform parent, Vector3 pos, float scale, Material fire, Material smoke)
        {
            var root = new GameObject("Fuego").transform; root.SetParent(parent); root.position = pos;
            Particles("Llamas", root, pos, fire, 40 * scale, new Vector2(0.5f, 1.1f), new Vector2(1.2f, 2.4f) * Mathf.Sqrt(scale), new Vector2(0.6f, 1.3f) * scale,
                      new Color(1f, 0.55f, 0.15f, 0.9f), new Color(0.8f, 0.2f, 0.05f, 0.4f), 0.5f * scale, -0.1f, 120);
            Particles("Humo", root, pos + Vector3.up * 0.8f * scale, smoke, 10 * scale, new Vector2(3f, 6f), new Vector2(1.2f, 2.2f), new Vector2(1.5f, 3f) * scale,
                      new Color(0.12f, 0.11f, 0.1f, 0.65f), new Color(0.25f, 0.24f, 0.23f, 0.25f), 0.4f * scale, -0.02f, 120);
            Particles("Chispas", root, pos, fire, 12 * scale, new Vector2(0.8f, 1.8f), new Vector2(2f, 4f), new Vector2(0.04f, 0.09f),
                      new Color(1f, 0.7f, 0.3f, 1f), new Color(1f, 0.3f, 0.05f, 0.8f), 0.6f * scale, -0.2f, 60);
            var lg = new GameObject("Luz"); lg.transform.SetParent(root); lg.transform.position = pos + Vector3.up * 1.2f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.intensity = 6f * scale; l.range = 10f * scale;
            l.shadows = LightShadows.None;
            lg.AddComponent<FireLight>();
        }

        // ------------------------------------------------------------------ vestibulo
        static void Lobby()
        {
            // mostrador de recepcion al fondo a la izquierda (la escalera de caracol esta a la derecha), mirando a la entrada
            Model(Ext + "ReceptionDesk.fbx", lobby, new Vector3(-2.4f, 0f, 12.6f), 180f, true, "Recepcion");
            Model(Props + "Chair.fbx", lobby, new Vector3(-3.6f, 0f, 13.4f), 200f, true);
            Model(Props + "Chair.fbx", lobby, new Vector3(-1.2f, 0f, 13.5f), 160f, true);
            Model(Props + "FilingCabinet.fbx", lobby, new Vector3(-5.0f, 0f, 15.4f), 180f, true);
            Model(Props + "FilingCabinet.fbx", lobby, new Vector3(-4.3f, 0f, 15.4f), 180f, true);
            // bancos de espera contra las paredes de los lados (sin tapar las puertas, a z = 6)
            foreach (var (x, z, yaw) in new[] { (-7.6f, 2.6f, 90f), (-7.6f, 10.2f, 90f), (7.6f, 2.6f, -90f) })
                Model(Ext + "WaitingBench.fbx", lobby, new Vector3(x, 0f, z), yaw, true, "Banco");
            Model(Props + "Crate.fbx", lobby, new Vector3(6.6f, 0f, 3.6f), 25f, true);                 // cajas de un desalojo a medias
            Model(Props + "Barrel.fbx", lobby, new Vector3(-6.8f, 0f, 14.6f), 0f, true);
        }

        // ------------------------------------------------------------------ exterior
        static void Exterior(Material fire, Material smoke)
        {
            var wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            var metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            var facA = CityMat("Env_FacadeA", "tex_facade_a", true);
            var facB = CityMat("Env_FacadeB", "tex_facade_b", true);
            var asphalt = CityMat("Env_Asphalt", "tex_asphalt", false);
            var sidewalk = CityMat("Env_Sidewalk", "tex_sidewalk", false);
            var calle = level.Find("Calle"); if (calle != null) Object.DestroyImmediate(calle.gameObject);

            // suelos: patio y aceras alrededor de la comisaria (dentro de la verja), acera de fuera, calzada y acera de enfrente
            // la verja va pegada a la comisaria: patio de 8 m delante (donde empieza el jugador) y 2,5 m a los lados y detras
            Box("Patio", ext, new Vector3(0, -0.1f, -4f), new Vector3(45f, 0.2f, 8f), sidewalk, 4f);
            Box("Patio_O", ext, new Vector3(-21.25f, -0.1f, 16f), new Vector3(2.5f, 0.2f, 32f), sidewalk, 4f);
            Box("Patio_E", ext, new Vector3(21.25f, -0.1f, 16f), new Vector3(2.5f, 0.2f, 32f), sidewalk, 4f);
            Box("Patio_N", ext, new Vector3(0, -0.1f, 33.25f), new Vector3(45f, 0.2f, 2.5f), sidewalk, 4f);
            Box("Acera", ext, new Vector3(0, -0.1f, -9f), new Vector3(140f, 0.2f, 2f), sidewalk, 4f);
            Box("Calzada", ext, new Vector3(0, -0.15f, -14f), new Vector3(140f, 0.2f, 8f), asphalt, 4f);
            Box("Acera_Enfrente", ext, new Vector3(0, -0.1f, -19f), new Vector3(140f, 0.2f, 2f), sidewalk, 4f);
            Box("Bordillo", ext, new Vector3(0, 0.0f, -9.95f), new Vector3(140f, 0.2f, 0.12f), wall, 3f);
            foreach (float sx in new[] { -1f, 1f })                                                     // callejones a los lados
                Box("Solar", ext, new Vector3(sx * 41f, -0.12f, 22f), new Vector3(37f, 0.2f, 60f), asphalt, 4f);
            Box("Solar_N", ext, new Vector3(0, -0.12f, 44f), new Vector3(45f, 0.2f, 19f), asphalt, 4f);

            // fachada de la comisaria: hormigon con ventanas, a 5 cm de los muros, con el hueco de la puerta principal
            float top = ComisariaV2Builder.ArchiveY + 7.2f;
            void Clad(string n, Vector3 c, Vector3 s) => Box(n, ext, c, s, facB, 6f, false);
            Clad("Fachada_S_O", new Vector3(-10.9f, top / 2f, -0.23f), new Vector3(18.6f, top, 0.06f));
            Clad("Fachada_S_E", new Vector3(10.9f, top / 2f, -0.23f), new Vector3(18.6f, top, 0.06f));
            Clad("Fachada_S_Dintel", new Vector3(0f, (top + 2.5f) / 2f, -0.23f), new Vector3(3.2f, top - 2.5f, 0.06f));
            Clad("Fachada_N", new Vector3(0f, top / 2f, 32.23f), new Vector3(40.5f, top, 0.06f));
            Clad("Fachada_O", new Vector3(-20.23f, top / 2f, 16f), new Vector3(0.06f, top, 32.5f));
            Clad("Fachada_E", new Vector3(20.23f, top / 2f, 16f), new Vector3(0.06f, top, 32.5f));
            Box("Marquesina", ext, new Vector3(0f, 3.0f, -1.4f), new Vector3(6f, 0.18f, 2.6f), metal, 1f, false);   // tejadillo de la entrada
            foreach (float x in new[] { -2.8f, 2.8f }) Box("Pilar_Marquesina", ext, new Vector3(x, 1.45f, -2.55f), new Vector3(0.18f, 2.9f, 0.18f), metal, 1f);
            var sign = Box("Rotulo", ext, new Vector3(0f, 3.55f, -0.32f), new Vector3(5.2f, 0.7f, 0.12f), metal, 1f, false);
            var sl = new GameObject("Luz_Rotulo"); sl.transform.SetParent(ext); sl.transform.position = new Vector3(0, 3.1f, -1.2f);
            var sll = sl.AddComponent<Light>(); sll.type = LightType.Point; sll.color = new Color(0.7f, 0.85f, 1f); sll.intensity = 3f; sll.range = 7f;

            // verja alrededor (x -24..24, z -12..38) con colision; tramos de 3 m
            var fence = new GameObject("Verja").transform; fence.SetParent(ext);
            void FenceLine(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a; float len = d.magnitude; int n = Mathf.CeilToInt(len / 3f);
                float yaw = Quaternion.LookRotation(Vector3.Cross(Vector3.up, d.normalized)).eulerAngles.y;
                for (int i = 0; i < n; i++)
                {
                    var p = a + d * ((i + 0.5f) / n);
                    var seg = Model(Ext + "FenceSegment.fbx", fence, p, yaw, false, "Verja_Tramo");
                    if (seg == null) continue;
                    var bc = seg.AddComponent<BoxCollider>();
                    var mf = seg.GetComponentInChildren<MeshFilter>(); bc.center = mf.sharedMesh.bounds.center; bc.size = Vector3.Scale(mf.sharedMesh.bounds.size, new Vector3(1f, 6f, 1f));
                }
            }
            FenceLine(new Vector3(-22.5f, 0, -8), new Vector3(22.5f, 0, -8));
            FenceLine(new Vector3(-22.5f, 0, -8), new Vector3(-22.5f, 0, 34.5f));
            FenceLine(new Vector3(22.5f, 0, -8), new Vector3(22.5f, 0, 34.5f));
            FenceLine(new Vector3(-22.5f, 0, 34.5f), new Vector3(22.5f, 0, 34.5f));

            // coche patrulla ardiendo en la entrada y otro abandonado en la calle
            Model(Ext + "PoliceCar.fbx", ext, new Vector3(4.6f, 0f, -4.4f), 25f, true, "CochePatrulla");
            Fire(ext, new Vector3(4.4f, 1.1f, -5.5f), 1.0f, fire, smoke);
            Model(Ext + "PoliceCar.fbx", ext, new Vector3(-13f, 0f, -14f), 100f, true, "CocheAbandonado");
            Fire(ext, new Vector3(-12f, 0.6f, -14.3f), 0.6f, fire, smoke);

            // farolas a lo largo de la acera de fuera (una de cada tres, fundida)
            for (int i = 0; i < 9; i++)
            {
                float x = -48f + i * 12f;
                Model(Ext + "StreetLamp.fbx", ext, new Vector3(x, 0f, -9.3f), 0f, true, "Farola");
                if (i % 3 == 1) continue;
                var lg = new GameObject("Luz_Farola"); lg.transform.SetParent(ext); lg.transform.position = new Vector3(x, 5.7f, -10.6f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Spot; l.spotAngle = 110f; l.color = new Color(1f, 0.78f, 0.5f); l.intensity = 9f; l.range = 13f;
                lg.transform.rotation = Quaternion.Euler(90, 0, 0);
            }

            // edificios alrededor: frente (otro lado de la calle), lados y fondo; alturas y fachadas variadas
            var rng = new System.Random(7);
            void Building(string n, float x0, float x1, float z0, float z1, float h)
            {
                var m = rng.NextDouble() < 0.5 ? facA : facB;
                Box(n, ext, new Vector3((x0 + x1) / 2f, h / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, h, z1 - z0), m, 6f);
                Box(n + "_Azotea", ext, new Vector3((x0 + x1) / 2f, h + 0.4f, (z0 + z1) / 2f), new Vector3(x1 - x0 + 0.4f, 0.8f, z1 - z0 + 0.4f), wall, 3f, false);
            }
            float[] fx = { -62f, -42f, -24f, -6f, 12f, 30f, 48f, 66f };
            for (int i = 0; i < fx.Length - 1; i++)
                Building("Edificio_Frente_" + i, fx[i] + 0.5f, fx[i + 1] - 0.5f, -34f, -20.5f, 10f + (float)rng.NextDouble() * 10f);
            Building("Edificio_O", -50f, -26f, -8f, 15f, 18f);
            Building("Edificio_O2", -50f, -26f, 17f, 40f, 13f);
            Building("Edificio_E", 26f, 50f, -8f, 13f, 15f);
            Building("Edificio_E2", 26f, 50f, 15f, 40f, 20f);
            Building("Edificio_N", -24f, 24f, 38f, 52f, 17f);
            // caos: fuegos en la calle, columnas de humo a lo lejos, escombros, cajas y bidones volcados
            Fire(ext, new Vector3(22f, 0.3f, -16f), 0.8f, fire, smoke);
            Fire(ext, new Vector3(-23f, 0.3f, -12f), 0.7f, fire, smoke);
            foreach (var p in new[] { new Vector3(-38f, 24f, 4f), new Vector3(40f, 22f, -28f), new Vector3(8f, 24f, 46f) })
                Particles("Columna_Humo", ext, p, smoke, 6f, new Vector2(8f, 14f), new Vector2(2f, 4f), new Vector2(6f, 12f),
                          new Color(0.08f, 0.08f, 0.08f, 0.6f), new Color(0.2f, 0.2f, 0.2f, 0.2f), 3f, -0.03f, 80);
            var junk = new (string path, Vector3 p, float yaw)[]
            {
                (Props + "Rubble.fbx", new Vector3(-8f, 0f, -14f), 30f), (Props + "Rubble.fbx", new Vector3(18f, 0f, -16f), 140f),
                (Props + "Barrel.fbx", new Vector3(-5f, 0f, -12.6f), 0f), (Props + "Barrel.fbx", new Vector3(28f, 0f, -12.8f), 0f),
                (Props + "Crate.fbx", new Vector3(9f, 0f, -12.4f), 35f), (Props + "Crate.fbx", new Vector3(-18f, 0f, -5f), 10f),
                (Props + "Crate.fbx", new Vector3(14f, 0f, -6f), 60f), (Props + "Rubble.fbx", new Vector3(-10f, 0f, -3f), 80f),
            };
            foreach (var (path, p, yaw) in junk) Model(path, ext, p, yaw, true);
        }

        /// <summary>Zombis agolpados tras la verja de delante (no pueden pasar) y algunos por la calle.</summary>
        static void FenceZombies()
        {
            var zr = new GameObject("Zombis_Verja").transform; zr.SetParent(ext);
            string[] types = { "Zombie_Civil", "Zombie_Girl", "Zombie_Cop", "Zombie_Oficial", "Zombie_Pxl1", "Zombie_Pxl2", "Zombie_Civil", "Zombie_Girl" };
            float[] xs = { -15f, -10.5f, -6.8f, -2.5f, 2.2f, 7.0f, 11.6f, 16f };
            for (int i = 0; i < types.Length; i++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + types[i] + ".prefab");
                var z = (GameObject)PrefabUtility.InstantiatePrefab(pf, zr);
                z.name = "Verja_" + types[i];
                z.transform.SetPositionAndRotation(new Vector3(xs[i] + (i % 2) * 0.4f, 1f, -8.75f - (i % 3) * 0.25f), Quaternion.identity);
                var fr = z.AddComponent<FenceRattler>(); fr.faceDir = Vector3.forward;
            }
        }

        [MenuItem("Horror/Comisaria v2/2 Vestibulo y exterior")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaV2Builder.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaV2Builder.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase 1 (Horror/Comisaria v2/1 Estructura)";
            level = rootGo.transform;
            foreach (var n in new[] { "Exterior", "Vestibulo_Props", "Intro" }) { var o = level.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
            ItemTextureKit.Apply();                                                     // materiales horneados de los modelos nuevos
            ext = new GameObject("Exterior").transform; ext.SetParent(level);
            lobby = new GameObject("Vestibulo_Props").transform; lobby.SetParent(level.Find("Props"));
            var fire = ParticleMat("FX_Fuego", new Color(1f, 0.6f, 0.25f, 1f), true);
            var smoke = ParticleMat("FX_Humo", new Color(0.3f, 0.3f, 0.3f, 1f), false);
            Lobby();
            Exterior(fire, smoke);
            FenceZombies();

            // noche: luz direccional de luna, fria y baja; niebla para que la ciudad se pierda a lo lejos
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null) { sun.color = new Color(0.55f, 0.62f, 0.8f); sun.intensity = 0.25f; sun.transform.rotation = Quaternion.Euler(38f, 160f, 0f); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.012f; RenderSettings.fogColor = new Color(0.07f, 0.06f, 0.06f);

            // escena de camara del principio; al acabar atranca la puerta principal
            var intro = new GameObject("Intro"); intro.transform.SetParent(level);
            var ic = intro.AddComponent<IntroCutscene>();
            ic.radius = 34f; ic.height = 26f; ic.endPoint = new Vector3(0f, 2.0f, -7.2f);
            var mainDoors = new[] { "Puerta_Principal_O", "Puerta_Principal_E" }.Select(n => level.Find(n)).Where(t => t != null).Select(t => t.GetComponentInChildren<Door>()).ToArray();
            ic.sealOnEnd = new Door[0];
            // empieza fuera, en el patio entre la verja (con los zombis) y la comisaria, mirando a la puerta
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.transform.SetPositionAndRotation(new Vector3(-1.2f, 1.05f, -5.6f), Quaternion.identity);
            var flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null) { flow.startObjective = "Entra en la comisaría."; EditorUtility.SetDirty(flow); }
            // al pasar la puerta principal se cierra y se atranca detras
            var seal = new GameObject("Atrancar_Entrada"); seal.transform.SetParent(intro.transform);
            seal.transform.position = new Vector3(0f, 1.5f, 2.6f);
            var sbc = seal.AddComponent<BoxCollider>(); sbc.isTrigger = true; sbc.size = new Vector3(3.4f, 3f, 1.2f);
            seal.AddComponent<SealOnEnter>().doors = mainDoors;

            // NavMesh otra vez (muebles, verja y exterior), con las hojas de las puertas fuera
            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase 2: vestibulo, exterior (" + ext.childCount + " piezas), zombis tras la verja e intro";
        }
    }
}
#endif
