#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Casquillos y cartuchos vacios que salen de las armas (menu Horror/Crear casquillos). Crea los materiales (laton, plastico
    /// rojo) y los prefabs Casing_Pistol y Casing_Shotgun (primitivas con Rigidbody, algo mas grandes que los reales para que se
    /// vean desde la camara) y los asigna a W_Pistol (sale al disparar) y W_Shotgun (sale al bombear). Repetible.
    /// </summary>
    public static class CasingKit
    {
        const string Dir = "Assets/_Project/Prefabs/Weapons/";
        const string MatDir = "Assets/_Project/Materials/";

        static Material Mat(string name, Color c, float metallic, float smooth)
        {
            string p = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static PhysicsMaterial Phys()
        {
            string p = MatDir + "Casing.physicMaterial";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(p);
            if (m == null) { m = new PhysicsMaterial("Casing"); AssetDatabase.CreateAsset(m, p); }
            m.bounciness = 0.45f; m.dynamicFriction = 0.5f; m.staticFriction = 0.6f;
            m.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Cyl(string name, Transform parent, Vector3 pos, float dia, float len, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(dia, len * 0.5f, dia);
            g.GetComponent<Renderer>().sharedMaterial = mat;
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        static GameObject Build(string name, float dia, float len, Material body, Material baseMat, float baseLen)
        {
            var root = new GameObject(name);
            if (baseMat != null)
            {
                Cyl("Hull", root.transform, new Vector3(0f, baseLen * 0.5f, 0f), dia * 0.97f, len - baseLen, body);
                Cyl("Base", root.transform, new Vector3(0f, -(len - baseLen) * 0.5f, 0f), dia, baseLen, baseMat);
                // el cartucho: cuerpo de plastico arriba, culote de laton abajo; el origen queda en el centro
                root.transform.Find("Hull").localPosition = new Vector3(0f, baseLen * 0.5f, 0f);
            }
            else Cyl("Body", root.transform, Vector3.zero, dia, len, body);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.01f; rb.linearDamping = 0.05f; rb.angularDamping = 0.05f; rb.maxAngularVelocity = 50f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var col = root.AddComponent<CapsuleCollider>();
            col.direction = 1; col.radius = dia * 0.5f; col.height = len; col.sharedMaterial = Phys();
            root.AddComponent<EjectedCasing>();
            string path = Dir + name + ".prefab";
            System.IO.Directory.CreateDirectory(Dir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        [MenuItem("Horror/Crear casquillos")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var brass = Mat("Casing_Brass", new Color(0.85f, 0.62f, 0.22f), 0.9f, 0.65f);
            var red = Mat("Casing_ShellRed", new Color(0.7f, 0.08f, 0.06f), 0f, 0.5f);
            var pistolCasing = Build("Casing_Pistol", 0.017f, 0.032f, brass, null, 0f);
            var shell = Build("Casing_Shotgun", 0.026f, 0.07f, red, brass, 0.014f);
            AssetDatabase.SaveAssets();

            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Data/W_Pistol.asset");
            var shotgun = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Data/W_Shotgun.asset");
            pistol.casingPrefab = pistolCasing; pistol.ejectAtCycle = false; pistol.ejectOffset = new Vector3(0.04f, 0.05f, 0.12f); pistol.ejectSpeed = 2.0f;
            shotgun.casingPrefab = shell; shotgun.ejectAtCycle = true; shotgun.ejectOffset = new Vector3(0.04f, 0.04f, 0.10f); shotgun.ejectSpeed = 2.3f;
            EditorUtility.SetDirty(pistol); EditorUtility.SetDirty(shotgun);
            AssetDatabase.SaveAssets();
            return "casquillos creados y asignados a W_Pistol (al disparar) y W_Shotgun (al bombear)";
        }
    }
}
#endif
