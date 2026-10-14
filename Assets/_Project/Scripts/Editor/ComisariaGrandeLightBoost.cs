#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande (menu Horror/Comisaria grande/15 Luz de relleno): mide el brillo medio de cada sala (4 vistas desde el centro, a 1,5 m del suelo, 80 grados)
    /// y a las que quedan por debajo del objetivo les anade luces puntuales de relleno sin sombras en una rejilla (separacion 6 a 9 m), en pasadas hasta llegar
    /// al objetivo o a 3 pasadas. Cuelgan de "Luz_Relleno" bajo el nivel y se rehacen en cada ejecucion (no tocan las lamparas del menu 7). Las salas que son
    /// oscuras a proposito (sala de pruebas) y la sala del jefe se excluyen.
    /// </summary>
    public static class ComisariaGrandeLightBoost
    {
        public const float Target = 0.2f;
        static readonly string[] Skip = { "G_Dark" };

        public static float Measure(Camera cam, RenderTexture rt, Texture2D tex, ComisariaGrande.Room r)
        {
            var c = r.r.center; float sum = 0; int cnt = 0;
            for (int k = 0; k < 4; k++)
            {
                cam.transform.position = new Vector3(c.x, r.floor + 1.5f, c.y); cam.transform.rotation = Quaternion.Euler(8f, k * 90f, 0f); cam.Render();
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
                foreach (var p in tex.GetPixels()) { sum += 0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b; cnt++; }
            }
            return sum / cnt;
        }

        [MenuItem("Horror/Comisaria grande/15 Luz de relleno")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---"); if (level == null) return "falta el nivel";
            var old = level.transform.Find("Luz_Relleno"); if (old != null) Object.DestroyImmediate(old.gameObject);
            ComisariaGrande.Define();
            var root = new GameObject("Luz_Relleno").transform; root.SetParent(level.transform);
            var cam = new GameObject("lumcam").AddComponent<Camera>(); cam.fieldOfView = 80; cam.nearClipPlane = 0.1f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.enabled = false;
            var rt = new RenderTexture(96, 54, 24); cam.targetTexture = rt; var tex = new Texture2D(96, 54);
            var log = new List<string>(); int total = 0;
            var rooms = ComisariaGrande.Rooms.Where(r => r.Indoor && !Skip.Contains(r.id)).ToList();
            foreach (var r in rooms)
            {
                float lum = Measure(cam, rt, tex, r); float first = lum; int added = 0; float spacing = 9f;
                for (int pass = 0; pass < 3 && lum < Target; pass++)
                {
                    spacing = pass == 0 ? 9f : pass == 1 ? 7f : 5.5f;
                    foreach (var l in root.Cast<Transform>().Where(t => t.name == "Relleno_" + r.id).ToList()) Object.DestroyImmediate(l.gameObject);
                    added = 0;
                    int nx = Mathf.Max(1, Mathf.RoundToInt(r.r.width / spacing)), nz = Mathf.Max(1, Mathf.RoundToInt(r.r.height / spacing));
                    for (int i = 0; i < nx; i++)
                        for (int j = 0; j < nz; j++)
                        {
                            var p = new Vector3(r.r.xMin + (i + 0.5f) * r.r.width / nx, r.floor + Mathf.Min(2.5f, r.ceil - r.floor - 0.4f), r.r.yMin + (j + 0.5f) * r.r.height / nz);
                            var g = new GameObject("Relleno_" + r.id); g.transform.SetParent(root); g.transform.position = p;
                            var lt = g.AddComponent<Light>(); lt.type = LightType.Point; lt.color = Color.Lerp(r.light, Color.white, 0.3f); lt.range = spacing * 0.95f + 1.5f; lt.intensity = 3.2f + pass * 1.2f; lt.shadows = LightShadows.None;
                            added++;
                        }
                    lum = Measure(cam, rt, tex, r);
                }
                if (added > 0) { total += added; log.Add(r.id + ": " + first.ToString("F2") + " -> " + lum.ToString("F2") + " (" + added + " luces)"); }
            }
            Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(rt);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "luz de relleno: " + total + " luces. " + string.Join("; ", log);
        }
    }
}
#endif
