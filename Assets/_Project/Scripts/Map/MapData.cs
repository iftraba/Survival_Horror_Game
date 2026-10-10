using System;
using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    [Serializable]
    public struct MapRoom
    {
        public string id, label, type, group;
        public float x0, z0, x1, z1, floorY;
        public bool indoor, safe;
        public Vector2 Center => new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
        public bool Contains(float x, float z) => x >= x0 && x <= x1 && z >= z0 && z <= z1;
    }

    [Serializable]
    public struct MapDoor
    {
        public string name;
        public float x, z, floorY, width;
        /// <summary>0 normal, 1 candado, 2 tarjeta de seguridad, 3 tarjeta del jefe, 4 sin corriente, 5 metalica, 6 persiana.</summary>
        public int kind;
    }

    /// <summary>
    /// Datos del plano de la comisaria (salas y puertas con su cota), generados por el kit de editor ComisariaGrandeMapKit (menu 9) a partir de
    /// ComisariaGrande.Define(): en una build no existe el codigo de editor, asi que el plano viaja en la escena. Lo dibuja el menu del inventario (pestana MAPA).
    /// </summary>
    public class MapData : MonoBehaviour
    {
        public MapRoom[] rooms = new MapRoom[0];
        public MapDoor[] doors = new MapDoor[0];
        public static MapData Instance { get; private set; }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>0 sotano, 1 planta baja, 2 primera, 3 segunda (por la cota del suelo; la sala de calderas esta hundida).</summary>
        public static int FloorIndex(float floorY) => floorY < -1f ? 0 : floorY < 2f ? 1 : floorY < 6f ? 2 : 3;
        public static readonly string[] FloorNames = { "SÓTANO", "PLANTA BAJA", "PRIMERA PLANTA", "SEGUNDA PLANTA" };

        /// <summary>Sala que contiene el punto (x,z) a esa altura de pies; null si ninguna.</summary>
        public bool TryRoomAt(float x, float z, float feetY, out MapRoom room)
        {
            room = default; bool found = false; float bestY = -99f;
            foreach (var r in rooms)
                if (r.Contains(x, z) && r.floorY <= feetY + 0.8f && r.floorY > bestY) { bestY = r.floorY; room = r; found = true; }
            return found;
        }
    }

    /// <summary>Salas ya visitadas por el jugador (el mapa solo dibuja esas): se guarda con la partida.</summary>
    public static class MapMemory
    {
        static readonly HashSet<string> visited = new HashSet<string>();
        public static event Action Changed;

        public static bool Has(string group) => visited.Contains(group);
        public static void Visit(string group) { if (!string.IsNullOrEmpty(group) && visited.Add(group)) Changed?.Invoke(); }
        public static List<string> Ids() => new List<string>(visited);
        public static void Clear() { visited.Clear(); Changed?.Invoke(); }
        public static void Restore(IEnumerable<string> ids)
        {
            visited.Clear();
            if (ids != null) foreach (var i in ids) if (!string.IsNullOrEmpty(i)) visited.Add(i);
            Changed?.Invoke();
        }
    }
}
