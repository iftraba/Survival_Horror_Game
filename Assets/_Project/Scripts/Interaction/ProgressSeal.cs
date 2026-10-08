using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Mantiene atrancada una puerta hasta que se cumple una marca de progreso (comisaria grande: la puerta del pasillo de las
    /// calderas no tiene corriente hasta poner los tres fusibles). Con la marca, la puerta se libera sola (tambien al cargar).
    /// </summary>
    public class ProgressSeal : MonoBehaviour
    {
        public Door door;
        public string flag = "corriente";
        [Tooltip("Luz o piloto que se enciende cuando hay corriente")] public GameObject poweredVisual;
        [Tooltip("Luz o piloto que se ve sin corriente")] public GameObject unpoweredVisual;

        void OnEnable() { Progress.Changed += Check; }
        void OnDisable() => Progress.Changed -= Check;
        void Start() => Check();

        void Check()
        {
            if (door == null) return;
            bool on = Progress.Has(flag);
            if (on) door.Unseal(); else door.Seal();
            if (poweredVisual != null) poweredVisual.SetActive(on);
            if (unpoweredVisual != null) unpoweredVisual.SetActive(!on);
        }
    }
}
