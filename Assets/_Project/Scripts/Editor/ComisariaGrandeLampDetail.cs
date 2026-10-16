#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, lavado de cara del interior, tanda 3 (menu Horror/Comisaria grande/14 Detalle de luminarias): las luminarias de techo son un cajon
    /// oscuro con un panel emisivo. Cada una recibe un unico hijo "Marco" (una malla combinada de ~10 cajas, estatica y por tanto agrupable): marco de
    /// aluminio alrededor del difusor, dos tapas laterales, dos escuadras de anclaje, rejilla central y tornillos. No se toca la luz ni el panel (los usa CeilingLamp).
    /// Repetible: borra los "Marco" y los vuelve a crear. Se vuelve a lanzar tras rehacer las lamparas (menu 7a).
    /// Calidad AA (2026-10-16): si existe Art/Props/Lamp/CeilingLampAA.fbx (build_furniture_aa.py), el "Marco" es esa pantalla fluorescente (carcasa, marco
    /// de aluminio, rejilla parabolica delante del panel emisivo y tapas) y la caja "Housing" deja de dibujarse; el panel "Bulb" y la luz no cambian.
    /// </summary>
    public static class ComisariaGrandeLampDetail
    {
        const string MeshPath = "Assets/_Project/Art/Props/Lamp/LampFrame.asset";
        const string MatPath = "Assets/_Project/Materials/Lampara_Marco.mat";

        static Mesh BuildMesh()
        {
            var parts = new List<(Vector3 c, Vector3 s)>
            {
                (new Vector3(0f, -0.047f, 0.157f), new Vector3(1.24f, 0.014f, 0.022f)),     // marco: borde largo +z
                (new Vector3(0f, -0.047f, -0.157f), new Vector3(1.24f, 0.014f, 0.022f)),    // borde largo -z
                (new Vector3(0.61f, -0.047f, 0f), new Vector3(0.022f, 0.014f, 0.34f)),      // borde corto +x
                (new Vector3(-0.61f, -0.047f, 0f), new Vector3(0.022f, 0.014f, 0.34f)),     // borde corto -x
                (new Vector3(0.668f, 0.0f, 0f), new Vector3(0.03f, 0.082f, 0.45f)),         // tapa lateral +x
                (new Vector3(-0.668f, 0.0f, 0f), new Vector3(0.03f, 0.082f, 0.45f)),        // tapa lateral -x
                (new Vector3(0.32f, -0.046f, 0f), new Vector3(0.012f, 0.012f, 0.3f)),       // listones del difusor
                (new Vector3(0f, -0.046f, 0f), new Vector3(0.012f, 0.012f, 0.3f)),
                (new Vector3(-0.32f, -0.046f, 0f), new Vector3(0.012f, 0.012f, 0.3f)),
                (new Vector3(0.45f, 0.045f, 0.205f), new Vector3(0.05f, 0.012f, 0.03f)),    // escuadras de anclaje a la caja
                (new Vector3(-0.45f, 0.045f, -0.205f), new Vector3(0.05f, 0.012f, 0.03f)),
                (new Vector3(0.62f, -0.047f, 0.157f), new Vector3(0.02f, 0.02f, 0.02f)),    // tornillos de las esquinas
                (new Vector3(-0.62f, -0.047f, -0.157f), new Vector3(0.02f, 0.02f, 0.02f)),
            };
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var ci = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++) ci[i] = new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(parts[i].c, Quaternion.identity, parts[i].s) };
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            bool isNew = m == null;
            if (isNew) m = new Mesh();
            m.Clear(); m.name = "LampFrame";
            m.CombineMeshes(ci, true, true);
            m.RecalculateBounds();
            if (isNew) { System.IO.Directory.CreateDirectory("Assets/_Project/Art/Props/Lamp"); AssetDatabase.CreateAsset(m, MeshPath); } else EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Horror/Comisaria grande/14 Detalle de luminarias")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---");
            if (level == null) return "falta el nivel";
            var mesh = BuildMesh();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, MatPath); }
            mat.SetColor("_BaseColor", new Color(0.42f, 0.43f, 0.45f)); mat.SetFloat("_Metallic", 0.8f); mat.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(mat);
            int n = 0;
            var aa = AssetDatabase.LoadAssetAtPath<GameObject>(AAMaterialKit.LampPath);     // pantalla AA (fase C): sustituye a la caja y al marco
            foreach (var lamp in level.GetComponentsInChildren<CeilingLamp>(true))
            {
                var old = lamp.transform.Find("Marco"); if (old != null) Object.DestroyImmediate(old.gameObject);
                var housing = lamp.transform.Find("Housing"); if (housing == null) continue;
                var hr = housing.GetComponent<MeshRenderer>();
                if (aa != null)
                {
                    if (hr != null) hr.enabled = false;                                             // la caja se queda (sin dibujarse) por si algo la busca
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(aa, lamp.transform);
                    inst.name = "Marco";
                    inst.transform.position = housing.position;
                    inst.transform.rotation = aa.transform.rotation;                                 // la del FBX en ejes del mundo (X = largo, como la caja)
                    foreach (var ir in inst.GetComponentsInChildren<MeshRenderer>()) ir.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    foreach (var t in inst.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
                    n++;
                    continue;
                }
                if (hr != null) hr.enabled = true;
                var go = new GameObject("Marco");
                go.transform.SetParent(lamp.transform, true);
                go.transform.position = housing.position; go.transform.rotation = Quaternion.identity;       // ejes del mundo: la malla esta hecha con X = largo de la luminaria
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.isStatic = true;
                n++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return (aa != null ? "luminarias AA: " : "luminarias con marco: ") + n;
        }
    }
}
#endif
