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
        const float FillBase = 6f, MinRange = 2.5f, Margin = 0.1f;

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
                string roomId = fill.transform.parent.name.Substring(5); int hash = roomId.IndexOf('#'); if (hash >= 0) roomId = roomId.Substring(0, hash);
                var room = ComisariaGrande.Rooms.FirstOrDefault(r => r.id == roomId);
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
            int outdoor = OutdoorLights(level);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "pase de luces: " + total + " rellenos, " + trimmed + " recortados (el mas corto, " + shortest.ToString("F1") + " m), " + outdoor + " luces exteriores";
        }

        /// <summary>Luces de emergencia (puntuales, sin sombra, con un piloto emisivo) en la rampa del garaje, su explanada y el callejon oeste. Rehace el grupo "Luces_Exterior".</summary>
        static int OutdoorLights(Transform level)
        {
            var old = level.Find("Luces_Exterior"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Luces_Exterior").transform; root.SetParent(level);
            var bulbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Bulb_FFF2D9.mat");
            int n = 0;
            void Post(string name, Vector3 p, Color c, float range, float intensity)
            {
                var g = new GameObject(name); g.transform.SetParent(root); g.transform.position = p;
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
                var head = GameObject.CreatePrimitive(PrimitiveType.Cube); head.name = "Foco"; Object.DestroyImmediate(head.GetComponent<Collider>());
                head.transform.SetParent(g.transform, false); head.transform.localPosition = Vector3.up * 0.12f; head.transform.localScale = new Vector3(0.32f, 0.1f, 0.32f);
                if (bulbMat != null) head.GetComponent<Renderer>().sharedMaterial = bulbMat;
                head.isStatic = true; n++;
            }
            var sodium = new Color(1f, 0.7f, 0.35f);
            foreach (float z in new[] { 12f, 22f, 32f, 41f })                                   // rampa: sube de z 5,5 (y -4,5) a z 44 (y 0)
                Post("Rampa_" + z, new Vector3(-39.35f, Mathf.Lerp(ComisariaGrande.B, 0f, (z - 5.5f) / 38.5f) + 2.9f, z), sodium, 8f, 5f);
            foreach (float x in new[] { -38f, -34f })                                            // explanada al pie de la rampa
                Post("Explanada_" + x, new Vector3(x, ComisariaGrande.B + 3.1f, 2.7f), sodium, 8f, 5f);
            foreach (float z in new[] { 14f, 27f, 40f })                                         // callejon oeste, mas tenue
                Post("Callejon_" + z, new Vector3(-34.5f, 3.0f, z), new Color(0.85f, 0.9f, 1f), 7f, 3f);
            return n;
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
