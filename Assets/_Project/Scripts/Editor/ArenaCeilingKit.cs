#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Sala del primer jefe con techo alto (2026-10-08), para que tenga sentido su salto: el techo pasa de 3 m a 6,5 m.
    /// Recorta las losas del techo general sobre la sala (z 9-20), pone un techo nuevo a 6,5 m, sube las paredes que la rodean,
    /// alarga las pilastras, sube la viga y las lamparas (con mas alcance). Menu: Horror/Techo alto en la sala del primer jefe.
    /// Repetible (rehace los objetos "ArenaTall_*" y solo recorta lo que aun no esta recortado).
    /// </summary>
    public static class ArenaCeilingKit
    {
        const float X0 = -15.5f, X1 = 15.5f, Z0 = 9.1f, Z1 = 20.5f;    // planta de la sala (con los muros)
        const float Ceil = 6.5f;                                        // cara inferior del techo nuevo
        const float OldTop = 3f;
        const string Mats = "Assets/_Project/Materials/";

        static Mesh Tiled(Vector3 pos, Vector3 size, float tile)
        {
            var m = typeof(TestSceneBuilder).GetMethod("TiledUnitCube", BindingFlags.NonPublic | BindingFlags.Static);
            return (Mesh)m.Invoke(null, new object[] { pos, size, tile });
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float tile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = center; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<MeshFilter>().sharedMesh = Tiled(center, size, tile);
            go.isStatic = true;
            return go;
        }

        /// <summary>Cambia el tamano de una caja del nivel conservando la textura a escala real.</summary>
        static void Resize(Transform t, Vector3 center, Vector3 size, float tile)
        {
            t.position = center; t.localScale = size;
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = Tiled(center, size, tile);
        }

        [MenuItem("Horror/Techo alto en la sala del primer jefe")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var log = new List<string>();
            var level = GameObject.Find("--- LEVEL ---").transform;
            var old = level.Find("ArenaTall");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("ArenaTall").transform;
            root.SetParent(level);
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            var ceilMat = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Ceiling.mat");

            // ---- losas del techo general: se recortan para que acaben en z = 9,1 (la sala queda sin techo bajo)
            var slabs = GameObject.Find("CeilingSlab");
            if (slabs != null)
                foreach (Transform s in slabs.transform)
                {
                    var r = s.GetComponent<Renderer>(); if (r == null) continue;
                    var b = r.bounds;
                    if (b.max.z <= Z0 + 0.01f || b.min.z >= Z1) continue;
                    if (b.min.z >= Z0 - 0.01f) { s.gameObject.SetActive(false); log.Add(s.name + " fuera"); continue; }   // entera dentro de la sala
                    var size = new Vector3(b.size.x, b.size.y, Z0 - b.min.z);
                    var center = new Vector3(b.center.x, b.center.y, (b.min.z + Z0) * 0.5f);
                    Resize(s, center, size, 2.4f);
                    log.Add(s.name + " recortada");
                }

            // ---- techo nuevo y paredes que suben hasta el
            float top = Ceil + 1f, h = top - OldTop, yc = (OldTop + top) * 0.5f;
            Box("ArenaTall_Ceiling", root, new Vector3(0f, Ceil + 0.5f, (Z0 + Z1) * 0.5f - 0.2f), new Vector3(X1 - X0, 1f, Z1 - Z0 + 0.2f), ceilMat, 2.4f);
            Box("ArenaTall_Wall_W", root, new Vector3(-15f, yc, (Z0 + Z1) * 0.5f), new Vector3(1f, h, Z1 - Z0), wallMat, 3f);
            Box("ArenaTall_Wall_E", root, new Vector3(15f, yc, (Z0 + Z1) * 0.5f), new Vector3(1f, h, Z1 - Z0), wallMat, 3f);
            Box("ArenaTall_Wall_N", root, new Vector3(0f, yc, 20f), new Vector3(X1 - X0, h, 1f), wallMat, 3f);
            Box("ArenaTall_Wall_S", root, new Vector3(0f, yc, 9f), new Vector3(X1 - X0, h, 0.2f), wallMat, 3f);

            // ---- pilastras, viga y lamparas de la sala
            var region = new Bounds(new Vector3(0f, 1.5f, (Z0 + Z1) * 0.5f), new Vector3(30f, 5f, Z1 - Z0 - 0.4f));
            foreach (var t in level.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Pilaster")))
            {
                var r = t.GetComponent<Renderer>(); if (r == null || !region.Contains(new Vector3(r.bounds.center.x, 1.5f, r.bounds.center.z))) continue;
                var b = r.bounds;
                Resize(t, new Vector3(b.center.x, Ceil * 0.5f, b.center.z), new Vector3(b.size.x, Ceil, b.size.z), 3f);
            }
            foreach (var t in level.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Beam"))
            {
                var r = t.GetComponent<Renderer>(); if (r == null || !region.Contains(new Vector3(r.bounds.center.x, 1.5f, r.bounds.center.z))) continue;
                if (r.bounds.center.y < OldTop + 0.2f) t.position += Vector3.up * (Ceil - OldTop);
            }
            int lamps = 0;
            foreach (var l in Object.FindObjectsByType<CeilingLamp>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = l.transform.position;
                if (p.z <= Z0 || p.z >= Z1 - 0.3f || Mathf.Abs(p.x) > 15f || p.y > OldTop + 1f) continue;
                l.transform.position += Vector3.up * (Ceil - OldTop);
                foreach (var light in l.GetComponentsInChildren<Light>(true)) { light.range += (Ceil - OldTop) + 2f; light.intensity *= 1.7f; }
                l.ratedIntensity *= 1.7f;
                EditorUtility.SetDirty(l);
                lamps++;
            }
            log.Add("techo a " + Ceil + " m, " + lamps + " lamparas subidas");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return string.Join(" | ", log);
        }
    }
}
#endif
