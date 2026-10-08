using UnityEngine;

namespace Horror
{
    /// <summary>Deja el Animator en el ultimo fotograma de su clip (cadaveres: la caida de la animacion de muerte, ya tumbados).</summary>
    public class HoldLastFrame : MonoBehaviour
    {
        void Start()
        {
            var a = GetComponentInChildren<Animator>();
            if (a == null) return;
            a.Play(0, 0, 1f);
            a.Update(0f);
        }
    }
}
