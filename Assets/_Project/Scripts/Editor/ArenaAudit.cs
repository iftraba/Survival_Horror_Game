#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Auditoria de las arenas de los jefes: busca las celdas (rejilla de 0,2 m) a las que el jugador puede llegar caminando desde
    /// la entrada (con su capsula real: radio 0,4 + piel) pero a las que el jefe NO puede (no hay NavMesh o esta desconectado).
    /// Son los sitios donde el jugador se queda "afk" disparando. Con 'seal' crea muros invisibles ("ArenaBlockers") que rellenan
    /// esas celdas para que el jugador tampoco pueda entrar. Menu: Horror/Auditar arenas (informe) y Horror/Auditar arenas y sellar huecos.
    /// </summary>
    public static class ArenaAudit
    {
        const float Cell = 0.2f;

        public struct Arena { public string name; public Vector2 min, max, entrance; }

        public static readonly Arena[] Arenas =
        {
            new Arena { name = "Arena 1 (primer jefe)", min = new Vector2(-14.3f, 9.3f), max = new Vector2(14.3f, 19.7f), entrance = new Vector2(0f, 9.8f) },
            new Arena { name = "Sala de calderas (segundo jefe)", min = new Vector2(-14.3f, 66.3f), max = new Vector2(14.3f, 83.8f), entrance = new Vector2(0f, 67.0f) },
        };

        [MenuItem("Horror/Auditar arenas")]
        public static void Menu() { Debug.Log("[Horror] " + Run(false)); }

        [MenuItem("Horror/Auditar arenas y sellar huecos")]
        public static void MenuSeal() { Debug.Log("[Horror] " + Run(true)); }

        static bool Ignorable(Collider c)
        {
            if (c.isTrigger) return true;
            if (c.GetComponentInParent<ZombieAI>() != null || c.GetComponentInParent<Pickup>() != null || c.GetComponentInParent<EjectedCasing>() != null) return true;
            if (c.GetComponentInParent<PlayerController>() != null || c.GetComponentInParent<Door>() != null) return true;
            return false;
        }

        static bool PlayerFree(Vector2 p)
        {
            const float r = 0.48f;                                          // radio del CharacterController (0,4) + piel (0,08)
            Vector3 a = new Vector3(p.x, 0.3f + r, p.y), b = new Vector3(p.x, 2f - r, p.y);   // por encima del escalon de 0,3 m
            foreach (var c in Physics.OverlapCapsule(a, b, r, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignorable(c)) return false;
            return true;
        }

        // las hojas de las puertas no cuentan para el NavMesh (se desactivan al hornear): sus celdas no son huecos
        static List<Bounds> doorBounds;
        static bool NearDoor(Vector2 p)
        {
            if (doorBounds == null)
            {
                doorBounds = new List<Bounds>();
                foreach (var d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
                {
                    var cs = d.GetComponentsInChildren<Collider>(true);
                    if (cs.Length == 0) continue;
                    var b = cs[0].bounds; foreach (var c in cs) b.Encapsulate(c.bounds);
                    b.Expand(new Vector3(1.6f, 0f, 1.6f));
                    doorBounds.Add(b);
                }
                foreach (var e in Object.FindObjectsByType<ExitDoor>(FindObjectsSortMode.None))
                {
                    var c = e.GetComponent<Collider>(); if (c == null) continue;
                    var b = c.bounds; b.Expand(new Vector3(1.6f, 0f, 1.6f)); doorBounds.Add(b);
                }
            }
            var pt = new Vector3(p.x, 1f, p.y);
            foreach (var b in doorBounds) if (b.Contains(pt)) return true;
            return false;
        }

        static bool OnNavMesh(Vector2 p) => NavMesh.SamplePosition(new Vector3(p.x, 0.1f, p.y), out _, 0.3f, NavMesh.AllAreas);

        public static string Run(bool seal)
        {
            var rt = Object.FindFirstObjectByType<RuntimeNavMesh>();
            foreach (var g in rt.disableDuringBake) if (g != null) g.SetActive(false);
            rt.surface.BuildNavMesh();
            foreach (var g in rt.disableDuringBake) if (g != null) g.SetActive(true);
            Physics.SyncTransforms();

            doorBounds = null;
            var sb = new StringBuilder();
            foreach (var old in new[] { "ArenaBlockers" }) { var o = GameObject.Find(old); if (seal && o != null) Object.DestroyImmediate(o); }
            if (seal) { Physics.SyncTransforms(); rt.surface.BuildNavMesh(); }   // la limpieza cambia el estado
            Transform blockers = null;
            foreach (var ar in Arenas)
            {
                int nx = Mathf.CeilToInt((ar.max.x - ar.min.x) / Cell), nz = Mathf.CeilToInt((ar.max.y - ar.min.y) / Cell);
                Vector2 Pos(int ix, int iz) => new Vector2(ar.min.x + (ix + 0.5f) * Cell, ar.min.y + (iz + 0.5f) * Cell);
                var free = new bool[nx, nz]; var nav = new bool[nx, nz];
                for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++) { var p = Pos(ix, iz); free[ix, iz] = PlayerFree(p); nav[ix, iz] = OnNavMesh(p); }
                int sx = Mathf.Clamp((int)((ar.entrance.x - ar.min.x) / Cell), 0, nx - 1), sz = Mathf.Clamp((int)((ar.entrance.y - ar.min.y) / Cell), 0, nz - 1);
                bool[,] Flood(bool[,] ok, int x0, int z0)
                {
                    var seen = new bool[nx, nz]; var q = new Queue<Vector2Int>();
                    if (!ok[x0, z0]) return seen;
                    seen[x0, z0] = true; q.Enqueue(new Vector2Int(x0, z0));
                    while (q.Count > 0)
                    {
                        var c = q.Dequeue();
                        foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                        {
                            int x = c.x + d.x, z = c.y + d.y;
                            if (x < 0 || z < 0 || x >= nx || z >= nz || seen[x, z] || !ok[x, z]) continue;
                            seen[x, z] = true; q.Enqueue(new Vector2Int(x, z));
                        }
                    }
                    return seen;
                }
                var playerReach = Flood(free, sx, sz);
                var navReach = Flood(nav, sx, sz);
                int total = 0, pockets = 0;
                var pocketCells = new List<Vector2Int>();
                for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++)
                    if (playerReach[ix, iz]) { total++; if (!navReach[ix, iz] && !NearDoor(Pos(ix, iz))) { pockets++; pocketCells.Add(new Vector2Int(ix, iz)); } }
                sb.AppendLine($"{ar.name}: {total} celdas alcanzables por el jugador ({total * Cell * Cell:F0} m2), {pockets} sin acceso para el jefe ({pockets * Cell * Cell:F2} m2)");
                if (pockets > 0)
                {
                    // grupos de celdas pegadas (informe)
                    var left = new HashSet<Vector2Int>(pocketCells); int k = 0;
                    while (left.Count > 0 && k < 12)
                    {
                        var first = left.First(); var q = new Queue<Vector2Int>(); q.Enqueue(first); left.Remove(first);
                        Vector2 mn = Pos(first.x, first.y), mx = mn; int n = 0;
                        while (q.Count > 0)
                        {
                            var c = q.Dequeue(); n++; var p = Pos(c.x, c.y); mn = Vector2.Min(mn, p); mx = Vector2.Max(mx, p);
                            foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                            { var nb = c + d; if (left.Remove(nb)) q.Enqueue(nb); }
                        }
                        sb.AppendLine($"    hueco {++k}: {n} celdas, x {mn.x:F1}..{mx.x:F1}, z {mn.y:F1}..{mx.y:F1}");
                    }
                    if (seal)
                    {
                        if (blockers == null) blockers = new GameObject("ArenaBlockers").transform;
                        // se rellenan por filas de celdas contiguas (muros invisibles de 3 m de alto)
                        var byRow = pocketCells.GroupBy(c => c.y).OrderBy(g => g.Key);
                        int boxes = 0;
                        foreach (var row in byRow)
                        {
                            var xs = row.Select(c => c.x).OrderBy(v => v).ToList();
                            int start = xs[0], prev = xs[0];
                            for (int i = 1; i <= xs.Count; i++)
                            {
                                if (i < xs.Count && xs[i] == prev + 1) { prev = xs[i]; continue; }
                                var go = new GameObject("Blocker"); go.transform.SetParent(blockers);
                                var bc = go.AddComponent<BoxCollider>();
                                float w = (prev - start + 1) * Cell + 0.04f;
                                go.transform.position = new Vector3(ar.min.x + (start + (prev - start + 1) * 0.5f) * Cell, 1.5f, ar.min.y + (row.Key + 0.5f) * Cell);
                                bc.size = new Vector3(w, 3f, Cell + 0.04f);
                                boxes++;
                                if (i < xs.Count) { start = xs[i]; prev = xs[i]; }
                            }
                        }
                        sb.AppendLine($"    sellado con {boxes} muros invisibles");
                    }
                }
            }
            if (seal && blockers != null) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            return sb.ToString();
        }
    }
}
#endif
