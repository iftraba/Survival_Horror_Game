using UnityEngine;

namespace Horror
{
    [CreateAssetMenu(menuName = "Horror/Weapon", fileName = "NewWeapon")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "Arma";
        [Tooltip("Modelo que se muestra en la mano al equiparla")] public GameObject heldPrefab;
        public AudioClip fireSound;
        [Tooltip("Volumen del disparo. Mas de 1 suma una segunda fuente de audio (un AudioSource no pasa de 1)")]
        [Range(0.1f, 3f)] public float fireVolume = 1f;
        public AudioClip reloadSound;
        [Tooltip("Escala del modelo en la mano (los personajes son bloques grandes, el arma se agranda para equilibrar)")]
        public float heldScale = 1f;
        public AmmoType ammoType = AmmoType.Handgun;
        public float damage = 25f;
        public float range = 40f;
        [Tooltip("Disparos por segundo")] public float fireRate = 3f;
        public int magazineSize = 12;
        public float reloadTime = 1.6f;
        public int pellets = 1;
        [Tooltip("Dispersion en grados")] public float spread = 0.5f;
        public bool automatic;
        [Tooltip("Arma larga a dos manos (escopeta, rifle): usa el agarre y las animaciones de arma larga")]
        public bool twoHanded;
    }
}
