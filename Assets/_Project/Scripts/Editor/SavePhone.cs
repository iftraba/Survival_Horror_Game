#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Punto de guardado fisico: un telefono antiguo de disco sobre una mesita. Crea el prefab
    /// Prefabs/Interactables/SavePhone.prefab (modelo Art/Props/Phone.fbx, hecho con Tools/blender/build_phone.py)
    /// y sustituye en la escena los terminales viejos, en su misma posicion y orientacion.
    /// </summary>
    public static class SavePhone
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Interactables/SavePhone.prefab";

        [MenuItem("Horror/Crear telefono de guardado")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Env_Wood.mat");
            var phoneFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Props/Phone.fbx");
            if (phoneFbx == null) return "falta Art/Props/Phone.fbx (ejecuta Tools/blender/build_phone.py)";

            var root = new GameObject("SavePhone");
            // la mesita: tablero + cuatro patas; el telefono mira a +Z como el resto de interactuables
            GameObject Cube(string n, Vector3 pos, Vector3 size, Transform parent, bool collider)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = n; c.transform.SetParent(parent, false);
                c.transform.localPosition = pos; c.transform.localScale = size;
                if (wood != null) c.GetComponent<MeshRenderer>().sharedMaterial = wood;
                if (!collider) Object.DestroyImmediate(c.GetComponent<Collider>());
                return c;
            }
            var table = new GameObject("Mesita").transform; table.SetParent(root.transform, false);
            Cube("Tablero", new Vector3(0, 0.74f, 0), new Vector3(0.58f, 0.04f, 0.42f), table, false);
            foreach (var sx in new[] { -0.25f, 0.25f })
                foreach (var sz in new[] { -0.17f, 0.17f })
                    Cube("Pata", new Vector3(sx, 0.36f, sz), new Vector3(0.05f, 0.72f, 0.05f), table, false);
            Cube("Estante", new Vector3(0, 0.22f, 0), new Vector3(0.5f, 0.03f, 0.36f), table, false);

            var phone = (GameObject)PrefabUtility.InstantiatePrefab(phoneFbx, root.transform);
            phone.name = "Telefono";
            phone.transform.localPosition = new Vector3(0f, 0.76f, 0f);
            phone.transform.localRotation = Quaternion.identity;   // el disco de marcar mira a +Z, como el resto de interactuables
            phone.transform.localScale = Vector3.one * 1.15f;
            // apoyarlo exactamente sobre el tablero (la base del modelo puede no coincidir con su origen)
            var pr = phone.GetComponentInChildren<Renderer>();
            phone.transform.localPosition += Vector3.up * (0.76f - pr.bounds.min.y);

            // el interactuable: caja que cubre mesita y telefono
            var inter = new GameObject("SaveTerminal");
            inter.transform.SetParent(root.transform, false);
            inter.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var bc = inter.AddComponent<BoxCollider>(); bc.size = new Vector3(0.62f, 1.1f, 0.46f);
            inter.AddComponent<SaveTerminal>();

            // luz calida tenue encima: guia al jugador hacia el punto de guardado
            var glowGo = new GameObject("PhoneGlow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 1.25f, 0.15f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point; glow.color = new Color(1f, 0.7f, 0.4f); glow.intensity = 1.6f; glow.range = 2.6f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // sustituir los terminales de la escena
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            int n = 0;
            foreach (var t in Object.FindObjectsByType<SaveTerminal>(FindObjectsSortMode.None).ToArray())
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != null &&
                    AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) == PrefabPath) continue;   // ya es el telefono
                var top = t.transform;
                while (top.parent != null && !top.parent.name.Contains("LEVEL") && top.parent.name != "BossWing_Solid") top = top.parent;
                var pos = new Vector3(top.position.x, 0f, top.position.z);
                var yaw = t.transform.eulerAngles.y;
                var parent = top.parent;
                string name = top.name.Replace("SaveTerminal", "SavePhone");
                if (top.name == "SaveTerminal_Group") { pos = new Vector3(t.transform.position.x, 0f, t.transform.position.z); }
                Object.DestroyImmediate(top.gameObject);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                inst.name = name;
                inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return "telefono creado; sustituidos " + n + " terminales";
        }
    }
}
#endif
