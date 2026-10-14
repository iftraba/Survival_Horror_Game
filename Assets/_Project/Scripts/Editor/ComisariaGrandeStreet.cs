#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, lavado de cara del exterior, fase 2 (menu Horror/Comisaria grande/10 Calle y mobiliario urbano): mobiliario urbano de
    /// <c>Art/Props/Street</c> (Tools/blender/build_street.py) repartido por las dos aceras y la calzada, mas la capa de suelo con grietas, parches,
    /// manchas de aceite, alcantarillas y pasos de cebra (<see cref="ComisariaGrandeStreetDecals"/>). Todo cuelga de "Calle_Detalle" bajo el nivel,
    /// es estatico y no entra en el NavMesh del patio (esta fuera de la verja). Repetible: rehace "Calle_Detalle" y no toca nada mas.
    /// Coordenadas: calzada z -18..-10, acera sur (junto a la verja) z -10..-8, acera de enfrente z -20..-18, farolas cada 12 m en z -9,3.
    /// </summary>
    public static class ComisariaGrandeStreet
    {
        const string Street = "Assets/_Project/Art/Props/Street/";
        static Transform root;

        // nombre del modelo, x, z, yaw, inclinacion X, inclinacion Z, escala, colision (0 ninguna, 1 caja, 2 capsula del tronco/poste)
        static readonly (string m, float x, float z, float yaw, float tx, float tz, float s, int col)[] Items =
        {
            // --- acera sur (junto a la verja): hidrantes, parquimetros, buzones, papeleras, senales y semaforos
            ("Hydrant", -31f, -9.7f, 15f, 0, 0, 1f, 0), ("Hydrant", 17f, -9.7f, -20f, 0, 0, 1f, 0),
            ("ParkingMeter", -55f, -9.75f, 0f, 0, 0, 1f, 0), ("ParkingMeter", -50f, -9.75f, 4f, 0, 0, 1f, 0), ("ParkingMeter", -45f, -9.75f, -3f, 0, 0, 1f, 0),
            ("ParkingMeter", 41f, -9.75f, 2f, 0, 0, 1f, 0), ("ParkingMeter", 46f, -9.75f, -2f, 0, 0, 1f, 0),
            ("NewsBox", -14f, -8.7f, 180f, 0, 0, 1f, 0), ("NewsBox", 6.3f, -8.6f, 175f, 0, 0, 1f, 0), ("Mailbox", 23f, -8.7f, 185f, 0, 0, 1f, 0),
            ("TrashCan", -27f, -9.4f, 20f, 0, 0, 1f, 0), ("TrashCan", -3.5f, -9.4f, 70f, 0, 0, 1f, 0), ("TrashCan", 40f, -9.2f, 0f, 0, 0, 1f, 0),
            ("TrashCan", 9.5f, -9.0f, 30f, 0, 90f, 1f, 0),                                                                    // volcada
            ("SignStop", -6f, -9.85f, 180f, 0, 0, 1f, 0), ("SignOneWay", 30f, -9.85f, 180f, 0, 0, 1f, 0), ("SignNoParking", -40f, -9.85f, 175f, 0, 0, 1f, 0),
            ("TrafficLight", -48f, -9.85f, 90f, 0, 0, 1f, 0), ("TrafficLight", 46f, -19.85f, -90f, 0, 6f, 1f, 0),            // el segundo, algo ladeado
            ("GarbageBags", -35f, -10.55f, 20f, 0, 0, 1f, 0), ("GarbageBags", 32f, -9.2f, 140f, 0, 0, 1f, 0), ("GarbageBags", -20.5f, -8.55f, 60f, 0, 0, 0.8f, 0),
            // --- calzada: chicane de barreras, conos y valla de obras
            ("JerseyBarrier", 14f, -12.2f, 20f, 0, 0, 1f, 1), ("JerseyBarrier", 17.3f, -13.6f, 38f, 0, 0, 1f, 1), ("JerseyBarrier", 20.5f, -15.1f, 10f, 0, 0, 1f, 1),
            ("TrafficCone", 10.4f, -11.2f, 0f, 0, 0, 1f, 0), ("TrafficCone", 11.8f, -12.0f, 0f, 0, 0, 1f, 0), ("TrafficCone", 12.4f, -10.7f, 0f, 0, 90f, 1f, 0),
            ("TrafficCone", 23.2f, -16.0f, 0f, 0, 0, 1f, 0), ("TrafficCone", -13.5f, -12.6f, 0f, 0, 80f, 1f, 0),
            ("Barricade", -17f, -11.6f, 6f, 0, 0, 1f, 1), ("Barricade", 2.5f, -16.8f, -12f, 0, 0, 1f, 1),
            // --- coches civiles abandonados en la calzada (carroceria de la patrulla sin rotulos): uno aparcado pegado a la acera de enfrente, uno atravesado, otro quemado
            ("CivilCarA", -38f, -16.7f, 92f, 0, 0, 1f, 1), ("CivilCarB", -26f, -11.3f, 262f, 0, 0, 1f, 1), ("CivilCarC", 50f, -16.5f, 80f, 0, 0, 1f, 1),
            ("CivilCarB", 66f, -12.6f, 105f, 0, 0, 1f, 1),
            // --- acera de enfrente: parada de autobus, contenedores, arboles muertos, bolsas de basura y mas mobiliario
            ("BusShelter", -62f, -19.1f, 0f, 0, 0, 1f, 1), ("Dumpster", -37f, -19.3f, 3f, 0, 0, 1f, 1), ("Dumpster", 27f, -19.3f, -4f, 0, 0, 1f, 1),
            ("GarbageBags", -34f, -18.4f, 40f, 0, 0, 1f, 0), ("GarbageBags", 31f, -18.4f, -30f, 0, 0, 1f, 0), ("GarbageBags", -1.5f, -18.4f, 100f, 0, 0, 0.9f, 0),
            ("DeadTree", -80f, -19.2f, 20f, 0, 0, 1.0f, 2), ("DeadTree", -44f, -19.2f, 140f, 0, 0, 1.1f, 2), ("DeadTree", -12f, -19.2f, 260f, 0, 0, 0.95f, 2),
            ("DeadTree", 16f, -19.2f, 70f, 0, 0, 1.15f, 2), ("DeadTree", 52f, -19.2f, 200f, 0, 0, 1.0f, 2), ("DeadTree", 84f, -19.2f, 310f, 0, 0, 1.05f, 2),
            ("Hydrant", -20f, -18.5f, 180f, 0, 0, 1f, 0), ("NewsBox", 36f, -18.4f, 0f, 0, 0, 1f, 0), ("Mailbox", -52f, -18.5f, 0f, 0, 0, 1f, 0),
            ("TrashCan", 60f, -18.5f, 0f, 0, 0, 1f, 0), ("TrashCan", -28f, -18.6f, 0f, 0, 0, 1f, 0), ("ParkingMeter", 6f, -18.55f, 180f, 0, 0, 1f, 0), ("ParkingMeter", 10f, -18.55f, 180f, 0, 0, 1f, 0),
            ("SignNoParking", 0f, -18.25f, 0f, 0, 0, 1f, 0), ("SignStop", 60f, -18.25f, 0f, 0, 0, 1f, 0),
        };

        [MenuItem("Horror/Comisaria grande/10 Calle y mobiliario urbano")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---");
            if (level == null) return "falta el nivel";
            var old = level.transform.Find("Calle_Detalle");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("Calle_Detalle").transform; root.SetParent(level.transform);
            int placed = 0, missing = 0;
            var rng = new System.Random(21);
            foreach (var it in Items)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Street + it.m + ".fbx");
                if (pf == null) { missing++; continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root);
                go.name = it.m;
                go.transform.SetPositionAndRotation(new Vector3(it.x, 0f, it.z), Quaternion.Euler(it.tx, it.yaw, it.tz) * pf.transform.localRotation);
                go.transform.localScale = Vector3.one * it.s;
                if (Mathf.Abs(it.tz) > 45f)                                        // volcado: apoyado sobre su lado
                {
                    var b = RendererBounds(go);
                    go.transform.position += Vector3.up * -b.min.y;
                }
                AddCollider(go, it.col);
                SetStatic(go);
                placed++;
            }
            ComisariaGrandeStreetDecals.Build(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "calle: " + placed + " objetos" + (missing > 0 ? " (faltan " + missing + " modelos)" : "") + " y capa de suelo";
        }

        static Bounds RendererBounds(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.zero); bool first = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            return b;
        }

        static void AddCollider(GameObject go, int mode)
        {
            if (mode == 0) return;
            var b = RendererBounds(go);
            if (mode == 1) { var bc = go.AddComponent<BoxCollider>(); bc.center = go.transform.InverseTransformPoint(b.center); bc.size = new Vector3(b.size.x, b.size.y, b.size.z) / Mathf.Max(0.0001f, go.transform.lossyScale.x); }
            else { var cc = go.AddComponent<CapsuleCollider>(); cc.center = go.transform.InverseTransformPoint(new Vector3(go.transform.position.x, 1.5f, go.transform.position.z)); cc.radius = 0.28f; cc.height = 3f; }
        }

        static void SetStatic(GameObject go) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true; }
    }
}
#endif
