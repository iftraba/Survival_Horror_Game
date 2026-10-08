using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Monumento del memorial (comisaria v2): tiene tres huecos para los medallones de bronce repartidos por la comisaria.
    /// Al usarlo coloca los que lleve el jugador; con los tres, se abre la reja de la escalera de servicio del archivo (marca
    /// de progreso 'doneFlag', que escucha ServiceGate). Cada medallon colocado queda guardado como marca 'flagPrefix' + indice.
    /// </summary>
    public class MedallionMonument : MonoBehaviour, IInteractable
    {
        public ItemData[] medallions = new ItemData[3];
        [Tooltip("Medallones ya puestos en el monumento (se encienden al colocarlos), en el mismo orden")] public GameObject[] placedVisuals = new GameObject[3];
        public string flagPrefix = "medallon_";
        public string doneFlag = "memorial";
        [TextArea] public string doneMessage = "Al encajar el ultimo medallon se oye una reja subiendo en la sala de ordenadores.";
        [TextArea] public string doneObjective = "Sube al archivo por la escalera de servicio de la sala de ordenadores (primera planta, al fondo).";

        Inventory inv;

        int Placed { get { int n = 0; for (int i = 0; i < medallions.Length; i++) if (Progress.Has(flagPrefix + i)) n++; return n; } }

        bool CanPlace()
        {
            if (inv == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) inv = pc.GetComponent<Inventory>(); }
            if (inv == null) return false;
            for (int i = 0; i < medallions.Length; i++) if (!Progress.Has(flagPrefix + i) && medallions[i] != null && inv.Has(medallions[i])) return true;
            return false;
        }

        public string Prompt => Progress.Has(doneFlag) ? "" : CanPlace() ? "E  Colocar medallon" : "E  Examinar monumento";

        void OnEnable() { Progress.Changed += Refresh; Refresh(); }
        void OnDisable() => Progress.Changed -= Refresh;

        void Refresh()
        {
            for (int i = 0; i < placedVisuals.Length; i++) if (placedVisuals[i] != null) placedVisuals[i].SetActive(Progress.Has(flagPrefix + i));
        }

        public void Interact(GameObject who)
        {
            inv = who.GetComponent<Inventory>();
            int put = 0;
            for (int i = 0; i < medallions.Length; i++)
            {
                if (Progress.Has(flagPrefix + i) || medallions[i] == null || inv == null || !inv.Has(medallions[i])) continue;
                inv.Remove(medallions[i]);
                Progress.Set(flagPrefix + i);
                put++;
            }
            int placed = Placed, missing = medallions.Length - placed;
            if (put == 0)
            {
                Hud.Message(missing == medallions.Length
                    ? "\"En memoria de los agentes caidos en acto de servicio.\" Debajo de la estrella hay tres huecos redondos vacios."
                    : $"Quedan {missing} huecos vacios. Faltan {missing} medallones.");
                return;
            }
            GameAudio.Play(Sfx.DoorUnlock, transform.position);
            if (missing > 0) { Hud.Message($"Colocas el medallon ({placed}/{medallions.Length})."); return; }
            Progress.Set(doneFlag);
            GameAudio.Play(Sfx.Switch, transform.position);
            Hud.Message(doneMessage);
            if (!string.IsNullOrEmpty(doneObjective)) Objectives.Set(doneObjective);
        }
    }
}
