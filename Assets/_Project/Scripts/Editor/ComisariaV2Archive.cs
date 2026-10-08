#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria v2, fase 4: el archivo de la segunda planta y el primer jefe (menu Horror/Comisaria v2/4 Archivo y jefe 1).
    /// Repetible: rehace "Props/Archivo_Props", "Archivo_Estanterias" y "Archivo_Jefe".
    ///  - Estanterias de obra contra las paredes (fijas, 3,4 m).
    ///  - Al oeste, tres pasillos de estanterias exentas espalda con espalda, con dos cruces; junto a la entrada, dos bloques mas.
    ///    Son muebles (grupo "_Props"): sirven de cobertura y el jefe las revienta al embestir (PropBreaker).
    ///  - En el centro, la arena: pilas de cajas, papeles por el suelo, carritos, fichero, escalerilla, la mesa del archivero y
    ///    una estanteria volcada.
    ///  - El jefe (Boss.prefab: salto, embestida, suelta la llave) en el centro, dormido, mirando a la entrada. Al pasar la puerta
    ///    del vestibulo del ascensor despierta y la puerta se atranca hasta que muere (BossRoomTrigger).
    /// </summary>
    public static class ComisariaV2Archive
    {
        const string A = "Assets/_Project/Art/Props/Archive/";
        const string O = "Assets/_Project/Art/Props/Office/";
        const string P = "Assets/_Project/Art/Props/";
        static readonly float Y = ComisariaV2Builder.ArchiveY;

        static Transform root;
        static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static readonly string[] Shelves = { "ArchiveShelfA", "ArchiveShelfB", "ArchiveShelfC" };
        static string AnyShelf() => A + Shelves[rng.Next(Shelves.Length)] + ".fbx";

        /// <summary>Modelo colocado (contenedor con el giro; el FBX conserva el suyo), con colision y tono al azar.</summary>
        static GameObject Place(Transform parent, string path, Vector3 pos, float yaw, bool collider = true, float jitter = 1f, bool tint = true)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[Horror] falta " + path); return null; }
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(parent);
            holder.transform.SetPositionAndRotation(pos + new Vector3(R(-0.04f, 0.04f), 0f, R(-0.04f, 0.04f)) * jitter, Quaternion.Euler(0, yaw + R(-2.5f, 2.5f) * jitter, 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
            m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            if (tint)
            {
                float v = R(0.8f, 1.05f);
                holder.AddComponent<PropVariant>().tint = new Color(v * R(0.96f, 1.03f), v, v * R(0.96f, 1.03f));
            }
            foreach (var t in holder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return holder;
        }

        /// <summary>Hilera de estanterias espalda con espalda a lo largo de Z (x = centro de la hilera), de z0 a z1.</summary>
        static void RowZ(float x, float z0, float z1)
        {
            int n = Mathf.FloorToInt((z1 - z0) / 1.82f);
            for (int i = 0; i < n; i++)
            {
                float z = z0 + 0.91f + i * 1.82f;
                Place(root, AnyShelf(), new Vector3(x - 0.26f, Y, z), -90f);   // frente al oeste
                Place(root, AnyShelf(), new Vector3(x + 0.26f, Y, z), 90f);    // frente al este
            }
        }

        /// <summary>Hilera espalda con espalda a lo largo de X (z = centro), de x0 a x1.</summary>
        static void RowX(float z, float x0, float x1)
        {
            int n = Mathf.FloorToInt((x1 - x0) / 1.82f);
            for (int i = 0; i < n; i++)
            {
                float x = x0 + 0.91f + i * 1.82f;
                Place(root, AnyShelf(), new Vector3(x, Y, z - 0.26f), 180f);   // frente al sur
                Place(root, AnyShelf(), new Vector3(x, Y, z + 0.26f), 0f);     // frente al norte
            }
        }

        static void WallShelves(Transform fixedRoot)
        {
            string w = A + "ArchiveWallShelf.fbx";
            for (int k = 0; k < 7; k++) Place(fixedRoot, w, new Vector3(-19.6f, Y, 2.6f + 4f * k), 90f, true, 0.3f);      // oeste
            for (int k = 0; k < 9; k++) Place(fixedRoot, w, new Vector3(-17.5f + 4f * k, Y, 31.6f), 180f, true, 0.3f);    // norte
            for (int k = 0; k < 9; k++) Place(fixedRoot, w, new Vector3(-17.5f + 4f * k, Y, 0.4f), 0f, true, 0.3f);       // sur
            foreach (float z in new[] { 4.5f, 22.0f, 26.0f }) Place(fixedRoot, w, new Vector3(19.6f, Y, z), -90f, true, 0.3f);   // este
        }

        static void Arena()
        {
            // estanteria volcada sobre su costado, con cajas y papeles alrededor
            var fallen = Place(root, A + "ArchiveShelfC.fbx", new Vector3(-4.2f, Y + 0.27f, 23.5f), 0f, true, 0f);
            fallen.transform.rotation = Quaternion.Euler(0, 32f, 0) * Quaternion.Euler(0, 0, 88f);
            Pickup.FitBoxCollider(fallen);
            Place(root, A + "PaperScatter.fbx", new Vector3(-3.2f, Y, 22.2f), R(0, 360), false);
            Place(root, A + "ArchiveBoxStackB.fbx", new Vector3(-2.4f, Y, 24.9f), R(0, 360));
            // mesa del archivero
            Place(root, P + "Desk.fbx", new Vector3(3.0f, Y, 26.4f), 180f);
            Place(root, O + "SwivelChair.fbx", new Vector3(3.2f, Y, 27.4f), 160f);
            Place(root, O + "DeskLamp.fbx", new Vector3(2.5f, Y + 0.76f, 26.2f), 200f, false, 0.5f, false);
            Place(root, O + "PaperStack.fbx", new Vector3(3.4f, Y + 0.76f, 26.3f), 170f, false, 0.5f, false);
            Place(root, A + "CardCatalog.fbx", new Vector3(-5.0f, Y, 1.25f), 0f);
            Place(root, A + "CardCatalog.fbx", new Vector3(-3.9f, Y, 1.25f), 0f);
            Place(root, A + "CardCatalog.fbx", new Vector3(5.6f, Y, 30.75f), 180f);
            Place(root, A + "RollingLadder.fbx", new Vector3(-14.7f, Y, 15.2f), 90f);
            Place(root, A + "RollingLadder.fbx", new Vector3(-11.2f, Y, 6.4f), -90f);
            Place(root, A + "ArchiveCart.fbx", new Vector3(-6.5f, Y, 12.0f), 30f);
            Place(root, A + "ArchiveCart.fbx", new Vector3(4.6f, Y, 18.6f), -60f);
            Place(root, A + "ArchiveCart.fbx", new Vector3(-10.8f, Y, 21.0f), 85f);
            foreach (var (x, z, s) in new[] { (2.0f, 4.0f, "A"), (-6.8f, 20.4f, "A"), (5.6f, 12.4f, "B"), (-1.0f, 29.6f, "B"), (12.8f, 19.6f, "A") })
                Place(root, A + "ArchiveBoxStack" + s + ".fbx", new Vector3(x, Y, z), R(0, 360));
            foreach (var (x, z) in new[] { (0.5f, 16.5f), (-3.0f, 9.0f), (3.5f, 21.5f), (-10.8f, 15.0f), (9.0f, 15.5f), (-14.6f, 26.0f), (8.4f, 2.3f) })
                Place(root, A + "PaperScatter.fbx", new Vector3(x, Y, z), R(0, 360), false);
            Place(root, P + "FilingCabinet.fbx", new Vector3(13.3f, Y, 10.6f), 180f);
            Place(root, P + "FilingCabinet.fbx", new Vector3(12.7f, Y, 10.6f), 180f);
        }

        [MenuItem("Horror/Comisaria v2/4 Archivo y jefe 1")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaV2Builder.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaV2Builder.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase 1";
            var props = rootGo.transform.Find("Props");
            foreach (var n in new[] { "Props/Archivo_Props", "Archivo_Estanterias", "Archivo_Jefe" })
            {
                var old = rootGo.transform.Find(n); if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            root = new GameObject("Archivo_Props").transform; root.SetParent(props);
            var fixedRoot = new GameObject("Archivo_Estanterias").transform; fixedRoot.SetParent(rootGo.transform);
            rng = new System.Random(47);
            ItemTextureKit.Apply();

            WallShelves(fixedRoot);
            // pasillos del oeste: tres hileras a lo largo de Z con dos cruces (z 10,4-12,6 y 20-22,4)
            foreach (float x in new[] { -16.35f, -12.75f, -9.15f })
            {
                RowZ(x, 2.6f, 10.0f); RowZ(x, 12.8f, 20.1f); RowZ(x, 22.4f, 29.8f);
            }
            // junto a la entrada: dos bloques al sur y dos al norte (dejan libre el paso desde la puerta, z 10-18)
            foreach (float z in new[] { 3.8f, 7.6f, 23.6f, 27.4f }) RowX(z, 5.6f, 12.9f);
            Arena();
            int shelves = root.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("ArchiveShelf"));

            // ---- jefe dormido en el centro, mirando a la entrada; despierta al pasar la puerta y la atranca
            var bossRoot = new GameObject("Archivo_Jefe").transform; bossRoot.SetParent(rootGo.transform);
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Boss.prefab"), bossRoot);
            boss.name = "Boss";
            boss.transform.SetPositionAndRotation(new Vector3(-1.0f, Y + 0.05f, 16.0f), Quaternion.Euler(0, 90f, 0));
            var trig = new GameObject("BossRoomTrigger"); trig.transform.SetParent(bossRoot);
            trig.transform.position = new Vector3(12.2f, Y + 1.5f, 14.0f);
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1.6f, 3f, 7.2f);
            var brt = trig.AddComponent<BossRoomTrigger>();
            brt.boss = boss.GetComponent<ZombieAI>();
            var archDoor = GameObject.Find("Puerta_Archivo");
            brt.sealDoors = archDoor != null ? archDoor.GetComponentsInChildren<Door>() : new Door[0];

            // ---- NavMesh
            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            surface.overrideVoxelSize = true; surface.voxelSize = 0.1f;
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase 4: " + root.childCount + " muebles (" + shelves + " estanterias rompibles), " + fixedRoot.childCount + " de obra, jefe y disparador (" + brt.sealDoors.Length + " puerta)";
        }
    }
}
#endif
