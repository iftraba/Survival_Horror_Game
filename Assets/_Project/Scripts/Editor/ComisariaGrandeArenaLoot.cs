#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase F (menu Horror/Comisaria grande/6 Reservas de arenas): municion de reserva dentro de las arenas de
    /// los dos jefes, para que el jugador que entra con poca no se quede atrapado (la puerta se sella hasta que el jefe muere).
    /// Va aparte del menu 5 para no rehacer zombis ni botin. Repetible: rehace solo el grupo "Botin_Arenas".
    /// Cada reserva va en el suelo, a 4,5-8,5 m del jefe, con NavMesh a menos de 0,6 m (alcanzable) y separadas entre si.
    /// </summary>
    public static class ComisariaGrandeArenaLoot
    {
        // sala, jefe (bajo Puzles/Jefes), disparador que sella la puerta (si cubre la arena: la reserva debe caer dentro, para no poder
        // cogerla antes de que empiece el combate; el del jefe 2 es solo el rellano, no la arena) y reservas.
        // Valores provisionales: se ajustan jugando.
        static readonly (string room, string boss, string trigger, (string item, int n)[] items)[] Arenas =
        {
            ("S_Arch", "Boss", "BossRoomTrigger", new[] { ("I_ShotgunAmmo", 6), ("I_HandgunAmmo", 12) }),
            ("B_Boiler", "Boss_2", null, new[] { ("I_ShotgunAmmo", 6), ("I_HandgunAmmo", 12) }),
        };

        [MenuItem("Horror/Comisaria grande/6 Reservas de arenas")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---").transform;
            var old = level.Find("Botin_Arenas"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Botin_Arenas").transform; root.SetParent(level);
            Physics.SyncTransforms();

            var rng = new System.Random(77);
            var log = new List<string>(); int placed = 0;
            foreach (var (roomId, bossName, triggerName, items) in Arenas)
            {
                var room = ComisariaGrande.Rooms.FirstOrDefault(r => r.id == roomId);
                var boss = level.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == bossName && t.parent != null && t.parent.name == "Jefes");
                if (room == null || boss == null) { log.Add(roomId + ": falta " + (room == null ? "la sala" : "el jefe " + bossName)); continue; }
                Bounds? zone = null;
                if (triggerName != null)
                {
                    var tr = level.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => c.name == triggerName && c.isTrigger);
                    if (tr == null) { log.Add(roomId + ": falta el disparador " + triggerName); continue; }
                    zone = tr.bounds;
                }
                var taken = new List<Vector3>();
                foreach (var (itemName, n) in items)
                {
                    var it = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + itemName + ".asset");
                    if (it == null) { log.Add(roomId + ": falta " + itemName); continue; }
                    Vector3? spot = null;
                    for (int i = 0; i < 600 && spot == null; i++)
                    {
                        float ang = (float)(rng.NextDouble() * Mathf.PI * 2f), rad = 4.5f + (float)rng.NextDouble() * 4f;
                        float x = boss.position.x + Mathf.Cos(ang) * rad, z = boss.position.z + Mathf.Sin(ang) * rad;
                        if (!room.r.Contains(new Vector2(x, z)) || x < room.r.xMin + 1f || x > room.r.xMax - 1f || z < room.r.yMin + 1f || z > room.r.yMax - 1f) continue;
                        if (zone != null && (x < zone.Value.min.x + 0.5f || x > zone.Value.max.x - 0.5f || z < zone.Value.min.z + 0.5f || z > zone.Value.max.z - 0.5f)) continue;
                        var hits = Physics.RaycastAll(new Vector3(x, room.floor + 2f, z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                        if (hits.Length == 0) continue;
                        var top = hits[0];
                        if (top.point.y < room.floor - 0.05f || top.point.y > room.floor + 0.15f) continue;      // solo suelo desnudo: no sobre muebles rompibles
                        if (!NavMesh.SamplePosition(top.point, out var nh, 0.6f, NavMesh.AllAreas) || Mathf.Abs(nh.position.y - room.floor) > 0.4f) continue;
                        if (taken.Any(t => Vector3.Distance(t, top.point) < 3f)) continue;
                        spot = top.point;
                    }
                    if (spot == null) { log.Add(roomId + ": sin sitio para " + itemName); continue; }
                    taken.Add(spot.Value);
                    var pk = Pickup.Spawn(it, n, spot.Value + Vector3.up * 0.12f);
                    pk.name = "Reserva_" + roomId + "_" + itemName;
                    pk.transform.SetParent(root);
                    placed++;
                    log.Add(roomId + ": " + n + " x " + itemName + " en " + spot.Value.ToString("F1") + " (a " + Vector3.Distance(new Vector3(boss.position.x, spot.Value.y, boss.position.z), spot.Value).ToString("F1") + " m del jefe)");
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "reservas de arenas: " + placed + " objetos. " + string.Join(" | ", log);
        }
    }
}
#endif
