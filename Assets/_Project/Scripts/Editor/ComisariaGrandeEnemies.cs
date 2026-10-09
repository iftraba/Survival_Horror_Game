#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase E (menu Horror/Comisaria grande/5 Zombis y botin): la dificultad.
    ///  - Unos 74 zombis repartidos por zonas (2-4 en las salas grandes y pasillos), ninguno en las salas seguras ni en el
    ///    vestibulo. Mezcla de modelos (sin los del jefe 2), reptantes, carroneros comiendose un cadaver (solo despiertan si les
    ///    disparan) y una parte en letargo: no reaccionan hasta oir un ruido o recibir un tiro (emboscadas al cruzar una sala).
    ///  - Botin justo: cajas de balas, cartuchos y sprays encima de los muebles (lo que hay en las taquillas lo pone la fase D).
    /// Repetible: rehace "Enemigos" y "Botin".
    /// </summary>
    public static class ComisariaGrandeEnemies
    {
        // Solo los aprobados por el usuario (docs/enemies.md, 2026-10-08): los de Mixamo con el juego de animaciones unificado y los de Pxltiger
        // (Pxl3 lleva la piel del jefe 2). Los ZombieGen* (Ejecutivo, Infectado, Mecanico, Paciente, Policia) estan descartados: no usarlos.
        public static readonly string[] Normal = { "Zombie_Civil", "Zombie_Cop", "Zombie_Girl", "Zombie_Oficial", "Zombie_Pxl1", "Zombie_Pxl2" };

        // sala -> (zombis, probabilidad de letargo, extras: 'R' reptante, 'C' carronero)
        static readonly (string room, int n, float dormant, string extra)[] Spawns =
        {
            ("G_Wait", 2, 0.2f, ""), ("G_C1W", 2, 0f, ""), ("G_Garage", 2, 0.3f, "R"), ("G_Dark", 2, 0.8f, ""), ("G_WC", 0, 0f, ""),
            ("G_Elev", 0, 0f, ""), ("G_OffE", 2, 0.2f, ""), ("G_Armory", 1, 0f, ""), ("G_C1E", 2, 0f, ""), ("G_Sec", 1, 0.3f, ""),
            ("G_Break", 1, 0f, "C"), ("G_C2", 2, 0.1f, ""), ("G_Lock", 2, 0.4f, ""), ("G_Cells", 2, 0.3f, "R"), ("G_Store", 2, 0.5f, ""),
            ("G_Work", 2, 0.2f, ""), ("G_Alley", 1, 0f, "R"),
            ("F_Conf", 2, 0.3f, ""), ("F_Comm", 1, 0.5f, ""), ("F_OffA", 2, 0.2f, ""), ("F_OffB", 2, 0.2f, ""), ("F_C1", 2, 0f, ""),
            ("F_Lib", 2, 0.5f, ""), ("F_Det", 2, 0.2f, ""), ("F_C2", 2, 0f, ""), ("F_WC", 0, 0f, ""), ("F_Inter", 1, 0.3f, ""),
            ("F_Records", 2, 0.4f, ""), ("F_Lounge", 0, 0f, "C"), ("S_RoofA", 1, 0f, ""), ("S_Ante", 1, 0.3f, ""), ("S_Arch", 2, 0.6f, ""),
            ("B_Pump", 2, 0.3f, ""), ("B_Tanks", 0, 0f, ""), ("B_Mach", 2, 0.2f, ""), ("B_Work", 2, 0.3f, ""), ("B_C1", 2, 0f, ""),
            ("B_Store", 2, 0.4f, ""), ("B_Lab", 2, 0.3f, "C"), ("B_Fuse", 1, 0.3f, ""), ("B_C2E", 2, 0f, ""), ("B_Control", 2, 0.2f, ""),
            ("B_Pipes", 2, 0.3f, "R"),
        };

        // sala -> objetos sueltos (encima de los muebles si hay)
        static readonly (string room, string item, int n)[] Loot =
        {
            ("G_Wait", "I_HandgunAmmo", 10), ("G_Garage", "I_Spray", 1), ("G_OffE", "I_HandgunAmmo", 12), ("G_Sec", "I_HandgunAmmo", 10),
            ("G_Break", "I_Spray", 1), ("G_Store", "I_ShotgunAmmo", 6), ("G_Work", "I_HandgunAmmo", 10),
            ("F_Conf", "I_HandgunAmmo", 10), ("F_OffA", "I_Spray", 1), ("F_OffB", "I_HandgunAmmo", 12), ("F_Det", "I_ShotgunAmmo", 6),
            ("F_Records", "I_HandgunAmmo", 10), ("F_Lounge", "I_Spray", 1), ("F_Evid", "I_ShotgunAmmo", 6), ("S_Ante", "I_ShotgunAmmo", 8),
            ("S_Ante", "I_Spray", 1), ("B_Safe", "I_HandgunAmmo", 12), ("B_Safe", "I_Spray", 1), ("B_Mach", "I_ShotgunAmmo", 6),
            ("B_Work", "I_HandgunAmmo", 10), ("B_Control", "I_Spray", 1), ("B_C2W", "I_ShotgunAmmo", 8), ("B_C2W", "I_HandgunAmmo", 12),
        };

        static System.Random rng;

        static Vector3? NavPoint(ComisariaGrande.Room r, List<Vector3> taken, float minDist)
        {
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector3((float)(r.r.xMin + 1f + rng.NextDouble() * (r.r.width - 2f)), r.floor + 0.2f, (float)(r.r.yMin + 1f + rng.NextDouble() * (r.r.height - 2f)));
                if (!NavMesh.SamplePosition(p, out var h, 0.5f, NavMesh.AllAreas)) continue;
                if (Mathf.Abs(h.position.y - r.floor) > 0.4f || !r.r.Contains(new Vector2(h.position.x, h.position.z))) continue;
                if (taken.Any(t => Vector3.Distance(t, h.position) < minDist)) continue;
                return h.position;
            }
            return null;
        }

        [MenuItem("Horror/Comisaria grande/5 Zombis y botin")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---").transform;
            foreach (var n in new[] { "Enemigos", "Botin" }) { var o = level.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
            var zr = new GameObject("Enemigos").transform; zr.SetParent(level);
            var lr = new GameObject("Botin").transform; lr.SetParent(level);
            rng = new System.Random(66);
            var taken = new List<Vector3>();
            int total = 0, dorm = 0;
            GameObject Spawn(string prefab, Vector3 p, string name)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + prefab + ".prefab");
                if (pf == null) return null;
                var z = (GameObject)PrefabUtility.InstantiatePrefab(pf, zr);
                z.name = name; z.transform.SetPositionAndRotation(p + Vector3.up * 0.05f, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                total++;
                return z;
            }
            foreach (var (roomId, n, dp, extra) in Spawns)
            {
                var r = ComisariaGrande.Rooms.FirstOrDefault(x => x.id == roomId); if (r == null) continue;
                for (int i = 0; i < n; i++)
                {
                    var p = NavPoint(r, taken, 2.5f); if (p == null) continue; taken.Add(p.Value);
                    var z = Spawn(Normal[rng.Next(Normal.Length)], p.Value, "Z_" + roomId + "_" + i);
                    if (z != null && rng.NextDouble() < dp) { var ai = z.GetComponent<ZombieAI>(); ai.dormant = true; EditorUtility.SetDirty(ai); PrefabUtility.RecordPrefabInstancePropertyModifications(ai); dorm++; }
                }
                foreach (var c in extra)
                {
                    var p = NavPoint(r, taken, 2.5f); if (p == null) continue; taken.Add(p.Value);
                    if (c == 'R') Spawn("Zombie_OficialReptante", p.Value, "Z_Reptante_" + roomId);
                    else if (c == 'C') Spawn("Zombie_Carronero", p.Value, "Z_Carronero_" + roomId);
                }
            }
            // botin: encima del primer mueble que haya en un punto al azar de la sala (o en el suelo)
            Physics.SyncTransforms();
            int items = 0;
            foreach (var (roomId, item, n) in Loot)
            {
                var r = ComisariaGrande.Rooms.FirstOrDefault(x => x.id == roomId); var it = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + item + ".asset");
                if (r == null || it == null) continue;
                Vector3 best = Vector3.zero; bool ok = false;
                for (int i = 0; i < 40 && !ok; i++)
                {
                    float x = (float)(r.r.xMin + 0.8f + rng.NextDouble() * (r.r.width - 1.6f)), z = (float)(r.r.yMin + 0.8f + rng.NextDouble() * (r.r.height - 1.6f));
                    var hits = Physics.RaycastAll(new Vector3(x, r.floor + 1.5f, z), Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                    if (hits.Length == 0) continue;
                    var top = hits[0];
                    if (top.point.y > r.floor + 0.5f && top.point.y < r.floor + 1.3f && Vector3.Angle(top.normal, Vector3.up) < 10f) { best = top.point; ok = true; }
                    else if (i > 30 && top.point.y < r.floor + 0.1f) { best = top.point; ok = true; }
                }
                if (!ok) continue;
                var pk = Pickup.Spawn(it, n, best + Vector3.up * 0.12f); pk.transform.SetParent(lr); items++;
            }
            var oldMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script; Physics.SyncTransforms();
            for (int i = 0; i < 150; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase E: " + total + " zombis (" + dorm + " en letargo), " + items + " objetos sueltos";
        }
    }
}
#endif
