#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Puertas de madera con modelo (2026-10-08, menu Horror/Modelos de puertas). Las piezas de los prefabs Door_Wood_150 y
    /// Door_Wood_140 eran cubos: la hoja (Leaf), el marco (Jamb_A/B, Header) y los apliques (panel, placa, manivela, escudo).
    /// Cada malla de Tools/blender/build_doors.py se normaliza a la caja de la pieza que sustituye (asi el cubo escalado del prefab
    /// la pone a su medida y se conservan las colisiones y la logica de Door) y se guarda como asset. La hoja nueva ya trae el
    /// panel, la placa, la manivela y las bisagras: esas piezas se ocultan. El cristal (Window) se queda. Repetible.
    /// </summary>
    public static class DoorModelKit
    {
        const string Dir = "Assets/_Project/Art/Props/Door/";
        const string TexDir = "Assets/_Project/Art/Textures/Props/";

        // centro y tamano (en metros, ejes de Unity) de la pieza original de la puerta de 1,50 que sustituye cada malla
        static readonly (string fbx, Vector3 c, Vector3 s)[] Parts =
        {
            ("DoorLeaf", new Vector3(0.75f, 1.2f, 0f), new Vector3(1.46f, 2.36f, 0.05f)),
            ("DoorJamb", new Vector3(-0.04f, 1.2f, 0f), new Vector3(0.1f, 2.45f, 0.05f)),
            ("DoorHeader", new Vector3(0.75f, 2.45f, 0f), new Vector3(1.68f, 0.1f, 0.05f)),
        };

        [MenuItem("Horror/Modelos de puertas")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var log = new List<string>();
            var meshes = new Dictionary<string, Mesh>();
            foreach (var (fbx, c, s) in Parts)
            {
                string path = Dir + fbx + ".fbx";
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                if (imp == null) return "falta " + path;
                if (imp.bakeAxisConversion) { imp.bakeAxisConversion = false; imp.SaveAndReimport(); }   // con la conversion horneada el giro sale al reves
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var mfSrc = model.GetComponentInChildren<MeshFilter>();
                var src = mfSrc.sharedMesh;
                var toUnity = mfSrc.transform.localToWorldMatrix;          // el giro y la escala que el FBX trae de Blender (Z arriba)
                meshes[fbx] = Normalize(src, toUnity, c, s, fbx, false);
                if (fbx == "DoorJamb") meshes["DoorJambB"] = Normalize(src, toUnity, c, s, "DoorJambB", true);   // la jamba del otro lado, girada
                log.Add(fbx + " " + src.bounds.size.ToString("F2"));
            }
            var mats = new Dictionary<string, Material>
            {
                ["DoorLeaf"] = AssetDatabase.LoadAssetAtPath<Material>(TexDir + "DoorLeaf_baked.mat"),
                ["DoorJamb"] = AssetDatabase.LoadAssetAtPath<Material>(TexDir + "DoorJamb_baked.mat"),
                ["DoorHeader"] = AssetDatabase.LoadAssetAtPath<Material>(TexDir + "DoorHeader_baked.mat"),
            };
            // madera barnizada oscura, como el resto de la madera del escenario (el horneado sale clara y anaranjada)
            foreach (var mt in mats.Values) if (mt != null) { mt.SetColor("_BaseColor", new Color(0.55f, 0.42f, 0.36f)); EditorUtility.SetDirty(mt); }
            foreach (var pf in new[] { "Door_Wood_150", "Door_Wood_140" })
            {
                string pp = "Assets/_Project/Prefabs/Doors/" + pf + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(pp);
                int n = 0;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>();
                    if (mf == null || mr == null) continue;
                    switch (t.name)
                    {
                        case "Leaf": mf.sharedMesh = meshes["DoorLeaf"]; mr.sharedMaterial = mats["DoorLeaf"]; n++; break;
                        case "Jamb_A": mf.sharedMesh = meshes["DoorJamb"]; mr.sharedMaterial = mats["DoorJamb"]; n++; break;
                        case "Jamb_B": mf.sharedMesh = meshes["DoorJambB"]; mr.sharedMaterial = mats["DoorJamb"]; n++; break;
                        case "Header": mf.sharedMesh = meshes["DoorHeader"]; mr.sharedMaterial = mats["DoorHeader"]; n++; break;
                        case "PanelLow": case "KickPlate": case "Handle": case "Escutcheon": mr.enabled = false; break;   // ya van en la hoja
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, pp);
                PrefabUtility.UnloadPrefabContents(root);
                log.Add(pf + ": " + n + " piezas");
            }
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }

        /// <summary>
        /// Lleva la malla (en metros) a la caja unidad de la pieza: v' = (v - c) / s. Si la importacion invierte X (ejes de Blender),
        /// se gira 180 grados en Y para conservar el sentido de las caras. 'mirror' la gira ademas para el otro lado del marco.
        /// </summary>
        static Mesh Normalize(Mesh src, Matrix4x4 toUnity, Vector3 c, Vector3 s, string name, bool mirror)
        {
            var v = src.vertices; var nrm = src.normals;
            for (int i = 0; i < v.Length; i++) { v[i] = toUnity.MultiplyPoint3x4(v[i]); if (i < nrm.Length) nrm[i] = toUnity.MultiplyVector(nrm[i]).normalized; }
            // la caja de la malla debe caer sobre la de la pieza: si X sale al reves (media lejos de c.x), se gira
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var p0 in v) { minX = Mathf.Min(minX, p0.x); maxX = Mathf.Max(maxX, p0.x); }
            float cx = (minX + maxX) * 0.5f;
            bool flip = Mathf.Abs(-cx - c.x) < Mathf.Abs(cx - c.x);
            if (mirror) flip = !flip;
            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i];
                var n = nrm.Length > i ? nrm[i] : Vector3.up;
                if (flip) { p.x = -p.x; p.z = -p.z; n.x = -n.x; n.z = -n.z; }
                if (mirror) p.x += 2f * c.x;                                  // girada sobre su propio centro, no sobre la bisagra
                v[i] = new Vector3((p.x - c.x) / s.x, (p.y - c.y) / s.y, (p.z - c.z) / s.z);
                nrm[i] = new Vector3(n.x * s.x, n.y * s.y, n.z * s.z).normalized;   // la escala del prefab las devuelve a su sitio
            }
            string path = Dir + name + "_Norm.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = m == null;
            if (isNew) m = new Mesh();
            m.Clear();
            m.name = name;
            m.indexFormat = src.indexFormat;
            m.vertices = v; m.normals = nrm; m.uv = src.uv;
            m.subMeshCount = src.subMeshCount;
            for (int k = 0; k < src.subMeshCount; k++) m.SetTriangles(src.GetTriangles(k), k);
            m.RecalculateBounds();
            m.RecalculateTangents();
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);   // se rellena la misma malla: las referencias siguen valiendo
            return m;
        }
    }
}
#endif
