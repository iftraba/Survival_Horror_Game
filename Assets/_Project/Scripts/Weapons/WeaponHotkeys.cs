using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Atajos de arma: las teclas 1-4 equipan (o guardan, si ya esta equipada) el arma asignada. Las armas se asignan desde el
    /// inventario (arma seleccionada + tecla 1-4; pulsar la misma tecla otra vez la libera). Se guardan en PlayerPrefs por nombre de
    /// objeto y solo funcionan si el arma esta en el inventario. Las armas nuevas toman solas la primera tecla libre.
    /// </summary>
    public static class WeaponHotkeys
    {
        public const int Count = 4;
        static string[] names;

        static void Load()
        {
            if (names != null) return;
            names = new string[Count];
            for (int i = 0; i < Count; i++) names[i] = PlayerPrefs.GetString("hotkey_" + (i + 1), "");
        }

        static void Save(int i)
        {
            PlayerPrefs.SetString("hotkey_" + (i + 1), names[i] ?? "");
            PlayerPrefs.Save();
        }

        /// <summary>Arma del inventario asignada a la tecla (0-3), o null.</summary>
        public static ItemData Resolve(Inventory inv, int slot)
        {
            Load();
            if (inv == null || slot < 0 || slot >= Count || string.IsNullOrEmpty(names[slot])) return null;
            foreach (var s in inv.slots)
                if (!s.IsEmpty && s.item.type == ItemType.Weapon && s.item.name == names[slot]) return s.item;
            return null;
        }

        /// <summary>Tecla (0-3) a la que esta asignada el arma, o -1.</summary>
        public static int SlotOf(ItemData item)
        {
            Load();
            if (item == null) return -1;
            for (int i = 0; i < Count; i++) if (names[i] == item.name) return i;
            return -1;
        }

        /// <summary>Asigna el arma a la tecla (la quita de la que tuviera antes). Si ya estaba en esa tecla, la libera. Devuelve true si queda asignada.</summary>
        public static bool Assign(int slot, ItemData item)
        {
            Load();
            if (slot < 0 || slot >= Count || item == null || item.type != ItemType.Weapon) return false;
            if (names[slot] == item.name) { names[slot] = ""; Save(slot); return false; }
            for (int i = 0; i < Count; i++) if (names[i] == item.name) { names[i] = ""; Save(i); }
            names[slot] = item.name; Save(slot);
            return true;
        }

        /// <summary>Las armas del inventario sin tecla ocupan la primera libre (la pistola al empezar queda en la 1).</summary>
        public static void EnsureDefaults(Inventory inv)
        {
            Load();
            if (inv == null) return;
            foreach (var s in inv.slots)
            {
                if (s.IsEmpty || s.item.type != ItemType.Weapon || SlotOf(s.item) >= 0) continue;
                for (int i = 0; i < Count; i++)
                    if (string.IsNullOrEmpty(names[i]) || Resolve(inv, i) == null)       // libre, o asignada a un arma que ya no tienes
                    { names[i] = s.item.name; Save(i); break; }
            }
        }
    }
}
