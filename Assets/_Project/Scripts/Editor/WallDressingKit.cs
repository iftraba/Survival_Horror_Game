#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Detalles en las paredes (2026-10-08, menu Horror/Vestir paredes): para que las salas no sean cajas lisas.
    /// Recorre las paredes del nivel (cajas con Env_Wall de 2,4 m o mas) y en cada cara que da a una sala pone:
    ///  - una moldura de madera en el encuentro con el techo;
    ///  - atrezzo de pared modelado en Blender (build_wall_props.py): radiadores, extintores, cuadros electricos con sus tubos,
    ///    tablones de anuncios, rejillas, relojes, cada ~3,5 m y solo donde hay sitio (no tapa puertas, muebles ni interruptores);
    ///  - en algunas paredes largas, una tuberia a lo largo junto al techo.
    /// Todo va en "--- LEVEL ---/WallDressing", sin colisiones (no cambia por donde se anda). Repetible: rehace el grupo con la misma
    /// semilla, asi sale siempre igual.
    /// </summary>
    public static class WallDressingKit
    {
        const string PropDir = "Assets/_Project/Art/Props/Wall/";

        struct Item { public string name; public float weight, height, width, depth, tall; public bool fromCeiling; }

        // altura = base del objeto sobre el suelo (o, si fromCeiling, distancia de su base al techo)
        static readonly Item[] Items =
        {
            new Item { name = "WallRadiator", weight = 0.22f, height = 0.0f, width = 1.0f, depth = 0.18f, tall = 0.7f },
            new Item { name = "WallNoticeBoard", weight = 0.2f, height = 1.15f, width = 1.1f, depth = 0.06f, tall = 0.72f },
            new Item { name = "WallExtinguisher", weight = 0.14f, height = 0.75f, width = 0.3f, depth = 0.2f, tall = 1.1f },
            new Item { name = "WallElectricPanel", weight = 0.12f, height = 1.1f, width = 0.55f, depth = 0.18f, tall = 1.75f },   // sus tubos tocan el techo: se mira algo menos
            new Item { name = "WallVent", weight = 0.18f, height = 0.55f, width = 0.5f, depth = 0.05f, tall = 0.27f, fromCeiling = true },
            new Item { name = "WallClock", weight = 0.08f, height = 2.0f, width = 0.36f, depth = 0.06f, tall = 0.34f },
        };

        struct Face { public Renderer wall; public Vector3 a, b, normal; public float floor, ceiling; }

        [MenuItem("Horror/Vestir paredes (molduras y atrezzo)")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var level = GameObject.Find("--- LEVEL ---").transform;
            var old = level.Find("WallDressing");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("WallDressing").transform;
            root.SetParent(level);
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Env_Wood.mat");
            var models = Items.ToDictionary(i => i.name, i => AssetDatabase.LoadAssetAtPath<GameObject>(PropDir + i.name + ".fbx"));
            var pipe = AssetDatabase.LoadAssetAtPath<GameObject>(PropDir + "WallPipeRun.fbx");
            Physics.SyncTransforms();

            var faces = FindFaces();
            int trims = 0, props = 0, pipes = 0;
            var counts = new Dictionary<string, int>();
            foreach (var f in faces)
            {
                var rng = new System.Random(Mathf.RoundToInt(f.a.x * 73f + f.a.z * 131f + f.normal.x * 17f + f.normal.z * 29f + f.floor * 7f));
                Vector3 along = (f.b - f.a); float len = along.magnitude; along /= len;
                var rot = Quaternion.LookRotation(f.normal);
                // ---- moldura en el encuentro con el techo (tablas de 8 cm, algo separadas de la pared)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = "Moldura";
                    Object.DestroyImmediate(g.GetComponent<Collider>());
                    g.transform.SetParent(root);
                    Vector3 mid = (f.a + f.b) * 0.5f + f.normal * 0.035f; mid.y = f.ceiling - 0.05f;
                    g.transform.SetPositionAndRotation(mid, rot);
                    g.transform.localScale = new Vector3(len, 0.1f, 0.07f);
                    g.GetComponent<MeshFilter>().sharedMesh = Tiled(mid, alongXWorld(along) ? new Vector3(len, 0.1f, 0.07f) : new Vector3(0.07f, 0.1f, len), 1f, rot);
                    g.GetComponent<Renderer>().sharedMaterial = wood;
                    g.isStatic = true;
                    trims++;
                }
                // ---- tuberia junto al techo en algunas paredes largas
                bool hasPipe = false;
                if (pipe != null && len >= 4f && rng.NextDouble() < 0.3)
                {
                    hasPipe = true;
                    float y = f.ceiling - 0.3f;
                    for (float s = 0.2f; s + 1.0f <= len - 0.2f; s += 2f)
                    {
                        float segLen = Mathf.Min(2f, len - 0.2f - s);
                        Vector3 c = f.a + along * (s + segLen * 0.5f); c.y = y;
                        if (!Free(c + f.normal * 0.09f, new Vector3(segLen * 0.5f, 0.07f, 0.08f), rot, f.wall)) continue;
                        var p = (GameObject)PrefabUtility.InstantiatePrefab(pipe, root);
                        p.transform.SetPositionAndRotation(c, rot * pipe.transform.localRotation);   // el FBX trae el giro de Blender (Z arriba)
                        p.transform.localScale = Vector3.Scale(pipe.transform.localScale, new Vector3(segLen / 2f, 1f, 1f));
                        Strip(p);
                        pipes++;
                    }
                }
                // ---- atrezzo cada ~3,5 m donde quepa
                for (float s = 0.9f; s < len - 0.9f; s += 3.2f + (float)rng.NextDouble() * 1.2f)
                {
                    if (rng.NextDouble() > 0.62) continue;
                    var it = Pick(rng, f.ceiling - f.floor);
                    if (it.name == null || models[it.name] == null) continue;
                    if (hasPipe && it.name == "WallElectricPanel") continue;                      // sus tubos cruzarian la tuberia
                    if (s - it.width * 0.5f < 0.3f || s + it.width * 0.5f > len - 0.3f) continue;
                    float baseY = it.fromCeiling ? f.ceiling - it.height - (hasPipe ? 0.25f : 0f) : f.floor + it.height;   // la rejilla, bajo la tuberia
                    Vector3 at = f.a + along * s; at.y = baseY;
                    Vector3 center = at + f.normal * (it.depth * 0.5f + 0.02f) + Vector3.up * (it.tall * 0.5f);
                    Vector3 half = new Vector3(it.width * 0.5f + 0.1f, it.tall * 0.5f, it.depth * 0.5f + 0.05f);
                    if (it.height < 0.2f && !it.fromCeiling) { center.y += 0.1f; half.y -= 0.1f; }        // los rodapies no cuentan
                    if (!Free(center, half, rot, f.wall)) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(models[it.name], root);
                    go.transform.SetPositionAndRotation(at + f.normal * 0.005f, rot * models[it.name].transform.localRotation);
                    Strip(go);
                    props++;
                    counts[it.name] = counts.TryGetValue(it.name, out var k) ? k + 1 : 1;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return faces.Count + " caras de pared: " + trims + " molduras, " + pipes + " tramos de tuberia, " + props + " objetos (" +
                   string.Join(", ", counts.Select(kv => kv.Key.Replace("Wall", "") + " " + kv.Value)) + ")";
        }

        static bool alongXWorld(Vector3 along) => Mathf.Abs(along.x) > Mathf.Abs(along.z);

        /// <summary>Cubo con la textura a escala real (el mismo de TestSceneBuilder); el tamano va en ejes del mundo y se pasa a locales.</summary>
        static Mesh Tiled(Vector3 pos, Vector3 worldSize, float tile, Quaternion rot)
        {
            var m = typeof(TestSceneBuilder).GetMethod("TiledUnitCube", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            // las UV se calculan con la caja alineada a los ejes del mundo; la moldura siempre es paralela a X o a Z
            var local = Quaternion.Inverse(rot) * worldSize;
            return (Mesh)m.Invoke(null, new object[] { pos, new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)), tile });
        }

        static Item Pick(System.Random rng, float roomHeight)
        {
            float total = Items.Sum(i => i.weight), r = (float)rng.NextDouble() * total;
            foreach (var it in Items)
            {
                r -= it.weight;
                if (r > 0f) continue;
                if (it.name == "WallElectricPanel" && Mathf.Abs(roomHeight - 3f) > 0.15f) return default;   // sus tubos llegan a 3 m
                return it;
            }
            return default;
        }

        /// <summary>Hueco libre delante de la pared (sin muebles, puertas, interruptores ni otras paredes).</summary>
        static bool Free(Vector3 center, Vector3 half, Quaternion rot, Renderer wall)
        {
            foreach (var c in Physics.OverlapBox(center, half, rot, ~0, QueryTriggerInteraction.Collide))
            {
                if (c.transform == wall.transform || c.GetComponent<BossRoomTrigger>() != null) continue;
                if (c.GetComponentInParent<ZombieAI>() != null || c is CharacterController) continue;
                return false;
            }
            return true;
        }

        static void Strip(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        }

        /// <summary>Caras de pared que dan a una sala: delante hay suelo a la altura de su base y techo encima.</summary>
        static List<Face> FindFaces()
        {
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Env_Wall.mat");
            var list = new List<Face>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.sharedMaterial != wallMat || !r.gameObject.activeInHierarchy) continue;
                if (r.name.StartsWith("Column") || r.name.StartsWith("Pillar") || r.name.StartsWith("ArenaTall")) continue;
                var b = r.bounds;
                if (b.size.y < 2.4f) continue;
                bool alongX = b.size.x >= b.size.z;
                float len = alongX ? b.size.x : b.size.z, thick = alongX ? b.size.z : b.size.x;
                if (len < 1.2f || thick > 1.2f) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 n = alongX ? new Vector3(0, 0, side) : new Vector3(side, 0, 0);
                    Vector3 c = b.center + n * (thick * 0.5f);
                    Vector3 axis = alongX ? Vector3.right : Vector3.forward;
                    Vector3 a = c - axis * len * 0.5f, e = c + axis * len * 0.5f;
                    a.y = e.y = b.min.y;
                    // sala delante: suelo a la altura de la base y techo encima, sin estar dentro de otra cosa
                    Vector3 probe = c + n * 0.7f; probe.y = b.min.y + 1.2f;
                    if (Physics.CheckSphere(probe, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!Physics.Raycast(probe, Vector3.down, out var down, 2f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(down.point.y - b.min.y) > 0.35f) continue;
                    if (!Physics.Raycast(probe, Vector3.up, out var up, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    float ceiling = Mathf.Min(up.point.y, b.max.y);
                    if (ceiling - b.min.y < 2.4f) continue;
                    list.Add(new Face { wall = r, a = a, b = e, normal = n, floor = b.min.y, ceiling = ceiling });
                }
            }
            return list;
        }
    }
}
#endif
