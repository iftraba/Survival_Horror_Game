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
    /// Comisaria grande (2026-10-09, rama rediseno-comisaria): reconstruye el nivel de Comisaria_v2 con una planta de 64 x 44 m
    /// hecha de pasillos y salas pequenas, como pidio el usuario (antes eran naves grandes y casi vacias).
    /// La planta se describe con DATOS (lista de salas por piso, puertas y huecos) y el constructor saca solo las paredes
    /// (en el borde entre dos espacios distintos), los huecos de paso con su dintel, rodapies, suelos, techos y lamparas.
    /// Pisos: sotano (-4,5), baja (0), primera (4), segunda (7,5: archivo, antesala y la azotea al aire libre).
    /// Fuera, al oeste, un callejon con la escalera de incendios que sube al balcon de la primera planta y a la azotea.
    /// Menu: Horror/Comisaria grande/1 Estructura. Repetible: rehace la escena desde la comisaria original.
    /// </summary>
    public static class ComisariaGrande
    {
        public const string ScenePath = "Assets/Scenes/Comisaria_v2.unity";
        const string Mats = "Assets/_Project/Materials/";
        const string Pre = "Assets/_Project/Prefabs/";
        public const float B = -4.5f, G = 0f, F1 = 4f, F2 = 7.5f;
        public const float X0 = -32f, X1 = 32f, Z0 = 0f, Z1 = 44f;
        const float DoorH = 2.45f, Grid = 0.5f;

        // ------------------------------------------------------------------ datos
        public enum Kind { Room, Corridor, Outdoor, Shaft }
        public class Room
        {
            public string id, label, type, group;
            public Rect r; public float floor, ceil; public Kind kind = Kind.Room;
            public Color light = new Color(1f, 0.93f, 0.82f);
            public bool Indoor => kind == Kind.Room || kind == Kind.Corridor;
        }
        public enum DoorKind { Wood, Double, Opening, Padlock, Card, ChiefCard, Power, Metal, Fixed, Roll }
        public class DoorDef { public string name; public Vector2 p; public DoorKind kind; public float width = 1.6f; public float floor; }

        public static readonly List<Room> Rooms = new List<Room>();
        public static readonly List<DoorDef> Doors = new List<DoorDef>();
        /// <summary>Huecos en las losas (escaleras): rect y la cota de la losa que atraviesan (techo del piso de abajo / suelo del de arriba).</summary>
        static readonly List<(Rect r, float y)> SlabHoles = new List<(Rect, float)>();

        public static readonly Vector3 SpiralC = new Vector3(4.0f, 0f, 6.5f);
        public const float SpiralR = 2.2f;
        public static Rect SpiralHole => new Rect(SpiralC.x - 2.5f, SpiralC.z - 2.5f, 5f, 5f);
        public static readonly Rect Shaft = Rect.MinMaxRect(5f, 23f, 8f, 26.5f);
        /// <summary>Hueco de doble altura de la galeria de la primera planta sobre el atrio (x -1..5, z 14,5..24,5).</summary>
        public static readonly Rect GalleryHole = Rect.MinMaxRect(-1f, 14.5f, 5f, 24.5f);
        /// <summary>Escalera de la sala de espera al garaje (a lo largo de X, pegada a la pared sur; baja hacia el oeste).</summary>
        public static readonly Rect GarageStairHole = Rect.MinMaxRect(-23f, 0.2f, -14.5f, 2.2f);
        /// <summary>Tramo de la escalera norte que baja del hall de ingreso a la custodia (entra por el sur, baja hacia el norte).</summary>
        public static readonly Rect CustodyStairHole = Rect.MinMaxRect(7f, 32f, 9f, 40f);

        static Room R(string id, string label, string type, float x0, float z0, float x1, float z1, float floor, float ceil, Kind k = Kind.Room, string group = null)
        {
            var room = new Room { id = id, label = label, type = type, r = Rect.MinMaxRect(x0, z0, x1, z1), floor = floor, ceil = ceil, kind = k, group = group ?? id };
            if (k == Kind.Corridor) room.light = new Color(0.86f, 0.93f, 1f);
            Rooms.Add(room);
            return room;
        }
        static void D(string name, float x, float z, float floor, DoorKind k = DoorKind.Wood, float w = 1.6f) =>
            Doors.Add(new DoorDef { name = name, p = new Vector2(x, z), floor = floor, kind = k, width = w });

        public static void Define()
        {
            Rooms.Clear(); Doors.Clear(); SlabHoles.Clear();
            float gC = 3.5f, fC = 7.0f, bC = -1.0f;
            // Version 3 del esquema (docs/planes/rediseno-planta.md, 2026-10-10): garaje y calabozos en el sotano, atrio de doble altura,
            // galeria sobre el atrio, halls en lugar de pasillos de 2,5 m. Las salas del mismo 'group' no llevan pared entre si.

            // ===== SOTANO (y -4,5): garaje y calabozos aparte de la zona industrial =====
            R("B_Garage", "Garaje", "garage", -32, 0, -8, 26.5f, B, bC);
            R("B_Pump", "Bombas y depositos", "pumps", -8, 0, 10, 10, B, bC);
            R("B_Mach", "Maquinas y taller", "machines", 10, 0, 24, 10, B, bC);
            R("B_Store", "Almacen del sotano", "storage", 24, 0, 32, 10, B, bC);
            R("B_Gal", "Galeria de servicio", "corridor", -8, 10, 32, 14, B, bC, Kind.Corridor);
            R("B_Safe", "Sala segura del sotano", "safe", -8, 14, 0, 26.5f, B, bC).light = new Color(1f, 0.82f, 0.6f);
            R("B_Hub", "Nudo del ascensor", "elevator", 0, 14, 8, 23, B, bC, Kind.Room, "B_Hub");
            R("B_HubB", "Nudo del ascensor", "elevator", 0, 23, 5, 26.5f, B, bC, Kind.Room, "B_Hub");
            R("B_Shaft", "Ascensor", "shaft", Shaft.xMin, Shaft.yMin, Shaft.xMax, Shaft.yMax, B, bC, Kind.Shaft);
            R("B_Lab", "Laboratorio", "lab", 8, 14, 20, 26.5f, B, bC);
            R("B_Fuse", "Cuadro electrico", "fuse", 20, 14, 32, 26.5f, B, bC);
            R("B_CN", "Galeria norte", "corridor", -12, 26.5f, 32, 29, B, bC, Kind.Corridor);
            R("B_CW", "Pasillo de las calderas", "corridor", -32, 26.5f, -12, 29, B, bC, Kind.Corridor);
            R("B_Boiler", "Sala de calderas", "boiler", -32, 29, -12, 44, -6.5f, bC).light = new Color(1f, 0.55f, 0.4f);
            R("B_Control", "Sala de control", "control", -12, 29, 2, 44, B, bC);
            R("B_Custody", "Custodia", "custody", 2, 29, 10, 44, B, bC);
            R("B_Cells", "Calabozos", "cells", 10, 29, 32, 44, B, bC);

            D("Puerta_Garaje_Rampa", -32, 2, B, DoorKind.Roll, 4f);              // puerta enrollable: cerrada, da a la explanada de la rampa
            D("Puerta_Garaje_Bombas", -8, 5, B);                                 // paso de un solo sentido (OneWayDoor en la etapa E): se abre desde las bombas
            D("Puerta_SeguraS", 0, 20, B);
            D("Puerta_AscensorS", 4, 14, B);
            D("Puerta_Hub_Norte", 2.5f, 26.5f, B);
            D("Puerta_Laboratorio", 14, 14, B);
            D("Puerta_Cuadro", 26, 14, B);
            D("Puerta_Bombas", 1, 10, B, DoorKind.Wood, 2f);
            D("Puerta_Maquinas", 17, 10, B);
            D("Puerta_AlmacenS", 28, 10, B);
            D("Puerta_Control", -5, 29, B);
            D("Puerta_Sin_Corriente", -12, 27.75f, B, DoorKind.Power);
            D("Puerta_Calderas", -22, 29, B, DoorKind.Metal);
            D("Puerta_Calabozos", 10, 42, B, DoorKind.ChiefCard);
            D("Ascensor_Sotano", Shaft.xMin, 24.75f, B, DoorKind.Fixed, 1.4f);

            // ===== PLANTA BAJA (y 0) =====
            R("G_Safe", "Sala segura", "safe", -32, 0, -25, 12, G, gC).light = new Color(1f, 0.82f, 0.6f);
            R("G_Wait", "Espera y atencion", "waiting", -25, 0, -8, 12, G, gC);
            R("G_Lobby", "Vestibulo", "lobby", -8, 0, 8, 12, G, gC);
            R("G_Brief", "Sala de agentes", "office", 8, 0, 21, 12, G, gC);
            R("G_Armory", "Armeria", "armory", 21, 0, 32, 12, G, gC);
            R("G_Radio", "Radio y despachos", "radio", -32, 12, -18, 29, G, gC);
            R("G_Dark", "Sala de pruebas", "darkroom", -18, 12, -8, 29, G, gC);
            R("G_WC", "Aseos", "restroom", -8, 12, -2, 19, G, gC);
            // atrio central de doble altura (hueco de la galeria en la primera planta): varias piezas del mismo grupo alrededor del aseo y del ascensor
            R("G_Atrio1", "Atrio central", "atrium", -2, 12, 8, 19, G, gC, Kind.Room, "G_Atrio");
            R("G_Atrio2", "Atrio central", "atrium", -8, 19, 5, 29, G, gC, Kind.Room, "G_Atrio");
            R("G_Atrio3", "Atrio central", "atrium", 5, 19, 8, 23, G, gC, Kind.Room, "G_Atrio");
            R("G_Atrio4", "Atrio central", "atrium", 5, 26.5f, 8, 29, G, gC, Kind.Room, "G_Atrio");
            R("G_Shaft", "Ascensor", "shaft", Shaft.xMin, Shaft.yMin, Shaft.xMax, Shaft.yMax, G, gC, Kind.Shaft);
            R("G_Sec", "Oficina de seguridad", "security", 8, 12, 20, 29, G, gC);
            R("G_Break", "Sala de descanso", "breakroom", 20, 12, 32, 29, G, gC);
            R("G_Lock", "Vestuarios", "lockers", -32, 29, -20, 44, G, gC);
            R("G_Work", "Taller y almacen", "workshop", -20, 29, -6, 44, G, gC);
            R("G_Intake", "Ingreso", "intake", -6, 29, 2, 44, G, gC);
            R("G_Stair2", "Escalera norte", "stairwell", 2, 29, 10, 44, G, gC);
            R("G_Interr", "Interrogatorios y observacion", "interrogation", 10, 29, 32, 44, G, gC);
            R("G_Alley", "Callejon", "alley", -37, 8, -32, 44, G, 0f, Kind.Outdoor);

            D("Puerta_Principal", 0, 0, G, DoorKind.Double, 3f);
            D("Puerta_Espera_Vestibulo", -8, 6, G, DoorKind.Opening, 3f);
            D("Puerta_Segura", -25, 6, G);
            D("Puerta_Vestibulo_E", 8, 6, G, DoorKind.Padlock);
            D("Puerta_Agentes_Armeria", 21, 6, G);
            D("Puerta_Vestibulo_Atrio", 1, 12, G, DoorKind.Opening, 6f);
            D("Puerta_Espera_Radio", -21, 12, G);
            D("Puerta_Radio_Pruebas", -18, 22, G);
            D("Puerta_Pruebas", -8, 22, G);
            D("Puerta_Aseos", -2, 15.5f, G);
            D("Puerta_Pasillo_Candado", 8, 17, G, DoorKind.Padlock);
            D("Puerta_Agentes_Seguridad", 14, 12, G);
            D("Puerta_Armeria_Descanso", 26, 12, G);
            D("Puerta_Descanso", 20, 20, G);
            D("Puerta_Tarjeta", -2, 29, G, DoorKind.Card);
            D("Puerta_Ingreso_Taller", -6, 36, G);
            D("Puerta_Vestuarios", -20, 36, G);
            D("Puerta_Escalera_Norte", 2, 35, G, DoorKind.Card);
            D("Puerta_Interrogatorios", 10, 42, G);                              // al norte del tramo que baja a la custodia (entre ambos queda 1 m)
            D("Puerta_Callejon", -32, 40, G, DoorKind.Padlock);
            D("Ascensor_Baja", Shaft.xMin, 24.75f, G, DoorKind.Fixed, 1.4f);

            // ===== PRIMERA PLANTA (y 4) =====
            R("F_Comm", "Despacho del comisario", "commissioner", -32, 0, -20, 12, F1, fC).light = new Color(1f, 0.85f, 0.65f);
            R("F_Conf", "Sala de conferencias", "conference", -20, 0, -8, 12, F1, fC);
            R("F_Mem", "Memorial", "memorial", -8, 0, 8, 12, F1, fC);
            R("F_Det", "Detectives", "offices", 8, 0, 32, 12, F1, fC);
            R("F_Chief", "Despacho del jefe de seguridad", "chief", -32, 12, -20, 29, F1, fC);
            R("F_Lib", "Biblioteca", "library", -20, 12, -8, 29, F1, fC);
            // galeria sobre el atrio (el hueco de doble altura va como SlabHoles)
            R("F_GalA", "Galeria", "gallery", -8, 12, 8, 23, F1, fC, Kind.Room, "F_Gal");
            R("F_GalB", "Galeria", "gallery", -8, 23, 5, 29, F1, fC, Kind.Room, "F_Gal");
            R("F_GalC", "Galeria", "gallery", 5, 26.5f, 8, 29, F1, fC, Kind.Room, "F_Gal");
            R("F_Shaft", "Ascensor", "shaft", Shaft.xMin, Shaft.yMin, Shaft.xMax, Shaft.yMax, F1, fC, Kind.Shaft);
            R("F_Canteen", "Comedor y descanso", "breakroom", 8, 12, 32, 29, F1, fC);
            R("F_Admin", "Oficinas y archivo auxiliar", "offices", -32, 29, -14, 44, F1, fC);
            R("F_Serv", "Escalera del archivo", "servicestair", -14, 29, -4, 44, F1, fC);
            R("F_NHall", "Hall norte", "hall", -4, 29, 10, 44, F1, fC);
            R("F_Rec", "Registro", "records", 10, 29, 22, 44, F1, fC);
            R("F_Lounge", "Sala del sindicato", "lounge", 22, 29, 32, 44, F1, fC);
            R("F_Balcony", "Escalera de incendios", "fireescape", -36, 17, -32, 26, F1, 0f, Kind.Outdoor, "F_Balcony");

            D("Puerta_Comisario", -20, 6, F1);
            D("Puerta_Conf_Memorial", -8, 6, F1, DoorKind.Opening, 3f);
            D("Puerta_Memorial_Galeria", 1, 12, F1, DoorKind.Opening, 6f);
            D("Puerta_Memorial_Detectives", 8, 6, F1);
            D("Puerta_Detectives_Comedor", 14, 12, F1);
            D("Puerta_Detectives_Comedor2", 28, 12, F1);
            D("Puerta_Biblioteca", -14, 12, F1);
            D("Puerta_Biblioteca_Galeria", -8, 20, F1);
            D("Puerta_Galeria_Comedor", 8, 18, F1);
            D("Puerta_JefeSeguridad_Balcon", -32, 20, F1);
            D("Puerta_Galeria_HallNorte", 0, 29, F1);                           // paso de un solo sentido (OneWayDoor en la etapa E): se abre desde el hall norte
            D("Puerta_Admin_Biblioteca", -17, 29, F1);
            D("Puerta_Admin_Archivo", -14, 42, F1);
            D("Puerta_Escalera_Archivo", -4, 34, F1);
            D("Puerta_Hall_Registro", 10, 36, F1);
            D("Puerta_Registro", 16, 29, F1);
            D("Puerta_Sindicato", 27, 29, F1);
            D("Puerta_Registro_Sindicato", 22, 36, F1);
            D("Ascensor_Primera", Shaft.xMin, 24.75f, F1, DoorKind.Fixed, 1.4f);

            // ===== SEGUNDA PLANTA (y 7,5): antesala y archivo; el resto es azotea =====
            R("S_Ante", "Antesala del archivo", "ante", -14, 29, -4, 44, F2, 11f);
            R("S_Arch", "Archivo", "archive", -4, 14.5f, 32, 44, F2, 14f);
            R("S_Shed", "Caseta de la azotea", "shed", -24, 30, -20, 34, F2, 10f);
            foreach (var (n, a, b, c, d) in new[] { ("S_RoofA", -32f, 0f, -14f, 30f), ("S_RoofB", -32f, 34f, -14f, 44f), ("S_RoofC", -32f, 30f, -24f, 34f), ("S_RoofD", -20f, 30f, -14f, 34f), ("S_RoofE", -14f, 0f, 32f, 14.5f), ("S_RoofF", -14f, 14.5f, -4f, 29f) })
                R(n, "Azotea", "roof", a, b, c, d, F2, 0f, Kind.Outdoor, "S_Roof");
            R("S_Escape", "Escalera de incendios", "fireescape", -36, 6, -32, 11, F2, 0f, Kind.Outdoor, "S_Roof");
            D("Puerta_Archivo", -4, 36, F2);
            D("Puerta_Caseta", -22, 30, F2);
            D("Azotea_Escalera", -32, 8.5f, F2, DoorKind.Opening, 2f);

            // huecos en las losas para las escaleras y la galeria
            SlabHoles.Add((SpiralHole, F1));                                         // caracol del vestibulo
            SlabHoles.Add((Rect.MinMaxRect(4f, 31.6f, 6.4f, 40.0f), F1));           // escalera norte (sube a la primera)
            SlabHoles.Add((Rect.MinMaxRect(-14f, 31.6f, -11.4f, 39.0f), F2));       // escalera del archivo
            SlabHoles.Add((GalleryHole, F1));                                        // galeria sobre el atrio (doble altura)
            SlabHoles.Add((GarageStairHole, G));                                     // escalera de la sala de espera al garaje
            SlabHoles.Add((CustodyStairHole, G));                                    // escalera norte, tramo que baja a la custodia
        }

        // ------------------------------------------------------------------ utilidades
        static Material wall, floorM, ceil, wood, metal, outdoorM, grating;
        static Transform level, details, structure;
        static readonly List<GameObject> leaves = new List<GameObject>();
        static int lamps;

        static T Call<T>(string method, params object[] args) => (T)typeof(TestSceneBuilder).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

        public static GameObject Box(string name, Transform parent, Vector3 c, Vector3 s, Material m, float tile, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = c; go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (tile > 0f) go.GetComponent<MeshFilter>().sharedMesh = Call<Mesh>("TiledUnitCube", c, s, tile);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        static void SlabRect(string name, Rect r, float y0, float y1, IEnumerable<Rect> holes, Material m, float tile)
        {
            var hs = holes.Where(h => h.Overlaps(r)).ToList();
            var xs = new List<float> { r.xMin, r.xMax }; var zs = new List<float> { r.yMin, r.yMax };
            foreach (var h in hs) { xs.Add(Mathf.Clamp(h.xMin, r.xMin, r.xMax)); xs.Add(Mathf.Clamp(h.xMax, r.xMin, r.xMax)); zs.Add(Mathf.Clamp(h.yMin, r.yMin, r.yMax)); zs.Add(Mathf.Clamp(h.yMax, r.yMin, r.yMax)); }
            xs = xs.Distinct().OrderBy(v => v).ToList(); zs = zs.Distinct().OrderBy(v => v).ToList();
            for (int i = 0; i < xs.Count - 1; i++)
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    var cell = Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]);
                    if (cell.width < 0.01f || cell.height < 0.01f || hs.Any(h => h.Contains(cell.center))) continue;
                    Box(name, structure, new Vector3(cell.center.x, (y0 + y1) / 2f, cell.center.y), new Vector3(cell.width, y1 - y0, cell.height), m, tile);
                }
        }

        // ------------------------------------------------------------------ paredes a partir de las salas
        /// <summary>Altura a la que llega la pared de una sala (tapa la losa de encima).</summary>
        static float Top(Room a) => a.kind == Kind.Outdoor ? a.floor + (a.type == "alley" ? 3.2f : a.type == "fireescape" ? 0f : 1.1f) : a.ceil + (a.floor < -0.6f ? 0.75f : 0.5f);

        struct Seg { public bool alongX; public float c, s0, s1, y0, y1, t, floorA, floorB; public bool skirtA, skirtB; }

        /// <summary>Saca las paredes de todos los pisos: borde a borde de la rejilla de 0,5 m entre espacios distintos.</summary>
        static List<Seg> BuildSegments()
        {
            var segs = new List<Seg>();
            float minX = Rooms.Min(r => r.r.xMin) - 1, maxX = Rooms.Max(r => r.r.xMax) + 1, minZ = Rooms.Min(r => r.r.yMin) - 1, maxZ = Rooms.Max(r => r.r.yMax) + 1;
            int nx = Mathf.RoundToInt((maxX - minX) / Grid), nz = Mathf.RoundToInt((maxZ - minZ) / Grid);
            // un "piso" por cota de suelo de referencia: los espacios se agrupan por el piso en el que estan
            var levels = new[] { (B, -7f, -0.6f), (G, -0.6f, 3.9f), (F1, 3.9f, 7.3f), (F2, 7.3f, 20f) };
            foreach (var (lv, lo, hi) in levels)
            {
                var rooms = Rooms.Where(r => r.floor > lo - 0.01f && r.floor < hi).ToList();
                if (lv == B) rooms = Rooms.Where(r => r.floor < -0.6f).ToList();
                var lab = new int[nx, nz];
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < nz; j++)
                    {
                        var p = new Vector2(minX + (i + 0.5f) * Grid, minZ + (j + 0.5f) * Grid);
                        lab[i, j] = rooms.FindIndex(r => r.r.Contains(p));
                    }
                // aristas verticales (pared a lo largo de Z en x = minX + (i+1)*Grid) y horizontales
                for (int pass = 0; pass < 2; pass++)
                {
                    bool alongX = pass == 1;
                    int lines = alongX ? nz - 1 : nx - 1, len = alongX ? nx : nz;
                    for (int L = 0; L < lines; L++)
                    {
                        Seg? cur = null;
                        for (int k = 0; k <= len; k++)
                        {
                            Seg? s = null;
                            if (k < len)
                            {
                                int a = alongX ? lab[k, L] : lab[L, k], b = alongX ? lab[k, L + 1] : lab[L + 1, k];
                                if (a != b)
                                {
                                    Room ra = a >= 0 ? rooms[a] : null, rb = b >= 0 ? rooms[b] : null;
                                    if (!(ra != null && rb != null && ra.group == rb.group) && !(ra == null && rb != null && rb.kind == Kind.Outdoor && rb.type == "fireescape") && !(rb == null && ra != null && ra.kind == Kind.Outdoor && ra.type == "fireescape"))
                                    {
                                        bool outA = ra == null || ra.kind == Kind.Outdoor, outB = rb == null || rb.kind == Kind.Outdoor;
                                        float y0 = Mathf.Min(ra != null ? ra.floor : 99f, rb != null ? rb.floor : 99f) - 0.3f;
                                        float y1 = Mathf.Max(ra != null ? Top(ra) : -99f, rb != null ? Top(rb) : -99f);
                                        if (ra != null && rb != null && ra.kind == Kind.Outdoor && rb.kind == Kind.Outdoor) y1 = Mathf.Max(Top(ra), Top(rb));
                                        if (y1 > y0 + 0.2f)
                                            s = new Seg { alongX = alongX, c = (alongX ? minZ : minX) + (L + 1) * Grid, s0 = (alongX ? minX : minZ) + k * Grid, s1 = (alongX ? minX : minZ) + (k + 1) * Grid, y0 = y0, y1 = y1,
                                                t = (outA || outB) ? 0.35f : 0.2f, floorA = ra != null ? ra.floor : float.NaN, floorB = rb != null ? rb.floor : float.NaN,
                                                skirtA = ra != null && ra.Indoor, skirtB = rb != null && rb.Indoor };
                                    }
                                }
                            }
                            if (cur.HasValue && s.HasValue && Mathf.Approximately(cur.Value.y0, s.Value.y0) && Mathf.Approximately(cur.Value.y1, s.Value.y1) && Mathf.Approximately(cur.Value.t, s.Value.t)
                                && cur.Value.skirtA == s.Value.skirtA && cur.Value.skirtB == s.Value.skirtB && Mathf.Abs(cur.Value.s1 - s.Value.s0) < 0.01f)
                            { var c = cur.Value; c.s1 = s.Value.s1; cur = c; continue; }
                            if (cur.HasValue) segs.Add(cur.Value);
                            cur = s;
                        }
                        if (cur.HasValue) segs.Add(cur.Value);
                    }
                }
            }
            return segs;
        }

        static void EmitWalls(List<Seg> segs)
        {
            foreach (var sg in segs)
            {
                // huecos de las puertas que caen en este tramo
                var gaps = Doors.Where(d => (sg.alongX ? Mathf.Abs(d.p.y - sg.c) < 0.05f && d.p.x > sg.s0 && d.p.x < sg.s1 : Mathf.Abs(d.p.x - sg.c) < 0.05f && d.p.y > sg.s0 && d.p.y < sg.s1)
                                             && d.floor >= sg.y0 - 0.01f && d.floor < sg.y1 - 1f)
                                .OrderBy(d => sg.alongX ? d.p.x : d.p.y).ToList();
                var cuts = new List<(float a, float b, DoorDef d)>();
                float s = sg.s0;
                foreach (var d in gaps)
                {
                    float m = sg.alongX ? d.p.x : d.p.y, g0 = m - d.width / 2f, g1 = m + d.width / 2f;
                    if (g0 > s) cuts.Add((s, g0, null));
                    cuts.Add((g0, g1, d));
                    s = g1;
                }
                if (s < sg.s1) cuts.Add((s, sg.s1, null));
                foreach (var (a, b, d) in cuts)
                {
                    float len = b - a, mid = (a + b) / 2f;
                    if (d == null)
                    {
                        WallBox(sg, mid, len, sg.y0, sg.y1, true);
                    }
                    else
                    {
                        // tramo bajo el hueco (si la puerta esta en el piso de arriba de una pared que viene de abajo) y dintel encima
                        if (d.floor - 0.3f > sg.y0 + 0.05f) WallBox(sg, mid, len, sg.y0, d.floor - 0.3f, false);
                        if (sg.y1 > d.floor + DoorH + 0.02f) WallBox(sg, mid, len, d.floor + DoorH, sg.y1, false);
                    }
                }
            }
        }

        static void WallBox(Seg sg, float mid, float len, float y0, float y1, bool skirting)
        {
            var c = sg.alongX ? new Vector3(mid, (y0 + y1) / 2f, sg.c) : new Vector3(sg.c, (y0 + y1) / 2f, mid);
            var size = sg.alongX ? new Vector3(len, y1 - y0, sg.t) : new Vector3(sg.t, y1 - y0, len);
            Box("Pared", structure, c, size, wall, 3f);
            if (!skirting) return;
            foreach (int side in new[] { -1, 1 })
            {
                bool has = side < 0 ? sg.skirtA : sg.skirtB; float fy = side < 0 ? sg.floorA : sg.floorB;
                if (!has || float.IsNaN(fy) || fy + 0.14f > y1) continue;
                var off = sg.alongX ? new Vector3(0, 0, side * (sg.t / 2 + 0.02f)) : new Vector3(side * (sg.t / 2 + 0.02f), 0, 0);
                var bs = sg.alongX ? new Vector3(len, 0.14f, 0.04f) : new Vector3(0.04f, 0.14f, len);
                Box("Rodapie", details, new Vector3(c.x, fy + 0.07f, c.z) + off, bs, wood, 1f, false);
            }
        }

        // ------------------------------------------------------------------ puertas
        static void EmitDoors()
        {
            var w150 = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Doors/Door_Wood_150.prefab");
            foreach (var d in Doors)
            {
                if (d.kind == DoorKind.Opening) continue;
                // eje de la pared: si hay un tramo de pared a lo largo de X en z = p.y, es una puerta en pared a lo largo de X
                bool alongX = Rooms.Any(r => Mathf.Abs(r.r.yMin - d.p.y) < 0.05f || Mathf.Abs(r.r.yMax - d.p.y) < 0.05f) &&
                              !Rooms.Any(r => (Mathf.Abs(r.r.xMin - d.p.x) < 0.05f || Mathf.Abs(r.r.xMax - d.p.x) < 0.05f) && d.p.y > r.r.yMin && d.p.y < r.r.yMax && Mathf.Abs(r.floor - d.floor) < 0.6f);
                var center = new Vector3(d.p.x, d.floor, d.p.y);
                if (d.kind == DoorKind.Roll)
                {
                    // puerta enrollable de garaje: cerrada y fija (la abre el progreso mas adelante). Persiana con lamas y cajon.
                    var shutter = new GameObject(d.name).transform; shutter.SetParent(structure);
                    Vector3 depth = alongX ? Vector3.forward : Vector3.right;
                    Box("Persiana", shutter, center + Vector3.up * (DoorH / 2f), alongX ? new Vector3(d.width, DoorH, 0.12f) : new Vector3(0.12f, DoorH, d.width), metal, 1f);
                    for (int i = 1; i < 12; i++)
                        Box("Lama", shutter, center + Vector3.up * (DoorH * i / 12f) - depth * 0.08f, alongX ? new Vector3(d.width, 0.04f, 0.03f) : new Vector3(0.03f, 0.04f, d.width), metal, 0f, false);
                    Box("Cajon", shutter, center + Vector3.up * (DoorH + 0.22f), alongX ? new Vector3(d.width + 0.3f, 0.44f, 0.45f) : new Vector3(0.45f, 0.44f, d.width + 0.3f), metal, 1f, false);
                    continue;
                }
                if (d.kind == DoorKind.Fixed)
                {
                    Box(d.name, structure, center + Vector3.up * 1.2f, alongX ? new Vector3(1.4f, 2.4f, 0.06f) : new Vector3(0.06f, 2.4f, 1.4f), metal, 1f);
                    continue;
                }
                if (d.kind == DoorKind.Double)
                {
                    var l = (GameObject)PrefabUtility.InstantiatePrefab(w150, level); l.name = d.name + "_O";
                    l.transform.SetPositionAndRotation(center - new Vector3(1.5f, 0, 0), Quaternion.identity);
                    var r2 = (GameObject)PrefabUtility.InstantiatePrefab(w150, level); r2.name = d.name + "_E";
                    r2.transform.SetPositionAndRotation(center + new Vector3(1.5f, 0, 0), Quaternion.Euler(0, 180f, 0));
                    var d1 = l.GetComponentInChildren<Door>(); var d2 = r2.GetComponentInChildren<Door>(); d1.partner = d2; d2.partner = d1;
                    foreach (var t in l.GetComponentsInChildren<Transform>().Concat(r2.GetComponentsInChildren<Transform>())) if (t.name == "Leaf") leaves.Add(t.gameObject);
                    continue;
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(w150, level);
                go.name = d.name;
                go.transform.SetPositionAndRotation(alongX ? center - new Vector3(0.75f, 0, 0) : center - new Vector3(0, 0, 0.75f), Quaternion.Euler(0, alongX ? 0f : -90f, 0));
                foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == "Leaf") leaves.Add(t.gameObject);
            }
        }

        // ------------------------------------------------------------------ suelos, techos y luces
        static void EmitSlabs()
        {
            foreach (var r in Rooms)
            {
                if (r.kind == Kind.Shaft) { if (r.floor < -1f) Box("Ascensor_Fondo", structure, new Vector3(r.r.center.x, r.floor - 0.15f, r.r.center.y), new Vector3(r.r.width, 0.3f, r.r.height), metal, 1f); continue; }
                if (r.type == "fireescape") continue;                                   // la escalera de incendios es de rejilla (FireEscape)
                var fh = SlabHoles.Where(h => Mathf.Abs(h.y - r.floor) < 0.05f).Select(h => h.r);
                SlabRect(r.id + "_Suelo", r.r, r.floor - 0.3f, r.floor, fh, r.kind == Kind.Outdoor ? outdoorM : floorM, 4f);
                if (r.kind == Kind.Outdoor) continue;
                var ch = SlabHoles.Where(h => Mathf.Abs(h.y - (r.ceil + 0.5f)) < 0.05f || (r.ceil < h.y && h.y - r.ceil < 1.05f)).Select(h => h.r);
                SlabRect(r.id + "_Techo", r.r, r.ceil, r.ceil + 0.12f, ch, ceil, 2.4f);
                // cubierta del archivo y de las salas de la segunda planta
                if (r.floor >= F2 - 0.01f) Box(r.id + "_Cubierta", structure, new Vector3(r.r.center.x, r.ceil + 0.35f, r.r.center.y), new Vector3(r.r.width + 0.4f, 0.3f, r.r.height + 0.4f), wall, 3f);
            }
        }

        /// <summary>El punto si no cae en ningun hueco de losa; si cae, el punto mas cercano pegado al borde del hueco (a 0,7 m) que siga dentro de la sala y fuera de otros huecos; null si no hay.</summary>
        static Vector2? OutsideHoles(Room r, Vector2 p)
        {
            Rect? hit = null;
            foreach (var h in SlabHoles) if (h.r.Contains(p)) { hit = h.r; break; }
            if (hit == null) return p;
            var hr = hit.Value; float m = 0.7f;
            var cands = new[] { new Vector2(hr.xMin - m, Mathf.Clamp(p.y, hr.yMin, hr.yMax)), new Vector2(hr.xMax + m, Mathf.Clamp(p.y, hr.yMin, hr.yMax)),
                                new Vector2(Mathf.Clamp(p.x, hr.xMin, hr.xMax), hr.yMin - m), new Vector2(Mathf.Clamp(p.x, hr.xMin, hr.xMax), hr.yMax + m) };
            Vector2? best = null; float bd = float.MaxValue;
            foreach (var c in cands)
            {
                if (!r.r.Contains(c) || SlabHoles.Any(h => h.r.Contains(c))) continue;
                float d = Vector2.Distance(c, p); if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Color de las lamparas de una sala por zona (etapa F): ambar en vestibulo/atrio/galeria, sodio en el garaje, frio verdoso en calabozos, rojo de emergencia en el sotano industrial, azulado en los pasillos de servicio. Las salas con color propio lo conservan.</summary>
        static Color ZoneColor(Room r)
        {
            var d = new Vector3(r.light.r - 1f, r.light.g - 0.93f, r.light.b - 0.82f);
            if (r.kind == Kind.Corridor || d.magnitude > 0.02f) return r.light;
            switch (r.type)
            {
                case "garage": return new Color(1f, 0.72f, 0.38f);
                case "cells": case "custody": return new Color(0.72f, 1f, 0.86f);
                case "lobby": case "atrium": case "gallery": case "hall": case "waiting": return new Color(1f, 0.84f, 0.6f);
            }
            return r.floor < -1f ? new Color(1f, 0.64f, 0.56f) : r.light;
        }

        /// <summary>
        /// Lamparas (etapa F): una por ~40 m2 (2 a 8 por sala), foco de 36 (halls, atrio y galerias 29, pasillos de servicio 22) con rango 7,5, y relleno
        /// puntual (rango 6, intensidad 7) en una de cada dos lamparas de las salas de mas de 100 m2. Nombres "Lamp_&lt;sala&gt;#n". Entre el 25 y el 30 %
        /// de las del atrio, galerias y comedor estan rotas (nunca se encienden) y 1 de cada 3 de los pasillos del sotano parpadea.
        /// </summary>
        static void EmitLamps()
        {
            var rnd = new System.Random(31);
            foreach (var r in Rooms.Where(r => r.Indoor))
            {
                float area = r.r.width * r.r.height;
                int want = Mathf.Clamp(Mathf.RoundToInt(area / (r.kind == Kind.Corridor ? 45f : 40f)), 2, 8);
                // rejilla de nx x nz que reparte 'want' lamparas con celdas lo mas cuadradas posible
                int nx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(want * r.r.width / r.r.height))), nz = Mathf.Max(1, Mathf.CeilToInt(want / (float)nx));
                nx = Mathf.Min(nx, 8); nz = Mathf.Min(nz, 8);
                if (r.r.width < 3.1f) { nx = 1; nz = Mathf.Max(nz, want); }
                if (r.r.height < 3.1f) { nz = 1; nx = Mathf.Max(nx, want); }
                bool tall = r.type == "atrium" || r.type == "gallery" || r.type == "hall";
                float inten = r.kind == Kind.Corridor ? 22f : tall ? 29f : 36f;       // medido en Play (luminancia media de cada sala): con 20/16/12 la mediana era del 5,5 %, con estos ~8 %
                float range = Mathf.Min(7.5f, r.ceil - r.floor + 4f);
                var color = ZoneColor(r);
                bool mayBreak = r.type == "atrium" || r.type == "gallery" || r.id == "F_Canteen";
                int k = 0;
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < nz; j++)
                    {
                        var xz = new Vector3(r.r.xMin + r.r.width * (i + 0.5f) / nx, 0f, r.r.yMin + r.r.height * (j + 0.5f) / nz);
                        var spot = OutsideHoles(r, new Vector2(xz.x, xz.z));          // una lampara no cuelga de un hueco de escalera o de la galeria: se arrima a su borde
                        if (spot == null) continue;
                        xz = new Vector3(spot.Value.x, 0f, spot.Value.y);
                        bool fill = area > 100f && k % 2 == 0;
                        var lamp = Call<CeilingLamp>("Lamp", level, xz, color, inten, range, r.floor < -1f && r.kind == Kind.Corridor && k % 3 == 1, fill ? 6f : 0f, fill ? 7f : 0f);
                        lamp.transform.position += Vector3.up * (r.ceil - 3.0f);
                        lamp.name = "Lamp_" + r.id + "#" + k;
                        if (mayBreak && rnd.NextDouble() < 0.28) lamp.dead = true;
                        k++;
                        lamps++;
                    }
            }
        }

        // ------------------------------------------------------------------ escaleras
        /// <summary>Escalera recta a lo largo de Z (de z0 a z1, subiendo de y0 a y1) entre x0 y x1: peldanos (decorado) y rampa invisible.</summary>
        public static Transform StraightStairZ(string name, float x0, float x1, float z0, float z1, float y0, float y1, Material m)
        {
            var root = new GameObject(name).transform; root.SetParent(structure);
            int n = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(y1 - y0) / 0.2f));
            float run = (z1 - z0) / n, rise = (y1 - y0) / n, xc = (x0 + x1) / 2f, w = x1 - x0;
            float baseY = Mathf.Min(y0, y1) - 0.3f;
            for (int k = 0; k < n; k++)
            {
                float top = y0 + rise * (k + 0.5f), za = z0 + run * k, zb = za + run;
                Box("Peldano", root, new Vector3(xc, (top + baseY) / 2f, (za + zb) / 2f), new Vector3(w, top - baseY, Mathf.Abs(run)), m, 1f, false);
            }
            float len = Mathf.Sqrt((z1 - z0) * (z1 - z0) + (y1 - y0) * (y1 - y0)), ang = -Mathf.Atan2(y1 - y0, z1 - z0) * Mathf.Rad2Deg;
            var ramp = new GameObject("Rampa"); ramp.transform.SetParent(root);
            ramp.transform.SetPositionAndRotation(new Vector3(xc, (y0 + y1) / 2f + 0.02f, (z0 + z1) / 2f), Quaternion.Euler(ang, 0, 0));
            var rc = ramp.AddComponent<BoxCollider>(); rc.size = new Vector3(w, 0.1f, len); rc.center = new Vector3(0, -0.05f, 0); ramp.isStatic = true;
            return root;
        }

        /// <summary>Escalera recta a lo largo de X (de x0 a x1, de y0 a y1; x1 puede ser menor que x0) con ancho z0..z1: peldanos (decorado) y rampa invisible.</summary>
        public static Transform StraightStairX(string name, float x0, float x1, float z0, float z1, float y0, float y1, Material m)
        {
            var root = new GameObject(name).transform; root.SetParent(structure);
            int n = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(y1 - y0) / 0.2f));
            float run = (x1 - x0) / n, rise = (y1 - y0) / n, zc = (z0 + z1) / 2f, w = z1 - z0;
            float baseY = Mathf.Min(y0, y1) - 0.3f;
            for (int k = 0; k < n; k++)
            {
                float top = y0 + rise * (k + 0.5f), xa = x0 + run * k, xb = xa + run;
                Box("Peldano", root, new Vector3((xa + xb) / 2f, (top + baseY) / 2f, zc), new Vector3(Mathf.Abs(run), top - baseY, w), m, 1f, false);
            }
            float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)), ang = Mathf.Atan((y1 - y0) / (x1 - x0)) * Mathf.Rad2Deg;   // atan (no atan2): la caja es simetrica y asi su 'arriba' sigue arriba
            var ramp = new GameObject("Rampa"); ramp.transform.SetParent(root);
            ramp.transform.SetPositionAndRotation(new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f + 0.02f, zc), Quaternion.Euler(0, 0, ang));
            var rc = ramp.AddComponent<BoxCollider>(); rc.size = new Vector3(len, 0.1f, w); rc.center = new Vector3(0, -0.05f, 0); ramp.isStatic = true;
            return root;
        }

        public static void Rail(Transform parent, Vector3 a, Vector3 b, bool bars = true)
        {
            var g = new GameObject("Barandilla").transform; g.SetParent(parent);
            Vector3 d = b - a; float len = d.magnitude; if (len < 0.05f) return;
            var rot = Quaternion.LookRotation(d / len);
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 1.2f));
            for (int i = 0; i <= posts; i++) Box("Pie", g, Vector3.Lerp(a, b, i / (float)posts) + Vector3.up * 0.5f, new Vector3(0.05f, 1.0f, 0.05f), metal, 0f, false);
            var top = Box("Pasamanos", g, (a + b) / 2f + Vector3.up * 1.0f, new Vector3(0.07f, 0.05f, len), wood, 0f, false); top.transform.rotation = rot;
            var mid = Box("Barra", g, (a + b) / 2f + Vector3.up * 0.15f, new Vector3(0.04f, 0.04f, len), metal, 0f, false); mid.transform.rotation = rot;
            if (bars) { int nb = Mathf.FloorToInt(len / 0.12f); for (int i = 1; i < nb; i++) Box("Barrote", g, Vector3.Lerp(a, b, i / (float)nb) + Vector3.up * 0.58f, new Vector3(0.022f, 0.84f, 0.022f), metal, 0f, false); }
            var col = new GameObject("Colision"); col.transform.SetParent(g);
            col.transform.SetPositionAndRotation((a + b) / 2f + Vector3.up * 0.6f, rot);
            col.AddComponent<BoxCollider>().size = new Vector3(0.08f, 1.2f, len); col.isStatic = true;
        }

        /// <summary>Balaustrada de la galeria de la primera planta alrededor del hueco de doble altura sobre el atrio.</summary>
        static void GalleryRails()
        {
            var root = new GameObject("BalaustradaGaleria").transform; root.SetParent(structure);
            var h = GalleryHole; float y = F1;
            Rail(root, new Vector3(h.xMin, y, h.yMin), new Vector3(h.xMax, y, h.yMin));
            Rail(root, new Vector3(h.xMax, y, h.yMin), new Vector3(h.xMax, y, h.yMax));
            Rail(root, new Vector3(h.xMax, y, h.yMax), new Vector3(h.xMin, y, h.yMax));
            Rail(root, new Vector3(h.xMin, y, h.yMax), new Vector3(h.xMin, y, h.yMin));
        }

        /// <summary>
        /// Rampa de coches del garaje: baja por el callejon oeste (dentro de la verja, a x -41,5..-37,2) desde la calle (z 44, y 0) hasta una
        /// explanada a -4,5 m (z 0..5,5) junto a la puerta enrollable del muro oeste del garaje. Calzada inclinada con colision, muros de contencion
        /// a ambos lados y parapeto de 0,9 m. El lado este lo remata la pared del callejon (que empieza a -0,3).
        /// </summary>
        static void GarageRamp()
        {
            var root = new GameObject("RampaGaraje").transform; root.SetParent(structure);
            const float xw = -41.5f, xe = -37.2f, zTop = 44f, zBot = 5.5f, zS = 0f, ex1 = -32f, top = 0.9f;
            float L = zTop - zBot, yBot = B;
            System.Func<float, float> surf = z => z <= zBot ? yBot : Mathf.Lerp(yBot, 0f, (z - zBot) / L);
            float len = Mathf.Sqrt(L * L + yBot * yBot), ang = -Mathf.Atan2(-yBot, L) * Mathf.Rad2Deg;   // sube hacia +z
            var slab = Box("Calzada", root, new Vector3((xw + xe) / 2f, yBot / 2f - 0.15f, (zTop + zBot) / 2f), new Vector3(xe - xw, 0.3f, len), outdoorM, 4f);
            slab.transform.rotation = Quaternion.Euler(ang, 0, 0);
            Box("Explanada", root, new Vector3((xw + ex1) / 2f, yBot - 0.15f, (zS + zBot) / 2f), new Vector3(ex1 - xw, 0.3f, zBot - zS), outdoorM, 4f);
            for (float z = zS; z < zTop - 0.01f; z += 2f)
            {
                float z2 = Mathf.Min(z + 2f, zTop), zc = (z + z2) / 2f, yLow = surf(z) - 0.45f;
                Box("Muro_O", root, new Vector3(xw - 0.175f, (yLow + top) / 2f, zc), new Vector3(0.35f, top - yLow, z2 - z + 0.02f), wall, 3f);
                if (z < zBot - 0.01f) continue;
                float topE = z < 8f ? top : -0.3f;
                Box("Muro_E", root, new Vector3(xe + 0.175f, (yLow + topE) / 2f, zc), new Vector3(0.35f, topE - yLow, z2 - z + 0.02f), wall, 3f);
            }
            float lo = yBot - 0.45f;
            Box("Muro_S", root, new Vector3((xw - 0.35f + ex1) / 2f, (lo + top) / 2f, zS - 0.175f), new Vector3(ex1 - xw + 0.35f, top - lo, 0.35f), wall, 3f);
            Box("Muro_N", root, new Vector3((xe + ex1) / 2f, (lo + top) / 2f, zBot + 0.175f), new Vector3(ex1 - xe, top - lo, 0.35f), wall, 3f);
        }

        /// <summary>Caracol del vestibulo (de la planta baja a la primera), igual que la de la fase 1 pero en el vestibulo nuevo.</summary>
        static void SpiralStair()
        {
            var root = new GameObject("EscaleraCaracol").transform; root.SetParent(structure);
            var C = SpiralC; float R0 = SpiralR, inner = 0.3f;
            float rise = F1 - G, turn = 360f, start = 180f, sgn = -1f; int steps = 22;
            float Ang(float k) => start + sgn * turn * k;
            int seg = 64; var v = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float k = i / (float)seg, a = Ang(k) * Mathf.Deg2Rad, y = G + rise * k;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(C + dir * inner + Vector3.up * y); v.Add(C + dir * R0 + Vector3.up * y);
                if (i > 0) { int b = v.Count - 4; tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }); }
            }
            var mesh = new Mesh { name = "RampaCaracolGrande" }; mesh.SetVertices(v); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = "Assets/_Project/Art/Props/RampaCaracolGrande.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (old == null) AssetDatabase.CreateAsset(mesh, mp); else { old.Clear(); old.SetVertices(v); old.SetTriangles(tris, 0); old.RecalculateNormals(); old.RecalculateBounds(); EditorUtility.SetDirty(old); mesh = old; }
            var ramp = new GameObject("Rampa"); ramp.transform.SetParent(root); ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
            for (int i = 0; i < steps; i++)
            {
                float a = Ang((i + 0.5f) / steps), y = G + rise * (i + 1) / steps;
                var dir = Quaternion.Euler(0, -a, 0) * Vector3.right;
                var step = Box("Peldano", root, C + dir * ((inner + R0) / 2f) + Vector3.up * (y - 0.03f), new Vector3(R0 - inner, 0.05f, 0.36f), metal, 0f, false);
                step.transform.rotation = Quaternion.Euler(0, -a, 0);
                for (int j = 0; j < 3; j++)
                {
                    float aj = Ang((i + j / 3f) / steps), yj = G + rise * (i + j / 3f + 0.5f) / steps;
                    var dj = Quaternion.Euler(0, -aj, 0) * Vector3.right;
                    Box(j == 0 ? "PieDerecho" : "Barrote", root, C + dj * (R0 - 0.05f) + Vector3.up * (yj + 0.47f), j == 0 ? new Vector3(0.045f, 0.94f, 0.045f) : new Vector3(0.022f, 0.9f, 0.022f), metal, 0f, false);
                }
            }
            Box("Columna", root, C + Vector3.up * (rise / 2f + 0.5f), new Vector3(0.24f, rise + 1f, 0.24f), metal, 0f);
            int hs = steps * 2;
            for (int i = 0; i < hs - 1; i++)
            {
                float k0 = (i + 0.5f) / hs, k1 = (i + 1.5f) / hs;
                var p0 = C + Quaternion.Euler(0, -Ang(k0), 0) * Vector3.right * (R0 - 0.05f) + Vector3.up * (G + rise * k0 + 0.95f);
                var p1 = C + Quaternion.Euler(0, -Ang(k1), 0) * Vector3.right * (R0 - 0.05f) + Vector3.up * (G + rise * k1 + 0.95f);
                var h = Box("Pasamanos", root, (p0 + p1) / 2f, new Vector3(0.06f, 0.05f, Vector3.Distance(p0, p1) + 0.03f), wood, 0f, false);
                h.transform.rotation = Quaternion.LookRotation(p1 - p0);
            }
            var hole = SpiralHole;
            float rx0 = hole.xMin, rx1 = C.x - inner, rz0 = C.z, rz1 = hole.yMax;
            Box("Rellano", root, new Vector3((rx0 + rx1) / 2f, F1 - 0.15f, (rz0 + rz1) / 2f), new Vector3(rx1 - rx0, 0.3f, rz1 - rz0), floorM, 4f);
            float yf = F1;
            Rail(root, new Vector3(hole.xMin, yf, hole.yMin), new Vector3(hole.xMax, yf, hole.yMin));
            Rail(root, new Vector3(hole.xMax, yf, hole.yMin), new Vector3(hole.xMax, yf, hole.yMax));
            Rail(root, new Vector3(rx1, yf, hole.yMax), new Vector3(hole.xMax, yf, hole.yMax));
            Rail(root, new Vector3(hole.xMin, yf, hole.yMin), new Vector3(hole.xMin, yf, rz0));
            Rail(root, new Vector3(rx1, yf, rz0), new Vector3(rx1, yf, hole.yMax));
        }

        /// <summary>Escalera de servicio norte (baja -> primera) y la del archivo (primera -> segunda), con barandillas en el hueco.</summary>
        static void Stairs()
        {
            var n = StraightStairZ("EscaleraNorte", 4.2f, 6.2f, 32f, 40f, G, F1, metal);
            Rail(n, new Vector3(6.3f, G, 32f), new Vector3(6.3f, F1, 40f));                            // lado abierto, subiendo
            Rail(n, new Vector3(6.4f, F1, 31.6f), new Vector3(6.4f, F1, 40.0f));                       // hueco arriba
            Rail(n, new Vector3(4f, F1, 31.6f), new Vector3(6.4f, F1, 31.6f));
            var a = StraightStairZ("EscaleraArchivo", -13.8f, -11.8f, 32f, 39f, F1, F2, metal);
            Rail(a, new Vector3(-11.7f, F1, 31.4f), new Vector3(-11.7f, F2, 39f));
            Rail(a, new Vector3(-11.4f, F2, 31.6f), new Vector3(-11.4f, F2, 39.0f));
            Rail(a, new Vector3(-14f, F2, 31.6f), new Vector3(-11.4f, F2, 31.6f));
            // sala de espera -> garaje (sotano): a lo largo de X pegada a la pared sur, baja hacia el oeste; el hueco queda a ras de la planta baja con barandilla
            var g = StraightStairX("EscaleraGaraje", -14.5f, -23f, 0.2f, 2.2f, G, B, metal);
            Rail(g, new Vector3(-14.5f, G, 2.2f), new Vector3(-23f, G, 2.2f));        // borde norte del hueco
            Rail(g, new Vector3(-23f, G, 0.2f), new Vector3(-23f, G, 2.2f));          // cierre oeste
            Rail(g, new Vector3(-14.5f, G, 2.2f), new Vector3(-23f, B, 2.2f));        // pasamanos de bajada
            // escalera norte, tramo que baja a la custodia (se entra por el sur, a z 32; baja hacia el norte)
            var cu = StraightStairZ("EscaleraCustodia", 7f, 9f, 32f, 40f, G, B, metal);
            Rail(cu, new Vector3(7f, G, 32f), new Vector3(7f, G, 40f));
            Rail(cu, new Vector3(9f, G, 32f), new Vector3(9f, G, 40f));
            Rail(cu, new Vector3(7f, G, 40f), new Vector3(9f, G, 40f));
            Rail(cu, new Vector3(7f, G, 32f), new Vector3(7f, B, 40f));
            // sala de calderas (hundida 2 m): rellano junto a la puerta y escalera hacia el norte hasta el foso
            var c = StraightStairZ("EscaleraCalderas", -23f, -21f, 31.5f, 35.5f, B, -6.5f, metal);
            Box("Rellano_Calderas", c, new Vector3(-22f, (B - 0.3f + -6.8f) / 2f + 0.15f, 30.25f), new Vector3(4f, B - (-6.8f), 2.5f), floorM, 4f);
            Rail(c, new Vector3(-24f, B, 31.5f), new Vector3(-23.1f, B, 31.5f));
            Rail(c, new Vector3(-20.9f, B, 31.5f), new Vector3(-20f, B, 31.5f));
            Rail(c, new Vector3(-24f, B, 29.2f), new Vector3(-24f, B, 31.5f));
            Rail(c, new Vector3(-20f, B, 29.2f), new Vector3(-20f, B, 31.5f));
            Rail(c, new Vector3(-23.1f, B, 31.5f), new Vector3(-23.1f, -6.5f, 35.5f));
            Rail(c, new Vector3(-20.9f, B, 31.5f), new Vector3(-20.9f, -6.5f, 35.5f));
        }

        /// <summary>Escalera de incendios de chapa en la fachada oeste: callejon -> balcon de la primera (puerta del jefe de seguridad) -> azotea.</summary>
        static void FireEscape()
        {
            var root = new GameObject("EscaleraIncendios").transform; root.SetParent(structure);
            float xa = -35.6f, xb = -33.6f;                                       // tramos, a 1,6 m de la fachada
            // tramo 1: del callejon (z 34) sube hacia el sur hasta el balcon (z 26, y 4)
            var t1 = StraightStairZ("Tramo1", xa, xb, 34f, 26f, G, F1, grating); t1.SetParent(root);
            // balcon (x -36..-32, z 17..26) y rellano de arriba (x -36..-32, z 6..11)
            Box("Balcon", root, new Vector3(-34f, F1 - 0.06f, 21.5f), new Vector3(4f, 0.12f, 9f), grating, 1f);
            var t2 = StraightStairZ("Tramo2", xa, xb, 17f, 11f, F1, F2, grating); t2.SetParent(root);
            Box("Rellano_Azotea", root, new Vector3(-34f, F2 - 0.06f, 8.5f), new Vector3(4f, 0.12f, 5f), grating, 1f);
            // barandillas: borde exterior de todo y el lado de los tramos que da al vacio
            Rail(root, new Vector3(-36f, F1, 17f), new Vector3(-36f, F1, 26f), false);
            Rail(root, new Vector3(-33.4f, F1, 26f), new Vector3(-32f, F1, 26f), false);      // borde norte del balcon, junto al tramo que llega
            Rail(root, new Vector3(-36f, F2, 6f), new Vector3(-36f, F2, 11f), false);
            Rail(root, new Vector3(-36f, F2, 6f), new Vector3(-32f, F2, 6f), false);
            Rail(root, new Vector3(-33.5f, F2, 11f), new Vector3(-32f, F2, 11f), false);
            Rail(root, new Vector3(-35.8f, G, 34f), new Vector3(-35.8f, F1, 26f), false);
            Rail(root, new Vector3(-33.4f, G, 34f), new Vector3(-33.4f, F1, 26.3f), false);
            Rail(root, new Vector3(-35.8f, F1, 17f), new Vector3(-35.8f, F2, 11f), false);
            Rail(root, new Vector3(-33.4f, F1, 17f), new Vector3(-33.4f, F2, 11f), false);
            // pies derechos que lo sujetan
            foreach (var (x, z, top) in new[] { (-36f, 17f, F1), (-36f, 26f, F1), (-36f, 6f, F2), (-36f, 11f, F2) })
                Box("Pilar", root, new Vector3(x, top / 2f, z), new Vector3(0.12f, top, 0.12f), metal, 0f);
        }

        // ------------------------------------------------------------------ entrada
        [MenuItem("Horror/Comisaria grande/1 Estructura")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        /// <summary>Rehace solo las lamparas (sin tocar la estructura, el mobiliario ni lo demas) para ajustar la luz rapido. Despues hay que repetir el menu 4 (los interruptores apuntan a las lamparas) y el 7.</summary>
        [MenuItem("Horror/Comisaria grande/7a Rehacer lamparas")]
        public static void RebuildLampsMenu() { Debug.Log("[Horror] " + RebuildLamps()); }

        public static string RebuildLamps()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("--- COMISARIA V2 ---"); if (root == null) return "falta la fase A";
            level = root.transform; lamps = 0;
            foreach (var t in level.Cast<Transform>().Where(t => t.name.StartsWith("Lamp_")).ToList()) Object.DestroyImmediate(t.gameObject);
            EmitLamps();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "lamparas rehechas: " + lamps + " (repite el menu 4 y el 7)";
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            Define();
            EditorSceneManager.SaveOpenScenes();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) AssetDatabase.CopyAsset("Assets/Scenes/Comisaria.unity", ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "--- LEVEL ---" || go.name == "Details" || go.name == "Items" || go.name == "Zombies" || go.name == "ArenaBlockers" || go.name == "--- COMISARIA V2 ---")
                    Object.DestroyImmediate(go);
            wall = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat");
            floorM = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Floor.mat");
            ceil = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Ceiling.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat");
            outdoorM = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Sidewalk.mat") ?? floorM;
            grating = metal;
            leaves.Clear(); lamps = 0;

            var rootGo = new GameObject("--- COMISARIA V2 ---");
            level = rootGo.transform;
            structure = new GameObject("Estructura").transform; structure.SetParent(level);
            details = new GameObject("Details").transform; details.SetParent(level);
            new GameObject("Zombies").transform.SetParent(level);
            new GameObject("Items").transform.SetParent(level);
            var props = new GameObject("Props"); props.transform.SetParent(level);
            var nw = props.AddComponent<NavMeshModifier>(); nw.overrideArea = true; nw.area = 1; nw.applyToChildren = true;

            var segs = BuildSegments();
            EmitWalls(segs);
            EmitSlabs();
            EmitDoors();
            EmitLamps();
            SpiralStair();
            Stairs();
            FireEscape();
            GalleryRails();
            GarageRamp();

            var surface = rootGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = 0.1f;
            Physics.SyncTransforms();
            foreach (var l in leaves) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in leaves) l.SetActive(true);
            var rt = rootGo.AddComponent<RuntimeNavMesh>(); rt.surface = surface; rt.disableDuringBake = leaves.ToArray();

            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) { pc.transform.SetPositionAndRotation(new Vector3(0f, G + 1.05f, 1.6f), Quaternion.identity); var cam = pc.cam; if (cam != null) cam.SetYaw(0f); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "comisaria grande: " + Rooms.Count + " espacios, " + segs.Count + " tramos de pared, " + Doors.Count + " puertas, " + lamps + " lamparas";
        }
    }
}
#endif
