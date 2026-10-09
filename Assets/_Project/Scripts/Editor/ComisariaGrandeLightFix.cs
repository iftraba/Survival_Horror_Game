#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase F (menu Horror/Comisaria grande/7 Pase de luces): quita la fuga de luz por fuera del edificio.
    /// Diagnostico (2026-10-09): las manchas en la cara exterior de los muros (se veian en el archivo) las dan las luces de
    /// relleno "Fill" (puntuales, sin sombra, rango 5, una por lampara), no los focos, que si proyectan sombra. Aqui se recorta el
    /// rango de cada relleno que tiene un muro exterior mas cerca que su alcance, para que su luz muera en el muro.
    /// Idempotente: parte siempre del rango base (5) y lo recorta segun la sala de su lampara ("Lamp_&lt;sala&gt;"), asi que se puede repetir.
    /// Si se rehace el nivel con el menu 1, hay que volver a ejecutarlo.
    /// </summary>
    public static class ComisariaGrandeLightFix
    {
        const float FillBase = 5f, MinRange = 2.5f, Margin = 0.1f;

        [MenuItem("Horror/Comisaria grande/7 Pase de luces")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---").transform;

            int total = 0, trimmed = 0; float shortest = FillBase;
            foreach (var fill in level.GetComponentsInChildren<Light>(true))
            {
                if (fill.name != "Fill" || fill.transform.parent == null || !fill.transform.parent.name.StartsWith("Lamp_")) continue;
                var room = ComisariaGrande.Rooms.FirstOrDefault(r => r.id == fill.transform.parent.name.Substring(5));
                if (room == null) continue;
                total++;
                var p = fill.transform.position;
                float d = ExteriorDistance(room, new Vector2(p.x, p.z));
                float range = d < FillBase + 0.2f ? Mathf.Clamp(d - Margin, MinRange, FillBase) : FillBase;
                // luz que sube por el forjado e ilumina por fuera los muros que se levantan sobre la azotea (archivo, antesala, caseta)
                float roof = RoofDistance(p);
                if (roof < FillBase + 0.2f) range = Mathf.Min(range, Mathf.Clamp(roof - Margin, MinRange, FillBase));
                if (!Mathf.Approximately(fill.range, range)) { fill.range = range; EditorUtility.SetDirty(fill); }
                if (range < FillBase) { trimmed++; shortest = Mathf.Min(shortest, range); }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "pase de luces: " + total + " rellenos, " + trimmed + " recortados (el mas corto, " + shortest.ToString("F1") + " m)";
        }

        /// <summary>
        /// Distancia 3D del relleno a la base del muro mas cercano que se levanta sobre una azotea (salas de la segunda planta: archivo,
        /// antesala y caseta). Un relleno que esta debajo de una de ellas ilumina su suelo por dentro, no por fuera: infinito.
        /// </summary>
        static float RoofDistance(Vector3 p)
        {
            float best = float.PositiveInfinity;
            foreach (var q in ComisariaGrande.Rooms)
            {
                if (!q.Indoor || q.floor < ComisariaGrande.F2 - 0.01f) continue;
                var xz = new Vector2(p.x, p.z);
                if (q.r.Contains(xz)) return float.PositiveInfinity;
                float dv = q.floor - p.y;
                if (dv < 0f) continue;
                float dx = Mathf.Max(q.r.xMin - xz.x, 0f, xz.x - q.r.xMax), dz = Mathf.Max(q.r.yMin - xz.y, 0f, xz.y - q.r.yMax);
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz + dv * dv));
            }
            return best;
        }

        /// <summary>Distancia (en planta) del punto al lado de la sala que da al exterior; infinito si ninguno.</summary>
        static float ExteriorDistance(ComisariaGrande.Room room, Vector2 p)
        {
            var r = room.r;
            // lado, direccion hacia fuera, distancia del punto a ese lado
            var sides = new (Vector2 a, Vector2 b, Vector2 outward, float dist)[]
            {
                (new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), Vector2.left,  p.x - r.xMin),
                (new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), Vector2.right, r.xMax - p.x),
                (new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), Vector2.down,  p.y - r.yMin),
                (new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), Vector2.up,    r.yMax - p.y),
            };
            float best = float.PositiveInfinity;
            foreach (var s in sides)
            {
                // un lado es exterior si la franja de fuera no la cubre ninguna otra sala interior de la misma planta (en la mayoria de sus puntos)
                int uncovered = 0;
                for (int i = 1; i <= 5; i++)
                {
                    var q = Vector2.Lerp(s.a, s.b, i / 6f) + s.outward * 0.4f;
                    bool covered = ComisariaGrande.Rooms.Any(o => o != room && o.Indoor && Mathf.Abs(o.floor - room.floor) < 0.1f && o.r.Contains(q));
                    if (!covered) uncovered++;
                }
                if (uncovered >= 3 && s.dist < best) best = s.dist;
            }
            return best;
        }
    }
}
#endif
