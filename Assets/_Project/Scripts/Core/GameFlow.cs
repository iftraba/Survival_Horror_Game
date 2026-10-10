using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Arranque de la partida: restaura un guardado pendiente o, si no lo hay, fija el objetivo inicial.
    /// Va en Start (no en Awake) para que el resto de la escena ya este inicializado.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [TextArea] public string startObjective = "Busca una llave para abrir la puerta de la sala del fondo.";

        [Header("Dificultad")]
        [Tooltip("Multiplica la velocidad de persecucion de todos los enemigos (zombis y jefes). 1 = la original")]
        [Range(0.5f, 2f)] public float enemySpeedMultiplier = 1.2f;
        [Tooltip("Suelo de velocidad de persecucion (m/s): los zombis que andan mas despacio suben a este valor. 0 = sin suelo")]
        [Range(0f, 3f)] public float minEnemySpeed = 1.0f;

        [Header("Equipo inicial (partida nueva)")]
        [Tooltip("Arma con la que se empieza, ya equipada")] public ItemData startWeapon;
        [Tooltip("Balas cargadas al empezar. 10 = dos zombis normales a cuerpo (4 tiros cada uno) + 2 de margen")]
        public int startMagazine = 10;

        void Start()
        {
            GameState.ResetAll();
            ZombieAI.SpeedMultiplier = enemySpeedMultiplier;
            ZombieAI.MinSpeed = minEnemySpeed;
            NoteArchive.Clear();
            Progress.Clear();
            MapMemory.Clear();
            if (SaveSystem.ApplyPending()) return;
            ItemStorage.Clear();   // partida nueva: baul vacio
            Objectives.Set(startObjective, false);
            GiveStartingKit();
        }

        void GiveStartingKit()
        {
            if (startWeapon == null || startWeapon.weapon == null) return;
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            var inv = pc.GetComponent<Inventory>();
            var wc = pc.GetComponent<WeaponController>();
            if (inv != null && !inv.Has(startWeapon)) inv.TryAdd(startWeapon, 1);
            if (wc != null)
            {
                wc.SetMagazine(startWeapon.weapon, startMagazine);
                wc.Equip(startWeapon.weapon);
            }
        }
    }
}
