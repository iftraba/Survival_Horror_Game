#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, lavado de cara del interior (menu Horror/Comisaria grande/13 Molduras de pared): da relieve a las paredes lisas sin rehacer el nivel.
    /// Cada "Rodapie" que genera el menu 1 marca un tramo de pared interior con su lado, su largo y el suelo de su sala; sobre el se pone un friso oscuro
    /// (lambrin) hasta 1,05 m, una moldura de madera encima (media caña) y una cornisa de techo. Las puertas quedan libres porque alli no hay rodapie.
    /// Todo cuelga de "Interior_Molduras" bajo el nivel, es estatico y sin colision. Repetible: rehace solo ese grupo.
    /// </summary>
    public static class ComisariaGrandeTrim
    {
        const string Mats = "Assets/_Project/Materials/";

        static Material Tinted(string name, string baseMat, Color tint)
        {
            string path = Mats + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var src = AssetDatabase.LoadAssetAtPath<Material>(Mats + baseMat + ".mat");
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            m.CopyPropertiesFromMaterial(src);
            m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Horror/Comisaria grande/13 Molduras de pared")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---");
            if (level == null) return "falta el nivel";
            var old = level.transform.Find("Interior_Molduras");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            ComisariaGrande.Define();
            var root = new GameObject("Interior_Molduras").transform; root.SetParent(level.transform);
            var dado = Tinted("Env_Lambrin", "Env_Wall", new Color(0.42f, 0.5f, 0.46f, 1f));
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            var cornice = Tinted("Env_Cornisa", "Env_Wall", new Color(0.82f, 0.82f, 0.78f, 1f));
            int pieces = 0, skipped = 0;
            foreach (var t in level.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Rodapie" || t.parent == root) continue;
                var p = t.position; var s = t.localScale;
                bool alongX = s.x > s.z;                                   // el largo va en X si es mas largo que ancho
                float len = alongX ? s.x : s.z, fy = p.y - 0.07f;
                // lado: el rodapie esta a +-(t/2 + 0,02) del eje de la pared; la normal hacia la sala es el signo del desplazamiento. Se busca la sala que contiene el punto de dentro.
                Vector2 inward = alongX ? new Vector2(0, 1) : new Vector2(1, 0);
                ComisariaGrande.Room room = null; float sign = 0f;
                foreach (float sg in new[] { 1f, -1f })
                {
                    var q = new Vector2(p.x, p.z) + inward * sg * 0.45f;
                    foreach (var r in ComisariaGrande.Rooms) if (r.Indoor && Mathf.Abs(r.floor - fy) < 0.12f && r.r.Contains(q)) { room = r; sign = sg; break; }
                    if (room != null) break;
                }
                if (room == null) { skipped++; continue; }
                float ceil = room.ceil;
                Vector3 n = alongX ? new Vector3(0, 0, sign) : new Vector3(sign, 0, 0);       // hacia la sala
                Vector3 baseP = new Vector3(p.x, 0, p.z) - n * 0.02f;                           // sobre la pared (el rodapie sobresale 0,02 + 0,02)
                void Strip(string name, float y0, float y1, float protrude, Material mat)
                {
                    var size = alongX ? new Vector3(len, y1 - y0, protrude) : new Vector3(protrude, y1 - y0, len);
                    var c = new Vector3(baseP.x, (y0 + y1) / 2f, baseP.z) + n * (protrude / 2f);
                    var g = ComisariaV2Exterior.Box(name, root, c, size, mat, 3f, false);
                    g.isStatic = true;
                }
                if (fy + 1.2f < ceil - 0.4f) { Strip("Lambrin", fy + 0.14f, fy + 1.05f, 0.04f, dado); pieces++; Strip("Moldura", fy + 1.05f, fy + 1.1f, 0.075f, wood); pieces++; }
                if (ceil - fy > 2.5f) { Strip("Cornisa", ceil - 0.14f, ceil, 0.1f, cornice); pieces++; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "molduras: " + pieces + " piezas (" + skipped + " rodapies sin sala)";
        }
    }
}
#endif
