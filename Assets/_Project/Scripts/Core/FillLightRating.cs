using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Intensidad nominal de una luz de relleno de lampara. Se guarda aparte porque la de la Light queda a 0
    /// si la escena se guarda con la lampara apagada (igual que CeilingLamp.ratedIntensity).
    /// </summary>
    public class FillLightRating : MonoBehaviour
    {
        public float rated;
    }
}
