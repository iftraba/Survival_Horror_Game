using UnityEngine;

namespace Horror
{
    public enum ItemType { Weapon, Ammo, Healing, Key, Misc, Bag }
    public enum AmmoType { None, Handgun, Shotgun, Rifle }

    [CreateAssetMenu(menuName = "Horror/Item", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        public string displayName = "Objeto";
        [TextArea] public string description;
        public ItemType type = ItemType.Misc;
        public Sprite icon;
        public Color tint = Color.white;
        [Min(1)] public int maxStack = 1;

        /// <summary>Objeto clave de un puzle (llaves, tarjetas, cizalla, medallones, fusibles): no se puede tirar hasta que este usado por completo.</summary>
        public bool IsKey => type == ItemType.Key;

        [Header("En el mundo")]
        [Tooltip("Modelo que se ve en el suelo")] public GameObject worldPrefab;
        public float worldScale = 1f;
        [Tooltip("Masa del cuerpo rigido (kg)")] public float mass = 0.5f;
        [Tooltip("Objeto clave de un puzle: en el mundo lleva un brillo suave pulsante (PickupGlint) para distinguirlo del desorden")] public bool highlight;

        [Header("Objetivo")]
        [Tooltip("Si no esta vacio, al recoger este objeto el objetivo de la partida pasa a este texto")]
        public string pickupObjective;

        [Header("Bag")]
        [Tooltip("Casillas que suma al inventario de forma permanente al recogerla (la bolsa no ocupa casilla)")]
        public int extraSlots = 2;

        [Header("Healing")]
        public float healAmount = 50f;

        [Header("Weapon")]
        public WeaponData weapon;

        [Header("Ammo")]
        public AmmoType ammoType = AmmoType.None;
    }
}
