using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Contenido del baul de objetos. Es UNICO para toda la partida (estilo Resident Evil): lo que guardas en un baul
    /// aparece en cualquier otro. Se guarda con la partida.
    /// </summary>
    public static class ItemStorage
    {
        public const int Capacity = 48;
        public static readonly List<ItemStack> Slots = new List<ItemStack>();

        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Slots.Clear();
            Changed?.Invoke();
        }

        /// <summary>Mete un monton; se apila con los del mismo objeto. Devuelve cuantos no cupieron.</summary>
        public static int Put(ItemData item, int count)
        {
            if (item == null || count <= 0) return count;
            for (int i = 0; i < Slots.Count && count > 0; i++)
            {
                if (Slots[i].item != item) continue;
                int add = Mathf.Min(item.maxStack - Slots[i].count, count);
                if (add <= 0) continue;
                Slots[i] = new ItemStack { item = item, count = Slots[i].count + add };
                count -= add;
            }
            while (count > 0 && Slots.Count < Capacity)
            {
                int add = Mathf.Min(item.maxStack, count);
                Slots.Add(new ItemStack { item = item, count = add });
                count -= add;
            }
            Changed?.Invoke();
            return count;
        }

        public static void RemoveAt(int index, int count)
        {
            if (index < 0 || index >= Slots.Count) return;
            var s = Slots[index];
            s.count -= count;
            if (s.count <= 0) Slots.RemoveAt(index); else Slots[index] = s;
            Changed?.Invoke();
        }

        public static void Set(IEnumerable<ItemStack> stacks)
        {
            Slots.Clear();
            foreach (var s in stacks) if (!s.IsEmpty && Slots.Count < Capacity) Slots.Add(s);
            Changed?.Invoke();
        }
    }
}
