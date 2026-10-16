#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, lavado de cara del exterior, fase 4 (menu Horror/Comisaria grande/12 Fachada de la comisaria): lo primero que se ve al empezar.
    /// Placa SECTOR 7 / POLICE DEPARTMENT sobre la marquesina (sustituye al rotulo de chapa liso), dos banderas en mastiles inclinados, lamparas de pared
    /// con luz, jardineras secas flanqueando la puerta, sacos de arena y focos de obra en el patio, alambre de espino sobre la verja y senales de peligro.
    /// Todo cuelga de "Fachada_Detalle" y es repetible. Va con un 18 % menos de luz que el exterior para no quemar la entrada.
    /// </summary>
    public static class ComisariaGrandeFront
    {
        const string Front = "Assets/_Project/Art/Props/Front/";
        const string Mats = "Assets/_Project/Materials/";
        static Transform root;

        [MenuItem("Horror/Comisaria grande/12 Fachada de la comisaria")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        static GameObject Place(string model, Vector3 pos, float yaw, bool collider, string name = null)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Front + model + ".fbx");
            if (pf == null) { Debug.LogWarning("[Horror] falta " + model); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root);
            go.name = name ?? model;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * pf.transform.localRotation);
            if (collider)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                var bc = go.AddComponent<BoxCollider>();
                if (mf != null) { bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size; }
            }
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return go;
        }

        static Light Lamp(string name, Vector3 pos, Color c, float intensity, float range, LightType type = LightType.Point, float spot = 90f, Quaternion? rot = null)
        {
            var g = new GameObject(name); g.transform.SetParent(root); g.transform.position = pos;
            if (rot.HasValue) g.transform.rotation = rot.Value;
            var l = g.AddComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            if (type == LightType.Spot) l.spotAngle = spot;
            return l;
        }

        static Material FlagMaterial(string name, string tex)
        {
            string path = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/" + tex + ".png"));
            m.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.8f, 1f));
            m.SetFloat("_Cull", 0f);                        // tela de doble cara
            m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---");
            if (level == null) return "falta el nivel";
            var old = level.transform.Find("Fachada_Detalle");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("Fachada_Detalle").transform; root.SetParent(level.transform);
            int n = 0;

            // el rotulo de chapa liso del menu 2 y su luz se sustituyen por la placa
            var ext = level.transform.Find("Exterior");
            if (ext != null) foreach (Transform t in ext) if (t.name == "Rotulo") t.gameObject.SetActive(false);

            // rotulo sobre la marquesina (la fachada mira a -Z: yaw 180). El revestimiento Fachada_S tiene la cara en z -0,26: la trasera del rotulo AA
            // queda 5 mm por delante (antes la placa estaba en el mismo plano y se metia en el muro: parpadeaba). Por debajo de las ventanas de la 1.a planta.
            var sign = Place("StationSign", new Vector3(0f, 3.12f, -0.265f), 180f, false); if (sign != null) n++;
            Lamp("Luz_Placa", new Vector3(0f, 3.4f, -1.4f), new Color(1f, 0.9f, 0.7f), 1.8f, 5f);

            // banderas: mastiles inclinados a ambos lados de la placa, con tela de la comisaria y de la ciudad
            var cloths = new[] { (-5.2f, "Bandera_Policia", "tex_flag_police"), (5.2f, "Bandera_Ciudad", "tex_flag_city") };
            foreach (var c in cloths)
            {
                var pole = Place("FlagPole", new Vector3(c.Item1, 5.4f, -0.26f), 180f, false); if (pole != null) n++;
                var cloth = Place("FlagCloth", new Vector3(c.Item1, 7.6f, -1.71f), 180f, false, c.Item2);
                if (cloth != null) { foreach (var r in cloth.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = FlagMaterial(c.Item2, c.Item3); r.shadowCastingMode = ShadowCastingMode.Off; } n++; }
            }

            // lamparas de pared con luz (fachada sur, entre ventanas) y a los lados de la puerta
            foreach (float x in new[] { -3.6f, 3.6f, -9f, 9f, -15f, 15f, -21f, 21f })
            {
                var lamp = Place("WallLamp", new Vector3(x, 3.0f, -0.26f), 180f, false); if (lamp != null) n++;
                Lamp("Luz_Lampara", new Vector3(x, 3.0f, -0.75f), new Color(1f, 0.75f, 0.45f), 5f, 7f);
            }

            // jardineras secas y sacos de arena
            foreach (var p in new[] { (-6.6f, -3.1f, 0f), (6.6f, -3.1f, 0f), (-13f, -3.1f, 2f), (13f, -3.1f, -3f) }) { var g = Place("Planter", new Vector3(p.Item1, 0f, p.Item2), p.Item3, true); if (g != null) n++; }
            foreach (var p in new[] { (-9.5f, -6.9f, 8f), (10.5f, -7.1f, -10f), (-24f, -7.0f, 185f), (22f, -2.2f, 90f) }) { var g = Place("SandbagWall", new Vector3(p.Item1, 0f, p.Item2), p.Item3, true); if (g != null) n++; }

            // focos de obra en el patio, apuntando a la verja y a la calle
            foreach (var p in new[] { (-17.5f, -4.5f), (17.5f, -4.5f) })
            {
                var f = Place("Floodlight", new Vector3(p.Item1, 0f, p.Item2), 180f, true); if (f != null) n++;
                Lamp("Luz_Foco", new Vector3(p.Item1 - 0.38f, 3.1f, p.Item2 - 0.28f), new Color(0.95f, 0.95f, 1f), 70f, 22f, LightType.Spot, 70f, Quaternion.Euler(26f, 190f, 0f));
                Lamp("Luz_Foco", new Vector3(p.Item1 + 0.38f, 3.1f, p.Item2 - 0.28f), new Color(0.95f, 0.95f, 1f), 70f, 22f, LightType.Spot, 70f, Quaternion.Euler(26f, 170f, 0f));
            }

            // verja: alambre de espino arriba y senales de peligro (la verja de delante esta en z -8 y mide 2,75 m)
            for (float x = -40.5f; x <= 35.5f; x += 3f) { var w = Place("RazorWire", new Vector3(x, 2.72f, -8f), 0f, false); if (w != null) n++; }
            for (float x = -36f; x <= 33f; x += 12f) { var s = Place("SignHazard", new Vector3(x, 1.7f, -8.12f), 180f, false); if (s != null) n++; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fachada: " + n + " piezas";
        }
    }
}
#endif
