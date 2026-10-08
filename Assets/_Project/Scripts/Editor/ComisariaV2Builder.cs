#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria nueva (rama rediseno-comisaria, fase 1: estructura). Menu: Horror/Comisaria v2/1 Estructura.
    /// Crea Assets/Scenes/Comisaria_v2.unity copiando la escena actual (se quedan el jugador, el HUD, el audio, GameFlow, la camara
    /// y la iluminacion global) y levanta un nivel nuevo de cuatro plantas:
    ///   sotano (y = -4,5) | planta baja (0) | primera planta (4) | archivo (7,5; techo a 6,5 m para el salto del jefe 1).
    /// Planta baja: vestibulo con puertas a izquierda y derecha y escalera de caracol a la primera planta. Un hueco de ascensor
    /// recorre las cuatro plantas (el ascensor y su logica, mas adelante). Plano en docs/rediseno-comisaria.md. Repetible: rehace
    /// el nivel de la escena v2 entero.
    /// </summary>
    public static class ComisariaV2Builder
    {
        public const string ScenePath = "Assets/Scenes/Comisaria_v2.unity";
        const string Mats = "Assets/_Project/Materials/";
        const string P = "Assets/_Project/Prefabs/";

        // alturas: cara superior del suelo de cada planta y cara inferior de su techo
        public const float BasementY = -4.5f, GroundY = 0f, FirstY = 4f, ArchiveY = 7.5f;
        const float BasementCeil = -1.0f, GroundCeil = 3.5f, FirstCeil = 7.0f, ArchiveCeil = 14.0f;
        const float X0 = -20f, X1 = 20f, Z0 = 0f, Z1 = 32f;          // planta del edificio
        const float DoorH = 2.45f;                                     // altura del hueco de paso

        // escalera de caracol del vestibulo (planta baja -> primera) y ascensor
        public static readonly Vector3 StairC = new Vector3(5f, 0f, 12f);
        const float StairR = 2.2f;
        static readonly Rect StairHole = new Rect(StairC.x - 2.5f, StairC.z - 2.5f, 5f, 5f);
        static readonly Rect Shaft = new Rect(17f, 12f, 3f, 3f);       // hueco del ascensor (x 17-20, z 12-15)

        static Material wall, floorM, ceil, wood, metal, column;
        static Transform level, details;
        static readonly List<GameObject> leaves = new List<GameObject>();
        static int lamps;

        static T Call<T>(string method, params object[] args)
        {
            var m = typeof(TestSceneBuilder).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            return (T)m.Invoke(null, args);
        }

        static Mesh Tiled(Vector3 pos, Vector3 size, float tile) => Call<Mesh>("TiledUnitCube", pos, size, tile);

        // ------------------------------------------------------------------ piezas
        static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material m, float tile, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = c; go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (tile > 0f) go.GetComponent<MeshFilter>().sharedMesh = Tiled(c, s, tile);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        /// <summary>Losa horizontal (de y0 a y1) que cubre el rectangulo menos los huecos (se trocea alrededor de ellos).</summary>
        static void Slab(string name, Rect r, float y0, float y1, Rect[] holes, Material m, float tile)
        {
            var xs = new List<float> { r.xMin, r.xMax }; var zs = new List<float> { r.yMin, r.yMax };
            foreach (var h in holes) { xs.Add(Mathf.Clamp(h.xMin, r.xMin, r.xMax)); xs.Add(Mathf.Clamp(h.xMax, r.xMin, r.xMax)); zs.Add(Mathf.Clamp(h.yMin, r.yMin, r.yMax)); zs.Add(Mathf.Clamp(h.yMax, r.yMin, r.yMax)); }
            xs = xs.Distinct().OrderBy(v => v).ToList(); zs = zs.Distinct().OrderBy(v => v).ToList();
            for (int i = 0; i < xs.Count - 1; i++)
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    var cell = Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]);
                    if (cell.width < 0.01f || cell.height < 0.01f) continue;
                    if (holes.Any(h => h.Contains(cell.center))) continue;
                    Box(name, level, new Vector3(cell.center.x, (y0 + y1) / 2f, cell.center.y), new Vector3(cell.width, y1 - y0, cell.height), m, tile);
                }
        }

        /// <summary>Planta: suelo encima de la losa y techo (placas) debajo de la siguiente.</summary>
        static void Storey(string name, float floorY, float ceilY, float nextFloorY, Rect area, Rect[] floorHoles, Rect[] ceilHoles)
        {
            Slab(name + "_Floor", area, floorY - 0.3f, floorY, floorHoles, floorM, 4f);
            Slab(name + "_Ceiling", area, ceilY, Mathf.Min(ceilY + 0.12f, nextFloorY - 0.3f), ceilHoles, ceil, 2.4f);
        }

        /// <summary>Pared de (a) a (b) en planta, de y0 a y1, con huecos de paso [centro, ancho] y dintel encima. Rodapie a los dos lados.</summary>
        static void Wall(string name, Vector2 a, Vector2 b, float y0, float y1, float t = 0.25f, params Vector2[] gaps)
        {
            bool alongX = Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.y - a.y);
            float s0 = alongX ? Mathf.Min(a.x, b.x) : Mathf.Min(a.y, b.y), s1 = alongX ? Mathf.Max(a.x, b.x) : Mathf.Max(a.y, b.y);
            float fixedC = alongX ? a.y : a.x;
            var cuts = new List<(float, float, bool)>();      // tramos (desde, hasta, es hueco)
            float s = s0;
            foreach (var g in gaps.OrderBy(g => g.x))
            {
                float g0 = g.x - g.y / 2f, g1 = g.x + g.y / 2f;
                if (g0 > s) cuts.Add((s, g0, false));
                cuts.Add((g0, g1, true));
                s = g1;
            }
            if (s < s1) cuts.Add((s, s1, false));
            foreach (var (c0, c1, isGap) in cuts)
            {
                float len = c1 - c0, mid = (c0 + c1) / 2f;
                float yb = isGap ? y0 + DoorH : y0;
                if (y1 - yb < 0.02f) continue;
                var c = alongX ? new Vector3(mid, (yb + y1) / 2f, fixedC) : new Vector3(fixedC, (yb + y1) / 2f, mid);
                var size = alongX ? new Vector3(len, y1 - yb, t) : new Vector3(t, y1 - yb, len);
                Box(isGap ? name + "_Lintel" : name, level, c, size, wall, 3f);
                if (isGap) continue;
                foreach (int side in new[] { -1, 1 })           // rodapie
                {
                    var off = alongX ? new Vector3(0, 0, side * (t / 2 + 0.02f)) : new Vector3(side * (t / 2 + 0.02f), 0, 0);
                    var bs = alongX ? new Vector3(len, 0.14f, 0.04f) : new Vector3(0.04f, 0.14f, len);
                    Box(name + "_Base", details, new Vector3(c.x, y0 + 0.07f, c.z) + off, bs, wood, 1f, false);
                }
            }
        }

        /// <summary>Puerta de madera (prefab) centrada en el hueco 'center' de una pared a lo largo de X (alongX) o de Z.</summary>
        static GameObject Door(string name, Vector3 center, bool alongX, bool wide = true)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(P + (wide ? "Doors/Door_Wood_150.prefab" : "Doors/Door_Wood_140.prefab"));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, level);
            go.name = name;
            float half = wide ? 0.75f : 0.7f;
            go.transform.SetPositionAndRotation(alongX ? center - new Vector3(half, 0, 0) : center - new Vector3(0, 0, half), Quaternion.Euler(0, alongX ? 0f : -90f, 0));
            foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == "Leaf") leaves.Add(t.gameObject);
            return go;
        }

        /// <summary>Lamparas de techo en rejilla sobre un rectangulo (la lampara del constructor de escena, subida a su techo).</summary>
        static void Lamps(Rect r, float ceilY, float spacing, Color color, float intensity = 32f)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt(r.width / spacing)), nz = Mathf.Max(1, Mathf.RoundToInt(r.height / spacing));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var xz = new Vector3(r.xMin + r.width * (i + 0.5f) / nx, 0f, r.yMin + r.height * (j + 0.5f) / nz);
                    if (StairHole.Contains(new Vector2(xz.x, xz.z)) || Shaft.Contains(new Vector2(xz.x, xz.z))) continue;
                    var lamp = Call<CeilingLamp>("Lamp", level, xz, color, intensity, Mathf.Min(14f, ceilY - 0f + 9f), false, 6f, 10f);
                    lamp.transform.position += Vector3.up * (ceilY - 3.0f);       // el constructor las pone a 3 m
                    lamps++;
                }
        }

        // ------------------------------------------------------------------ escalera de caracol
        /// <summary>
        /// Escalera de caracol de la planta baja a la primera: peldanos de chapa (decorado), columna central y barandilla; la
        /// colision es una rampa helicoidal invisible (el jugador sube sin tropezar y el NavMesh deja subir a los zombis).
        /// Llega a un rellano orientado hacia el oeste (al centro de las oficinas).
        /// </summary>
        static void SpiralStair()
        {
            var root = new GameObject("EscaleraCaracol").transform; root.SetParent(level);
            float rise = FirstY - GroundY, turn = 400f, end = 180f, start = end - turn;   // vuelta y algo: pendiente suave para que los zombis puedan subir; termina mirando a -X
            int steps = 24;
            float inner = 0.3f;
            // rampa (malla helicoidal)
            int seg = 60;
            var v = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float k = i / (float)seg, a = (start + turn * k) * Mathf.Deg2Rad, y = GroundY + rise * k;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(StairC + dir * inner + Vector3.up * y);
                v.Add(StairC + dir * StairR + Vector3.up * y);
                if (i > 0) { int b = v.Count - 4; tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }); }
            }
            var mesh = new Mesh { name = "RampaCaracol" }; mesh.SetVertices(v); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = "Assets/_Project/Art/Props/RampaCaracol.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (old == null) AssetDatabase.CreateAsset(mesh, mp); else { old.Clear(); old.SetVertices(v); old.SetTriangles(tris, 0); old.RecalculateNormals(); old.RecalculateBounds(); EditorUtility.SetDirty(old); mesh = old; }
            var ramp = new GameObject("Rampa"); ramp.transform.SetParent(root);
            ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
            // peldanos, columna y barandilla
            for (int i = 0; i < steps; i++)
            {
                float k = (i + 0.5f) / steps, a = start + turn * k, y = GroundY + rise * (i + 1) / steps;
                var dir = Quaternion.Euler(0, -a, 0) * Vector3.right;
                var c = StairC + dir * ((inner + StairR) / 2f) + Vector3.up * (y - 0.03f);
                var step = Box("Peldano", root, c, new Vector3(StairR - inner, 0.05f, 0.34f), metal, 0f, false);
                step.transform.rotation = Quaternion.Euler(0, -a, 0);
                var post = Box("Balaustre", root, StairC + dir * (StairR - 0.05f) + Vector3.up * (y + 0.45f), new Vector3(0.035f, 0.9f, 0.035f), metal, 0f, false);
            }
            Box("Columna", root, StairC + Vector3.up * (rise / 2f + 0.5f), new Vector3(0.24f, rise + 1f, 0.24f), metal, 0f);
            for (int i = 0; i < steps; i++)                       // pasamanos: tramos entre balaustres
            {
                float a0 = start + turn * (i + 0.5f) / steps, a1 = start + turn * Mathf.Min(steps - 0.5f, i + 1.5f) / steps;
                float y0 = GroundY + rise * (i + 1) / steps + 0.9f, y1 = GroundY + rise * Mathf.Min(steps, i + 2) / steps + 0.9f;
                var p0 = StairC + Quaternion.Euler(0, -a0, 0) * Vector3.right * (StairR - 0.05f) + Vector3.up * y0;
                var p1 = StairC + Quaternion.Euler(0, -a1, 0) * Vector3.right * (StairR - 0.05f) + Vector3.up * y1;
                if (i == steps - 1) break;
                var h = Box("Pasamanos", root, (p0 + p1) / 2f, new Vector3(0.05f, 0.05f, Vector3.Distance(p0, p1) + 0.02f), wood, 0f, false);
                h.transform.rotation = Quaternion.LookRotation(p1 - p0);
            }
            // rellano de llegada (hacia -X) y barandilla alrededor del hueco en la primera planta
            var endDir = Quaternion.Euler(0, -end, 0) * Vector3.right;
            // al final la rampa avanza hacia -Z (tangente a 180 grados): el rellano va a continuacion, no encima de su ultimo tramo
            Box("Rellano", root, new Vector3(StairHole.xMin + 1.1f, FirstY - 0.15f, StairC.z - 1.0f), new Vector3(2.2f, 0.3f, 2.0f), floorM, 4f);
            float yr = FirstY + 0.5f;
            Box("Barandilla_N", root, new Vector3(StairHole.center.x, yr, StairHole.yMax), new Vector3(StairHole.width, 1.0f, 0.06f), metal, 0f);
            Box("Barandilla_S", root, new Vector3(StairHole.center.x, yr, StairHole.yMin), new Vector3(StairHole.width, 1.0f, 0.06f), metal, 0f);
            Box("Barandilla_E", root, new Vector3(StairHole.xMax, yr, StairHole.center.y), new Vector3(0.06f, 1.0f, StairHole.height), metal, 0f);
            // al oeste queda la salida del rellano: barandilla solo en los tramos que no dan a el
            float gap0 = StairC.z - 2.0f, gap1 = StairC.z;
            if (gap0 - StairHole.yMin > 0.1f) Box("Barandilla_O1", root, new Vector3(StairHole.xMin, yr, (StairHole.yMin + gap0) / 2f), new Vector3(0.06f, 1.0f, gap0 - StairHole.yMin), metal, 0f);
            if (StairHole.yMax - gap1 > 0.1f) Box("Barandilla_O2", root, new Vector3(StairHole.xMin, yr, (gap1 + StairHole.yMax) / 2f), new Vector3(0.06f, 1.0f, StairHole.yMax - gap1), metal, 0f);
        }

        // ------------------------------------------------------------------ ascensor (hueco)
        static void ElevatorShaft()
        {
            var r = Shaft;
            float yb = BasementY - 0.3f, yt = ArchiveCeil + 0.5f;
            Wall("Ascensor_N", new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), yb, yt);
            Wall("Ascensor_S", new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), yb, yt);
            Wall("Ascensor_E", new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), yb, yt);
            // cara oeste: una puerta por planta (de momento cerradas: puertas de chapa fijas)
            float[] floors = { BasementY, GroundY, FirstY, ArchiveY };
            float z = r.center.y;
            for (int i = 0; i < floors.Length; i++)
            {
                float y0 = floors[i], y1 = i + 1 < floors.Length ? floors[i + 1] - 0.3f : yt;
                if (i == 0) y0 = yb;
                Wall("Ascensor_O" + i, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), floors[i], y1, 0.25f, new Vector2(z, 1.4f));
                if (i == 0) Box("Ascensor_O_Base", level, new Vector3(r.xMin, (yb + floors[0]) / 2f, z), new Vector3(0.25f, floors[0] - yb, r.height), wall, 3f);
                Box("PuertaAscensor_" + i, level, new Vector3(r.xMin - 0.02f, floors[i] + 1.2f, z), new Vector3(0.06f, 2.4f, 1.4f), metal, 1f);
            }
            Box("Ascensor_Fondo", level, new Vector3(r.center.x, yb - 0.15f, r.center.y), new Vector3(r.width, 0.3f, r.height), metal, 1f);
        }

        // ------------------------------------------------------------------ plantas
        static void GroundFloor()
        {
            float y0 = GroundY, y1 = GroundCeil, w = FirstY - 0.3f;   // paredes hasta la losa de arriba
            // exterior (fachada con la puerta principal doble en el centro del lado sur)
            Wall("Fachada_S", new Vector2(X0, Z0), new Vector2(X1, Z0), y0, w, 0.35f, new Vector2(0f, 3.0f));
            Wall("Fachada_N", new Vector2(X0, Z1), new Vector2(X1, Z1), y0, w, 0.35f);
            Wall("Fachada_O", new Vector2(X0, Z0), new Vector2(X0, Z1), y0, w, 0.35f);
            Wall("Fachada_E", new Vector2(X1, Z0), new Vector2(X1, Z1), y0, w, 0.35f);
            var main = Door("Puerta_Principal_O", new Vector3(-0.75f, y0, Z0), true);
            var main2 = Door("Puerta_Principal_E", new Vector3(0.75f, y0, Z0), true);
            main2.transform.SetPositionAndRotation(new Vector3(1.5f, y0, Z0), Quaternion.Euler(0, 180f, 0));   // hoja gemela abriendo hacia el otro lado
            var d1 = main.GetComponentInChildren<Door>(); var d2 = main2.GetComponentInChildren<Door>(); d1.partner = d2; d2.partner = d1;
            // vestibulo: x -8..8, z 0..16; puertas a izquierda y derecha y al fondo
            Wall("Vestibulo_O", new Vector2(-8, Z0), new Vector2(-8, 16), y0, w, 0.25f, new Vector2(6f, 1.6f));
            Wall("Vestibulo_E", new Vector2(8, Z0), new Vector2(8, 16), y0, w, 0.25f, new Vector2(6f, 1.6f));
            Wall("Vestibulo_N", new Vector2(-8, 16), new Vector2(8, 16), y0, w, 0.25f, new Vector2(-4f, 1.6f));
            Door("Puerta_Vestibulo_O", new Vector3(-8, y0, 6f), false);
            Door("Puerta_Vestibulo_E", new Vector3(8, y0, 6f), false);
            Door("Puerta_Vestibulo_N", new Vector3(-4f, y0, 16), true);
            // ala oeste: sala de espera (z 0-10) y sala segura (z 10-16)
            Wall("Oeste_Div", new Vector2(X0, 10), new Vector2(-8, 10), y0, w, 0.25f, new Vector2(-14f, 1.6f));
            Door("Puerta_SalaSegura", new Vector3(-14f, y0, 10), true);
            // ala este: oficina (z 0-8) y vestibulo del ascensor (z 8-16)
            Wall("Este_Div", new Vector2(8, 8), new Vector2(X1, 8), y0, w, 0.25f, new Vector2(11f, 1.6f));
            Door("Puerta_Oficina_E", new Vector3(11f, y0, 8), true);
            // fondo: vestuarios (oeste) y calabozos/interrogatorios (este)
            Wall("Fondo_O", new Vector2(X0, 16), new Vector2(-8, 16), y0, w);
            Wall("Fondo_E", new Vector2(8, 16), new Vector2(X1, 16), y0, w, 0.25f, new Vector2(12f, 1.6f));
            Door("Puerta_Interrogatorio", new Vector3(12f, y0, 16), true);
            Wall("Fondo_Div", new Vector2(0, 16), new Vector2(0, Z1), y0, w, 0.25f, new Vector2(24f, 1.6f));
            Door("Puerta_Fondo", new Vector3(0, y0, 24f), false);
            Lamps(Rect.MinMaxRect(-8, 0, 8, 16), y1, 7.2f, new Color(1f, 0.92f, 0.8f), 38f);
            Lamps(Rect.MinMaxRect(X0, 0, -8, 10), y1, 7.2f, new Color(1f, 0.9f, 0.75f));
            Lamps(Rect.MinMaxRect(X0, 10, -8, 16), y1, 7.2f, new Color(1f, 0.85f, 0.65f));
            Lamps(Rect.MinMaxRect(8, 0, X1, 8), y1, 7.2f, new Color(0.85f, 0.95f, 1f));
            Lamps(Rect.MinMaxRect(8, 8, X1, 16), y1, 7.2f, new Color(0.85f, 0.95f, 1f));
            Lamps(Rect.MinMaxRect(X0, 16, 0, Z1), y1, 8.7f, new Color(0.8f, 0.9f, 1f));
            Lamps(Rect.MinMaxRect(0, 16, X1, Z1), y1, 8.7f, new Color(0.8f, 0.9f, 1f));
        }

        static void FirstFloor()
        {
            float y0 = FirstY, y1 = FirstCeil, w = ArchiveY - 0.3f;
            Wall("P1_Fachada_S", new Vector2(X0, Z0), new Vector2(X1, Z0), y0, w, 0.35f);
            Wall("P1_Fachada_N", new Vector2(X0, Z1), new Vector2(X1, Z1), y0, w, 0.35f);
            Wall("P1_Fachada_O", new Vector2(X0, Z0), new Vector2(X0, Z1), y0, w, 0.35f);
            Wall("P1_Fachada_E", new Vector2(X1, Z0), new Vector2(X1, Z1), y0, w, 0.35f);
            // oficinas en el centro (x -8..8, z 0..16): despacho del comisario al oeste, sala de conferencias al este
            Wall("P1_Oficinas_O", new Vector2(-8, Z0), new Vector2(-8, 16), y0, w, 0.25f, new Vector2(8f, 1.6f));
            Wall("P1_Oficinas_E", new Vector2(8, Z0), new Vector2(8, 16), y0, w, 0.25f, new Vector2(6f, 1.6f), new Vector2(14f, 1.6f));
            Wall("P1_Oficinas_N", new Vector2(-8, 16), new Vector2(8, 16), y0, w, 0.25f, new Vector2(0f, 1.6f));
            Door("Puerta_Comisario", new Vector3(-8, y0, 8f), false);
            Door("Puerta_Conferencias", new Vector3(8, y0, 6f), false);
            Door("Puerta_P1_Ascensor", new Vector3(8, y0, 14f), false);
            Door("Puerta_P1_Fondo", new Vector3(0f, y0, 16), true);
            // sala de conferencias (x 8..20, z 0..12) y vestibulo del ascensor (z 12..16)
            Wall("P1_Conf_N", new Vector2(8, 12), new Vector2(Shaft.xMin, 12), y0, w);
            Wall("P1_Fondo", new Vector2(X0, 16), new Vector2(-8, 16), y0, w);
            Wall("P1_Fondo_E", new Vector2(8, 16), new Vector2(X1, 16), y0, w);
            Lamps(Rect.MinMaxRect(-8, 0, 8, 16), y1, 6.5f, new Color(0.9f, 0.95f, 1f));
            Lamps(Rect.MinMaxRect(X0, 0, -8, 16), y1, 7.2f, new Color(1f, 0.88f, 0.7f));
            Lamps(Rect.MinMaxRect(8, 0, X1, 12), y1, 6.5f, new Color(1f, 0.95f, 0.85f));
            Lamps(Rect.MinMaxRect(8, 12, Shaft.xMin, 16), y1, 5.8f, new Color(0.85f, 0.95f, 1f));
            Lamps(Rect.MinMaxRect(X0, 16, X1, Z1), y1, 8.7f, new Color(0.8f, 0.9f, 1f));
        }

        static void ArchiveFloor()
        {
            float y0 = ArchiveY, y1 = ArchiveCeil, w = ArchiveCeil + 0.5f;
            Wall("Archivo_S", new Vector2(X0, Z0), new Vector2(X1, Z0), y0, w, 0.35f);
            Wall("Archivo_N", new Vector2(X0, Z1), new Vector2(X1, Z1), y0, w, 0.35f);
            Wall("Archivo_O", new Vector2(X0, Z0), new Vector2(X0, Z1), y0, w, 0.35f);
            Wall("Archivo_E", new Vector2(X1, Z0), new Vector2(X1, Z1), y0, w, 0.35f);
            // vestibulo del ascensor (x 14..20, z 10..18) separado de la nave del archivo
            Wall("Archivo_Vest_O", new Vector2(14, 10), new Vector2(14, 18), y0, y0 + 3.2f, 0.25f, new Vector2(14f, 1.6f));
            Wall("Archivo_Vest_S", new Vector2(14, 10), new Vector2(X1, 10), y0, y0 + 3.2f);
            Wall("Archivo_Vest_N", new Vector2(14, 18), new Vector2(X1, 18), y0, y0 + 3.2f);
            Box("Archivo_Vest_Techo", level, new Vector3(17f, y0 + 3.25f, 14f), new Vector3(6f, 0.1f, 8f), ceil, 2.4f);
            Door("Puerta_Archivo", new Vector3(14, y0, 14f), false);
            Lamps(Rect.MinMaxRect(X0, Z0, 14, Z1), y1, 10.2f, new Color(0.8f, 0.92f, 1f), 30f);
        }

        static void Basement()
        {
            float y0 = BasementY, y1 = BasementCeil, w = GroundY - 0.3f;
            Wall("Sotano_S", new Vector2(X0, Z0), new Vector2(X1, Z0), y0, w, 0.35f);
            Wall("Sotano_N", new Vector2(X0, Z1), new Vector2(X1, Z1), y0, w, 0.35f);
            Wall("Sotano_O", new Vector2(X0, Z0), new Vector2(X0, Z1), y0, w, 0.35f);
            Wall("Sotano_E", new Vector2(X1, Z0), new Vector2(X1, Z1), y0, w, 0.35f);
            // pasillo central (z 12..16) desde el ascensor hacia el oeste; salas al sur y al norte; sala de calderas al noroeste
            Wall("Sotano_Pasillo_S", new Vector2(-6, 12), new Vector2(Shaft.xMin, 12), y0, w, 0.25f, new Vector2(-2f, 1.6f), new Vector2(8f, 1.6f));
            Wall("Sotano_Pasillo_N", new Vector2(-6, 16), new Vector2(Shaft.xMin, 16), y0, w, 0.25f, new Vector2(-2f, 1.6f), new Vector2(8f, 1.6f));
            Wall("Sotano_Pasillo_O", new Vector2(-6, 12), new Vector2(-6, 16), y0, w, 0.25f, new Vector2(14f, 1.6f));
            Wall("Sotano_Sur_Div", new Vector2(3, Z0), new Vector2(3, 12), y0, w);
            Wall("Sotano_Norte_Div", new Vector2(3, 16), new Vector2(3, Z1), y0, w);
            Wall("Sotano_Calderas_E", new Vector2(-6, 16), new Vector2(-6, Z1), y0, w);
            Wall("Sotano_Calderas_S", new Vector2(X0, 12), new Vector2(-6, 12), y0, w);
            Door("Puerta_S1", new Vector3(-2f, y0, 12), true); Door("Puerta_S2", new Vector3(8f, y0, 12), true);
            Door("Puerta_N1", new Vector3(-2f, y0, 16), true); Door("Puerta_N2", new Vector3(8f, y0, 16), true);
            Door("Puerta_Calderas", new Vector3(-6, y0, 14f), false);
            Lamps(Rect.MinMaxRect(-6, 12, Shaft.xMin, 16), y1, 5.8f, new Color(0.8f, 1f, 0.85f), 26f);
            Lamps(Rect.MinMaxRect(-6, Z0, X1, 12), y1, 7.2f, new Color(0.8f, 1f, 0.85f), 26f);
            Lamps(Rect.MinMaxRect(-6, 16, X1, Z1), y1, 7.2f, new Color(0.8f, 1f, 0.85f), 26f);
            Lamps(Rect.MinMaxRect(X0, 12, -6, Z1), y1, 7.2f, new Color(1f, 0.6f, 0.4f), 30f);
            Lamps(Rect.MinMaxRect(X0, Z0, -6, 12), y1, 7.2f, new Color(0.8f, 1f, 0.85f), 26f);
        }

        // ------------------------------------------------------------------ entrada
        [MenuItem("Horror/Comisaria v2/1 Estructura (escena nueva)")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            EditorSceneManager.SaveOpenScenes();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                AssetDatabase.CopyAsset("Assets/Scenes/Comisaria.unity", ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // fuera el nivel viejo (o el de una pasada anterior): se quedan jugador, HUD, audio, GameFlow, camara y luces globales
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "--- LEVEL ---" || go.name == "Details" || go.name == "Items" || go.name == "Zombies" || go.name == "ArenaBlockers" || go.name == "--- COMISARIA V2 ---")
                    Object.DestroyImmediate(go);

            wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            floorM = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Floor.mat");
            ceil = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Ceiling.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            column = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Column.mat");
            leaves.Clear(); lamps = 0;

            var rootGo = new GameObject("--- COMISARIA V2 ---");
            level = rootGo.transform;
            details = new GameObject("Details").transform; details.SetParent(level);
            var zombies = new GameObject("Zombies"); zombies.transform.SetParent(level);
            var items = new GameObject("Items"); items.transform.SetParent(level);
            var props = new GameObject("Props"); props.transform.SetParent(level);
            var nw = props.AddComponent<NavMeshModifier>(); nw.overrideArea = true; nw.area = 1; nw.applyToChildren = true;   // los muebles no son suelo

            var area = Rect.MinMaxRect(X0, Z0, X1, Z1);
            // losas: sotano, baja, primera, archivo y cubierta (huecos: escalera entre baja y primera; ascensor en todas)
            Storey("Sotano", BasementY, BasementCeil, GroundY, area, new[] { Shaft }, new[] { Shaft });
            Storey("Baja", GroundY, GroundCeil, FirstY, area, new[] { Shaft }, new[] { Shaft, StairHole });
            Storey("Primera", FirstY, FirstCeil, ArchiveY, area, new[] { Shaft, StairHole }, new[] { Shaft });
            Storey("Archivo", ArchiveY, ArchiveCeil, ArchiveCeil + 0.6f, area, new[] { Shaft }, new[] { Shaft });
            Box("Cubierta", level, new Vector3(0, ArchiveCeil + 0.4f, (Z0 + Z1) / 2f), new Vector3(X1 - X0 + 1f, 0.3f, Z1 - Z0 + 1f), wall, 3f);
            // calle delante de la fachada (la exterior de verdad es la fase 2)
            Box("Calle", level, new Vector3(0, -0.15f, -15f), new Vector3(80f, 0.3f, 30f), floorM, 4f);

            GroundFloor(); FirstFloor(); ArchiveFloor(); Basement();
            SpiralStair(); ElevatorShaft();

            // NavMesh: superficie con las colisiones del nivel (se rehace al arrancar; las hojas de las puertas fuera)
            var surface = rootGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            foreach (var l in leaves) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in leaves) l.SetActive(true);
            var rt = rootGo.AddComponent<RuntimeNavMesh>(); rt.surface = surface; rt.disableDuringBake = leaves.ToArray();

            // jugador en la entrada, mirando al vestibulo
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                pc.transform.SetPositionAndRotation(new Vector3(0f, GroundY + 1.05f, 1.6f), Quaternion.identity);
                var cam = pc.cam; if (cam != null) cam.SetYaw(0f);
            }
            var flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null) { flow.startObjective = "Entra en la comisaría y busca la forma de llegar al archivo."; EditorUtility.SetDirty(flow); }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Comisaria_v2: 4 plantas, " + leaves.Count + " puertas, " + lamps + " lamparas, escalera de caracol y hueco de ascensor";
        }
    }
}
#endif
