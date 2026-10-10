using System;
using UnityEngine;

namespace Horror
{
    [Serializable]
    public struct ItemStack
    {
        public ItemData item;
        public int count;
        public bool IsEmpty => item == null || count <= 0;
    }

    /// <summary>Inventario por casillas estilo Resident Evil.</summary>
    public class Inventory : MonoBehaviour
    {
        public int slotCount = 8;
        [Tooltip("Tope de casillas extra que pueden dar las bolsas")] public int maxBagSlots = 6;
        public ItemStack[] slots;

        public event Action Changed;

        /// <summary>Casillas extra ganadas con bolsas (se guardan en la partida).</summary>
        public int BagSlots { get; private set; }

        /// <summary>Suma casillas de forma permanente. Devuelve cuantas se anadieron realmente (respeta el tope).</summary>
        public int AddBagSlots(int extra)
        {
            int add = Mathf.Clamp(extra, 0, maxBagSlots - BagSlots);
            if (add > 0) SetBagSlots(BagSlots + add);
            return add;
        }

        /// <summary>Fija el total de casillas de bolsa (al cargar una partida) conservando el contenido.</summary>
        public void SetBagSlots(int total)
        {
            BagSlots = Mathf.Clamp(total, 0, maxBagSlots);
            int size = slotCount + BagSlots;
            if (slots == null || slots.Length == size) { Changed?.Invoke(); return; }
            var old = slots;
            slots = new ItemStack[size];
            if (old != null) Array.Copy(old, slots, Mathf.Min(old.Length, size));
            Changed?.Invoke();
        }

        void Awake()
        {
            if (slots == null || slots.Length != slotCount)
            {
                var old = slots;
                slots = new ItemStack[slotCount];
                if (old != null) Array.Copy(old, slots, Mathf.Min(old.Length, slotCount));
            }
        }

        /// <summary>Anade objetos. Devuelve cuantos NO cupieron.</summary>
        public int TryAdd(ItemData item, int count)
        {
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].item != item) continue;
                int space = item.maxStack - slots[i].count;
                int add = Mathf.Min(space, count);
                slots[i].count += add;
                count -= add;
            }
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                int add = Mathf.Min(item.maxStack, count);
                slots[i] = new ItemStack { item = item, count = add };
                count -= add;
            }
            Changed?.Invoke();
            return count;
        }

        /// <summary>Sustituye el contenido de las casillas (al cargar una partida).</summary>
        public void SetSlots(System.Collections.Generic.IList<ItemStack> stacks)
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i] = i < stacks.Count ? stacks[i] : default;
            Changed?.Invoke();
        }

        /// <summary>
        /// Mueve la pila de 'from' a 'to' (arrastrar en el inventario): a una casilla vacia la traslada, sobre el mismo objeto apilable
        /// suma hasta el tope de pila y en cualquier otro caso intercambia las dos. Devuelve false si no hacia nada.
        /// </summary>
        public bool Move(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= slots.Length || to >= slots.Length || slots[from].IsEmpty) return false;
            var a = slots[from]; var b = slots[to];
            if (!b.IsEmpty && b.item == a.item && a.item.maxStack > 1)
            {
                int room = a.item.maxStack - b.count;
                if (room > 0)
                {
                    int mv = room < a.count ? room : a.count;
                    slots[to].count += mv; slots[from].count -= mv;
                    if (slots[from].count <= 0) slots[from] = default;
                    Changed?.Invoke();
                    return true;
                }
            }
            slots[from] = b; slots[to] = a;
            Changed?.Invoke();
            return true;
        }

        public void RemoveAt(int index, int count = 1)
        {
            if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;
            slots[index].count -= count;
            if (slots[index].count <= 0) slots[index] = default;
            Changed?.Invoke();
        }

        public bool Has(ItemData item)
        {
            foreach (var s in slots) if (!s.IsEmpty && s.item == item) return true;
            return false;
        }

        public bool Remove(ItemData item, int count = 1)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty || slots[i].item != item) continue;
                RemoveAt(i, count);
                return true;
            }
            return false;
        }

        public int CountAmmo(AmmoType type)
        {
            int total = 0;
            foreach (var s in slots)
                if (!s.IsEmpty && s.item.type == ItemType.Ammo && s.item.ammoType == type) total += s.count;
            return total;
        }

        /// <summary>Gasta municion del inventario. Devuelve la cantidad realmente consumida.</summary>
        public int ConsumeAmmo(AmmoType type, int amount)
        {
            int taken = 0;
            for (int i = 0; i < slots.Length && taken < amount; i++)
            {
                if (slots[i].IsEmpty || slots[i].item.type != ItemType.Ammo || slots[i].item.ammoType != type) continue;
                int t = Mathf.Min(slots[i].count, amount - taken);
                slots[i].count -= t;
                taken += t;
                if (slots[i].count <= 0) slots[i] = default;
            }
            if (taken > 0) Changed?.Invoke();
            return taken;
        }

        public void Use(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;
            var item = slots[index].item;
            switch (item.type)
            {
                case ItemType.Healing:
                    var hp = GetComponent<Health>();
                    if (hp != null && hp.Current < hp.maxHealth)
                    {
                        hp.Heal(item.healAmount);
                        RemoveAt(index);
                        Hud.Message($"Usaste {item.displayName}");
                    }
                    else Hud.Message("Ya tienes la salud completa");
                    break;
                case ItemType.Weapon:
                    GetComponent<WeaponController>()?.Equip(item.weapon);
                    Hud.Message($"{item.displayName} equipada");
                    break;
                default:
                    Hud.Message(item.displayName);
                    break;
            }
        }

        public void Drop(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;
            var s = slots[index];
            var wc = GetComponent<WeaponController>();
            if (s.item.type == ItemType.Weapon && wc != null && wc.Equipped == s.item.weapon) { Hud.Message("Equipa otra arma antes de tirar la que llevas"); return; }
            slots[index] = default;
            Changed?.Invoke();
            // Tirar es desechar: el objeto desaparece del todo (antes salia lanzado al suelo y se acumulaba por el nivel)
            Hud.Message(s.count > 1 ? $"{s.item.displayName} x{s.count} desechado" : $"{s.item.displayName} desechado");
        }
    }
}
