using UnityEngine;

namespace Horror
{
    /// <summary>Apunta en <see cref="MapMemory"/> la sala (y las piezas de su grupo) en la que esta el jugador, 4 veces por segundo.</summary>
    [RequireComponent(typeof(MapData))]
    public class MapTracker : MonoBehaviour
    {
        /// <summary>Grupo de salas en el que esta el jugador ahora ("" si ninguno): los zombis de esa sala se lanzan a por el.</summary>
        public static string CurrentGroup { get; private set; } = "";

        MapData data;
        Transform player;
        float next;

        void Awake() => data = GetComponent<MapData>();

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.25f;
            if (player == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc == null) return; player = pc.transform; }
            // pivote del jugador ~1 m sobre los pies
            if (data.TryRoomAt(player.position.x, player.position.z, player.position.y - 1f, out var room)) { CurrentGroup = room.group; MapMemory.Visit(room.group); }
            else CurrentGroup = "";
        }
    }
}
