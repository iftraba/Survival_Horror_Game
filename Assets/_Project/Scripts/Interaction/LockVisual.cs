using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Lo que se ve de una cerradura (comisaria grande): el candado con su cadena en la hoja, o el piloto rojo del lector de
    /// tarjetas. Mientras la puerta siga cerrada con llave se ve 'lockedVisual'; al desbloquearla se apaga y se enciende
    /// 'unlockedVisual' (piloto verde). Tambien al cargar una partida con la puerta ya abierta.
    /// </summary>
    public class LockVisual : MonoBehaviour
    {
        public Door door;
        public GameObject lockedVisual;
        public GameObject unlockedVisual;
        bool last = true;

        void Start() => Apply(true);
        void Update() => Apply(false);

        void Apply(bool force)
        {
            if (door == null) return;
            bool locked = !door.IsUnlocked;
            if (!force && locked == last) return;
            last = locked;
            if (lockedVisual != null) lockedVisual.SetActive(locked);
            if (unlockedVisual != null) unlockedVisual.SetActive(!locked);
        }
    }
}
