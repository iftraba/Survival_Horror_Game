#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase C (menu Horror/Comisaria grande/3 Mobiliario): amuebla cada sala segun su tipo (ComisariaGrande.Rooms).
    /// Cada tipo tiene su funcion (oficina: islas de mesas; vestuarios: filas de taquillas; biblioteca: estanterias; etc.). Lo que va
    /// contra una pared se coloca a lo largo de ella saltandose las puertas; nada tapa el paso de una puerta ni las escaleras.
    /// Variedad: giro, desplazamiento y tono al azar en cada mueble (PropVariant) y lo que hay encima de las mesas cambia.
    /// Los muebles van en "Props/Mobiliario" (rompibles por el jefe 1 y no transitables); lo fijo (estanterias de obra del archivo,
    /// caldera, tanques) en "Mobiliario_Fijo". Repetible.
    /// </summary>
    public static class ComisariaGrandeProps
    {
        const string P = "Assets/_Project/Art/Props/";
        const string O = P + "Office/", M = P + "Memorial/", A = P + "Archive/", Bm = P + "Basement/", S = P + "Station/", E = P + "Exterior/";
        const string Mats = "Assets/_Project/Materials/";
        const float Wall = 0.2f;                       // de la linea de pared a su cara interior (con margen)

        static Transform props, fixedRoot;
        static System.Random rng;
        static float Rn(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static bool Chance(double p) => rng.NextDouble() < p;
        static int count;
        static Material metal, wood;

        // ------------------------------------------------------------------ colocacion
        static GameObject Place(string path, Vector3 pos, float yaw, bool collider = true, bool tint = true, Transform parent = null, float jitter = 1f)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) { Debug.LogWarning("[Horror] falta " + path); return null; }
            var holder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(parent ?? props);
            holder.transform.SetPositionAndRotation(pos + new Vector3(Rn(-0.03f, 0.03f), 0f, Rn(-0.03f, 0.03f)) * jitter, Quaternion.Euler(0, yaw + Rn(-2.5f, 2.5f) * jitter, 0));
            var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, holder.transform);
            m.transform.localPosition = Vector3.zero;
            if (collider) Pickup.FitBoxCollider(holder);
            if (tint) { float v = Rn(0.8f, 1.05f); holder.AddComponent<PropVariant>().tint = new Color(v * Rn(0.96f, 1.03f), v, v * Rn(0.96f, 1.03f)); }
            foreach (var t in holder.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            count++;
            return holder;
        }

        static GameObject Inst(string prefab, Vector3 pos, float yaw, string name, Transform parent)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/" + prefab);
            if (pf == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); go.name = name;
            return go;
        }

        static List<ComisariaGrande.DoorDef> DoorsOf(ComisariaGrande.Room r) =>
            ComisariaGrande.Doors.Where(d => Mathf.Abs(d.floor - r.floor) < 0.6f && d.p.x >= r.r.xMin - 0.05f && d.p.x <= r.r.xMax + 0.05f && d.p.y >= r.r.yMin - 0.05f && d.p.y <= r.r.yMax + 0.05f).ToList();

        /// <summary>Zonas que no se pueden tapar en esta sala: el paso delante de cada puerta y las escaleras.</summary>
        static List<Rect> KeepOut(ComisariaGrande.Room r)
        {
            var k = new List<Rect>();
            foreach (var d in DoorsOf(r))
            {
                bool alongX = Mathf.Abs(d.p.y - r.r.yMin) < 0.05f || Mathf.Abs(d.p.y - r.r.yMax) < 0.05f;
                float w = d.width / 2f + 0.5f, depth = 2.0f;
                k.Add(alongX ? Rect.MinMaxRect(d.p.x - w, d.p.y - depth, d.p.x + w, d.p.y + depth) : Rect.MinMaxRect(d.p.x - depth, d.p.y - w, d.p.x + depth, d.p.y + w));
            }
            var sh = ComisariaGrande.SpiralHole;
            if (r.id == "G_Lobby" || r.id == "F_Mem") { k.Add(new Rect(sh.x - 0.8f, sh.y - 0.8f, sh.width + 1.6f, sh.height + 1.6f)); if (r.id == "G_Lobby") k.Add(Rect.MinMaxRect(-1.8f, 0f, 2.6f, 6.0f)); }
            if (r.type == "stairwell") k.Add(Rect.MinMaxRect(3.6f, 29f, 7.2f, 44f));
            if (r.type == "servicestair") k.Add(Rect.MinMaxRect(-14f, 29f, -10.6f, 44f));
            if (r.id == "S_Ante") k.Add(Rect.MinMaxRect(-14f, 29f, -10.8f, 44f));
            if (r.id == "B_Boiler") k.Add(Rect.MinMaxRect(-25f, 29f, -19f, 37f));
            var shaft = ComisariaGrande.Shaft; k.Add(new Rect(shaft.x - 1.6f, shaft.y - 0.2f, 1.6f, shaft.height + 0.2f));   // delante del ascensor
            return k;
        }

        /// <summary>True si un mueble de ese tamano (rectangulo centrado en p) cabe sin tapar puertas/escaleras y dentro de la sala.</summary>
        static bool Free(ComisariaGrande.Room r, Vector2 p, Vector2 half, List<Rect> keep, List<Rect> used)
        {
            var rr = Rect.MinMaxRect(p.x - half.x, p.y - half.y, p.x + half.x, p.y + half.y);
            if (rr.xMin < r.r.xMin + 0.1f || rr.xMax > r.r.xMax - 0.1f || rr.yMin < r.r.yMin + 0.1f || rr.yMax > r.r.yMax - 0.1f) return false;
            if (keep.Any(k => k.Overlaps(rr))) return false;
            if (used.Any(u => u.Overlaps(rr))) return false;
            return true;
        }

        /// <summary>
        /// Fila de muebles contra una pared ('S','N','W','E'): 'len' a lo largo, 'depth' hacia dentro, separados 'gap'. Elige al
        /// azar de 'paths' y se salta los sitios ocupados o delante de una puerta. 'fill' = probabilidad de poner algo en cada hueco.
        /// </summary>
        static int AlongWall(ComisariaGrande.Room r, char side, string[] paths, float len, float depth, float gap, List<Rect> keep, List<Rect> used, double fill = 1, float margin = 0.3f)
        {
            int n = 0;
            bool alongX = side == 'S' || side == 'N';
            float a0 = (alongX ? r.r.xMin : r.r.yMin) + margin, a1 = (alongX ? r.r.xMax : r.r.yMax) - margin;
            float yaw = side == 'S' ? 0f : side == 'N' ? 180f : side == 'W' ? 90f : -90f;
            float fixedC = side == 'S' ? r.r.yMin + Wall + depth / 2f : side == 'N' ? r.r.yMax - Wall - depth / 2f : side == 'W' ? r.r.xMin + Wall + depth / 2f : r.r.xMax - Wall - depth / 2f;
            for (float a = a0 + len / 2f; a + len / 2f <= a1; a += len + gap)
            {
                var p = alongX ? new Vector2(a, fixedC) : new Vector2(fixedC, a);
                var half = alongX ? new Vector2(len / 2f, depth / 2f) : new Vector2(depth / 2f, len / 2f);
                if (!Free(r, p, half, keep, used)) continue;
                if (!Chance(fill)) continue;
                Place(paths[rng.Next(paths.Length)], new Vector3(p.x, r.floor, p.y), yaw);
                used.Add(Rect.MinMaxRect(p.x - half.x, p.y - half.y, p.x + half.x, p.y + half.y));
                n++;
            }
            return n;
        }

        /// <summary>Un mueble suelto en un punto (si cabe).</summary>
        static GameObject At(ComisariaGrande.Room r, string path, float x, float z, float yaw, Vector2 half, List<Rect> keep, List<Rect> used, bool collider = true)
        {
            var p = new Vector2(x, z);
            bool rot = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(yaw, -90f)) < 45f;
            var h = rot ? new Vector2(half.y, half.x) : half;
            if (!Free(r, p, h, keep, used)) return null;
            used.Add(Rect.MinMaxRect(x - h.x, z - h.y, x + h.x, z + h.y));
            return Place(path, new Vector3(x, r.floor, z), yaw, collider);
        }

        /// <summary>Encima de una mesa (no ocupa suelo).</summary>
        static void OnTop(string path, Vector3 pos, float yaw) => Place(path, pos, yaw, false, false, null, 0.5f);

        /// <summary>Puesto: mesa, silla y cosas encima al azar (ordenador o portatil, papeles, lampara, telefono, papelera).</summary>
        static void Desk(Vector3 c, float yaw)
        {
            Place(P + "Desk.fbx", c, yaw);
            var rot = Quaternion.Euler(0, yaw, 0);
            Vector3 worker = rot * Vector3.forward, side = rot * Vector3.right;
            if (Chance(0.85)) Place(Chance(0.7) ? O + "SwivelChair.fbx" : P + "Chair.fbx", c + worker * Rn(0.75f, 1.0f) + side * Rn(-0.25f, 0.25f), yaw + 180f + Rn(-35f, 35f));
            float top = c.y + 0.76f; var t = new Vector3(c.x, top, c.z);
            double k = rng.NextDouble();
            if (k < 0.55) OnTop(O + "DeskComputer.fbx", t - worker * 0.08f + side * Rn(-0.3f, 0.1f), yaw + Rn(-8f, 8f));
            else if (k < 0.8) OnTop(O + "Laptop.fbx", t + worker * 0.05f + side * Rn(-0.3f, 0.3f), yaw + Rn(-20f, 20f));
            if (Chance(0.55)) OnTop(O + "PaperStack.fbx", t + side * Rn(0.35f, 0.5f) * (Chance(0.5) ? -1 : 1) + worker * Rn(-0.1f, 0.15f), yaw + Rn(-40f, 40f));
            if (Chance(0.3)) OnTop(O + "DeskLamp.fbx", t + side * 0.6f - worker * 0.2f, yaw + Rn(-30f, 30f));
            if (Chance(0.25)) OnTop(P + "Phone.fbx", t - side * 0.55f, yaw + Rn(-30f, 30f));
            if (Chance(0.4)) Place(O + "TrashBin.fbx", c + side * Rn(0.85f, 0.95f) * (Chance(0.5) ? -1 : 1) + worker * 0.3f, Rn(0f, 360f));
        }

        /// <summary>Islas de dos mesas enfrentadas en rejilla dentro de la sala (respetando puertas y pasillos).</summary>
        static void Pods(ComisariaGrande.Room r, List<Rect> keep, List<Rect> used, float inset = 1.9f)
        {
            for (float x = r.r.xMin + inset; x <= r.r.xMax - inset + 0.01f; x += 3.4f)
                for (float z = r.r.yMin + inset + 0.6f; z <= r.r.yMax - inset - 0.6f + 0.01f; z += 5.0f)
                {
                    var half = new Vector2(0.95f, 2.0f);
                    if (!Free(r, new Vector2(x, z), half, keep, used)) continue;
                    used.Add(Rect.MinMaxRect(x - half.x, z - half.y, x + half.x, z + half.y));
                    Desk(new Vector3(x, r.floor, z - 0.375f), 180f);
                    Desk(new Vector3(x, r.floor, z + 0.375f), 0f);
                }
        }

        /// <summary>Algo de desorden por el suelo: papeles, sillas volcadas, cajas.</summary>
        static void Clutter(ComisariaGrande.Room r, List<Rect> keep, List<Rect> used, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float x = Rn(r.r.xMin + 0.8f, r.r.xMax - 0.8f), z = Rn(r.r.yMin + 0.8f, r.r.yMax - 0.8f);
                double k = rng.NextDouble();
                if (k < 0.5) { if (Free(r, new Vector2(x, z), new Vector2(0.8f, 0.8f), new List<Rect>(), new List<Rect>())) Place(A + "PaperScatter.fbx", new Vector3(x, r.floor, z), Rn(0, 360), false); }
                else if (k < 0.75) { var c = At(r, P + "Chair.fbx", x, z, Rn(0, 360), new Vector2(0.3f, 0.3f), keep, used); if (c != null && Chance(0.6)) { c.transform.rotation = Quaternion.Euler(0, Rn(0, 360), 0) * Quaternion.Euler(Rn(80, 95), 0, 0); c.transform.position += Vector3.up * 0.25f; } }
                else At(r, Chance(0.5) ? P + "Crate.fbx" : A + "ArchiveBoxStackA.fbx", x, z, Rn(0, 360), new Vector2(0.6f, 0.6f), keep, used);
            }
        }

        // ------------------------------------------------------------------ tipos de sala
        static void Furnish(ComisariaGrande.Room r)
        {
            var keep = KeepOut(r); var used = new List<Rect>();
            float x0 = r.r.xMin, x1 = r.r.xMax, z0 = r.r.yMin, z1 = r.r.yMax, y = r.floor, cx = r.r.center.x, cz = r.r.center.y;
            string[] shelves = { P + "Shelf.fbx" }, cabinets = { P + "FilingCabinet.fbx" }, lockers = { P + "Locker.fbx" };
            switch (r.type)
            {
                case "safe":
                    Inst(r.floor < -1f ? "Interactables/SavePhone.prefab" : "Interactables/SaveTerminal.prefab", new Vector3(x0 + 0.5f, y, cz - 2f), 90f, "Guardar_" + r.id, fixedRoot);
                    Inst("Interactables/ItemBox.prefab", new Vector3(x1 - 0.5f, y, cz - 2f), -90f, "Baul_" + r.id, fixedRoot);
                    used.Add(Rect.MinMaxRect(x0, cz - 3f, x0 + 1.2f, cz - 1f)); used.Add(Rect.MinMaxRect(x1 - 1.2f, cz - 3f, x1, cz - 1f));
                    At(r, P + "Cot.fbx", x0 + 1.2f, z0 + 1.2f, 0f, new Vector2(1.0f, 0.5f), keep, used);
                    At(r, P + "Desk.fbx", x1 - 1.2f, z0 + 0.9f, 0f, new Vector2(0.75f, 0.4f), keep, used);
                    At(r, M + "PottedPlant.fbx", x0 + 0.6f, cz + 2.5f, 0, new Vector2(0.4f, 0.4f), keep, used);
                    AlongWall(r, 'E', shelves, 1.2f, 0.45f, 0.3f, keep, used, 0.5);
                    break;
                case "waiting":
                    AlongWall(r, 'S', new[] { E + "WaitingBench.fbx" }, 2.2f, 0.6f, 0.6f, keep, used, 0.85);
                    AlongWall(r, 'W', new[] { E + "WaitingBench.fbx" }, 2.2f, 0.6f, 0.6f, keep, used, 0.85);
                    for (float x = x0 + 4f; x < x1 - 3f; x += 4.2f) { At(r, E + "WaitingBench.fbx", x, cz - 0.35f, 180f, new Vector2(1.1f, 0.3f), keep, used); At(r, E + "WaitingBench.fbx", x, cz + 0.35f, 0f, new Vector2(1.1f, 0.3f), keep, used); }
                    At(r, O + "WaterCooler.fbx", x1 - 0.5f, z0 + 0.5f, -90f, new Vector2(0.2f, 0.2f), keep, used);
                    At(r, M + "PottedPlant.fbx", x0 + 0.6f, z1 - 0.6f, 0, new Vector2(0.4f, 0.4f), keep, used);
                    Clutter(r, keep, used, 5);
                    break;
                case "lobby":
                    At(r, E + "ReceptionDesk.fbx", -3.6f, 9.9f, 180f, new Vector2(1.8f, 0.9f), keep, used);
                    At(r, P + "Chair.fbx", -4.4f, 10.9f, 200f, new Vector2(0.3f, 0.3f), keep, used);
                    At(r, P + "Chair.fbx", -2.6f, 10.9f, 160f, new Vector2(0.3f, 0.3f), keep, used);
                    AlongWall(r, 'N', cabinets, 0.6f, 0.65f, 0.1f, keep, used, 0.7);
                    AlongWall(r, 'W', new[] { E + "WaitingBench.fbx" }, 2.2f, 0.6f, 1.0f, keep, used);
                    foreach (var (px, pz) in new[] { (-7.4f, 0.6f), (7.4f, 0.6f), (7.4f, 11.4f) }) At(r, M + "PottedPlant.fbx", px, pz, 0, new Vector2(0.4f, 0.4f), keep, used);
                    Clutter(r, keep, used, 4);
                    break;
                case "office": case "offices":
                    Pods(r, keep, used);
                    AlongWall(r, 'N', cabinets, 0.6f, 0.65f, 0.05f, keep, used, 0.6);
                    AlongWall(r, 'W', new[] { O + "Whiteboard.fbx", O + "WaterCooler.fbx", O + "CoatRack.fbx", P + "Shelf.fbx" }, 1.3f, 0.5f, 1.2f, keep, used, 0.6);
                    Clutter(r, keep, used, 4);
                    break;
                case "armory":
                    AlongWall(r, 'S', new[] { S + "GunRack.fbx" }, 1.6f, 0.4f, 0.4f, keep, used);
                    AlongWall(r, 'E', new[] { S + "GunRack.fbx", Bm + "MetalRack.fbx" }, 2.4f, 0.8f, 0.4f, keep, used);
                    AlongWall(r, 'N', lockers, 0.6f, 0.55f, 0.02f, keep, used, 0.9);
                    At(r, Bm + "Workbench.fbx", cx, cz - 0.5f, 0f, new Vector2(1.0f, 0.5f), keep, used);
                    Clutter(r, keep, used, 3);
                    break;
                case "corridor":
                    {
                        bool alongX = r.r.width > r.r.height;
                        float len = alongX ? r.r.width : r.r.height;
                        string[] items = { E + "WaitingBench.fbx", M + "PottedPlant.fbx", O + "TrashBin.fbx", P + "Crate.fbx", P + "Barrel.fbx", O + "WaterCooler.fbx", P + "FilingCabinet.fbx" };
                        for (float a = 2.5f; a < len - 2f; a += Rn(5f, 9f))
                        {
                            bool sideA = Chance(0.5);
                            string it = items[rng.Next(items.Length)];
                            float d = it.Contains("Bench") ? 0.6f : 0.5f, l = it.Contains("Bench") ? 2.2f : 0.7f;
                            float fixedC = alongX ? (sideA ? z0 + Wall + d / 2f : z1 - Wall - d / 2f) : (sideA ? x0 + Wall + d / 2f : x1 - Wall - d / 2f);
                            float along = (alongX ? x0 : z0) + a;
                            float yaw = alongX ? (sideA ? 0f : 180f) : (sideA ? 90f : -90f);
                            var p = alongX ? new Vector2(along, fixedC) : new Vector2(fixedC, along);
                            var half = alongX ? new Vector2(l / 2f, d / 2f) : new Vector2(d / 2f, l / 2f);
                            if (!Free(r, p, half, keep, used)) continue;
                            used.Add(Rect.MinMaxRect(p.x - half.x, p.y - half.y, p.x + half.x, p.y + half.y));
                            Place(it, new Vector3(p.x, y, p.y), yaw);
                            if (Chance(0.5)) Place(A + "PaperScatter.fbx", new Vector3(p.x + (alongX ? Rn(-1, 1) : 0), y, p.y + (alongX ? 0 : Rn(-1, 1))), Rn(0, 360), false);
                        }
                    }
                    break;
                case "garage":
                    At(r, E + "PoliceCar.fbx", cx + 1.2f, cz + 1.0f, 90f + Rn(-8, 8), new Vector2(2.4f, 1.1f), keep, used);
                    AlongWall(r, 'W', new[] { Bm + "Workbench.fbx", Bm + "MetalRack.fbx" }, 2.4f, 0.9f, 0.6f, keep, used);
                    AlongWall(r, 'N', new[] { S + "TireStack.fbx", P + "Barrel.fbx", Bm + "MetalRack.fbx" }, 1.5f, 0.8f, 0.5f, keep, used, 0.8);
                    Clutter(r, keep, used, 3);
                    break;
                case "darkroom": case "evidence":
                    for (float x = x0 + 2.2f; x < x1 - 1.5f; x += 3.0f) for (float z = z0 + 3.2f; z < z1 - 1.5f; z += 4.4f) At(r, Bm + "MetalRack.fbx", x, z, 90f, new Vector2(1.2f, 0.4f), keep, used);
                    AlongWall(r, 'E', new[] { A + "ArchiveShelfA.fbx", A + "ArchiveShelfB.fbx" }, 1.8f, 0.55f, 0.2f, keep, used, 0.8);
                    At(r, P + "Desk.fbx", x0 + 1.3f, z1 - 0.9f, 180f, new Vector2(0.75f, 0.4f), keep, used);
                    Clutter(r, keep, used, 4);
                    break;
                case "restroom":
                    AlongWall(r, r.r.width > r.r.height ? 'N' : 'E', new[] { S + "RestroomStall.fbx" }, 1.05f, 1.6f, 0.02f, keep, used, 0.95, 0.4f);
                    AlongWall(r, r.r.width > r.r.height ? 'S' : 'W', new[] { S + "SinkCounter.fbx" }, 2.4f, 0.6f, 0.6f, keep, used, 1, 0.6f);
                    At(r, O + "TrashBin.fbx", x0 + 0.5f, z1 - 0.5f, 0, new Vector2(0.2f, 0.2f), keep, used);
                    Clutter(r, keep, used, 2);
                    break;
                case "elevator":
                    At(r, E + "WaitingBench.fbx", x0 + 0.5f, cz, 90f, new Vector2(1.1f, 0.3f), keep, used);
                    At(r, M + "PottedPlant.fbx", x0 + 0.6f, z1 - 0.6f, 0, new Vector2(0.4f, 0.4f), keep, used);
                    break;
                case "security":
                    At(r, S + "CCTVDesk.fbx", cx, z1 - 0.65f, 180f, new Vector2(1.2f, 0.5f), keep, used);
                    At(r, O + "SwivelChair.fbx", cx - 0.5f, z1 - 1.6f, 170f, new Vector2(0.3f, 0.3f), keep, used);
                    At(r, O + "SwivelChair.fbx", cx + 0.6f, z1 - 1.5f, 200f, new Vector2(0.3f, 0.3f), keep, used);
                    AlongWall(r, 'E', lockers, 0.6f, 0.55f, 0.02f, keep, used, 0.9);
                    AlongWall(r, 'W', cabinets, 0.6f, 0.65f, 0.05f, keep, used, 0.7);
                    Desk(new Vector3(cx - 2.5f, y, cz - 1.5f), 0f); used.Add(Rect.MinMaxRect(cx - 3.3f, cz - 2.6f, cx - 1.7f, cz - 0.5f));
                    Clutter(r, keep, used, 3);
                    break;
                case "breakroom": case "lounge":
                    At(r, S + "KitchenCounter.fbx", cx, z1 - 0.5f, 180f, new Vector2(1.2f, 0.35f), keep, used);
                    AlongWall(r, 'E', new[] { S + "VendingMachine.fbx" }, 1.0f, 0.85f, 0.3f, keep, used, 0.7);
                    AlongWall(r, 'W', new[] { S + "Couch.fbx" }, 2.3f, 0.95f, 1.0f, keep, used, 0.9);
                    if (At(r, P + "Desk.fbx", cx, cz, 0f, new Vector2(0.8f, 0.45f), keep, used) != null)
                        foreach (var dz in new[] { -0.9f, 0.9f }) Place(P + "Chair.fbx", new Vector3(cx + Rn(-0.3f, 0.3f), y, cz + dz), dz < 0 ? Rn(-30, 30) : 180f + Rn(-30, 30));
                    Clutter(r, keep, used, 3);
                    break;
                case "lockers":
                    AlongWall(r, 'N', lockers, 0.62f, 0.55f, 0.0f, keep, used, 0.97);
                    AlongWall(r, 'E', lockers, 0.62f, 0.55f, 0.0f, keep, used, 0.97);
                    for (float x = x0 + 2f; x < x1 - 2f; x += 0.62f) { At(r, P + "Locker.fbx", x, cz - 0.3f, 180f, new Vector2(0.3f, 0.28f), keep, used); At(r, P + "Locker.fbx", x, cz + 0.3f, 0f, new Vector2(0.3f, 0.28f), keep, used); }
                    for (float x = x0 + 3f; x < x1 - 2f; x += 4.5f) { At(r, E + "WaitingBench.fbx", x, cz - 2.2f, 0f, new Vector2(1.1f, 0.3f), keep, used); At(r, E + "WaitingBench.fbx", x, cz + 2.4f, 180f, new Vector2(1.1f, 0.3f), keep, used); }
                    Clutter(r, keep, used, 2);
                    break;
                case "cells":
                    Cells(r, keep, used);
                    break;
                case "stairwell": case "servicestair":
                    At(r, P + "Crate.fbx", x1 - 0.7f, z1 - 0.7f, Rn(0, 90), new Vector2(0.5f, 0.5f), keep, used);
                    At(r, P + "Barrel.fbx", x1 - 0.6f, z0 + 2.5f, 0, new Vector2(0.35f, 0.35f), keep, used);
                    break;
                case "storage":
                    for (float x = x0 + 2.0f; x < x1 - 1.5f; x += 3.2f) for (float z = z0 + 2.6f; z < z1 - 1.5f; z += 3.6f) At(r, Bm + "MetalRack.fbx", x, z, 90f, new Vector2(1.2f, 0.4f), keep, used);
                    AlongWall(r, 'N', new[] { Bm + "MetalRack.fbx" }, 2.4f, 0.8f, 0.3f, keep, used, 0.8);
                    Clutter(r, keep, used, 5);
                    break;
                case "workshop":
                    AlongWall(r, 'N', new[] { Bm + "Workbench.fbx", Bm + "MetalRack.fbx" }, 2.4f, 0.9f, 0.5f, keep, used);
                    AlongWall(r, 'E', new[] { Bm + "PipeValves.fbx", S + "TireStack.fbx", P + "Barrel.fbx" }, 1.9f, 0.6f, 0.6f, keep, used, 0.8);
                    At(r, Bm + "Generator.fbx", cx, cz, 0f, new Vector2(1.35f, 0.6f), keep, used);
                    Clutter(r, keep, used, 4);
                    break;
                case "commissioner":
                    At(r, O + "Rug.fbx", cx, cz, 90f, new Vector2(1.5f, 1.0f), keep, new List<Rect>(), false);
                    At(r, O + "ExecutiveDesk.fbx", cx - 2.2f, cz, -90f, new Vector2(1.0f, 0.5f), keep, used);
                    At(r, O + "ExecutiveChair.fbx", cx - 3.2f, cz + 0.2f, 90f, new Vector2(0.35f, 0.35f), keep, used);
                    At(r, P + "Chair.fbx", cx - 0.9f, cz - 0.7f, -90f, new Vector2(0.3f, 0.3f), keep, used);
                    At(r, P + "Chair.fbx", cx - 0.9f, cz + 0.7f, -90f, new Vector2(0.3f, 0.3f), keep, used);
                    OnTop(O + "DeskComputer.fbx", new Vector3(cx - 2.25f, y + 0.78f, cz + 0.55f), -90f);
                    OnTop(O + "DeskLamp.fbx", new Vector3(cx - 2.5f, y + 0.78f, cz - 0.8f), -120f);
                    AlongWall(r, 'W', new[] { O + "Bookcase.fbx" }, 1.25f, 0.45f, 0.0f, keep, used, 0.9);
                    AlongWall(r, 'S', new[] { O + "TrophyCabinet.fbx", O + "Bookcase.fbx" }, 1.25f, 0.5f, 0.3f, keep, used, 0.7);
                    At(r, S + "Couch.fbx", x1 - 1.0f, cz + 2.5f, -90f, new Vector2(1.15f, 0.5f), keep, used);
                    break;
                case "chief":
                    At(r, O + "ExecutiveDesk.fbx", cx + 1.0f, cz, 90f, new Vector2(1.0f, 0.5f), keep, used);
                    At(r, O + "ExecutiveChair.fbx", cx + 2.0f, cz, -90f, new Vector2(0.35f, 0.35f), keep, used);
                    OnTop(O + "Laptop.fbx", new Vector3(cx + 1.0f, y + 0.78f, cz + 0.3f), 90f);
                    AlongWall(r, 'N', new[] { O + "Bookcase.fbx", P + "FilingCabinet.fbx" }, 1.25f, 0.5f, 0.2f, keep, used, 0.9);
                    AlongWall(r, 'E', lockers, 0.62f, 0.55f, 0.0f, keep, used, 0.6);
                    At(r, S + "CCTVDesk.fbx", cx, z0 + 0.8f, 0f, new Vector2(1.2f, 0.5f), keep, used);
                    Clutter(r, keep, used, 3);
                    break;
                case "conference":
                    At(r, O + "Lectern.fbx", x0 + 1.6f, cz, 90f, new Vector2(0.35f, 0.35f), keep, used);
                    At(r, O + "Whiteboard.fbx", x0 + 0.5f, cz + 2.2f, 90f, new Vector2(0.7f, 0.3f), keep, used);
                    for (float x = x0 + 3.6f; x < x1 - 1.2f; x += 1.0f)
                        for (int side = 0; side < 2; side++)
                            for (float z = side == 0 ? z0 + 0.9f : cz + 1.0f; side == 0 ? z < cz - 0.9f : z < z1 - 0.7f; z += 0.64f)
                            {
                                if (Chance(0.07)) continue;
                                var ch = At(r, O + "AuditoriumChair.fbx", x, z, -90f, new Vector2(0.28f, 0.28f), keep, used);
                                if (ch != null && Chance(0.07)) { ch.transform.rotation = Quaternion.Euler(0, Rn(0, 360), 0) * Quaternion.Euler(Rn(80, 95), 0, 0); ch.transform.position += Vector3.up * 0.25f; }
                            }
                    break;
                case "memorial":
                    Memorial(r, keep, used);
                    break;
                case "library":
                    for (float z = z0 + 3.2f; z < z1 - 1.8f; z += 2.6f)
                        for (float x = x0 + 1.9f; x < cx - 0.5f; x += 1.25f) { At(r, O + "Bookcase.fbx", x, z - 0.25f, 180f, new Vector2(0.62f, 0.23f), keep, used); At(r, O + "Bookcase.fbx", x, z + 0.25f, 0f, new Vector2(0.62f, 0.23f), keep, used); }
                    AlongWall(r, 'N', new[] { O + "Bookcase.fbx" }, 1.25f, 0.45f, 0.0f, keep, used, 0.9);
                    foreach (var dz in new[] { -2.5f, 2.0f })
                        if (At(r, P + "Desk.fbx", x1 - 2.6f, cz + dz, 0f, new Vector2(0.8f, 0.45f), keep, used) != null)
                        { OnTop(O + "DeskLamp.fbx", new Vector3(x1 - 2.9f, y + 0.76f, cz + dz), Rn(0, 360)); Place(P + "Chair.fbx", new Vector3(x1 - 2.6f, y, cz + dz - 0.9f), Rn(-30, 30)); }
                    At(r, A + "CardCatalog.fbx", x1 - 0.5f, z0 + 2.6f, -90f, new Vector2(0.5f, 0.3f), keep, used);
                    At(r, A + "RollingLadder.fbx", cx - 0.2f, cz, 90f, new Vector2(0.3f, 0.3f), keep, used);
                    Clutter(r, keep, used, 2);
                    break;
                case "interrogation":
                    foreach (var dz in new[] { -3.5f, 3.5f })
                    {
                        if (At(r, P + "Desk.fbx", cx, cz + dz, 0f, new Vector2(0.8f, 0.45f), keep, used) == null) continue;
                        Place(P + "Chair.fbx", new Vector3(cx, y, cz + dz - 0.9f), Rn(-15, 15)); Place(P + "Chair.fbx", new Vector3(cx, y, cz + dz + 0.9f), 180f + Rn(-15, 15));
                        OnTop(O + "DeskLamp.fbx", new Vector3(cx + 0.5f, y + 0.76f, cz + dz), Rn(0, 360));
                    }
                    AlongWall(r, 'W', cabinets, 0.6f, 0.65f, 0.1f, keep, used, 0.5);
                    Clutter(r, keep, used, 2);
                    break;
                case "records":
                    for (float z = z0 + 3.4f; z < z1 - 2f; z += 3.0f)
                        for (float x = x0 + 1.6f; x < x1 - 1.4f; x += 1.85f) { At(r, A + "ArchiveShelf" + "ABC"[rng.Next(3)] + ".fbx", x, z - 0.26f, 180f, new Vector2(0.9f, 0.25f), keep, used); At(r, A + "ArchiveShelf" + "ABC"[rng.Next(3)] + ".fbx", x, z + 0.26f, 0f, new Vector2(0.9f, 0.25f), keep, used); }
                    Clutter(r, keep, used, 3);
                    break;
                case "ante":
                    AlongWall(r, 'E', new[] { E + "WaitingBench.fbx" }, 2.2f, 0.6f, 1.2f, keep, used, 0.8);
                    At(r, P + "Desk.fbx", x1 - 2.0f, z1 - 1.2f, 180f, new Vector2(0.8f, 0.45f), keep, used);
                    At(r, M + "PottedPlant.fbx", x1 - 0.6f, z0 + 0.6f, 0, new Vector2(0.4f, 0.4f), keep, used);
                    Clutter(r, keep, used, 3);
                    break;
                case "archive":
                    Archive(r, keep, used);
                    break;
                case "shed":
                    At(r, Bm + "ControlPanel.fbx", cx, z1 - 0.5f, 180f, new Vector2(1.5f, 0.35f), keep, used);
                    At(r, P + "Crate.fbx", x0 + 0.6f, z0 + 1.4f, 20f, new Vector2(0.5f, 0.5f), keep, used);
                    break;
                case "roof":
                    if (r.r.width * r.r.height > 100f)
                        for (int i = 0; i < 3; i++) At(r, Bm + "Generator.fbx", Rn(x0 + 3, x1 - 3), Rn(z0 + 3, z1 - 3), Chance(0.5) ? 0f : 90f, new Vector2(1.4f, 1.4f), keep, used);
                    break;
                case "pumps":
                    for (float x = x0 + 2.5f; x < x1 - 2f; x += 3.4f) At(r, Bm + "WaterPump.fbx", x, cz - 1f, 0f, new Vector2(1.0f, 0.4f), keep, used);
                    AlongWall(r, 'S', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 1.6f, keep, used);
                    AlongWall(r, 'W', new[] { P + "Barrel.fbx" }, 0.7f, 0.7f, 0.1f, keep, used, 0.6);
                    break;
                case "tanks":
                    foreach (var tx in new[] { x0 + 3f, cx, x1 - 3f })
                    {
                        var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder); t.name = "Deposito"; t.transform.SetParent(fixedRoot);
                        t.transform.position = new Vector3(tx, y + 1.5f, cz - 1.0f); t.transform.localScale = new Vector3(2.4f, 1.5f, 2.4f);
                        t.GetComponent<Renderer>().sharedMaterial = metal; t.isStatic = true; used.Add(Rect.MinMaxRect(tx - 1.3f, cz - 2.3f, tx + 1.3f, cz + 0.3f));
                    }
                    AlongWall(r, 'S', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 1.5f, keep, used);
                    break;
                case "machines":
                    for (float x = x0 + 2.8f; x < x1 - 2.5f; x += 4.6f) At(r, Bm + "Generator.fbx", x, cz - 0.5f, 0f, new Vector2(1.35f, 0.6f), keep, used);
                    AlongWall(r, 'S', new[] { Bm + "ControlPanel.fbx" }, 3.0f, 0.65f, 0.6f, keep, used);
                    AlongWall(r, 'E', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 1.0f, keep, used);
                    break;
                case "lab":
                    for (float x = x0 + 2.5f; x < x1 - 2f; x += 3.6f) At(r, Bm + "LabBench.fbx", x, cz, 90f, new Vector2(1.2f, 0.42f), keep, used);
                    AlongWall(r, 'N', new[] { Bm + "LabCabinet.fbx" }, 1.0f, 0.48f, 0.1f, keep, used, 0.9);
                    AlongWall(r, 'E', new[] { Bm + "LabCabinet.fbx", P + "FilingCabinet.fbx" }, 1.0f, 0.65f, 0.2f, keep, used, 0.8);
                    Clutter(r, keep, used, 3);
                    break;
                case "fuse":
                    AlongWall(r, 'N', new[] { Bm + "ControlPanel.fbx" }, 3.0f, 0.65f, 0.4f, keep, used);
                    AlongWall(r, 'W', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 1.2f, keep, used);
                    break;
                case "control":
                    At(r, S + "CCTVDesk.fbx", cx, z1 - 0.65f, 180f, new Vector2(1.2f, 0.5f), keep, used);
                    AlongWall(r, 'W', new[] { Bm + "ControlPanel.fbx" }, 3.0f, 0.65f, 0.4f, keep, used);
                    AlongWall(r, 'E', new[] { Bm + "ControlPanel.fbx" }, 3.0f, 0.65f, 0.4f, keep, used);
                    Pods(r, keep, used, 3.0f);
                    break;
                case "pipes":
                    // galeria: hileras de armarios y bancos de tuberias que hacen pasillos
                    for (float x = x0 + 3.5f; x < x1 - 2f; x += 5.0f)
                        for (float z = z0 + 3.5f; z < z1 - 2f; z += 5.5f) At(r, Chance(0.5) ? Bm + "ControlPanel.fbx" : Bm + "MetalRack.fbx", x, z, Chance(0.5) ? 0f : 90f, new Vector2(1.5f, 0.4f), keep, used);
                    AlongWall(r, 'N', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 0.8f, keep, used);
                    AlongWall(r, 'S', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 2.0f, keep, used, 0.7);
                    Clutter(r, keep, used, 4);
                    break;
                case "boiler":
                    Place(Bm + "Boiler.fbx", new Vector3(-16.5f, y, 39.0f), 0f, true, false, fixedRoot, 0f);
                    used.Add(Rect.MinMaxRect(-19.2f, 37.5f, -13.8f, 40.6f));
                    foreach (var (bx, bz, yaw, path) in new[] { (-28.5f, 33.5f, 90f, Bm + "ControlPanel.fbx"), (-14.5f, 33.0f, 90f, Bm + "Generator.fbx"), (-28.5f, 41.0f, 90f, Bm + "ControlPanel.fbx"), (-21.5f, 42.8f, 0f, Bm + "Generator.fbx"), (-26.0f, 37.6f, 0f, Bm + "WaterPump.fbx") })
                        At(r, path, bx, bz, yaw, path.Contains("Generator") ? new Vector2(1.35f, 0.6f) : new Vector2(1.5f, 0.35f), keep, used);
                    AlongWall(r, 'N', new[] { Bm + "PipeValves.fbx" }, 1.9f, 0.4f, 2.4f, keep, used);
                    AlongWall(r, 'E', new[] { Bm + "PipeValves.fbx", P + "Barrel.fbx" }, 1.9f, 0.4f, 2.0f, keep, used, 0.8);
                    break;
            }
        }

        static void Cells(ComisariaGrande.Room r, List<Rect> keep, List<Rect> used)
        {
            float x0 = r.r.xMin, x1 = r.r.xMax, z1 = r.r.yMax, y = r.floor, cz = z1 - 5.8f;      // rejas a 5,8 m de la pared del fondo
            int n = 4; float w = (x1 - x0) / n;
            var bars = new GameObject("Celdas").transform; bars.SetParent(fixedRoot);
            var doorsX = Enumerable.Range(0, n).Select(i => x0 + w * (i + 0.5f)).ToArray();
            for (float x = x0 + 0.2f; x < x1 - 0.1f; x += 0.16f)
            {
                if (doorsX.Any(d => Mathf.Abs(x - d) < 0.7f)) continue;
                ComisariaGrande.Box("Barrote", bars, new Vector3(x, y + 1.3f, cz), new Vector3(0.035f, 2.6f, 0.035f), metal, 0f, false);
            }
            foreach (float yy in new[] { 0.06f, 1.2f, 2.55f }) ComisariaGrande.Box("Travesano", bars, new Vector3((x0 + x1) / 2f, y + yy, cz), new Vector3(x1 - x0 - 0.2f, 0.06f, 0.06f), metal, 0f, false);
            var xs = new List<float> { x0 + 0.1f }; foreach (var d in doorsX) { xs.Add(d - 0.66f); xs.Add(d + 0.66f); } xs.Add(x1 - 0.1f);
            for (int i = 0; i + 1 < xs.Count; i += 2) { var c = new GameObject("Reja_Col"); c.transform.SetParent(bars); c.transform.position = new Vector3((xs[i] + xs[i + 1]) / 2f, y + 1.3f, cz); c.AddComponent<BoxCollider>().size = new Vector3(xs[i + 1] - xs[i], 2.6f, 0.12f); c.isStatic = true; }
            for (int i = 1; i < n; i++) ComisariaGrande.Box("Celda_Muro", bars, new Vector3(x0 + w * i, y + 1.6f, (cz + z1) / 2f), new Vector3(0.2f, 3.2f, z1 - cz), AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wall.mat"), 3f);
            for (int i = 0; i < n; i++) { Place(P + "Cot.fbx", new Vector3(x0 + w * (i + 0.5f), y, z1 - 1.0f), 0f); used.Add(Rect.MinMaxRect(x0 + w * i, cz, x0 + w * (i + 1), z1)); }
            used.Add(Rect.MinMaxRect(x0, cz - 0.3f, x1, cz + 0.3f));
            At(r, P + "Desk.fbx", x0 + 2.5f, r.r.yMin + 3.0f, 0f, new Vector2(0.8f, 0.45f), keep, used);
            At(r, P + "Chair.fbx", x0 + 2.5f, r.r.yMin + 2.1f, 0f, new Vector2(0.3f, 0.3f), keep, used);
            AlongWall(r, 'E', new[] { P + "FilingCabinet.fbx", P + "Locker.fbx" }, 0.62f, 0.6f, 0.1f, keep, used, 0.6);
            Clutter(r, keep, used, 3);
        }

        static void Memorial(ComisariaGrande.Room r, List<Rect> keep, List<Rect> used)
        {
            float y = r.floor; var c = new Vector3(-4.0f, y, 6.0f);
            var mon = Place(M + "MemorialMonument.fbx", c, 90f, true, false, null, 0f); mon.name = "Memorial_Monumento";
            used.Add(Rect.MinMaxRect(-6.4f, 3.4f, -1.6f, 8.6f));
            float px0 = -6.2f, px1 = -1.8f, pz0 = 3.6f, pz1 = 8.4f;
            var posts = new[] { new Vector3(px1, y, pz0), new Vector3((px0 + px1) / 2f, y, pz0), new Vector3(px0, y, pz0), new Vector3(px0, y, (pz0 + pz1) / 2f), new Vector3(px0, y, pz1), new Vector3((px0 + px1) / 2f, y, pz1), new Vector3(px1, y, pz1) };
            foreach (var p in posts) Place(M + "Stanchion.fbx", p, 0f, true, false, null, 0f);
            for (int i = 0; i < posts.Length - 1; i++)
            {
                Vector3 a = posts[i], b = posts[i + 1], d = b - a; int n = Mathf.Max(1, Mathf.RoundToInt(d.magnitude / 1.5f));
                for (int k = 0; k < n; k++) { var rope = Place(M + "StanchionRope.fbx", a + d * ((k + 0.5f) / n), Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, false, false, null, 0f); rope.transform.localScale = new Vector3(1f, 1f, d.magnitude / n / 1.5f); }
            }
            Place(M + "FlowerWreath.fbx", new Vector3(-2.2f, y, 4.4f), 70f, true, false, null, 0.5f);
            Place(M + "FlowerWreath.fbx", new Vector3(-2.2f, y, 7.6f), 110f, true, false, null, 0.5f);
            Place(M + "Candles.fbx", new Vector3(-2.3f, y, 5.5f), Rn(0, 360), false, false, null, 0.5f);
            Place(M + "Candles.fbx", new Vector3(-2.25f, y, 6.6f), Rn(0, 360), false, false, null, 0.5f);
            for (int i = 0; i < 6; i++) Place(M + "MemorialPlaque.fbx", new Vector3(-7.0f + i * 2.4f, y + 1.15f, 0.25f), 0f, false, false, null, 0.3f);
            foreach (float z in new[] { 2.0f, 3.6f, 9.0f, 10.6f }) Place(M + "MemorialPlaque.fbx", new Vector3(-7.75f, y + 1.15f, z), 90f, false, false, null, 0.3f);
            At(r, E + "WaitingBench.fbx", -0.6f, 2.0f, -90f, new Vector2(1.1f, 0.3f), keep, used);
            At(r, E + "WaitingBench.fbx", -0.6f, 10.4f, -90f, new Vector2(1.1f, 0.3f), keep, used);
            foreach (var (px, pz) in new[] { (-7.4f, 0.6f), (7.4f, 0.6f), (7.4f, 11.4f), (-7.4f, 11.4f) }) At(r, M + "PottedPlant.fbx", px, pz, Rn(0, 360), new Vector2(0.4f, 0.4f), keep, used);
            At(r, O + "TrophyCabinet.fbx", 6.0f, 11.5f, 180f, new Vector2(0.7f, 0.3f), keep, used);
        }

        /// <summary>
        /// Archivo (x -4..32, z 14,5..44): un laberinto de estanterias de obra fijas (pasillos de ~2,2 m) alrededor de una sala
        /// central despejada (x 8..22, z 22..36) donde espera el jefe 1; en los pasillos, estanterias exentas que el jefe revienta.
        /// </summary>
        static void Archive(ComisariaGrande.Room r, List<Rect> keep, List<Rect> used)
        {
            float y = r.floor;
            var arena = Rect.MinMaxRect(8f, 22f, 22f, 36f);
            used.Add(arena);
            string wsh = A + "ArchiveWallShelf.fbx";
            // estanterias de obra a lo largo de las paredes
            for (float x = r.r.xMin + 2.3f; x < r.r.xMax - 2f; x += 4.05f) { At(r, wsh, x, r.r.yMax - 0.45f, 180f, new Vector2(2.0f, 0.25f), keep, used); At(r, wsh, x, r.r.yMin + 0.45f, 0f, new Vector2(2.0f, 0.25f), keep, used); }
            for (float z = r.r.yMin + 2.5f; z < r.r.yMax - 2f; z += 4.05f) At(r, wsh, r.r.xMax - 0.45f, z, -90f, new Vector2(2.0f, 0.25f), keep, used);
            foreach (Transform t in props.Cast<Transform>().Where(t => t.name == "ArchiveWallShelf").ToList()) t.SetParent(fixedRoot, true);
            // muros de estanterias fijas que hacen pasillos: anillo alrededor de la arena con aberturas, y lineas en el oeste
            void FixedLine(float xa, float za, float xb, float zb)
            {
                bool alongX = Mathf.Abs(xb - xa) > Mathf.Abs(zb - za); float len = alongX ? Mathf.Abs(xb - xa) : Mathf.Abs(zb - za);
                int n = Mathf.FloorToInt(len / 4.05f);
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) / n; float x = Mathf.Lerp(xa, xb, t), z = Mathf.Lerp(za, zb, t);
                    var g = Place(wsh, new Vector3(x, y, z), alongX ? 0f : 90f, true, true, fixedRoot, 0.2f);
                    used.Add(alongX ? Rect.MinMaxRect(x - 2f, z - 0.3f, x + 2f, z + 0.3f) : Rect.MinMaxRect(x - 0.3f, z - 2f, x + 0.3f, z + 2f));
                }
            }
            FixedLine(6f, 20f, 14.1f, 20f); FixedLine(16.9f, 20f, 25f, 20f);            // sur del anillo, con abertura en x 15,5
            FixedLine(6f, 38f, 14.1f, 38f); FixedLine(18.9f, 38f, 27f, 38f);            // norte, abertura en x 16,5
            FixedLine(6f, 22.2f, 6f, 26.3f); FixedLine(6f, 31.2f, 6f, 35.3f);           // oeste, abertura en z 28,7
            FixedLine(24f, 22.2f, 24f, 30.3f);                                          // este, abertura al norte
            FixedLine(1f, 18f, 1f, 30.2f); FixedLine(1f, 34f, 1f, 42f);                 // linea del oeste (entrada en z 36 por la puerta)
            // pasillos con estanterias exentas rompibles (filas a lo largo)
            foreach (var (xa, xb, z) in new[] { (2.5f, 13.5f, 17.0f), (17.5f, 29.5f, 17.0f), (2.5f, 13f, 41.0f), (19f, 29f, 41.0f) })
                for (float x = xa; x < xb; x += 1.85f) At(r, A + "ArchiveShelf" + "ABC"[rng.Next(3)] + ".fbx", x, z, Chance(0.5) ? 0f : 180f, new Vector2(0.9f, 0.27f), keep, used);
            foreach (var (x, za, zb) in new[] { (27.5f, 22f, 36f), (3.5f, 20f, 28f) })
                for (float z = za; z < zb; z += 1.85f) At(r, A + "ArchiveShelf" + "ABC"[rng.Next(3)] + ".fbx", x, z, Chance(0.5) ? 90f : -90f, new Vector2(0.9f, 0.27f), keep, used);
            // la arena: cajas, papeles, carritos, una estanteria volcada y la mesa del archivero
            used.Remove(arena);
            var fallen = At(r, A + "ArchiveShelfC.fbx", 12.0f, 32.0f, 0f, new Vector2(1.3f, 1.3f), keep, used);
            if (fallen != null) { fallen.transform.rotation = Quaternion.Euler(0, 32f, 0) * Quaternion.Euler(0, 0, 88f); fallen.transform.position += Vector3.up * 0.27f; Pickup.FitBoxCollider(fallen); }
            foreach (var (bx, bz) in new[] { (10.5f, 25.0f), (19.5f, 33.5f), (20.5f, 24.5f) }) At(r, A + "ArchiveBoxStack" + (Chance(0.5) ? "A" : "B") + ".fbx", bx, bz, Rn(0, 360), new Vector2(0.75f, 0.65f), keep, used);
            At(r, A + "ArchiveCart.fbx", 14.0f, 26.0f, Rn(0, 360), new Vector2(0.5f, 0.4f), keep, used);
            At(r, P + "Desk.fbx", 17.0f, 34.6f, 180f, new Vector2(0.8f, 0.45f), keep, used);
            for (int i = 0; i < 6; i++) Place(A + "PaperScatter.fbx", new Vector3(Rn(9f, 21f), y, Rn(23f, 35f)), Rn(0, 360), false);
            Clutter(r, keep, used, 8);
        }

        // ------------------------------------------------------------------ entrada
        [MenuItem("Horror/Comisaria grande/3 Mobiliario")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("--- COMISARIA V2 ---");
            if (rootGo == null) return "falta la fase A";
            var level = rootGo.transform;
            foreach (var n in new[] { "Props/Mobiliario", "Mobiliario_Fijo" }) { var o = level.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
            props = new GameObject("Mobiliario").transform; props.SetParent(level.Find("Props"));
            fixedRoot = new GameObject("Mobiliario_Fijo").transform; fixedRoot.SetParent(level);
            metal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Metal.mat"); wood = AssetDatabase.LoadAssetAtPath<Material>(Mats + "Env_Wood.mat");
            rng = new System.Random(2026); count = 0;
            ItemTextureKit.Apply();
            var log = new List<string>();
            foreach (var r in ComisariaGrande.Rooms)
            {
                int before = count;
                try { Furnish(r); } catch (Exception e) { log.Add(r.id + ": " + e.Message); }
            }
            var surface = rootGo.GetComponent<NavMeshSurface>(); var rt = rootGo.GetComponent<RuntimeNavMesh>();
            Physics.SyncTransforms();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(false);
            surface.BuildNavMesh();
            foreach (var l in rt.disableDuringBake) if (l != null) l.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase C: " + count + " muebles | " + string.Join(" | ", log);
        }
    }
}
#endif
