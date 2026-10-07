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
        [Tooltip("Sonido que suena tras cada disparo (el bombeo de la escopeta). Vacio = ninguno")]
        public AudioClip cycleSound;
        [Tooltip("Segundos tras el disparo a los que suena el ciclo")] public float cycleDelay = 0.45f;
        [Range(0.1f, 2f)] public float cycleVolume = 0.6f;
        [Header("Casquillos")]
        [Tooltip("Casquillo/cartucho vacio que sale del arma (vacio = ninguno)")] public GameObject casingPrefab;
        [Tooltip("true = sale al bombear (escopeta); false = sale al disparar (pistola)")] public bool ejectAtCycle;
        [Tooltip("Donde sale respecto a la mano del arma, en ejes de la camara de apuntado (derecha, arriba, delante), en metros")]
        public Vector3 ejectOffset = new Vector3(0.02f, 0.05f, 0.15f);
        [Tooltip("Velocidad de salida (m/s) hacia la derecha; la salida hacia arriba es algo menor")] public float ejectSpeed = 2.2f;
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
