#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Anclas por sala: en vez de coordenadas absolutas, los objetos de puzle, el botin y las notas se colocan "encima de un mueble
    /// de este tipo de esta sala" (la cizalla en el banco de trabajo del garaje, la tarjeta en la mesa de seguridad...). Asi siguen
    /// funcionando si cambia el trazado. Se usa desde los menus 4 (puzles) y 5 (zombis y botin), con el mobiliario ya puesto (menu 3).
    ///
    /// La superficie se saca de la MALLA del mueble (triangulos que miran hacia arriba), no de su collider: los muebles llevan una sola
    /// caja de collider del alto total (un banco con tablero de herramientas mide 2,15 m) y encima de ella no se puede dejar nada util.
    /// Se elige la superficie horizontal grande mas cercana a 0,9 m (alcanzable), un punto libre de objetos pequenos y de otros
    /// objetos ya colocados, y se recorta el alto de la caja del mueble a esa superficie para que el objeto se asiente sin que
    /// la fisica lo expulse. Recortar el alto no cambia por donde se puede andar (la huella es la misma).
    /// </summary>
    public static class ComisariaGrandeAnchors
    {
        static Transform level, mob;
        static List<Bounds> smalls;
        static readonly List<Vector3> taken = new List<Vector3>();
        static readonly Dictionary<Transform, List<(Vector3 c, float area)>> surfaces = new Dictionary<Transform, List<(Vector3, float)>>();
        public static readonly List<string> Log = new List<string>();

        public static void Init(Transform levelRoot)
        {
            level = levelRoot; mob = level.Find("Props/Mobiliario");
            Physics.SyncTransforms();
            // cosas pequenas encima de las mesas (ordenador, lampara, papeles): holders sin collider
            smalls = new List<Bounds>();
            if (mob != null)
                foreach (Transform t in mob)
                {
                    if (t.GetComponent<Collider>() != null) continue;
                    var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    smalls.Add(b);
                }
            Log.Clear(); taken.Clear(); surfaces.Clear();
            foreach (var pk in level.GetComponentsInChildren<Pickup>(true)) taken.Add(pk.transform.position);          // lo ya colocado por otro menu
        }

        /// <summary>Muebles (con collider) de esta sala cuyo nombre es uno de 'names'.</summary>
        public static List<Transform> Furniture(string roomId, params string[] names)
        {
            var room = ComisariaGrande.Rooms.FirstOrDefault(r => r.id == roomId);
            var res = new List<Transform>();
            if (room == null || mob == null) return res;
            foreach (Transform t in mob)
            {
                if (!names.Contains(t.name)) continue;
                var bc = t.GetComponent<BoxCollider>(); if (bc == null) continue;
                var b = bc.bounds;
                if (!room.r.Contains(new Vector2(b.center.x, b.center.z))) continue;
                if (b.min.y < room.floor - 0.3f || b.min.y > room.floor + 0.6f) continue;
                res.Add(t);
            }
            return res;
        }

        /// <summary>Centroides (con area) de la superficie horizontal elegida del mueble; null si no tiene ninguna alcanzable.</summary>
        static List<(Vector3 c, float area)> Surface(Transform furniture, out float y)
        {
            y = 0f;
            var bc = furniture.GetComponent<BoxCollider>(); float floorY = bc.bounds.min.y;
            var tris = new List<(Vector3 c, float area)>();
            foreach (var mf in furniture.GetComponentsInChildren<MeshFilter>())
            {
                var m = mf.sharedMesh; if (m == null) continue;
                var v = m.vertices; var tr = m.triangles; var tf = mf.transform;
                for (int i = 0; i < tr.Length; i += 3)
                {
                    var a = tf.TransformPoint(v[tr[i]]); var b = tf.TransformPoint(v[tr[i + 1]]); var c = tf.TransformPoint(v[tr[i + 2]]);
                    var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * 0.5f;
                    if (area < 0.003f || n.normalized.y < 0.95f) continue;
                    var ctr = (a + b + c) / 3f; float h = ctr.y - floorY;
                    if (h < 0.35f || h > 1.5f) continue;
                    tris.Add((ctr, area));
                }
            }
            if (tris.Count == 0) return null;
            // agrupa por altura (cada 5 cm) y se queda con las grandes; entre ellas la mas cercana a 0,9 m
            var groups = tris.GroupBy(t => Mathf.Round(t.c.y * 20f) / 20f).Select(g => (y: g.Key, area: g.Sum(t => t.area))).ToList();
            float maxArea = groups.Max(g => g.area);
            var pick = groups.Where(g => g.area >= 0.5f * maxArea).OrderBy(g => Mathf.Abs(g.y - floorY - 0.9f)).First();
            float sel = pick.y; y = sel;
            return tris.Where(t => Mathf.Abs(t.c.y - sel) < 0.03f).ToList();
        }

        /// <summary>Recorta el alto de la caja del mueble hasta 'topY' (mundo) para que lo que se deje encima no quede dentro del collider.</summary>
        static void CutBoxTo(Transform furniture, float topY)
        {
            var bc = furniture.GetComponent<BoxCollider>(); var b = bc.bounds;
            if (topY + 0.04f >= b.max.y) return;
            float h = topY + 0.005f - b.min.y;
            var c = bc.center; var s = bc.size;
            float localMin = b.min.y - furniture.position.y;
            bc.size = new Vector3(s.x, h, s.z); bc.center = new Vector3(c.x, localMin + h / 2f, c.z);
            Physics.SyncTransforms();
        }

        /// <summary>Punto de la superficie del mueble mas cercano a 'pref' (o al centro) y libre de objetos pequenos y de otros objetos colocados.</summary>
        public static Vector3? TopSpot(Transform furniture, Vector3? pref = null, float clearRadius = 0.12f)
        {
            if (furniture.GetComponent<BoxCollider>() == null) return null;
            if (!surfaces.TryGetValue(furniture, out var tris))
            {
                tris = Surface(furniture, out float sy);
                surfaces[furniture] = tris;
                if (tris != null) CutBoxTo(furniture, sy);
            }
            if (tris == null) return null;
            var center = tris.Aggregate(Vector3.zero, (acc, t) => acc + t.c * t.area) / tris.Sum(t => t.area);
            var target = pref.HasValue ? new Vector3(pref.Value.x, center.y, pref.Value.z) : center;
            Vector3? best = null; float bestScore = float.MaxValue;
            foreach (var (c, area) in tris)
            {
                var p = c;
                bool blocked = taken.Any(q => Vector3.Distance(q, p) < 0.3f);
                if (!blocked)
                    foreach (var s in smalls)
                        if (p.x > s.min.x - clearRadius && p.x < s.max.x + clearRadius && p.z > s.min.z - clearRadius && p.z < s.max.z + clearRadius && s.min.y < p.y + 0.5f && s.max.y > p.y - 0.05f) { blocked = true; break; }
                if (blocked) continue;
                float score = Vector3.Distance(p, target) - area * 0.5f;      // cerca del objetivo y, a igualdad, en un triangulo grande (no en un borde)
                if (score < bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        /// <summary>
        /// Sitio para un objeto: encima del 'index'-esimo mueble (ordenado por cercania a 'pref', o por x,z) de la primera clase de
        /// 'names' que exista en la sala y tenga un punto libre. Si no hay ninguno, el suelo en 'pref' (o el centro de la sala) y lo apunta.
        /// </summary>
        public static Vector3 Spot(string roomId, string[] names, int index = 0, Vector3? pref = null, string what = null)
        {
            foreach (var n in names)
            {
                var list = Furniture(roomId, n);
                if (list.Count == 0) continue;
                list = pref.HasValue
                    ? list.OrderBy(t => Vector3.Distance(t.GetComponent<BoxCollider>().bounds.center, pref.Value)).ToList()
                    : list.OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
                for (int k = 0; k < list.Count; k++)
                {
                    var f = list[(index + k) % list.Count];
                    var p = TopSpot(f, pref);
                    if (p.HasValue) { taken.Add(p.Value); return p.Value; }
                }
            }
            var room = ComisariaGrande.Rooms.First(r => r.id == roomId);
            var c = pref ?? new Vector3(room.r.center.x, room.floor, room.r.center.y);
            Log.Add("sin mueble (" + string.Join("/", names) + ") en " + roomId + (what != null ? " para " + what : "") + ": al suelo");
            if (Physics.Raycast(new Vector3(c.x, room.floor + 2f, c.z), Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
            return new Vector3(c.x, room.floor, c.z);
        }
    }
}
#endif
