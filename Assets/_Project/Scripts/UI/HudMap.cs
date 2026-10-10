using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Pestana MAPA del menu del inventario (tecla M o Q): plano de la comisaria por planta, norte arriba, con niebla de guerra (solo las salas ya
    /// visitadas), la posicion y orientacion del jugador, los puntos de guardado y las puertas con cerradura (cerrada / abierta). Las flechas o A/D cambian de planta.
    /// </summary>
    public partial class Hud
    {
        bool mapTab;
        int mapFloor = -1;
        Dictionary<string, Door> mapDoors;
        SaveTerminal[] mapPhones;

        void OpenMap()
        {
            GameState.SetInventoryOpen(true);
            archiveTab = false; mapTab = true; mapFloor = -1;
            mapDoors = null; mapPhones = null;
            CloseInventoryPanels(); ItemPreview.Get().Show(null);
        }

        void MapKeys(Keyboard kb)
        {
            if (MapData.Instance == null) return;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) mapFloor = Mathf.Max(0, MapFloorNow() - 1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) mapFloor = Mathf.Min(3, MapFloorNow() + 1);
        }

        int MapFloorNow()
        {
            if (mapFloor >= 0) return mapFloor;
            return player != null ? MapData.FloorIndex(player.transform.position.y - 1f) : 1;
        }

        static Color MapRoomColor(MapRoom r, bool here)
        {
            Color c = r.safe ? new Color(0.16f, 0.36f, 0.2f) : !r.indoor ? new Color(0.14f, 0.2f, 0.15f) : r.type == "corridor" ? new Color(0.27f, 0.3f, 0.34f) : new Color(0.2f, 0.22f, 0.27f);
            if (here) c = Color.Lerp(c, new Color(0.55f, 0.5f, 0.25f), 0.35f);
            c.a = 0.96f;
            return c;
        }

        void DrawMap()
        {
            var md = MapData.Instance;
            var tag = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            if (md == null) { GUI.Label(new Rect(0, Screen.height / 2f - 20f, Screen.width, 40f), "Aún no tienes mapa de este lugar.", new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = 18 }); return; }
            if (mapDoors == null)
            {
                mapDoors = new Dictionary<string, Door>();
                foreach (var d in Door.All) { string k = d.transform.parent != null ? d.transform.parent.name : d.name; if (!mapDoors.ContainsKey(k)) mapDoors[k] = d; }
                mapPhones = Object.FindObjectsByType<SaveTerminal>(FindObjectsSortMode.None);
            }

            int floor = MapFloorNow();
            // extension total (todas las plantas, para que el plano no cambie de tamano al cambiar de planta)
            float minX = 1e9f, maxX = -1e9f, minZ = 1e9f, maxZ = -1e9f;
            foreach (var r in md.rooms) { minX = Mathf.Min(minX, r.x0); maxX = Mathf.Max(maxX, r.x1); minZ = Mathf.Min(minZ, r.z0); maxZ = Mathf.Max(maxZ, r.z1); }
            float top = 92f, bottom = 70f, side = 40f;
            float scale = Mathf.Min((Screen.width - side * 2f) / (maxX - minX), (Screen.height - top - bottom) / (maxZ - minZ));
            float ox = Screen.width / 2f - (maxX - minX) * scale / 2f, oy = top + (maxZ - minZ) * scale;      // oy = y de pantalla de z = minZ
            System.Func<float, float, Vector2> P = (x, z) => new Vector2(ox + (x - minX) * scale, oy - (z - minZ) * scale);

            // pestanas de planta
            float bw = 180f, bh = 30f, bx = Screen.width / 2f - (bw * 4f + 18f) / 2f;
            for (int f = 0; f < 4; f++)
                if (TabButton(new Rect(bx + f * (bw + 6f), 54f, bw, bh), MapData.FloorNames[f], f == floor)) mapFloor = f;

            // salas visitadas de la planta
            var room = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Mathf.RoundToInt(scale * 1.15f), 10, 15), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            room.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
            MapRoom here = default; bool inRoom = player != null && md.TryRoomAt(player.transform.position.x, player.transform.position.z, player.transform.position.y - 1f, out here);
            int shown = 0;
            foreach (var r in md.rooms)
            {
                if (MapData.FloorIndex(r.floorY) != floor || !MapMemory.Has(r.group)) continue;
                var a = P(r.x0, r.z1); var b = P(r.x1, r.z0);
                var rect = new Rect(a.x, a.y, b.x - a.x, b.y - a.y);
                Fill(rect, MapRoomColor(r, inRoom && here.group == r.group));
                Frame(rect, new Color(0.75f, 0.78f, 0.82f, 0.55f), 1.5f);
                shown++;
            }
            // etiquetas encima de todo (para que un relleno no las tape), una por grupo
            var labeled = new HashSet<string>();
            foreach (var r in md.rooms)
            {
                if (MapData.FloorIndex(r.floorY) != floor || !MapMemory.Has(r.group) || r.type == "shaft" || r.type == "roof" || !labeled.Add(r.group)) continue;
                var c = P(r.Center.x, r.Center.y); float w = Mathf.Max(90f, (r.x1 - r.x0) * scale);
                GUI.Label(new Rect(c.x - w / 2f, c.y - 22f, w, 44f), r.label, room);
            }
            // puertas con cerradura (solo si alguna de las salas pegadas esta visitada)
            foreach (var d in md.doors)
            {
                if (MapData.FloorIndex(d.floorY) != floor) continue;
                bool seen = false;
                foreach (var r in md.rooms) if (MapData.FloorIndex(r.floorY) == floor && MapMemory.Has(r.group) && r.x0 - 0.3f <= d.x && d.x <= r.x1 + 0.3f && r.z0 - 0.3f <= d.z && d.z <= r.z1 + 0.3f) { seen = true; break; }
                if (!seen) continue;
                var p = P(d.x, d.z);
                bool locked = d.kind != 0 && mapDoors.TryGetValue(d.name, out var dd) && (!dd.IsUnlocked || (d.kind == 4 && dd.Sealed) || d.kind == 6);
                Color dc = d.kind == 0 ? new Color(0.78f, 0.7f, 0.5f, 0.9f) : !locked ? new Color(0.35f, 0.8f, 0.4f, 0.95f) : d.kind == 1 ? new Color(0.95f, 0.6f, 0.15f) : d.kind == 2 ? new Color(0.3f, 0.55f, 1f) : d.kind == 3 ? new Color(1f, 0.8f, 0.2f) : d.kind == 4 ? new Color(0.9f, 0.2f, 0.15f) : new Color(0.7f, 0.7f, 0.75f);
                float s = d.kind == 0 ? 5f : 9f;
                Fill(new Rect(p.x - s / 2f, p.y - s / 2f, s, s), dc);
                if (d.kind != 0) Frame(new Rect(p.x - s / 2f - 1f, p.y - s / 2f - 1f, s + 2f, s + 2f), new Color(0f, 0f, 0f, 0.8f), 1f);
            }
            // puntos de guardado
            foreach (var ph in mapPhones)
            {
                if (ph == null) continue;
                var pos = ph.transform.position;
                if (MapData.FloorIndex(pos.y) != floor || !md.TryRoomAt(pos.x, pos.z, pos.y, out var pr) || !MapMemory.Has(pr.group)) continue;
                var p = P(pos.x, pos.z);
                var box = new Rect(p.x - 9f, p.y - 9f, 18f, 18f);
                Fill(box, new Color(0.15f, 0.55f, 0.25f)); Frame(box, Color.white, 1.5f);
                GUI.Label(box, "G", new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            }
            // jugador (flecha roja que apunta hacia donde mira)
            if (player != null && MapData.FloorIndex(player.transform.position.y - 1f) == floor)
            {
                var p = P(player.transform.position.x, player.transform.position.z);
                float yaw = player.transform.eulerAngles.y;
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(yaw, p);
                Fill(new Rect(p.x - 5f, p.y - 5f, 10f, 10f), new Color(0.95f, 0.15f, 0.1f));
                Fill(new Rect(p.x - 2f, p.y - 15f, 4f, 11f), new Color(0.95f, 0.15f, 0.1f));
                Fill(new Rect(p.x - 5f, p.y - 15f, 10f, 3f), new Color(0.95f, 0.15f, 0.1f));
                GUI.matrix = m;
            }
            if (shown == 0)
                GUI.Label(new Rect(0, Screen.height / 2f - 16f, Screen.width, 32f), "Aún no has visto nada de esta planta.", new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = 17 });

            // leyenda
            float ly = Screen.height - 54f, lx = Screen.width / 2f - 330f;
            var leg = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            leg.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            void Key(Color c, string text, ref float x) { Fill(new Rect(x, ly + 4f, 12f, 12f), c); GUI.Label(new Rect(x + 16f, ly, 150f, 20f), text, leg); x += 24f + text.Length * 7.4f; }
            Key(new Color(0.95f, 0.15f, 0.1f), "tú", ref lx); Key(new Color(0.15f, 0.55f, 0.25f), "guardado", ref lx); Key(new Color(0.95f, 0.6f, 0.15f), "candado", ref lx);
            Key(new Color(0.3f, 0.55f, 1f), "tarjeta azul", ref lx); Key(new Color(1f, 0.8f, 0.2f), "tarjeta dorada", ref lx); Key(new Color(0.9f, 0.2f, 0.15f), "sin corriente", ref lx); Key(new Color(0.35f, 0.8f, 0.4f), "abierta", ref lx);
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, Screen.height - 28f, Screen.width, 22f), "← →  /  A D: cambiar de planta     M: cerrar el mapa     Q: cambiar de pestaña", new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = 13 });
            GUI.color = Color.white;
        }
    }
}
